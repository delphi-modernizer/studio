using CommunityToolkit.Mvvm.ComponentModel;

namespace DelphiModernizer.Studio.Modules.Workspaces.UI.ViewModels;

/// <summary>One entry of the wizard's step list.</summary>
public sealed partial class WizardStep(int number, string label) : ObservableObject
{
    public int Number => number;

    public string NumberText => number.ToString(System.Globalization.CultureInfo.CurrentCulture);

    public string Label => label;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTodo))]
    public partial bool IsDone { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTodo))]
    public partial bool IsCurrent { get; set; }

    public bool IsTodo => !IsDone && !IsCurrent;

    public void Show(int current)
    {
        IsDone = number < current;
        IsCurrent = number == current;
    }
}
