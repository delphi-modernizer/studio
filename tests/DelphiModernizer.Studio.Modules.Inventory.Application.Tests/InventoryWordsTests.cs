using DelphiModernizer.Engine.Delphi.Inventory;
using Xunit;

namespace DelphiModernizer.Studio.Modules.Inventory.Application.Tests;

public sealed class InventoryWordsTests
{
    [Fact]
    public void A_code_module_row_reads_as_the_handoff_names_it()
    {
        var unit = new UnitInfo(
            "Pricing",
            "Sales/Pricing.pas",
            "unit",
            31,
            23,
            "us-ascii",
            [],
            ["Utils"],
            [],
            1,
            1,
            false,
            false,
            null,
            3,
            1);

        var name = InventoryWords.CodeModule(unit, inCycle: true);

        Assert.Equal("Pricing, Sales/Pricing.pas, cycle, 23 code lines of 31, fan-in 3, fan-out 1", name);
    }

    [Theory]
    [InlineData("Sales/Pricing.pas#L18-L29", "Evidence: legacy/Sales/Pricing.pas, lines 18 to 29")]
    [InlineData("Data/DataModule.dfm#L5", "Evidence: legacy/Data/DataModule.dfm, line 5")]
    [InlineData("Data/DataModule.pas", "Evidence: legacy/Data/DataModule.pas")]
    public void A_link_into_legacy_reads_as_evidence(string link, string expected)
    {
        Assert.Equal(expected, InventoryWords.Evidence(link));
    }
}
