using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DelphiModernizer.Studio.Screenshots;

/// <summary>The studio's data folder, filled the way the app leaves it: one recent workspace and a theme.</summary>
/// <remarks>
/// The file names and keys are the app's own (LocalSettingsService, RecentWorkspaceStore); the person's folder is moved
/// aside for the run and put back afterwards.
/// </remarks>
internal sealed class StudioData : IDisposable
{
    private readonly string _directory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DelphiModernizer", "Studio");

    private readonly string _backup;

    public StudioData()
    {
        _backup = _directory + ".screenshots-backup";

        if (Directory.Exists(_directory))
        {
            Directory.Move(_directory, _backup);
        }
    }

    public string LogDirectory => Path.Combine(_directory, "logs");

    /// <param name="theme">The app's ElementTheme value: 0 follows Windows, 1 is light, 2 is dark.</param>
    public void Prepare(string workspace, int theme)
    {
        var manifest = JsonNode.Parse(File.ReadAllText(Path.Combine(workspace, "modernizer.json")))
            ?? throw new InvalidDataException("modernizer.json is empty.");
        Directory.CreateDirectory(_directory);
        File.Delete(Path.Combine(_directory, "recent-workspaces.json"));
        File.WriteAllText(Path.Combine(_directory, "settings.json"), JsonSerializer.Serialize(new { Theme = theme }));

        var recent = new JsonArray {
            new JsonObject {
                ["name"] = manifest["name"]?.GetValue<string>(),
                ["root"] = Path.GetFullPath(workspace),
                ["targetStack"] = manifest["targetStack"]?.GetValue<string>(),
                ["openedAt"] = DateTimeOffset.UtcNow,
            },
        };
        File.WriteAllText(Path.Combine(_directory, "recent-workspaces.json"), recent.ToJsonString());
    }

    /// <summary>The log lines at Error or Critical, which the app writes for every exception that escapes.</summary>
    public IReadOnlyList<string> Errors()
    {
        if (!Directory.Exists(LogDirectory))
        {
            return [];
        }

        return [.. Directory
            .EnumerateFiles(LogDirectory, "*.log")
            .SelectMany(File.ReadLines)
            .Where(l => l.Split(' ', 3) is [_, "Error" or "Critical", _])];
    }

    public void CopyLogs(string destination)
    {
        if (!Directory.Exists(LogDirectory))
        {
            return;
        }

        Directory.CreateDirectory(destination);

        foreach (var file in Directory.EnumerateFiles(LogDirectory))
        {
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite: true);
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }

        if (Directory.Exists(_backup))
        {
            Directory.Move(_backup, _directory);
        }
    }
}
