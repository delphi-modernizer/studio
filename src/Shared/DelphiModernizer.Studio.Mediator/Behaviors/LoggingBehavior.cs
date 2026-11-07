using System;
using System.Threading;
using System.Threading.Tasks;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;
using Microsoft.Extensions.Logging;

namespace DelphiModernizer.Studio.Mediator.Behaviors;

/// <summary>Logs the start, end and failure of every request.</summary>
public sealed partial class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);
        var name = typeof(TRequest).Name;
        LogHandling(_logger, name);

        try
        {
            var response = await next();
            LogHandled(_logger, name);

            return response;
        }
        catch (Exception exception)
        {
            LogFailed(_logger, exception, name);
            throw;
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Handling {RequestName}")]
    private static partial void LogHandling(ILogger logger, string requestName);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Handled {RequestName}")]
    private static partial void LogHandled(ILogger logger, string requestName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to handle {RequestName}")]
    private static partial void LogFailed(ILogger logger, Exception exception, string requestName);
}
