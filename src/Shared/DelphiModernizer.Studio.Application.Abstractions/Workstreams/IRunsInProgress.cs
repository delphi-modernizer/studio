using System;
using System.Collections.Generic;

namespace DelphiModernizer.Studio.Application.Abstractions.Workstreams;

/// <summary>Which agents have a run in progress, for pages outside Workstreams that say so.</summary>
public interface IRunsInProgress
{
    /// <summary>Raised on any thread when a run starts or ends.</summary>
    event EventHandler? Changed;

    /// <summary>The agents, as <see cref="WorkstreamRole.Agent"/> names them, running in the workspace.</summary>
    IReadOnlySet<string> AgentsRunning(string workspaceRoot);
}
