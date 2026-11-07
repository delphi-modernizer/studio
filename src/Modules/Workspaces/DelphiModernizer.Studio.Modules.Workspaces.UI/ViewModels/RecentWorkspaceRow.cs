using System.Globalization;
using DelphiModernizer.Studio.Application.Abstractions.Workspaces;
using DelphiModernizer.Studio.Modules.Workspaces.Application.Recent;

namespace DelphiModernizer.Studio.Modules.Workspaces.UI.ViewModels;

/// <summary>One row of Recent workspaces.</summary>
public sealed class RecentWorkspaceRow(RecentWorkspace workspace)
{
    public string Name => workspace.Name;

    public string Root => workspace.Root;

    public string Target { get; } = TargetStackNames.Of(workspace.TargetStack);

    public string OpenedAt { get; } = workspace.OpenedAt.LocalDateTime.ToString("g", CultureInfo.CurrentCulture);

    public string AnnouncedName => $"{Name}, {Target}, {Root}, opened {OpenedAt}";
}
