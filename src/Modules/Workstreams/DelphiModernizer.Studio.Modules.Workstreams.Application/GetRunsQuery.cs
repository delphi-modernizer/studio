using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DelphiModernizer.Engine.Agents;
using DelphiModernizer.Engine.Core;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;
using DelphiModernizer.Studio.Application.Abstractions.Workspaces;

namespace DelphiModernizer.Studio.Modules.Workstreams.Application;

/// <summary>The open workspace's finished runs, newest first.</summary>
public sealed record GetRunsQuery : IRequest<Result<IReadOnlyList<RunSummary>>>;

internal sealed class GetRunsQueryHandler(IWorkspaceSession session) : IRequestHandler<GetRunsQuery, Result<IReadOnlyList<RunSummary>>>
{
    public Task<Result<IReadOnlyList<RunSummary>>> Handle(GetRunsQuery request, CancellationToken cancellationToken)
    {
        if (session.Current is not { } workspace)
        {
            return Task.FromResult(Result.Failure<IReadOnlyList<RunSummary>>(WorkspaceRequired.Error));
        }

        IReadOnlyList<RunSummary> runs = [.. RunStore
            .List(workspace.Root)
            .OrderByDescending(r => r.StartedAt)];

        return Task.FromResult(Result.Success(runs));
    }
}
