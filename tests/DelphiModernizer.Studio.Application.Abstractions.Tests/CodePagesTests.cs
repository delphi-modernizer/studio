using DelphiModernizer.Studio.Application.Abstractions.Settings;
using Xunit;

namespace DelphiModernizer.Studio.Application.Abstractions.Tests;

public sealed class CodePagesTests
{
    [Fact]
    public void A_code_page_reads_as_its_label()
    {
        var western = CodePages.Common[0];

        Assert.Equal("windows-1252 · Western European", western.ToString());
    }
}
