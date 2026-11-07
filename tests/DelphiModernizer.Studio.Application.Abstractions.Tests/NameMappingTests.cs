using System;
using System.IO;
using System.Linq;
using DelphiModernizer.Engine.Kit;
using DelphiModernizer.Studio.Application.Abstractions.SignOffs;
using DelphiModernizer.Studio.Application.Abstractions.Workstreams;
using Xunit;

namespace DelphiModernizer.Studio.Application.Abstractions.Tests;

public sealed class NameMappingTests
{
    [Fact]
    public void Every_gate_in_the_kit_has_a_name_without_its_code()
    {
        var gates = KitBundle.Embedded.Spec.Gates.Gates.Select(g => g.Id);

        Assert.Equal(gates.Order(), SignOffNames.Gates.Order());
        Assert.All(gates, gate => Assert.DoesNotContain(gate, SignOffNames.Of(gate), StringComparison.Ordinal));
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
