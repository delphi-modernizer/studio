using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace DelphiModernizer.Studio.UI.Common.Controls;

/// <summary>A path in Cascadia Mono, secondary, that keeps its end when narrow: "…Sales/Pricing.pas".</summary>
/// <remarks>WinUI trims text only at the end. Narrator always reads the whole path.</remarks>
public sealed partial class PathText : UserControl
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text),
        typeof(string),
        typeof(PathText),
        new PropertyMetadata(string.Empty, OnTextChanged));

    private const string _ellipsis = "…";

    private readonly TextBlock _shown = Mono();

    /// <summary>Measures candidates outside the tree, so measuring never changes what is shown mid-layout.</summary>
    private readonly TextBlock _probe = Mono();

    public PathText()
    {
        AutomationProperties.SetAccessibilityView(_shown, AccessibilityView.Raw);
        IsTabStop = false;
        Content = _shown;
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new TextAutomationPeer(this, nameof(PathText));
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var fitted = Fit(Text ?? string.Empty, availableSize.Width);

        if (_shown.Text != fitted)
        {
            _shown.Text = fitted;
        }

        return base.MeasureOverride(availableSize);
    }

    private static TextBlock Mono()
    {
        return new TextBlock {
            Style = (Style)Microsoft.UI.Xaml.Application.Current.Resources["MonoTextStyle"],
            TextTrimming = TextTrimming.None,
            TextWrapping = TextWrapping.NoWrap,
        };
    }

    private static void OnTextChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var path = (PathText)sender;
        AutomationProperties.SetName(path, (string?)args.NewValue ?? string.Empty);
        path.InvalidateMeasure();
    }

    /// <summary>The whole path when it fits, else "…" and the longest end of it that does.</summary>
    private string Fit(string path, double width)
    {
        if (double.IsInfinity(width) || Fits(path, width))
        {
            return path;
        }

        var kept = 0;
        var most = path.Length - 1;

        while (kept < most)
        {
            var middle = (kept + most + 1) / 2;

            if (Fits(_ellipsis + path[^middle..], width))
            {
                kept = middle;
            }
            else
            {
                most = middle - 1;
            }
        }

        return _ellipsis + path[^kept..];
    }

    private bool Fits(string text, double width)
    {
        _probe.Text = text;
        _probe.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        return _probe.DesiredSize.Width <= width;
    }
}
