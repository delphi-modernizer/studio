using System.Threading;
using System.Threading.Tasks;
using DelphiModernizer.Engine.Workspaces;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;
using DelphiModernizer.Studio.Application.Abstractions.Workspaces;

namespace DelphiModernizer.Studio.Modules.SignOffs.Application;

/// <summary>
/// Who git says approves in the open workspace; null when user.name is unset or no workspace is open.
/// </summary>
/// <remarks>
/// Security decision: the dialogs only show this; the engine reads the identity again when it records.
/// </remarks>
public sealed record GetApproverQuery : IRequest<GitIdentity?>;

internal sealed class GetApproverQueryHandler(IWorkspaceSession session)
    : IRequestHandler<GetApproverQuery, GitIdentity?>
{
    public async Task<GitIdentity?> Handle(GetApproverQuery request, CancellationToken cancellationToken)
    {
        return session.Current is { } workspace
            ? await GitIdentity.ReadAsync(workspace.Root, cancellationToken)
            : null;
    }
}
