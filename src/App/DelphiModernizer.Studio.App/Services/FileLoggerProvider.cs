using System;
using System.Globalization;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace DelphiModernizer.Studio.App.Services;

/// <summary>Appends log lines to one file per day under the studio's data folder, the record outside a debugger.</summary>
/// <remarks>A failed write is dropped: logging must never take the app down.</remarks>
public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string _directory;
    private readonly Lock _gate = new();

    public FileLoggerProvider(string directory)
    {
        ArgumentException.ThrowIfNullOrEmpty(directory);
        _directory = directory;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(this, categoryName);
    }

    public void Dispose()
    {
        // Each write opens and closes the file, so there is nothing to release.
    }

    private void Write(string line)
    {
        lock (_gate)
        {
            try
            {
                Directory.CreateDirectory(_directory);
                var file = Path.Combine(_directory, $"studio-{DateTime.UtcNow:yyyyMMdd}.log");
                File.AppendAllText(file, line + "\n");
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // Nowhere left to report it.
            }
        }
    }

    private sealed class FileLogger(FileLoggerProvider provider, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= LogLevel.Information;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);

            if (!IsEnabled(logLevel))
            {
                return;
            }

            var time = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            var detail = exception is null ? string.Empty : $"\n{exception}";
            provider.Write($"{time} {logLevel} {category}: {formatter(state, exception)}{detail}");
        }
    }
}
