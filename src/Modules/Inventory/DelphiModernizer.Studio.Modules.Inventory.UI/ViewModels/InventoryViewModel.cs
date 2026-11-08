using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DelphiModernizer.Engine.Delphi;
using DelphiModernizer.Engine.Delphi.Inventory;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;
using DelphiModernizer.Studio.Application.Abstractions.Workspaces;
using DelphiModernizer.Studio.Modules.Inventory.Application;
using DelphiModernizer.Studio.UI.Common;
using DelphiModernizer.Studio.UI.Common.Icons;
using DelphiModernizer.Studio.UI.Contracts.Services;

namespace DelphiModernizer.Studio.Modules.Inventory.UI.ViewModels;

/// <summary>What the scanner found in legacy/, by tab; every path opens in the person's editor.</summary>
public sealed partial class InventoryViewModel : WorkspacePageViewModel
{
    private const string _legacy = "legacy/";

    /// <summary>More code modules than this are grouped by folder, as the design draws a large codebase.</summary>
    private const int _groupAbove = 50;

    private readonly ISender _sender;
    private readonly IEditorLauncher _editor;
    private List<UnitRow> _units = [];
    private InventoryScan? _scan;

    public InventoryViewModel(IWorkspaceSession session, ISender sender, IEditorLauncher editor)
        : base(session)
    {
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(editor);
        _sender = sender;
        _editor = editor;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsScanned), nameof(IsNotScanned), nameof(ScanLabel))]
    [NotifyPropertyChangedFor(
        nameof(IsUnits),
        nameof(IsScreens),
        nameof(IsData),
        nameof(IsRisks),
        nameof(IsCycles),
        nameof(IsDiagnostics))]
    public partial LegacyInventory? Inventory { get; set; }

    [ObservableProperty]
    public partial ScanFacts Facts { get; set; } = new(string.Empty, string.Empty, string.Empty, string.Empty);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotScanned), nameof(CanScan), nameof(IsScanned))]
    [NotifyPropertyChangedFor(
        nameof(IsUnits),
        nameof(IsScreens),
        nameof(IsData),
        nameof(IsRisks),
        nameof(IsCycles),
        nameof(IsDiagnostics))]
    public partial bool IsScanning { get; set; }

    /// <summary>"7 of 13 files" while a scan runs; empty until the scanner has counted the files.</summary>
    [ObservableProperty]
    public partial string ScanCount { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsScanCounted))]
    public partial double ScanTotal { get; set; }

    [ObservableProperty]
    public partial double ScanDone { get; set; }

    public bool IsScanCounted => ScanTotal > 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotScanned))]
    public partial bool IsLoaded { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasProblem))]
    public partial string? Problem { get; set; }

    public bool HasProblem => Problem is not null;

    /// <remarks>A scan in progress hides the previous inventory, which it replaces when it finishes.</remarks>
    public bool IsScanned => Inventory is not null && !IsScanning;

    public bool IsNotScanned => IsLoaded && Inventory is null && !IsScanning;

    public bool CanScan => !IsScanning;

    public string ScanLabel => Inventory is null ? "Scan" : "Rescan";

    /// <remarks>An <c>IList</c> because the page binds each of the four cards by index.</remarks>
    [ObservableProperty]
    public partial IList<InventoryStat> Stats { get; set; } = Blank();

    [ObservableProperty]
    public partial IReadOnlyList<InventoryTabChoice> Tabs { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(
        nameof(IsUnits),
        nameof(IsScreens),
        nameof(IsData),
        nameof(IsRisks),
        nameof(IsCycles),
        nameof(IsDiagnostics))]
    public partial InventoryTab Tab { get; set; }

    public bool IsUnits => IsScanned && Tab == InventoryTab.Units;

    public bool IsScreens => IsScanned && Tab == InventoryTab.Screens;

    public bool IsData => IsScanned && Tab == InventoryTab.Data;

    public bool IsRisks => IsScanned && Tab == InventoryTab.Risks;

    public bool IsCycles => IsScanned && Tab == InventoryTab.Cycles;

    public bool IsDiagnostics => IsScanned && Tab == InventoryTab.Diagnostics;

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    /// <summary>Whether the code modules are many enough to need search and folder headings.</summary>
    [ObservableProperty]
    public partial bool IsLarge { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<UnitRow> Units { get; set; } = [];

    /// <summary>"10,001 code modules in 101 folders. Rows load as you scroll."</summary>
    [ObservableProperty]
    public partial string LargeNote { get; set; } = string.Empty;

    /// <summary>The sortable column headers in table order.</summary>
    /// <remarks>An <c>IList</c> because the header binds each by index.</remarks>
    [ObservableProperty]
    public partial IList<SortHeader> Headers { get; set; } = HeadersFor(UnitSort.FanIn, descending: true);

    [ObservableProperty]
    public partial string UnitsNote { get; set; } = string.Empty;

    [ObservableProperty]
    public partial IReadOnlyList<ScreenRow> Screens { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasUnreadable))]
    public partial string? Unreadable { get; set; }

    [ObservableProperty]
    public partial string UnreadableTitle { get; set; } = string.Empty;

    public bool HasUnreadable => Unreadable is not null;

    [ObservableProperty]
    public partial IReadOnlyList<ControlRow> Controls { get; set; } = [];

    /// <summary>The data cards two to a row: database access, connections, libraries, then external routines.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<DataCardPair> DataCards { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<SqlRow> Sql { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<RiskRow> Risks { get; set; } = [];

    [ObservableProperty]
    public partial string SqlCount { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string RiskCount { get; set; } = string.Empty;

    [ObservableProperty]
    public partial IReadOnlyList<CycleRow> Cycles { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<DiagnosticRow> Diagnostics { get; set; } = [];

    /// <param name="link">A path relative to legacy/, with <c>#L</c> and the line when it has one.</param>
    public Task<bool> OpenAsync(string link)
    {
        ArgumentNullException.ThrowIfNull(link);
        int hash = link.IndexOf("#L", StringComparison.Ordinal);
        string path = hash < 0 ? link : link[..hash];
        int? line = hash >= 0 && int.TryParse(link[(hash + 2)..], CultureInfo.InvariantCulture, out int number)
            ? number
            : null;

        return _editor.OpenAsync(_legacy + path, line);
    }

    /// <summary>Sorts by <paramref name="column"/>, or reverses the order when already sorted by it.</summary>
    /// <remarks>Numbers sort largest first and names A to Z, so the first click shows what matters most.</remarks>
    public void SortBy(UnitSort column)
    {
        var current = Headers.First(h => h.IsSorted);
        bool descending = current.Column == column ? !current.Descending : column != UnitSort.Name;
        Headers = HeadersFor(column, descending);
        Units = FilteredUnits();
    }

    public Task<bool> OpenMarkdownAsync()
    {
        return _editor.OpenAsync(".modernizer/scans/latest/inventory.md", line: null);
    }

    protected override void OnWorkspaceChanged()
    {
        IsLoaded = false;
        Inventory = null;

        if (HasWorkspace)
        {
            LoadCommand.Execute(parameter: null);
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (!HasWorkspace)
        {
            return;
        }

        var loaded = await _sender.Send(new GetInventoryQuery(), cancellationToken);
        bool notScanned = loaded.IsFailure && loaded.Error == InventoryErrors.NotScanned;
        Problem = loaded.IsFailure && !notScanned ? loaded.Error.Message : null;
        Show(loaded.IsSuccess ? loaded.Value : null);
        IsLoaded = true;
    }

    [RelayCommand(CanExecute = nameof(CanScan))]
    private async Task ScanAsync(CancellationToken cancellationToken)
    {
        var previous = _scan;
        ScanTotal = 0;
        ScanDone = 0;
        ScanCount = string.Empty;
        IsScanning = true;
        Stats = Blank();
        ScanCommand.NotifyCanExecuteChanged();

        // Progress<T> reports on the UI thread it is created on.
        var progress = new Progress<ScanProgress>(p => {
            ScanTotal = p.FilesTotal;
            ScanDone = p.FilesDone;
            ScanCount = $"{Number(p.FilesDone)} of {Count(p.FilesTotal, "file")}";
        });

        try
        {
            var scanned = await _sender.Send(new ScanLegacyCommand(progress), cancellationToken);
            Problem = scanned.IsFailure ? scanned.Error.Message : null;
            Show(scanned.IsSuccess ? scanned.Value : previous);
        }
        finally
        {
            IsScanning = false;
            ScanCommand.NotifyCanExecuteChanged();
        }
    }

    partial void OnSearchChanged(string value)
    {
        Units = FilteredUnits();
    }

    private void Show(InventoryScan? scan)
    {
        _scan = scan;
        Inventory = scan?.Inventory;

        if (scan is null)
        {
            Stats = Blank();
            Tabs = [];

            return;
        }

        var (inventory, scannedAt) = scan;

        var summary = inventory.Summary;
        int programs = inventory.Units.Count(u => u.Kind == "program");
        string withPrograms = programs switch {
            0 => string.Empty,
            1 => " · plus the main program",
            _ => $" · plus {Number(programs)} programs",
        };
        var cycleUnits = inventory.Dependencies.Cycles
            .SelectMany(c => c)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        int secrets = inventory.Connections.Count(c => c.HasEmbeddedPassword);
        int cycles = inventory.Dependencies.Cycles.Length;
        var extensions = summary.FilesByExtension
            .OrderByDescending(e => e.Count)
            .Take(2)
            .Select(e => $"{Number(e.Count)} {e.Name}");
        string unreadable = summary.BinaryForms > 0
            ? $" · {Number(summary.BinaryForms)} can’t be read"
            : string.Empty;

        Stats = [
            new(
                "Files",
                StudioIcons.Deliverables,
                Number(summary.Files),
                "files",
                $"{string.Join(" · ", extensions)} · {Count(summary.Projects, "project file")}"),
            new(
                "Code modules",
                StudioIcons.DelphiCode,
                Number(summary.Units),
                "modules",
                $"{Number(summary.CodeLines)} code lines of {Number(summary.Lines)}{withPrograms}"),
            new(
                "Screens",
                StudioIcons.Screens,
                Number(summary.Forms),
                "form files",
                $"{Count(summary.Forms - summary.DataModules, "screen")}, "
                + $"{Count(summary.DataModules, "data module")}{unreadable}"),
            new(
                "Risk markers",
                StudioIcons.Risks,
                Number(inventory.RiskMarkers.Length),
                "markers",
                $"{Count(secrets, "hard-coded credential")} · {Count(cycles, "cycle")}"),
        ];
        var version = inventory.Projects.FirstOrDefault(p => p.DelphiVersion is not null);
        string encodings = string.Join(", ", summary.Encodings.Select(e => $"{e.Name} {Number(e.Count)}"));
        string shortFingerprint = inventory.SourceFingerprint.Length > 19
            ? inventory.SourceFingerprint[..19] + "…"
            : inventory.SourceFingerprint;
        string versionNote = version is null
            ? "Delphi version not named"
            : $"{version.DelphiVersion}, from {System.IO.Path.GetFileName(version.Path)}";
        Facts = new ScanFacts(
            versionNote,
            $"Encodings: {encodings}",
            $"Scanned {Scanned(scannedAt)}",
            shortFingerprint);

        _units = [.. inventory.Units
            .OrderBy(u => u.Name, StringComparer.OrdinalIgnoreCase)
            .Select(u => new UnitRow(
                u.Name,
                u.Path,
                u.CodeLines,
                u.Lines,
                u.Routines,
                u.GlobalVariables,
                u.FanIn,
                u.FanOut,
                u.InterfaceUses.Length + u.ImplementationUses.Length,
                u.Types.Length == 0 ? "—" : string.Join(", ", u.Types.Select(t => t.Name)),
                cycleUnits.Contains(u.Name),
                u.Kind == "program",
                false,
                InventoryWords.CodeModule(u, cycleUnits.Contains(u.Name))))];
        IsLarge = _units.Count > _groupAbove;
        Units = FilteredUnits();
        int folders = _units
            .Select(u => System.IO.Path.GetDirectoryName(u.Path) ?? string.Empty)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        LargeNote = $"{Count(_units.Count, "code module")} in {Count(folders, "folder")}. Rows load as you scroll.";
        string missing = inventory.UnresolvedUnits.Length == 0
            ? "Code modules that are used but missing: none."
            : $"Code modules that are used but missing: {string.Join(", ", inventory.UnresolvedUnits)}.";
        UnitsNote = string.Join(
            " ",
            inventory.Dependencies.Cycles
                .Select(c => InventoryWords.Cycle(c))
                .Append(missing));

        var dataModules = inventory.Units
            .SelectMany(u => u.Types)
            .Where(t => t.Ancestors.Any(a => a.Equals("TDataModule", StringComparison.OrdinalIgnoreCase)))
            .Select(t => t.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        Screens = [.. inventory.Forms.Select(f => new ScreenRow(
            InventoryWords.Screen(f, dataModules),
            f.Path,
            f.IsBinary,
            f.Components.Length,
            f.Events.Length))];
        Unreadable = UnreadableNote(inventory.Forms);
        UnreadableTitle = summary.BinaryForms == 1
            ? "1 screen can’t be read."
            : $"{Number(summary.BinaryForms)} screens can’t be read.";
        Controls = [.. inventory.Components.Select(c => new ControlRow(
            c.ClassName,
            c.Count,
            c.Vendor ?? "Not reported",
            c.Category))];

        var connections = inventory.Connections
            .Select(Connection)
            .ToList();
        var externals = new ExternalsCard(
            [.. inventory.ExternalRoutines.Select(e => new ExternalRow(e.Routine, e.Library, Link(e.Path, e.Line)))],
            inventory.ComObjects.Length == 0
                ? "COM objects: none found."
                : $"COM objects: {string.Join(", ", inventory.ComObjects.Select(c => c.ProgId))}.");
        object[] cards = [
            .. inventory.DatabaseAccess.Select(Technology),
            .. connections,
            .. inventory.ThirdParty.Select(Technology),
            externals,
        ];
        DataCards = [.. cards
            .Chunk(2)
            .Select(p => new DataCardPair(p[0], p.Length > 1 ? p[1] : null))];

        Sql = [.. inventory.Sql.Select(s => new SqlRow(
            Link(s.Path, s.Line),
            s.Kind,
            s.Text,
            Owner(s),
            s.BuiltByConcatenation))];
        Risks = [.. inventory.RiskMarkers
            .OrderBy(r => InventoryWords.RiskRank(r.Kind))
            .ThenByDescending(r => r.Count)
            .Select(r => new RiskRow(InventoryWords.RiskKind(r.Kind), Link(r.Path, r.Line), r.Count, r.Detail))];
        SqlCount = Number(Sql.Count);
        RiskCount = Number(Risks.Count);
        Cycles = [.. inventory.Dependencies.Cycles.Select(c => new CycleRow(string.Join(" ↔ ", c), [.. c]))];
        Diagnostics = [.. inventory.Diagnostics.Select(d => new DiagnosticRow(Link(d.Path, d.Line), d.Message))];

        Tabs = [
            new(InventoryTab.Units, "Code modules", inventory.Units.Length),
            new(InventoryTab.Screens, "Screens", inventory.Forms.Length),
            new(InventoryTab.Data, "Data and libraries", cards.Length),
            new(InventoryTab.Risks, "SQL and risks", Sql.Count + Risks.Count),
            new(InventoryTab.Cycles, "Cycles", cycles),
            new(InventoryTab.Diagnostics, "Diagnostics", Diagnostics.Count),
        ];
    }

    /// <remarks>A large codebase reads by folder: each folder heading is followed by its modules.</remarks>
    private List<UnitRow> FilteredUnits()
    {
        var matching = Sorted(Search.Length == 0
            ? _units
            : _units.FindAll(u => u.Name.Contains(Search, StringComparison.OrdinalIgnoreCase)
                || u.Path.Contains(Search, StringComparison.OrdinalIgnoreCase)));

        if (_units.Count <= _groupAbove)
        {
            return matching;
        }

        return [.. matching
            .GroupBy(u => System.IO.Path.GetDirectoryName(u.Path) ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .SelectMany(g => g.Prepend(Folder(g.Key, g.Count())))];
    }

    /// <remarks>Ties keep the order by name the rows are built in.</remarks>
    private List<UnitRow> Sorted(List<UnitRow> units)
    {
        var sort = Headers.First(h => h.IsSorted);
        Func<UnitRow, IComparable> key = sort.Column switch {
            UnitSort.CodeLines => u => u.CodeLines,
            UnitSort.Routines => u => u.Routines,
            UnitSort.Globals => u => u.Globals,
            UnitSort.FanIn => u => u.FanIn,
            UnitSort.FanOut => u => u.FanOut,
            UnitSort.Uses => u => u.Uses,
            _ => u => u.Name.ToUpperInvariant(),
        };

        return sort.Descending ? [.. units.OrderByDescending(key)] : [.. units.OrderBy(key)];
    }

    private static UnitRow Folder(string folder, int count)
    {
        string name = folder.Length == 0 ? "legacy/" : folder.Replace('\\', '/') + "/";
        string modules = Count(count, "code module");

        return new UnitRow(
            name,
            modules,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            string.Empty,
            false,
            false,
            true,
            $"Folder {name}, {modules}");
    }

    private static List<SortHeader> HeadersFor(UnitSort sorted, bool descending)
    {
        (UnitSort Column, string Label)[] columns = [
            (UnitSort.Name, "Code module and path"),
            (UnitSort.CodeLines, "Code lines"),
            (UnitSort.Routines, "Routines"),
            (UnitSort.Globals, "Globals"),
            (UnitSort.FanIn, "Fan-in"),
            (UnitSort.FanOut, "Fan-out"),
            (UnitSort.Uses, "Uses"),
        ];

        return [.. columns.Select(c => new SortHeader(c.Column, c.Label, c.Column == sorted, descending))];
    }

    private static string? UnreadableNote(IEnumerable<FormInfo> forms)
    {
        var binary = forms
            .Where(f => f.IsBinary)
            .Select(f => f.Path)
            .ToList();

        if (binary.Count == 0)
        {
            return null;
        }

        var (verb, pronoun) = binary.Count == 1 ? ("is", "it") : ("are", "them");

        return $"{string.Join(", ", binary)} {verb} stored in binary. "
            + $"Convert {pronoun} to text with convert.exe, then scan again.";
    }

    private static string Owner(SqlStatement statement)
    {
        string where = statement.Source == "form" ? "form" : "code";

        return statement.Owner is null ? $"In the {where}" : $"{statement.Owner}, in the {where}";
    }

    private static TechnologyCard Technology(TechnologyUse technology)
    {
        return new TechnologyCard(
            technology.Name,
            technology.Category,
            technology.Category == "database" ? StudioIcons.Database : StudioIcons.Components,
            Count(technology.Occurrences, "place"),
            [.. technology.Evidence.Select(e => new Place(Link(e.Path, e.Line), e.Detail))]);
    }

    /// <summary>The scan's time, with its date when it was not today.</summary>
    private static string Scanned(DateTimeOffset scannedAt)
    {
        var local = scannedAt.LocalDateTime;
        string format = local.Date == DateTime.Today ? "t" : "g";

        return local.ToString(format, CultureInfo.CurrentCulture);
    }

    private static ConnectionCard Connection(DatabaseConnection connection)
    {
        var settings = connection.Details
            .Split("; ", StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split('=', 2))
            .SelectMany(kv => kv.Length > 1 ? Settings(kv[0], kv[1]) : [new Setting(kv[0], string.Empty)])
            .ToList();
        string meta = connection.Technology is null
            ? connection.ClassName
            : $"{connection.ClassName} · {connection.Technology}";

        return new ConnectionCard(
            $"Connection {connection.Component}",
            meta,
            Link(connection.Path, connection.Line),
            connection.HasEmbeddedPassword,
            settings);
    }

    /// <summary>A setting, or the settings a list such as Params.Strings holds, one per name.</summary>
    /// <remarks>A list's secret then reads alone, as "PASSWORD, redacted", never inside the list's text.</remarks>
    private static IEnumerable<Setting> Settings(string key, string value)
    {
        string[] parts = value.Split(';', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0 || !parts.All(p => p.Contains('=', StringComparison.Ordinal)))
        {
            return [new Setting(key, value)];
        }

        return parts
            .Select(p => p.Split('=', 2))
            .Select(kv => new Setting(kv[0].Trim(), kv[1].Trim()));
    }

    private static string Link(string path, int line)
    {
        return line > 0 ? $"{path}#L{line}" : path;
    }

    private static List<InventoryStat> Blank()
    {
        return [
            new("Files", StudioIcons.Deliverables, "—", string.Empty, "Not scanned yet"),
            new("Code modules", StudioIcons.DelphiCode, "—", string.Empty, string.Empty),
            new("Screens", StudioIcons.Screens, "—", string.Empty, string.Empty),
            new("Risk markers", StudioIcons.Risks, "—", string.Empty, string.Empty),
        ];
    }

    private static string Count(int value, string singular)
    {
        return value == 1 ? $"1 {singular}" : $"{Number(value)} {singular}s";
    }

    private static string Number(int value)
    {
        return value.ToString("N0", CultureInfo.CurrentCulture);
    }
}
