using System;
using System.Collections.Generic;
using DelphiModernizer.Studio.Modules.Deliverables.UI.ViewModels;
using DelphiModernizer.Studio.Modules.Deliverables.UI.Views;
using DelphiModernizer.Studio.UI.Common.Icons;
using DelphiModernizer.Studio.UI.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace DelphiModernizer.Studio.Modules.Deliverables.UI;

/// <summary>The Deliverables module: its page and the services behind it.</summary>
public sealed class DeliverablesModule : IModule
{
    public string Name => "Deliverables";

    public void RegisterServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddTransient<DeliverablesViewModel>();
        services.AddTransient<DeliverablesPage>();
    }

    public IReadOnlyList<NavigationItem> GetNavigationItems()
    {
        return [
            new NavigationItem("Deliverables", StudioIcons.Deliverables, StudioIcons.DeliverablesFilled, typeof(DeliverablesPage), 40),
        ];
    }
}
