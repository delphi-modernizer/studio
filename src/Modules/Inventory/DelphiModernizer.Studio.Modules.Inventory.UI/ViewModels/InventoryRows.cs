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
    public string Text => $"{Label} {Count}";

    public string AnnouncedName => $"{Label}, {Count}";
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

    public string CodeLinesText =>
        $"{CodeLines.ToString("N0", CultureInfo.CurrentCulture)} / {Lines.ToString("N0", CultureInfo.CurrentCulture)}";

    public string OpenName => $"Open {Path} in your editor";
}

public sealed record ScreenRow(string Root, string Path, bool IsBinary, int Controls, int Events)
{
    public string Format => IsBinary ? "Binary" : "Text";

    public string AnnouncedName =>
        $"{Root}, {Path}, {Format}, {Counted(Controls, "control")}, {Counted(Events, "event")}";

    public string OpenName => $"Open {Path} in your editor";

    private static string Counted(int value, string singular)
    {
        return value == 1 ? $"1 {singular}" : $"{value} {singular}s";
    }
}

public sealed record ControlRow(string ClassName, int Count, string Vendor, string Category);

/// <summary>One place in the code, as a link that opens it.</summary>
public sealed record Place(string Link, string Detail)
{
    public string LinkName => InventoryWords.Evidence(Link);
}

/// <summary>A database technology or third-party library and where the code uses it.</summary>
public sealed record TechnologyCard(string Name, string Category, string Places, IReadOnlyList<Place> Evidence);

/// <summary>A connection a form declares; secret values arrive as *** and stay that way.</summary>
public sealed record ConnectionCard(
    string Name,
    string Meta,
    string Link,
    bool HasEmbeddedPassword,
    IReadOnlyList<Setting> Settings)
{
    public string LinkName => InventoryWords.Evidence(Link);
}

/// <summary>A setting of a connection; a redacted value reads as "PASSWORD, redacted".</summary>
public sealed record Setting(string Key, string Value)
{
    public string AnnouncedName => Value == "***" ? $"{Key}, redacted" : $"{Key}, {Value}";
}

public sealed record ExternalRow(string Routine, string Library, string Link)
{
    public string LinkName => InventoryWords.Evidence(Link);
}

public sealed record SqlRow(string Link, string Kind, string Text, string Note, bool BuiltByConcatenation)
{
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
