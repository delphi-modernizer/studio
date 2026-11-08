using System;
using System.IO;
using System.Linq;
using DelphiModernizer.Engine.Kit;
using DelphiModernizer.Studio.Application.Abstractions.Deliverables;
using DelphiModernizer.Studio.Application.Abstractions.SignOffs;
using DelphiModernizer.Studio.Application.Abstractions.Workstreams;
using Xunit;

namespace DelphiModernizer.Studio.Application.Abstractions.Tests;

public sealed class NameMappingTests
{
    [Fact]
    public void Every_gate_in_the_kit_has_the_kit_s_name_without_its_code()
    {
        var gates = KitBundle.Embedded.Spec.Gates.Gates;
        var ids = gates
            .Select(g => g.Id)
            .Order();

        Assert.Equal(ids, SignOffNames.Gates.Order());
        Assert.All(gates, gate => Assert.Equal(gate.Name, SignOffNames.Of(gate.Id)));
        Assert.All(gates, gate => Assert.DoesNotContain(gate.Id, SignOffNames.Of(gate.Id), StringComparison.Ordinal));
    }

    [Fact]
    public void Every_deliverable_in_the_kit_has_a_short_name()
    {
        var deliverables = KitBundle.Embedded.Spec.Deliverables.Deliverables;
        var ids = deliverables
            .Select(d => d.Id)
            .Order();

        Assert.Equal(ids, DeliverableNames.Deliverables.Order());
        Assert.Equal("Testing strategy", DeliverableNames.ShortName("D14", "D14 Testing strategy"));
        Assert.Equal("Kit title", DeliverableNames.ShortName("D99", "Kit title"));
    }

    [Fact]
    public void Every_owner_in_the_kit_is_a_role_with_a_name_and_an_agent()
    {
        var spec = KitBundle.Embedded.Spec.Deliverables;
        var roles = WorkstreamRole.All.ToDictionary(r => r.Code);

        Assert.Equal(spec.Owners.Keys.Order(), roles.Keys.Order());
        Assert.All(spec.Deliverables, d => Assert.Contains(d.Owner, roles.Keys));
        Assert.All(WorkstreamRole.All, role => Assert.DoesNotContain(role.Code, role.Name, StringComparison.Ordinal));
    }

    [Fact]
    public void Every_agent_a_role_names_is_in_the_kit()
    {
        var agents = KitBundle.Embedded.WorkspaceFiles
            .Select(f => f.Path)
            .Where(p => p.StartsWith(".claude/agents/", StringComparison.Ordinal))
            .Select(Path.GetFileNameWithoutExtension)
            .ToHashSet();

        Assert.All(WorkstreamRole.All, role => Assert.Contains(role.Agent, agents));
    }
}
