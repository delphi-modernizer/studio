using DelphiModernizer.Studio.Application.Abstractions.Agents;
using DelphiModernizer.Studio.UI.Common.Controls;

namespace DelphiModernizer.Studio.Modules.Workspaces.UI.ViewModels;

/// <summary>What the card's link does.</summary>
public enum ClaudeCardAction
{
    None,
    OpenSettings,
    SetUp,
    TestAgain,
}

/// <summary>The Claude Code card on Start, in the design's words for each state.</summary>
public sealed record ClaudeCard(
    PillState Pill,
    string Text,
    string? Path,
    string Note,
    string Link,
    ClaudeCardAction Action,
    ClaudeStatus? Status)
{
    public static ClaudeCard Looking { get; } = new(
        PillState.Open, "Looking for Claude Code…", null, string.Empty, string.Empty, ClaudeCardAction.None, null);

    public bool HasStatus => Status is not null;

    public bool HasPath => Path is not null;

    public bool HasLink => Action != ClaudeCardAction.None;

    /// <summary>What Narrator reads for the card, such as "Claude Code, Not found. Agent runs need it; …".</summary>
    public string AnnouncedName => Pill == PillState.NotFound
        ? "Claude Code, Not found. Agent runs need it; everything else works without it."
        : $"Claude Code, {PillLook.Of(Pill).Word}. {Text}";

    public static ClaudeCard Of(ClaudeStatus status)
    {
        return status.State switch {
            ClaudeState.Ready => new(
                PillState.Found,
                $"Claude Code {status.Version} is ready for agent runs.",
                status.Path,
                "Runs use your own Claude Code installation and account. The studio stores no keys.",
                "Agent settings",
                ClaudeCardAction.OpenSettings,
                status),
            ClaudeState.NotFound => new(
                PillState.NotFound,
                status.Path is null
                    ? "The Claude Code CLI is not on PATH. Everything works without it except agent runs."
                    : status.Problem ?? string.Empty,
                null,
                "With it, workstream agents draft deliverables with evidence links and fact labels "
                    + "for people to review.",
                "Set up Claude Code",
                ClaudeCardAction.SetUp,
                status),
            ClaudeState.TooOld => new(
                PillState.TooOld,
                status.Problem ?? string.Empty,
                status.Path,
                "Everything else works as it is. Agent runs stay off until it is updated.",
                "Test again",
                ClaudeCardAction.TestAgain,
                status),
            _ => new(
                PillState.Unusable,
                status.Problem ?? string.Empty,
                status.Path,
                "Everything else works as it is. Agent runs stay off until Claude Code can be used.",
                "Agent settings",
                ClaudeCardAction.OpenSettings,
                status),
        };
    }
}
