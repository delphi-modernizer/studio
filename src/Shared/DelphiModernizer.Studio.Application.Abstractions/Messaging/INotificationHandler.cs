using System.Threading;
using System.Threading.Tasks;

namespace DelphiModernizer.Studio.Application.Abstractions.Messaging;

/// <summary>Handles one notification type; a module reacts to another's events through this.</summary>
public interface INotificationHandler<in TNotification>
    where TNotification : INotification
{
    Task Handle(TNotification notification, CancellationToken cancellationToken);
}
