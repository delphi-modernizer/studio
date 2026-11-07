using System;
using System.Globalization;
using System.Text.RegularExpressions;
using DelphiModernizer.Engine.Kit;

namespace DelphiModernizer.Studio.Application.Abstractions.Evidence;

/// <summary>A link from a claim to the legacy code it comes from, such as <c>legacy/Sales/Pricing.pas#L18-L29</c>.</summary>
/// <param name="Path">The workspace-relative file, starting with the kit's evidence root (<c>legacy/</c>).</param>
/// <param name="EndLine">The last cited line, or null when the link cites one line.</param>
public sealed record EvidenceReference(string Path, int StartLine, int? EndLine)
{
    /// <summary>The kit's evidence pattern, so the studio reads links exactly as the trace check does.</summary>
    private static readonly Lazy<Regex> _pattern = new(() => new Regex(
        $"^{KitBundle.Embedded.Spec.Ids.Evidence.Pattern}$",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1)));

    public static bool TryParse(string text, out EvidenceReference? reference)
    {
        ArgumentNullException.ThrowIfNull(text);
        var groups = KitBundle.Embedded.Spec.Ids.Evidence.Groups;
        var match = _pattern.Value.Match(text.Trim());

        if (!match.Success)
        {
            reference = null;

            return false;
        }

        var end = match.Groups[groups.End];
        reference = new EvidenceReference(
            $"{KitBundle.Embedded.Spec.Ids.Evidence.Root.TrimEnd('/')}/{match.Groups[groups.Path].Value}",
            int.Parse(match.Groups[groups.Start].Value, CultureInfo.InvariantCulture),
            end.Success ? int.Parse(end.Value, CultureInfo.InvariantCulture) : null);

        return true;
    }

    /// <summary>What Narrator reads, such as "Evidence: legacy/Sales/Pricing.pas, lines 18 to 29".</summary>
    public string Describe()
    {
        return EndLine is { } end && end != StartLine
            ? $"Evidence: {Path}, lines {StartLine} to {end}"
            : $"Evidence: {Path}, line {StartLine}";
    }
}
