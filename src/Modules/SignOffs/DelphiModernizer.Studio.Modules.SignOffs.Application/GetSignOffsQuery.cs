using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DelphiModernizer.Engine.Core;
using DelphiModernizer.Engine.Kit;
using DelphiModernizer.Engine.Kit.Spec;
using DelphiModernizer.Engine.Traceability;
using DelphiModernizer.Engine.Workspaces;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;
using DelphiModernizer.Studio.Application.Abstractions.SignOffs;
using DelphiModernizer.Studio.Application.Abstractions.Workspaces;

namespace DelphiModernizer.Studio.Modules.SignOffs.Application;

/// <summary>The five sign-offs in order, each with its checks in words; the current approval is on its status.</summary>
public sealed record GetSignOffsQuery : IRequest<Result<IReadOnlyList<SignOff>>>;

public sealed record SignOff(GateStatus Status, int Number, string Name, string Approves, IReadOnlyList<SignOffCheck> Checks);

/// <param name="ShowsProblems">Whether the check fails on problems in the documents, so it offers Show the problems.</param>
public sealed record SignOffCheck(string Name, string Detail, bool Passed, bool ShowsProblems);

internal sealed class GetSignOffsQueryHandler(IWorkspaceSession session) : IRequestHandler<GetSignOffsQuery, Result<IReadOnlyList<SignOff>>>
{
    /// <summary>A gate that needs more deliverables than this shows them as one check, as the final plan does.</summary>
    private const int _listedDeliverables = 7;

    public Task<Result<IReadOnlyList<SignOff>>> Handle(GetSignOffsQuery request, CancellationToken cancellationToken)
    {
        return Task.Run(
            () => session.Current is { } workspace
                ? Result.Success(Gather(workspace))
                : Result.Failure<IReadOnlyList<SignOff>>(WorkspaceRequired.Error),
            cancellationToken);
    }

    private static IReadOnlyList<SignOff> Gather(Workspace workspace)
    {
        var spec = KitBundle.Embedded.Spec;
        var checker = new TraceChecker(spec);
        var trace = checker.Check(workspace.Root, strict: false);
        var strict = new Lazy<TraceReport>(() => checker.Check(workspace.Root, strict: true));
        var statuses = trace.Deliverables.ToDictionary(d => d.Id, d => d.Status, StringComparer.Ordinal);
        var gates = new GateEvaluator(workspace).Evaluate();
        var context = new Context(workspace, statuses, trace.Errors, () => strict.Value.Errors);

        return [.. gates.Select((status, index) => new SignOff(
            status,
            index + 1,
            SignOffNames.Of(status.Gate),
            SignOffWordings.Approves(status.Gate, status.Approves),
            Checks(status, index == 0 ? null : gates[index - 1], spec.Gates.Gates.First(g => g.Id == status.Gate), context)))];
    }

    /// <remarks>
    /// The engine lists a gate's checks as the previous gate first when there is one, then the deliverables and the
    /// conditions in the kit's order; the words follow that order, and the pass or fail is always the engine's.
    /// </remarks>
    private static List<SignOffCheck> Checks(GateStatus status, GateStatus? previous, Gate gate, Context context)
    {
        var checks = new List<SignOffCheck>();
        var engine = status.Checks.ToList();
        var offset = 0;

        if (previous is not null)
        {
            var passed = engine[0].Passed;
            checks.Add(new SignOffCheck($"{SignOffNames.Of(previous.Gate)} approved", passed ? "Approved" : $"It is {previous.State}", passed, false));
            offset = 1;
        }

        var required = gate.Requires.Deliverables;
        var deliverableChecks = engine.GetRange(offset, required.Length);

        if (required.Length > _listedDeliverables)
        {
            var met = deliverableChecks.Count(c => c.Passed);
            var word = StatusWord(required[0].MinStatus);
            checks.Add(new SignOffCheck($"All {required.Length} deliverables {word}", $"{met} of {required.Length} {word}", met == required.Length, false));
        }
        else
        {
            checks.AddRange(required.Select((r, i) => Deliverable(r, deliverableChecks[i].Passed, context)));
        }

        checks.AddRange(gate.Requires.Conditions.Select((condition, i) => Condition(condition, engine[offset + required.Length + i], context)));

        return checks;
    }

    private static SignOffCheck Deliverable(DeliverableRequirement requirement, bool passed, Context context)
    {
        var title = KitBundle.Embedded.Spec.Deliverables.Deliverables.FirstOrDefault(d => d.Id == requirement.Id)?.Title ?? requirement.Id;
        var name = $"{SignOffWordings.Deliverable(requirement.Id, title)} {StatusWord(requirement.MinStatus)}";
        var actual = context.Statuses.GetValueOrDefault(requirement.Id, "missing");
        var detail = passed ? Capitalized(StatusWord(actual)) : actual switch {
            "template" => "It is still the empty template",
            "draft" => "It is a draft",
            "missing" => "Its file is missing",
            _ => $"It is {actual}",
        };

        return new SignOffCheck(name, detail, passed, false);
    }

    private static SignOffCheck Condition(string condition, GateCheck check, Context context)
    {
        var target = TargetStackNames.Of(context.Workspace.Manifest.TargetStack);

        return condition switch {
            "legacy-attached" => new(
                "Delphi code added", check.Passed ? "legacy/ holds the Delphi sources" : "legacy/ holds no Delphi sources", check.Passed, false),
            "target-stack-recorded" => new(
                "Target platform recorded", check.Passed ? $"{target}, in the decision log" : "Neither chosen nor left for later", check.Passed, false),
            "target-stack-decided" => new(
                "Target platform decided", check.Passed ? $"{target}, in the decision log" : "Still to decide after discovery", check.Passed, false),
            "scan-present" => new(
                "Delphi code scanned", check.Passed ? "A scan of legacy/ is saved" : "Not scanned yet; scan it from Inventory", check.Passed, false),
            "trace-clean" => new(
                "No problems in the documents", check.Passed ? "None found" : Problems(context.Errors), check.Passed, !check.Passed),
            "trace-strict-clean" => new(
                "No problems, warnings included", check.Passed ? "None found" : $"{Problems(context.StrictErrors())}; for the final plan, warnings count too", check.Passed, !check.Passed),
            _ => new(condition, check.Detail, check.Passed, false),
        };
    }

    private static string StatusWord(string status)
    {
        return status switch {
            "in-review" => "in review",
            "draft" => "drafted",
            _ => status,
        };
    }

    private static string Capitalized(string text)
    {
        return text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text[1..];
    }

    private static string Problems(int count)
    {
        return count == 1 ? "1 problem" : $"{count} problems";
    }

    private sealed record Context(Workspace Workspace, Dictionary<string, string> Statuses, int Errors, Func<int> StrictErrors);
}
