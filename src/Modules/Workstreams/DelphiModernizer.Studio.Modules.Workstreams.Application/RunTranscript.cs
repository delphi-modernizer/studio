using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using DelphiModernizer.Engine.Agents;

namespace DelphiModernizer.Studio.Modules.Workstreams.Application;

/// <summary>One line of agent text with the fact label it carries, such as <c>[Confirmed]</c>, taken out of it.</summary>
/// <param name="Label"><c>Confirmed</c>, <c>Inferred</c>, <c>Assumption</c> or <c>Unknown</c>; null for a line without one.</param>
public sealed record AgentLine(string Text, string? Label);

/// <summary>What the Run page reads from the engine's events: fact labels, guard refusals and the files a tool used.</summary>
public static partial class RunTranscript
{
    /// <summary>Claude Code's words for a tool call its PreToolUse hook (the guard) or the workspace's permissions blocked.</summary>
    /// <remarks>The engine streams these as failed tool results; the markers live here until it types them (open questions).</remarks>
    private static readonly string[] _refusalMarkers = ["PreToolUse:", "Permission to use "];

    private static readonly FrozenSet<string> _editingTools = new[] { "Edit", "Write", "MultiEdit", "NotebookEdit" }.ToFrozenSet(StringComparer.Ordinal);

    public static IReadOnlyList<AgentLine> Lines(string text)
    {
        return [.. text
            .Split('\n')
            .Select(LineOf)
            .Where(l => l.Text.Length > 0 || l.Label is not null)];
    }

    private static AgentLine LineOf(string line)
    {
        var label = FactLabel().Match(line);
        var text = FactLabel().Replace(line, string.Empty);

        return new AgentLine(text.Trim(), label.Success ? label.Groups["label"].Value : null);
    }

    public static bool IsRefusal(ToolResult result)
    {
        return result.IsError && _refusalMarkers.Any(m => result.Content.Contains(m, StringComparison.Ordinal));
    }

    /// <summary>The guard's own sentence: what follows the hook's name, or the whole text when it has none.</summary>
    public static string RefusalReason(ToolResult result)
    {
        var content = result.Content.Trim();
        var end = content.LastIndexOf("]: ", StringComparison.Ordinal);

        return end < 0 ? content : content[(end + 3)..].Trim();
    }

    public static bool Edits(ToolCall call)
    {
        return _editingTools.Contains(call.Name);
    }

    /// <summary>The workspace file or folder a tool call names, relative and with forward slashes; null for none or one outside.</summary>
    /// <remarks>The engine shortens long inputs, so the path is read from the JSON text, not a parse of the whole input.</remarks>
    public static string? PathOf(ToolCall call, string workspaceRoot)
    {
        var match = PathProperty().Match(call.Input);

        if (!match.Success)
        {
            return null;
        }

        string? path;

        try
        {
            path = JsonDocument
                .Parse($"\"{match.Groups["value"].Value}\"")
                .RootElement
                .GetString();
        }
        catch (JsonException)
        {
            return null;
        }

        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        var relative = Path.IsPathRooted(path) ? Path.GetRelativePath(workspaceRoot, path) : path;

        return relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative)
            ? null
            : relative.Replace('\\', '/');
    }

    [GeneratedRegex(@"\*{0,2}\[(?<label>Confirmed|Inferred|Assumption|Unknown)\]\*{0,2}")]
    private static partial Regex FactLabel();

    [GeneratedRegex("\"(?:file_path|notebook_path|path)\"\\s*:\\s*\"(?<value>(?:[^\"\\\\]|\\\\.)*)\"")]
    private static partial Regex PathProperty();
}
