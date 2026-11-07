using System;
using DelphiModernizer.Studio.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace DelphiModernizer.Studio.Modules.Traceability.Application;

public static class TraceabilityApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddTraceabilityApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddMediator(typeof(TraceabilityApplicationServiceCollectionExtensions).Assembly);
    }
}
