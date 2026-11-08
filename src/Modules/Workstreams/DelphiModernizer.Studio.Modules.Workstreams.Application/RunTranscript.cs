using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using DelphiModernizer.Engine.Agents;
using DelphiModernizer.Engine.Kit;

namespace DelphiModernizer.Studio.Modules.Workstreams.Application;

/// <summary>
/// One line of agent text with the fact label it carries, such as <c>[Confirmed]</c>, taken out of it.
/// </summary>
/// <param name="Label">
/// <c>Confirmed</c>, <c>Inferred</c>, <c>Assumption</c> or <c>Unknown</c>; null for a line without one.
/// </param>
public sealed record AgentLine(string Text, string? Label);

/// <summary>
/// What the Run page reads from the engine's events: fact labels, evidence links and the files a tool used.
/// </summary>
public static partial class RunTranscript
{
    private static readonly FrozenSet<string> _editingTools =
        new[] { "Edit", "Write", "MultiEdit", "NotebookEdit" }.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>The kit's evidence pattern, unanchored, so links are found as the trace check reads them.</summary>
    private static readonly Lazy<Regex> _evidence = new(() => new Regex(
        KitBundle.Embedded.Spec.Ids.Evidence.Pattern,
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1)));

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
        string text = FactLabel().Replace(line, string.Empty);

        return new AgentLine(text.Trim(), label.Success ? label.Groups["label"].Value : null);
    }

    /// <summary>The evidence links a line of agent text cites, such as <c>legacy/Sales/Pricing.pas#L18</c>.</summary>
    public static IReadOnlyList<string> Evidence(string text)
    {
        return [.. _evidence.Value
            .Matches(text)
            .Select(m => m.Value)];
    }

    public static bool Edits(ToolCall call)
    {
        return _editingTools.Contains(call.Name);
    }

    /// <summary>
    /// The workspace file or folder a tool call names, relative and with forward slashes; null for none or one outside.
    /// </summary>
    /// <remarks>
    /// The engine shortens long inputs, so the path is read from the JSON text, not a parse of the whole input.
    /// </remarks>
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

        string relative = Path.IsPathRooted(path) ? Path.GetRelativePath(workspaceRoot, path) : path;

        return relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative)
            ? null
            : relative.Replace('\\', '/');
    }

    [GeneratedRegex(@"\*{0,2}\[(?<label>Confirmed|Inferred|Assumption|Unknown)\]\*{0,2}")]
    private static partial Regex FactLabel();

    [GeneratedRegex("\"(?:file_path|notebook_path|path)\"\\s*:\\s*\"(?<value>(?:[^\"\\\\]|\\\\.)*)\"")]
    private static partial Regex PathProperty();
}
