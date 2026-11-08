# Screenshots of the running app

The handoff's definition of done puts a screenshot of the running app beside every artboard. The app is WinUI 3 and
runs only on Windows, so the screenshots come from the `screenshots` job in [ci.yml](../../.github/workflows/ci.yml).
That job is also the app's smoke test: it fails when the app does not start, a page cannot be reached or the app logs
an error.

## Options

| | FlaUI (UIA3) | WinAppDriver or Appium | The app captures itself (`RenderTargetBitmap`) |
| --- | --- | --- | --- |
| What it is | A .NET library over the UI Automation COM client; the harness is a console tool | A WebDriver server over UI Automation; Appium's Windows driver runs WinAppDriver underneath | A launch argument that makes the app walk its pages and render each into a PNG |
| On `windows-latest` | In process, no server, no port; needs the runner's interactive desktop, which hosted runners have | WinAppDriver needs Developer Mode and a server started before the tests; Appium adds Node.js and the Appium server | Needs nothing from the runner |
| Licence | MIT | WinAppDriver is a free Microsoft binary under its own licence, closed source; Appium is Apache 2.0 | — |
| Maintenance | 5.0.0 is current and supports .NET 8 and later | WinAppDriver's last stable release is 1.2.1 (2020) and the project is inactive | — |
| CVEs | None recorded for FlaUI; its transitive packages pass the NuGet audit this repository runs at `low` | None recorded for WinAppDriver; Appium brings a Node.js dependency tree to audit | None |
| Captures | The window as the screen shows it: title bar, caption buttons, the backdrop, flyouts and dialogs | The same, as a screen capture | Only the XAML tree: no Mica, no system-drawn caption buttons, and popups and dialogs live in other roots |
| Also checks | The Narrator names the pages expose, since every step finds its control by name | The same | Nothing about automation |
| Code | About 650 lines in `tests/`, nothing in the app | About the same in `tests/`, plus server setup in CI | Navigation and capture code inside the app and its shell, behind a launch argument |

## Decision

FlaUI with UIA3. It captures what a person sees, title bar and Mica included, which the definition of done compares
against the artboards; it runs in process on a hosted runner with nothing to install; and it keeps every line of the
harness out of the app. Finding each control by its automation name means a renamed control breaks the job, which is
the Narrator contract the handoff asks for anyway.

## A stand-in Claude Code

The Run page, the Workstreams states and every Claude Code state need a CLI the engine accepts. The runners have no
Claude Code, and the engine on Windows takes only a native `claude.exe` that answers `--version` with
`ClaudeExecutable.MinimumVersion` or later, then runs it with the stream-json flags.

| | The engine's FakeClaude | Real Claude Code | A stand-in `claude.exe` in `tests/` |
| --- | --- | --- | --- |
| What it is | Replays an NDJSON file; its first two arguments are that file and a file to record the rest in | The CLI, signed in with an API key from a repository secret | A console tool named `claude`, which answers `--version` and plays a scenario the harness picks through an environment variable |
| Accepted by the engine | No: `ClaudeExecutable.Locate` passes no leading arguments, so the replay file cannot be named | Yes | Yes: it is a `claude.exe` and answers `--version` |
| Every state on demand | Only with an engine change to name the script another way | No: a real run cannot be made to fail, stop or reach its budget at a chosen point | Yes: completed, failed, stopped and budget reached, started and held, and a version that is too old |
| Same pixels every run | Yes | No: the model's text, cost and turns change each run, and each run costs money | Yes |
| Engine change | Yes, in a security decision (which executables a run starts) | No | No |
| Code | Engine change and a script per state | A secret and CI setup | About 250 lines in `tests/`, nothing in the app |

Decision: the stand-in, `tests/DelphiModernizer.Studio.ClaudeStandIn`. It writes the stream-json events
`StreamJsonParser` reads (`system` init, `assistant` text and `tool_use`, `user` `tool_result`, `result`). Its events
are the Run board's: the Legacy discovery agent runs the kit's `ws01-business-rules` recipe on `legacy/Sales`, reads
`Pricing.pas`, greps for `CREDIT_LIMIT`, states fact-labelled findings with evidence links, tries to edit
`legacy/Sales/Pricing.pas` and is refused, then edits its deliverable. Nothing in a result is invented: a read returns
the file, a grep returns its matching lines, the guard's answer is the engine's own `Guard.Evaluate`, and an edit the
guard allows is made, so the run leaves the Functional Requirements Specification as Draft. The refusal is worded as
Claude Code words a blocking PreToolUse hook, which is the text the studio's `RunTranscript` recognises.
`tests/DelphiModernizer.Studio.ClaudeStandIn.Tests` runs each scenario through the engine's `AgentRunner` and checks
the events it parses.

| `STUDIO_CLAUDE_SCENARIO` | What the stand-in does after `--version` |
| --- | --- |
| `completed` (default) | Streams the run, then a `success` result |
| `failed` | Streams the run up to the refusal, writes an error to standard error and exits 1 without a result |
| `stopped` | Streams the run, then waits until the harness presses Stop |
| `budget` | Streams the run, then an `error_max_budget_usd` result at the run's own budget |
| `starting` | Waits before the session starts, until the harness presses Stop |

`STUDIO_CLAUDE_VERSION=too-old` makes `--version` answer one patch below the minimum. "Not found" is no `claude.exe`
on PATH and no path in Settings; "a script shim" is a `claude.cmd` the harness writes and chooses in Settings.

## How the job works

1. It publishes the app and `dmz` as the `package` job does.
2. It creates the brief's sample workspace, a Delphi 7 ERP on the BDE named "Acme ERP modernization" with the target
   C#/.NET, from the engine's `tests/fixtures/delphi/legacy-erp`, with `dmz init` and `dmz scan`. The workspace lives in
   the runner's temporary folder and is never committed. Only a person approves, so no sign-off is approved and every
   deliverable is a template; the brief's "G0 approved" and "D01 approved" states do not appear.
3. `tests/DelphiModernizer.Studio.Screenshots` writes the app's own local settings: the workspace as the one recent
   workspace and the Theme setting. The app needs no launch argument. The person's data folder is moved aside for the
   run and put back afterwards.
4. It starts the app once per theme, opens the workspace from Start, visits each page through the pane, and sizes the
   window with `SetWindowPos` before each capture.
5. It uploads `out/screenshots` as the artifact `screenshots`, with the app's log under `logs/` when it wrote one.

The window size is the size of its visible frame, title bar included, without the invisible resize borders: a
1280 × 800 capture is a 1280 × 800 PNG. Hosted runners start at 1024 × 768, so the harness sets the display to
1920 × 1080 first, at 100 % scale.

## Files

`<page>-<state>-<width>x<height>-<theme>.png`, for example `signoffs-expanded-960x576-dark.png`.

| Page | States |
| --- | --- |
| `start` | `default`: the one recent workspace |
| `newworkspace` | `step1` |
| `overview` | `default` |
| `inventory` | One per SelectorBar section, named after it without its count: `code-modules`, `screens`, `data-and-libraries`, `sql-and-risks`, `cycles`, `diagnostics` |
| `workstreams` | `default`: Claude Code is not installed on the runner |
| `deliverables` | `default` |
| `traceability` | One per SelectorBar view: `issues`, `matrix` |
| `signoffs` | `expanded`: one sign-off open |
| `settings` | `default` |

Themes are `light`, `dark` and `highcontrast`; high contrast covers Overview and Sign-offs. Run is not captured: it
opens only for an agent run, and a fresh workspace has none; starting one needs Claude Code, which the runner does not
have.

## High contrast and Mica

The harness turns high contrast on with `SystemParametersInfo(SPI_SETHIGHCONTRAST)` before it starts the app with the
Theme setting following Windows, and turns it off afterwards. Hosted runners allow it and apply Windows' default
contrast theme. Where Windows refuses, the job logs "High contrast cannot be turned on here" and skips those
screenshots.

The backdrop in the screenshots is a flat colour, whether Mica over the runner's plain desktop or its fallback, so they
show layout, type, colour roles and states, not the tint a person's wallpaper gives Mica.

## Download

From the run's summary page on GitHub (Artifacts, `screenshots`), or through the API: list the run's artifacts, then
download the one named `screenshots`. It is a zip of 64 PNGs: 30 per theme for light and dark, and 4 for high contrast.
The job takes about 4 minutes after the app is published.
