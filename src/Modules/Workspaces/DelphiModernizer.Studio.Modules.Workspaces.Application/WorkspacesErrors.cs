using DelphiModernizer.Engine.Core;

namespace DelphiModernizer.Studio.Modules.Workspaces.Application;

public static class WorkspacesErrors
{
    public static Error FolderMissing(string folder)
    {
        return new Error("Workspaces.FolderMissing", $"The folder {folder} does not exist.");
    }

    public static Error FolderNotEmpty(string folder)
    {
        return new Error("Workspaces.FolderNotEmpty", $"{folder} isn’t empty. Pick a new folder or an empty one.");
    }
}
