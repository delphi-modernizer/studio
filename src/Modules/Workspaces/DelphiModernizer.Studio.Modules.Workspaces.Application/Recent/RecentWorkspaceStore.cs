using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace DelphiModernizer.Studio.Modules.Workspaces.Application.Recent;

/// <summary>The workspaces opened most recently, newest first, kept in one JSON file per user.</summary>
/// <remarks>A missing or unreadable file reads as an empty list: losing the list loses only convenience.</remarks>
public sealed class RecentWorkspaceStore
{
    private const int _capacity = 10;

    private readonly string _path;
    private readonly Lock _gate = new();

    public RecentWorkspaceStore(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        _path = path;
    }

    public IReadOnlyList<RecentWorkspace> Load()
    {
        lock (_gate)
        {
            return Read();
        }
    }

    /// <summary>Puts <paramref name="workspace"/> first, dropping an older entry for the same folder and the oldest beyond ten.</summary>
    public void Remember(RecentWorkspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        lock (_gate)
        {
            var list = Read()
                .Where(w => !string.Equals(w.Root, workspace.Root, StringComparison.OrdinalIgnoreCase))
                .Prepend(workspace)
                .Take(_capacity)
                .ToList();

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
                File.WriteAllBytes(_path, JsonSerializer.SerializeToUtf8Bytes(list, RecentJsonContext.Default.ListRecentWorkspace));
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // The list is a convenience; a read-only profile must not stop a workspace from opening.
            }
        }
    }

    private List<RecentWorkspace> Read()
    {
        try
        {
            return File.Exists(_path)
                ? JsonSerializer.Deserialize(File.ReadAllBytes(_path), RecentJsonContext.Default.ListRecentWorkspace) ?? []
                : [];
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return [];
        }
    }
}
