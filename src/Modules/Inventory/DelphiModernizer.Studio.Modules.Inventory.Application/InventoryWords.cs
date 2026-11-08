using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DelphiModernizer.Engine.Delphi.Inventory;
using DelphiModernizer.Engine.Kit;
using DelphiModernizer.Studio.Application.Abstractions.Evidence;

namespace DelphiModernizer.Studio.Modules.Inventory.Application;

/// <summary>The words the inventory shows and Narrator reads, built from the scan.</summary>
public static class InventoryWords
{
    /// <summary>The scanner's risk marker codes in the order the table lists them, each with its name.</summary>
    /// <remarks>
    /// The canvas names and orders twelve codes; the others are placed and named in the same style, listed in
    /// docs/open-questions.md. The codes are the engine's contract (its docs/SCANNER.md).
    /// </remarks>
    private static readonly (string Code, string Name)[] _riskKinds = [
        ("hard-coded-credential", "Hard-coded credential"),
        ("sql-concatenation", "SQL concatenation"),
        ("process-launch", "Process launch"),
        ("inline-assembler", "Inline assembler"),
        ("absolute-variable", "Absolute variable"),
        ("pointer-math", "Pointer arithmetic"),
        ("manual-memory", "Manual memory"),
        ("raw-memory", "Raw memory"),
        ("short-string", "Short string"),
        ("real48", "Real48"),
        ("extended-precision", "Extended precision"),
        ("currency", "Currency"),
        ("variant", "Variant"),
        ("goto", "Goto"),
        ("with-statement", "With statement"),
        ("process-messages", "ProcessMessages"),
        ("thread", "Thread"),
        ("registry", "Registry"),
        ("ini-file", "INI file"),
        ("overflow-checks", "Overflow checks"),
        ("range-checks", "Range checks"),
        ("io-checks", "I/O checks"),
        ("conditional-compilation", "Conditional compilation"),
        ("include-file", "Include file"),
        ("global-state", "Global state"),
    ];

    public static IReadOnlyCollection<string> RiskCodes => [.. _riskKinds.Select(k => k.Code)];

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
        string path = InLegacy(link);

        return EvidenceReference.TryParse(path, out var reference) ? reference!.Describe() : $"Evidence: {path}";
    }

    /// <summary>A link relative to legacy/ as evidence cites it: "legacy/Data/DataModule.dfm#L5".</summary>
    public static string InLegacy(string link)
    {
        return KitBundle.Embedded.Spec.Ids.Evidence.Root + link;
    }

    /// <summary>A risk marker's name; a code a newer scanner adds reads as itself until it gets words here.</summary>
    public static string RiskKind(string code)
    {
        int index = RiskRank(code);

        return index < _riskKinds.Length ? _riskKinds[index].Name : code;
    }

    /// <summary>Where a risk marker's kind sorts: credentials first, codes without words last.</summary>
    public static int RiskRank(string code)
    {
        int index = Array.FindIndex(_riskKinds, k => k.Code == code);

        return index < 0 ? _riskKinds.Length : index;
    }

    /// <summary>A form by its class: "TFrmMain", "TDmMain · data module, not a screen" or "Can’t be read".</summary>
    /// <param name="dataModules">The classes the scan found descending from TDataModule.</param>
    public static string Screen(FormInfo form, IReadOnlySet<string> dataModules)
    {
        ArgumentNullException.ThrowIfNull(form);
        ArgumentNullException.ThrowIfNull(dataModules);

        if (form.IsBinary)
        {
            return "Can’t be read";
        }

        string name = form.RootClass ?? form.RootName ?? System.IO.Path.GetFileNameWithoutExtension(form.Path);

        return form.RootClass is not null && dataModules.Contains(form.RootClass)
            ? $"{name} · data module, not a screen"
            : name;
    }

    /// <summary>"Cycle: Pricing uses Utils and Utils uses Pricing.", or a larger group using one another.</summary>
    /// <param name="members">A strongly connected group of code modules, as the scan sorts it.</param>
    public static string Cycle(IReadOnlyList<string> members)
    {
        ArgumentNullException.ThrowIfNull(members);

        return members.Count == 2
            ? $"Cycle: {members[0]} uses {members[1]} and {members[1]} uses {members[0]}."
            : $"Cycle: {string.Join(", ", members.Take(members.Count - 1))} and {members[^1]} use one another.";
    }

    private static string Number(int value)
    {
        return value.ToString("N0", CultureInfo.CurrentCulture);
    }
}
