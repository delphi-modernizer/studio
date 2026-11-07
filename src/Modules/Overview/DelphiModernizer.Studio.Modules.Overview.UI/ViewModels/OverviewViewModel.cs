using DelphiModernizer.Studio.Application.Abstractions.Workspaces;
using DelphiModernizer.Studio.UI.Common;

namespace DelphiModernizer.Studio.Modules.Overview.UI.ViewModels;

/// <summary>The Overview page's state; it fills in from the engine once the page's content is built.</summary>
public sealed partial class OverviewViewModel(IWorkspaceSession session) : WorkspacePageViewModel(session);
