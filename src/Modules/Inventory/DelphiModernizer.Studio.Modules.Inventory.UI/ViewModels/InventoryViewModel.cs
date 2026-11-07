using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    [NotifyPropertyChangedFor(nameof(IsNotScanned), nameof(CanScan))]
    public partial bool IsScanning { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotScanned))]
    public partial bool IsLoaded { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasProblem))]
    public partial string? Problem { get; set; }

    public bool HasProblem => Problem is not null;

    public bool IsScanned => Inventory is not null;

    public bool IsNotScanned => IsLoaded && Inventory is null && !IsScanning;

    public bool CanScan => !IsScanning;

    public string ScanLabel => Inventory is null ? "Scan legacy/" : "Scan again";

    [ObservableProperty]
    public partial IReadOnlyList<InventoryStat> Stats { get; set; } = Blank();

    [ObservableProperty]
    public partial string Facts { get; set; } = string.Empty;

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

    public bool HasUnreadable => Unreadable is not null;

    [ObservableProperty]
    public partial IReadOnlyList<ControlRow> Controls { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<TechnologyCard> Technologies { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<ConnectionCard> Connections { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<ExternalRow> Externals { get; set; } = [];

    [ObservableProperty]
    public partial string ComNote { get; set; } = string.Empty;

    [ObservableProperty]
    public partial IReadOnlyList<SqlRow> Sql { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<RiskRow> Risks { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<CycleRow> Cycles { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<DiagnosticRow> Diagnostics { get; set; } = [];

    /// <param name="link">A path relative to legacy/, with <c>#L</c> and the line when it has one.</param>
    public Task<bool> OpenAsync(string link)
    {
        ArgumentNullException.ThrowIfNull(link);
        var hash = link.IndexOf("#L", StringComparison.Ordinal);
        var path = hash < 0 ? link : link[..hash];
        int? line = hash >= 0 && int.TryParse(link[(hash + 2)..], CultureInfo.InvariantCulture, out var number)
            ? number
            : null;

        return _editor.OpenAsync(_legacy + path, line);
    }

    /// <summary>Sorts by <paramref name="column"/>, or reverses the order when already sorted by it.</summary>
    /// <remarks>Numbers sort largest first and names A to Z, so the first click shows what matters most.</remarks>
    public void SortBy(UnitSort column)
    {
        var current = Headers.First(h => h.IsSorted);
        var descending = current.Column == column ? !current.Descending : column != UnitSort.Name;
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
        var notScanned = loaded.IsFailure && loaded.Error == InventoryErrors.NotScanned;
        Problem = loaded.IsFailure && !notScanned ? loaded.Error.Message : null;
        Show(loaded.IsSuccess ? loaded.Value : null);
        IsLoaded = true;
    }

    [RelayCommand(CanExecute = nameof(CanScan))]
    private async Task ScanAsync(CancellationToken cancellationToken)
    {
        IsScanning = true;
        ScanCommand.NotifyCanExecuteChanged();

        try
        {
            var scanned = await _sender.Send(new ScanLegacyCommand(), cancellationToken);
            Problem = scanned.IsFailure ? scanned.Error.Message : null;

            if (scanned.IsSuccess)
            {
                Show(scanned.Value);
            }
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

    private void Show(LegacyInventory? inventory)
    {
        Inventory = inventory;

        if (inventory is null)
        {
            Stats = Blank();
            Tabs = [];

            return;
        }

        var summary = inventory.Summary;
        var cycleUnits = inventory.Dependencies.Cycles
            .SelectMany(c => c)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var secrets = inventory.Connections.Count(c => c.HasEmbeddedPassword);
        var cycles = inventory.Dependencies.Cycles.Length;
        var extensions = summary.FilesByExtension
            .OrderByDescending(e => e.Count)
            .Take(2)
            .Select(e => $"{Number(e.Count)} {e.Name}");
        var unreadable = summary.BinaryForms > 0 ? $" · {Number(summary.BinaryForms)} can’t be read" : string.Empty;

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
                $"{Number(summary.CodeLines)} code lines of {Number(summary.Lines)}"),
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
        var encodings = string.Join(", ", summary.Encodings.Select(e => $"{e.Name} {Number(e.Count)}"));
        var shortFingerprint = inventory.SourceFingerprint.Length > 19
            ? inventory.SourceFingerprint[..19] + "…"
            : inventory.SourceFingerprint;
        var versionNote = version is null
            ? "Delphi version not named"
            : $"{version.DelphiVersion}, from {System.IO.Path.GetFileName(version.Path)}";
        Facts = $"{versionNote}   ·   Encodings: {encodings}   ·   {shortFingerprint}";

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
        UnitsNote = inventory.UnresolvedUnits.Length == 0
            ? "Code modules that are used but missing: none."
            : $"Code modules that are used but missing: {string.Join(", ", inventory.UnresolvedUnits)}.";

        Screens = [.. inventory.Forms.Select(f => new ScreenRow(
            f.RootName ?? System.IO.Path.GetFileNameWithoutExtension(f.Path),
            f.Path,
            f.IsBinary,
            f.Components.Length,
            f.Events.Length))];
        Unreadable = UnreadableNote(inventory.Forms);
        Controls = [.. inventory.Components.Select(c => new ControlRow(
            c.ClassName,
            c.Count,
            c.Vendor ?? "Not reported",
            c.Category))];

        Technologies = [.. inventory.DatabaseAccess
            .Concat(inventory.ThirdParty)
            .Select(t => new TechnologyCard(
                t.Name,
                t.Category,
                Count(t.Occurrences, "place"),
                [.. t.Evidence.Select(e => new Place(Link(e.Path, e.Line), e.Detail))]))];
        Connections = [.. inventory.Connections.Select(Connection)];
        Externals = [.. inventory.ExternalRoutines.Select(e => new ExternalRow(
            e.Routine,
            e.Library,
            Link(e.Path, e.Line)))];
        ComNote = inventory.ComObjects.Length == 0
            ? "COM objects: none found."
            : $"COM objects: {string.Join(", ", inventory.ComObjects.Select(c => c.ProgId))}.";

        Sql = [.. inventory.Sql.Select(s => new SqlRow(
            Link(s.Path, s.Line),
            s.Kind,
            s.Text,
            Owner(s),
            s.BuiltByConcatenation))];
        Risks = [.. inventory.RiskMarkers.Select(r => new RiskRow(r.Kind, Link(r.Path, r.Line), r.Count, r.Detail))];
        Cycles = [.. inventory.Dependencies.Cycles.Select(c => new CycleRow(string.Join(" ↔ ", c), [.. c]))];
        Diagnostics = [.. inventory.Diagnostics.Select(d => new DiagnosticRow(Link(d.Path, d.Line), d.Message))];

        Tabs = [
            new(InventoryTab.Units, "Code modules", inventory.Units.Length),
            new(InventoryTab.Screens, "Screens", inventory.Forms.Length),
            new(InventoryTab.Data, "Data and libraries", Technologies.Count + Connections.Count),
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
        var name = folder.Length == 0 ? "legacy/" : folder.Replace('\\', '/') + "/";
        var modules = Count(count, "code module");

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
        var where = statement.Source == "form" ? "form" : "code";

        return statement.Owner is null ? $"In the {where}" : $"{statement.Owner}, in the {where}";
    }

    private static ConnectionCard Connection(DatabaseConnection connection)
    {
        var settings = connection.Details
            .Split("; ", StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split('=', 2))
            .SelectMany(kv => kv.Length > 1 ? Settings(kv[0], kv[1]) : [new Setting(kv[0], string.Empty)])
            .ToList();
        var meta = connection.Technology is null
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
        var parts = value.Split(';', StringSplitOptions.RemoveEmptyEntries);

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

    private static IReadOnlyList<InventoryStat> Blank()
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
