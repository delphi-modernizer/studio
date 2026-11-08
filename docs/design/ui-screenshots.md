# Screenshots of the running app

The handoff's definition of done puts a screenshot of the running app beside every artboard. The app is WinUI 3 and
runs only on Windows, so QA runs the harness on a Windows PC, or on a GitHub Windows runner through the
[qa-screenshots](../../.github/workflows/qa-screenshots.yml) workflow, which only a person starts, and compares its
screenshots with the artboards. CI does not run it: the comparison is a person's, and a capture with nobody looking
asserts nothing. The harness fails when the app does not start, a page cannot be reached or the app logs an error.

## Options

| | FlaUI (UIA3) | WinAppDriver or Appium | The app captures itself (`RenderTargetBitmap`) |
| --- | --- | --- | --- |
| What it is | A .NET library over the UI Automation COM client; the harness is a console tool | A WebDriver server over UI Automation; Appium's Windows driver runs WinAppDriver underneath | A launch argument that makes the app walk its pages and render each into a PNG |
| Setup | In process, no server, no port; needs an interactive desktop | WinAppDriver needs Developer Mode and a server started before the tests; Appium adds Node.js and the Appium server | Needs nothing |
| Licence | MIT | WinAppDriver is a free Microsoft binary under its own licence, closed source; Appium is Apache 2.0 | — |
| Maintenance | 5.0.0 is current and supports .NET 8 and later | WinAppDriver's last stable release is 1.2.1 (2020) and the project is inactive | — |
| CVEs | None recorded for FlaUI; its transitive packages pass the NuGet audit this repository runs at `low` | None recorded for WinAppDriver; Appium brings a Node.js dependency tree to audit | None |
| Captures | The window as the screen shows it: title bar, caption buttons, the backdrop, flyouts and dialogs | The same, as a screen capture | Only the XAML tree: no Mica, no system-drawn caption buttons, and popups and dialogs live in other roots |
| Also checks | The Narrator names the pages expose, since every step finds its control by name | The same | Nothing about automation |
| Code | About 650 lines in `tests/`, nothing in the app | About the same in `tests/`, plus the server's setup | Navigation and capture code inside the app and its shell, behind a launch argument |

## Decision

FlaUI with UIA3. It captures what a person sees, title bar and Mica included, which the definition of done compares
against the artboards; it runs in process with nothing to install; and it keeps every line of the harness out of the
app. Finding each control by its automation name means a renamed control breaks the run, which is the Narrator
contract the handoff asks for anyway.

## A stand-in Claude Code

The Run page, the Workstreams states and every Claude Code state need a CLI the engine accepts, in every state on
demand, and the engine on Windows takes only a native `claude.exe` that answers `--version` with
`ClaudeExecutable.MinimumVersion` or later, then runs it with the stream-json flags.

| | The engine's FakeClaude | Real Claude Code | A stand-in `claude.exe` in `tests/` |
| --- | --- | --- | --- |
| What it is | Replays an NDJSON file; its first two arguments are that file and a file to record the rest in | The CLI, signed in with an API key from a repository secret | A console tool named `claude`, which answers `--version` and plays a scenario the harness picks through an environment variable |
| Accepted by the engine | No: `ClaudeExecutable.Locate` passes no leading arguments, so the replay file cannot be named | Yes | Yes: it is a `claude.exe` and answers `--version` |
| Every state on demand | Only with an engine change to name the script another way | No: a real run cannot be made to fail, stop or reach its budget at a chosen point | Yes: completed, failed, stopped and budget reached, started and held, and a version that is too old |
| Same pixels every run | Yes | No: the model's text, cost and turns change each run, and each run costs money | Yes |
| Engine change | Yes, in a security decision (which executables a run starts) | No | No |
| Code | Engine change and a script per state | A signed-in CLI and an API key | About 250 lines in `tests/`, nothing in the app |

Decision: the stand-in, `tests/DelphiModernizer.Studio.ClaudeStandIn`. It writes the stream-json events
`StreamJsonParser` reads (`system` init, `assistant` text and `tool_use`, `user` `tool_result`, `result`). Its events
are the Run board's: the Legacy discovery agent runs the kit's `ws01-business-rules` recipe on `legacy/Sales`, reads
`Pricing.pas`, greps for `CREDIT_LIMIT`, states fact-labelled findings with evidence links, tries to edit
`legacy/Sales/Pricing.pas` and is refused, then edits its deliverable. Nothing in a result is invented: a read returns
the file, a grep returns its matching lines, the guard's answer is the engine's own `Guard.Evaluate`, and an edit the
guard allows is made, so the run leaves the Functional Requirements Specification as Draft. The refusal is worded as
Claude Code words a blocking PreToolUse hook, which the engine reads as a `GuardRefused` event.
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

## How a run works

1. QA publishes the app, `dmz` and the stand-in as `claude.exe` ([BUILDING.md](../BUILDING.md)), then runs one screen
   group (`start`, `overview`, `pages`, `agents`) or all of them. The workflow does the same, one job per group.
2. `tests/DelphiModernizer.Studio.Screenshots` creates its workspaces with `dmz init` and `dmz scan` in a temporary
   folder, never committed, all named "Acme ERP modernization" with the target C#/.NET:
   - `sample`: the brief's Delphi 7 ERP on the BDE, the engine's `tests/fixtures/delphi/legacy-erp`, scanned. Two lines
     added to the system inventory give it the design's two problems in the documents: a reference to `FR-999`, which
     is defined nowhere, and evidence in a file that does not exist.
   - `clean`: the same sample with all six context answers, scanned; it has no problem at all.
   - `unscanned`: the sample, not scanned.
   - `large`: 10,000 generated units in 100 folders, scanned, without git so that it is quick to create.
3. It launches the app once per configuration, with a fresh copy of each workspace it lists. It writes the app's own
   settings before each launch, since the app reads them once: the recent workspaces, the Theme setting and the
   Claude Code path. It sets the stand-in's variables and `GIT_CONFIG_GLOBAL` (a file with the screenshots identity,
   or an empty one) on the app's process. The person's data folder is moved aside for the run and put back afterwards.
4. Each state is reached as a person reaches it: through the UI (opening a workspace from Start, the pane, buttons,
   SelectorBar items, text boxes, dialogs), through the stand-in's runs, or through the workspace's files (a deleted
   deliverable, a charter set back to a template). States that last only while something runs (loading, scanning,
   preview running, creating) are triggered once per size and captured as soon as they show; the large tree makes them
   last long enough.
5. It sizes the window with `SetWindowPos` before each capture and writes the PNGs and `manifest.json` to `--out`,
   with the app's log under `logs/` when it wrote one. The workflow uploads that folder as `screenshots-<group>`.

No step approves. The approve dialog and the no-identity dialog are opened on Project setup, captured and cancelled,
and the harness fails if `.modernizer/approvals.json` changed.

The window size is the size of its visible frame, title bar included, without the invisible resize borders: a
1280 × 800 capture is a 1280 × 800 PNG. The harness sets the display to 1920 × 1080 first, at 100 % scale, and puts
it back afterwards.

## Files

`<page>-<state>-<width>x<height>-<theme>.png`, for example `signoffs-blocked-960x576-dark.png`, at 1280 × 800 and
960 × 576 in `light` and `dark`. `manifest.json` lists every PNG with its page, state and theme, the canvas board it
pairs with (`project/<Board>.dc.html`) and that artboard's label as the canvas names it. A state with an artboard of its
own pairs with that file at every size and theme; a page's default state, and a state no artboard draws (Inventory's
Cycles and Diagnostics), pair with the page's main boards: light, dark, `Min`, `MinDark` and `HC`.

| Group | Page | States |
| --- | --- | --- |
| `start` | `start` | `default`, `first-run`, `claude-missing`, `claude-missing-dialog`, `claude-too-old` |
| `start` | `newworkspace` | `step1`, `folder-not-empty`, `step2` (preview done), `preview-failed`, `preview-running`, `step3`, `step4`, `step5`, `creating`, `created` |
| `start` | `settings` | `found`, `not-found`, `script-shim` |
| `start` | `workstreams` | `claude-update-dialog`: the dialog for a Claude Code that is too old |
| `overview` | `overview` | `no-workspace`, `no-scan`, `default`, `hover`, `focus`, `scrolled` (960 × 576 only), `pane-compact`, `running`, `no-claude` |
| `overview` | `shared` | `title-bar` (1280 × 48) and `nav-pane` (240 × 752), cut from a 1280 × 800 Overview |
| `overview` | `signoffs` | `blocked` (Discovery expanded), `approve-dialog`, `no-identity` |
| `pages` | `inventory` | `no-workspace`, one per section (`code-modules`, `screens`, `data-and-libraries`, `sql-and-risks`, `cycles`, `diagnostics`), `not-scanned`, `large`, `scanning` |
| `pages` | `traceability` | `issues`, `errors-only`, `strict`, `matrix`, `clean` |
| `pages` | `deliverables` | `all`, `draft`, `missing`, `templates` |
| `pages` | `overview` | `loading`, on the large workspace |
| `agents` | `workstreams` | `default`, `run-dialog`, `blocked`, `history`, `no-claude`, `claude-dialog` |
| `agents` | `run` | `running`, `stopped`, `starting`, `completed`, `failed`, `budget-dialog` |

High contrast (`highcontrast`) covers Overview `default` and Sign-offs `blocked`, in the `overview` group.

### Not captured

| Artboard | Why |
| --- | --- |
| Sign-offs · Discovery ready, Approval refused, Discovery approved, Revoke dialog, Project setup stale, Approve the final plan; Overview · Everything approved | Each needs a recorded approval, and Discovery ready needs Project setup approved first. Only a person approves; the harness records none |
| Sign-offs · High contrast · Discovery approved | The same; high contrast captures Discovery blocked |
| Overview · Workspace can't be read | The app shows it only when the session loses its workspace while the page is open, which no person's action causes (open questions) |
| Start · Claude Code too old dialog | Start offers Test again when Claude Code is too old and opens no dialog; the same dialog is captured from Workstreams |

## High contrast and Mica

The harness turns high contrast on with `SystemParametersInfo(SPI_SETHIGHCONTRAST)` before it starts the app with the
Theme setting following Windows, and turns it off afterwards; Windows applies its default contrast theme. Where
Windows refuses, the harness logs "High contrast cannot be turned on here" and skips those screenshots.

Mica takes its tint from the wallpaper, so the backdrop differs between PCs and is flat on a runner's plain desktop;
compare layout, type, colour roles and states, not the tint.

## Download

From the workflow run's summary page on GitHub (Artifacts, `screenshots-<group>`), or through the API: list the run's
artifacts, then download each one named `screenshots-` and its group. Each is a zip of PNGs and its `manifest.json`.
