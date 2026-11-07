using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace DelphiModernizer.Studio.Mediator;

/// <summary>Dispatches a request whose type is known only at run time to its statically typed handler.</summary>
internal interface IRequestHandlerWrapper<TResponse>
{
    Task<TResponse> Handle(object request, IServiceProvider services, CancellationToken cancellationToken);
}

internal sealed class RequestHandlerWrapper<TRequest, TResponse> : IRequestHandlerWrapper<TResponse>
    where TRequest : IRequest<TResponse>
{
    public Task<TResponse> Handle(object request, IServiceProvider services, CancellationToken cancellationToken)
    {
        var typedRequest = (TRequest)request;
        RequestHandlerDelegate<TResponse> next = () => services
            .GetRequiredService<IRequestHandler<TRequest, TResponse>>()
            .Handle(typedRequest, cancellationToken);
        var behaviors = services
            .GetServices<IPipelineBehavior<TRequest, TResponse>>()
            .Reverse();

        foreach (var behavior in behaviors)
        {
            var inner = next;
            next = () => behavior.Handle(typedRequest, inner, cancellationToken);
        }

        return next();
    }
}
