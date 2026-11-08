using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace DelphiModernizer.Studio.Screenshots;

/// <summary>Captures every state of the built app in light and dark, and high contrast where drawn.</summary>
/// <remarks>Exits 1 when the app does not start, a state cannot be reached or the app logs an error.</remarks>
internal static class Program
{
    private const int _light = 1;
    private const int _dark = 2;
    private const int _followWindows = 0;

    private static readonly Dictionary<string, Action<Harness, string, int>> _groups = new() {
        ["start"] = Groups.Start,
        ["overview"] = Groups.Overview,
        ["pages"] = Groups.Pages,
        ["agents"] = Groups.Agents,
    };

    private static int Main(string[] args)
    {
        if (args is not ["--app", var exe, "--dmz", var dmz, "--claude", var claude, "--legacy", var legacy,
            "--out", var output, .. var rest])
        {
            Console.Error.WriteLine(
                "Usage: --app <studio exe> --dmz <dmz exe> --claude <stand-in claude.exe> --legacy <Delphi tree> "
                    + "--out <folder> [--group start|overview|pages|agents]");

            return 2;
        }

        var groups = rest is ["--group", var only] ? [only] : _groups.Keys.ToList();

        if (groups.Exists(g => !_groups.ContainsKey(g)))
        {
            Console.Error.WriteLine($"The groups are {string.Join(", ", _groups.Keys)}.");

            return 2;
        }

        Directory.CreateDirectory(output);
        Console.WriteLine($"Display {Desktop.DisplayMode}.");

        if (!Desktop.EnsureDisplayMode(1920, 1080))
        {
            Console.Error.WriteLine($"The display stays at {Desktop.DisplayMode}, too small for a 1280 x 800 window.");

            return 1;
        }

        var manifest = new Manifest();
        IReadOnlyList<string> failures;

        using (var data = new StudioData())
        using (var workspaces = new Workspaces(dmz, legacy))
        {
            var harness = new Harness(
                Path.GetFullPath(exe), Path.GetFullPath(claude), output, data, workspaces, manifest);

            foreach (var group in groups)
            {
                Console.WriteLine($"Group {group}:");
                _groups[group](harness, "light", _light);
                _groups[group](harness, "dark", _dark);
            }

            if (groups.Contains("overview"))
            {
                RunHighContrast(harness);
            }

            foreach (var error in data.Errors())
            {
                harness.Fail($"The app logged: {error}");
            }

            data.CopyLogs(Path.Combine(output, "logs"));
            failures = harness.Failures;
        }

        manifest.Write(Path.Combine(output, "manifest.json"));
        Console.WriteLine(failures.Count == 0 ? "Every state captured." : $"{failures.Count} failures.");

        return failures.Count == 0 ? 0 : 1;
    }

    private static void RunHighContrast(Harness harness)
    {
        if (!Desktop.SetHighContrast(on: true))
        {
            Console.WriteLine("High contrast cannot be turned on here; its screenshots are skipped.");

            return;
        }

        try
        {
            // Windows repaints every window and broadcasts the theme change before apps see high contrast.
            Thread.Sleep(5000);
            Groups.HighContrast(harness, "highcontrast", _followWindows);
        }
        finally
        {
            Desktop.SetHighContrast(on: false);
        }
    }
}
