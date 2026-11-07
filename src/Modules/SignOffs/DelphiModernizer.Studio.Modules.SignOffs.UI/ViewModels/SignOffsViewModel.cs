using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;
using DelphiModernizer.Studio.Application.Abstractions.Workspaces;
using DelphiModernizer.Studio.Modules.SignOffs.Application;
using DelphiModernizer.Studio.UI.Common;
using DelphiModernizer.Studio.UI.Contracts.Services;

namespace DelphiModernizer.Studio.Modules.SignOffs.UI.ViewModels;

/// <summary>The five sign-offs in order, each opening to its checks and where its approval stands.</summary>
public sealed partial class SignOffsViewModel : WorkspacePageViewModel
{
    private readonly ISender _sender;
    private readonly INavigationService _navigation;
    private readonly IClipboardService _clipboard;
    private string? _requested;

    public SignOffsViewModel(IWorkspaceSession session, ISender sender, INavigationService navigation, IClipboardService clipboard)
        : base(session)
    {
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(navigation);
        ArgumentNullException.ThrowIfNull(clipboard);
        _sender = sender;
        _navigation = navigation;
        _clipboard = clipboard;
    }

    [ObservableProperty]
    public partial IReadOnlyList<SignOffRow> SignOffs { get; set; } = [];

    [ObservableProperty]
    public partial bool IsOpen { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLoadProblem))]
    public partial string? LoadProblem { get; set; }

    public bool HasLoadProblem => LoadProblem is not null;

    /// <summary>The first sign-off whose approval went stale, which the banner names.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStale), nameof(StaleTitle))]
    public partial SignOffRow? Stale { get; set; }

    public bool HasStale => Stale is not null;

    public string StaleTitle => Stale is null ? string.Empty : $"{Stale.Name} changed after it was approved.";

    [ObservableProperty]
    public partial string NextName { get; set; } = string.Empty;

    /// <param name="parameter">The gate to open, as Overview passes it; null opens the next one.</param>
    public override void OnNavigatedTo(object? parameter)
    {
        _requested = parameter as string;
    }

    public void Copy(string text)
    {
        _clipboard.SetText(text);
    }

    /// <summary>The dialog for approving or revoking a sign-off, with who git says the person is.</summary>
    public async Task<ApprovalDialogViewModel> PrepareDialogAsync(SignOffRow signOff, bool isRevoke)
    {
        var approver = await _sender.Send(new GetApproverQuery(), CancellationToken.None);

        return new ApprovalDialogViewModel(_sender, signOff, isRevoke, approver);
    }

    public void ShowProblems()
    {
        _navigation.NavigateTo("Traceability");
    }

    public void Prepare()
    {
        _navigation.NavigateTo("Workstreams", "PA");
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

        var expanded = SignOffs
            .Where(s => s.IsExpanded)
            .Select(s => s.Gate)
            .ToHashSet(StringComparer.Ordinal);
        var loaded = await _sender.Send(new GetSignOffsQuery(), cancellationToken);
        LoadProblem = loaded.IsFailure ? loaded.Error.Message : null;

        if (loaded.IsSuccess)
        {
            Show(loaded.Value, expanded);
        }

        IsOpen = loaded.IsSuccess;
    }

    private void Show(IReadOnlyList<SignOff> signOffs, HashSet<string> expanded)
    {
        var next = signOffs.FirstOrDefault(s => s.Status.State != "approved");
        var rows = signOffs
            .Select(s => new SignOffRow(s, signOffs.Count, s == next))
            .ToList();
        var open = _requested ?? (expanded.Count > 0 ? null : next?.Status.Gate);
        _requested = null;

        foreach (var row in rows)
        {
            row.IsExpanded = row.Gate == open || expanded.Contains(row.Gate);
        }

        SignOffs = rows;
        Stale = rows.Find(r => r.IsStale);
        NextName = next?.Name ?? rows[^1].Name;
    }
}
