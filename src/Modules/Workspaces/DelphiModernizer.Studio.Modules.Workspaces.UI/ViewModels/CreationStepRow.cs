using CommunityToolkit.Mvvm.ComponentModel;
using DelphiModernizer.Engine.Workspaces;

namespace DelphiModernizer.Studio.Modules.Workspaces.UI.ViewModels;

/// <summary>One line of the progress list while the workspace is created.</summary>
public sealed partial class CreationStepRow(InitStep step, string label) : ObservableObject
{
    public InitStep Step => step;

    /// <summary>The step's name; the wizard words the records step by how many questions it writes.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AnnouncedName))]
    public partial string Label { get; set; } = label;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWaiting), nameof(IsStarted), nameof(IsWorking), nameof(IsDone), nameof(IsFailed))]
    [NotifyPropertyChangedFor(nameof(StatusText), nameof(AnnouncedName))]
    public partial InitStepState? State { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    public partial string? Detail { get; set; }

    public bool IsWaiting => State is null or InitStepState.Skipped;

    public bool IsStarted => !IsWaiting;

    public bool IsWorking => State == InitStepState.Working;

    public bool IsDone => State == InitStepState.Done;

    public bool IsFailed => State == InitStepState.Failed;

    public string StatusText => State switch {
        InitStepState.Working => "Working…",
        InitStepState.Done => Detail ?? "Done",
        InitStepState.Skipped => "Skipped",
        InitStepState.Failed => Detail ?? "Failed",
        _ => "Waiting",
    };

    /// <summary>What Narrator reads as the step changes, such as "Initialise git, working" and then "done".</summary>
    public string AnnouncedName => State switch {
        InitStepState.Working => $"{Label}, working",
        InitStepState.Done => $"{Label}, done",
        InitStepState.Skipped => $"{Label}, skipped",
        InitStepState.Failed => $"{Label}, failed",
        _ => $"{Label}, waiting",
    };
}
