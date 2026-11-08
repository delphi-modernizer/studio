using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DelphiModernizer.Engine.Agents;
using DelphiModernizer.Engine.Workspaces;
using Xunit;

namespace DelphiModernizer.Studio.ClaudeStandIn.Tests;

/// <summary>Each scenario, run by the engine's runner as the studio runs it, read as the engine's events.</summary>
/// <remarks>The scenario is a process-wide variable, so the tests stay in one class, which runs them in turn.</remarks>
public sealed class ScenarioTests : IDisposable
{
    private const string _guardReason = "legacy/ is read-only evidence; record findings in docs/ instead.";
    private const string _draft = "docs/02-functional-requirements/D03-functional-requirements.md";

    private readonly string _parent = Directory.CreateTempSubdirectory("dmz-stand-in-").FullName;

    private static ClaudeExecutable StandIn => new("dotnet", [Path.Combine(AppContext.BaseDirectory, "claude.dll")]);

    [Theory]
    [InlineData("completed", "success")]
    [InlineData("budget", "error_max_budget_usd")]
    [InlineData("failed", "failed")]
    public async Task A_scenario_streams_the_run_board_and_ends_as_it_says(string scenario, string outcome)
    {
        var root = await WorkspaceAsync();

        var events = await RunAsync(root, scenario, CancellationToken.None);

        Assert.Equal("stand-in", Assert.IsType<SessionStarted>(events[0]).Model);
        var completed = Assert.IsType<RunCompleted>(events[^1]);
        Assert.Equal(outcome, completed.Outcome);
        var refused = Assert.Single(events.OfType<GuardRefused>());
        Assert.Equal(("Edit", "legacy/Sales/Pricing.pas", _guardReason), (refused.Tool, refused.Target, refused.Reason));
        Assert.Contains(
            events,
            e => e is AssistantText t && t.Text.Contains("**[Inferred]**", StringComparison.Ordinal));
        Assert.Contains(
            events,
            e => e is ToolResult r && r.Content.Contains("CustomerForm.pas:35:", StringComparison.Ordinal));
        Assert.Contains("status: draft", await File.ReadAllTextAsync(Path.Combine(root, _draft)));
        var pricing = await File.ReadAllTextAsync(Path.Combine(root, "legacy", "Sales", "Pricing.pas"));
        Assert.DoesNotContain("BR-001", pricing);
    }

    [Fact]
    public async Task A_budget_reached_costs_the_whole_budget_and_records_the_refusal()
    {
        var root = await WorkspaceAsync();

        var events = await RunAsync(root, "budget", CancellationToken.None);
        var completed = events
            .OfType<RunCompleted>()
            .Single();

        Assert.Equal((1.5m, 1), (completed.CostUsd, completed.PermissionDenials));
    }

    [Theory]
    [InlineData("stopped", true)]
    [InlineData("starting", false)]
    public async Task A_waiting_scenario_ends_when_the_run_is_stopped(string scenario, bool started)
    {
        var root = await WorkspaceAsync();
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        var events = new List<AgentEvent>();
        Environment.SetEnvironmentVariable(Program.ScenarioVariable, scenario);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => {
            await foreach (var item in new AgentRunner(StandIn).RunAsync(Request(root), stop.Token))
            {
                events.Add(item);
            }
        });

        Assert.Equal(started, events.Exists(e => e is SessionStarted));
        Assert.DoesNotContain(events, e => e is RunCompleted);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("too-old", false)]
    public async Task The_version_is_the_engine_minimum_or_one_patch_below(string? answer, bool accepted)
    {
        Environment.SetEnvironmentVariable(Program.VersionVariable, answer);

        var version = await StandIn.VersionAsync(CancellationToken.None);
        Environment.SetEnvironmentVariable(Program.VersionVariable, null);

        Assert.Equal(accepted, version.IsSuccess);
        Assert.Equal(accepted ? null : "Claude.TooOld", version.IsFailure ? version.Error.Code : null);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(Program.ScenarioVariable, null);
        Directory.Delete(_parent, recursive: true);
    }

    private static AgentRunRequest Request(string root)
    {
        return new AgentRunRequest(
            "run-1",
            root,
            "ws01-legacy-discovery",
            "/extract-business-rules legacy/Sales",
            $"{Guid.NewGuid()}",
            1.5m,
            null,
            null,
            null);
    }

    private static async Task<List<AgentEvent>> RunAsync(
        string root,
        string scenario,
        CancellationToken cancellationToken)
    {
        Environment.SetEnvironmentVariable(Program.ScenarioVariable, scenario);
        var events = new List<AgentEvent>();

        await foreach (var item in new AgentRunner(StandIn).RunAsync(Request(root), cancellationToken))
        {
            events.Add(item);
        }

        return events;
    }

    private async Task<string> WorkspaceAsync()
    {
        var request = new InitRequest(
            Path.Combine(_parent, "ws"),
            "Acme ERP modernization",
            Path.Combine(AppContext.BaseDirectory, "legacy-erp"),
            "dotnet",
            1252,
            "Test Person <test@example.com>",
            null,
            InitializeGit: false);
        var created = await WorkspaceInitializer.InitializeAsync(request, null, CancellationToken.None);

        return created.Value.Root;
    }
}
