using System;
using DelphiModernizer.Studio.Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DelphiModernizer.Studio.Modules.Workstreams.Application;

public static class WorkstreamsApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddWorkstreamsApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<IClaudeLocator, ClaudeLocator>();
        services.AddSingleton<RunMonitor>();

        return services.AddMediator(typeof(WorkstreamsApplicationServiceCollectionExtensions).Assembly);
    }
}
