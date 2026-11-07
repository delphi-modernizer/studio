using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DelphiModernizer.Engine.Agents;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;
using DelphiModernizer.Studio.Application.Abstractions.Settings;
using DelphiModernizer.Studio.Application.Abstractions.Workspaces;
using DelphiModernizer.Studio.Tests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DelphiModernizer.Studio.Modules.Workstreams.Application.Tests;

public sealed class RunTests
{
    private const string _init = """{"type":"system","subtype":"init","session_id":"s-1","model":"claude-test","claude_code_version":"2.1.300"}""";

    [Fact]
    public async Task A_run_streams_its_events_and_joins_the_history_when_it_ends()
    {
        using var workspace = await TestWorkspace.CreateAsync();
        var fake = new FakeClaude(
            workspace.Parent,
            _init,
            """{"type":"assistant","message":{"content":[{"type":"text","text":"**[Confirmed]** Gold customers get 7.5 %."}]}}""",
            """{"type":"result","subtype":"success","is_error":false,"total_cost_usd":0.25,"num_turns":3}""");
        var (sender, monitor) = Build(workspace.Session, fake);

        var started = await sender.Send(new StartRunCommand("ws01-business-rules", " legacy/Sales ", 1.5m), TestContext.Current.CancellationToken);
        var completed = await started.Value.Ended;
        var runs = await sender.Send(new GetRunsQuery(), TestContext.Current.CancellationToken);

        Assert.Equal("success", completed.Outcome);
        Assert.Contains(started.Value.Subscribe((_, _) => { }), e => e is AssistantText);
        Assert.Equal("1.5", fake.Arguments[Array.IndexOf(fake.Arguments, "--max-budget-usd") + 1]);
        Assert.Equal("/extract-business-rules legacy/Sales", fake.Arguments[Array.IndexOf(fake.Arguments, "-p") + 1]);
        var run = Assert.Single(runs.Value);
        Assert.Equal(RunOutcome.Completed, run.Outcome);
        Assert.Equal("ws01-business-rules", run.Recipe?.Id);
        Assert.Equal("legacy/Sales", run.Argument);
        Assert.Equal(0, monitor.InProgress);
    }

    [Fact]
    public async Task A_run_that_reaches_its_budget_says_so()
    {
        using var workspace = await TestWorkspace.CreateAsync();
        var fake = new FakeClaude(
            workspace.Parent,
            _init,
            """{"type":"result","subtype":"error_max_budget_usd","is_error":true,"total_cost_usd":1.0,"num_turns":9}""");
        var (sender, _) = Build(workspace.Session, fake);

        var started = await sender.Send(new StartRunCommand("ws01-discover", null, 1m), TestContext.Current.CancellationToken);
        var completed = await started.Value.Ended;

        Assert.Equal(RunOutcome.BudgetReached, RunOutcomes.Of(completed.Outcome));
    }

    [Fact]
    public async Task While_a_run_writes_a_deliverable_another_task_that_writes_it_is_blocked_until_it_stops()
    {
        using var workspace = await TestWorkspace.CreateAsync();
        var fake = new FakeClaude(workspace.Parent, _init, "#sleep 5000");
        var (sender, monitor) = Build(workspace.Session, fake);

        var first = await sender.Send(new StartRunCommand("ws01-business-rules", "legacy/Sales", 1m), TestContext.Current.CancellationToken);
        var second = await sender.Send(new StartRunCommand("ws02-frs", null, 1m), TestContext.Current.CancellationToken);
        var writing = monitor.Writing(workspace.Workspace.Root, ["D03"]);
        Assert.Equal(1, monitor.InProgress);
        first.Value.Stop();
        var stopped = await first.Value.Ended;
        var again = await sender.Send(new StartRunCommand("ws02-frs", null, 1m), TestContext.Current.CancellationToken);
        again.Value.Stop();
        await again.Value.Ended;

        Assert.True(second.IsFailure);
        Assert.Same(first.Value, writing);
        Assert.Equal(RunOutcome.Stopped, RunOutcomes.Of(stopped.Outcome));
        Assert.True(again.IsSuccess);
        Assert.Null(monitor.Writing(workspace.Workspace.Root, ["D03"]));
    }

    [Fact]
    public async Task A_run_needs_a_budget()
    {
        using var workspace = await TestWorkspace.CreateAsync();
        var (sender, _) = Build(workspace.Session, new FakeClaude(workspace.Parent, _init));

        var started = await sender.Send(new StartRunCommand("ws01-discover", null, 0m), TestContext.Current.CancellationToken);

        Assert.Equal("Workstreams.NoBudget", started.Error.Code);
    }

    [Fact]
    public async Task Each_deliverable_has_the_status_its_file_records()
    {
        using var workspace = await TestWorkspace.CreateAsync();
        var (sender, _) = Build(workspace.Session, new FakeClaude(workspace.Parent));

        var states = await sender.Send(new GetDeliverableStatesQuery(), TestContext.Current.CancellationToken);

        Assert.Equal(20, states.Value.Count);
        Assert.Contains(states.Value, d => d.Id == "D03" && d.Owner == "WS02");
    }

    [Fact]
    public void Fact_labels_are_taken_out_of_the_agent_text()
    {
        var lines = RunTranscript.Lines("I read Pricing.pas.\n\n**[Confirmed]** BR-001 Gold discount.\nGold follows the limit. [Inferred]");

        Assert.Equal(
            [new AgentLine("I read Pricing.pas.", null), new AgentLine("BR-001 Gold discount.", "Confirmed"), new AgentLine("Gold follows the limit.", "Inferred")],
            lines);
    }

    [Fact]
    public void A_call_the_guard_blocked_is_a_refusal_with_the_guard_reason()
    {
        var refused = new ToolResult("t-1", true, "PreToolUse:Edit hook error: [dmz guard pre-tool-use]: legacy/ is read-only evidence; record findings in docs/ instead.");
        var failed = new ToolResult("t-2", true, "File does not exist.");

        Assert.True(RunTranscript.IsRefusal(refused));
        Assert.Equal("legacy/ is read-only evidence; record findings in docs/ instead.", RunTranscript.RefusalReason(refused));
        Assert.False(RunTranscript.IsRefusal(failed));
    }

    [Fact]
    public void A_tool_call_names_a_workspace_file_relative_to_its_root()
    {
        var root = Path.Combine(Path.GetTempPath(), "ws");
        var absolute = Path.Combine(root, "legacy", "Sales", "Pricing.pas");
        var read = new ToolCall("t-1", "Read", $$"""{"file_path":{{System.Text.Json.JsonSerializer.Serialize(absolute)}}}""", false);
        var grep = new ToolCall("t-2", "Grep", """{"pattern":"CREDIT_LIMIT","path":"legacy/Sales"}""", false);
        var outside = new ToolCall("t-3", "Read", $$"""{"file_path":{{System.Text.Json.JsonSerializer.Serialize(Path.GetTempPath())}}}""", false);

        Assert.Equal("legacy/Sales/Pricing.pas", RunTranscript.PathOf(read, root));
        Assert.Equal("legacy/Sales", RunTranscript.PathOf(grep, root));
        Assert.Null(RunTranscript.PathOf(outside, root));
        Assert.False(RunTranscript.Edits(read));
    }

    private static (ISender Sender, RunMonitor Monitor) Build(IWorkspaceSession session, FakeClaude fake)
    {
        var services = new ServiceCollection();
        services.AddSingleton(session);
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
        services.AddSingleton<IStudioSettings>(new Settings());
        services.AddSingleton<IClaudeLocator>(fake);
        services.AddWorkstreamsApplication();
        var provider = services.BuildServiceProvider();

        return (provider.GetRequiredService<ISender>(), provider.GetRequiredService<RunMonitor>());
    }

    private sealed class Settings : IStudioSettings
    {
        public AgentSettings Agents => AgentSettings.Default;

        public int DefaultCodePage => 1252;

        public string? ToolDirectory => null;

        public void SaveAgents(AgentSettings settings)
        {
            // The tests never change the agent settings.
        }

        public void SaveDefaultCodePage(int codePage)
        {
            // The tests never change the code page.
        }
    }
}
