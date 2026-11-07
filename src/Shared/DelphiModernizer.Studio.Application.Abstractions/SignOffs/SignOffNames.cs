using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using DelphiModernizer.Engine.Kit;

namespace DelphiModernizer.Studio.Application.Abstractions.SignOffs;

/// <summary>The names people read for the kit's gates; the codes stay in files, approvals and the CLI.</summary>
/// <remarks>The names come from the kit's gates.json, which the engine's approval commits also use.</remarks>
public static class SignOffNames
{
    private static readonly FrozenDictionary<string, string> _byGate = KitBundle.Embedded.Spec.Gates.Gates
        .ToFrozenDictionary(g => g.Id, g => g.Name);

    public static IReadOnlyCollection<string> Gates => _byGate.Keys;

    /// <summary>The name of a gate; a gate the kit does not know reads as its own code.</summary>
    public static string Of(string gate)
    {
        return _byGate.GetValueOrDefault(gate, gate);
    }
}
