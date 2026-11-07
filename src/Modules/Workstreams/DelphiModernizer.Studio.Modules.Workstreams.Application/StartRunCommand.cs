using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DelphiModernizer.Engine.Agents;
using DelphiModernizer.Engine.Core;
using DelphiModernizer.Engine.Kit;
using DelphiModernizer.Engine.Workspaces;
using DelphiModernizer.Studio.Application.Abstractions;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;
using DelphiModernizer.Studio.Application.Abstractions.Settings;
using DelphiModernizer.Studio.Application.Abstractions.Workspaces;

namespace DelphiModernizer.Studio.Modules.Workstreams.Application;

/// <summary>Starts a kit recipe in the open workspace with the person's Claude Code settings and this run's budget.</summary>
/// <remarks>
/// The run holds a lease on each deliverable the recipe writes until it ends; <see cref="RunMonitor"/> reads it to the
/// end, and <see cref="LiveRun.Stop"/> stops the agent and releases the leases.
/// </remarks>
public sealed record StartRunCommand(string RecipeId, string? Argument, decimal BudgetUsd) : IRequest<Result<LiveRun>>;

internal sealed class StartRunCommandHandler(
    IWorkspaceSession session,
    IStudioSettings settings,
    IClaudeLocator locator,
    RunMonitor monitor,
    IClock clock)
    : IRequestHandler<StartRunCommand, Result<LiveRun>>
{
    public async Task<Result<LiveRun>> Handle(StartRunCommand request, CancellationToken cancellationToken)
    {
        if (session.Current is not { } workspace)
        {
            return Result.Failure<LiveRun>(WorkspaceRequired.Error);
        }

        var recipe = KitBundle.Embedded.Spec.Recipes.Recipes.FirstOrDefault(r => r.Id == request.RecipeId);

        if (recipe is null)
        {
            return Result.Failure<LiveRun>(WorkstreamsErrors.UnknownRecipe(request.RecipeId));
        }

        if (request.BudgetUsd <= 0)
        {
            return Result.Failure<LiveRun>(WorkstreamsErrors.NoBudget);
        }

        var prompt = RecipePrompt.Render(recipe, request.Argument);
        var executable = locator.Locate(settings.Agents.ClaudePath);

        if (prompt.IsFailure || executable.IsFailure)
        {
            return Result.Failure<LiveRun>(prompt.IsFailure ? prompt.Error : executable.Error);
        }

        var agents = settings.Agents;
        var startedAt = clock.UtcNow;
        var run = new AgentRunRequest(
            $"{startedAt:yyyyMMddHHmmss}-{recipe.Id}",
            workspace.Root,
            recipe.Agent,
            prompt.Value,
            $"{Guid.NewGuid()}",
            request.BudgetUsd,
            settings.ToolDirectory,
            agents.Model,
            agents.Effort);
        var leased = RunLocks.Acquire(workspace, run.RunId, recipe.Writes);

        if (leased.IsFailure)
        {
            return Result.Failure<LiveRun>(leased.Error);
        }

        var tracked = false;

        // The leases come first, so a task another run blocks is refused without waiting for the CLI.
        try
        {
            var version = await executable.Value.VersionAsync(cancellationToken);

            if (version.IsFailure)
            {
                return Result.Failure<LiveRun>(version.Error);
            }

            var argument = string.IsNullOrWhiteSpace(request.Argument) ? null : request.Argument.Trim();
            var events = Leased(new AgentRunner(executable.Value), run, workspace, recipe.Writes, CancellationToken.None);
            tracked = true;

            return monitor.Track(run.RunId, recipe, argument, request.BudgetUsd, startedAt, events);
        }
        finally
        {
            if (!tracked)
            {
                RunLocks.Release(workspace, run.RunId, recipe.Writes);
            }
        }
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
