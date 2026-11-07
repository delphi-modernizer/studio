using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DelphiModernizer.Engine.Core;
using DelphiModernizer.Engine.Workspaces;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;
using DelphiModernizer.Studio.Application.Abstractions.Workspaces;

namespace DelphiModernizer.Studio.Modules.SignOffs.Application;

/// <summary>Where every gate of the open workspace stands now.</summary>
public sealed record GetGatesQuery : IRequest<Result<IReadOnlyList<GateStatus>>>;

internal sealed class GetGatesQueryHandler(IWorkspaceSession session) : IRequestHandler<GetGatesQuery, Result<IReadOnlyList<GateStatus>>>
{
    public Task<Result<IReadOnlyList<GateStatus>>> Handle(GetGatesQuery request, CancellationToken cancellationToken)
    {
        return Task.Run(
            () => session.Current is { } workspace
                ? Result.Success(new GateEvaluator(workspace).Evaluate())
                : Result.Failure<IReadOnlyList<GateStatus>>(WorkspaceRequired.Error),
            cancellationToken);
    }
}
