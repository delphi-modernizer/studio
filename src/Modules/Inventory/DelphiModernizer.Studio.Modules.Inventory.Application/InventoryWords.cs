using System.Globalization;
using DelphiModernizer.Engine.Delphi.Inventory;
using DelphiModernizer.Engine.Kit;
using DelphiModernizer.Studio.Application.Abstractions.Evidence;

namespace DelphiModernizer.Studio.Modules.Inventory.Application;

/// <summary>What Narrator reads for the inventory's rows and links, built from the scan.</summary>
public static class InventoryWords
{
    /// <summary>"Pricing, Sales/Pricing.pas, cycle, 23 code lines of 31, fan-in 3, fan-out 1".</summary>
    public static string CodeModule(UnitInfo unit, bool inCycle)
    {
        string cycle = inCycle ? ", cycle" : string.Empty;
        string lines = $"{Number(unit.CodeLines)} code lines of {Number(unit.Lines)}";

        return $"{unit.Name}, {unit.Path}{cycle}, {lines}, fan-in {unit.FanIn}, fan-out {unit.FanOut}";
    }

    /// <summary>"Evidence: legacy/Sales/Pricing.pas, line 18", or the file alone when the link names no line.</summary>
    /// <param name="link">A path relative to legacy/, with <c>#L</c> and the line when it has one.</param>
    public static string Evidence(string link)
    {
        string path = KitBundle.Embedded.Spec.Ids.Evidence.Root + link;

        return EvidenceReference.TryParse(path, out var reference) ? reference!.Describe() : $"Evidence: {path}";
    }

    private static string Number(int value)
    {
        return value.ToString("N0", CultureInfo.CurrentCulture);
    }
}
