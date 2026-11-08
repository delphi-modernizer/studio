using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DelphiModernizer.Engine.Core;
using DelphiModernizer.Engine.Kit;
using DelphiModernizer.Engine.Workspaces;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;
using DelphiModernizer.Studio.Application.Abstractions.Workspaces;

namespace DelphiModernizer.Studio.Modules.Workstreams.Application;

/// <summary>
/// For each kit recipe, the live run that holds a lease on a deliverable it writes, in the open workspace.
/// </summary>
/// <remarks>
/// The engine's leases only read here; <see cref="StartRunCommand"/> still gets the last word when it takes them, for a
/// run that starts between this check and Run….
/// </remarks>
public sealed record GetRunBlockersQuery : IRequest<Result<IReadOnlyDictionary<string, RunBlocker>>>;

/// <param name="TakenAt">When the run took the lease, as it started.</param>
/// <param name="Run">The studio's own run; null for one it did not start, such as <c>dmz run</c> in a terminal.</param>
public sealed record RunBlocker(string RunId, DateTimeOffset TakenAt, LiveRun? Run);

internal sealed class GetRunBlockersQueryHandler(IWorkspaceSession session, RunMonitor monitor)
    : IRequestHandler<GetRunBlockersQuery, Result<IReadOnlyDictionary<string, RunBlocker>>>
{
    public Task<Result<IReadOnlyDictionary<string, RunBlocker>>> Handle(
        GetRunBlockersQuery request,
        CancellationToken cancellationToken)
    {
        if (session.Current is not { } workspace)
        {
            return Task.FromResult(Result.Failure<IReadOnlyDictionary<string, RunBlocker>>(WorkspaceRequired.Error));
        }

        var blockers = new Dictionary<string, RunBlocker>(StringComparer.Ordinal);

        foreach (var recipe in KitBundle.Embedded.Spec.Recipes.Recipes)
        {
            // An abandoned lease does not block: the next run's Acquire clears it.
            var holder = recipe.Writes
                .Select(d => RunLocks.HolderOf(workspace, d))
                .FirstOrDefault(h => h is { IsAlive: true });

            if (holder is not null)
            {
                blockers[recipe.Id] = new RunBlocker(holder.RunId, holder.TakenAt, monitor.Find(holder.RunId));
            }
        }

        return Task.FromResult(Result.Success<IReadOnlyDictionary<string, RunBlocker>>(blockers));
    }
}
