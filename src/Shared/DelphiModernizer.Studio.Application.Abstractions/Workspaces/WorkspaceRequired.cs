using DelphiModernizer.Engine.Core;

namespace DelphiModernizer.Studio.Application.Abstractions.Workspaces;

/// <summary>The error every handler returns when it needs a workspace and none is open.</summary>
public static class WorkspaceRequired
{
    public static readonly Error Error = new("Workspace.NotOpen", "Open a workspace first.");
}
