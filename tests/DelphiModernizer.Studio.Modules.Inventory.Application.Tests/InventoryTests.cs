using System.IO;
using System.Threading.Tasks;
using DelphiModernizer.Studio.Application.Abstractions.Workspaces;
using DelphiModernizer.Studio.Tests;
using Xunit;

namespace DelphiModernizer.Studio.Modules.Inventory.Application.Tests;

public sealed class InventoryTests
{
    [Fact]
    public async Task A_scan_writes_the_inventory_the_query_then_reads()
    {
        using var workspace = await TestWorkspace.CreateAsync();
        var sender = Services.Sender(workspace.Session, services => services.AddInventoryApplication());

        var scanned = await sender.Send(new ScanLegacyCommand(), TestContext.Current.CancellationToken);
        var read = await sender.Send(new GetInventoryQuery(), TestContext.Current.CancellationToken);

        Assert.True(scanned.IsSuccess, scanned.Error.Message);
        Assert.True(File.Exists(Path.Combine(workspace.Workspace.LatestScanFolder, "inventory.md")));
        Assert.Equal(scanned.Value.SourceFingerprint, read.Value.SourceFingerprint);
        Assert.Equal(2, read.Value.Summary.Units);
    }

    [Fact]
    public async Task Before_the_first_scan_the_inventory_is_not_there()
    {
        using var workspace = await TestWorkspace.CreateAsync();
        var sender = Services.Sender(workspace.Session, services => services.AddInventoryApplication());

        var read = await sender.Send(new GetInventoryQuery(), TestContext.Current.CancellationToken);

        Assert.Equal(InventoryErrors.NotScanned, read.Error);
    }

    [Fact]
    public async Task Without_a_workspace_nothing_is_scanned()
    {
        var sender = Services.Sender(new WorkspaceSession(), services => services.AddInventoryApplication());

        var scanned = await sender.Send(new ScanLegacyCommand(), TestContext.Current.CancellationToken);

        Assert.Equal(WorkspaceRequired.Error, scanned.Error);
    }
}
