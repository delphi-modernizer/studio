using System.Globalization;
using Windows.Globalization.NumberFormatting;

namespace DelphiModernizer.Studio.Modules.Workstreams.UI.Views;

/// <summary>Shows a budget as "2.00" after the box's grey "$", and reads it back with or without a "$".</summary>
/// <remarks>Budgets are US dollars, as Claude Code's <c>--max-budget-usd</c> takes them, in any culture.</remarks>
public sealed partial class DollarFormatter : INumberFormatter2, INumberParser
{
    public string FormatDouble(double value)
    {
        return value.ToString("0.00", CultureInfo.InvariantCulture);
    }

    public string FormatInt(long value)
    {
        return FormatDouble(value);
    }

    public string FormatUInt(ulong value)
    {
        return FormatDouble(value);
    }

    public double? ParseDouble(string text)
    {
        string number = text
            .Trim()
            .TrimStart('$')
            .Trim();

        return double.TryParse(number, NumberStyles.Number, CultureInfo.InvariantCulture, out double value)
            ? value
            : null;
    }

    public long? ParseInt(string text)
    {
        return ParseDouble(text) is { } value ? (long)value : null;
    }

    public ulong? ParseUInt(string text)
    {
        return ParseDouble(text) is { } value and >= 0 ? (ulong)value : null;
    }
}
