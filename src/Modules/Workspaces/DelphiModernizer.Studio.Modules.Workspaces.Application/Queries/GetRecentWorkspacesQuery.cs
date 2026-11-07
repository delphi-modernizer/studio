using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;
using DelphiModernizer.Studio.Modules.Workspaces.Application.Recent;

namespace DelphiModernizer.Studio.Modules.Workspaces.Application.Queries;

public sealed record GetRecentWorkspacesQuery : IRequest<IReadOnlyList<RecentWorkspace>>;

internal sealed class GetRecentWorkspacesQueryHandler(RecentWorkspaceStore store)
    : IRequestHandler<GetRecentWorkspacesQuery, IReadOnlyList<RecentWorkspace>>
{
    public Task<IReadOnlyList<RecentWorkspace>> Handle(GetRecentWorkspacesQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(store.Load());
    }
}
