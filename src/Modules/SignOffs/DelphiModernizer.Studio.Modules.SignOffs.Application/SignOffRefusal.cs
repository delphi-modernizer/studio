using System;
using System.Linq;
using DelphiModernizer.Engine.Core;

namespace DelphiModernizer.Studio.Modules.SignOffs.Application;

/// <summary>The words for an approval the engine refused because a check fails when it evaluates them again.</summary>
public static class SignOffRefusal
{
    /// <summary>Whether the engine refused because a check failed when it evaluated them again.</summary>
    /// <remarks>The engine's error code for a gate that is not ready; WorkspaceErrors.GateNotReady writes it.</remarks>
    public static bool IsRefusal(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return error.Code == "Gate.NotReady";
    }

    /// <summary>What fails now, as "1 problem in the documents" or a check's name and detail.</summary>
    public static string Reason(SignOff signOff)
    {
        ArgumentNullException.ThrowIfNull(signOff);
        var failing = signOff.Checks
            .Where(c => !c.Passed)
            .Select(c => c.ShowsProblems
                ? $"{c.Detail.Split(';')[0]} in the documents"
                : $"{c.Name} ({Lowered(c.Detail)})")
            .ToList();

        return failing.Count == 0 ? "a check fails now" : string.Join("; ", failing);
    }

    private static string Lowered(string text)
    {
        return text.Length == 0 ? text : char.ToLowerInvariant(text[0]) + text[1..];
    }
}
