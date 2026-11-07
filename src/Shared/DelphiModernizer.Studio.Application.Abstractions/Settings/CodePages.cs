using System.Collections.Generic;

namespace DelphiModernizer.Studio.Application.Abstractions.Settings;

/// <summary>A code page a Delphi tree is commonly saved in, as the wizard and Settings offer it.</summary>
public sealed record CodePage(int Number, string Label);

public static class CodePages
{
    /// <summary>The ANSI code pages Windows used for Delphi sources, Western European first.</summary>
    public static IReadOnlyList<CodePage> Common { get; } = [
        new(1252, "windows-1252 · Western European"),
        new(1250, "windows-1250 · Central European"),
        new(1251, "windows-1251 · Cyrillic"),
        new(1253, "windows-1253 · Greek"),
        new(1254, "windows-1254 · Turkish"),
        new(1255, "windows-1255 · Hebrew"),
        new(1256, "windows-1256 · Arabic"),
        new(1257, "windows-1257 · Baltic"),
        new(1258, "windows-1258 · Vietnamese"),
        new(874, "windows-874 · Thai"),
        new(932, "shift_jis · Japanese"),
        new(936, "gb2312 · Chinese, Simplified"),
        new(949, "ks_c_5601-1987 · Korean"),
        new(950, "big5 · Chinese, Traditional"),
    ];
}
