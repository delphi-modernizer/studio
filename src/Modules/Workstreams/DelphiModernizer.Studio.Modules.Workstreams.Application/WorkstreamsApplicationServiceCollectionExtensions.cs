using System;
using DelphiModernizer.Studio.Application.Abstractions.Workstreams;
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
        services.AddSingleton<IRunsInProgress>(provider => provider.GetRequiredService<RunMonitor>());

        return services.AddMediator(typeof(WorkstreamsApplicationServiceCollectionExtensions).Assembly);
    }
}
