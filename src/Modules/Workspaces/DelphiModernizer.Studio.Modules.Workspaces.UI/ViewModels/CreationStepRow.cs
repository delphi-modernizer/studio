using CommunityToolkit.Mvvm.ComponentModel;
using DelphiModernizer.Engine.Workspaces;

namespace DelphiModernizer.Studio.Modules.Workspaces.UI.ViewModels;

/// <summary>One line of the progress list while the workspace is created.</summary>
public sealed partial class CreationStepRow(InitStep step, string label) : ObservableObject
{
    public InitStep Step => step;

    public string Label => label;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWaiting), nameof(IsWorking), nameof(IsDone), nameof(IsFailed), nameof(StatusText), nameof(AnnouncedName))]
    public partial InitStepState? State { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText), nameof(AnnouncedName))]
    public partial string? Detail { get; set; }

    public bool IsWaiting => State is null or InitStepState.Skipped;

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

    public string AnnouncedName => $"{label}, {StatusText}";
}
