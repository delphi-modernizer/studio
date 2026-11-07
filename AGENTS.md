# delphi-modernizer/studio: working rules

The studio is the Windows app over the engine. These rules apply to people and AI agents alike.

## How to work

1. **The design decides.** The approved design canvas decides how the app looks and behaves, the brief decides scope,
   the engine decides what the data means ([docs/ARCHITECTURE.md](docs/ARCHITECTURE.md), Sources of truth). Anything
   the design does not cover is raised as a question in [docs/open-questions.md](docs/open-questions.md), not guessed.
2. **The engine is the only source of data and rules.** Never re-implement a scan, a trace rule, a gate check or an
   approval here; propose the engine change instead.
3. **Only a person approves.** No code path leads from an agent run to an approval, and the studio never passes an
   approver name of its own.
4. **Layers.** Modules never reference one another; Application layers know nothing of WinUI; only the App composes
   modules. `tests/DelphiModernizer.Studio.ArchitectureTests` enforces it.
5. **Minimum code and surgical changes.** No abstraction for code used once; mention unrelated problems instead of
   fixing them silently.
6. **Comments** follow [docs/COMMENTS.md](docs/COMMENTS.md); style follows [docs/csharp-style.md](docs/csharp-style.md).
   Icons are Studio Icons glyphs written as escapes ([docs/icons.md](docs/icons.md)).

## Gates

All must pass before a change is done. The WinUI projects build only on Windows; on Linux the Application layers and
every test project build and run.

```bash
dotnet format DelphiModernizer.Studio.slnx --verify-no-changes --severity warn
dotnet build-server shutdown
dotnet build DelphiModernizer.Studio.slnx -c Release
dotnet test DelphiModernizer.Studio.slnx -c Release --no-build
dotnet list DelphiModernizer.Studio.slnx package --vulnerable --include-transitive
python3 scripts/build-icon-font.py && git diff --exit-code -- src
ruff format --check scripts && ruff check scripts
markdownlint-cli2
```

## Process

Gitflow and Conventional Commits, as in the organization's
[CONTRIBUTING.md](https://github.com/delphi-modernizer/.github/blob/main/CONTRIBUTING.md).
