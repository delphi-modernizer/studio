using System;
using DelphiModernizer.Studio.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace DelphiModernizer.Studio.Modules.Deliverables.Application;

public static class DeliverablesApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddDeliverablesApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddMediator(typeof(DeliverablesApplicationServiceCollectionExtensions).Assembly);
    }
}
