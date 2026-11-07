using System;
using System.IO;
using DelphiModernizer.Studio.App.UI;
using Microsoft.UI.Windowing;
using WinUIEx;

namespace DelphiModernizer.Studio.App;

/// <summary>The one window: Mica, the content drawn into the title bar, and the shell inside it.</summary>
/// <remarks>
/// 1280 × 760 by default, as the brief says, and never smaller than the design's 960 × 576 minimum.
/// </remarks>
public sealed partial class MainWindow : WindowEx
{
    public MainWindow()
    {
        InitializeComponent();

        var icon = Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");

        if (File.Exists(icon))
        {
            AppWindow.SetIcon(icon);
        }

        this.CenterOnScreen();
    }

    internal void AttachShell(ShellPage shell)
    {
        ArgumentNullException.ThrowIfNull(shell);
        ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        Root.Children.Add(shell);
        SetTitleBar(shell.TitleBar);
    }
}
