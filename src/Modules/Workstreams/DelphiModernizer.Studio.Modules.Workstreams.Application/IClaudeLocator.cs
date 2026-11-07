using DelphiModernizer.Engine.Agents;
using DelphiModernizer.Engine.Core;

namespace DelphiModernizer.Studio.Modules.Workstreams.Application;

/// <summary>Finds the Claude Code CLI a run starts; tests put the engine's fake CLI in its place.</summary>
public interface IClaudeLocator
{
    /// <param name="configuredPath">The path the person set in Settings; null looks on PATH.</param>
    Result<ClaudeExecutable> Locate(string? configuredPath);
}

internal sealed class ClaudeLocator : IClaudeLocator
{
    public Result<ClaudeExecutable> Locate(string? configuredPath)
    {
        return ClaudeExecutable.Locate(configuredPath);
    }
}
