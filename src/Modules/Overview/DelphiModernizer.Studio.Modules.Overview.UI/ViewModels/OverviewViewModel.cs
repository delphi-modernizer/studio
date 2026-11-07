using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DelphiModernizer.Engine.Agents;
using DelphiModernizer.Engine.Workspaces;
using DelphiModernizer.Studio.Application.Abstractions.Agents;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;
using DelphiModernizer.Studio.Application.Abstractions.SignOffs;
using DelphiModernizer.Studio.Application.Abstractions.Workspaces;
using DelphiModernizer.Studio.Application.Abstractions.Workstreams;
using DelphiModernizer.Studio.Modules.Overview.Application;
using DelphiModernizer.Studio.UI.Common;
using DelphiModernizer.Studio.UI.Common.Controls;
using DelphiModernizer.Studio.UI.Contracts.Services;

namespace DelphiModernizer.Studio.Modules.Overview.UI.ViewModels;

/// <summary>The Overview: from the Delphi code, through six teams and the Principal Architect, to five sign-offs.</summary>
public sealed partial class OverviewViewModel : WorkspacePageViewModel
{
    private readonly ISender _sender;
    private readonly INavigationService _navigation;
    private readonly IEditorLauncher _editor;

    public OverviewViewModel(IWorkspaceSession session, ISender sender, INavigationService navigation, IEditorLauncher editor)
        : base(session)
    {
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(navigation);
        ArgumentNullException.ThrowIfNull(editor);
        _sender = sender;
        _navigation = navigation;
        _editor = editor;
        Teams = [.. WorkstreamRole.All
            .Skip(1)
            .Select(r => Team(r, null))];
        Architect = Team(WorkstreamRole.All[0], null);
        Gates = [.. SignOffNames.Gates
            .Order(StringComparer.Ordinal)
            .Select(g => new GateNode(g, SignOffNames.Of(g), PillState.Blocked, false, SignOffNames.Of(g)))];
    }

    [ObservableProperty]
    public partial string Purpose { get; set; } = "How a workspace stands, from the Delphi code to an approved plan.";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOpen), nameof(IsLoading))]
    public partial bool IsLoaded { get; set; }

    public bool IsOpen => HasWorkspace && IsLoaded && LoadProblem is null;

    public bool IsLoading => HasWorkspace && !IsLoaded;

    /// <summary>Why the workspace could not be read, in the engine's words.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOpen), nameof(HasLoadProblem))]
    public partial string? LoadProblem { get; set; }

    public bool HasLoadProblem => LoadProblem is not null;

    [ObservableProperty]
    public partial NextStep NextStep { get; set; } = new(NextStepTone.Informational, string.Empty, string.Empty, string.Empty, NextStepAction.None, null);

    [ObservableProperty]
    public partial string Summary { get; set; } = string.Empty;

    [ObservableProperty]
    public partial LegacyNode Legacy { get; set; } = new(string.Empty, string.Empty, string.Empty, string.Empty, "Delphi code");

    /// <summary>The six teams in the kit's order; an <c>IList</c> because the diagram binds each one by index.</summary>
    [ObservableProperty]
    public partial IList<TeamNode> Teams { get; set; }

    [ObservableProperty]
    public partial TeamNode Architect { get; set; }

    /// <summary>The five sign-offs in order; an <c>IList</c> because the diagram binds each one by index.</summary>
    [ObservableProperty]
    public partial IList<GateNode> Gates { get; set; }

    [ObservableProperty]
    public partial BuildNode Build { get; set; } = Locked();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasQuestions), nameof(HasNoQuestions), nameof(QuestionCount))]
    public partial IReadOnlyList<QuestionRow> Questions { get; set; } = [];

    public bool HasQuestions => Questions.Count > 0;

    public bool HasNoQuestions => !HasQuestions;

    public string QuestionCount => Questions.Count.ToString(CultureInfo.CurrentCulture);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRuns), nameof(HasNoRuns))]
    public partial IReadOnlyList<RunRow> Runs { get; set; } = [];

    public bool HasRuns => Runs.Count > 0;

    public bool HasNoRuns => !HasRuns;

    /// <summary>Whether Claude Code can run agents, which decides what the empty runs card says.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RunsEmptyTitle), nameof(RunsEmptyText), nameof(RunsEmptyLink))]
    public partial ClaudeStatus? Claude { get; set; }

    public string RunsEmptyTitle => Claude is { IsReady: false } ? "Claude Code isn’t set up" : "No agent runs yet";

    public string RunsEmptyText => Claude is { IsReady: false }
        ? "Everything on this page works without it. With it, agents draft the teams’ deliverables, with evidence, for people to review."
        : "Agents draft deliverables with evidence and a label on every claim. People review the drafts and approve every sign-off.";

    public string RunsEmptyLink => Claude is { IsReady: false } ? "Set up Claude Code" : "Go to Workstreams";

    public void OpenInventory()
    {
        _navigation.NavigateTo("Inventory");
    }

    /// <param name="roleCode">The role Workstreams opens with selected; null opens it as it was.</param>
    public void OpenWorkstreams(string? roleCode)
    {
        _navigation.NavigateTo("Workstreams", roleCode);
    }

    /// <param name="gate">The sign-off Sign-offs opens expanded; null opens the list as it is.</param>
    public void OpenSignOffs(string? gate)
    {
        _navigation.NavigateTo("SignOffs", gate);
    }

    public void OpenTraceability()
    {
        _navigation.NavigateTo("Traceability");
    }

    public Task<bool> OpenQuestionAsync(QuestionRow row)
    {
        ArgumentNullException.ThrowIfNull(row);

        return _editor.OpenAsync(row.Path, row.Line);
    }

    protected override void OnWorkspaceChanged()
    {
        IsLoaded = false;
        OnPropertyChanged(nameof(IsOpen));

        if (HasWorkspace)
        {
            LoadCommand.Execute(parameter: null);
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (!HasWorkspace)
        {
            return;
        }

        var overview = await _sender.Send(new GetOverviewQuery(), cancellationToken);
        LoadProblem = overview.IsFailure ? overview.Error.Message : null;

        if (overview.IsSuccess)
        {
            Show(overview.Value);
        }

        Claude = await _sender.Send(new GetClaudeStatusQuery(), cancellationToken);
        IsLoaded = true;
    }

    private void Show(WorkspaceOverview overview)
    {
        var next = overview.Gates
            .Select((gate, index) => (gate, index))
            .FirstOrDefault(g => g.gate.State != "approved");
        var nextIndex = next.gate is null ? -1 : next.index;

        Purpose = $"How {overview.Name} stands, from the Delphi code to an approved plan.";
        NextStep = NextStepFor(overview, next.gate);
        Summary = SummaryOf(overview);
        Legacy = LegacyOf(overview);
        Teams = [.. overview.Workstreams
            .Where(w => w.Role.Code != WorkstreamRole.All[0].Code)
            .Select(w => Team(w.Role, w))];
        Architect = Team(WorkstreamRole.All[0], overview.Workstreams.FirstOrDefault(w => w.Role.Code == WorkstreamRole.All[0].Code));
        Gates = [.. overview.Gates.Select((gate, index) => GateOf(gate, index, index == nextIndex, overview.Gates.Count))];
        Build = overview.Gates.Count > 0 && overview.Gates[^1].State == "approved" ? Unlocked() : Locked();
        Questions = [.. overview.Questions.Select(q => new QuestionRow(q.Id, q.Text, q.Path, q.Line))];
        Runs = [.. overview.RecentRuns.Select(RunOf)];
    }

    private static NextStep NextStepFor(WorkspaceOverview overview, GateStatus? next)
    {
        if (next is null)
        {
            return new NextStep(
                NextStepTone.Success,
                "Every sign-off is approved and current.",
                "The team may build the new system while these approvals stay current. Changing a signed-off document makes its approval stale.",
                string.Empty,
                NextStepAction.None,
                null);
        }

        var name = SignOffNames.Of(next.Gate);

        if (next.State == "ready")
        {
            var scan = overview.Inventory is null ? " After that, scan the Delphi code to start discovery." : string.Empty;

            return new NextStep(NextStepTone.Informational, $"{name} is ready for sign-off.", $"Every check passes; a person needs to approve it.{scan}", $"Review {name}", NextStepAction.SignOffs, next.Gate);
        }

        if (next.State == "stale")
        {
            return new NextStep(NextStepTone.Caution, $"{name} needs a new sign-off.", "A document it covers changed after it was approved, so the approval no longer holds.", $"Review {name}", NextStepAction.SignOffs, next.Gate);
        }

        if (overview.Trace.Errors > 0)
        {
            return new NextStep(NextStepTone.Critical, $"{name} can’t be signed off yet.", $"{Count(overview.Trace.Errors, "problem")} in the documents {(overview.Trace.Errors == 1 ? "needs" : "need")} fixing first.", "Show the problems", NextStepAction.Traceability, next.Gate);
        }

        var passed = next.Checks.Count(c => c.Passed);

        return new NextStep(NextStepTone.Informational, $"{name} is the next sign-off.", $"{passed} of {next.Checks.Length} checks pass.", $"Review {name}", NextStepAction.SignOffs, next.Gate);
    }

    private static string SummaryOf(WorkspaceOverview overview)
    {
        var deliverables = overview.Workstreams.Sum(w => w.Deliverables.Count);
        var started = overview.Workstreams.Sum(w => w.Started);
        var approved = overview.Gates.Count(g => g.State == "approved");
        var tail = overview switch {
            { Inventory: null } => "Delphi code not scanned yet",
            { Trace.Errors: 0 } => "nothing to fix",
            _ => $"{Count(overview.Trace.Errors, "problem")} to fix",
        };

        return $"{started} of {deliverables} deliverables started · {approved} of {overview.Gates.Count} sign-offs approved · {tail}";
    }

    private static LegacyNode LegacyOf(WorkspaceOverview overview)
    {
        if (overview.Inventory is not { } inventory)
        {
            return new LegacyNode("Not scanned yet", "Not scanned yet", "Scan it from Inventory", string.Empty, "Delphi code, not scanned yet. Opens Inventory.");
        }

        var version = inventory.DelphiVersions.FirstOrDefault() ?? "Delphi version unknown";
        var screens = inventory.Forms - inventory.DataModules;
        var first = Count(inventory.Units, "code module");
        var second = $"{Count(screens, "screen")}, {Count(inventory.DataModules, "data module")}";
        var third = overview.Cycles == 0 ? "No circular dependencies" : Count(overview.Cycles, "circular dependency", "circular dependencies");
        var announced = $"Delphi code, {version}: {Count(inventory.Files, "file")}, {first}, {second}, {third}. Opens Inventory.";

        return new LegacyNode(version, first, second, third, announced);
    }

    private static TeamNode Team(WorkstreamRole role, WorkstreamProgress? progress)
    {
        var total = progress?.Deliverables.Count ?? 0;
        var started = progress?.Started ?? 0;
        var line = $"{started} of {total} started";
        var architect = role.Code == WorkstreamRole.All[0].Code;
        var name = architect ? role.Name : $"{role.Name} team";

        return new TeamNode(
            role.Code,
            role.Name,
            RoleGlyphs.Of(role.Code),
            line,
            $"{name}, {started} of {total} deliverables started. {role.Purpose} Opens Workstreams.",
            $"Opens {role.Name} in Workstreams. {role.Purpose}");
    }

    private static GateNode GateOf(GateStatus gate, int index, bool isNext, int count)
    {
        var name = SignOffNames.Of(gate.Gate);
        var pill = PillStates.OfGate(gate.State);
        var next = isNext ? ", next" : string.Empty;

        return new GateNode(gate.Gate, name, pill, isNext, $"{name} sign-off, {PillLook.Of(pill).Word}{next}, {index + 1} of {count}. Opens Sign-offs.");
    }

    private static BuildNode Locked()
    {
        return new BuildNode(PillState.Locked, "Starts once a person approves the final plan", false, "Build the new system, Locked. Starts once a person approves the final plan.");
    }

    private static BuildNode Unlocked()
    {
        return new BuildNode(PillState.Unlocked, "Allowed while the final plan stays approved", true, "Build the new system, Unlocked. Allowed while the final plan stays approved.");
    }

    private static RunRow RunOf(RunSummary run)
    {
        var role = WorkstreamRole.All.FirstOrDefault(r => r.Agent == run.Agent);
        var cost = run.CostUsd is { } usd ? $" · ${usd.ToString("0.00", CultureInfo.InvariantCulture)}" : string.Empty;
        var pill = run.Outcome switch {
            "success" => PillState.Completed,
            "error_max_budget_usd" => PillState.BudgetReached,
            "stopped" => PillState.Stopped,
            _ => PillState.Failed,
        };

        return new RunRow(role?.Name ?? run.Agent, $"{run.StartedAt.LocalDateTime.ToString("g", CultureInfo.CurrentCulture)}{cost}", pill);
    }

    private static string Count(int value, string singular, string? plural = null)
    {
        var number = value.ToString("N0", CultureInfo.CurrentCulture);

        return value == 1 ? $"{number} {singular}" : $"{number} {plural ?? singular + "s"}";
    }
}
