using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DelphiModernizer.Engine.Delphi;
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
        Assert.Equal(scanned.Value.Inventory.SourceFingerprint, read.Value.Inventory.SourceFingerprint);
        Assert.Equal(2, read.Value.Inventory.Summary.Units);
        Assert.Equal(scanned.Value.ScannedAt, read.Value.ScannedAt);
    }

    [Fact]
    public async Task A_scan_reports_the_files_it_has_read_up_to_all_of_them()
    {
        using var workspace = await TestWorkspace.CreateAsync();
        var sender = Services.Sender(workspace.Session, services => services.AddInventoryApplication());
        var reports = new List<ScanProgress>();

        var scanned = await sender.Send(
            new ScanLegacyCommand(new Reports(reports)),
            TestContext.Current.CancellationToken);

        Assert.True(scanned.IsSuccess, scanned.Error.Message);
        Assert.NotEmpty(reports);
        Assert.Equal(reports[^1].FilesTotal, reports[^1].FilesDone);
        Assert.Equal(scanned.Value.Inventory.Summary.Files, reports[^1].FilesTotal);
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

    /// <summary>Collects reports as they are made, where <see cref="Progress{T}"/> would post them later.</summary>
    private sealed class Reports(List<ScanProgress> reports) : IProgress<ScanProgress>
    {
        public void Report(ScanProgress value)
        {
            reports.Add(value);
        }
    }
}
