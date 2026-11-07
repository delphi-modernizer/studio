# Delphi Modernizer Studio

The Windows app for a governed modernization of a legacy Delphi codebase to C#/.NET or Java/JVM. It scans the Delphi
code, lets six teams and their optional AI agents draft the 20 deliverables of the plan, checks the documents for
traceability, and records **people's** sign-offs. Building the new system stays locked until a person signs off the
final plan.

The studio is a client over a workspace folder: all data and rules come from the
[engine](https://github.com/delphi-modernizer/engine), which embeds the [kit](https://github.com/delphi-modernizer/kit).

## Status

Built milestone by milestone against the approved design (see [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)):

| Milestone | Scope | State |
| --- | --- | --- |
| 1 Shell | Solution, engine wiring, Mica, themes, title bar, navigation pane with badges, page frame, Studio Icons, app icon, shared controls | Done |
| 2 Without AI | Start, New workspace, Overview, Inventory, Deliverables, Traceability, Sign-offs (read), Settings | Done |
| 3 Sign-offs | Approve and revoke dialogs, git identity, refusal, stale approvals, history | In progress |
| 4 Agent runs | Workstreams, the Run an agent task dialog, the live Run page, budget and Claude Code dialogs | In progress |
| 5 Finish | Narrow layouts, high contrast, keyboard order and Narrator names, a full pass against every artboard | Next |

## Build

Windows 10 1809 or later, the .NET SDK that [global.json](global.json) names, and the engine checked out beside this
repository (`../engine`, with its `kit` submodule). Details and the package-feed alternative:
[docs/BUILDING.md](docs/BUILDING.md).

```powershell
dotnet build DelphiModernizer.Studio.slnx -c Release
dotnet test DelphiModernizer.Studio.slnx -c Release --no-build
```

CI builds the app on Windows and uploads it, self-contained with `dmz` beside it, as the
`delphi-modernizer-studio-win-x64` artifact.

## Licence

MIT. The icons come from Fluent UI System Icons (MIT); see [NOTICE](NOTICE).
