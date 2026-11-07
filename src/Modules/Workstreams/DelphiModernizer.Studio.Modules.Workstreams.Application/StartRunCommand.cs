using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DelphiModernizer.Engine.Agents;
using DelphiModernizer.Engine.Core;
using DelphiModernizer.Engine.Kit;
using DelphiModernizer.Engine.Kit.Spec;
using DelphiModernizer.Engine.Workspaces;
using DelphiModernizer.Studio.Application.Abstractions;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;
using DelphiModernizer.Studio.Application.Abstractions.Settings;
using DelphiModernizer.Studio.Application.Abstractions.Workspaces;

namespace DelphiModernizer.Studio.Modules.Workstreams.Application;

/// <summary>Starts a kit recipe in the open workspace with the person's Claude Code settings.</summary>
/// <remarks>
/// The run holds a lease on each deliverable the recipe writes until <see cref="AgentRun.Events"/> ends, so the caller
/// must enumerate it to the end; cancelling the enumeration stops the agent and releases the leases.
/// </remarks>
public sealed record StartRunCommand(string RecipeId, string? Argument) : IRequest<Result<AgentRun>>;

public sealed record AgentRun(string RunId, Recipe Recipe, IAsyncEnumerable<AgentEvent> Events);

internal sealed class StartRunCommandHandler(IWorkspaceSession session, IStudioSettings settings, IClock clock)
    : IRequestHandler<StartRunCommand, Result<AgentRun>>
{
    public async Task<Result<AgentRun>> Handle(StartRunCommand request, CancellationToken cancellationToken)
    {
        if (session.Current is not { } workspace)
        {
            return Result.Failure<AgentRun>(WorkspaceRequired.Error);
        }

        var recipe = KitBundle.Embedded.Spec.Recipes.Recipes.FirstOrDefault(r => r.Id == request.RecipeId);

        if (recipe is null)
        {
            return Result.Failure<AgentRun>(WorkstreamsErrors.UnknownRecipe(request.RecipeId));
        }

        var prompt = RecipePrompt.Render(recipe, request.Argument);
        var executable = ClaudeExecutable.Locate(settings.Agents.ClaudePath);

        if (prompt.IsFailure || executable.IsFailure)
        {
            return Result.Failure<AgentRun>(prompt.IsFailure ? prompt.Error : executable.Error);
        }

        var version = await executable.Value.VersionAsync(cancellationToken);

        if (version.IsFailure)
        {
            return Result.Failure<AgentRun>(version.Error);
        }

        var agents = settings.Agents;
        var run = new AgentRunRequest(
            $"{clock.UtcNow:yyyyMMddHHmmss}-{recipe.Id}",
            workspace.Root,
            recipe.Agent,
            prompt.Value,
            $"{Guid.NewGuid()}",
            agents.BudgetUsd,
            settings.ToolDirectory,
            agents.Model,
            agents.Effort);
        var leased = RunLocks.Acquire(workspace, run.RunId, recipe.Writes);

        return leased.IsSuccess
            ? new AgentRun(run.RunId, recipe, Leased(new AgentRunner(executable.Value), run, workspace, recipe.Writes, CancellationToken.None))
            : Result.Failure<AgentRun>(leased.Error);
    }

    private static async IAsyncEnumerable<AgentEvent> Leased(
        AgentRunner runner,
        AgentRunRequest run,
        Workspace workspace,
        IReadOnlyList<string> writes,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var item in runner.RunAsync(run, cancellationToken))
            {
                yield return item;
            }
        }
        finally
        {
            RunLocks.Release(workspace, run.RunId, writes);
        }
    }
}
