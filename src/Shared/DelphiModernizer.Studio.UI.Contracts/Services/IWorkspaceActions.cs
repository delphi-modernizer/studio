using System.Threading.Tasks;

namespace DelphiModernizer.Studio.UI.Contracts.Services;

/// <summary>The actions every page offers while no workspace is open; the Workspaces module carries them out.</summary>
public interface IWorkspaceActions
{
    void StartNewWorkspace();

    /// <summary>Asks for a folder, opens the workspace in it and lands on Overview.</summary>
    /// <returns>Why it did not open, or null when it opened or the person cancelled.</returns>
    Task<string?> OpenFolderAsync();
}
