# Architecture

## Sources of truth

| Source | Decides |
| --- | --- |
| [Design canvas](https://claude.ai/artifact/RuWUjBCvomq63bzaSHaCH5) | How the app looks and behaves: every screen, state, dialog and part, with handoff notes |
| [Implementation handoff](https://claude.ai/artifact/5LWgzow6CJi6U2nERZNy44) | Rules, decisions, the work plan and the definition of done |
| [Design brief](https://claude.ai/artifact/WAM66dyVJWpjNrwQcQtbUJ) | Scope, users and sample data |
| [engine](https://github.com/delphi-modernizer/engine) | What the data means: scan, trace, gates, approvals, guard, agent runs |

Where the canvas and the brief disagree, the canvas is newer and wins, and the conflict is raised in
[open-questions.md](open-questions.md).

## Projects

```text
src/
  App/DelphiModernizer.Studio.App          startup, window, shell, platform services
  Shared/
    Application.Abstractions               messaging contracts, workspace session, settings, roles, evidence links
    Mediator                               the in-process mediator
    UI.Contracts                           IModule, navigation, view model base, platform service interfaces
    UI.Common                              Studio Icons table, StatusPill, FactLabel, EvidenceLink, EmptyState, styles
  Modules/<Area>/
    DelphiModernizer.Studio.Modules.<Area>.Domain       rules of the area, when it has any
    DelphiModernizer.Studio.Modules.<Area>.Application  requests and handlers over the engine
    DelphiModernizer.Studio.Modules.<Area>.UI           pages, view models, the module's navigation items
```

The areas are Workspaces (Start, New workspace), Overview, Inventory, Workstreams (with Run), Deliverables,
Traceability, Sign-offs and Settings ([ADR 0001](adr/0001-modular-monolith-on-the-engine.md)).

## Rules the tests enforce

- Modules never reference one another.
- Domain and Application layers, the abstractions and the mediator reference no WinUI project or package.
- Only Application layers and the abstractions name the engine; a UI uses the engine's types only through them.
- Shared projects never reference a module or the App.
- Only the App references the module UIs, all of them, and its catalog creates every module.
- The solution lists every project.

Unit tests cover the Domain and Application layers, the abstractions and the mediator. The WinUI projects, the App and
every `*.UI` project, are checked by QA with the screenshot harness instead, so SonarQube Cloud leaves them out of the coverage
figure (`sonar-coverage-exclusions` in [ci.yml](../.github/workflows/ci.yml)); their issues are still analyzed and kept
at zero.

## Shell

One window with Mica and the content drawn into a 48 px title bar (Back, pane toggle, search). The navigation pane is
240 px wide, stays open down to the 960 px minimum and collapses only from the title-bar toggle. Its selection shows
as the backplate and the filled glyph. Every navigation keeps the page it leaves on the back stack with its scope, so
Back returns to the same page; opening a workspace clears the stack and lands on Overview. F6 moves between the title
bar, the pane and the page.
