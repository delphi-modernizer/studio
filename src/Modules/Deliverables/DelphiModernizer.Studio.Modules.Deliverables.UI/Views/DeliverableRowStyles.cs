using DelphiModernizer.Studio.Modules.Deliverables.UI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DelphiModernizer.Studio.Modules.Deliverables.UI.Views;

/// <summary>Picks a row's container style: a sign-off's heading row or a deliverable's row.</summary>
public sealed partial class DeliverableRowStyles : StyleSelector
{
    public Style? Deliverable { get; set; }

    public Style? Group { get; set; }

    protected override Style? SelectStyleCore(object item, DependencyObject container)
    {
        return item is DeliverableRow { IsGroup: true } ? Group : Deliverable;
    }
}
