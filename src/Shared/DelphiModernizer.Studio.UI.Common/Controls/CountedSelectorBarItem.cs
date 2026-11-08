using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace DelphiModernizer.Studio.UI.Common.Controls;

/// <summary>A SelectorBar item with its label, then its count in smaller secondary text, as drawn.</summary>
public static class CountedSelectorBarItem
{
    /// <param name="announcedName">What Narrator reads, such as "Code modules, 7".</param>
    public static SelectorBarItem Create(string label, string count, string announcedName, object tag)
    {
        var panel = new StackPanel {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(12, 0, 12, 0),
        };
        panel.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center });
        panel.Children.Add(new TextBlock {
            Text = count,
            VerticalAlignment = VerticalAlignment.Center,
            Style = (Style)Microsoft.UI.Xaml.Application.Current.Resources["SecondaryCaptionTextStyle"],
        });

        var item = new SelectorBarItem { Child = panel, Tag = tag };
        AutomationProperties.SetName(item, announcedName);

        return item;
    }
}
