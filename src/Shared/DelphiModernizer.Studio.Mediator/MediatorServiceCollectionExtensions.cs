using System;
using System.Linq;
using System.Reflection;
using DelphiModernizer.Studio.Application.Abstractions;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;
using DelphiModernizer.Studio.Mediator.Behaviors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DelphiModernizer.Studio.Mediator;

/// <summary>Registers the mediator and the handlers it finds in the given assemblies.</summary>
public static class MediatorServiceCollectionExtensions
{
    private static readonly Type[] _handlerInterfaces = [typeof(IRequestHandler<,>), typeof(INotificationHandler<>)];

    /// <summary>Adds the mediator once and every request and notification handler in <paramref name="handlerAssembly"/>.</summary>
    /// <remarks>Each module calls this for its own Application assembly; the core services are added only once.</remarks>
    public static IServiceCollection AddMediator(this IServiceCollection services, Assembly handlerAssembly)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(handlerAssembly);

        services.TryAddTransient<IMediator, Mediator>();
        services.TryAddTransient<ISender>(provider => provider.GetRequiredService<IMediator>());
        services.TryAddTransient<IPublisher>(provider => provider.GetRequiredService<IMediator>());
        services.TryAddSingleton<IClock, SystemClock>();
        services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>)));

        var implementations = handlerAssembly
            .GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false });

        foreach (var implementation in implementations)
        {
            var handlerServices = implementation
                .GetInterfaces()
                .Where(@interface => @interface.IsGenericType && _handlerInterfaces.Contains(@interface.GetGenericTypeDefinition()));

            foreach (var handlerService in handlerServices)
            {
                services.TryAddEnumerable(ServiceDescriptor.Transient(handlerService, implementation));
            }
        }

        return services;
    }
}
