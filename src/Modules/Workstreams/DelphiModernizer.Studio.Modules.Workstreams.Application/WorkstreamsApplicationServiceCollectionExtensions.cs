using System;
using DelphiModernizer.Studio.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace DelphiModernizer.Studio.Modules.Workstreams.Application;

public static class WorkstreamsApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddWorkstreamsApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddMediator(typeof(WorkstreamsApplicationServiceCollectionExtensions).Assembly);
    }
}
