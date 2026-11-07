using System;
using DelphiModernizer.Studio.Application.Abstractions;

namespace DelphiModernizer.Studio.Mediator;

/// <summary>An <see cref="IClock"/> backed by the operating system clock.</summary>
public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
