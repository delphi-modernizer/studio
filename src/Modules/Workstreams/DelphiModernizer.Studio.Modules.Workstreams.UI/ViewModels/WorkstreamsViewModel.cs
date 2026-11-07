using DelphiModernizer.Studio.Application.Abstractions.Workspaces;
using DelphiModernizer.Studio.UI.Common;

namespace DelphiModernizer.Studio.Modules.Workstreams.UI.ViewModels;

/// <summary>The Workstreams page's state; it fills in from the engine once the page's content is built.</summary>
public sealed partial class WorkstreamsViewModel(IWorkspaceSession session) : WorkspacePageViewModel(session);
