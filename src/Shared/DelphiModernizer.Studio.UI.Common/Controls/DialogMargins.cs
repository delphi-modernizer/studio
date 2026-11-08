using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace DelphiModernizer.Studio.UI.Common.Controls;

/// <summary>Keeps a dialog 24 from the window's top and bottom, as the canvas's dialog scrim does.</summary>
/// <remarks>
/// WinUI centres a dialog in the window and caps it at 756 tall, so at 960 × 576 a long dialog meets both edges. Its
/// template binds no margin, so the cap is set on the dialog's surface while it is open, and set again each time the
/// window changes size.
/// </remarks>
public static class DialogMargins
{
    private const double _edge = 24;

    /// <summary>Call from the dialog's constructor.</summary>
    public static void Apply(ContentDialog dialog)
    {
        ArgumentNullException.ThrowIfNull(dialog);

        FrameworkElement? surface = null;
        double templateMax = double.PositiveInfinity;
        XamlRoot? watched = null;

        void Fit(XamlRoot root, XamlRootChangedEventArgs? args)
        {
            if (surface is not null)
            {
                surface.MaxHeight = Math.Min(templateMax, root.Size.Height - (2 * _edge));
            }
        }

        dialog.Opened += (_, _) => {
            if (surface is null && Find(dialog, "BackgroundElement") is { } found)
            {
                surface = found;
                templateMax = found.MaxHeight;
            }

            watched = dialog.XamlRoot;

            if (watched is not null)
            {
                Fit(watched, null);
                watched.Changed += Fit;
            }
        };
        dialog.Closed += (_, _) => {
            if (watched is not null)
            {
                watched.Changed -= Fit;
                watched = null;
            }
        };
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
