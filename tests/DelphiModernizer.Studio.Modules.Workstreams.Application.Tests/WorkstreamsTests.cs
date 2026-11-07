using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DelphiModernizer.Studio.Application.Abstractions.Agents;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;
using DelphiModernizer.Studio.Application.Abstractions.Settings;
using DelphiModernizer.Studio.Application.Abstractions.Workspaces;
using DelphiModernizer.Studio.Tests;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DelphiModernizer.Studio.Modules.Workstreams.Application.Tests;

public sealed class WorkstreamsTests
{
    private static readonly AgentSettings _noClaude = AgentSettings.Default with {
        ClaudePath = Path.Combine(Path.GetTempPath(), "no-such-dir", "claude.exe"),
    };

    [Fact]
    public async Task Each_role_lists_its_deliverables_and_recipes_from_the_kit()
    {
        var sender = Sender(new WorkspaceSession());

        var workstreams = await sender.Send(new GetWorkstreamsQuery(), TestContext.Current.CancellationToken);

        var discovery = workstreams.Single(w => w.Role.Code == "WS01");
        Assert.Equal(7, workstreams.Count);
        Assert.Equal(["D02", "D06"], discovery.Deliverables.Select(d => d.Id));
        Assert.Contains(discovery.Recipes, r => r.Id == "ws01-business-rules");
    }

    [Fact]
    public async Task An_unknown_recipe_does_not_start()
    {
        using var workspace = await TestWorkspace.CreateAsync();
        var sender = Sender(workspace.Session);

        var started = await sender.Send(
            new StartRunCommand("ws99-nothing", null, 1m), TestContext.Current.CancellationToken);

        Assert.Equal("Workstreams.UnknownRecipe", started.Error.Code);
    }

    [Fact]
    public async Task Without_Claude_Code_no_run_starts_and_the_status_says_why()
    {
        using var workspace = await TestWorkspace.CreateAsync();
        var sender = Sender(workspace.Session);

        var started = await sender.Send(
            new StartRunCommand("ws01-discover", null, 1m), TestContext.Current.CancellationToken);
        var status = await sender.Send(new GetClaudeStatusQuery(), TestContext.Current.CancellationToken);

        Assert.True(started.IsFailure);
        Assert.False(status.IsReady);
        Assert.Equal(ClaudeState.NotFound, status.State);
        Assert.NotNull(status.Problem);
    }

    [Fact]
    public async Task A_new_workspace_has_no_runs()
    {
        using var workspace = await TestWorkspace.CreateAsync();

        var runs = await Sender(workspace.Session).Send(new GetRunsQuery(), TestContext.Current.CancellationToken);

        Assert.Empty(runs.Value);
    }

    private static ISender Sender(IWorkspaceSession session)
    {
        return Services.Sender(session, services => {
            services.AddSingleton<IStudioSettings>(new FixedSettings(_noClaude));
            services.AddWorkstreamsApplication();
        });
    }

    private sealed class FixedSettings(AgentSettings agents) : IStudioSettings
    {
        public AgentSettings Agents { get; private set; } = agents;

        public int DefaultCodePage => 1252;

        public string? ToolDirectory => null;

        public void SaveAgents(AgentSettings settings)
        {
            Agents = settings;
        }

        public void SaveDefaultCodePage(int codePage)
        {
            // The tests never change the code page.
        }
    }
}
