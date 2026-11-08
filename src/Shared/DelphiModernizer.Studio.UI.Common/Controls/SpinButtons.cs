using System;
using DelphiModernizer.Studio.UI.Common.Icons;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;

namespace DelphiModernizer.Studio.UI.Common.Controls;

/// <summary>Draws a NumberBox's inline spin buttons as the canvas does: 24 square, subtle, 12 px chevrons.</summary>
/// <remarks>
/// WinUI's template sizes the glyphs from the box's own font size and draws filled 32 wide buttons in primary text,
/// and it exposes neither. Set <c>controls:SpinButtons.IsCanvas="True"</c> on the NumberBox.
/// </remarks>
public static class SpinButtons
{
    public static readonly DependencyProperty IsCanvasProperty = DependencyProperty.RegisterAttached(
        "IsCanvas", typeof(bool), typeof(SpinButtons), new PropertyMetadata(false, OnIsCanvasChanged));

    private static readonly Uri _brushes = new("ms-appx:///DelphiModernizer.Studio.UI.Common/Styles/SpinButtons.xaml");

    public static bool GetIsCanvas(NumberBox box)
    {
        ArgumentNullException.ThrowIfNull(box);

        return (bool)box.GetValue(IsCanvasProperty);
    }

    public static void SetIsCanvas(NumberBox box, bool value)
    {
        ArgumentNullException.ThrowIfNull(box);
        box.SetValue(IsCanvasProperty, value);
    }

    private static void OnIsCanvasChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NumberBox box && e.NewValue is true)
        {
            box.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = _brushes });
            box.Loaded += (_, _) => Draw(box);
        }
    }

    private static void Draw(NumberBox box)
    {
        Draw(box, "UpSpinButton", StudioIcons.ChevronUp);
        Draw(box, "DownSpinButton", StudioIcons.ChevronDown);
    }

    private static void Draw(NumberBox box, string part, string glyph)
    {
        if (TemplateParts.Find(box, part) is not RepeatButton button)
        {
            return;
        }

        button.FontFamily = (FontFamily)Microsoft.UI.Xaml.Application.Current.Resources["StudioIconsFontFamily"];
        button.FontSize = 12;
        button.Content = glyph;
        button.MinWidth = 24;
        button.Width = 24;
        button.Height = 24;
        button.VerticalAlignment = VerticalAlignment.Center;
    }
}
