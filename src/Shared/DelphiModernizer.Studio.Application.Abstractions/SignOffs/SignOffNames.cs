using System.Collections.Frozen;
using System.Collections.Generic;

namespace DelphiModernizer.Studio.Application.Abstractions.SignOffs;

/// <summary>The names people read for the kit's gates; the codes stay in files, approvals and the CLI.</summary>
/// <remarks>The design's words, until the kit's gates.json carries a name per gate (handoff, approval commits).</remarks>
public static class SignOffNames
{
    private static readonly FrozenDictionary<string, string> _byGate = new Dictionary<string, string> {
        ["G0"] = "Project setup",
        ["G1"] = "Discovery",
        ["G2"] = "Requirements",
        ["G3"] = "Architecture",
        ["G4"] = "Final plan",
    }.ToFrozenDictionary();

    public static IReadOnlyCollection<string> Gates => _byGate.Keys;

    /// <summary>The name of a gate; a gate a newer kit adds reads as its own code until it gets a name here.</summary>
    public static string Of(string gate)
    {
        return _byGate.GetValueOrDefault(gate, gate);
    }
}
