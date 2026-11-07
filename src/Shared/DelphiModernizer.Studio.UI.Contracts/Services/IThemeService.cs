using Microsoft.UI.Xaml;

namespace DelphiModernizer.Studio.UI.Contracts.Services;

/// <summary>
/// Applies the person's theme to the main window and remembers it; Windows' contrast themes override it.
/// </summary>
public interface IThemeService
{
    ElementTheme Theme { get; }

    /// <summary>Applies the remembered theme; call once, after the main window has content.</summary>
    void Initialize();

    void SetTheme(ElementTheme theme);
}
