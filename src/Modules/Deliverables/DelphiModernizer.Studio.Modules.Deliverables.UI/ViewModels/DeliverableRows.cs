using System.Collections.Generic;
using System.Globalization;
using DelphiModernizer.Studio.UI.Common.Controls;

namespace DelphiModernizer.Studio.Modules.Deliverables.UI.ViewModels;

/// <summary>What the status bar above the table shows.</summary>
public enum DeliverableFilter
{
    All,
    Approved,
    InReview,
    Draft,
    Template,
    Missing,
}

/// <summary>One deliverable: its title and file, the team that drafts it, and where it stands.</summary>
/// <param name="IsGroup">The heading of a sign-off's deliverables: its name, its number as Path, and its state.</param>
public sealed record DeliverableRow(
    string Id,
    string Title,
    string Path,
    string Owner,
    PillState Pill,
    string AnnouncedName,
    bool IsGroup = false)
{
    public bool IsDeliverable => !IsGroup;

    public bool CanOpen => IsDeliverable && Pill != PillState.Missing;

    public string PathText => Pill == PillState.Missing ? $"Not found: {Path}" : Path;

    public string OpenName => $"Open {Title} in your editor";
}

/// <summary>The deliverables one sign-off needs, under its name and state.</summary>
public sealed record SignOffGroup(string Name, string Number, PillState Pill, IReadOnlyList<DeliverableRow> Rows);

/// <summary>A status the table can be filtered to, with how many deliverables are in it.</summary>
public sealed record FilterChoice(DeliverableFilter Filter, string Label, int Count)
{
    public string CountText => Count.ToString(CultureInfo.CurrentCulture);

    public string AnnouncedName => $"{Label}, {CountText}";
}
