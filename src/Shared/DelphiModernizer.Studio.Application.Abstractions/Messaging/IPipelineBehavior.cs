using System.Threading;
using System.Threading.Tasks;

namespace DelphiModernizer.Studio.Application.Abstractions.Messaging;

/// <summary>Wraps the handling of every request, such as logging; behaviors run in registration order.</summary>
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken);
}
