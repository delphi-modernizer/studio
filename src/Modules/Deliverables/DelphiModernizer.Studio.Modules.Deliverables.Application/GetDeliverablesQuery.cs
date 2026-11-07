using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DelphiModernizer.Engine.Core;
using DelphiModernizer.Engine.Kit;
using DelphiModernizer.Engine.Traceability;
using DelphiModernizer.Engine.Workspaces;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;
using DelphiModernizer.Studio.Application.Abstractions.Workspaces;

namespace DelphiModernizer.Studio.Modules.Deliverables.Application;

/// <summary>The plan's deliverables as their files say they stand, and the sign-offs that need them.</summary>
public sealed record GetDeliverablesQuery : IRequest<Result<DeliverablesOverview>>;

public sealed record DeliverablesOverview(IReadOnlyList<DeliverableState> Deliverables, IReadOnlyList<GateStatus> Gates);

internal sealed class GetDeliverablesQueryHandler(IWorkspaceSession session)
    : IRequestHandler<GetDeliverablesQuery, Result<DeliverablesOverview>>
{
    public Task<Result<DeliverablesOverview>> Handle(GetDeliverablesQuery request, CancellationToken cancellationToken)
    {
        return Task.Run(
            () => session.Current is { } workspace
                ? Result.Success(Gather(workspace))
                : Result.Failure<DeliverablesOverview>(WorkspaceRequired.Error),
            cancellationToken);
    }

    private static DeliverablesOverview Gather(Workspace workspace)
    {
        var trace = new TraceChecker(KitBundle.Embedded.Spec).Check(workspace.Root, strict: false);

        return new DeliverablesOverview(trace.Deliverables, new GateEvaluator(workspace).Evaluate());
    }
}
