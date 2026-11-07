using System;
using System.Threading;
using System.Threading.Tasks;
using DelphiModernizer.Engine.Agents;
using DelphiModernizer.Engine.Core;
using DelphiModernizer.Studio.Application.Abstractions.Agents;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;
using DelphiModernizer.Studio.Application.Abstractions.Settings;

namespace DelphiModernizer.Studio.Modules.Workstreams.Application;

internal sealed class GetClaudeStatusQueryHandler(IStudioSettings settings)
    : IRequestHandler<GetClaudeStatusQuery, ClaudeStatus>
{
    private static readonly Version _minimum = ClaudeExecutable.MinimumVersion;

    public async Task<ClaudeStatus> Handle(GetClaudeStatusQuery request, CancellationToken cancellationToken)
    {
        var configured = settings.Agents.ClaudePath;
        var located = ClaudeExecutable.Locate(configured);

        if (located.IsFailure)
        {
            return new ClaudeStatus(StateOf(located.Error), configured, null, located.Error.Message, _minimum);
        }

        var version = await located.Value.VersionAsync(cancellationToken);

        return version.IsSuccess
            ? new ClaudeStatus(ClaudeState.Ready, located.Value.FileName, version.Value, null, _minimum)
            : new ClaudeStatus(StateOf(version.Error), located.Value.FileName, null, version.Error.Message, _minimum);
    }

    private static ClaudeState StateOf(Error error)
    {
        return error.Code switch {
            "Claude.NotFound" => ClaudeState.NotFound,
            "Claude.TooOld" => ClaudeState.TooOld,
            _ => ClaudeState.Unusable,
        };
    }
}
