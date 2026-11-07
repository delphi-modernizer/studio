using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace DelphiModernizer.Studio.Screenshots;

/// <summary>Captures every page of the built app on one workspace, in light, dark and high contrast.</summary>
/// <remarks>Exits 1 when the app does not start, a page cannot be reached or the app logs an error.</remarks>
internal static class Program
{
    private const int _light = 1;
    private const int _dark = 2;
    private const int _followWindows = 0;

    private static readonly Size[] _sizes = [new(1280, 800), new(960, 576)];

    private static int Main(string[] args)
    {
        if (args is not ["--app", var exe, "--workspace", var workspace, "--out", var output])
        {
            Console.Error.WriteLine("Usage: --app <DelphiModernizer.Studio.exe> --workspace <folder> --out <folder>");

            return 2;
        }

        exe = Path.GetFullPath(exe);
        Directory.CreateDirectory(output);
        Console.WriteLine($"Display {Desktop.DisplayMode}.");

        if (!Desktop.EnsureDisplayMode(1920, 1080))
        {
            Console.Error.WriteLine($"The display stays at {Desktop.DisplayMode}, too small for a 1280 x 800 window.");

            return 1;
        }

        Console.WriteLine($"Display {Desktop.DisplayMode}.");
        var failures = new List<string>();

        using (var data = new StudioData())
        {
            Run(exe, workspace, output, data, "light", _light, failures, AllPages);
            Run(exe, workspace, output, data, "dark", _dark, failures, AllPages);
            RunHighContrast(exe, workspace, output, data, failures);

            foreach (var error in data.Errors())
            {
                failures.Add($"The app logged: {error}");
            }

            data.CopyLogs(Path.Combine(output, "logs"));
        }

        foreach (var failure in failures)
        {
            Console.Error.WriteLine($"FAILED {failure}");
        }

        return failures.Count == 0 ? 0 : 1;
    }

    private static void AllPages(StudioDriver app, Action<string, Action> step)
    {
        step("start", () => app.Capture("start", "default"));
        step("overview", () => {
            app.ClickFirstItem("Recent workspaces");
            app.Capture("overview", "default");
        });
        step("inventory", () => {
            app.Navigate("Inventory");
            app.ForEachSection("Inventory sections", s => app.Capture("inventory", StudioDriver.Slug(s)));
        });
        step("workstreams", () => {
            app.Navigate("Workstreams");
            app.Capture("workstreams", "default");
        });
        step("deliverables", () => {
            app.Navigate("Deliverables");
            app.Capture("deliverables", "default");
        });
        step("traceability", () => {
            app.Navigate("Traceability");
            app.ForEachSection("Traceability views", s => app.Capture("traceability", StudioDriver.Slug(s)));
        });
        step("signoffs", () => {
            app.Navigate("SignOffs");
            app.EnsureOneExpanded();
            app.Capture("signoffs", "expanded");
        });
        step("settings", () => {
            app.Navigate("Settings");
            app.Capture("settings", "default");
        });
        step("newworkspace", () => {
            app.Navigate("Start");
            app.Invoke("New workspace");
            app.Capture("newworkspace", "step1");
        });
    }

    private static void HighContrastPages(StudioDriver app, Action<string, Action> step)
    {
        step("overview", () => {
            app.ClickFirstItem("Recent workspaces");
            app.Capture("overview", "default");
        });
        step("signoffs", () => {
            app.Navigate("SignOffs");
            app.EnsureOneExpanded();
            app.Capture("signoffs", "expanded");
        });
    }

    private static void RunHighContrast(
        string exe,
        string workspace,
        string output,
        StudioData data,
        List<string> failures)
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
            Run(exe, workspace, output, data, "highcontrast", _followWindows, failures, HighContrastPages);
        }
        finally
        {
            Desktop.SetHighContrast(on: false);
        }
    }

    private static void Run(
        string exe,
        string workspace,
        string output,
        StudioData data,
        string theme,
        int themeValue,
        List<string> failures,
        Action<StudioDriver, Action<string, Action>> pages)
    {
        Console.WriteLine($"Theme {theme}:");
        data.Prepare(workspace, themeValue);

        try
        {
            using var app = new StudioDriver(exe, output, theme, _sizes);
            pages(app, (page, capture) => {
                if (app.HasExited)
                {
                    failures.Add($"{theme} {page}: the app is not running.");

                    return;
                }

                try
                {
                    capture();
                }
                catch (Exception exception) when (exception is InvalidOperationException or IOException
                    or COMException or TimeoutException)
                {
                    failures.Add($"{theme} {page}: {exception.Message}");
                }
            });
        }
        catch (Exception exception) when (exception is InvalidOperationException or Win32Exception)
        {
            failures.Add($"{theme}: the app did not start: {exception.Message}");
        }
    }
}
