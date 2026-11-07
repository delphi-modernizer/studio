using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DelphiModernizer.Engine.Agents;
using DelphiModernizer.Engine.Kit;
using DelphiModernizer.Engine.Kit.Spec;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;
using DelphiModernizer.Studio.Application.Abstractions.Settings;
using DelphiModernizer.Studio.Application.Abstractions.Workspaces;
using DelphiModernizer.Studio.Application.Abstractions.Workstreams;
using DelphiModernizer.Studio.Modules.Workstreams.Application;
using DelphiModernizer.Studio.UI.Common;
using DelphiModernizer.Studio.UI.Common.Controls;
using DelphiModernizer.Studio.UI.Contracts.Services;
using Microsoft.UI.Dispatching;

namespace DelphiModernizer.Studio.Modules.Workstreams.UI.ViewModels;

/// <summary>One run: its session, its live transcript, what it writes and touches, and how it ended.</summary>
/// <remarks>
/// Nothing here approves a sign-off or changes a deliverable's status; what a run writes stays Draft.
/// </remarks>
public sealed partial class RunViewModel : WorkspacePageViewModel
{
    private readonly ISender _sender;
    private readonly INavigationService _navigation;
    private readonly IEditorLauncher _editor;
    private readonly IStudioSettings _settings;
    private readonly RunMonitor _monitor;
    private readonly DispatcherQueue _dispatcher;
    private readonly DispatcherQueueTimer _clock;
    private readonly Dictionary<string, (ToolRow Row, string? Path, bool Edits)> _calls = new(StringComparer.Ordinal);
    private LiveRun? _live;
    private RunRecord? _record;
    private string _roleCode = string.Empty;

    public RunViewModel(
        IWorkspaceSession session,
        ISender sender,
        INavigationService navigation,
        IEditorLauncher editor,
        IStudioSettings settings,
        RunMonitor monitor)
        : base(session)
    {
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(navigation);
        ArgumentNullException.ThrowIfNull(editor);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(monitor);
        _sender = sender;
        _navigation = navigation;
        _editor = editor;
        _settings = settings;
        _monitor = monitor;
        _dispatcher = DispatcherQueue.GetForCurrentThread();
        _clock = _dispatcher.CreateTimer();
        _clock.Interval = TimeSpan.FromSeconds(1);
        _clock.Tick += (_, _) => ShowTime();
    }

    /// <summary>Raised on the UI thread when the run ends at its budget while the page shows it.</summary>
    public event EventHandler? BudgetReached;

    public ObservableCollection<TranscriptRow> Items { get; } = [];

    public ObservableCollection<FileRow> Files { get; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<WriteRow> Writes { get; set; } = [];

    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string RoleName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Subtitle { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Pill), nameof(IsLive), nameof(IsEnded), nameof(IsFailed), nameof(TimeLabel))]
    [NotifyPropertyChangedFor(nameof(WritesNote), nameof(EndTitle), nameof(EndText), nameof(BackText))]
    public partial RunOutcome Outcome { get; set; }

    public PillState Pill => RunWords.PillOf(Outcome);

    public bool IsLive => Outcome == RunOutcome.Running;

    public bool IsEnded => !IsLive;

    public bool IsFailed => Outcome == RunOutcome.Failed;

    [ObservableProperty]
    public partial bool IsStarting { get; set; } = true;

    [ObservableProperty]
    public partial string Model { get; set; } = "—";

    [ObservableProperty]
    public partial string CliVersion { get; set; } = "—";

    [ObservableProperty]
    public partial string Cost { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double CostPercent { get; set; }

    [ObservableProperty]
    public partial string Turns { get; set; } = "—";

    [ObservableProperty]
    public partial string Time { get; set; } = "0:00";

    public string TimeLabel => IsLive ? "Elapsed" : "Duration";

    [ObservableProperty]
    public partial string SessionLine { get; set; } = "Waiting for the session…";

    [ObservableProperty]
    public partial string Denials { get; set; } = "0";

    [ObservableProperty]
    public partial int Refusals { get; set; }

    public string GuardCount => $"{Refusals} refused";

    [ObservableProperty]
    public partial string? FailureMessage { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasProblem))]
    public partial string? Problem { get; set; }

    public bool HasProblem => Problem is not null;

    /// <summary>
    /// A run from an earlier session: the store keeps its summary, and its transcript only as the raw file.
    /// </summary>
    [ObservableProperty]
    public partial bool IsRecordOnly { get; set; }

    public string WritesNote => IsLive
        ? "Locked while this run writes it; other tasks that write it wait."
        : "Lands as Draft for a person to review.";

    public string BackText => $"Back to {RoleName}";

    public string EndTitle => Outcome switch {
        RunOutcome.Completed => "The agent finished.",
        RunOutcome.Failed => "The run ended without a result.",
        RunOutcome.Stopped => "You stopped the run.",
        RunOutcome.BudgetReached => $"The run reached its {RunWords.Dollars(BudgetUsd)} budget.",
        _ => string.Empty,
    };

    public string EndText => Outcome switch {
        RunOutcome.Completed =>
            $"It wrote the {Writing} as a Draft. Read it, then move it to In review yourself; the agent can’t.",
        RunOutcome.Failed => "Anything it wrote before failing stays as a Draft. Run it again or write it yourself.",
        RunOutcome.Stopped => "What it wrote before you stopped it stays as a Draft.",
        RunOutcome.BudgetReached => "What it wrote so far stays as a Draft.",
        _ => string.Empty,
    };

    public string ExpandAllText => _allExpanded ? "Collapse all tool calls" : "Expand all tool calls";

    private bool _allExpanded
    {
        get {
            var tools = Items
                .OfType<ToolRow>()
                .ToList();

            return tools.Count > 0 && tools.TrueForAll(r => r.IsExpanded);
        }
    }

    public string TranscriptFile => _record is null ? string.Empty : $".modernizer/runs/{_record.RunId}.jsonl";

    public Recipe? Recipe { get; private set; }

    public string? Argument { get; private set; }

    public decimal BudgetUsd { get; private set; }

    /// <summary>
    /// The budget a new run of this task starts with: this run's, or the Settings default for a recorded run.
    /// </summary>
    public decimal NextBudgetUsd => BudgetUsd > 0 ? BudgetUsd : _settings.Agents.BudgetUsd;

    public string Writing => Recipe is null ? "draft" : RunWords.Writes(Recipe);

    /// <param name="parameter">The run's id, as Workstreams passes it.</param>
    public override void OnNavigatedTo(object? parameter)
    {
        if (parameter is not string runId)
        {
            return;
        }

        if (_monitor.Find(runId) is { } live)
        {
            Follow(live);

            return;
        }

        _ = ShowRecordAsync(runId);
    }

    public void Stop()
    {
        _live?.Stop();
    }

    public void OpenWorkstreams()
    {
        _navigation.NavigateTo("Workstreams");
    }

    public void OpenRole()
    {
        _navigation.NavigateTo("Workstreams", _roleCode);
    }

    public async Task OpenDraftAsync()
    {
        if (Writes.Count > 0)
        {
            await _editor.OpenAsync(Writes[0].Path, line: null);
        }
    }

    public Task OpenFileAsync(string path)
    {
        return _editor.OpenAsync(path, line: null);
    }

    public void Toggle(ToolRow row)
    {
        row.IsExpanded = !row.IsExpanded;
        OnPropertyChanged(nameof(ExpandAllText));
    }

    public void ExpandAll()
    {
        var expand = !_allExpanded;

        foreach (var row in Items.OfType<ToolRow>())
        {
            row.IsExpanded = expand;
        }

        OnPropertyChanged(nameof(ExpandAllText));
    }

    /// <summary>Starts the same task with the same scope as a new run; it never resumes this one.</summary>
    /// <returns>Why it did not start, or null when the new run's page opened.</returns>
    public async Task<string?> RunAgainAsync(string? argument, decimal budgetUsd)
    {
        if (Recipe is null)
        {
            return "The kit has no task for this run.";
        }

        var started = await _sender.Send(new StartRunCommand(Recipe.Id, argument, budgetUsd), CancellationToken.None);

        if (started.IsFailure)
        {
            Problem = started.Error.Message;

            return Problem;
        }

        _navigation.NavigateTo("Run", started.Value.RunId);

        return null;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _clock.Stop();
            _live?.Unsubscribe(OnEvent);
        }

        base.Dispose(disposing);
    }

    private void Follow(LiveRun run)
    {
        _live = run;
        Describe(run.Recipe, run.Argument, run.BudgetUsd);
        Outcome = RunOutcome.Running;
        Cost = $"— of {RunWords.Dollars(run.BudgetUsd)}";
        ShowTime();
        _clock.Start();

        foreach (var item in run.Subscribe(OnEvent))
        {
            Show(item, live: false);
        }
    }

    private async Task ShowRecordAsync(string runId)
    {
        var runs = await _sender.Send(new GetRunsQuery(), CancellationToken.None);

        if (runs.IsFailure || runs.Value.FirstOrDefault(r => r.RunId == runId) is not { } record)
        {
            Problem = runs.IsFailure ? runs.Error.Message : "The run store has no record of this run.";

            return;
        }

        _record = record;
        IsRecordOnly = true;
        IsStarting = false;
        Describe(record.Recipe, record.Argument, budgetUsd: 0);
        Outcome = record.Outcome;
        Cost = record.CostUsd is { } cost ? RunWords.Dollars(cost) : "—";
        Turns = record.Turns?.ToString(CultureInfo.CurrentCulture) ?? "—";
        Time = record.FinishedAt is { } finished ? RunWords.Duration(finished - record.StartedAt) : "—";
        SessionLine = $"Started {RunWords.Time(record.StartedAt)}";
        OnPropertyChanged(nameof(TranscriptFile));
    }

    private void Describe(Recipe? recipe, string? argument, decimal budgetUsd)
    {
        Recipe = recipe;
        Argument = argument;
        BudgetUsd = budgetUsd;
        var role = WorkstreamRole.All.FirstOrDefault(r => r.Agent == recipe?.Agent);
        _roleCode = role?.Code ?? string.Empty;
        RoleName = role?.Name ?? "Workstreams";
        Title = recipe?.Title ?? "Agent run";
        Subtitle = recipe is null
            ? RoleName
            : $"{RoleName} · {RunWords.ArgumentText(recipe, argument)} · "
                + $"writes the {RunWords.Writes(recipe)} as a draft";
        var deliverables = KitBundle.Embedded.Spec.Deliverables.Deliverables;
        Writes = recipe is null
            ? []
            : [.. recipe.Writes
                .Select(id => deliverables.FirstOrDefault(d => d.Id == id))
                .OfType<Deliverable>()
                .Select(d => new WriteRow(d.Title, d.File))];
        OnPropertyChanged(nameof(BackText));
    }

    private void OnEvent(object? sender, AgentEvent item)
    {
        _dispatcher.TryEnqueue(() => Show(item, live: true));
    }

    private void Show(AgentEvent item, bool live)
    {
        switch (item)
        {
            case SessionStarted session:
                IsStarting = false;
                Model = session.Model ?? "—";
                CliVersion = session.CliVersion ?? "—";
                var started = RunWords.Time(_live?.StartedAt ?? DateTimeOffset.Now);
                SessionLine = $"Started {started} · model {Model} · Claude Code {CliVersion}";
                break;
            case AssistantText text:
                var lines = RunTranscript.Lines(text.Text);

                if (lines.Count > 0)
                {
                    Items.Add(new AgentRow(text.FromSubagent ? "Sub-agent" : $"{RoleName} agent", lines));
                }

                break;
            case ToolCall call:
                var row = new ToolRow(call.Id, call.Name, call.Input);
                var path = _session.Current is { } workspace ? RunTranscript.PathOf(call, workspace.Root) : null;
                _calls[call.Id] = (row, path, RunTranscript.Edits(call));
                Items.Add(row);
                break;
            case ToolResult result:
                ShowResult(result);
                break;
            case Diagnostic diagnostic:
                Items.Add(new DiagnosticRow(diagnostic.Source, diagnostic.Text));
                break;
            case RunCompleted completed:
                End(completed, live);
                break;
        }
    }

    private void ShowResult(ToolResult result)
    {
        if (!_calls.TryGetValue(result.ToolCallId, out var call))
        {
            return;
        }

        if (RunTranscript.IsRefusal(result))
        {
            var reason = RunTranscript.RefusalReason(result);
            Items[Items.IndexOf(call.Row)] = new RefusalRow(call.Row.Name, call.Row.Input, call.Path, reason);
            Refusals++;
            OnPropertyChanged(nameof(GuardCount));

            return;
        }

        call.Row.IsError = result.IsError;
        call.Row.Result = result.Content;

        if (!result.IsError && call.Path is { } path)
        {
            var file = Files.FirstOrDefault(f => f.Path == path);

            if (file is null)
            {
                file = new FileRow(path);
                Files.Add(file);
            }

            file.IsEdited |= call.Edits;
        }
    }

    private void End(RunCompleted completed, bool live)
    {
        _clock.Stop();
        IsStarting = false;
        Outcome = RunOutcomes.Of(completed.Outcome);
        Cost = completed.CostUsd is { } cost
            ? $"{RunWords.Dollars(cost)} of {RunWords.Dollars(BudgetUsd)}"
            : $"— of {RunWords.Dollars(BudgetUsd)}";
        CostPercent = completed.CostUsd is { } spent && BudgetUsd > 0
            ? (double)Math.Min(1m, spent / BudgetUsd) * 100
            : 0;
        Turns = completed.Turns?.ToString(CultureInfo.CurrentCulture) ?? "—";
        Denials = completed.PermissionDenials.ToString(CultureInfo.CurrentCulture);
        Time = completed.DurationMs is { } ms ? RunWords.Duration(TimeSpan.FromMilliseconds(ms)) : Time;
        FailureMessage = Outcome == RunOutcome.Failed ? completed.Message : null;

        if (live && Outcome == RunOutcome.BudgetReached)
        {
            BudgetReached?.Invoke(this, EventArgs.Empty);
        }
    }

    private void ShowTime()
    {
        if (_live is not null)
        {
            Time = RunWords.Duration(DateTimeOffset.UtcNow - _live.StartedAt);
        }
    }
}
