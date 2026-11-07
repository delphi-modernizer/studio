using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DelphiModernizer.Studio.Tests;
using Xunit;

namespace DelphiModernizer.Studio.Modules.Deliverables.Application.Tests;

public sealed class DeliverablesTests
{
    [Fact]
    public async Task A_new_workspace_lists_every_deliverable_with_its_status_and_the_five_gates()
    {
        using var workspace = await TestWorkspace.CreateAsync();
        var sender = Services.Sender(workspace.Session, services => services.AddDeliverablesApplication());

        var overview = await sender.Send(new GetDeliverablesQuery(), TestContext.Current.CancellationToken);

        Assert.True(overview.IsSuccess, overview.Error.Message);
        Assert.Equal(20, overview.Value.Deliverables.Count);
        Assert.Equal("draft", overview.Value.Deliverables.Single(d => d.Id == "D01").Status);
        Assert.Equal(5, overview.Value.Gates.Count);
    }

    [Fact]
    public async Task A_missing_deliverable_comes_back_from_its_template_and_an_existing_one_is_kept()
    {
        using var workspace = await TestWorkspace.CreateAsync();
        var sender = Services.Sender(workspace.Session, services => services.AddDeliverablesApplication());
        var cancellationToken = TestContext.Current.CancellationToken;
        var missing = (await sender.Send(new GetDeliverablesQuery(), cancellationToken)).Value.Deliverables.Single(d => d.Id == "D14");
        File.Delete(Path.Combine(workspace.Workspace.Root, missing.Path));

        var restored = await sender.Send(new RestoreDeliverableCommand("D14"), cancellationToken);
        var again = await sender.Send(new RestoreDeliverableCommand("D14"), cancellationToken);
        var after = (await sender.Send(new GetDeliverablesQuery(), cancellationToken)).Value.Deliverables.Single(d => d.Id == "D14");

        Assert.True(restored.IsSuccess, restored.Error.Message);
        Assert.Equal("Deliverables.Exists", again.Error.Code);
        Assert.Equal("template", after.Status);
    }
}
