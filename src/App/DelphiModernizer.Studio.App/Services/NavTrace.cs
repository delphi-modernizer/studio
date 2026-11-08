using System;
using System.Diagnostics;
using System.IO;

namespace DelphiModernizer.Studio.App.Services;

/// <summary>Diagnostic only: appends each navigation and its caller to the screenshots folder on CI.</summary>
internal static class NavTrace
{
    private static readonly string? _file = Environment.GetEnvironmentVariable("GITHUB_WORKSPACE") is { } root
        ? Path.Combine(root, "out", "screenshots", "nav-trace.log")
        : null;

    public static void Write(string what)
    {
        if (_file is null)
        {
            return;
        }

        string stack = new StackTrace(1, false).ToString();
        string line = $"{DateTime.UtcNow:HH:mm:ss.fff} {what}";
        File.AppendAllText(_file, line + Environment.NewLine + stack + Environment.NewLine);
    }
}
