using DelphiModernizer.Studio.Application.Abstractions.Evidence;
using Xunit;

namespace DelphiModernizer.Studio.Application.Abstractions.Tests;

public sealed class EvidenceReferenceTests
{
    [Theory]
    [InlineData(
        "legacy/Sales/Pricing.pas#L18-L29",
        "legacy/Sales/Pricing.pas",
        18,
        29,
        "Evidence: legacy/Sales/Pricing.pas, lines 18 to 29")]
    [InlineData("legacy/Utils.pas#L7", "legacy/Utils.pas", 7, null, "Evidence: legacy/Utils.pas, line 7")]
    public void A_link_reads_as_its_file_and_lines(string text, string path, int start, int? end, string spoken)
    {
        Assert.True(EvidenceReference.TryParse(text, out var reference));
        Assert.Equal(new EvidenceReference(path, start, end), reference);
        Assert.Equal(spoken, reference!.Describe());
    }

    [Theory]
    [InlineData("docs/02-functional-requirements/business-rules.md#L4")]
    [InlineData("legacy/Sales/Pricing.pas")]
    [InlineData("see legacy/Sales/Pricing.pas#L18")]
    public void Anything_else_is_not_evidence(string text)
    {
        Assert.False(EvidenceReference.TryParse(text, out _));
    }
}
