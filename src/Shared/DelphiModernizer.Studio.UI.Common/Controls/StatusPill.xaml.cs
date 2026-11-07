using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace DelphiModernizer.Studio.UI.Common.Controls;

/// <summary>A state as a word and a glyph in a small rounded chip; colour only adds to them.</summary>
public sealed partial class StatusPill : UserControl
{
    public static readonly DependencyProperty StateProperty = DependencyProperty.Register(
        nameof(State), typeof(PillState), typeof(StatusPill), new PropertyMetadata(PillState.Blocked, OnStateChanged));

    public StatusPill()
    {
        InitializeComponent();
        Loaded += (_, _) => Show();
    }

    public PillState State
    {
        get => (PillState)GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }

    private static void OnStateChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        ((StatusPill)sender).Show();
    }

    private void Show()
    {
        var look = PillLook.Of(State);
        var running = State == PillState.Running;
        Word.Text = look.Word;
        Glyph.Glyph = look.Glyph;
        Glyph.Visibility = running ? Visibility.Collapsed : Visibility.Visible;
        Ring.Visibility = running ? Visibility.Visible : Visibility.Collapsed;
        Ring.IsActive = running;
        AutomationProperties.SetName(this, look.Word);
        VisualStateManager.GoToState(this, look.Tone.ToString(), false);
    }
}
