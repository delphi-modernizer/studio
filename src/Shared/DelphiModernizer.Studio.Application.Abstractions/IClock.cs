using System;

namespace DelphiModernizer.Studio.Application.Abstractions;

/// <summary>The current time, injected so handlers can be tested at a fixed instant.</summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
