using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DelphiModernizer.Engine.Delphi;
using DelphiModernizer.Engine.Delphi.Inventory;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;
using DelphiModernizer.Studio.Tests;
using Xunit;

namespace DelphiModernizer.Studio.Modules.SignOffs.Application.Tests;

public sealed class ApprovalTests
{
    [Fact]
    public async Task The_approver_is_who_git_says_in_the_workspace()
    {
        using var workspace = await TestWorkspace.CreateAsync();
        await GitAsync(workspace, "init");
        await GitAsync(workspace, "config", "user.name", "Dana Reviewer");
        await GitAsync(workspace, "config", "user.email", "dana@example.com");
        var sender = Services.Sender(workspace.Session, services => services.AddSignOffsApplication());

        var approver = await sender.Send(new GetApproverQuery(), TestContext.Current.CancellationToken);

        Assert.Equal("Dana Reviewer", approver?.Name);
        Assert.Equal("dana@example.com", approver?.Email);
    }

    [Fact]
    public async Task Without_a_git_name_there_is_no_approver()
    {
        using var workspace = await TestWorkspace.CreateAsync();
        await GitAsync(workspace, "init");
        await GitAsync(workspace, "config", "user.name", string.Empty);
        var sender = Services.Sender(workspace.Session, services => services.AddSignOffsApplication());

        var approver = await sender.Send(new GetApproverQuery(), TestContext.Current.CancellationToken);

        Assert.Null(approver);
    }

    [Fact]
    public async Task Approving_records_the_git_identity_and_the_note_and_commits_it()
    {
        using var workspace = await ApprovableAsync();
        var sender = Services.Sender(workspace.Session, services => services.AddSignOffsApplication());

        var approved = await sender.Send(new ApproveGateCommand("G0", "Read the charter."), TestContext.Current.CancellationToken);
        var log = await GitAsync(workspace, "log", "--format=%s", "--", ".modernizer/approvals.json");

        Assert.Equal("Dana Reviewer <dana@example.com>", approved.Value.By);
        Assert.Equal("Read the charter.", approved.Value.Note);
        Assert.NotEmpty(log);
    }

    [Fact]
    public async Task A_refusal_names_what_fails_in_words()
    {
        using var workspace = await ApprovableAsync();
        var sender = Services.Sender(workspace.Session, services => services.AddSignOffsApplication());

        var approved = await sender.Send(new ApproveGateCommand("G1", "Looks fine."), TestContext.Current.CancellationToken);
        var signOffs = await sender.Send(new GetSignOffsQuery(), TestContext.Current.CancellationToken);

        Assert.True(SignOffRefusal.IsRefusal(approved.Error));
        Assert.StartsWith("Project setup approved (it is ready)", SignOffRefusal.Reason(signOffs.Value[1]));
    }

    [Fact]
    public async Task Revoking_keeps_later_approvals_and_blocks_approving_them_again()
    {
        using var workspace = await ApprovableAsync();
        await MakeDiscoveryReadyAsync(workspace);
        var sender = Services.Sender(workspace.Session, services => services.AddSignOffsApplication());
        var cancellationToken = TestContext.Current.CancellationToken;
        Assert.True((await sender.Send(new ApproveGateCommand("G0", "Read the charter."), cancellationToken)).IsSuccess);
        var discovery = await sender.Send(new ApproveGateCommand("G1", "Read the inventory."), cancellationToken);
        Assert.True(discovery.IsSuccess, discovery.IsFailure ? discovery.Error.Message : null);

        await sender.Send(new RevokeGateCommand("G0", "The scope changed."), cancellationToken);
        var gates = await sender.Send(new GetGatesQuery(), cancellationToken);
        var again = await sender.Send(new ApproveGateCommand("G1", "Read it again."), cancellationToken);

        var states = gates.Value
            .Take(2)
            .Select(g => g.State);

        Assert.Equal(["ready", "approved"], states);
        Assert.True(SignOffRefusal.IsRefusal(again.Error));
    }

    private static async Task<TestWorkspace> ApprovableAsync()
    {
        var workspace = await TestWorkspace.CreateAsync();
        await GitAsync(workspace, "init");
        await GitAsync(workspace, "config", "user.name", "Dana Reviewer");
        await GitAsync(workspace, "config", "user.email", "dana@example.com");

        return workspace;
    }

    /// <summary>Puts the system inventory in review and saves a scan, so Discovery waits only on Project setup.</summary>
    private static async Task MakeDiscoveryReadyAsync(TestWorkspace workspace)
    {
        var root = workspace.Workspace.Root;
        var inventory = Path.Combine(root, "docs", "01-discovery", "D02-system-inventory.md");
        var text = await File.ReadAllTextAsync(inventory, CancellationToken.None);
        await File.WriteAllTextAsync(inventory, text.Replace("status: template", "status: in-review"), CancellationToken.None);
        var scan = LegacyScanner.Scan(workspace.Workspace.LegacyRoot, new ScanOptions(1252), CancellationToken.None);
        Directory.CreateDirectory(workspace.Workspace.LatestScanFolder);
        var json = JsonSerializer.SerializeToUtf8Bytes(scan, InventoryJsonContext.Default.LegacyInventory);
        await File.WriteAllBytesAsync(Path.Combine(workspace.Workspace.LatestScanFolder, "inventory.json"), json, CancellationToken.None);
    }

    private static async Task<string> GitAsync(TestWorkspace workspace, params string[] arguments)
    {
        var start = new ProcessStartInfo("git") { WorkingDirectory = workspace.Workspace.Root, RedirectStandardOutput = true };

        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        using var git = Process.Start(start)!;
        var output = await git.StandardOutput.ReadToEndAsync(CancellationToken.None);
        await git.WaitForExitAsync(CancellationToken.None);

        return output;
    }
}
