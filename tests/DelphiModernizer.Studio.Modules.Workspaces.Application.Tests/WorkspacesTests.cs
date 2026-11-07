using System;
using System.IO;
using System.Threading.Tasks;
using DelphiModernizer.Engine.Workspaces;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;
using DelphiModernizer.Studio.Application.Abstractions.Workspaces;
using DelphiModernizer.Studio.Modules.Workspaces.Application.Commands;
using DelphiModernizer.Studio.Modules.Workspaces.Application.Queries;
using DelphiModernizer.Studio.Tests;
using Xunit;

namespace DelphiModernizer.Studio.Modules.Workspaces.Application.Tests;

public sealed class WorkspacesTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("dmz-studio-").FullName;
    private readonly WorkspaceSession _session = new();
    private readonly ISender _sender;

    public WorkspacesTests()
    {
        _sender = Services.Sender(
            _session,
            services => services.AddWorkspacesApplication(Path.Combine(_folder, "profile")));
    }

    [Fact]
    public async Task Creating_a_workspace_opens_it_and_remembers_it()
    {
        var request = new InitRequest(
            Path.Combine(_folder, "ws"),
            "Acme ERP",
            TestWorkspace.CreateLegacyTree(_folder),
            "dotnet",
            1252,
            "Test Person <test@example.com>",
            null,
            InitializeGit: false);

        var created = await _sender.Send(
            new CreateWorkspaceCommand(request, null),
            TestContext.Current.CancellationToken);
        var recent = await _sender.Send(new GetRecentWorkspacesQuery(), TestContext.Current.CancellationToken);

        Assert.True(created.IsSuccess, created.Error.Message);
        Assert.Same(created.Value, _session.Current);
        Assert.Equal("Acme ERP", Assert.Single(recent).Name);
    }

    [Fact]
    public async Task Opening_a_folder_inside_a_workspace_opens_the_workspace()
    {
        using var existing = await TestWorkspace.CreateAsync();

        var opened = await _sender.Send(
            new OpenWorkspaceCommand(Path.Combine(existing.Workspace.Root, "docs")),
            TestContext.Current.CancellationToken);

        Assert.True(opened.IsSuccess, opened.Error.Message);
        Assert.Equal(existing.Workspace.Root, _session.Current?.Root);
    }

    [Fact]
    public async Task A_folder_outside_any_workspace_does_not_open()
    {
        var opened = await _sender.Send(new OpenWorkspaceCommand(_folder), TestContext.Current.CancellationToken);

        Assert.True(opened.IsFailure);
        Assert.Null(_session.Current);
    }

    [Fact]
    public async Task The_preview_counts_what_the_scanner_finds()
    {
        var preview = await _sender.Send(
            new PreviewLegacyQuery(TestWorkspace.CreateLegacyTree(_folder), 1252),
            TestContext.Current.CancellationToken);

        Assert.True(preview.IsSuccess, preview.Error.Message);
        Assert.Equal(2, preview.Value.Summary.Units);
        Assert.Equal(["ErpApp"], preview.Value.ProjectNames);
        Assert.Equal(2, Assert.Single(preview.Value.Cycles).Count);
    }

    [Fact]
    public async Task A_folder_without_Delphi_source_is_not_previewed()
    {
        var empty = Directory.CreateDirectory(Path.Combine(_folder, "empty")).FullName;

        var preview = await _sender.Send(new PreviewLegacyQuery(empty, 1252), TestContext.Current.CancellationToken);

        Assert.Equal("Workspace.LegacySourceMissing", preview.Error.Code);
    }

    [Fact]
    public async Task Only_a_new_or_empty_folder_can_hold_a_workspace()
    {
        var used = Directory.CreateDirectory(Path.Combine(_folder, "used")).FullName;
        await File.WriteAllTextAsync(Path.Combine(used, "notes.txt"), "kept", TestContext.Current.CancellationToken);

        var fresh = await _sender.Send(
            new CheckWorkspaceFolderQuery(Path.Combine(_folder, "fresh")),
            TestContext.Current.CancellationToken);
        var taken = await _sender.Send(new CheckWorkspaceFolderQuery(used), TestContext.Current.CancellationToken);

        Assert.True(fresh.IsSuccess);
        Assert.Equal("Workspaces.FolderNotEmpty", taken.Error.Code);
    }

    [Fact]
    public async Task The_preview_of_a_missing_folder_says_so()
    {
        var preview = await _sender.Send(
            new PreviewLegacyQuery(Path.Combine(_folder, "nowhere"), 1252),
            TestContext.Current.CancellationToken);

        Assert.Equal("Workspaces.FolderMissing", preview.Error.Code);
    }

    public void Dispose()
    {
        Directory.Delete(_folder, recursive: true);
    }
}
