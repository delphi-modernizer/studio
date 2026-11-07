using System.Threading;
using System.Threading.Tasks;
using DelphiModernizer.Engine.Core;
using DelphiModernizer.Engine.Kit;
using DelphiModernizer.Engine.Traceability;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;
using DelphiModernizer.Studio.Application.Abstractions.Workspaces;

namespace DelphiModernizer.Studio.Modules.Traceability.Application;

/// <summary>Checks every ID, reference and evidence link of the open workspace.</summary>
/// <param name="Strict">Whether warnings count as errors, as they do for G4.</param>
public sealed record RunTraceQuery(bool Strict) : IRequest<Result<TraceReport>>;

internal sealed class RunTraceQueryHandler(IWorkspaceSession session) : IRequestHandler<RunTraceQuery, Result<TraceReport>>
{
    public Task<Result<TraceReport>> Handle(RunTraceQuery request, CancellationToken cancellationToken)
    {
        return Task.Run(
            () => session.Current is { } workspace
                ? Result.Success(new TraceChecker(KitBundle.Embedded.Spec).Check(workspace.Root, request.Strict))
                : Result.Failure<TraceReport>(WorkspaceRequired.Error),
            cancellationToken);
    }
}
