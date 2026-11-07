using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DelphiModernizer.Studio.Application.Abstractions.Agents;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;
using DelphiModernizer.Studio.Modules.Workspaces.Application.Commands;
using DelphiModernizer.Studio.Modules.Workspaces.Application.Queries;
using DelphiModernizer.Studio.UI.Contracts;
using DelphiModernizer.Studio.UI.Contracts.Services;

namespace DelphiModernizer.Studio.Modules.Workspaces.UI.ViewModels;

/// <summary>The Start page: ways to begin, the workspaces opened before, and whether agent runs can work here.</summary>
public sealed partial class StartViewModel : ViewModel
{
    private readonly ISender _sender;
    private readonly INavigationService _navigation;
    private readonly IWorkspaceActions _actions;

    public StartViewModel(ISender sender, INavigationService navigation, IWorkspaceActions actions)
    {
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(navigation);
        ArgumentNullException.ThrowIfNull(actions);
        _sender = sender;
        _navigation = navigation;
        _actions = actions;
    }

    public ObservableCollection<RecentWorkspaceRow> Recent { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoRecent))]
    public partial bool HasRecent { get; set; }

    public bool HasNoRecent => !HasRecent;

    [ObservableProperty]
    public partial ClaudeCard Claude { get; set; } = ClaudeCard.Looking;

    /// <summary>Why the last workspace the person picked did not open.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasProblem))]
    public partial string? Problem { get; set; }

    public bool HasProblem => Problem is not null;

    public void ShowClaude(ClaudeStatus status)
    {
        Claude = ClaudeCard.Of(status);
    }

    public void OpenSettings()
    {
        _navigation.NavigateTo("Settings");
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var recent = await _sender.Send(new GetRecentWorkspacesQuery(), cancellationToken);
        Recent.Clear();

        foreach (var workspace in recent)
        {
            Recent.Add(new RecentWorkspaceRow(workspace));
        }

        HasRecent = Recent.Count > 0;
        await CheckClaudeAsync(cancellationToken);
    }

    [RelayCommand]
    private async Task CheckClaudeAsync(CancellationToken cancellationToken)
    {
        ShowClaude(await _sender.Send(new GetClaudeStatusQuery(), cancellationToken));
    }

    [RelayCommand]
    private void NewWorkspace()
    {
        _actions.StartNewWorkspace();
    }

    [RelayCommand]
    private async Task OpenFolderAsync()
    {
        Problem = null;
        Problem = await _actions.OpenFolderAsync();
    }

    /// <remarks>Opening a workspace forgets the pages before it and lands on Overview (navigation model).</remarks>
    [RelayCommand]
    private async Task OpenRecentAsync(RecentWorkspaceRow row)
    {
        Problem = null;
        var opened = await _sender.Send(new OpenWorkspaceCommand(row.Root));

        if (opened.IsFailure)
        {
            Problem = opened.Error.Message;

            return;
        }

        _navigation.NavigateFresh("Overview");
    }
}
