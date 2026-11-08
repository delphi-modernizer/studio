using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DelphiModernizer.Engine.Agents;
using DelphiModernizer.Engine.Core;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;
using DelphiModernizer.Studio.Application.Abstractions.Workspaces;

namespace DelphiModernizer.Studio.Modules.Workstreams.Application;

/// <summary>
/// A recorded run of the open workspace read back as the events its live run streamed, so the Run page shows it the
/// same way; standard error's diagnostics are not recorded.
/// </summary>
public sealed record GetRunTranscriptQuery(string RunId) : IRequest<Result<IReadOnlyList<AgentEvent>>>;

internal sealed class GetRunTranscriptQueryHandler(IWorkspaceSession session)
    : IRequestHandler<GetRunTranscriptQuery, Result<IReadOnlyList<AgentEvent>>>
{
    public async Task<Result<IReadOnlyList<AgentEvent>>> Handle(
        GetRunTranscriptQuery request,
        CancellationToken cancellationToken)
    {
        if (session.Current is not { } workspace)
        {
            return Result.Failure<IReadOnlyList<AgentEvent>>(WorkspaceRequired.Error);
        }

        IAsyncEnumerable<AgentEvent> transcript;

        try
        {
            transcript = RunStore.ReadTranscriptAsync(workspace.Root, request.RunId, cancellationToken);
        }
        catch (ArgumentException)
        {
            return Result.Failure<IReadOnlyList<AgentEvent>>(WorkstreamsErrors.UnknownRun(request.RunId));
        }

        var events = new List<AgentEvent>();

        await foreach (var item in transcript)
        {
            events.Add(item);
        }

        return Result.Success<IReadOnlyList<AgentEvent>>(events);
    }
}
