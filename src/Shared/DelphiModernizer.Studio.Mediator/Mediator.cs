using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;

namespace DelphiModernizer.Studio.Mediator;

/// <summary>
/// The in-process <see cref="IMediator"/>. It resolves handlers and behaviors from the container on demand and caches
/// one closed-generic dispatch wrapper per message type, so reflection runs once per type.
/// </summary>
public sealed class Mediator : IMediator
{
    private static readonly ConcurrentDictionary<Type, object> _requestWrappers = new();
    private static readonly ConcurrentDictionary<Type, INotificationHandlerWrapper> _notificationWrappers = new();

    private readonly IServiceProvider _services;

    public Mediator(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);
        _services = services;
    }

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var wrapper = (IRequestHandlerWrapper<TResponse>)_requestWrappers.GetOrAdd(
            request.GetType(),
            requestType => Activator.CreateInstance(
                typeof(RequestHandlerWrapper<,>).MakeGenericType(requestType, typeof(TResponse)))!);

        return wrapper.Handle(request, _services, cancellationToken);
    }

    public Task Publish(INotification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var wrapper = _notificationWrappers.GetOrAdd(
            notification.GetType(),
            notificationType => (INotificationHandlerWrapper)Activator.CreateInstance(
                typeof(NotificationHandlerWrapper<>).MakeGenericType(notificationType))!);

        return wrapper.Handle(notification, _services, cancellationToken);
    }
}
