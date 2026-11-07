using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace DelphiModernizer.Studio.ArchitectureTests;

/// <summary>Every project file of the repository, read as XML, so the rules hold without building WinUI.</summary>
internal sealed record ProjectFile(string Name, string Path, IReadOnlyList<string> References, IReadOnlyList<string> Packages, IReadOnlyList<string> EngineReferences)
{
    public bool IsApplication => Name.EndsWith(".Application", StringComparison.Ordinal);

    public bool IsDomain => Name.EndsWith(".Domain", StringComparison.Ordinal);

    public bool IsModuleUi => Name.StartsWith("DelphiModernizer.Studio.Modules.", StringComparison.Ordinal) && Name.EndsWith(".UI", StringComparison.Ordinal);

    public bool IsShared => Path.Contains($"{System.IO.Path.DirectorySeparatorChar}Shared{System.IO.Path.DirectorySeparatorChar}", StringComparison.Ordinal);

    /// <summary>The module a project belongs to, such as <c>Inventory</c>, or null outside src/Modules.</summary>
    public string? Module => Name.StartsWith("DelphiModernizer.Studio.Modules.", StringComparison.Ordinal)
        ? Name.Split('.')[3]
        : null;
}

internal static class ProjectGraph
{
    public static string Root { get; } = FindRoot();

    public static IReadOnlyList<ProjectFile> Source { get; } = Load(System.IO.Path.Combine(Root, "src"));

    public static IReadOnlyList<ProjectFile> Tests { get; } = Load(System.IO.Path.Combine(Root, "tests"));

    private static List<ProjectFile> Load(string directory)
    {
        return [.. Directory
            .EnumerateFiles(directory, "*.csproj", SearchOption.AllDirectories)
            .Select(Read)
            .OrderBy(p => p.Name, StringComparer.Ordinal)];
    }

    private static ProjectFile Read(string path)
    {
        var xml = XDocument.Load(path);

        return new ProjectFile(
            System.IO.Path.GetFileNameWithoutExtension(path),
            path,
            [.. Includes(xml, "ProjectReference").Select(r => System.IO.Path.GetFileNameWithoutExtension(r.Replace('\\', '/')))],
            [.. Includes(xml, "PackageReference")],
            [.. Includes(xml, "EngineReference")]);
    }

    private static IEnumerable<string> Includes(XDocument xml, string item)
    {
        return xml
            .Descendants(item)
            .Select(e => (string?)e.Attribute("Include"))
            .OfType<string>();
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(System.IO.Path.Combine(directory.FullName, "DelphiModernizer.Studio.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("DelphiModernizer.Studio.slnx was not found above the test output.");
    }
}
