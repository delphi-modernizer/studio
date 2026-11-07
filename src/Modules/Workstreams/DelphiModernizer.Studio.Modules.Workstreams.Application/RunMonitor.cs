using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DelphiModernizer.Engine.Agents;
using DelphiModernizer.Engine.Kit.Spec;

namespace DelphiModernizer.Studio.Modules.Workstreams.Application;

/// <summary>The runs this studio started since it opened, newest first, each read to its end in the background.</summary>
public sealed class RunMonitor : IDisposable
{
    private readonly Lock _gate = new();
    private readonly List<LiveRun> _runs = [];

    /// <summary>Raised on any thread when a run starts or ends.</summary>
    public event EventHandler? Changed;

    public IReadOnlyList<LiveRun> Runs
    {
        get {
            lock (_gate)
            {
                return [.. _runs];
            }
        }
    }

    public int InProgress => Runs.Count(r => !r.IsEnded);

    public LiveRun? Find(string runId)
    {
        return Runs.FirstOrDefault(r => r.RunId == runId);
    }

    /// <summary>The run in progress in the workspace at <paramref name="workspaceRoot"/> that writes any of <paramref name="deliverables"/>.</summary>
    public LiveRun? Writing(string workspaceRoot, IEnumerable<string> deliverables)
    {
        var wanted = deliverables.ToHashSet(StringComparer.Ordinal);

        return Runs.FirstOrDefault(r => !r.IsEnded && r.WorkspaceRoot == workspaceRoot && r.Recipe.Writes.Any(wanted.Contains));
    }

    public void Dispose()
    {
        foreach (var run in Runs)
        {
            run.Dispose();
        }
    }

    /// <param name="events">The engine's stream; it is enumerated to its end, or until the run is stopped.</param>
    internal LiveRun Track(string runId, string workspaceRoot, Recipe recipe, string? argument, decimal budgetUsd, DateTimeOffset startedAt, IAsyncEnumerable<AgentEvent> events)
    {
        var run = new LiveRun(runId, workspaceRoot, recipe, argument, budgetUsd, startedAt);

        lock (_gate)
        {
            _runs.Insert(0, run);
        }

        Changed?.Invoke(this, EventArgs.Empty);
        _ = Task.Run(() => PumpAsync(run, events), CancellationToken.None);

        return run;
    }

    private async Task PumpAsync(LiveRun run, IAsyncEnumerable<AgentEvent> events)
    {
        try
        {
            await foreach (var item in events.WithCancellation(run._stopToken))
            {
                run.Add(item);
            }
        }
        catch (OperationCanceledException)
        {
            run.Add(new RunCompleted("cancelled", true, "You stopped the run.", null, null, null, 0));
        }
        catch (Exception exception) when (exception is IOException or Win32Exception or InvalidOperationException)
        {
            run.Add(new RunCompleted("failed", true, exception.Message, null, null, null, 0));
        }

        if (!run.IsEnded)
        {
            run.Add(new RunCompleted("failed", true, "The run ended without a result.", null, null, null, 0));
        }

        run.Finish();
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
