using CommunityToolkit.Mvvm.ComponentModel;

namespace DelphiModernizer.Studio.Modules.Workspaces.UI.ViewModels;

/// <summary>One row of the essential context; a row marked Unknown, or left empty, becomes an open question.</summary>
public sealed partial class ContextField(string label) : ObservableObject
{
    public string Label => label;

    [ObservableProperty]
    public partial string Value { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Placeholder))]
    public partial bool IsUnknown { get; set; }

    public string Placeholder => IsUnknown ? "Unknown" : string.Empty;

    public string UnknownName => $"{label} unknown. Adds an open question.";

    /// <summary>What the engine records: the value, or null for a question.</summary>
    public string? Answer => IsUnknown || string.IsNullOrWhiteSpace(Value) ? null : Value.Trim();
}
