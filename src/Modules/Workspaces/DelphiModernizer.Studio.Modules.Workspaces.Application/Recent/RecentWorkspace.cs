using System;

namespace DelphiModernizer.Studio.Modules.Workspaces.Application.Recent;

/// <param name="TargetStack"><c>dotnet</c>, <c>jvm</c> or <c>deferred</c>, as the manifest says.</param>
public sealed record RecentWorkspace(string Name, string Root, string TargetStack, DateTimeOffset OpenedAt);
