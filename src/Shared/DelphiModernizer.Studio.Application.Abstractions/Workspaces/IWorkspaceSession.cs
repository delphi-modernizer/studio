using System;
using DelphiModernizer.Engine.Workspaces;

namespace DelphiModernizer.Studio.Application.Abstractions.Workspaces;

/// <summary>The workspace the studio has open; every module reads it, only the Workspaces module changes it.</summary>
public interface IWorkspaceSession
{
    Workspace? Current { get; }

    /// <summary>Raised after another workspace opens or the open one closes.</summary>
    event EventHandler? Changed;

    void Open(Workspace workspace);

    void Close();
}
