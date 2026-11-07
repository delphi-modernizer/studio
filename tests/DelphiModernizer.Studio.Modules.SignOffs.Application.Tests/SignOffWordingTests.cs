using System.Linq;
using System.Threading.Tasks;
using DelphiModernizer.Engine.Kit;
using DelphiModernizer.Studio.Tests;
using Xunit;

namespace DelphiModernizer.Studio.Modules.SignOffs.Application.Tests;

public sealed class SignOffWordingTests
{
    [Fact]
    public void Every_gate_and_deliverable_in_the_kit_has_words()
    {
        var spec = KitBundle.Embedded.Spec;

        var gates = spec.Gates.Gates.Select(g => g.Id);
        var deliverables = spec.Deliverables.Deliverables.Select(d => d.Id);

        Assert.Equal(gates.Order(), SignOffWordings.Gates.Order());
        Assert.Equal(deliverables.Order(), SignOffWordings.Deliverables.Order());
    }

    [Fact]
    public async Task A_new_workspace_reads_as_five_sign_offs_with_their_checks_in_words()
    {
        using var workspace = await TestWorkspace.CreateAsync();
        var sender = Services.Sender(workspace.Session, services => services.AddSignOffsApplication());

        var signOffs = (await sender.Send(new GetSignOffsQuery(), TestContext.Current.CancellationToken)).Value;

        Assert.Equal(["Project setup", "Discovery", "Requirements", "Architecture", "Final plan"], signOffs.Select(s => s.Name));
        Assert.Contains(signOffs[0].Checks, c => c is { Name: "Project charter drafted", Passed: true });
        Assert.Contains(signOffs[1].Checks, c => c is { Name: "Project setup approved", Passed: false });
        Assert.Contains(signOffs[4].Checks, c => c.Name == "All 20 deliverables in review");
        Assert.All(signOffs, s => Assert.True(s.Status.Checks.Length >= s.Checks.Count));
    }
}
