# Icons

The studio draws every icon from its own font, **Studio Icons**, the way Contr for Windows does: Segoe Fluent Icons
is not used, so no glyph depends on a code point that may or may not exist in the system font.

- `assets/icons/svg` holds one SVG per icon, taken unchanged from Fluent UI System Icons (MIT, see
  `assets/icons/LICENSE`), 20 px regular, or filled where the name ends in `Filled`.
- `assets/icons/icons.json` pins each icon's code point (E100 upwards) and records its Fluent source file and where
  the design uses it. Add new icons at the end so existing glyphs keep their code points.
- `scripts/build_icon_font.py` writes `Assets/Fonts/StudioIcons.ttf` and the `StudioIcons` constants
  (`StudioIcons.g.cs`). `FONT`, `TABLE` and `NAMESPACE` at the top of the script name the project layout.

```xml
<!-- App.xaml -->
<FontFamily x:Key="StudioIconsFontFamily">ms-appx:///Assets/Fonts/StudioIcons.ttf#Studio Icons</FontFamily>
<Style x:Key="IconStyle" TargetType="FontIcon">
    <Setter Property="FontFamily" Value="{StaticResource StudioIconsFontFamily}" />
    <Setter Property="FontSize" Value="16" />
</Style>

<!-- a page -->
<FontIcon Glyph="{x:Bind icons:StudioIcons.Requirements}" Style="{StaticResource IconStyle}" />
```

Sizes in the design: 16 by default, 12 inside status pills, 18 in diagram tiles, 20 in settings cards and role tiles.
A selected navigation item shows the `…Filled` glyph of its icon.

## App icon

`assets/app-icon` holds four masters: `app.svg` (unplated, 32 px and up), `app-small.svg` (unplated, 24 px and
below: wider crest, no nose guard), and the plated versions of both. `scripts/build_app_icon.py` writes `app.ico`
(16 to 256), the `Square44x44Logo` target sizes (plated, and unplated for the taskbar and Start), the
`Square150x150Logo` and `StoreLogo` scales, and a 256 px `StudioLogo.png`. Flat colours only, no gradients.
