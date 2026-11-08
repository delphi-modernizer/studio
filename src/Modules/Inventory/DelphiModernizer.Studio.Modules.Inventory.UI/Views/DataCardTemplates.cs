using DelphiModernizer.Studio.Modules.Inventory.UI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DelphiModernizer.Studio.Modules.Inventory.UI.Views;

/// <summary>Picks a data card's template by the kind of card.</summary>
public sealed partial class DataCardTemplates : DataTemplateSelector
{
    public DataTemplate? Technology { get; set; }

    public DataTemplate? Connection { get; set; }

    public DataTemplate? Externals { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item)
    {
        return item switch {
            TechnologyCard => Technology,
            ConnectionCard => Connection,
            ExternalsCard => Externals,
            _ => null,
        };
    }

    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
    {
        return SelectTemplateCore(item);
    }
}
