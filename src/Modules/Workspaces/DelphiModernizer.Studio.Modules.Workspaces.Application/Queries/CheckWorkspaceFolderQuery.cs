using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DelphiModernizer.Engine.Core;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;

namespace DelphiModernizer.Studio.Modules.Workspaces.Application.Queries;

/// <summary>Whether a workspace can be created in a folder: one that does not exist yet, or an empty one.</summary>
public sealed record CheckWorkspaceFolderQuery(string Folder) : IRequest<Result>;

internal sealed class CheckWorkspaceFolderQueryHandler : IRequestHandler<CheckWorkspaceFolderQuery, Result>
{
    public Task<Result> Handle(CheckWorkspaceFolderQuery request, CancellationToken cancellationToken)
    {
        var usable = !Directory.Exists(request.Folder) || !Directory
            .EnumerateFileSystemEntries(request.Folder)
            .Any();

        return Task.FromResult(usable ? Result.Success() : Result.Failure(WorkspacesErrors.FolderNotEmpty(request.Folder)));
    }
}
