using System.Threading;
using System.Threading.Tasks;
using DelphiModernizer.Engine.Core;
using DelphiModernizer.Engine.Workspaces;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;
using DelphiModernizer.Studio.Application.Abstractions.Workspaces;

namespace DelphiModernizer.Studio.Modules.SignOffs.Application;

/// <summary>Records the person's approval of a gate under their git identity and commits it.</summary>
/// <remarks>
/// Security decision: only the UI sends this, after the person confirms; agents never reach the studio.
/// </remarks>
public sealed record ApproveGateCommand(string Gate, string Note) : IRequest<Result<ApprovalEntry>>;

/// <summary>Withdraws the person's approval of a gate; the history keeps both entries.</summary>
public sealed record RevokeGateCommand(string Gate, string Note) : IRequest<Result<ApprovalEntry>>;

internal sealed class ApproveGateCommandHandler(IWorkspaceSession session)
    : IRequestHandler<ApproveGateCommand, Result<ApprovalEntry>>,
        IRequestHandler<RevokeGateCommand, Result<ApprovalEntry>>
{
    public Task<Result<ApprovalEntry>> Handle(ApproveGateCommand request, CancellationToken cancellationToken)
    {
        return session.Current is { } workspace
            ? ApprovalService.ApproveAsync(
                workspace, request.Gate, by: null, request.Note, commit: true, cancellationToken)
            : Task.FromResult(Result.Failure<ApprovalEntry>(WorkspaceRequired.Error));
    }

    public Task<Result<ApprovalEntry>> Handle(RevokeGateCommand request, CancellationToken cancellationToken)
    {
        return session.Current is { } workspace
            ? ApprovalService.RevokeAsync(
                workspace, request.Gate, by: null, request.Note, commit: true, cancellationToken)
            : Task.FromResult(Result.Failure<ApprovalEntry>(WorkspaceRequired.Error));
    }
}
