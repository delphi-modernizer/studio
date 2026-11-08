using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace DelphiModernizer.Studio.Screenshots;

/// <summary>Throwaway workspaces created by <c>dmz</c> as a person would, and a fresh copy for each launch.</summary>
/// <remarks>Nothing is committed: the large legacy tree is generated, and everything lives in a temp folder.</remarks>
internal sealed class Workspaces : IDisposable
{
    public const string Name = "Acme ERP modernization";
    public const string Identity = "Studio screenshots <screenshots@delphi-modernizer.invalid>";

    /// <summary>Enough units that a scan or a copy lasts long enough to capture it running.</summary>
    private const int _largeAreas = 100;
    private const int _largeUnitsPerArea = 100;

    private readonly string _root = Directory.CreateTempSubdirectory("dmz-screenshots-").FullName;
    private readonly string _dmz;
    private int _copies;

    public Workspaces(string dmz, string legacy)
    {
        _dmz = Path.GetFullPath(dmz);
        Legacy = Path.GetFullPath(legacy);
        IdentityConfig = Path.Combine(_root, "identity.gitconfig");
        EmptyConfig = Path.Combine(_root, "empty.gitconfig");
        File.WriteAllText(
            IdentityConfig,
            "[user]\n\tname = Studio screenshots\n\temail = screenshots@delphi-modernizer.invalid\n");
        File.WriteAllText(EmptyConfig, string.Empty);
        LargeLegacy = GenerateLargeTree(Path.Combine(_root, "legacy-large"));
        Shim = Path.Combine(_root, "shim", "claude.cmd");
        Directory.CreateDirectory(Path.GetDirectoryName(Shim)!);
        File.WriteAllText(Shim, "@echo off\r\nrem A script shim, which the engine refuses.\r\n");
        Missing = Path.Combine(_root, "does-not-exist");
        NotEmpty = Path.Combine(_root, "not-empty");
        Directory.CreateDirectory(NotEmpty);
        File.WriteAllText(Path.Combine(NotEmpty, "notes.txt"), "A folder that is not empty.");
        NoDelphi = Path.Combine(_root, "no-delphi");
        Directory.CreateDirectory(NoDelphi);
        File.WriteAllText(Path.Combine(NoDelphi, "README.txt"), "No Delphi source here.");

        Init("sample", Legacy, "--domain", "ERP, a Delphi 7 application on the BDE");
        Scan("sample");
        AddTwoProblems(Path.Combine(_root, "templates", "sample"));
        Init(
            "clean",
            Legacy,
            "--domain", "ERP, a Delphi 7 application on the BDE",
            "--objectives", "Move to C#/.NET with the same business rules",
            "--users", "40 sales and accounting staff",
            "--geography", "One office in Lyon, France",
            "--criticality", "Invoicing must not stop for more than a day",
            "--constraints", "One year, a team of four");
        Scan("clean");
        Init("unscanned", Legacy, "--domain", "ERP, a Delphi 7 application on the BDE");
        Init("large", LargeLegacy, "--domain", "ERP, a Delphi 7 application on the BDE", "--no-git");
    }

    /// <summary>The brief's sample, the engine's Delphi 7 ERP on the BDE.</summary>
    public string Legacy { get; }

    /// <summary>A legacy tree of thousands of units.</summary>
    public string LargeLegacy { get; }

    /// <summary>A git config with the screenshots identity, given to the app as GIT_CONFIG_GLOBAL.</summary>
    public string IdentityConfig { get; }

    /// <summary>An empty git config, so the app finds no git identity.</summary>
    public string EmptyConfig { get; }

    public string Shim { get; }

    public string Missing { get; }

    public string NotEmpty { get; }

    public string NoDelphi { get; }

    /// <summary>A folder for the wizard to create a workspace in; it does not exist yet.</summary>
    public string NewFolder()
    {
        return Path.Combine(_root, $"new-{++_copies}");
    }

    /// <summary>A fresh copy of a workspace: <c>sample</c>, <c>clean</c>, <c>unscanned</c> or <c>large</c>.</summary>
    public string Fresh(string template)
    {
        string copy = Path.Combine(_root, "copies", $"{++_copies}-{template}");
        Copy(Path.Combine(_root, "templates", template), copy);

        return copy;
    }

    public void Dispose()
    {
        try
        {
            // git writes its objects read-only, and Windows does not delete a read-only file.
            foreach (string file in Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }

            Directory.Delete(_root, recursive: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Console.WriteLine($"The temp folder {_root} stays: {exception.Message}");
        }
    }

    /// <summary>The design's two problems: a reference to an undefined ID, and evidence that is not there.</summary>
    private static void AddTwoProblems(string workspace)
    {
        string inventory = Path.Combine(workspace, "docs", "01-discovery", "D02-system-inventory.md");
        File.AppendAllText(
            inventory,
            "\nThe discount follows FR-999.\n\nThe search screen: legacy/Sales/CustomerSearch.pas#L12\n");
    }

    private static string GenerateLargeTree(string folder)
    {
        var program = new StringBuilder("program BigErp;\n\nuses\n");
        var units = new List<string>();

        for (int area = 1; area <= _largeAreas; area++)
        {
            string directory = Path.Combine(folder, $"Area{area:D3}");
            Directory.CreateDirectory(directory);

            for (int index = 1; index <= _largeUnitsPerArea; index++)
            {
                string unit = $"A{area:D3}Unit{index:D3}";
                string uses = units.Count == 0 ? string.Empty : $"uses\n  {units[^1]};\n\n";
                File.WriteAllText(
                    Path.Combine(directory, unit + ".pas"),
                    $"unit {unit};\n\ninterface\n\n{uses}function Value{index}: Integer;\n\nimplementation\n\n"
                        + $"function Value{index}: Integer;\nbegin\n  Result := {index};\nend;\n\nend.\n");
                units.Add(unit);
                program.Append(CultureInfo.InvariantCulture, $"  {unit} in 'Area{area:D3}\\{unit}.pas',\n");
            }
        }

        string projectUses = program
            .ToString()
            .TrimEnd(',', '\n');
        File.WriteAllText(Path.Combine(folder, "BigErp.dpr"), projectUses + ";\n\nbegin\nend.\n");

        return folder;
    }

    private static void Copy(string from, string to)
    {
        foreach (string directory in Directory.EnumerateDirectories(from, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(directory.Replace(from, to, StringComparison.Ordinal));
        }

        Directory.CreateDirectory(to);

        foreach (string file in Directory.EnumerateFiles(from, "*", SearchOption.AllDirectories))
        {
            File.Copy(file, file.Replace(from, to, StringComparison.Ordinal));
        }
    }

    private void Init(string template, string legacy, params string[] context)
    {
        Dmz([
            "init", Path.Combine(_root, "templates", template),
            "--name", Name,
            "--legacy", legacy,
            "--stack", "dotnet",
            "--by", Identity,
            .. context,
        ]);
    }

    private void Scan(string template)
    {
        Dmz(["scan", "--workspace", Path.Combine(_root, "templates", template)]);
    }

    private void Dmz(IEnumerable<string> arguments)
    {
        var start = new ProcessStartInfo(_dmz) { UseShellExecute = false, RedirectStandardError = true };
        start.Environment["GIT_CONFIG_GLOBAL"] = IdentityConfig;
        start.Environment["GIT_CONFIG_NOSYSTEM"] = "1";

        foreach (string argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        using var process = Process.Start(start) ?? throw new InvalidOperationException("dmz did not start.");
        string errors = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"dmz {string.Join(' ', start.ArgumentList.Take(2))} exited with {process.ExitCode}: {errors.Trim()}");
        }
    }
}
