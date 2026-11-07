using System;

namespace DelphiModernizer.Studio.Application.Abstractions.Agents;

/// <summary>Whether Claude Code can run agent tasks here.</summary>
/// <param name="Path">The executable found or configured, or null.</param>
/// <param name="Version">Its version, or null when it was not found or did not answer.</param>
/// <param name="Problem">The engine's words for why agents cannot run, or null when they can.</param>
/// <param name="Minimum">The oldest version the engine drives.</param>
public sealed record ClaudeStatus(ClaudeState State, string? Path, Version? Version, string? Problem, Version Minimum)
{
    public bool IsReady => State == ClaudeState.Ready;

    /// <summary>What Narrator reads when a test ends: "Claude Code 2.1.300 found", or the engine's words.</summary>
    public string Describe()
    {
        return IsReady ? $"Claude Code {Version} found" : Problem ?? "Claude Code not found";
    }
}

/// <summary>The states the Start and Settings pages draw for Claude Code.</summary>
public enum ClaudeState
{
    Ready,
    NotFound,
    TooOld,

    /// <summary>Found but not usable: a script shim, or a version it would not report.</summary>
    Unusable,
}
