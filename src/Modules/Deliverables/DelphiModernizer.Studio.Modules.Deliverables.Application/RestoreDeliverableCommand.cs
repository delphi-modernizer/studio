using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DelphiModernizer.Engine.Core;
using DelphiModernizer.Engine.Kit;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;
using DelphiModernizer.Studio.Application.Abstractions.Workspaces;

namespace DelphiModernizer.Studio.Modules.Deliverables.Application;

/// <summary>Writes a missing deliverable back from the kit's template; a file that exists is never replaced.</summary>
public sealed record RestoreDeliverableCommand(string Id) : IRequest<Result>;

internal sealed class RestoreDeliverableCommandHandler(IWorkspaceSession session)
    : IRequestHandler<RestoreDeliverableCommand, Result>
{
    public async Task<Result> Handle(RestoreDeliverableCommand request, CancellationToken cancellationToken)
    {
        if (session.Current is not { } workspace)
        {
            return Result.Failure(WorkspaceRequired.Error);
        }

        var deliverable = KitBundle.Embedded.Spec.Deliverables.Deliverables.FirstOrDefault(d => d.Id == request.Id);
        var template = KitBundle.Embedded.WorkspaceFiles.FirstOrDefault(f => f.Path == deliverable?.File);

        if (deliverable is null || template is null)
        {
            return Result.Failure(new Error("Deliverables.NoTemplate", $"The kit has no template for {request.Id}."));
        }

        var path = Path.Combine(workspace.Root, deliverable.File);

        if (File.Exists(path))
        {
            return Result.Failure(
                new Error("Deliverables.Exists", $"{deliverable.File} exists; restoring would replace it."));
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? throw new InvalidOperationException(path));
        await File.WriteAllBytesAsync(path, template.Content, cancellationToken);

        return Result.Success();
    }
}
