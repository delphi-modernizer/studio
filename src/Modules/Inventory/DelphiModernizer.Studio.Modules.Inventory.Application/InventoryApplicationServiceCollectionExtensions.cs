using System;
using DelphiModernizer.Studio.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace DelphiModernizer.Studio.Modules.Inventory.Application;

public static class InventoryApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddInventoryApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddMediator(typeof(InventoryApplicationServiceCollectionExtensions).Assembly);
    }
}
