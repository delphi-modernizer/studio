using System.Collections.Frozen;
using System.Collections.Generic;
using DelphiModernizer.Studio.Application.Abstractions.Deliverables;

namespace DelphiModernizer.Studio.Modules.SignOffs.Application;

/// <summary>The design's words for what each sign-off approves and for the deliverables its checks name.</summary>
public static class SignOffWordings
{
    private static readonly FrozenDictionary<string, string> _approves = new Dictionary<string, string> {
        ["G0"] = "The scope, the target platform (or the choice to decide later) and the essential context.",
        ["G1"] = "What is known, found and still unknown about the Delphi system.",
        ["G2"] = "The functional and non-functional requirements, each backed by evidence.",
        ["G3"] = "The current and target architecture, the technology choice and the decision records.",
        ["G4"] = "The complete modernization plan. Building the new system may start once it’s approved.",
    }.ToFrozenDictionary();

    public static IReadOnlyCollection<string> Gates => _approves.Keys;

    /// <param name="fallback">The kit's own words, used for a gate a newer kit adds.</param>
    public static string Approves(string gate, string fallback)
    {
        return _approves.GetValueOrDefault(gate, fallback);
    }

    /// <param name="fallback">The kit's title, used for a deliverable a newer kit adds.</param>
    public static string Deliverable(string id, string fallback)
    {
        return DeliverableNames.ShortName(id, fallback);
    }
}
