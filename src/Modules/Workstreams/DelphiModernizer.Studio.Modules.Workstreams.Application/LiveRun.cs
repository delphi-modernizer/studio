using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DelphiModernizer.Engine.Agents;
using DelphiModernizer.Engine.Kit.Spec;

namespace DelphiModernizer.Studio.Modules.Workstreams.Application;

/// <summary>
/// A run this studio started: what it was asked, every event it has streamed so far, and how it ended.
/// </summary>
public sealed class LiveRun : IDisposable
{
    private readonly Lock _gate = new();
    private readonly List<AgentEvent> _events = [];
    private readonly CancellationTokenSource _stop = new();
    private readonly TaskCompletionSource<RunCompleted> _ended =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private EventHandler<AgentEvent>? _received;

    internal LiveRun(
        string runId,
        string workspaceRoot,
        Recipe recipe,
        string? argument,
        decimal budgetUsd,
        DateTimeOffset startedAt)
    {
        RunId = runId;
        WorkspaceRoot = workspaceRoot;
        Recipe = recipe;
        Argument = argument;
        BudgetUsd = budgetUsd;
        StartedAt = startedAt;
    }

    public string RunId { get; }

    public string WorkspaceRoot { get; }

    public Recipe Recipe { get; }

    public string? Argument { get; }

    public decimal BudgetUsd { get; }

    public DateTimeOffset StartedAt { get; }

    public DateTimeOffset? EndedAt { get; private set; }

    public RunCompleted? Completed { get; private set; }

    public bool IsEnded => Completed is not null;

    /// <summary>
    /// Completes with the run's last event once the engine has closed the run: summary written, leases released.
    /// </summary>
    public Task<RunCompleted> Ended => _ended.Task;

    internal CancellationToken _stopToken => _stop.Token;

    /// <summary>
    /// Returns every event so far and calls <paramref name="handler"/>, on any thread, for each one after them.
    /// </summary>
    /// <remarks>Taken together under one lock, so no event is missed or seen twice.</remarks>
    public IReadOnlyList<AgentEvent> Subscribe(EventHandler<AgentEvent> handler)
    {
        lock (_gate)
        {
            _received += handler;

            return [.. _events];
        }
    }

    public void Unsubscribe(EventHandler<AgentEvent> handler)
    {
        lock (_gate)
        {
            _received -= handler;
        }
    }

    /// <summary>Ends the session at once; what the run already wrote stays as it is.</summary>
    public void Stop()
    {
        if (!IsEnded)
        {
            _stop.Cancel();
        }
    }

    public void Dispose()
    {
        _stop.Dispose();
    }

    internal void Add(AgentEvent item)
    {
        EventHandler<AgentEvent>? received;

        lock (_gate)
        {
            if (IsEnded)
            {
                return;
            }

            _events.Add(item);
            received = _received;

            if (item is RunCompleted completed)
            {
                Completed = completed;
                EndedAt = DateTimeOffset.UtcNow;
            }
        }

        received?.Invoke(this, item);
    }

    /// <summary>Called by the monitor after the engine's stream is closed, which follows the last event.</summary>
    internal void Finish()
    {
        if (Completed is { } completed)
        {
            _ended.TrySetResult(completed);
        }
    }
}
