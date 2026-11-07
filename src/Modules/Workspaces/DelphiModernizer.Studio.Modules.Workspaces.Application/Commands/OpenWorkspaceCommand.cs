using System.Threading;
using System.Threading.Tasks;
using DelphiModernizer.Engine.Core;
using DelphiModernizer.Engine.Workspaces;
using DelphiModernizer.Studio.Application.Abstractions;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;
using DelphiModernizer.Studio.Application.Abstractions.Workspaces;
using DelphiModernizer.Studio.Modules.Workspaces.Application.Recent;

namespace DelphiModernizer.Studio.Modules.Workspaces.Application.Commands;

/// <summary>Opens the workspace that contains <paramref name="Folder"/> and makes it the session's.</summary>
public sealed record OpenWorkspaceCommand(string Folder) : IRequest<Result<Workspace>>;

internal sealed class OpenWorkspaceCommandHandler(IWorkspaceSession session, RecentWorkspaceStore recent, IClock clock)
    : IRequestHandler<OpenWorkspaceCommand, Result<Workspace>>
{
    public Task<Result<Workspace>> Handle(OpenWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var found = Workspace.Find(request.Folder);

        if (found.IsSuccess)
        {
            Activate(session, recent, clock, found.Value);
        }

        return Task.FromResult(found);
    }

    internal static void Activate(
        IWorkspaceSession session,
        RecentWorkspaceStore recent,
        IClock clock,
        Workspace workspace)
    {
        session.Open(workspace);
        recent.Remember(new RecentWorkspace(
            workspace.Manifest.Name,
            workspace.Root,
            workspace.Manifest.TargetStack,
            clock.UtcNow));
    }
}
