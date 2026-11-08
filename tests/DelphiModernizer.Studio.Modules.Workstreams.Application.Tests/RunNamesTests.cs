using Xunit;

namespace DelphiModernizer.Studio.Modules.Workstreams.Application.Tests;

/// <summary>The Narrator names the handoff notes give for agent task rows and transcript items.</summary>
public sealed class RunNamesTests
{
    [Fact]
    public void A_task_another_role_is_writing_reads_as_blocked_with_the_blocking_run()
    {
        string reason = RunNames.BlockedReason(
            "functional requirements", "Extract business rules", "Legacy discovery", "10:42");

        string name = RunNames.Task("Functional requirements", "functional requirements", reason);

        Assert.Equal(
            "Functional requirements, writes the functional requirements, Blocked: Extract business rules "
                + "(Legacy discovery) is writing them, started 10:42.",
            name);
    }

    [Fact]
    public void A_task_its_own_role_is_writing_points_at_the_run_below()
    {
        string reason = RunNames.BlockedReason("functional requirements", "Extract business rules", null, "10:42");

        Assert.Equal("The run below is writing the functional requirements, started 10:42.", reason);
    }

    [Fact]
    public void A_task_a_run_from_elsewhere_is_writing_names_that_run()
    {
        Assert.Equal(
            "Run 20261108101500-ws02-frs, which this studio did not start, is writing them, started 10:42.",
            RunNames.BlockedByOtherRun("20261108101500-ws02-frs", "10:42"));
    }

    [Fact]
    public void A_task_that_can_run_reads_its_title_and_what_it_writes()
    {
        Assert.Equal(
            "Inventory and discovery report, writes the system inventory",
            RunNames.Task("Inventory and discovery report", "system inventory", blockedReason: null));
    }

    [Fact]
    public void Agent_text_reads_each_claim_with_its_fact_label()
    {
        var lines = RunTranscript.Lines(
            "Gold status seems to follow from the credit limit alone. [Inferred]");

        Assert.Equal(
            "Agent: Gold status seems to follow from the credit limit alone. Inferred.",
            RunNames.Agent(lines));
    }

    [Theory]
    [InlineData("Grep", "legacy/Sales/CustomerForm.dfm:22\nlegacy/Sales/CustomerForm.pas:35\n", false, "2 matches")]
    [InlineData("Read", "unit Pricing;\ninterface", false, "2 lines")]
    [InlineData("Edit", "", false, "Done")]
    [InlineData("Read", "File does not exist.", true, "Failed")]
    [InlineData("Read", null, false, "Working")]
    public void A_tool_call_sums_up_its_result(string tool, string? result, bool isError, string summary)
    {
        Assert.Equal(summary, RunNames.ToolSummary(tool, result, isError));
    }

    [Fact]
    public void A_collapsed_tool_call_reads_its_tool_its_result_and_its_state()
    {
        string summary = RunNames.ToolSummary("Grep", "a.pas:1\nb.pas:2", isError: false);

        Assert.Equal("Tool call Grep, 2 matches, collapsed", RunNames.Tool("Grep", summary, isExpanded: false));
    }

    [Fact]
    public void A_refusal_reads_the_call_its_file_and_the_guard_reason()
    {
        string name = RunNames.Refusal(
            "Edit",
            "legacy/Sales/Pricing.pas",
            "legacy/ is read-only evidence; record findings in docs/ instead.");

        Assert.Equal(
            "Refused by the guard: Edit legacy/Sales/Pricing.pas. "
                + "legacy/ is read-only evidence; record findings in docs/ instead.",
            name);
    }

    [Fact]
    public void Agent_text_links_the_evidence_it_cites()
    {
        var evidence = RunTranscript.Evidence(
            "BR-001 Gold discount (legacy/Sales/Pricing.pas#L18-L29), see also `legacy/Sales/CustomerForm.pas#L35`.");

        Assert.Equal(["legacy/Sales/Pricing.pas#L18-L29", "legacy/Sales/CustomerForm.pas#L35"], evidence);
    }
}
