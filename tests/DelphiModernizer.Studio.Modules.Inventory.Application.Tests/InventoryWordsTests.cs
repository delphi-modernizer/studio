using System.Collections.Generic;
using System.Linq;
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

        string name = InventoryWords.CodeModule(unit, inCycle: true);

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

    [Fact]
    public void Risk_markers_read_by_name_with_hard_coded_credentials_first()
    {
        string[] codes = ["global-state", "currency", "absolute-variable", "hard-coded-credential"];

        var names = codes
            .OrderBy(InventoryWords.RiskRank)
            .Select(InventoryWords.RiskKind);

        Assert.Equal(["Hard-coded credential", "Absolute variable", "Currency", "Global state"], names);
    }

    [Fact]
    public void A_risk_code_a_newer_scanner_adds_reads_as_itself_and_sorts_last()
    {
        Assert.Equal("new-marker", InventoryWords.RiskKind("new-marker"));
        Assert.Equal(InventoryWords.RiskCodes.Count, InventoryWords.RiskRank("new-marker"));
    }

    [Fact]
    public void Every_risk_code_the_scanner_documents_has_a_name()
    {
        Assert.All(InventoryWords.RiskCodes, code => Assert.NotEqual(code, InventoryWords.RiskKind(code)));
        Assert.Equal(25, InventoryWords.RiskCodes
            .Distinct()
            .Count());
    }

    [Theory]
    [InlineData("Data/DataModule.dfm", "TDmMain", false, "TDmMain · data module, not a screen")]
    [InlineData("MainForm.dfm", "TFrmMain", false, "TFrmMain")]
    [InlineData("Reports/Invoice.dfm", null, true, "Can’t be read")]
    public void A_screen_reads_by_its_class(string path, string? rootClass, bool binary, string expected)
    {
        var form = new FormInfo(path, rootClass?[1..], rootClass, binary, [], []);
        var dataModules = new HashSet<string>(["TDmMain"]);

        Assert.Equal(expected, InventoryWords.Screen(form, dataModules));
    }

    [Fact]
    public void A_cycle_of_two_reads_as_each_using_the_other()
    {
        Assert.Equal("Cycle: Pricing uses Utils and Utils uses Pricing.", InventoryWords.Cycle(["Pricing", "Utils"]));
        Assert.Equal("Cycle: A, B and C use one another.", InventoryWords.Cycle(["A", "B", "C"]));
    }

    [Fact]
    public void A_link_shows_with_legacy_in_front()
    {
        Assert.Equal("legacy/Data/DataModule.dfm#L5", InventoryWords.InLegacy("Data/DataModule.dfm#L5"));
    }
}
