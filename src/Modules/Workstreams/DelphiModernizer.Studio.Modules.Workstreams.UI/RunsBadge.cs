using System;
using DelphiModernizer.Studio.Modules.Workstreams.Application;
using DelphiModernizer.Studio.UI.Contracts;

namespace DelphiModernizer.Studio.Modules.Workstreams.UI;

/// <summary>The Workstreams item's badge: the agent runs in progress.</summary>
public sealed class RunsBadge : IBadgeSource
{
    private readonly RunMonitor _monitor;

    public RunsBadge(RunMonitor monitor)
    {
        ArgumentNullException.ThrowIfNull(monitor);
        _monitor = monitor;
        _monitor.Changed += OnRunsChanged;
    }

    public event EventHandler? Changed;

    public BadgeTone Tone => BadgeTone.Attention;

    public int Count => _monitor.InProgress;

    public string Describe(int count)
    {
        return count == 1 ? "1 run in progress" : $"{count} runs in progress";
    }

    private void OnRunsChanged(object? sender, EventArgs e)
    {
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
