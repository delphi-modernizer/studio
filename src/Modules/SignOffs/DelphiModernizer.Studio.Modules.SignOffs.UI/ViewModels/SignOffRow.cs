using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DelphiModernizer.Engine.Workspaces;
using DelphiModernizer.Studio.Modules.SignOffs.Application;
using DelphiModernizer.Studio.UI.Common.Controls;
using DelphiModernizer.Studio.UI.Common.Icons;

namespace DelphiModernizer.Studio.Modules.SignOffs.UI.ViewModels;

/// <summary>One check of a sign-off: met, not yet met, or failing on problems in the documents.</summary>
public sealed record CheckRow(SignOffCheck Check)
{
    public string Name => Check.Name;

    public string Detail => Check.Detail;

    public bool Passed => Check.Passed;

    public bool ShowsProblems => Check.ShowsProblems;

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

    public bool IsFailing => Tone == CheckTone.Failing;

    public bool IsNotYet => Tone == CheckTone.NotYet;

    public string AnnouncedName => Check.AnnouncedName;
}

public enum CheckTone
{
    Passed,
    NotYet,
    Failing,
}

/// <summary>One file a sign-off covers, marked when it changed since the approval.</summary>
public sealed record CoveredFileRow(string Path, bool Changed);

/// <summary>One approval or revocation in a sign-off's history.</summary>
public sealed record HistoryRow(string Text, string Detail, bool IsApproval)
{
    public bool IsRevocation => !IsApproval;

    public static HistoryRow Of(ApprovalEntry entry)
    {
        var when = entry.At.LocalDateTime.ToString("g", CultureInfo.CurrentCulture);
        var commit = entry.Commit is { Length: >= 7 } sha ? $" · commit {sha[..7]}" : string.Empty;
        var note = entry.Note.Length > 0 ? $" · “{entry.Note}”" : string.Empty;
        var verb = entry.Action == "revoke" ? "Revoked" : "Approved";

        return new HistoryRow($"{verb} by {PersonName(entry.By)}", $"{when}{commit}{note}", entry.Action != "revoke");
    }

    /// <summary>The name part of an approver recorded as <c>Name &lt;email&gt;</c>.</summary>
    public static string PersonName(string by)
    {
        var open = by.IndexOf(" <", StringComparison.Ordinal);

        return open > 0 ? by[..open] : by;
    }
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
        IsLast = signOff.Number == count;
        Checks = [.. signOff.Checks.Select(c => new CheckRow(c))];
        var passed = Checks.Count(c => c.Passed);
        PassCount = $"{passed} of {Checks.Count} pass";
        var failing = Checks.Count - passed;
        NotMet = failing == 1 ? "1 check isn’t met." : $"{failing} checks aren’t met.";
        Fingerprint = signOff.Status.ContentHash;
        var changed = signOff.Changed.ToHashSet(StringComparer.Ordinal);
        Covers = [.. signOff.Status.CoveredFiles.Select(f => new CoveredFileRow(f, changed.Contains(f)))];
        CoversCount = Covers.Count == 1 ? "· 1 file" : $"· {Covers.Count} files";
        History = [.. signOff.History.Select(HistoryRow.Of)];
        ChangedSummary = signOff.Changed.Count switch {
            0 => "A file it covers differs from the one approved.",
            1 => $"{FileName(signOff.Changed[0])} differs from {ApprovedCommit(signOff)}.",
            _ => $"{FileName(signOff.Changed[0])} and {signOff.Changed.Count - 1} more differ from "
                + $"{ApprovedCommit(signOff)}.",
        };

        if (signOff.Status.Approval is { } approval)
        {
            var at = approval.At.LocalDateTime.ToString("g", CultureInfo.CurrentCulture);
            var commit = approval.Commit is { Length: >= 7 } sha ? $" · commit {sha[..7]}" : string.Empty;
            var open = approval.By.IndexOf(" <", StringComparison.Ordinal);
            ApprovedBy = $"Approved by {HistoryRow.PersonName(approval.By)}";
            ApproverEmail = open > 0 ? approval.By[(open + 2)..].TrimEnd('>') : string.Empty;
            ApprovedWhen = $"{at}{commit}";
            Note = approval.Note;
        }

        (Summary, SummaryDetail) = signOff.Status.State switch {
            "approved" => (ApprovedBy, ApprovedWhen),
            "stale" => (
                "Changed since approval",
                signOff.Changed.Count > 0
                    ? string.Join(", ", signOff.Changed.Select(FileName))
                    : "A file it covers was edited"),
            "ready" => ($"{passed} of {Checks.Count} checks pass", "Waiting for a person"),
            _ => (
                $"{passed} of {Checks.Count} checks pass",
                Checks.FirstOrDefault(c => !c.Passed)?.Name ?? string.Empty),
        };
        AnnouncedName = $"{Name}, sign-off {Number} of {count}, {PillLook.Of(Pill).Word}, {Summary}";
    }

    public string Gate { get; }

    public string Number { get; }

    public string Name { get; }

    public string Approves { get; }

    public PillState Pill { get; }

    public bool IsNext { get; }

    public bool IsNotNext => !IsNext;

    public bool IsLast { get; }

    public string ApproveText => $"Approve {Name}…";

    public string ApproveName => $"Approve {Name}";

    public IReadOnlyList<CheckRow> Checks { get; }

    public string PassCount { get; }

    public string NotMet { get; }

    public string Fingerprint { get; }

    public IReadOnlyList<CoveredFileRow> Covers { get; }

    public string CoversCount { get; }

    public IReadOnlyList<HistoryRow> History { get; }

    public bool HasHistory => History.Count > 0;

    /// <summary>
    /// What the stale InfoBar says changed: the files git reports, or the fingerprint alone when it cannot tell.
    /// </summary>
    public string ChangedSummary { get; }

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

    /// <summary>Under 1100 the body is one column and the header drops its summary's second line.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowsSummaryDetail))]
    public partial bool IsNarrow { get; set; }

    public bool ShowsSummaryDetail => !IsNarrow;

    private static string FileName(string path)
    {
        return path[(path.LastIndexOf('/') + 1)..];
    }

    private static string ApprovedCommit(SignOff signOff)
    {
        return signOff.Status.Approval?.Commit is { Length: >= 7 } sha ? $"commit {sha[..7]}" : "the approved version";
    }
}
