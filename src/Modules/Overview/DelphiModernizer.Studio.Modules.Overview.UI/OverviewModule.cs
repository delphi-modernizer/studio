using System;
using System.Collections.Generic;
using DelphiModernizer.Studio.Modules.Overview.Application;
using DelphiModernizer.Studio.Modules.Overview.UI.ViewModels;
using DelphiModernizer.Studio.Modules.Overview.UI.Views;
using DelphiModernizer.Studio.UI.Common.Icons;
using DelphiModernizer.Studio.UI.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace DelphiModernizer.Studio.Modules.Overview.UI;

/// <summary>The Overview module: its page and the services behind it.</summary>
public sealed class OverviewModule : IModule
{
    public string Name => "Overview";

    public void RegisterServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddOverviewApplication();
        services.AddTransient<OverviewViewModel>();
        services.AddTransient<OverviewPage>();
    }

    public IReadOnlyList<NavigationItem> GetNavigationItems()
    {
        return [
            new NavigationItem("Overview", StudioIcons.Overview, StudioIcons.OverviewFilled, typeof(OverviewPage), 10),
        ];
    }
}
