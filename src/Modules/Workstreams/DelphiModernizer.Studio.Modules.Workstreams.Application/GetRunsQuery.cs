using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DelphiModernizer.Engine.Agents;
using DelphiModernizer.Engine.Core;
using DelphiModernizer.Engine.Kit;
using DelphiModernizer.Engine.Kit.Spec;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;
using DelphiModernizer.Studio.Application.Abstractions.Workspaces;

namespace DelphiModernizer.Studio.Modules.Workstreams.Application;

/// <summary>The open workspace's runs, newest first: those in progress here and those the run store records.</summary>
public sealed record GetRunsQuery : IRequest<Result<IReadOnlyList<RunRecord>>>;

/// <param name="Recipe">The recipe the run's prompt invokes; null for a prompt no kit recipe renders.</param>
/// <param name="Argument">The one-line argument the person gave, or null.</param>
/// <param name="FinishedAt">When it ended; null while it runs.</param>
public sealed record RunRecord(
    string RunId,
    string Agent,
    Recipe? Recipe,
    string? Argument,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt,
    RunOutcome Outcome,
    decimal? CostUsd,
    int? Turns)
{
    public static RunRecord Of(LiveRun run)
    {
        var completed = run.Completed;

        return new RunRecord(
            run.RunId,
            run.Recipe.Agent,
            run.Recipe,
            run.Argument,
            run.StartedAt,
            run.EndedAt,
            RunOutcomes.Of(completed?.Outcome),
            completed?.CostUsd,
            completed?.Turns);
    }

    public static RunRecord Of(RunSummary summary)
    {
        var (recipe, argument) = RecipeOf(summary.Agent, summary.Prompt);

        return new RunRecord(
            summary.RunId,
            summary.Agent,
            recipe,
            argument,
            summary.StartedAt,
            summary.FinishedAt,
            RunOutcomes.Of(summary.Outcome),
            summary.CostUsd,
            summary.Turns);
    }

    /// <summary>
    /// Reads a prompt <see cref="RecipePrompt"/> rendered, <c>/skill</c> or <c>/skill argument</c>, back.
    /// </summary>
    private static (Recipe? Recipe, string? Argument) RecipeOf(string agent, string prompt)
    {
        foreach (var recipe in KitBundle.Embedded.Spec.Recipes.Recipes.Where(r => r.Agent == agent))
        {
            var command = $"/{recipe.Skill}";

            if (prompt == command)
            {
                return (recipe, null);
            }

            if (prompt.StartsWith(command + " ", StringComparison.Ordinal))
            {
                return (recipe, prompt[(command.Length + 1)..]);
            }
        }

        return (null, null);
    }
}

internal sealed class GetRunsQueryHandler(IWorkspaceSession session, RunMonitor monitor)
    : IRequestHandler<GetRunsQuery, Result<IReadOnlyList<RunRecord>>>
{
    public Task<Result<IReadOnlyList<RunRecord>>> Handle(GetRunsQuery request, CancellationToken cancellationToken)
    {
        if (session.Current is not { } workspace)
        {
            return Task.FromResult(Result.Failure<IReadOnlyList<RunRecord>>(WorkspaceRequired.Error));
        }

        var stored = RunStore
            .List(workspace.Root)
            .Select(RunRecord.Of)
            .ToList();
        var recorded = stored
            .Select(r => r.RunId)
            .ToHashSet(StringComparer.Ordinal);
        var live = monitor.Runs
            .Where(r => r.WorkspaceRoot == workspace.Root && !recorded.Contains(r.RunId))
            .Select(RunRecord.Of);
        IReadOnlyList<RunRecord> runs = [.. live
            .Concat(stored)
            .OrderByDescending(r => r.StartedAt)];

        return Task.FromResult(Result.Success(runs));
    }
}
