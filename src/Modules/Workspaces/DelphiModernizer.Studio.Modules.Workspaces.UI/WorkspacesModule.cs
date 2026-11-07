using System;
using System.Collections.Generic;
using DelphiModernizer.Studio.Application.Abstractions;
using DelphiModernizer.Studio.Modules.Workspaces.Application;
using DelphiModernizer.Studio.Modules.Workspaces.UI.ViewModels;
using DelphiModernizer.Studio.Modules.Workspaces.UI.Views;
using DelphiModernizer.Studio.UI.Common.Icons;
using DelphiModernizer.Studio.UI.Contracts;
using DelphiModernizer.Studio.UI.Contracts.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DelphiModernizer.Studio.Modules.Workspaces.UI;

/// <summary>The Start module: its page and the services behind it.</summary>
public sealed class WorkspacesModule : IModule
{
    public string Name => "Workspaces";

    public void RegisterServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddWorkspacesApplication(StudioPaths.DataDirectory);
        services.AddSingleton<IWorkspaceActions, WorkspaceActions>();
        services.AddTransient<StartViewModel>();
        services.AddTransient<StartPage>();
        services.AddTransient<NewWorkspaceViewModel>();
        services.AddTransient<NewWorkspacePage>();
    }

    public IReadOnlyList<NavigationItem> GetNavigationItems()
    {
        return [
            new NavigationItem("Start", StudioIcons.Start, StudioIcons.StartFilled, typeof(StartPage), 0),
        ];
    }

    public IReadOnlyList<Type> GetPages()
    {
        return [typeof(NewWorkspacePage)];
    }
}
