using System;
using DelphiModernizer.Studio.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace DelphiModernizer.Studio.Modules.SignOffs.Application;

public static class SignOffsApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddSignOffsApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddMediator(typeof(SignOffsApplicationServiceCollectionExtensions).Assembly);
    }
}
