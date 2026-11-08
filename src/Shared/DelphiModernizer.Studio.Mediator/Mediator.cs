using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace DelphiModernizer.Studio.Mediator;

/// <summary>
/// The in-process <see cref="IMediator"/>. It resolves handlers and behaviors from the container on demand and caches
/// one closed-generic dispatch wrapper per message type, so reflection runs once per type.
/// </summary>
/// <remarks>
/// Each request and notification resolves its handlers in a scope of its own: a page's scope ends when the page leaves
/// the back stack, and a request the page started must still finish then.
/// </remarks>
public sealed class Mediator : IMediator
{
    private static readonly ConcurrentDictionary<Type, object> _requestWrappers = new();
    private static readonly ConcurrentDictionary<Type, INotificationHandlerWrapper> _notificationWrappers = new();

    private readonly IServiceScopeFactory _scopes;

    public Mediator(IServiceScopeFactory scopes)
    {
        ArgumentNullException.ThrowIfNull(scopes);
        _scopes = scopes;
    }

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var wrapper = (IRequestHandlerWrapper<TResponse>)_requestWrappers.GetOrAdd(
            request.GetType(),
            requestType => Activator.CreateInstance(
                typeof(RequestHandlerWrapper<,>).MakeGenericType(requestType, typeof(TResponse)))!);

        return SendAsync(wrapper, request, cancellationToken);
    }

    public Task Publish(INotification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var wrapper = _notificationWrappers.GetOrAdd(
            notification.GetType(),
            notificationType => (INotificationHandlerWrapper)Activator.CreateInstance(
                typeof(NotificationHandlerWrapper<>).MakeGenericType(notificationType))!);

        return PublishAsync(wrapper, notification, cancellationToken);
    }

    private async Task<TResponse> SendAsync<TResponse>(
        IRequestHandlerWrapper<TResponse> wrapper,
        object request,
        CancellationToken cancellationToken)
    {
        await using var scope = _scopes.CreateAsyncScope();

        return await wrapper.Handle(request, scope.ServiceProvider, cancellationToken);
    }

    private async Task PublishAsync(
        INotificationHandlerWrapper wrapper,
        INotification notification,
        CancellationToken cancellationToken)
    {
        await using var scope = _scopes.CreateAsyncScope();
        await wrapper.Handle(notification, scope.ServiceProvider, cancellationToken);
    }
}
