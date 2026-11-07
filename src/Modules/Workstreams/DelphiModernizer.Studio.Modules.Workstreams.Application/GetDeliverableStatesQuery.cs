using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DelphiModernizer.Engine.Core;
using DelphiModernizer.Engine.Kit;
using DelphiModernizer.Engine.Traceability;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;
using DelphiModernizer.Studio.Application.Abstractions.Workspaces;

namespace DelphiModernizer.Studio.Modules.Workstreams.Application;

/// <summary>Every deliverable of the open workspace with the status its front matter records, as the trace reads it.</summary>
public sealed record GetDeliverableStatesQuery : IRequest<Result<IReadOnlyList<DeliverableState>>>;

internal sealed class GetDeliverableStatesQueryHandler(IWorkspaceSession session)
    : IRequestHandler<GetDeliverableStatesQuery, Result<IReadOnlyList<DeliverableState>>>
{
    public Task<Result<IReadOnlyList<DeliverableState>>> Handle(GetDeliverableStatesQuery request, CancellationToken cancellationToken)
    {
        if (session.Current is not { } workspace)
        {
            return Task.FromResult(Result.Failure<IReadOnlyList<DeliverableState>>(WorkspaceRequired.Error));
        }

        return Task.Run(
            () => {
                IReadOnlyList<DeliverableState> states = new TraceChecker(KitBundle.Embedded.Spec)
                    .Check(workspace.Root, strict: false)
                    .Deliverables;

                return Result.Success(states);
            },
            cancellationToken);
    }
}
