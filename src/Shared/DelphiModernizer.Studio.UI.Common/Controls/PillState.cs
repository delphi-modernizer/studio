using DelphiModernizer.Studio.UI.Common.Icons;

namespace DelphiModernizer.Studio.UI.Common.Controls;

/// <summary>Every state the studio shows as a pill: sign-offs, documents, runs, the build and Claude Code.</summary>
public enum PillState
{
    Approved,
    Ready,
    Stale,
    Blocked,
    Template,
    Draft,
    InReview,
    Missing,
    Running,
    Completed,
    Failed,
    Stopped,
    BudgetReached,
    Locked,
    Unlocked,
    Open,
    Error,
    Warning,
    Found,
    NotFound,
    TooOld,
    Unusable,
}

/// <summary>The colour family of a pill; the word and the glyph carry the meaning, the tone only adds to it.</summary>
public enum PillTone
{
    Neutral,
    Success,
    Caution,
    Critical,
    Attention,
}

/// <param name="Glyph">A Studio Icons glyph; empty for Running, which shows a progress ring instead.</param>
public sealed record PillLook(string Word, string Glyph, PillTone Tone)
{
    /// <summary>The look of each state, from the design's component sheet.</summary>
    /// <remarks>Success with the certificate glyph means a person's current approval and nothing else.</remarks>
    public static PillLook Of(PillState state)
    {
        return state switch {
            PillState.Approved => new("Approved", StudioIcons.Approved, PillTone.Success),
            PillState.Ready => new("Ready", StudioIcons.Ready, PillTone.Attention),
            PillState.Stale => new("Stale", StudioIcons.WarningFilled, PillTone.Caution),
            PillState.Blocked => new("Blocked", StudioIcons.Blocked, PillTone.Neutral),
            PillState.Template => new("Template", StudioIcons.NotYet, PillTone.Neutral),
            PillState.Draft => new("Draft", StudioIcons.Draft, PillTone.Neutral),
            PillState.InReview => new("In review", StudioIcons.InReview, PillTone.Attention),
            PillState.Missing => new("Missing", StudioIcons.ErrorFilled, PillTone.Critical),
            PillState.Running => new("Running", string.Empty, PillTone.Attention),
            PillState.Completed => new("Completed", StudioIcons.Checkmark, PillTone.Neutral),
            PillState.Failed => new("Failed", StudioIcons.ErrorFilled, PillTone.Critical),
            PillState.Stopped => new("Stopped", StudioIcons.Stop, PillTone.Neutral),
            PillState.BudgetReached => new("Budget reached", StudioIcons.WarningFilled, PillTone.Caution),
            PillState.Locked => new("Locked", StudioIcons.Locked, PillTone.Neutral),
            PillState.Unlocked => new("Unlocked", StudioIcons.Unlocked, PillTone.Success),
            PillState.Open => new("Open", StudioIcons.Question, PillTone.Neutral),
            PillState.Error => new("Error", StudioIcons.ErrorFilled, PillTone.Critical),
            PillState.Found => new("Found", StudioIcons.CheckPassed, PillTone.Success),
            PillState.NotFound => new("Not found", StudioIcons.WarningFilled, PillTone.Caution),
            PillState.TooOld => new("Too old", StudioIcons.WarningFilled, PillTone.Caution),
            PillState.Unusable => new("Can’t use this file", StudioIcons.ErrorFilled, PillTone.Critical),
            _ => new("Warning", StudioIcons.WarningFilled, PillTone.Caution),
        };
    }
}

/// <summary>The pill for each state word the engine writes.</summary>
public static class PillStates
{
    /// <param name="state">A gate's state: <c>approved</c>, <c>stale</c>, <c>ready</c> or <c>blocked</c>.</param>
    public static PillState OfGate(string state)
    {
        return state switch {
            "approved" => PillState.Approved,
            "stale" => PillState.Stale,
            "ready" => PillState.Ready,
            _ => PillState.Blocked,
        };
    }

    /// <param name="status">A deliverable's front-matter status, or <c>missing</c> when its file is gone.</param>
    public static PillState OfDeliverable(string status)
    {
        return status switch {
            "draft" => PillState.Draft,
            "in-review" => PillState.InReview,
            "missing" => PillState.Missing,
            _ => PillState.Template,
        };
    }
}
