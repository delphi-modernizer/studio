using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;

namespace DelphiModernizer.Studio.UI.Common.Controls;

/// <summary>A state as a word and a glyph in a small rounded chip; colour only adds to them.</summary>
/// <remarks>
/// Narrator reads the word alone. With <c>AutomationProperties.LiveSetting</c> set, as on run pills, each change of the
/// word is announced.
/// </remarks>
public sealed partial class StatusPill : UserControl
{
    public static readonly DependencyProperty StateProperty = DependencyProperty.Register(
        nameof(State), typeof(PillState), typeof(StatusPill), new PropertyMetadata(PillState.Blocked, OnStateChanged));

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(string), typeof(StatusPill), new PropertyMetadata(null, OnStateChanged));

    public static readonly DependencyProperty GlyphProperty = DependencyProperty.Register(
        nameof(Glyph), typeof(string), typeof(StatusPill), new PropertyMetadata(null, OnStateChanged));

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

    /// <summary>
    /// Words that replace the state's own, such as the version Claude Code reports; null keeps the state's.
    /// </summary>
    public string? Text
    {
        get => (string?)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>A Studio Icons glyph that replaces the state's own, such as the key on a credential; null keeps it.</summary>
    public string? Glyph
    {
        get => (string?)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new TextAutomationPeer(this, nameof(StatusPill));
    }

    private static void OnStateChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        ((StatusPill)sender).Show();
    }

    private void Show()
    {
        var look = PillLook.Of(State);
        bool running = State == PillState.Running;
        string word = string.IsNullOrEmpty(Text) ? look.Word : Text;
        bool changed = Word.Text != word;
        Word.Text = word;
        Icon.Glyph = string.IsNullOrEmpty(Glyph) ? look.Glyph : Glyph;
        Icon.Visibility = running ? Visibility.Collapsed : Visibility.Visible;
        Ring.Visibility = running ? Visibility.Visible : Visibility.Collapsed;
        Ring.IsActive = running;
        AutomationProperties.SetName(this, word);
        VisualStateManager.GoToState(this, look.Tone.ToString(), false);

        if (changed && IsLoaded && AutomationProperties.GetLiveSetting(this) != AutomationLiveSetting.Off)
        {
            FrameworkElementAutomationPeer.FromElement(this)?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
        }
    }
}
