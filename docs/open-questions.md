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
| Short names for deliverables in sign-off checks: the canvas names D01 to D09 ("System inventory", "Technology evaluation"); D10 to D20 are new | "Data migration strategy", "Testing strategy" and the like, one place to change | `SignOffWordings` |
| The refusal's words for a check other than problems in the documents: the canvas words only "1 problem in the documents" | The check's name with its detail, as "System inventory in review (it is a draft)", one place to change | `SignOffRefusal` |
| The preview's "[n] of [total] files" while scanning: the engine's scanner reports no progress | "Scanning the folder…" with an indeterminate bar | `NewWorkspacePage` |
| Problem wording: the canvas words three trace rules (broken evidence, missing evidence, open question); the other eleven are new | Labels and sentences in the same style, one place to change | `ProblemWordings` |
| Default window height | 760, as the brief says | `MainWindow.xaml` |
| When does the pane collapse on its own? | Never; only from the title-bar toggle | `ShellPage` |
| Guard refusals: the engine streams a refused tool call as a failed tool result, with no event of its own | A failed result whose text carries Claude Code's hook or permission wording reads as a refusal | `RunTranscript` |
| A run's cost and turns while it runs: the stream reports them only in its result | "—" until the run ends, then the result's numbers | `RunViewModel` |
| A run from an earlier session: the engine's transcript parser is internal, so the studio cannot replay the recorded `.jsonl` | The summary the run store keeps, and the transcript file with Open in editor | `RunViewModel.ShowRecordAsync` |
| The budget a new run offers after one reached its own ("[higher budget]") | Twice the budget that was reached | `RunPage` |
| Blocked by a run the studio did not start, such as `dmz run` in a terminal: the engine's leases name no holder publicly | Run… is offered; the engine refuses the lease and the dialog shows its reason | `StartRunCommand` |
| The Run an agent task dialog's description: the kit has none per recipe; the canvas draws one for Extract business rules | One sentence from what the recipe writes | `RunRecipeDialog` |
| Short names for what a recipe writes ("Writes the functional requirements") | The kit's deliverable titles | `RunWords.Writes` |
| Is an artboard's 1280 × 800 the window's visible frame, title bar included? | Yes: screenshots size the frame Windows draws, without the invisible resize borders | `Desktop.SetVisibleFrame` |
| The brief's sample shows G0 and D01 approved; only a person approves, so the CI workspace has no approval | Screenshots show the freshly scanned workspace: no sign-off approved, every deliverable a template | `ci.yml`, `screenshots` job |
| The other handoff questions (stale on answered questions, settings defaults, `inventory.md`, Unknown context wording, task ownership, fan-in wording) | Taken up in the milestone that needs them | |
| Sign-offs' approval panel width: the handoff says 320, the canvas stylesheet draws 336 | 320, as the handoff says | `SignOffsPage` |
| Run's keyboard order once a run has ended: the handoff lists Open the draft, then Back to Legacy discovery, but the header's Back comes first on the page | Reading order: the header's Back, Run again… on the failed InfoBar, then the end card's Open the draft and Back | `RunPage` |
| Evidence links in a run's transcript: the canvas draws them under a claim; the engine streams plain agent text | The links the kit's evidence pattern finds in each line, under that line | `RunTranscript.Evidence` |
| A tool call's summary: the canvas draws "31 lines" for Read and "2 matches" for Grep only | Matches for Grep, lines for every other tool, Done for an empty result | `RunNames.ToolSummary` |
