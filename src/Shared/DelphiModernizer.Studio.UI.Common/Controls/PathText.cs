using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Windows.Foundation;

namespace DelphiModernizer.Studio.UI.Common.Controls;

/// <summary>
/// A path in Cascadia Mono, secondary unless Foreground is set, that keeps its file name whole and as much of its start
/// as fits: "docs/02-…/business-rules.md" (canvas Run, Files touched).
/// </summary>
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
        RegisterPropertyChangedCallback(ForegroundProperty, (_, _) => FollowForeground());
        Loaded += (_, _) => FollowForeground();
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
        string fitted = Fit(Text ?? string.Empty, availableSize.Width);

        if (_shown.Text != fitted)
        {
            _shown.Text = fitted;
        }

        return base.MeasureOverride(availableSize);
    }

    /// <summary>
    /// A Foreground set on the control, such as a ThemeResource, replaces the style's secondary text and follows theme
    /// changes. It is checked on load too, since the callback alone missed the value a template sets.
    /// </summary>
    private void FollowForeground()
    {
        if (ReadLocalValue(ForegroundProperty) != DependencyProperty.UnsetValue
            && _shown.ReadLocalValue(TextBlock.ForegroundProperty) == DependencyProperty.UnsetValue)
        {
            _shown.SetBinding(
                TextBlock.ForegroundProperty,
                new Binding { Source = this, Path = new PropertyPath(nameof(Foreground)) });
        }
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

    /// <summary>
    /// The whole path when it fits; else its start, "…" and the file name; else "…" and the longest end that fits.
    /// </summary>
    private string Fit(string path, double width)
    {
        if (double.IsInfinity(width) || Fits(path, width))
        {
            return path;
        }

        int slash = path.LastIndexOfAny(['/', '\\']);

        if (slash > 0 && Fits(_ellipsis + path[slash..], width))
        {
            return KeepName(path, slash, width);
        }

        int kept = 0;
        int most = path.Length - 1;

        while (kept < most)
        {
            int middle = (kept + most + 1) / 2;

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

    /// <summary>The longest start of the folders that fits before "…" and the separator and file name.</summary>
    private string KeepName(string path, int slash, double width)
    {
        string end = _ellipsis + path[slash..];
        int kept = 0;
        int most = slash - 1;

        while (kept < most)
        {
            int middle = (kept + most + 1) / 2;

            if (Fits(path[..middle] + end, width))
            {
                kept = middle;
            }
            else
            {
                most = middle - 1;
            }
        }

        return path[..kept] + end;
    }

    private bool Fits(string text, double width)
    {
        _probe.Text = text;
        _probe.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        return _probe.DesiredSize.Width <= width;
    }
}
