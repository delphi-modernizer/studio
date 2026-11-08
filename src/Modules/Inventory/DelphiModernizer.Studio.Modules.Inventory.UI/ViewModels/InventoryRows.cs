using System.Collections.Generic;
using System.Globalization;
using DelphiModernizer.Studio.Modules.Inventory.Application;
using DelphiModernizer.Studio.UI.Common.Icons;

namespace DelphiModernizer.Studio.Modules.Inventory.UI.ViewModels;

public enum InventoryTab
{
    Units,
    Screens,
    Data,
    Risks,
    Cycles,
    Diagnostics,
}

/// <summary>A column the code modules table sorts by.</summary>
public enum UnitSort
{
    Name,
    CodeLines,
    Routines,
    Globals,
    FanIn,
    FanOut,
    Uses,
}

/// <summary>A tab of the inventory with how many findings it holds.</summary>
public sealed record InventoryTabChoice(InventoryTab Tab, string Label, int Count)
{
    public string CountText => Count.ToString("N0", CultureInfo.CurrentCulture);

    public string AnnouncedName => $"{Label}, {CountText}";
}

/// <summary>A column header of the code modules table; the one the table is sorted by shows a chevron.</summary>
public sealed record SortHeader(UnitSort Column, string Label, bool IsSorted, bool Descending)
{
    public string Glyph => Descending ? StudioIcons.ChevronDown : StudioIcons.ChevronUp;

    public string AnnouncedName => IsSorted ? $"{Label}, sorted {_order}" : Label;

    private string _order => Descending ? "descending" : "ascending";
}

/// <summary>One of the four summary cards above the tabs.</summary>
public sealed record InventoryStat(string Label, string Glyph, string Number, string Unit, string Detail);

/// <summary>The line under the summary: the Delphi version, the encodings, when the scan ran and its fingerprint.</summary>
public sealed record ScanFacts(string Version, string Encodings, string Scanned, string Fingerprint);

/// <summary>A row of the code modules table; a folder heading when <see cref="IsFolder"/> is set.</summary>
public sealed record UnitRow(
    string Name,
    string Path,
    int CodeLines,
    int Lines,
    int Routines,
    int Globals,
    int FanIn,
    int FanOut,
    int Uses,
    string Types,
    bool InCycle,
    bool IsProgram,
    bool IsFolder,
    string AnnouncedName)
{
    public bool IsUnit => !IsFolder;

    public string CodeLinesText => $"{Number(CodeLines)} / {Number(Lines)}";

    public string RoutinesText => Number(Routines);

    public string GlobalsText => Number(Globals);

    public string FanInText => Number(FanIn);

    public string FanOutText => Number(FanOut);

    public string UsesText => Number(Uses);

    public string OpenName => $"Open {Path} in your editor";

    private static string Number(int value)
    {
        return value.ToString("N0", CultureInfo.CurrentCulture);
    }
}

/// <summary>A form file; a binary one cannot be read, so its counts show as a dash.</summary>
public sealed record ScreenRow(string Root, string Path, bool IsBinary, int Controls, int Events)
{
    public string Format => IsBinary ? "Binary" : "Text";

    public bool IsText => !IsBinary;

    public string ControlsText => IsBinary ? "—" : Controls.ToString(CultureInfo.CurrentCulture);

    public string EventsText => IsBinary ? "—" : Events.ToString(CultureInfo.CurrentCulture);

    public string AnnouncedName => IsBinary
        ? $"{Root}, {Path}, {Format}"
        : $"{Root}, {Path}, {Format}, {Counted(Controls, "control")}, {Counted(Events, "event")}";

    public string OpenName => $"Open {Path} in your editor";

    private static string Counted(int value, string singular)
    {
        return value == 1 ? $"1 {singular}" : $"{value} {singular}s";
    }
}

public sealed record ControlRow(string ClassName, int Count, string Vendor, string Category);

/// <summary>One place in the code, as a link that opens it.</summary>
/// <param name="Link">A path relative to legacy/; the Data and libraries tab shows it with legacy/ in front.</param>
public sealed record Place(string Link, string Detail)
{
    public string Shown => InventoryWords.InLegacy(Link);

    public string LinkName => InventoryWords.Evidence(Link);
}

/// <summary>A database technology or third-party library and where the code uses it.</summary>
public sealed record TechnologyCard(
    string Name,
    string Category,
    string Glyph,
    string Places,
    IReadOnlyList<Place> Evidence);

/// <summary>A connection a form declares; secret values arrive as *** and stay that way.</summary>
public sealed record ConnectionCard(
    string Name,
    string Meta,
    string Link,
    bool HasEmbeddedPassword,
    IReadOnlyList<Setting> Settings)
{
    public string Shown => InventoryWords.InLegacy(Link);

    public string LinkName => InventoryWords.Evidence(Link);
}

/// <summary>The external routines the code declares and the COM objects it creates.</summary>
public sealed record ExternalsCard(IReadOnlyList<ExternalRow> Rows, string ComNote);

/// <summary>Two data cards side by side; a row is as tall as its taller card.</summary>
/// <param name="Left">A <see cref="TechnologyCard"/>, <see cref="ConnectionCard"/> or <see cref="ExternalsCard"/>.</param>
public sealed record DataCardPair(object Left, object? Right)
{
    public bool HasRight => Right is not null;
}

/// <summary>A setting of a connection; a redacted value reads as "PASSWORD, redacted".</summary>
public sealed record Setting(string Key, string Value)
{
    public string AnnouncedName => Value == "***" ? $"{Key}, redacted" : $"{Key}, {Value}";
}

public sealed record ExternalRow(string Routine, string Library, string Link)
{
    public string Shown => InventoryWords.InLegacy(Link);

    public string LinkName => InventoryWords.Evidence(Link);
}

/// <summary>A SQL statement; one built by concatenation shows that pill where the others show their owner.</summary>
public sealed record SqlRow(string Link, string Kind, string Text, string Note, bool BuiltByConcatenation)
{
    public bool ShowsNote => !BuiltByConcatenation;

    public string AnnouncedName =>
        $"{Kind}: {Text}. {Note}{(BuiltByConcatenation ? ", built by concatenation" : string.Empty)}";

    public string LinkName => InventoryWords.Evidence(Link);
}

public sealed record RiskRow(string Kind, string Link, int Count, string Detail)
{
    public string AnnouncedName => $"{Kind}, {Count}, {Detail}";

    public string LinkName => InventoryWords.Evidence(Link);
}

public sealed record CycleRow(string Units, IReadOnlyList<string> Members);

public sealed record DiagnosticRow(string Link, string Message)
{
    public string LinkName => InventoryWords.Evidence(Link);
}
