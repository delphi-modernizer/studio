using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DelphiModernizer.Engine.Kit.Spec;
using DelphiModernizer.Engine.Traceability;
using DelphiModernizer.Engine.Workspaces;
using DelphiModernizer.Studio.Application.Abstractions.Agents;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;
using DelphiModernizer.Studio.Application.Abstractions.Settings;
using DelphiModernizer.Studio.Application.Abstractions.Workspaces;
using DelphiModernizer.Studio.Modules.Workstreams.Application;
using DelphiModernizer.Studio.UI.Common;
using DelphiModernizer.Studio.UI.Common.Controls;
using DelphiModernizer.Studio.UI.Contracts.Services;
using Microsoft.UI.Dispatching;

namespace DelphiModernizer.Studio.Modules.Workstreams.UI.ViewModels;

/// <summary>The seven roles, and for the one selected: what it drafts, the tasks its agent runs and its run history.</summary>
public sealed partial class WorkstreamsViewModel : WorkspacePageViewModel
{
    private readonly ISender _sender;
    private readonly INavigationService _navigation;
    private readonly IStudioSettings _settings;
    private readonly RunMonitor _monitor;
    private readonly DispatcherQueue _dispatcher;
    private IReadOnlyList<Workstream> _workstreams = [];
    private IReadOnlyList<DeliverableState> _states = [];
    private IReadOnlyList<GateStatus> _gates = [];
    private IReadOnlyList<RunRecord> _runs = [];
    private string? _requested;

    public WorkstreamsViewModel(IWorkspaceSession session, ISender sender, INavigationService navigation, IStudioSettings settings, RunMonitor monitor)
        : base(session)
    {
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(navigation);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(monitor);
        _sender = sender;
        _navigation = navigation;
        _settings = settings;
        _monitor = monitor;
        _dispatcher = DispatcherQueue.GetForCurrentThread();
        _monitor.Changed += OnRunsChanged;
    }

    [ObservableProperty]
    public partial IReadOnlyList<RoleRow> Roles { get; set; } = [];

    [ObservableProperty]
    public partial RoleRow? SelectedRole { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<DeliverableRow> Deliverables { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<TaskRow> Tasks { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasHistory), nameof(HasNoHistory))]
    public partial IReadOnlyList<HistoryRow> History { get; set; } = [];

    public bool HasHistory => History.Count > 0;

    public bool HasNoHistory => !HasHistory;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoClaude), nameof(ClaudeMessage))]
    public partial ClaudeStatus? Claude { get; set; }

    public bool HasNoClaude => Claude is { IsReady: false };

    public string ClaudeMessage => Claude is null
        ? string.Empty
        : $"You can read every role, its deliverables and its history. Running an agent task needs Claude Code {Claude.Minimum} or later.";

    [ObservableProperty]
    public partial bool IsOpen { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasProblem))]
    public partial string? Problem { get; set; }

    public bool HasProblem => Problem is not null;

    public string RoleKind => SelectedRole?.Code == "PA" ? "Lead role · combines the six teams’ work" : "Team · works side by side with five others";

    public decimal DefaultBudget => _settings.Agents.BudgetUsd;

    /// <summary>The first sign-off without a current approval, which a gate argument starts from; null when all are.</summary>
    public string? NextGate => _gates.FirstOrDefault(g => g.State != "approved")?.Gate;

    /// <param name="parameter">The role code to select, such as <c>WS01</c>, as Overview and Sign-offs pass it.</param>
    public override void OnNavigatedTo(object? parameter)
    {
        _requested = parameter as string;
    }

    /// <returns>Why the run did not start, or null when it started and the Run page opened.</returns>
    public async Task<string?> StartAsync(Recipe recipe, string? argument, decimal budgetUsd)
    {
        var started = await _sender.Send(new StartRunCommand(recipe.Id, argument, budgetUsd), CancellationToken.None);

        if (started.IsFailure)
        {
            return started.Error.Message;
        }

        _navigation.NavigateTo("Run", started.Value.RunId);

        return null;
    }

    public void OpenRun(string runId)
    {
        _navigation.NavigateTo("Run", runId);
    }

    public void OpenSettings()
    {
        _navigation.NavigateTo("Settings");
    }

    protected override void OnWorkspaceChanged()
    {
        IsOpen = false;

        if (HasWorkspace)
        {
            LoadCommand.Execute(parameter: null);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _monitor.Changed -= OnRunsChanged;
        }

        base.Dispose(disposing);
    }

    partial void OnSelectedRoleChanged(RoleRow? value)
    {
        OnPropertyChanged(nameof(RoleKind));
        ShowRole();
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (!HasWorkspace)
        {
            return;
        }

        _workstreams = await _sender.Send(new GetWorkstreamsQuery(), cancellationToken);
        Claude = await _sender.Send(new GetClaudeStatusQuery(), cancellationToken);
        var states = await _sender.Send(new GetDeliverableStatesQuery(), cancellationToken);
        var runs = await _sender.Send(new GetRunsQuery(), cancellationToken);
        Problem = states.IsFailure ? states.Error.Message : null;
        _states = states.IsSuccess ? states.Value.Deliverables : [];
        _gates = states.IsSuccess ? states.Value.Gates : [];
        _runs = runs.IsSuccess ? runs.Value : [];
        IsOpen = true;
        ShowRoles(_requested ?? SelectedRole?.Code ?? "PA");
        _requested = null;
    }

    private void ShowRoles(string selected)
    {
        Roles = [.. _workstreams.Select(w => new RoleRow(
            w,
            _states.Count(s => s.Owner == w.Role.Code && s.Status is not ("template" or "missing")),
            _runs.Any(r => r.Agent == w.Role.Agent && r.Outcome == RunOutcome.Running)))];
        SelectedRole = Roles.FirstOrDefault(r => r.Code == selected) ?? Roles.FirstOrDefault();
        ShowRole();
    }

    private void ShowRole()
    {
        if (SelectedRole is not { } role || _session.Current is not { } workspace)
        {
            return;
        }

        var ready = Claude is { IsReady: true };
        Deliverables = [.. role.Workstream.Deliverables.Select(d => new DeliverableRow(d.Title, PillOf(d.Id, d.Gate)))];
        Tasks = [.. role.Workstream.Recipes.Select(recipe => {
            var blocker = _monitor.Writing(workspace.Root, recipe.Writes);

            return new TaskRow(recipe, role.Name, ready, blocker, blocker is null ? string.Empty : BlockedReason(blocker, recipe, role));
        })];
        History = [.. _runs
            .Where(r => r.Agent == role.Workstream.Role.Agent)
            .Select(r => new HistoryRow(r))];
    }

    /// <summary>Approved while the deliverable's sign-off has a current approval, as Deliverables shows it; else its file's status.</summary>
    private PillState PillOf(string deliverable, string gate)
    {
        var status = _states.FirstOrDefault(s => s.Id == deliverable)?.Status ?? "missing";
        var approved = _gates.Any(g => g.Gate == gate && g.State == "approved");

        return approved && status != "missing" ? PillState.Approved : PillStates.OfDeliverable(status);
    }

    private string BlockedReason(LiveRun blocker, Recipe recipe, RoleRow role)
    {
        var agent = blocker.Recipe.Agent;
        var owner = agent == role.Workstream.Role.Agent
            ? null
            : _workstreams.FirstOrDefault(w => w.Role.Agent == agent)?.Role.Name ?? agent;

        var started = RunWords.Time(blocker.StartedAt);

        return RunNames.BlockedReason(RunWords.Writes(recipe), blocker.Recipe.Title, owner, started);
    }

    private void OnRunsChanged(object? sender, EventArgs e)
    {
        _dispatcher.TryEnqueue(() => {
            if (IsOpen)
            {
                LoadCommand.Execute(parameter: null);
            }
        });
    }
}
