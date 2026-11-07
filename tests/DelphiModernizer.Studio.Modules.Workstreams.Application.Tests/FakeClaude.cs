using System;
using System.IO;
using DelphiModernizer.Engine.Agents;
using DelphiModernizer.Engine.Core;

namespace DelphiModernizer.Studio.Modules.Workstreams.Application.Tests;

/// <summary>The engine's fake CLI, replaying a script of stream-json lines, in place of Claude Code.</summary>
/// <remarks>It prints its whole script for <c>--version</c> too, so each script starts with the version line.</remarks>
internal sealed class FakeClaude : IClaudeLocator
{
    private readonly string _script;
    private readonly string _arguments;

    public FakeClaude(string folder, params string[] lines)
    {
        _script = Path.Combine(folder, "script.ndjson");
        _arguments = Path.Combine(folder, "arguments.txt");
        File.WriteAllLines(_script, ["2.1.300 (Claude Code)", .. lines]);
    }

    public string[] Arguments => File.ReadAllLines(_arguments);

    public Result<ClaudeExecutable> Locate(string? configuredPath)
    {
        var fake = Path.Combine(AppContext.BaseDirectory, "DelphiModernizer.Engine.Agents.FakeClaude.dll");

        return new ClaudeExecutable("dotnet", [fake, _script, _arguments]);
    }
}
