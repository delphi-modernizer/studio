using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DelphiModernizer.Engine.Agents;
using DelphiModernizer.Engine.Workspaces;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;
using DelphiModernizer.Studio.Application.Abstractions.Settings;
using DelphiModernizer.Studio.Application.Abstractions.Workspaces;
using DelphiModernizer.Studio.Application.Abstractions.Workstreams;
using DelphiModernizer.Studio.Tests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DelphiModernizer.Studio.Modules.Workstreams.Application.Tests;

public sealed class RunTests
{
    private const string _init =
        """{"type":"system","subtype":"init","session_id":"s-1","""
        + """ "model":"claude-test","claude_code_version":"2.1.300"}""";

    private const string _success =
        """{"type":"result","subtype":"success","is_error":false,"total_cost_usd":0.25,"num_turns":3}""";

    /// <summary>An Edit of the legacy tree, and the guard hook's refusal as Claude Code reports it.</summary>
    private static readonly string[] _refusedEdit =
    [
        """{"type":"assistant","message":{"content":[{"type":"tool_use","id":"t-1","name":"Edit","""
            + """ "input":{"file_path":"legacy/Sales/Pricing.pas"}}]}}""",
        """{"type":"user","message":{"content":[{"type":"tool_result","tool_use_id":"t-1","is_error":true,"""
            + """ "content":"PreToolUse:Edit hook error: [dmz guard pre-tool-use || exit 2]: """
            + """legacy/ is read-only evidence; record findings in docs/ instead."}]}}""",
    ];

    [Fact]
    public async Task A_run_streams_its_events_and_joins_the_history_when_it_ends()
    {
        using var workspace = await TestWorkspace.CreateAsync();
        var fake = new FakeClaude(
            workspace.Parent,
            _init,
            """{"type":"assistant","message":{"content":[{"type":"text","""
                + """ "text":"**[Confirmed]** Gold customers get 7.5 %."}]}}""",
            """{"type":"result","subtype":"success","is_error":false,"total_cost_usd":0.25,"num_turns":3}""");
        var (sender, monitor) = Build(workspace.Session, fake);

        var started = await sender.Send(
            new StartRunCommand("ws01-business-rules", " legacy/Sales ", 1.5m), TestContext.Current.CancellationToken);
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
            """{"type":"result","subtype":"error_max_budget_usd","is_error":true,"""
                + """ "total_cost_usd":1.0,"num_turns":9}""");
        var (sender, _) = Build(workspace.Session, fake);

        var started = await sender.Send(
            new StartRunCommand("ws01-discover", null, 1m), TestContext.Current.CancellationToken);
        var completed = await started.Value.Ended;

        Assert.Equal(RunOutcome.BudgetReached, RunOutcomes.Of(completed.Outcome));
    }

    [Fact]
    public async Task While_a_run_writes_a_deliverable_another_task_that_writes_it_is_blocked_until_it_stops()
    {
        using var workspace = await TestWorkspace.CreateAsync();
        var fake = new FakeClaude(workspace.Parent, _init, "#sleep 5000");
        var (sender, monitor) = Build(workspace.Session, fake);

        var first = await sender.Send(
            new StartRunCommand("ws01-business-rules", "legacy/Sales", 1m), TestContext.Current.CancellationToken);
        var second = await sender.Send(
            new StartRunCommand("ws02-frs", null, 1m), TestContext.Current.CancellationToken);
        var blockers = await sender.Send(new GetRunBlockersQuery(), TestContext.Current.CancellationToken);
        Assert.Equal(1, monitor.InProgress);
        first.Value.Stop();
        var stopped = await first.Value.Ended;
        var again = await sender.Send(new StartRunCommand("ws02-frs", null, 1m), TestContext.Current.CancellationToken);
        again.Value.Stop();
        await again.Value.Ended;

        var after = await sender.Send(new GetRunBlockersQuery(), TestContext.Current.CancellationToken);

        Assert.True(second.IsFailure);
        Assert.Same(first.Value, blockers.Value["ws02-frs"].Run);
        Assert.Equal(RunOutcome.Stopped, RunOutcomes.Of(stopped.Outcome));
        Assert.True(again.IsSuccess);
        Assert.Empty(after.Value);
    }

    [Fact]
    public async Task A_lease_held_by_a_run_the_studio_did_not_start_blocks_the_tasks_that_write_it()
    {
        using var workspace = await TestWorkspace.CreateAsync();
        var (sender, _) = Build(workspace.Session, new FakeClaude(workspace.Parent, _init));
        var held = RunLocks.Acquire(workspace.Workspace, "20261108101500-ws01-business-rules", ["D03"]);

        var blockers = await sender.Send(new GetRunBlockersQuery(), TestContext.Current.CancellationToken);
        var started = await sender.Send(
            new StartRunCommand("ws02-frs", null, 1m), TestContext.Current.CancellationToken);

        Assert.True(held.IsSuccess);
        var blocker = blockers.Value["ws02-frs"];
        Assert.Equal("20261108101500-ws01-business-rules", blocker.RunId);
        Assert.Null(blocker.Run);
        Assert.False(blockers.Value.ContainsKey("ws02-nfr"));
        Assert.Equal("Run.Locked", started.Error.Code);
    }

    [Fact]
    public async Task A_refused_tool_call_arrives_as_the_guards_refusal_in_place_of_its_result()
    {
        using var workspace = await TestWorkspace.CreateAsync();
        var fake = new FakeClaude(workspace.Parent, [_init, .. _refusedEdit, _success]);
        var (sender, _) = Build(workspace.Session, fake);

        var started = await sender.Send(
            new StartRunCommand("ws01-business-rules", "legacy/Sales", 1m), TestContext.Current.CancellationToken);
        await started.Value.Ended;
        var events = started.Value.Subscribe((_, _) => { });

        var refused = Assert.Single(events.OfType<GuardRefused>());
        Assert.Equal(("t-1", "Edit", "legacy/Sales/Pricing.pas"), (refused.ToolCallId, refused.Tool, refused.Target));
        Assert.Equal("legacy/ is read-only evidence; record findings in docs/ instead.", refused.Reason);
        Assert.DoesNotContain(events, e => e is ToolResult);
    }

    [Fact]
    public async Task An_earlier_run_reads_back_as_the_events_it_streamed()
    {
        using var workspace = await TestWorkspace.CreateAsync();
        var fake = new FakeClaude(workspace.Parent, [_init, .. _refusedEdit, _success]);
        var (sender, _) = Build(workspace.Session, fake);
        var started = await sender.Send(
            new StartRunCommand("ws01-business-rules", "legacy/Sales", 1m), TestContext.Current.CancellationToken);
        await started.Value.Ended;
        var live = started.Value.Subscribe((_, _) => { });

        var replayed = await sender.Send(
            new GetRunTranscriptQuery(started.Value.RunId), TestContext.Current.CancellationToken);
        var outside = await sender.Send(new GetRunTranscriptQuery("../x"), TestContext.Current.CancellationToken);

        Assert.Equal(live.Where(e => e is not Diagnostic { Source: "stderr" }), replayed.Value);
        Assert.Contains(replayed.Value, e => e is GuardRefused);
        Assert.Equal("success", Assert.IsType<RunCompleted>(replayed.Value[^1]).Outcome);
        Assert.Equal("Workstreams.UnknownRun", outside.Error.Code);
    }

    [Fact]
    public async Task Pages_outside_Workstreams_see_which_agents_have_a_run_in_progress()
    {
        using var workspace = await TestWorkspace.CreateAsync();
        var fake = new FakeClaude(workspace.Parent, _init, "#sleep 5000");
        var (sender, monitor) = Build(workspace.Session, fake);

        var run = await sender.Send(
            new StartRunCommand("ws01-business-rules", "legacy/Sales", 1m),
            TestContext.Current.CancellationToken);
        var running = monitor.AgentsRunning(workspace.Workspace.Root);
        var elsewhere = monitor.AgentsRunning(workspace.Parent);
        run.Value.Stop();
        await run.Value.Ended;

        Assert.Equal("ws01-legacy-discovery", Assert.Single(running));
        Assert.Empty(elsewhere);
        Assert.Empty(monitor.AgentsRunning(workspace.Workspace.Root));
    }

    [Fact]
    public async Task A_run_needs_a_budget()
    {
        using var workspace = await TestWorkspace.CreateAsync();
        var (sender, _) = Build(workspace.Session, new FakeClaude(workspace.Parent, _init));

        var started = await sender.Send(
            new StartRunCommand("ws01-discover", null, 0m), TestContext.Current.CancellationToken);

        Assert.Equal("Workstreams.NoBudget", started.Error.Code);
    }

    [Fact]
    public async Task Each_deliverable_has_the_status_its_file_records_and_no_sign_off_starts_approved()
    {
        using var workspace = await TestWorkspace.CreateAsync();
        var (sender, _) = Build(workspace.Session, new FakeClaude(workspace.Parent));

        var states = await sender.Send(new GetDeliverableStatesQuery(), TestContext.Current.CancellationToken);

        Assert.Equal(20, states.Value.Deliverables.Count);
        Assert.Contains(states.Value.Deliverables, d => d.Id == "D03" && d.Owner == "WS02");
        Assert.Equal(["G0", "G1", "G2", "G3", "G4"], states.Value.Gates.Select(g => g.Gate));
        Assert.DoesNotContain(states.Value.Gates, g => g.State == "approved");
    }

    [Fact]
    public void Fact_labels_are_taken_out_of_the_agent_text()
    {
        var lines = RunTranscript.Lines(
            "I read Pricing.pas.\n\n**[Confirmed]** BR-001 Gold discount.\nGold follows the limit. [Inferred]");

        Assert.Equal(
            [
                new AgentLine("I read Pricing.pas.", null),
                new AgentLine("BR-001 Gold discount.", "Confirmed"),
                new AgentLine("Gold follows the limit.", "Inferred"),
            ],
            lines);
    }

    [Fact]
    public void A_tool_call_names_a_workspace_file_relative_to_its_root()
    {
        string root = Path.Combine(Path.GetTempPath(), "ws");
        string absolute = Path.Combine(root, "legacy", "Sales", "Pricing.pas");
        string inside = System.Text.Json.JsonSerializer.Serialize(absolute);
        var read = new ToolCall("t-1", "Read", $$"""{"file_path":{{inside}}}""", false);
        var grep = new ToolCall("t-2", "Grep", """{"pattern":"CREDIT_LIMIT","path":"legacy/Sales"}""", false);
        string temp = System.Text.Json.JsonSerializer.Serialize(Path.GetTempPath());
        var outside = new ToolCall("t-3", "Read", $$"""{"file_path":{{temp}}}""", false);

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
