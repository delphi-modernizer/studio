# Building

## Where the engine comes from

The studio uses the engine's libraries ([ADR 0002](adr/0002-engine-from-source-until-packages.md)). `EngineSource`
decides how:

| `EngineSource` | Used when | What it does |
| --- | --- | --- |
| `Source` | An engine checkout exists at `.engine/` (CI) or `../engine/` (beside this repository), or `EngineRoot` names one | Project references into the checkout, so a change can span both repositories |
| `Package` | No checkout is found | Package references to `DelphiModernizer.Engine.*` at `EngineVersion` from GitHub Packages |

The test projects need `Source`: the Workstreams tests run the engine's fake Claude Code CLI
(`tests/DelphiModernizer.Engine.Agents.FakeClaude`) and the stand-in's tests read the engine's sample
(`tests/fixtures/delphi/legacy-erp`) through `$(EngineRoot)`, so they do not build with `EngineSource=Package`.

The engine checkout needs its `kit` submodule (`git submodule update --init`). CI checks the engine out at the commit
[engine.lock](../engine.lock) pins.

For `Package`, GitHub Packages needs a token even for public packages: create a personal access token with
`read:packages` and add it as the `delphi-modernizer` source's credentials, for example with
`dotnet nuget update source delphi-modernizer --username <you> --password <token> --store-password-in-clear-text`
in your user-level NuGet configuration.

## Icons

`scripts/build_icon_font.py` and `scripts/build_app_icon.py` (`uv run python <script>`) rebuild the committed font,
glyph table, app icon and logos from `assets/`; see [icons.md](icons.md).

## Screenshots

`tests/DelphiModernizer.Studio.Screenshots` captures every state of the built app; the `screenshots` CI job runs it,
one job per screen group, and uploads the artifacts `screenshots-<group>`
([ui-screenshots.md](design/ui-screenshots.md)).
It is a console tool, not a test project, so `dotnet test` skips it everywhere. It targets `net10.0-windows` with
`EnableWindowsTargeting`, so `dotnet build` compiles it on Linux too; it runs only on Windows.

`tests/DelphiModernizer.Studio.ClaudeStandIn` is the stand-in Claude Code the job gives the app. It builds as
`claude`; `tests/DelphiModernizer.Studio.ClaudeStandIn.Tests` runs it through the engine's runner on every platform.

To run the harness on a Windows PC, publish the app, `dmz` and the stand-in as the `screenshots` job does, then:

```bash
dotnet run --project tests/DelphiModernizer.Studio.Screenshots -c Release -- \
  --app out/studio/DelphiModernizer.Studio.exe --dmz out/studio/tools/dmz.exe --claude out/claude/claude.exe \
  --legacy ../engine/tests/fixtures/delphi/legacy-erp --out out/screenshots [--group start|overview|pages|agents]
```

It creates its workspaces with `dmz` in a temporary folder and deletes them afterwards. It moves your studio data
folder (`%LOCALAPPDATA%\DelphiModernizer\Studio`) aside while it runs and puts it back, may switch the display to
1920 × 1080 and briefly turns on high contrast. The app takes no launch argument for it: the tool writes the recent
workspaces and the settings the app already reads, and sets the stand-in's variables and `GIT_CONFIG_GLOBAL` on the
app's process.
