using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace DelphiModernizer.Studio.UI.Common.Controls;

/// <summary>Keeps a dialog 24 from the window's top and bottom, as the canvas's dialog scrim does.</summary>
/// <remarks>
/// WinUI centres a dialog in the window and caps it at 756 tall, so at 960 × 576 a long dialog meets both edges. Its
/// template binds no margin, so the cap is set on the dialog's surface once it opens.
/// </remarks>
public static class DialogMargins
{
    private const double _edge = 24;

    /// <summary>Call from the dialog's constructor.</summary>
    public static void Apply(ContentDialog dialog)
    {
        ArgumentNullException.ThrowIfNull(dialog);
        dialog.Opened += (_, _) => Fit(dialog);
    }

    private static void Fit(ContentDialog dialog)
    {
        if (dialog.XamlRoot is { } root && Find(dialog, "BackgroundElement") is { } surface)
        {
            surface.MaxHeight = Math.Min(surface.MaxHeight, root.Size.Height - (2 * _edge));
        }
    }

    private static FrameworkElement? Find(DependencyObject parent, string name)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);

            if (child is FrameworkElement element && element.Name == name)
            {
                return element;
            }

            if (Find(child, name) is { } found)
            {
                return found;
            }
        }

        return null;
    }
}
