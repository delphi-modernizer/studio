using System;
using CommunityToolkit.Mvvm.ComponentModel;
using DelphiModernizer.Studio.Application.Abstractions.Workspaces;
using DelphiModernizer.Studio.UI.Contracts;
using Microsoft.UI.Dispatching;

namespace DelphiModernizer.Studio.UI.Common;

/// <summary>The base of a page that needs a workspace: it follows the session and says whether one is open.</summary>
/// <remarks>Create it on the UI thread; session changes arrive on any thread and are marshalled back to it.</remarks>
public abstract partial class WorkspacePageViewModel : ViewModel
{
    private readonly DispatcherQueue _dispatcher;

    protected WorkspacePageViewModel(IWorkspaceSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
        _dispatcher = DispatcherQueue.GetForCurrentThread();
        HasWorkspace = session.Current is not null;
        _session.Changed += OnSessionChanged;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoWorkspace))]
    public partial bool HasWorkspace { get; set; }

    public bool HasNoWorkspace => !HasWorkspace;

    protected IWorkspaceSession _session { get; }

    /// <summary>Called on the UI thread after another workspace opened or the open one closed.</summary>
    protected virtual void OnWorkspaceChanged()
    {
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _session.Changed -= OnSessionChanged;
        }

        base.Dispose(disposing);
    }

    private void OnSessionChanged(object? sender, EventArgs e)
    {
        _dispatcher.TryEnqueue(() => {
            HasWorkspace = _session.Current is not null;
            OnWorkspaceChanged();
        });
    }
}
