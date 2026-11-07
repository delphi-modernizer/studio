using System;

namespace DelphiModernizer.Studio.Application.Abstractions.Agents;

/// <summary>Whether Claude Code can run recipes here.</summary>
/// <param name="Path">The executable found, or null.</param>
/// <param name="Version">Its version, or null when it was not found or did not answer.</param>
/// <param name="Problem">Why agents cannot run, or null when they can.</param>
public sealed record ClaudeStatus(string? Path, Version? Version, string? Problem)
{
    public bool IsReady => Problem is null;
}
