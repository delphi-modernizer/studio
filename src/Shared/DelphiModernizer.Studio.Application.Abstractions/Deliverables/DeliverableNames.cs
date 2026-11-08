using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;

namespace DelphiModernizer.Studio.Application.Abstractions.Deliverables;

/// <summary>The short names the studio gives the kit's deliverables, such as "Testing strategy".</summary>
/// <remarks>
/// These are studio wording, not kit data: the kit's deliverables.json has only the full titles. D01 to D09 are named
/// on the design canvas; the rest follow them (docs/open-questions.md).
/// </remarks>
public static class DeliverableNames
{
    private static readonly FrozenDictionary<string, string> _byId = new Dictionary<string, string> {
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

    public static IReadOnlyCollection<string> Deliverables => _byId.Keys;

    /// <param name="fallback">The kit's title, used for a deliverable a newer kit adds.</param>
    public static string ShortName(string id, string fallback)
    {
        return _byId.GetValueOrDefault(id, fallback);
    }

    /// <summary>The short name inside a sentence: "testing strategy", but "DevOps and deployment" as it is.</summary>
    /// <param name="fallback">The kit's title, used for a deliverable a newer kit adds.</param>
    public static string InSentence(string id, string fallback)
    {
        string name = ShortName(id, fallback);
        string firstWord = name.Split(' ')[0];

        return firstWord.Length == 0 || firstWord[1..].Any(char.IsUpper)
            ? name
            : char.ToLowerInvariant(name[0]) + name[1..];
    }
}
