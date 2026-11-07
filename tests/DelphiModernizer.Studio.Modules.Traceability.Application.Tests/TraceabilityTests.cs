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

        Assert.True(trace.Value.Strict);
        Assert.Equal(20, trace.Value.Deliverables.Length);
    }
}
