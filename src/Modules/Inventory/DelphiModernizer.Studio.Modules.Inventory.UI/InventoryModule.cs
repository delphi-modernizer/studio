using System;
using System.Collections.Generic;
using DelphiModernizer.Studio.Modules.Inventory.Application;
using DelphiModernizer.Studio.Modules.Inventory.UI.ViewModels;
using DelphiModernizer.Studio.Modules.Inventory.UI.Views;
using DelphiModernizer.Studio.UI.Common.Icons;
using DelphiModernizer.Studio.UI.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace DelphiModernizer.Studio.Modules.Inventory.UI;

/// <summary>The Inventory module: its page and the services behind it.</summary>
public sealed class InventoryModule : IModule
{
    public string Name => "Inventory";

    public void RegisterServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddInventoryApplication();
        services.AddTransient<InventoryViewModel>();
        services.AddTransient<InventoryPage>();
    }

    public IReadOnlyList<NavigationItem> GetNavigationItems()
    {
        return [
            new NavigationItem("Inventory", StudioIcons.Inventory, StudioIcons.InventoryFilled, typeof(InventoryPage), 20),
        ];
    }
}
