using System.Threading.Tasks;

namespace DelphiModernizer.Studio.Application.Abstractions.Messaging;

/// <summary>The rest of the pipeline after a behavior: the next behavior, or the handler.</summary>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();
