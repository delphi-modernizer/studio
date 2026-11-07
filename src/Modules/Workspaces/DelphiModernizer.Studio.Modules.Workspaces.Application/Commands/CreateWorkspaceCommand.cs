using System;
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
/// <remarks>The copy runs off the calling thread, so a window stays responsive while a large tree is copied.</remarks>
/// <param name="Progress">Told as each creation step starts and ends.</param>
public sealed record CreateWorkspaceCommand(InitRequest Request, IProgress<InitProgress>? Progress)
    : IRequest<Result<Workspace>>;

internal sealed class CreateWorkspaceCommandHandler(IWorkspaceSession session, RecentWorkspaceStore recent, IClock clock)
    : IRequestHandler<CreateWorkspaceCommand, Result<Workspace>>
{
    public async Task<Result<Workspace>> Handle(CreateWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var created = await Task.Run(
            () => WorkspaceInitializer.InitializeAsync(request.Request, request.Progress, cancellationToken),
            cancellationToken);

        if (created.IsSuccess)
        {
            OpenWorkspaceCommandHandler.Activate(session, recent, clock, created.Value);
        }

        return created;
    }
}
