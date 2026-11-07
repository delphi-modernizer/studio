using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DelphiModernizer.Engine.Core;
using DelphiModernizer.Engine.Kit;
using DelphiModernizer.Engine.Traceability;
using DelphiModernizer.Engine.Workspaces;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;
using DelphiModernizer.Studio.Application.Abstractions.Workspaces;

namespace DelphiModernizer.Studio.Modules.Workstreams.Application;

/// <summary>
/// Every deliverable of the open workspace with the status its front matter records, and where each sign-off stands.
/// </summary>
public sealed record GetDeliverableStatesQuery : IRequest<Result<DeliverableStates>>;

/// <param name="Gates">The sign-offs in the kit's order, as the engine evaluates them.</param>
public sealed record DeliverableStates(IReadOnlyList<DeliverableState> Deliverables, IReadOnlyList<GateStatus> Gates);

internal sealed class GetDeliverableStatesQueryHandler(IWorkspaceSession session)
    : IRequestHandler<GetDeliverableStatesQuery, Result<DeliverableStates>>
{
    public Task<Result<DeliverableStates>> Handle(
        GetDeliverableStatesQuery request,
        CancellationToken cancellationToken)
    {
        if (session.Current is not { } workspace)
        {
            return Task.FromResult(Result.Failure<DeliverableStates>(WorkspaceRequired.Error));
        }

        return Task.Run(
            () => {
                var trace = new TraceChecker(KitBundle.Embedded.Spec).Check(workspace.Root, strict: false);
                var gates = new GateEvaluator(workspace).Evaluate();

                return Result.Success(new DeliverableStates(trace.Deliverables, gates));
            },
            cancellationToken);
    }
}
