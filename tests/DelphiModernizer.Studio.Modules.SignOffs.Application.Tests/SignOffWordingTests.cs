using System;
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

        Assert.Equal(
            ["Project setup", "Discovery", "Requirements", "Architecture", "Final plan"],
            signOffs.Select(s => s.Name));
        Assert.Contains(signOffs[0].Checks, c => c is { Name: "Project charter drafted", Passed: true });
        Assert.Contains(signOffs[1].Checks, c => c is { Name: "Project setup approved", Passed: false });
        Assert.Contains(signOffs[4].Checks, c => c.Name == "All 20 deliverables in review");
        Assert.All(signOffs, s => Assert.True(s.Status.Checks.Length >= s.Checks.Count));
    }

    [Theory]
    [InlineData("System inventory in review", "In review", true, false, "Passed: System inventory in review.")]
    [InlineData("Discovery approved", "It is blocked", false, false, "Not met: Discovery approved. It is blocked.")]
    [InlineData(
        "No problems in the documents",
        "2 problems",
        false,
        true,
        "Failing: No problems in the documents. 2 problems.")]
    [InlineData(
        "No problems, warnings included",
        "3 problems; for the final plan, warnings count too",
        false,
        true,
        "Failing: No problems, warnings included. 3 problems.")]
    public void A_check_row_reads_its_state_its_name_and_what_is_missing(
        string name, string detail, bool passed, bool showsProblems, string announced)
    {
        var check = new SignOffCheck(name, detail, passed, showsProblems);

        Assert.Equal(announced, check.AnnouncedName);
    }

    [Fact]
    public async Task A_new_workspace_reads_its_discovery_checks_by_state()
    {
        using var workspace = await TestWorkspace.CreateAsync();
        var sender = Services.Sender(workspace.Session, services => services.AddSignOffsApplication());

        var signOffs = (await sender.Send(new GetSignOffsQuery(), TestContext.Current.CancellationToken)).Value;

        var previous = signOffs[1].Checks[0].AnnouncedName;

        Assert.StartsWith("Not met: Project setup approved. It is ", previous, StringComparison.Ordinal);
    }
}
