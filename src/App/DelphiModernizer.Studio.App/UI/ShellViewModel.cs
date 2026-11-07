using System;
using CommunityToolkit.Mvvm.ComponentModel;
using DelphiModernizer.Studio.Application.Abstractions.Workspaces;
using DelphiModernizer.Studio.UI.Contracts;
using DelphiModernizer.Studio.UI.Contracts.Services;
using Microsoft.UI.Dispatching;

namespace DelphiModernizer.Studio.App.UI;

/// <summary>
/// What the title bar shows: the open workspace's name as its subtitle, and whether Back has somewhere to go.
/// </summary>
public sealed partial class ShellViewModel : ViewModel
{
    private readonly IWorkspaceSession _session;
    private readonly INavigationService _navigation;
    private readonly DispatcherQueue _dispatcher;

    public ShellViewModel(IWorkspaceSession session, INavigationService navigation)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(navigation);
        _session = session;
        _navigation = navigation;
        _dispatcher = DispatcherQueue.GetForCurrentThread();
        Subtitle = string.Empty;
        _session.Changed += OnSessionChanged;
        _navigation.Navigated += OnNavigated;
    }

    /// <summary>The open workspace's name; empty without one.</summary>
    [ObservableProperty]
    public partial string Subtitle { get; set; }

    [ObservableProperty]
    public partial bool CanGoBack { get; set; }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _session.Changed -= OnSessionChanged;
            _navigation.Navigated -= OnNavigated;
        }

        base.Dispose(disposing);
    }

    private void OnSessionChanged(object? sender, EventArgs e)
    {
        _dispatcher.TryEnqueue(() => Subtitle = _session.Current?.Manifest.Name ?? string.Empty);
    }

    private void OnNavigated(object? sender, NavigationEvent e)
    {
        CanGoBack = _navigation.CanGoBack;
    }
}
