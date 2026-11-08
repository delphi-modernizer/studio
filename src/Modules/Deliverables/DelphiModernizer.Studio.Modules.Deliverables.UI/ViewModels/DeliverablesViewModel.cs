using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DelphiModernizer.Engine.Traceability;
using DelphiModernizer.Studio.Application.Abstractions.Deliverables;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;
using DelphiModernizer.Studio.Application.Abstractions.SignOffs;
using DelphiModernizer.Studio.Application.Abstractions.Workspaces;
using DelphiModernizer.Studio.Application.Abstractions.Workstreams;
using DelphiModernizer.Studio.Modules.Deliverables.Application;
using DelphiModernizer.Studio.UI.Common;
using DelphiModernizer.Studio.UI.Common.Controls;
using DelphiModernizer.Studio.UI.Contracts.Services;

namespace DelphiModernizer.Studio.Modules.Deliverables.UI.ViewModels;

/// <summary>The plan's deliverables grouped by the sign-off that needs them, filtered by status.</summary>
public sealed partial class DeliverablesViewModel : WorkspacePageViewModel
{
    private readonly ISender _sender;
    private readonly IEditorLauncher _editor;
    private readonly INavigationService _navigation;
    private List<SignOffGroup> _all = [];

    public DeliverablesViewModel(
        IWorkspaceSession session,
        ISender sender,
        IEditorLauncher editor,
        INavigationService navigation)
        : base(session)
    {
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(editor);
        ArgumentNullException.ThrowIfNull(navigation);
        _sender = sender;
        _editor = editor;
        _navigation = navigation;
    }

    [ObservableProperty]
    public partial IReadOnlyList<FilterChoice> Filters { get; set; } = [];

    /// <summary>Each sign-off's heading followed by the deliverables it needs that pass the filter.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<DeliverableRow> Rows { get; set; } = [];

    [ObservableProperty]
    public partial DeliverableFilter Filter { get; set; }

    [ObservableProperty]
    public partial bool IsOpen { get; set; }

    [ObservableProperty]
    public partial bool AllTemplates { get; set; }

    /// <summary>The first deliverable whose file is gone, which the critical bar offers to restore.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMissing), nameof(MissingTitle), nameof(MissingMessage))]
    public partial DeliverableRow? Missing { get; set; }

    public bool HasMissing => Missing is not null;

    public string MissingTitle => Missing is null
        ? string.Empty
        : $"The {DeliverableNames.ShortName(Missing.Id, Missing.Title).ToLowerInvariant()} file is missing.";

    public string MissingMessage => Missing is null
        ? string.Empty
        : $"It isn’t at {Missing.Path}. Restore it from git or from the template; "
            + "until then it counts as a problem in the documents.";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasProblem))]
    public partial string? Problem { get; set; }

    public bool HasProblem => Problem is not null;

    public Task<bool> OpenAsync(DeliverableRow row)
    {
        ArgumentNullException.ThrowIfNull(row);

        return _editor.OpenAsync(row.Path, line: null);
    }

    public Task<bool> OpenDocsAsync()
    {
        return _editor.OpenAsync("docs", line: null);
    }

    public void OpenWorkstreams()
    {
        _navigation.NavigateTo("Workstreams");
    }

    protected override void OnWorkspaceChanged()
    {
        IsOpen = false;

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

        var loaded = await _sender.Send(new GetDeliverablesQuery(), cancellationToken);
        Problem = loaded.IsFailure ? loaded.Error.Message : null;

        if (loaded.IsSuccess)
        {
            Show(loaded.Value);
        }

        IsOpen = loaded.IsSuccess;
    }

    [RelayCommand]
    private async Task RestoreAsync(CancellationToken cancellationToken)
    {
        if (Missing is not { } missing)
        {
            return;
        }

        var restored = await _sender.Send(new RestoreDeliverableCommand(missing.Id), cancellationToken);
        Problem = restored.IsFailure ? restored.Error.Message : null;
        await LoadAsync(cancellationToken);
    }

    partial void OnFilterChanged(DeliverableFilter value)
    {
        Rows = Filtered();
    }

    private void Show(DeliverablesOverview overview)
    {
        int count = overview.Gates.Count;
        _all = [.. overview.Gates.Select((gate, index) => new SignOffGroup(
            SignOffNames.Of(gate.Gate),
            $"Sign-off {index + 1} of {count}",
            PillStates.OfGate(gate.State),
            [.. overview.Deliverables
                .Where(d => d.Gate == gate.Gate)
                .Select(d => Row(d, gate.State == "approved"))]))];
        var rows = _all
            .SelectMany(g => g.Rows)
            .ToList();

        Filters = [
            new(DeliverableFilter.All, "All", rows.Count),
            new(DeliverableFilter.Template, "Template", rows.Count(r => r.Pill == PillState.Template)),
            new(DeliverableFilter.Draft, "Draft", rows.Count(r => r.Pill == PillState.Draft)),
            new(DeliverableFilter.InReview, "In review", rows.Count(r => r.Pill == PillState.InReview)),
            new(DeliverableFilter.Approved, "Approved", rows.Count(r => r.Pill == PillState.Approved)),
            new(DeliverableFilter.Missing, "Missing", rows.Count(r => r.Pill == PillState.Missing)),
        ];
        AllTemplates = rows.TrueForAll(r => r.Pill == PillState.Template);
        Missing = rows.Find(r => r.Pill == PillState.Missing);
        Rows = Filtered();
    }

    private List<DeliverableRow> Filtered()
    {
        return [.. _all
            .Select(g => g with { Rows = [.. g.Rows.Where(Matches)] })
            .Where(g => g.Rows.Count > 0)
            .SelectMany(g => g.Rows.Prepend(Heading(g)))];
    }

    private static DeliverableRow Heading(SignOffGroup group)
    {
        string name = $"{group.Name}, {group.Number}, {PillLook.Of(group.Pill).Word}";

        return new DeliverableRow(
            string.Empty,
            group.Name,
            group.Number,
            string.Empty,
            group.Pill,
            name,
            IsGroup: true);
    }

    private bool Matches(DeliverableRow row)
    {
        return Filter switch {
            DeliverableFilter.Approved => row.Pill == PillState.Approved,
            DeliverableFilter.InReview => row.Pill == PillState.InReview,
            DeliverableFilter.Draft => row.Pill == PillState.Draft,
            DeliverableFilter.Template => row.Pill == PillState.Template,
            DeliverableFilter.Missing => row.Pill == PillState.Missing,
            _ => true,
        };
    }

    /// <remarks>
    /// Approved shows only while the sign-off's approval is current; otherwise the file's own status.
    /// </remarks>
    private static DeliverableRow Row(DeliverableState deliverable, bool approved)
    {
        var pill = approved && deliverable.Status != "missing"
            ? PillState.Approved
            : PillStates.OfDeliverable(deliverable.Status);
        string owner = WorkstreamRole.All.FirstOrDefault(r => r.Code == deliverable.Owner)?.Name ?? deliverable.Owner;
        string gate = SignOffNames.Of(deliverable.Gate);

        return new DeliverableRow(
            deliverable.Id,
            deliverable.Title,
            deliverable.Path,
            owner,
            pill,
            $"{deliverable.Title}, {PillLook.Of(pill).Word}, drafted by {owner}, needed for the {gate} sign-off");
    }
}
