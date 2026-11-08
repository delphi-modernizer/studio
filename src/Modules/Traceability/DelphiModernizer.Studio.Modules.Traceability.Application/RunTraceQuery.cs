using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DelphiModernizer.Engine.Core;
using DelphiModernizer.Engine.Kit;
using DelphiModernizer.Engine.Traceability;
using DelphiModernizer.Engine.Workspaces;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;
using DelphiModernizer.Studio.Application.Abstractions.Workspaces;
using DelphiModernizer.Studio.Modules.Traceability.Domain;

namespace DelphiModernizer.Studio.Modules.Traceability.Application;

/// <summary>Checks every ID, reference and evidence link of the open workspace.</summary>
/// <param name="Strict">Whether warnings count as errors, as they do for the final plan.</param>
public sealed record RunTraceQuery(bool Strict) : IRequest<Result<TraceabilityReport>>;

/// <param name="HeldUp">Sign-offs not yet approved that one of their trace checks holds back.</param>
/// <param name="Checks">How many trace rules the kit defines, every one run over every document.</param>
public sealed record TraceabilityReport(TraceReport Trace, IReadOnlyList<Problem> Problems, int HeldUp, int Checks);

/// <summary>A trace issue in words people read.</summary>
/// <param name="Description">The sentence about the problem; an open question reads as the question it asks.</param>
/// <param name="Subject">
/// The record it is about, such as <c>business rule BR-003</c>; null when there is none to name.
/// </param>
public sealed record Problem(TraceIssue Issue, ProblemWording Wording, string Description, string? Subject);

internal sealed class RunTraceQueryHandler(IWorkspaceSession session)
    : IRequestHandler<RunTraceQuery, Result<TraceabilityReport>>
{
    private const string _openQuestion = "open-question";

    public Task<Result<TraceabilityReport>> Handle(RunTraceQuery request, CancellationToken cancellationToken)
    {
        return Task.Run(
            () => session.Current is { } workspace
                ? Result.Success(Check(workspace, request.Strict))
                : Result.Failure<TraceabilityReport>(WorkspaceRequired.Error),
            cancellationToken);
    }

    private static TraceabilityReport Check(Workspace workspace, bool strict)
    {
        var spec = KitBundle.Embedded.Spec;
        var trace = new TraceChecker(spec).Check(workspace.Root, strict);
        int heldUp = new GateEvaluator(workspace)
            .Evaluate()
            .Count(g => g.State != "approved"
                && g.Checks.Any(c => !c.Passed && c.Name.StartsWith("trace", StringComparison.Ordinal)));

        return new TraceabilityReport(
            trace,
            [.. trace.Issues.Select(i => Describe(workspace, i))],
            heldUp,
            spec.TraceRules.Rules.Length);
    }

    private static Problem Describe(Workspace workspace, TraceIssue issue)
    {
        var wording = ProblemWordings.Of(issue.RuleId);

        if (issue.Rule == _openQuestion)
        {
            string? question = RegisterRows.Cell(workspace.Root, issue.Path, issue.Line, 0);
            string description = question is { Length: > 0 }
                ? ProblemWordings.StillOpen + char.ToLowerInvariant(question[0]) + question[1..]
                : wording.Description;

            return new Problem(issue, wording, description, null);
        }

        var kind = issue.RecordId is null
            ? null
            : KitBundle.Embedded.Spec.Ids.Kinds.FirstOrDefault(
                k => issue.RecordId.StartsWith(k.Prefix + "-", StringComparison.Ordinal));
        string? subject = kind is null ? issue.RecordId : $"{kind.Name.ToLowerInvariant()} {issue.RecordId}";

        return new Problem(issue, wording, wording.Description, subject);
    }
}
