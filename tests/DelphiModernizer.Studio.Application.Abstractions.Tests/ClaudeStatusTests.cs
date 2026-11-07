using System;
using DelphiModernizer.Studio.Application.Abstractions.Agents;
using Xunit;

namespace DelphiModernizer.Studio.Application.Abstractions.Tests;

public sealed class ClaudeStatusTests
{
    private static readonly Version _minimum = new(2, 1, 259);

    [Fact]
    public void A_test_that_finds_Claude_Code_reads_its_version()
    {
        var status = new ClaudeStatus(ClaudeState.Ready, "claude.exe", new Version(2, 1, 300), null, _minimum);

        Assert.Equal("Claude Code 2.1.300 found", status.Describe());
    }

    [Fact]
    public void A_test_that_fails_reads_the_engine_words_as_written()
    {
        const string shim = @"C:\tools\claude.cmd is a script shim; install the native Claude Code build.";
        var status = new ClaudeStatus(ClaudeState.Unusable, @"C:\tools\claude.cmd", null, shim, _minimum);

        Assert.Equal(shim, status.Describe());
    }
}
