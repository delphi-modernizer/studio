# Open questions

Questions from the [implementation handoff](https://claude.ai/artifact/5LWgzow6CJi6U2nERZNy44) and those raised while
building. Until one is answered the app does what the design assumes, behind one constant, so the answer is a small
change.

| Question | What the app does now | Where |
| --- | --- | --- |
| How does the studio take the engine: a git submodule with project references, or a package feed? | A checkout pinned by `engine.lock` (CI) or beside the repository, else packages ([BUILDING.md](BUILDING.md)) | `Directory.Build.props` |
| What does title-bar search cover? | The studio's pages by name | `ShellPage.OnSearchTextChanged` |
| Do evidence links open in the person's editor or a read-only viewer? | The app Windows associates with the file, without the line; other file types are shown in Explorer, never run | `EditorLauncher` |
| What do Workstreams, Deliverables, Traceability and Sign-offs say with no workspace open? The canvas draws only Overview and Inventory | A line saying what the page would show, in the style of the drawn two | Each page's `NoWorkspaceState` |
| Clone from Git has an action card and a link but no drawn flow | The card is shown, turned off | Start, Overview |
| The target tag for a workspace that decided to choose after discovery | "Undecided" | `TargetStackNames` |
| Browse… for the workspace folder when the folder picked already holds files | A new folder named after the workspace inside it, shown in the box before Next | `NewWorkspaceViewModel.BrowseFolder` |
| The preview's "[n] of [total] files" while scanning: the engine's scanner reports no progress | "Scanning the folder…" with an indeterminate bar | `NewWorkspacePage` |
| Problem wording: the canvas words three trace rules (broken evidence, missing evidence, open question); the other eleven are new | Labels and sentences in the same style, one place to change | `ProblemWordings` |
| Default window height | 760, as the brief says | `MainWindow.xaml` |
| When does the pane collapse on its own? | Never; only from the title-bar toggle | `ShellPage` |
| The other handoff questions (stale on answered questions, changed-file lists, revoking earlier sign-offs, settings defaults, tool calls collapsed, Run again, `inventory.md`, Unknown context wording, task ownership, fan-in wording) | Taken up in the milestone that needs them | |
