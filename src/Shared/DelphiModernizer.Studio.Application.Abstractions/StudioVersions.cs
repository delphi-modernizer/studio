using System.Reflection;
using DelphiModernizer.Engine.Kit;

namespace DelphiModernizer.Studio.Application.Abstractions;

/// <summary>
/// The versions Start and Settings show: the studio, the engine it runs, and the kit the engine embeds.
/// </summary>
public sealed record StudioVersions(string Studio, string Engine, string Kit)
{
    public static StudioVersions Current { get; } = new(
        VersionOf(Assembly.GetEntryAssembly()),
        VersionOf(typeof(KitBundle).Assembly),
        KitBundle.Embedded.Version);

    /// <summary>The three on one line, as Settings shows them and Copy versions puts them on the clipboard.</summary>
    public string Line => $"Studio {Studio} · Engine (dmz) {Engine} · Kit {Kit}";

    /// <remarks>The informational version without the source revision the SDK appends after a plus sign.</remarks>
    private static string VersionOf(Assembly? assembly)
    {
        var version = assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        return version?.Split('+')[0] ?? "unknown";
    }
}
