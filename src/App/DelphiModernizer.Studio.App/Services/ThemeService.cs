using System;
using DelphiModernizer.Studio.UI.Contracts.Services;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.UI;

namespace DelphiModernizer.Studio.App.Services;

/// <summary>Applies the person's theme to the window's content and caption buttons, and remembers it.</summary>
/// <remarks>
/// With the content drawn into the title bar the system draws the caption buttons, so their colours are set on
/// <c>AppWindow.TitleBar</c>; while the theme follows Windows they follow the content's actual theme.
/// </remarks>
public sealed class ThemeService : IThemeService
{
    private const string _key = "Theme";

    private readonly ILocalSettingsService _settings;
    private readonly MainWindow _window;

    public ThemeService(ILocalSettingsService settings, MainWindow window)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(window);
        _settings = settings;
        _window = window;
    }

    public ElementTheme Theme { get; private set; }

    public void Initialize()
    {
        Theme = _settings.Read<ElementTheme>(_key);

        if (_window.Content is FrameworkElement root)
        {
            root.ActualThemeChanged += (_, _) => PaintCaptionButtons();
        }

        Apply();
    }

    public void SetTheme(ElementTheme theme)
    {
        Theme = theme;
        _settings.Save(_key, theme);
        Apply();
    }

    private void Apply()
    {
        if (_window.Content is FrameworkElement root)
        {
            root.RequestedTheme = Theme;
        }

        PaintCaptionButtons();
    }

    private void PaintCaptionButtons()
    {
        var dark = (_window.Content as FrameworkElement)?.ActualTheme == ElementTheme.Dark;
        AppWindowTitleBar bar = _window.AppWindow.TitleBar;
        var foreground = dark ? Colors.White : Colors.Black;
        bar.ButtonBackgroundColor = Colors.Transparent;
        bar.ButtonInactiveBackgroundColor = Colors.Transparent;
        bar.ButtonForegroundColor = foreground;
        bar.ButtonHoverForegroundColor = foreground;
        bar.ButtonPressedForegroundColor = foreground;
        bar.ButtonInactiveForegroundColor = dark
            ? Color.FromArgb(0xFF, 0x9A, 0x9A, 0x9A)
            : Color.FromArgb(0xFF, 0x60, 0x60, 0x60);
        bar.ButtonHoverBackgroundColor = dark
            ? Color.FromArgb(0x20, 0xFF, 0xFF, 0xFF)
            : Color.FromArgb(0x14, 0x00, 0x00, 0x00);
        bar.ButtonPressedBackgroundColor = dark
            ? Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF)
            : Color.FromArgb(0x24, 0x00, 0x00, 0x00);
    }
}
