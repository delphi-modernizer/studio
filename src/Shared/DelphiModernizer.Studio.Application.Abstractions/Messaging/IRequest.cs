using System.Diagnostics.CodeAnalysis;

namespace DelphiModernizer.Studio.Application.Abstractions.Messaging;

/// <summary>A message handled by exactly one <see cref="IRequestHandler{TRequest, TResponse}"/>.</summary>
/// <typeparam name="TResponse">What the handler returns.</typeparam>
[SuppressMessage(
    "Major Code Smell",
    "S2326:Unused type parameters should be removed",
    Justification = "The type parameter is the response type ISender.Send infers.")]
public interface IRequest<out TResponse>;
