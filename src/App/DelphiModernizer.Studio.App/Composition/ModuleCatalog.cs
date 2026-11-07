using System.Collections.Generic;
using DelphiModernizer.Studio.Modules.Deliverables.UI;
using DelphiModernizer.Studio.Modules.Inventory.UI;
using DelphiModernizer.Studio.Modules.Overview.UI;
using DelphiModernizer.Studio.Modules.Settings.UI;
using DelphiModernizer.Studio.Modules.SignOffs.UI;
using DelphiModernizer.Studio.Modules.Traceability.UI;
using DelphiModernizer.Studio.Modules.Workspaces.UI;
using DelphiModernizer.Studio.Modules.Workstreams.UI;
using DelphiModernizer.Studio.UI.Contracts;

namespace DelphiModernizer.Studio.App.Composition;

/// <summary>The modules composed into the app; the pane orders their pages by each item's Order.</summary>
internal static class ModuleCatalog
{
    public static IReadOnlyList<IModule> Modules { get; } = [
        new WorkspacesModule(),
        new OverviewModule(),
        new InventoryModule(),
        new WorkstreamsModule(),
        new DeliverablesModule(),
        new TraceabilityModule(),
        new SignOffsModule(),
        new SettingsModule(),
    ];
}
