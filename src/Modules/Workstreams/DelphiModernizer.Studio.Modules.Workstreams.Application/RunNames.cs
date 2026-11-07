using System.Collections.Generic;
using System.Linq;

namespace DelphiModernizer.Studio.Modules.Workstreams.Application;

/// <summary>What Narrator reads for an agent task row and for each item of a run's transcript.</summary>
public static class RunNames
{
    /// <summary>"Functional requirements, writes the functional requirements", then why it is blocked.</summary>
    public static string Task(string title, string writes, string? blockedReason)
    {
        return blockedReason is null
            ? $"{title}, writes the {writes}"
            : $"{title}, writes the {writes}, Blocked: {blockedReason}";
    }

    /// <summary>Why a task waits: another run in progress writes what it writes.</summary>
    /// <param name="blockerRole">The role that runs the blocking task; null when it is the task's own role.</param>
    public static string BlockedReason(string writes, string blockerTitle, string? blockerRole, string started)
    {
        return blockerRole is null
            ? $"The run below is writing the {writes}, started {started}."
            : $"{blockerTitle} ({blockerRole}) is writing them, started {started}.";
    }

    /// <summary>"Agent: Gold status seems to follow from the credit limit alone. Inferred."</summary>
    public static string Agent(IEnumerable<AgentLine> lines)
    {
        return $"Agent: {string.Join(" ", lines.Select(l => l.Label is null ? l.Text : $"{l.Text} {l.Label}."))}";
    }

    /// <summary>What a tool call says after its input: "Working", "Failed", "Done", "2 matches" or "31 lines".</summary>
    public static string ToolSummary(string tool, string? result, bool isError)
    {
        if (result is null)
        {
            return "Working";
        }

        if (isError)
        {
            return "Failed";
        }

        if (result.Length == 0)
        {
            return "Done";
        }

        var count = result
            .TrimEnd('\n')
            .Split('\n')
            .Length;

        return (tool, count) switch {
            ("Grep", 1) => "1 match",
            ("Grep", _) => $"{count} matches",
            (_, 1) => "1 line",
            _ => $"{count} lines",
        };
    }

    /// <summary>"Tool call Grep, 2 matches, collapsed".</summary>
    public static string Tool(string tool, string summary, bool isExpanded)
    {
        return $"Tool call {tool}, {summary}, {(isExpanded ? "expanded" : "collapsed")}";
    }

    /// <summary>"Refused by the guard: Edit legacy/Sales/Pricing.pas. legacy/ is read-only evidence; …".</summary>
    /// <param name="target">The file the call names, or its input when it names none.</param>
    public static string Refusal(string tool, string target, string reason)
    {
        return $"Refused by the guard: {tool} {target}. {reason}";
    }
}
