using System;
using System.IO;

namespace DelphiModernizer.Studio.Application.Abstractions;

/// <summary>
/// Where the studio keeps what belongs to this person on this PC: settings, recent workspaces and logs.
/// </summary>
/// <remarks>Local app data, which the unpackaged app can always write; nothing here belongs in a workspace.</remarks>
public static class StudioPaths
{
    public static string DataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DelphiModernizer", "Studio");
}
