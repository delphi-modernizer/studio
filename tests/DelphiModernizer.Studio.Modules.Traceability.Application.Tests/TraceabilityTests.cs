using System.IO;
using System.Threading.Tasks;
using DelphiModernizer.Studio.Tests;
using Xunit;

namespace DelphiModernizer.Studio.Modules.Traceability.Application.Tests;

public sealed class TraceabilityTests
{
    [Fact]
    public async Task The_trace_of_a_new_workspace_covers_every_deliverable()
    {
        using var workspace = await TestWorkspace.CreateAsync();
        var sender = Services.Sender(workspace.Session, services => services.AddTraceabilityApplication());

        var trace = await sender.Send(new RunTraceQuery(Strict: true), TestContext.Current.CancellationToken);

        Assert.True(trace.Value.Trace.Strict);
        Assert.Equal(20, trace.Value.Trace.Deliverables.Length);
        Assert.Equal(trace.Value.Trace.Issues.Length, trace.Value.Problems.Count);
        Assert.Equal(14, trace.Value.Checks);
    }

    [Fact]
    public async Task An_open_question_reads_as_the_question_and_a_broken_reference_names_its_record()
    {
        using var workspace = await TestWorkspace.CreateAsync();
        string charter = Path.Combine(workspace.Workspace.Root, "docs", "00-project-charter");
        var cancellationToken = TestContext.Current.CancellationToken;
        await File.AppendAllTextAsync(
            Path.Combine(charter, "open-questions.md"),
            "| Q-001 | Which regulations apply to invoices? | Tax. | Test Person | Open |  |\n",
            cancellationToken);
        await File.AppendAllTextAsync(
            Path.Combine(charter, "D01-project-charter.md"),
            "\nSee BR-404.\n",
            cancellationToken);
        var sender = Services.Sender(workspace.Session, services => services.AddTraceabilityApplication());

        var report = (await sender.Send(new RunTraceQuery(Strict: false), cancellationToken)).Value;

        Assert.Contains(
            report.Problems,
            p => p.Description == "Still open: which regulations apply to invoices?" && p.Subject is null);
        Assert.Contains(report.Problems, p => p.Subject == "business rule BR-404");
        Assert.True(report.HeldUp >= 1);
    }
}
