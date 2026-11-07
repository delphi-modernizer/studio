using System;
using DelphiModernizer.Studio.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace DelphiModernizer.Studio.Modules.Overview.Application;

public static class OverviewApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddOverviewApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddMediator(typeof(OverviewApplicationServiceCollectionExtensions).Assembly);
    }
}
