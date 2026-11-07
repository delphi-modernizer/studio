using System.Collections.Generic;

namespace DelphiModernizer.Studio.Application.Abstractions.Workstreams;

/// <summary>One role in the governance model: the Principal Architect or a workstream, and the kit agent that fills it.</summary>
/// <param name="Code">The owner code deliverables.json uses: <c>PA</c> or <c>WS01</c> to <c>WS06</c>.</param>
/// <param name="Agent">The agent file name under the workspace's .claude/agents/.</param>
public sealed record WorkstreamRole(string Code, string Name, string Agent, string Purpose)
{
    /// <summary>The seven roles of the kit's methodology, the Principal Architect first.</summary>
    public static IReadOnlyList<WorkstreamRole> All { get; } = [
        new("PA", "Principal Architect", "principal-architect", "Consolidates every workstream, detects contradictions and keeps one architectural baseline."),
        new("WS01", "Legacy discovery", "ws01-legacy-discovery", "Establishes what the legacy system is and does, from its source, with evidence."),
        new("WS02", "Requirements", "ws02-requirements", "Turns business rules into functional and non-functional requirements."),
        new("WS03", "Architecture", "ws03-architecture", "Assesses the current architecture, designs the target and records the decisions."),
        new("WS04", "Data and integration", "ws04-data-integration", "Plans the database migration and every integration the system has."),
        new("WS05", "Migration planning", "ws05-migration-planning", "Breaks the migration into increments, a roadmap, estimates and the cutover."),
        new("WS06", "Quality and security", "ws06-quality-security", "Sets the testing strategy and assesses compliance and security."),
    ];
}
