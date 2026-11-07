using DelphiModernizer.Studio.UI.Common.Controls;
using DelphiModernizer.Studio.UI.Common.Icons;

namespace DelphiModernizer.Studio.Modules.Overview.UI.ViewModels;

/// <summary>The tone of the next-step bar: what was done, what is ready, what is broken.</summary>
public enum NextStepTone
{
    Success,
    Informational,
    Caution,
    Critical,
}

/// <summary>Where the next-step bar's button goes.</summary>
public enum NextStepAction
{
    None,
    SignOffs,
    Traceability,
}

public sealed record NextStep(NextStepTone Tone, string Title, string Message, string ActionText, NextStepAction Action, string? Gate)
{
    public bool HasAction => Action != NextStepAction.None;

    public string Glyph => Tone switch {
        NextStepTone.Success => StudioIcons.Approved,
        NextStepTone.Caution => StudioIcons.WarningFilled,
        NextStepTone.Critical => StudioIcons.ErrorFilled,
        _ => StudioIcons.Ready,
    };
}

/// <summary>A team or the Principal Architect on the diagram.</summary>
public sealed record TeamNode(string Code, string Name, string Glyph, string Progress, string AnnouncedName, string ToolTip);

/// <summary>A sign-off on the diagram, in order; the next one carries the accent.</summary>
public sealed record GateNode(string Gate, string Name, PillState Pill, bool IsNext, string AnnouncedName)
{
    public string Word => PillLook.Of(Pill).Word;
}

public sealed record BuildNode(PillState Pill, string Subtitle, bool IsUnlocked, string AnnouncedName)
{
    public bool IsLocked => !IsUnlocked;
}

public sealed record QuestionRow(string Id, string Text, string Path, int Line);

public sealed record RunRow(string Title, string Subtitle, PillState Pill);
