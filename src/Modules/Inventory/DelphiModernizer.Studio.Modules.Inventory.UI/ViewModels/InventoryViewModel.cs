using DelphiModernizer.Studio.Application.Abstractions.Workspaces;
using DelphiModernizer.Studio.UI.Common;

namespace DelphiModernizer.Studio.Modules.Inventory.UI.ViewModels;

/// <summary>The Inventory page's state; it fills in from the engine once the page's content is built.</summary>
public sealed partial class InventoryViewModel(IWorkspaceSession session) : WorkspacePageViewModel(session);
