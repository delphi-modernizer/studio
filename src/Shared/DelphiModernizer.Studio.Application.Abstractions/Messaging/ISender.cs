using System.Threading;
using System.Threading.Tasks;

namespace DelphiModernizer.Studio.Application.Abstractions.Messaging;

/// <summary>Sends a request to its single handler through the pipeline behaviors.</summary>
public interface ISender
{
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}
