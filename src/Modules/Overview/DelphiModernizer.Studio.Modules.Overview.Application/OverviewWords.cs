using System.Collections.Frozen;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DelphiModernizer.Engine.Delphi.Inventory;
using DelphiModernizer.Studio.Application.Abstractions.Workstreams;

namespace DelphiModernizer.Studio.Modules.Overview.Application;

/// <summary>The Delphi code node: what the latest scan found, or how to get a scan.</summary>
public sealed record DelphiCodeWords(string Subtitle, string First, string Second, string Third, string AnnouncedName);

/// <summary>The words the diagram's nodes show and Narrator reads, built from the overview's counts.</summary>
/// <remarks>What each team drafts is worded on the design canvas, in the handoff's Narrator names.</remarks>
public static class OverviewWords
{
    private static readonly FrozenDictionary<string, string> _drafts = new Dictionary<string, string> {
        ["WS01"] = "the system inventory and the technical debt and risk assessment",
        ["WS02"] = "the functional and non-functional requirements and the definition of done",
        ["WS03"] = "the current and target architecture, the technology evaluation and the decision records",
        ["WS04"] = "the data migration and integration strategies",
        ["WS05"] = "the migration plan, deployment, roadmap, estimates and cutover",
        ["WS06"] = "the security, compliance and testing strategies",
    }.ToFrozenDictionary();

    private const string _combines = "Combines the teams’ drafts.";

    public static IReadOnlyCollection<string> Teams => _drafts.Keys;

    /// <summary>What a team drafts, or what the Principal Architect does with the drafts.</summary>
    private static string Does(WorkstreamRole role)
    {
        return _drafts.TryGetValue(role.Code, out string? drafts) ? $"Drafts {drafts}." : _combines;
    }

    /// <param name="approved">Whether every sign-off is approved, so every deliverable counts as approved.</param>
    public static string Progress(WorkstreamProgress? progress, bool approved)
    {
        int total = progress?.Deliverables.Count ?? 0;

        return approved ? $"{total} of {total} approved" : $"{progress?.Started ?? 0} of {total} started";
    }

    /// <summary>"Legacy discovery team, 2 of 2 deliverables started. Drafts …. Opens Workstreams."</summary>
    /// <param name="running">Whether the role's agent has a run in progress, which adds ", agent running".</param>
    public static string TeamName(WorkstreamRole role, WorkstreamProgress? progress, bool approved, bool running)
    {
        string name = _drafts.ContainsKey(role.Code) ? $"{role.Name} team" : role.Name;
        int total = progress?.Deliverables.Count ?? 0;
        string counted = approved
            ? $"{total} of {total} deliverables approved"
            : $"{progress?.Started ?? 0} of {total} deliverables started";

        string agent = running ? ", agent running" : string.Empty;

        return $"{name}, {counted}{agent}. {Does(role)} Opens Workstreams.";
    }

    public static string TeamToolTip(WorkstreamRole role)
    {
        string does = _drafts.TryGetValue(role.Code, out string? drafts) ? $"This team drafts {drafts}." : _combines;

        return $"Opens {role.Name} in Workstreams. {does}";
    }

    /// <remarks>Before a scan the subtitle is empty: the version is not known yet, and the first fact says why.</remarks>
    /// <param name="inventory">The latest scan's summary, or null before the first scan.</param>
    /// <param name="cycles">How many circular dependencies the scan found.</param>
    public static DelphiCodeWords DelphiCode(InventorySummary? inventory, int cycles)
    {
        if (inventory is null)
        {
            return new DelphiCodeWords(
                string.Empty,
                "Not scanned yet",
                "Scan it from Inventory",
                string.Empty,
                "Delphi code, not scanned yet. Opens Inventory.");
        }

        string version = inventory.DelphiVersions.FirstOrDefault() ?? "Delphi version unknown";
        string screens = Count(inventory.Forms - inventory.DataModules, "screen");
        string first = Count(inventory.Units, "code module");
        string second = $"{screens}, {Count(inventory.DataModules, "data module")}";
        string third = cycles == 0
            ? "No circular dependencies"
            : Count(cycles, "circular dependency", "circular dependencies");
        string withData = inventory.DataModules switch {
            0 => screens,
            1 => $"{screens} and a data module",
            _ => $"{screens} and {Count(inventory.DataModules, "data module")}",
        };
        string announced = $"Delphi code, {version}: {Count(inventory.Files, "file")}, {first}, {withData}, {third}. "
            + "Opens Inventory.";

        return new DelphiCodeWords(version, first, second, third, announced);
    }

    private static string Count(int value, string singular, string? plural = null)
    {
        string number = value.ToString("N0", CultureInfo.CurrentCulture);

        return value == 1 ? $"{number} {singular}" : $"{number} {plural ?? singular + "s"}";
    }
}
