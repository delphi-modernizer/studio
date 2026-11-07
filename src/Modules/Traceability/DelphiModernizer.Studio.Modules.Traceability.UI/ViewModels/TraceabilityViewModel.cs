using DelphiModernizer.Studio.Application.Abstractions.Workspaces;
using DelphiModernizer.Studio.UI.Common;

namespace DelphiModernizer.Studio.Modules.Traceability.UI.ViewModels;

/// <summary>The Traceability page's state; it fills in from the engine once the page's content is built.</summary>
public sealed partial class TraceabilityViewModel(IWorkspaceSession session) : WorkspacePageViewModel(session);
