using System.Threading.Tasks;
using DelphiModernizer.Studio.Application.Abstractions.Workspaces;
using DelphiModernizer.Studio.Tests;
using Xunit;

namespace DelphiModernizer.Studio.Modules.SignOffs.Application.Tests;

public sealed class SignOffsTests
{
    [Fact]
    public async Task A_new_workspace_has_five_sign_offs()
    {
        using var workspace = await TestWorkspace.CreateAsync();
        var sender = Services.Sender(workspace.Session, services => services.AddSignOffsApplication());

        var gates = await sender.Send(new GetGatesQuery(), TestContext.Current.CancellationToken);

        Assert.Equal(5, gates.Value.Count);
    }

    [Fact]
    public async Task An_unknown_sign_off_cannot_be_approved()
    {
        using var workspace = await TestWorkspace.CreateAsync();
        var sender = Services.Sender(workspace.Session, services => services.AddSignOffsApplication());

        var approved = await sender.Send(
            new ApproveGateCommand("G9", "Looks fine."), TestContext.Current.CancellationToken);

        Assert.True(approved.IsFailure);
    }

    [Fact]
    public async Task Without_a_workspace_nothing_is_revoked()
    {
        var sender = Services.Sender(new WorkspaceSession(), services => services.AddSignOffsApplication());

        var revoked = await sender.Send(
            new RevokeGateCommand("G0", "Changed my mind."), TestContext.Current.CancellationToken);

        Assert.Equal(WorkspaceRequired.Error, revoked.Error);
    }
}
