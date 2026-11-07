using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DelphiModernizer.Studio.Application.Abstractions.Workspaces;
using DelphiModernizer.Studio.Tests;
using Xunit;

namespace DelphiModernizer.Studio.Modules.Overview.Application.Tests;

public sealed class OverviewTests
{
    [Fact]
    public async Task The_overview_of_a_new_workspace_shows_five_gates_and_seven_roles()
    {
        using var workspace = await TestWorkspace.CreateAsync();
        var sender = Services.Sender(workspace.Session, services => services.AddOverviewApplication());

        var overview = await sender.Send(new GetOverviewQuery(), TestContext.Current.CancellationToken);

        Assert.True(overview.IsSuccess, overview.Error.Message);
        Assert.Equal(["G0", "G1", "G2", "G3", "G4"], overview.Value.Gates.Select(g => g.Gate));
        Assert.Equal(7, overview.Value.Workstreams.Count);
        Assert.Equal(["D01", "D16"], overview.Value.Workstreams[0].Deliverables);
        Assert.Null(overview.Value.Inventory);
        Assert.Equal("dotnet", overview.Value.TargetStack);
    }

    [Fact]
    public async Task An_open_question_reads_as_the_question_its_register_asks()
    {
        using var workspace = await TestWorkspace.CreateAsync();
        var register = Path.Combine(workspace.Workspace.Root, "docs", "00-project-charter", "open-questions.md");
        var row = "| Q-001 | Which regulations apply to invoices? | Tax rules shape the invoice module. "
            + "| Test Person | Open |  |\n";
        await File.AppendAllTextAsync(register, row, TestContext.Current.CancellationToken);
        var sender = Services.Sender(workspace.Session, services => services.AddOverviewApplication());

        var overview = await sender.Send(new GetOverviewQuery(), TestContext.Current.CancellationToken);

        var question = Assert.Single(overview.Value.Questions);
        Assert.Equal("Q-001", question.Id);
        Assert.Equal("Which regulations apply to invoices?", question.Text);
    }

    [Fact]
    public async Task Without_a_workspace_there_is_no_overview()
    {
        var sender = Services.Sender(new WorkspaceSession(), services => services.AddOverviewApplication());

        var overview = await sender.Send(new GetOverviewQuery(), TestContext.Current.CancellationToken);

        Assert.Equal(WorkspaceRequired.Error, overview.Error);
    }
}
