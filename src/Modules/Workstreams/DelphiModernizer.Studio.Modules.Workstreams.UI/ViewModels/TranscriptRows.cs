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

    /// <summary>The transcript is a ListView, which names an item's container from the item's text.</summary>
    public override string ToString()
    {
        return AnnouncedName;
    }
}

public sealed class SessionRow(string text) : TranscriptRow
{
    public string Text { get; } = text;

    public override string AnnouncedName => $"Session: {Text}";
}

/// <summary>A line of agent text, the fact label after it when it has one, and the evidence links it cites.</summary>
public sealed record AgentLineRow(string Text, FactKind? Label)
{
    public bool HasLabel => Label is not null;

    public FactKind Kind => Label ?? FactKind.Unknown;

    public IReadOnlyList<string> Evidence { get; } = RunTranscript.Evidence(Text);

    public bool HasEvidence => Evidence.Count > 0;
}

public sealed class AgentRow : TranscriptRow
{
    public AgentRow(string who, IReadOnlyList<AgentLine> lines)
    {
        Who = who;
        Lines = [.. lines.Select(l => new AgentLineRow(l.Text, Enum.TryParse(l.Label, out FactKind k) ? k : null))];
        AnnouncedName = RunNames.Agent(lines);
    }

    public string Who { get; }

    public IReadOnlyList<AgentLineRow> Lines { get; }

    public override string AnnouncedName { get; }
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

    public string Summary => RunNames.ToolSummary(Name, Result, IsError);

    public override string AnnouncedName => RunNames.Tool(Name, Summary, IsExpanded);
}

/// <summary>A tool call the guard refused: not an error of the run, which carries on.</summary>
/// <param name="path">The file the call names, which Narrator reads instead of the raw input; null for none.</param>
public sealed class RefusalRow(string name, string input, string? path, string reason) : TranscriptRow
{
    public string Name { get; } = name;

    public string Input { get; } = input;

    public string Reason { get; } = reason;

    public override string AnnouncedName => RunNames.Refusal(Name, path ?? Input, Reason);
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
