using System;
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
        using var repository = await GitWorkspace.CreateAsync();
        var workspace = repository.Workspace;
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
        using var repository = await GitWorkspace.CreateAsync();
        var workspace = repository.Workspace;
        await GitAsync(workspace, "init");
        await GitAsync(workspace, "config", "user.name", string.Empty);
        var sender = Services.Sender(workspace.Session, services => services.AddSignOffsApplication());

        var approver = await sender.Send(new GetApproverQuery(), TestContext.Current.CancellationToken);

        Assert.Null(approver);
    }

    [Fact]
    public async Task Approving_records_the_git_identity_and_the_note_and_commits_it()
    {
        using var repository = await ApprovableAsync();
        var workspace = repository.Workspace;
        var sender = Services.Sender(workspace.Session, services => services.AddSignOffsApplication());

        var approved = await sender.Send(
            new ApproveGateCommand("G0", "Read the charter."), TestContext.Current.CancellationToken);
        string log = await GitAsync(workspace, "log", "--format=%s", "--", ".modernizer/approvals.json");

        Assert.Equal("Dana Reviewer <dana@example.com>", approved.Value.By);
        Assert.Equal("Read the charter.", approved.Value.Note);
        Assert.Equal("chore: approve the Project setup sign-off", log.Trim());
    }

    [Fact]
    public async Task A_refusal_names_what_fails_in_words()
    {
        using var repository = await ApprovableAsync();
        var workspace = repository.Workspace;
        var sender = Services.Sender(workspace.Session, services => services.AddSignOffsApplication());

        var approved = await sender.Send(
            new ApproveGateCommand("G1", "Looks fine."), TestContext.Current.CancellationToken);
        var signOffs = await sender.Send(new GetSignOffsQuery(), TestContext.Current.CancellationToken);

        Assert.True(SignOffRefusal.IsRefusal(approved.Error));
        Assert.StartsWith("Project setup approved (it is ready)", SignOffRefusal.Reason(signOffs.Value[1]));
    }

    [Fact]
    public async Task Revoking_keeps_later_approvals_and_blocks_approving_them_again()
    {
        using var repository = await ApprovableAsync();
        var workspace = repository.Workspace;
        await MakeDiscoveryReadyAsync(workspace);
        var sender = Services.Sender(workspace.Session, services => services.AddSignOffsApplication());
        var cancellationToken = TestContext.Current.CancellationToken;
        var setup = await sender.Send(new ApproveGateCommand("G0", "Read the charter."), cancellationToken);
        Assert.True(setup.IsSuccess);
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

    [Fact]
    public async Task A_stale_sign_off_names_the_files_changed_since_its_approval()
    {
        using var repository = await ApprovableAsync();
        var workspace = repository.Workspace;
        await GitAsync(workspace, "add", "-A");
        await GitAsync(workspace, "commit", "-m", "chore: start");
        var sender = Services.Sender(workspace.Session, services => services.AddSignOffsApplication());
        var cancellationToken = TestContext.Current.CancellationToken;
        await sender.Send(new ApproveGateCommand("G0", "Read the charter."), cancellationToken);
        string questions = Path.Combine(workspace.Workspace.Root, "docs", "00-project-charter", "open-questions.md");
        await File.AppendAllTextAsync(questions, "\nOne more question.\n", cancellationToken);

        var setup = (await sender.Send(new GetSignOffsQuery(), cancellationToken)).Value[0];

        Assert.Equal("stale", setup.Status.State);
        Assert.Equal(["docs/00-project-charter/open-questions.md"], setup.Changed);
        Assert.Contains("docs/00-project-charter/open-questions.md", setup.Status.CoveredFiles);
    }

    [Fact]
    public async Task The_history_keeps_the_approval_and_its_revocation_oldest_first()
    {
        using var repository = await ApprovableAsync();
        var workspace = repository.Workspace;
        var sender = Services.Sender(workspace.Session, services => services.AddSignOffsApplication());
        var cancellationToken = TestContext.Current.CancellationToken;
        await sender.Send(new ApproveGateCommand("G0", "Read the charter."), cancellationToken);
        await sender.Send(new RevokeGateCommand("G0", "The scope changed."), cancellationToken);

        var setup = (await sender.Send(new GetSignOffsQuery(), cancellationToken)).Value[0];

        Assert.Equal(["approve", "revoke"], setup.History.Select(e => e.Action));
        Assert.Equal("ready", setup.Status.State);
    }

    private static async Task<GitWorkspace> ApprovableAsync()
    {
        var repository = await GitWorkspace.CreateAsync();
        await GitAsync(repository.Workspace, "init");
        await GitAsync(repository.Workspace, "config", "user.name", "Dana Reviewer");
        await GitAsync(repository.Workspace, "config", "user.email", "dana@example.com");

        return repository;
    }

    /// <summary>
    /// Puts the system inventory in review and saves a scan, so Discovery waits only on Project setup.
    /// </summary>
    private static async Task MakeDiscoveryReadyAsync(TestWorkspace workspace)
    {
        string root = workspace.Workspace.Root;
        string inventory = Path.Combine(root, "docs", "01-discovery", "D02-system-inventory.md");
        string text = await File.ReadAllTextAsync(inventory, CancellationToken.None);
        string inReview = text.Replace("status: template", "status: in-review");
        await File.WriteAllTextAsync(inventory, inReview, CancellationToken.None);
        var scan = LegacyScanner.Scan(workspace.Workspace.LegacyRoot, new ScanOptions(1252), CancellationToken.None);
        Directory.CreateDirectory(workspace.Workspace.LatestScanFolder);
        byte[] json = JsonSerializer.SerializeToUtf8Bytes(scan, InventoryJsonContext.Default.LegacyInventory);
        string file = Path.Combine(workspace.Workspace.LatestScanFolder, "inventory.json");
        await File.WriteAllBytesAsync(file, json, CancellationToken.None);
    }

    private static async Task<string> GitAsync(TestWorkspace workspace, params string[] arguments)
    {
        var start = new ProcessStartInfo("git") {
            WorkingDirectory = workspace.Workspace.Root,
            RedirectStandardOutput = true,
        };

        foreach (string argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        using var git = Process.Start(start)!;
        string output = await git.StandardOutput.ReadToEndAsync(CancellationToken.None);
        await git.WaitForExitAsync(CancellationToken.None);

        return output;
    }

    /// <summary>
    /// A test workspace made a git repository; Windows refuses to delete the read-only objects git writes.
    /// </summary>
    private sealed class GitWorkspace(TestWorkspace workspace) : IDisposable
    {
        public TestWorkspace Workspace { get; } = workspace;

        public static async Task<GitWorkspace> CreateAsync()
        {
            return new GitWorkspace(await TestWorkspace.CreateAsync());
        }

        public void Dispose()
        {
            foreach (string file in Directory.EnumerateFiles(Workspace.Parent, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }

            Workspace.Dispose();
        }
    }
}
