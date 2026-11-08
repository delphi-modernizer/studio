using System;
using System.Collections.Frozen;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using DelphiModernizer.Engine.Core;
using DelphiModernizer.Studio.Application.Abstractions.Workspaces;
using DelphiModernizer.Studio.UI.Contracts.Services;

namespace DelphiModernizer.Studio.App.Services;

/// <summary>Opens a workspace file with the app Windows associates with it.</summary>
/// <remarks>
/// Security decision: workspace documents name these files, so a path is opened only inside the workspace and only
/// when it is a source or document file; anything else, such as an executable under legacy/, is shown in Explorer
/// instead of being run. Opening at the cited line waits on the editor question (handoff, open question 8).
/// </remarks>
public sealed class EditorLauncher : IEditorLauncher
{
    private static readonly FrozenSet<string> _openable = FrozenSet.Create(
        StringComparer.OrdinalIgnoreCase,
        ".pas", ".dpr", ".dpk", ".inc", ".dfm", ".fmx", ".dproj", ".dof", ".cfg", ".bdsproj", ".groupproj",
        ".md", ".json", ".txt", ".sql", ".xml", ".ini", ".csv");

    private static readonly string _explorer = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe");

    private readonly IWorkspaceSession _session;

    public EditorLauncher(IWorkspaceSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
    }

    public Task<bool> OpenAsync(string relativePath, int? line)
    {
        if (_session.Current is not { } workspace
            || !WorkspacePaths.TryResolveInside(workspace.Root, workspace.Root, relativePath, out string path)
            || !(File.Exists(path) || Directory.Exists(path)))
        {
            return Task.FromResult(false);
        }

        try
        {
            using var process = Process.Start(StartInfoFor(path));

            return Task.FromResult(true);
        }
        catch (Win32Exception)
        {
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// A folder opens in Explorer, a source or document file in its app, and anything else is only shown.
    /// </summary>
    private static ProcessStartInfo StartInfoFor(string path)
    {
        if (Directory.Exists(path))
        {
            return new ProcessStartInfo(_explorer) { ArgumentList = { path } };
        }

        return _openable.Contains(Path.GetExtension(path))
            ? new ProcessStartInfo(path) { UseShellExecute = true }
            : new ProcessStartInfo(_explorer) { ArgumentList = { $"/select,{path}" } };
    }
}
