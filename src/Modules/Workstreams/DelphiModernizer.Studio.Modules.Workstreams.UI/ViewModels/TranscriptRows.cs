using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DelphiModernizer.Studio.Modules.Workstreams.Application;
using DelphiModernizer.Studio.UI.Common.Controls;
using DelphiModernizer.Studio.UI.Common.Icons;

namespace DelphiModernizer.Studio.Modules.Workstreams.UI.ViewModels;

/// <summary>One item of a run's transcript, built from the engine's streamed events.</summary>
public abstract class TranscriptRow : ObservableObject
{
    public abstract string AnnouncedName { get; }
}

public sealed class SessionRow(string text) : TranscriptRow
{
    public string Text { get; } = text;

    public override string AnnouncedName => $"Session: {Text}";
}

/// <summary>A line of agent text and the fact label after it, when it has one.</summary>
public sealed record AgentLineRow(string Text, FactKind? Label)
{
    public bool HasLabel => Label is not null;

    public FactKind Kind => Label ?? FactKind.Unknown;
}

public sealed class AgentRow : TranscriptRow
{
    public AgentRow(string who, IReadOnlyList<AgentLine> lines)
    {
        Who = who;
        Lines = [.. lines.Select(l => new AgentLineRow(l.Text, Enum.TryParse<FactKind>(l.Label, out var kind) ? kind : null))];
    }

    public string Who { get; }

    public IReadOnlyList<AgentLineRow> Lines { get; }

    public override string AnnouncedName => $"Agent: {string.Join(" ", Lines.Select(l => l.HasLabel ? $"{l.Text} {l.Kind}." : l.Text))}";
}

/// <summary>A tool call, collapsed to its name and input until it is expanded to show its result.</summary>
public sealed partial class ToolRow(string id, string name, string input) : TranscriptRow
{
    public string Id { get; } = id;

    public string Name { get; } = name;

    public string Input { get; } = input;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasResult), nameof(Summary), nameof(AnnouncedName))]
    public partial string? Result { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Summary))]
    public partial bool IsError { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowsResult), nameof(Chevron), nameof(AnnouncedName))]
    public partial bool IsExpanded { get; set; }

    public bool HasResult => Result is not null;

    public bool ShowsResult => IsExpanded && HasResult;

    public string Chevron => IsExpanded ? StudioIcons.ChevronUp : StudioIcons.ChevronDown;

    public string Summary => Result switch {
        null => "Working",
        _ when IsError => "Failed",
        "" => "Done",
        var text => Lines(text),
    };

    public override string AnnouncedName => $"Tool call {Name}, {Summary}, {(IsExpanded ? "expanded" : "collapsed")}";

    private static string Lines(string text)
    {
        var count = text.Split('\n').Length;

        return count == 1 ? "1 line" : $"{count} lines";
    }
}

/// <summary>A tool call the guard refused: not an error of the run, which carries on.</summary>
public sealed class RefusalRow(string name, string input, string reason) : TranscriptRow
{
    public string Name { get; } = name;

    public string Input { get; } = input;

    public string Reason { get; } = reason;

    public override string AnnouncedName => $"Refused by the guard: {Name} {Input}. {Reason}";
}

public sealed class DiagnosticRow(string source, string text) : TranscriptRow
{
    public string Text { get; } = $"{source} · {text}";

    public override string AnnouncedName => $"Diagnostic: {Text}";
}

/// <summary>A file the run read or edited, from its tool calls that succeeded.</summary>
public sealed partial class FileRow(string path) : ObservableObject
{
    public string Path { get; } = path;

    public string Name => System.IO.Path.GetFileName(Path.TrimEnd('/'));

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Glyph))]
    public partial bool IsEdited { get; set; }

    public string Glyph => IsEdited ? StudioIcons.FilesWritten : StudioIcons.InReview;
}

/// <summary>A deliverable the run writes, with its file in the workspace.</summary>
public sealed record WriteRow(string Title, string Path);
