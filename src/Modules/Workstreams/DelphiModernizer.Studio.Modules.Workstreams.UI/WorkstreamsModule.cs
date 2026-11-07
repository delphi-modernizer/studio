using System;
using System.Collections.Generic;
using DelphiModernizer.Studio.Modules.Workstreams.Application;
using DelphiModernizer.Studio.Modules.Workstreams.UI.ViewModels;
using DelphiModernizer.Studio.Modules.Workstreams.UI.Views;
using DelphiModernizer.Studio.UI.Common.Icons;
using DelphiModernizer.Studio.UI.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace DelphiModernizer.Studio.Modules.Workstreams.UI;

/// <summary>The Workstreams module: its page, the Run page reached from it, and the services behind them.</summary>
public sealed class WorkstreamsModule : IModule
{
    public string Name => "Workstreams";

    public void RegisterServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddWorkstreamsApplication();
        services.AddSingleton<RunsBadge>();
        services.AddTransient<WorkstreamsViewModel>();
        services.AddTransient<WorkstreamsPage>();
        services.AddTransient<RunViewModel>();
        services.AddTransient<RunPage>();
    }

    public IReadOnlyList<NavigationItem> GetNavigationItems()
    {
        return [
            new NavigationItem("Workstreams", StudioIcons.Workstreams, StudioIcons.WorkstreamsFilled, typeof(WorkstreamsPage), 30) {
                Badge = provider => provider.GetRequiredService<RunsBadge>(),
            },
        ];
    }

    public IReadOnlyList<Type> GetPages()
    {
        return [typeof(RunPage)];
    }
}
