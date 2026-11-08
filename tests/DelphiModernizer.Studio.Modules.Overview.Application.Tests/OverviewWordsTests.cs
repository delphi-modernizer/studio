using System.Linq;
using DelphiModernizer.Engine.Delphi.Inventory;
using DelphiModernizer.Studio.Application.Abstractions.Workstreams;
using Xunit;

namespace DelphiModernizer.Studio.Modules.Overview.Application.Tests;

public sealed class OverviewWordsTests
{
    private static readonly WorkstreamRole _architect = WorkstreamRole.All[0];

    private static readonly WorkstreamRole _discovery = WorkstreamRole.All[1];

    [Fact]
    public void Every_team_says_what_it_drafts()
    {
        var teams = WorkstreamRole.All
            .Skip(1)
            .Select(r => r.Code);

        Assert.Equal(teams.Order(), OverviewWords.Teams.Order());
    }

    [Fact]
    public void A_team_node_reads_as_the_handoff_names_it()
    {
        var progress = new WorkstreamProgress(_discovery, ["D02", "D06"], 2);

        string name = OverviewWords.TeamName(_discovery, progress, approved: false, running: false);

        Assert.Equal(
            "Legacy discovery team, 2 of 2 deliverables started. "
            + "Drafts the system inventory and the technical debt and risk assessment. Opens Workstreams.",
            name);
        Assert.Equal("2 of 2 started", OverviewWords.Progress(progress, approved: false));
    }

    [Fact]
    public void The_principal_architect_combines_the_drafts()
    {
        var progress = new WorkstreamProgress(_architect, ["D01", "D16"], 1);

        string name = OverviewWords.TeamName(_architect, progress, approved: false, running: false);

        Assert.Equal(
            "Principal Architect, 1 of 2 deliverables started. Combines the teams’ drafts. Opens Workstreams.",
            name);
    }

    [Fact]
    public void Once_every_sign_off_is_approved_a_team_counts_its_deliverables_as_approved()
    {
        var progress = new WorkstreamProgress(_discovery, ["D02", "D06"], 1);

        Assert.Equal("2 of 2 approved", OverviewWords.Progress(progress, approved: true));
        Assert.StartsWith(
            "Legacy discovery team, 2 of 2 deliverables approved.",
            OverviewWords.TeamName(_discovery, progress, approved: true, running: false));
    }

    [Fact]
    public void The_delphi_code_node_reads_the_scan_counts()
    {
        var words = OverviewWords.DelphiCode(Summary(files: 13, units: 6, forms: 4, dataModules: 1), cycles: 1);

        Assert.Equal(
            "Delphi code, Delphi 7: 13 files, 6 code modules, 3 screens and a data module, 1 circular dependency. "
            + "Opens Inventory.",
            words.AnnouncedName);
        Assert.Equal(
            ["6 code modules", "3 screens, 1 data module", "1 circular dependency"],
            [words.First, words.Second, words.Third]);
    }

    [Fact]
    public void The_delphi_code_node_counts_several_data_modules_and_cycles()
    {
        var words = OverviewWords.DelphiCode(Summary(files: 20, units: 9, forms: 5, dataModules: 2), cycles: 2);

        Assert.Equal(
            "Delphi code, Delphi 7: 20 files, 9 code modules, 3 screens and 2 data modules, 2 circular dependencies. "
            + "Opens Inventory.",
            words.AnnouncedName);
    }

    [Fact]
    public void The_delphi_code_node_says_what_the_scan_could_not_name()
    {
        var summary = Summary(files: 2, units: 1, forms: 1, dataModules: 0) with { DelphiVersions = [] };

        var words = OverviewWords.DelphiCode(summary, cycles: 0);

        Assert.Equal(
            ["Delphi version unknown", "1 code module", "1 screen, 0 data modules", "No circular dependencies"],
            [words.Subtitle, words.First, words.Second, words.Third]);
        Assert.StartsWith(
            "Delphi code, Delphi version unknown: 2 files, 1 code module, 1 screen, ",
            words.AnnouncedName);
    }

    [Fact]
    public void Before_a_scan_the_delphi_code_node_says_so()
    {
        var words = OverviewWords.DelphiCode(inventory: null, cycles: 0);

        Assert.Equal("Delphi code, not scanned yet. Opens Inventory.", words.AnnouncedName);
    }

    private static InventorySummary Summary(int files, int units, int forms, int dataModules)
    {
        return new InventorySummary(files, 3, units, forms, dataModules, 1, 205, 153, [], [], ["Delphi 7"], []);
    }

    [Fact]
    public void A_role_with_no_deliverables_counts_none()
    {
        Assert.Equal("0 of 0 started", OverviewWords.Progress(progress: null, approved: false));
        Assert.StartsWith(
            "Legacy discovery team, 0 of 0 deliverables started.",
            OverviewWords.TeamName(_discovery, progress: null, approved: false, running: false));
    }

    [Fact]
    public void A_team_tool_tip_says_what_the_team_drafts()
    {
        Assert.Equal(
            "Opens Legacy discovery in Workstreams. "
            + "This team drafts the system inventory and the technical debt and risk assessment.",
            OverviewWords.TeamToolTip(_discovery));
        Assert.Equal(
            "Opens Principal Architect in Workstreams. Combines the teams’ drafts.",
            OverviewWords.TeamToolTip(_architect));
    }

    [Fact]
    public void A_team_whose_agent_runs_says_so()
    {
        var progress = new WorkstreamProgress(_discovery, ["D02", "D06"], 2);

        string name = OverviewWords.TeamName(_discovery, progress, approved: false, running: true);

        Assert.StartsWith("Legacy discovery team, 2 of 2 deliverables started, agent running. Drafts ", name);
    }
}
