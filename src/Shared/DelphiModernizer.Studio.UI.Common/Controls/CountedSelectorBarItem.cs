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
        // The template puts Child outside its padded StackPanel, so this margin draws the canvas's 40 tall .seli:
        // with the bar's 4, the label sits 10 below the bar top and 11 above the underline.
        var panel = new StackPanel {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(12, 6, 12, 6),
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
