using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DelphiModernizer.Studio.Modules.SignOffs.Application;
using DelphiModernizer.Studio.UI.Common.Controls;
using DelphiModernizer.Studio.UI.Common.Icons;

namespace DelphiModernizer.Studio.Modules.SignOffs.UI.ViewModels;

/// <summary>One check of a sign-off: met, not yet met, or failing on problems in the documents.</summary>
public sealed record CheckRow(string Name, string Detail, bool Passed, bool ShowsProblems)
{
    public CheckTone Tone => (Passed, ShowsProblems) switch {
        (true, _) => CheckTone.Passed,
        (false, true) => CheckTone.Failing,
        _ => CheckTone.NotYet,
    };

    public string Glyph => Tone switch {
        CheckTone.Passed => StudioIcons.CheckPassed,
        CheckTone.Failing => StudioIcons.ErrorFilled,
        _ => StudioIcons.NotYet,
    };

    public string AnnouncedName => Tone switch {
        CheckTone.Passed => $"Passed: {Name}.",
        CheckTone.Failing => $"Failing: {Name}. {Detail}.",
        _ => $"Not met: {Name}. {Detail}.",
    };
}

public enum CheckTone
{
    Passed,
    NotYet,
    Failing,
}

/// <summary>One sign-off as an expander: its header, its checks and where its approval stands.</summary>
public sealed partial class SignOffRow : ObservableObject
{
    public SignOffRow(SignOff signOff, int count, bool isNext)
    {
        Gate = signOff.Status.Gate;
        Number = signOff.Number.ToString(CultureInfo.CurrentCulture);
        Name = signOff.Name;
        Approves = signOff.Approves;
        Pill = PillStates.OfGate(signOff.Status.State);
        IsNext = isNext;
        Checks = [.. signOff.Checks.Select(c => new CheckRow(c.Name, c.Detail, c.Passed, c.ShowsProblems))];
        var passed = Checks.Count(c => c.Passed);
        PassCount = $"{passed} of {Checks.Count} pass";
        var failing = Checks.Count - passed;
        NotMet = failing == 1 ? "1 check isn’t met." : $"{failing} checks aren’t met.";
        Fingerprint = signOff.Status.ContentHash;

        if (signOff.Status.Approval is { } approval)
        {
            var at = approval.At.LocalDateTime.ToString("g", CultureInfo.CurrentCulture);
            var commit = approval.Commit is { Length: >= 7 } sha ? $" · commit {sha[..7]}" : string.Empty;
            var open = approval.By.IndexOf(" <", System.StringComparison.Ordinal);
            ApprovedBy = $"Approved by {(open > 0 ? approval.By[..open] : approval.By)}";
            ApproverEmail = open > 0 ? approval.By[(open + 2)..].TrimEnd('>') : string.Empty;
            ApprovedWhen = $"{at}{commit}";
            Note = approval.Note;
        }

        (Summary, SummaryDetail) = signOff.Status.State switch {
            "approved" => (ApprovedBy, ApprovedWhen),
            "stale" => ("Changed since approval", "A file it covers was edited"),
            "ready" => ($"{passed} of {Checks.Count} checks pass", "Waiting for a person"),
            _ => ($"{passed} of {Checks.Count} checks pass", Checks.FirstOrDefault(c => !c.Passed)?.Name ?? string.Empty),
        };
        AnnouncedName = $"{Name}, sign-off {Number} of {count}, {PillLook.Of(Pill).Word}, {Summary}";
    }

    public string Gate { get; }

    public string Number { get; }

    public string Name { get; }

    public string Approves { get; }

    public PillState Pill { get; }

    public bool IsNext { get; }

    public IReadOnlyList<CheckRow> Checks { get; }

    public string PassCount { get; }

    public string NotMet { get; }

    public string Fingerprint { get; }

    public string ApprovedBy { get; } = string.Empty;

    public string ApproverEmail { get; } = string.Empty;

    public string ApprovedWhen { get; } = string.Empty;

    public string Note { get; } = string.Empty;

    public string Summary { get; }

    public string SummaryDetail { get; }

    public string AnnouncedName { get; }

    public bool IsBlocked => Pill == PillState.Blocked;

    public bool IsReady => Pill == PillState.Ready;

    public bool IsApproved => Pill == PillState.Approved;

    public bool IsStale => Pill == PillState.Stale;

    public bool HasApproval => ApprovedBy.Length > 0;

    [ObservableProperty]
    public partial bool IsExpanded { get; set; }
}
