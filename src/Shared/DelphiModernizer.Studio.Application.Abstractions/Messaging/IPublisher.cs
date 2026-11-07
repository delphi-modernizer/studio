using System.Threading;
using System.Threading.Tasks;

namespace DelphiModernizer.Studio.Application.Abstractions.Messaging;

/// <summary>Publishes a notification to every handler, one after another.</summary>
public interface IPublisher
{
    Task Publish(INotification notification, CancellationToken cancellationToken = default);
}
