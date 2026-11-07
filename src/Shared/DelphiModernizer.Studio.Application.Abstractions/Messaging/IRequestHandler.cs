using System.Threading;
using System.Threading.Tasks;

namespace DelphiModernizer.Studio.Application.Abstractions.Messaging;

/// <summary>Handles one request type.</summary>
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}
