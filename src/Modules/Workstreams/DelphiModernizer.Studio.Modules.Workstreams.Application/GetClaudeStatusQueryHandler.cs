using System.Threading;
using System.Threading.Tasks;
using DelphiModernizer.Engine.Agents;
using DelphiModernizer.Studio.Application.Abstractions.Agents;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;
using DelphiModernizer.Studio.Application.Abstractions.Settings;

namespace DelphiModernizer.Studio.Modules.Workstreams.Application;

internal sealed class GetClaudeStatusQueryHandler(IStudioSettings settings) : IRequestHandler<GetClaudeStatusQuery, ClaudeStatus>
{
    public async Task<ClaudeStatus> Handle(GetClaudeStatusQuery request, CancellationToken cancellationToken)
    {
        var located = ClaudeExecutable.Locate(settings.Agents.ClaudePath);

        if (located.IsFailure)
        {
            return new ClaudeStatus(null, null, located.Error.Message);
        }

        var version = await located.Value.VersionAsync(cancellationToken);

        return version.IsSuccess
            ? new ClaudeStatus(located.Value.FileName, version.Value, null)
            : new ClaudeStatus(located.Value.FileName, null, version.Error.Message);
    }
}
