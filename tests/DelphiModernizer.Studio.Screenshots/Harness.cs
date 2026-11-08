using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using FlaUI.Core.Exceptions;

namespace DelphiModernizer.Studio.Screenshots;

/// <summary>How the app is launched: its recent workspaces, its Claude Code, and its git identity.</summary>
/// <param name="Recent">Templates of <see cref="Workspaces"/>, copied fresh for this launch, most recent first.</param>
/// <param name="Claude">Whether Settings names the stand-in; without it no Claude Code is found.</param>
/// <param name="Scenario">The stand-in's <c>STUDIO_CLAUDE_SCENARIO</c>.</param>
/// <param name="Reuse">Whether to open the last launch's copies, so that its runs stay in their history.</param>
internal sealed record Launch(
    IReadOnlyList<string> Recent,
    bool Claude = true,
    string? Scenario = null,
    bool TooOld = false,
    bool Identity = true,
    bool Reuse = false);

/// <summary>Launches the app for each step list and records every state it could not reach.</summary>
internal sealed class Harness(
    string exe,
    string claude,
    string output,
    StudioData data,
    Workspaces workspaces,
    Manifest manifest)
{
    private readonly List<string> _failures = [];

    public IReadOnlyList<string> Failures => _failures;

    public StudioData Data => data;

    public Workspaces Workspaces => workspaces;

    /// <summary>The fresh copies of this launch's recent workspaces, by template name.</summary>
    public IReadOnlyDictionary<string, string> Opened { get; private set; } = new Dictionary<string, string>();

    public void Run(string theme, Launch launch, Action<StudioDriver> steps)
    {
        var opened = launch.Reuse ? Opened : launch.Recent.ToDictionary(t => t, workspaces.Fresh);
        Opened = opened;
        data.Prepare(launch.Claude ? claude : null, [.. launch.Recent.Select(t => opened[t])]);

        if (!launch.Claude && FindOnPath("claude.exe") is { } found)
        {
            Fail($"{theme}: {found} is on PATH, so Claude Code cannot be missing.");

            return;
        }

        var environment = new Dictionary<string, string?> {
            ["GIT_CONFIG_GLOBAL"] = launch.Identity ? workspaces.IdentityConfig : workspaces.EmptyConfig,
            ["GIT_CONFIG_NOSYSTEM"] = "1",
            ["STUDIO_CLAUDE_SCENARIO"] = launch.Scenario ?? "completed",
            ["STUDIO_CLAUDE_VERSION"] = launch.TooOld ? "too-old" : "current",
        };

        try
        {
            using var app = new StudioDriver(exe, output, theme, manifest, environment);
            steps(app);
        }
        catch (Exception exception) when (exception is InvalidOperationException or Win32Exception)
        {
            Fail($"{theme}: the app did not start: {exception.Message}");
        }
    }

    /// <summary>Runs one step; a step that fails is recorded and the next one runs.</summary>
    public void Step(StudioDriver app, string theme, string name, Action step)
    {
        if (app.HasExited)
        {
            Fail($"{theme} {name}: the app is not running.");

            return;
        }

        try
        {
            step();
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException or COMException
            or TimeoutException or FlaUIException or UnauthorizedAccessException)
        {
            Fail($"{theme} {name}: {exception.Message}");

            if (!app.HasExited)
            {
                app.SaveFailure(name);
            }
        }
    }

    public void Fail(string failure)
    {
        _failures.Add(failure);
        Console.Error.WriteLine($"FAILED {failure}");
    }

    private static string? FindOnPath(string name)
    {
        return (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(directory => Path.Combine(directory, name))
            .FirstOrDefault(File.Exists);
    }
}
