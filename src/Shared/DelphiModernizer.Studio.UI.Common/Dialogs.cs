using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DelphiModernizer.Studio.UI.Common;

/// <summary>Places a content dialog over a window, in the theme the person chose for the studio.</summary>
/// <remarks>
/// A ContentDialog opens in the window's popup layer, which takes the app's theme rather than the theme the studio sets
/// on the window's content, so a studio set to Dark on a light Windows would otherwise show its dialogs light.
/// </remarks>
public static class Dialogs
{
    public static T Over<T>(this T dialog, XamlRoot root)
        where T : ContentDialog
    {
        ArgumentNullException.ThrowIfNull(dialog);
        ArgumentNullException.ThrowIfNull(root);
        dialog.XamlRoot = root;
        dialog.RequestedTheme = root.Content is FrameworkElement content ? content.ActualTheme : ElementTheme.Default;

        return dialog;
    }
}
