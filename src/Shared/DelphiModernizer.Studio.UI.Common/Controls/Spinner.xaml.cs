using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.ViewManagement;

namespace DelphiModernizer.Studio.UI.Common.Controls;

/// <summary>The design's small busy ring: a half arc in the foreground colour, sized by Width and Height.</summary>
/// <remarks>
/// WinUI's ProgressRing at 12 to 20 px shows little more than a tick. With animations turned off in Windows the arc
/// stands still, as the canvas's reduced-motion rule draws it.
/// </remarks>
public sealed partial class Spinner : UserControl
{
    public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register(
        nameof(IsActive), typeof(bool), typeof(Spinner), new PropertyMetadata(true, OnIsActiveChanged));

    public Spinner()
    {
        InitializeComponent();
        Loaded += (_, _) => Animate();
        Unloaded += (_, _) => Spin.Stop();
    }

    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    private static void OnIsActiveChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        ((Spinner)sender).Animate();
    }

    private void Animate()
    {
        if (IsActive && IsLoaded && new UISettings().AnimationsEnabled)
        {
            Spin.Begin();
        }
        else
        {
            Spin.Stop();
        }
    }
}
