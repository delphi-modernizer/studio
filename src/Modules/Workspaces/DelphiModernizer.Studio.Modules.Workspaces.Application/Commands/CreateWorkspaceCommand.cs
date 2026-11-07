using System.Threading;
using System.Threading.Tasks;
using DelphiModernizer.Engine.Core;
using DelphiModernizer.Engine.Workspaces;
using DelphiModernizer.Studio.Application.Abstractions;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;
using DelphiModernizer.Studio.Application.Abstractions.Workspaces;
using DelphiModernizer.Studio.Modules.Workspaces.Application.Recent;

namespace DelphiModernizer.Studio.Modules.Workspaces.Application.Commands;

/// <summary>Creates a workspace from the kit the engine embeds, then opens it.</summary>
public sealed record CreateWorkspaceCommand(InitRequest Request) : IRequest<Result<Workspace>>;

internal sealed class CreateWorkspaceCommandHandler(IWorkspaceSession session, RecentWorkspaceStore recent, IClock clock)
    : IRequestHandler<CreateWorkspaceCommand, Result<Workspace>>
{
    public async Task<Result<Workspace>> Handle(CreateWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var created = await WorkspaceInitializer.InitializeAsync(request.Request, cancellationToken);

        if (created.IsSuccess)
        {
            OpenWorkspaceCommandHandler.Activate(session, recent, clock, created.Value);
        }

        return created;
    }
}
