using System;
using System.Threading;
using System.Threading.Tasks;
using DelphiModernizer.Engine.Workspaces;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;

namespace DelphiModernizer.Studio.Modules.Workspaces.Application.Queries;

/// <summary>Who git says the person is, to prefill "Decided by"; the person can change it before Create.</summary>
public sealed record GetDeciderQuery : IRequest<string?>;

internal sealed class GetDeciderQueryHandler : IRequestHandler<GetDeciderQuery, string?>
{
    public async Task<string?> Handle(GetDeciderQuery request, CancellationToken cancellationToken)
    {
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var identity = await GitIdentity.ReadAsync(home, cancellationToken);

        return identity?.ToString();
    }
}
