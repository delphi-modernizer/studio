using System;
using System.Collections.Generic;
using DelphiModernizer.Studio.Modules.Workstreams.Application;
using DelphiModernizer.Studio.Modules.Workstreams.UI.ViewModels;
using DelphiModernizer.Studio.Modules.Workstreams.UI.Views;
using DelphiModernizer.Studio.UI.Common.Icons;
using DelphiModernizer.Studio.UI.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace DelphiModernizer.Studio.Modules.Workstreams.UI;

/// <summary>The Workstreams module: its page and the services behind it.</summary>
public sealed class WorkstreamsModule : IModule
{
    public string Name => "Workstreams";

    public void RegisterServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddWorkstreamsApplication();
        services.AddTransient<WorkstreamsViewModel>();
        services.AddTransient<WorkstreamsPage>();
    }

    public IReadOnlyList<NavigationItem> GetNavigationItems()
    {
        return [
            new NavigationItem("Workstreams", StudioIcons.Workstreams, StudioIcons.WorkstreamsFilled, typeof(WorkstreamsPage), 30),
        ];
    }
}
