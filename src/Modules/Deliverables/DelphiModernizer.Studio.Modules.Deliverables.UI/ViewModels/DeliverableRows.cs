using System.Collections.Generic;
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
public sealed record DeliverableRow(string Id, string Title, string Path, string Owner, PillState Pill, string AnnouncedName)
{
    public bool CanOpen => Pill != PillState.Missing;

    public string OpenName => $"Open {Title} in your editor";
}

/// <summary>The deliverables one sign-off needs, under its name and state.</summary>
public sealed record SignOffGroup(string Name, string Number, PillState Pill, IReadOnlyList<DeliverableRow> Rows);

/// <summary>A status the table can be filtered to, with how many deliverables are in it.</summary>
public sealed record FilterChoice(DeliverableFilter Filter, string Label, int Count)
{
    public string Text => $"{Label} {Count}";
}
