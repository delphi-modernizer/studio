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
    /// <summary>How many lines of a folder search's result Files touched reads.</summary>
    private const int _filesOfLines = 200;

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

    /// <summary>A line of agent text without the evidence links it cites, which the Run page shows under it.</summary>
    public static string Claim(string text)
    {
        string claim = _evidence.Value
            .Replace(text, string.Empty)
            .Replace("()", string.Empty, StringComparison.Ordinal);

        return Spaces()
            .Replace(claim, " ")
            .Trim();
    }

    /// <summary>
    /// The input an editing call shows: its file and an ellipsis, as <c>{"file_path":"docs/a.md", …}</c>; others whole.
    /// </summary>
    public static string ShortInput(ToolCall call)
    {
        var path = PathProperty().Match(call.Input);

        return Edits(call) && path.Success && call.Input.Trim() != $"{{{path.Value}}}"
            ? $"{{{path.Value}, …}}"
            : call.Input;
    }

    public static bool Edits(ToolCall call)
    {
        return IsEditing(call.Name);
    }

    public static bool IsEditing(string tool)
    {
        return _editingTools.Contains(tool);
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

        return string.IsNullOrEmpty(path) ? null : Relative(path, workspaceRoot);
    }

    /// <summary>
    /// The workspace files a call that succeeded read or edited: the file it names, or for a folder, such as a
    /// search's, the files its result names.
    /// </summary>
    /// <remarks>
    /// A result line names a file as <c>path</c> or <c>path:line:text</c>; only files on disk count. Only the first
    /// <see cref="_filesOfLines"/> lines are read, so a content search with thousands of matches stays cheap on the UI
    /// thread.
    /// </remarks>
    public static IReadOnlyList<string> FilesOf(ToolCall call, string result, string workspaceRoot)
    {
        if (PathOf(call, workspaceRoot) is not { } path)
        {
            return [];
        }

        if (!Directory.Exists(Path.Combine(workspaceRoot, path)))
        {
            return [path];
        }

        return [.. result
            .Split('\n')
            .Take(_filesOfLines)
            .Select(line => FileOfLine(line.TrimEnd('\r'), workspaceRoot))
            .OfType<string>()
            .Distinct(StringComparer.Ordinal)];
    }

    /// <summary>The workspace file a search line names, whole or before a colon, as Grep prints.</summary>
    private static string? FileOfLine(string line, string workspaceRoot)
    {
        var ends = Enumerable
            .Range(0, line.Length)
            .Where(i => line[i] == ':')
            .Append(line.Length);

        return ends
            .Where(end => end > 0)
            .Select(end => Relative(line[..end], workspaceRoot))
            .FirstOrDefault(relative => relative is not null && File.Exists(Path.Combine(workspaceRoot, relative)));
    }

    private static string? Relative(string path, string workspaceRoot)
    {
        string relative = Path.IsPathRooted(path) ? Path.GetRelativePath(workspaceRoot, path) : path;

        return relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative)
            ? null
            : relative.Replace('\\', '/');
    }

    [GeneratedRegex(@"\s{2,}")]
    private static partial Regex Spaces();

    [GeneratedRegex(@"\*{0,2}\[(?<label>Confirmed|Inferred|Assumption|Unknown)\]\*{0,2}")]
    private static partial Regex FactLabel();

    [GeneratedRegex("\"(?:file_path|notebook_path|path)\"\\s*:\\s*\"(?<value>(?:[^\"\\\\]|\\\\.)*)\"")]
    private static partial Regex PathProperty();
}
