using System.Collections.Frozen;
using System.Collections.Generic;

namespace DelphiModernizer.Studio.Modules.Traceability.Domain;

/// <summary>A trace rule as people read it: a short label and a sentence about the problem, never the rule's code.</summary>
public sealed record ProblemWording(string Label, string Description);

/// <summary>The words the studio uses for each of the kit's trace rules.</summary>
/// <remarks>
/// TR004, TR005 and TR009 are worded on the design canvas; the others follow them and are listed for the product owner
/// to confirm in docs/open-questions.md. An open question reads as "Still open:" and the question itself.
/// </remarks>
public static class ProblemWordings
{
    public const string StillOpen = "Still open: ";

    private static readonly FrozenDictionary<string, ProblemWording> _byRule = new Dictionary<string, ProblemWording> {
        ["TR001"] = new("Defined twice", "The same ID is defined in two places, so references to it are ambiguous."),
        ["TR002"] = new("Broken reference", "A document refers to something that is defined nowhere."),
        ["TR003"] = new("In the wrong document", "Something is defined in a document that isn’t meant to hold it."),
        ["TR004"] = new("Broken evidence link", "A business rule points to code that doesn’t exist: a missing file, or lines past its end."),
        ["TR005"] = new("Missing evidence", "A business rule has no link to the code it comes from."),
        ["TR006"] = new("Untraced requirement", "A requirement points to no business rule and no code."),
        ["TR007"] = new("Untested requirement", "No test scenario checks this requirement."),
        ["TR008"] = new("Risk without mitigation", "A risk in the register has no mitigation."),
        ["TR009"] = new("Open question", "A question in the project charter is still open."),
        ["TR010"] = new("Open contradiction", "A contradiction between the teams’ drafts is still open."),
        ["TR011"] = new("Decision without a status", "A decision record has no status, or one that isn’t allowed."),
        ["TR012"] = new("Damaged document header", "A deliverable’s header is missing or doesn’t match the plan."),
        ["TR013"] = new("Malformed ID", "Something looks like an ID but isn’t written the way IDs are."),
        ["TR014"] = new("Missing deliverable", "A document the plan needs isn’t in the workspace."),
    }.ToFrozenDictionary();

    public static IReadOnlyCollection<string> Rules => _byRule.Keys;

    /// <summary>What Narrator reads for a problem row, its severity first and its file and line last.</summary>
    /// <param name="severity">The severity as its pill words it, "Error" or "Warning".</param>
    /// <param name="subject">The record the problem is about, or null when there is none to name.</param>
    public static string Announced(
        string severity,
        string description,
        string label,
        string? subject,
        string file,
        int line)
    {
        var about = subject is null ? label : $"{label}, {subject}";
        var where = line > 0 ? $"{file}, line {line}" : file;

        return $"{severity}: {description} {about}. {where}";
    }

    /// <summary>The wording of a rule; a rule a newer kit adds reads as its own code until it gets words here.</summary>
    public static ProblemWording Of(string ruleId)
    {
        return _byRule.TryGetValue(ruleId, out var wording) ? wording : new ProblemWording(ruleId, ruleId);
    }
}
