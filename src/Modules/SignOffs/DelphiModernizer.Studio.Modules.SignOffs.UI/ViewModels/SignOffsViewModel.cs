using DelphiModernizer.Studio.Application.Abstractions.Workspaces;
using DelphiModernizer.Studio.UI.Common;

namespace DelphiModernizer.Studio.Modules.SignOffs.UI.ViewModels;

/// <summary>The Sign-offs page's state; it fills in from the engine once the page's content is built.</summary>
public sealed partial class SignOffsViewModel(IWorkspaceSession session) : WorkspacePageViewModel(session);
