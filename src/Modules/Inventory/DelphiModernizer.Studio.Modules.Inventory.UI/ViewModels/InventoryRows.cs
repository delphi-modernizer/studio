using System.Collections.Generic;

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

/// <summary>A tab of the inventory with how many findings it holds.</summary>
public sealed record InventoryTabChoice(InventoryTab Tab, string Label, int Count)
{
    public string Text => $"{Label} {Count}";
}

/// <summary>One of the four summary cards above the tabs.</summary>
public sealed record InventoryStat(string Label, string Glyph, string Number, string Unit, string Detail);

/// <summary>A row of the code modules table; a folder heading when <see cref="IsFolder"/> is set.</summary>
public sealed record UnitRow(
    string Name,
    string Path,
    string CodeLines,
    int Routines,
    int Globals,
    int FanIn,
    int FanOut,
    int Uses,
    string Types,
    bool InCycle,
    bool IsProgram,
    bool IsFolder)
{
    public bool IsUnit => !IsFolder;

    public string AnnouncedName => IsFolder ? $"Folder {Name}, {Path}" : $"{Name}, {Path}{_cycleWord}, {CodeLines} code lines, fan-in {FanIn}, fan-out {FanOut}";

    private string _cycleWord => InCycle ? ", cycle" : string.Empty;

    public string OpenName => $"Open {Path} in your editor";
}

public sealed record ScreenRow(string Root, string Path, bool IsBinary, int Controls, int Events)
{
    public string Format => IsBinary ? "Binary" : "Text";

    public string OpenName => $"Open {Path} in your editor";
}

public sealed record ControlRow(string ClassName, int Count, string Vendor, string Category);

/// <summary>One place in the code, as a link that opens it.</summary>
public sealed record Place(string Link, string Detail);

/// <summary>A database technology or third-party library and where the code uses it.</summary>
public sealed record TechnologyCard(string Name, string Category, string Places, IReadOnlyList<Place> Evidence);

/// <summary>A connection a form declares; secret values arrive as *** and stay that way.</summary>
public sealed record ConnectionCard(string Name, string Meta, string Link, bool HasEmbeddedPassword, IReadOnlyList<Setting> Settings);

/// <summary>A setting of a connection; a redacted value reads as "PASSWORD, redacted".</summary>
public sealed record Setting(string Key, string Value)
{
    public string AnnouncedName => Value == "***" ? $"{Key}, redacted" : $"{Key}, {Value}";
}

public sealed record ExternalRow(string Routine, string Library, string Link);

public sealed record SqlRow(string Link, string Kind, string Text, string Note, bool BuiltByConcatenation);

public sealed record RiskRow(string Kind, string Link, int Count, string Detail);

public sealed record CycleRow(string Units, IReadOnlyList<string> Members);

public sealed record DiagnosticRow(string Link, string Message);
