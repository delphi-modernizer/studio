using System;
using DelphiModernizer.Engine.Workspaces;

namespace DelphiModernizer.Studio.Application.Abstractions.Workspaces;

/// <summary>The in-memory <see cref="IWorkspaceSession"/>; one per process.</summary>
public sealed class WorkspaceSession : IWorkspaceSession
{
    public Workspace? Current { get; private set; }

    public event EventHandler? Changed;

    public void Open(Workspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        Current = workspace;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Close()
    {
        Current = null;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
