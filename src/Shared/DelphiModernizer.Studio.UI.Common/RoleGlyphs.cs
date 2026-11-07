using DelphiModernizer.Studio.UI.Common.Icons;

namespace DelphiModernizer.Studio.UI.Common;

/// <summary>The glyph each role of the governance model shows on Overview and Workstreams.</summary>
public static class RoleGlyphs
{
    public static string Of(string roleCode)
    {
        return roleCode switch {
            "PA" => StudioIcons.PrincipalArchitect,
            "WS01" => StudioIcons.Discover,
            "WS02" => StudioIcons.Requirements,
            "WS03" => StudioIcons.Architecture,
            "WS04" => StudioIcons.Database,
            "WS05" => StudioIcons.MigrationPlanning,
            "WS06" => StudioIcons.QualitySecurity,
            _ => StudioIcons.Workstreams,
        };
    }
}
