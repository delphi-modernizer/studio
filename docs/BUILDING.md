# Building

## Where the engine comes from

The studio uses the engine's libraries ([ADR 0002](adr/0002-engine-from-source-until-packages.md)). `EngineSource`
decides how:

| `EngineSource` | Used when | What it does |
| --- | --- | --- |
| `Source` | An engine checkout exists at `.engine/` (CI) or `../engine/` (beside this repository), or `EngineRoot` names one | Project references into the checkout, so a change can span both repositories |
| `Package` | No checkout is found | Package references to `DelphiModernizer.Engine.*` at `EngineVersion` from GitHub Packages |

The test projects need `Source`: the Workstreams tests run the engine's fake Claude Code CLI
(`tests/DelphiModernizer.Engine.Agents.FakeClaude`) through `$(EngineRoot)`, so they do not build with
`EngineSource=Package`.

The engine checkout needs its `kit` submodule (`git submodule update --init`). CI checks the engine out at the commit
[engine.lock](../engine.lock) pins.

For `Package`, GitHub Packages needs a token even for public packages: create a personal access token with
`read:packages` and add it as the `delphi-modernizer` source's credentials, for example with
`dotnet nuget update source delphi-modernizer --username <you> --password <token> --store-password-in-clear-text`
in your user-level NuGet configuration.

## Icons

`scripts/build-icon-font.py` and `scripts/build-app-icon.py` rebuild the committed font, glyph table, app icon and
logos from `assets/`; see [icons.md](icons.md).
