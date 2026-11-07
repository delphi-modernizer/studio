using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DelphiModernizer.Engine.Agents;
using DelphiModernizer.Engine.Core;
using DelphiModernizer.Engine.Delphi.Inventory;
using DelphiModernizer.Engine.Kit;
using DelphiModernizer.Engine.Traceability;
using DelphiModernizer.Engine.Workspaces;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;
using DelphiModernizer.Studio.Application.Abstractions.Workspaces;
using DelphiModernizer.Studio.Application.Abstractions.Workstreams;

namespace DelphiModernizer.Studio.Modules.Overview.Application;

/// <summary>Everything the Overview page shows, gathered in one pass over the open workspace.</summary>
public sealed record GetOverviewQuery : IRequest<Result<WorkspaceOverview>>;

/// <param name="Inventory">The latest scan's summary, or null before the first scan.</param>
public sealed record WorkspaceOverview(
    string Name,
    string TargetStack,
    InventorySummary? Inventory,
    int Cycles,
    IReadOnlyList<GateStatus> Gates,
    TraceReport Trace,
    IReadOnlyList<WorkstreamProgress> Workstreams,
    IReadOnlyList<OpenQuestion> Questions,
    IReadOnlyList<RunSummary> RecentRuns);

/// <summary>A question the trace check reports as still open, with the words its register row asks.</summary>
public sealed record OpenQuestion(string Id, string Text, string Path, int Line);

/// <summary>How far a role's deliverables have come: one counts once it is past the template.</summary>
public sealed record WorkstreamProgress(WorkstreamRole Role, IReadOnlyList<string> Deliverables, int Started)
{
    public double Fraction => Deliverables.Count == 0 ? 0 : (double)Started / Deliverables.Count;
}

internal sealed class GetOverviewQueryHandler(IWorkspaceSession session)
    : IRequestHandler<GetOverviewQuery, Result<WorkspaceOverview>>
{
    private const int _recentRuns = 5;

    public Task<Result<WorkspaceOverview>> Handle(GetOverviewQuery request, CancellationToken cancellationToken)
    {
        return Task.Run(
            () => session.Current is { } workspace
                ? Result.Success(Gather(workspace))
                : Result.Failure<WorkspaceOverview>(WorkspaceRequired.Error),
            cancellationToken);
    }

    private static WorkspaceOverview Gather(Workspace workspace)
    {
        var inventory = ReadInventory(workspace);
        var trace = new TraceChecker(KitBundle.Embedded.Spec).Check(workspace.Root, strict: false);
        var progress = WorkstreamRole.All
            .Select(role => Progress(role, trace))
            .ToList();
        var runs = RunStore
            .List(workspace.Root)
            .OrderByDescending(r => r.StartedAt)
            .Take(_recentRuns)
            .ToList();

        return new WorkspaceOverview(
            workspace.Manifest.Name,
            workspace.Manifest.TargetStack,
            inventory?.Summary,
            inventory?.Dependencies.Cycles.Length ?? 0,
            new GateEvaluator(workspace).Evaluate(),
            trace,
            progress,
            Questions(workspace, trace),
            runs);
    }

    /// <remarks>The question is the register row's second cell; the row's line comes from the trace check.</remarks>
    private static List<OpenQuestion> Questions(Workspace workspace, TraceReport trace)
    {
        var questions = new List<OpenQuestion>();
        var files = new Dictionary<string, string[]>(StringComparer.Ordinal);

        foreach (var issue in trace.Issues.Where(i => i.Rule == "open-question" && i.RecordId is not null))
        {
            if (!files.TryGetValue(issue.Path, out var lines))
            {
                var path = Path.Combine(workspace.Root, issue.Path);
                lines = File.Exists(path) ? File.ReadAllLines(path) : [];
                files[issue.Path] = lines;
            }

            var cells = issue.Line > 0 && issue.Line <= lines.Length ? lines[issue.Line - 1].Split('|') : [];
            var text = cells.Length > 2 ? cells[2].Trim() : issue.Message;
            questions.Add(new OpenQuestion(issue.RecordId!, text, issue.Path, issue.Line));
        }

        return questions;
    }

    private static WorkstreamProgress Progress(WorkstreamRole role, TraceReport trace)
    {
        var owned = trace.Deliverables
            .Where(d => d.Owner == role.Code)
            .ToList();

        return new WorkstreamProgress(
            role,
            [.. owned.Select(d => d.Id)],
            owned.Count(d => d.Status is not ("template" or "missing")));
    }

    private static LegacyInventory? ReadInventory(Workspace workspace)
    {
        var path = Path.Combine(workspace.LatestScanFolder, "inventory.json");

        try
        {
            return File.Exists(path)
                ? JsonSerializer.Deserialize(File.ReadAllBytes(path), InventoryJsonContext.Default.LegacyInventory)
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
