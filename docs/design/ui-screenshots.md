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
5. It uploads `out/screenshots` as the artifact `screenshots`, with the app's log under `logs/`.

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
| `inventory` | One per SelectorBar section, named after it, as `code-modules` |
| `workstreams` | `default`: Claude Code is not installed on the runner |
| `deliverables` | `default` |
| `traceability` | One per SelectorBar view, as `issues` and `matrix` |
| `signoffs` | `expanded`: one sign-off open |
| `settings` | `default` |

Themes are `light`, `dark` and `highcontrast`; high contrast covers Overview and Sign-offs. Run is not captured: it
opens only for an agent run, and a fresh workspace has none; starting one needs Claude Code, which the runner does not
have.

## High contrast

The harness turns high contrast on with `SystemParametersInfo(SPI_SETHIGHCONTRAST)` before it starts the app with the
Theme setting following Windows, and turns it off afterwards. When Windows refuses, the job logs "High contrast cannot
be turned on here" and skips those screenshots.

## Download

From the run's summary page on GitHub, or through the API: list the run's artifacts, then download the one named
`screenshots`. It is a zip of the PNGs and `logs/`.
