using System;
using System.Threading;
using System.Threading.Tasks;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace DelphiModernizer.Studio.Mediator;

/// <summary>Dispatches a notification whose type is known only at run time to its statically typed handlers.</summary>
internal interface INotificationHandlerWrapper
{
    Task Handle(INotification notification, IServiceProvider services, CancellationToken cancellationToken);
}

internal sealed class NotificationHandlerWrapper<TNotification> : INotificationHandlerWrapper
    where TNotification : INotification
{
    public async Task Handle(INotification notification, IServiceProvider services, CancellationToken cancellationToken)
    {
        foreach (var handler in services.GetServices<INotificationHandler<TNotification>>())
        {
            await handler.Handle((TNotification)notification, cancellationToken);
        }
    }
}
