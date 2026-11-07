using System.Collections.Frozen;
using System.Collections.Generic;

namespace DelphiModernizer.Studio.Modules.SignOffs.Application;

/// <summary>The design's words for what each sign-off approves and for the deliverables its checks name.</summary>
/// <remarks>
/// Deliverables D01 to D09 are named on the design canvas; the rest follow them (docs/open-questions.md).
/// </remarks>
public static class SignOffWordings
{
    private static readonly FrozenDictionary<string, string> _approves = new Dictionary<string, string> {
        ["G0"] = "The scope, the target platform (or the choice to decide later) and the essential context.",
        ["G1"] = "What is known, found and still unknown about the Delphi system.",
        ["G2"] = "The functional and non-functional requirements, each backed by evidence.",
        ["G3"] = "The current and target architecture, the technology choice and the decision records.",
        ["G4"] = "The complete modernization plan. Building the new system may start once it’s approved.",
    }.ToFrozenDictionary();

    private static readonly FrozenDictionary<string, string> _deliverables = new Dictionary<string, string> {
        ["D01"] = "Project charter",
        ["D02"] = "System inventory",
        ["D03"] = "Functional requirements",
        ["D04"] = "Non-functional requirements",
        ["D05"] = "Current architecture",
        ["D06"] = "Technical debt and risk",
        ["D07"] = "Target architecture",
        ["D08"] = "Technology evaluation",
        ["D09"] = "Architecture decisions",
        ["D10"] = "Data migration strategy",
        ["D11"] = "Integration strategy",
        ["D12"] = "Compliance and security",
        ["D13"] = "Migration strategy",
        ["D14"] = "Testing strategy",
        ["D15"] = "DevOps and deployment",
        ["D16"] = "Risk register",
        ["D17"] = "Roadmap",
        ["D18"] = "Estimates",
        ["D19"] = "Cutover and rollback",
        ["D20"] = "Definition of done",
    }.ToFrozenDictionary();

    public static IReadOnlyCollection<string> Gates => _approves.Keys;

    public static IReadOnlyCollection<string> Deliverables => _deliverables.Keys;

    /// <param name="fallback">The kit's own words, used for a gate a newer kit adds.</param>
    public static string Approves(string gate, string fallback)
    {
        return _approves.GetValueOrDefault(gate, fallback);
    }

    /// <param name="fallback">The kit's title, used for a deliverable a newer kit adds.</param>
    public static string Deliverable(string id, string fallback)
    {
        return _deliverables.GetValueOrDefault(id, fallback);
    }
}
