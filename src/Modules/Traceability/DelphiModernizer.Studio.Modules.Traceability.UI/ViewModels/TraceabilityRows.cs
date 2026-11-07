using System.Collections.Generic;
using DelphiModernizer.Studio.UI.Common.Controls;

namespace DelphiModernizer.Studio.Modules.Traceability.UI.ViewModels;

public enum TraceView
{
    Issues,
    Matrix,
}

public enum SeverityFilter
{
    All,
    Errors,
    Warnings,
}

/// <summary>The colour a stat's status glyph takes.</summary>
public enum StatTone
{
    None,
    Success,
    Caution,
    Critical,
}

/// <summary>One of the four summary cards: errors, warnings, sign-offs held up, checks.</summary>
public sealed record StatCard(
    string Label,
    string Glyph,
    string Number,
    string Unit,
    string Detail,
    string StatusGlyph,
    StatTone Tone)
{
    public bool HasStatus => Tone != StatTone.None;
}

/// <summary>One problem in the documents: how serious, what it is, and the file and line it is at.</summary>
public sealed record ProblemRow(
    PillState Pill,
    string Description,
    string Rule,
    string Path,
    int Line,
    string FileLink,
    string Folder,
    string AnnouncedName)
{
    public bool IsError => Pill == PillState.Error;
}

/// <summary>One requirement and what traces to it; a dash where nothing does.</summary>
public sealed record MatrixRow(
    string Id,
    string Path,
    int Line,
    string BusinessRules,
    IReadOnlyList<string> Evidence,
    string Decisions,
    string WorkPackages,
    string Tests)
{
    public bool HasEvidence => Evidence.Count > 0;

    public bool HasNoEvidence => !HasEvidence;

    public bool HasTests => Tests.Length > 0;

    public bool HasNoTests => !HasTests;

    public string AnnouncedName =>
        $"{Id}, business rules {BusinessRules}, {Evidence.Count} evidence links, decisions {Decisions}, "
        + $"work packages {WorkPackages}, tests {(HasTests ? Tests : "none")}";
}

/// <summary>A choice in one of the page's selector bars, with its count.</summary>
public sealed record Choice<T>(T Value, string Label, int Count)
{
    public string Text => $"{Label} {Count}";

    public string AnnouncedName => $"{Label}, {Count}";
}
