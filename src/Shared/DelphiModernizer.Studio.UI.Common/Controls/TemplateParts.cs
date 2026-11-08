using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace DelphiModernizer.Studio.UI.Common.Controls;

/// <summary>Finds a named part inside a control's applied template.</summary>
internal static class TemplateParts
{
    public static FrameworkElement? Find(DependencyObject parent, string name)
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
