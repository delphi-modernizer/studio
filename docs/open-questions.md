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
| Short names for deliverables in sign-off checks: the canvas names D01 to D09 ("System inventory", "Technology evaluation"); D10 to D20 are new | "Data migration strategy", "Testing strategy" and the like, one place to change | `DeliverableNames` |
| The refusal's words for a check other than problems in the documents: the canvas words only "1 problem in the documents" | The check's name with its detail, as "System inventory in review (it is a draft)", one place to change | `SignOffRefusal` |
| The Traceability badge while warnings count as errors: the canvas draws the strict count on the badge (TraceabilityStrict), but the badge stands for problems in the documents everywhere else | The badge follows the page's last check, strict included, as the canvas draws it | `TraceabilityViewModel.CheckAsync` |
| The file being read during a scan: the canvas draws "Sales/CustomerForm.pas" beside "[n] of 13 files", but the engine's `ScanProgress` carries only counts | The counts and the bar, no file name, until the engine reports the file | `InventoryPage.xaml` |
| Risk marker names and order: the canvas names twelve of the scanner's 25 codes ("Hard-coded credential" first, "Global state" last); the other thirteen are new | Names in the same style ("Process launch", "INI file"), placed by kind among the drawn ones, one place to change | `InventoryWords.RiskKind` |
| The scan's time beside the scan details: the canvas draws "Scanned [time]" | The time `inventory.json` was written, with its date when not today | `InventoryViewModel.Scanned` |
| Problem wording: the canvas words three trace rules (broken evidence, missing evidence, open question); the other eleven are new | Labels and sentences in the same style, one place to change | `ProblemWordings` |
| Default window height | 760, as the brief says | `MainWindow.xaml` |
| When does the pane collapse on its own? | Never; only from the title-bar toggle | `ShellPage` |
| A run's cost and turns while it runs: the stream reports them only in its result | "—" until the run ends, then the result's numbers | `RunViewModel` |
| The budget a new run offers after one reached its own ("[higher budget]") | Twice the budget that was reached | `RunPage` |
| A task blocked by a run the studio did not start, such as `dmz run` in a terminal: the canvas words only a blocking run the studio started ("Extract business rules (Legacy discovery) is writing them") | "Run [run id], which this studio did not start, is writing them, started [time]." | `RunNames.BlockedByOtherRun` |
| Open run for a task blocked by a run the studio did not start: the canvas draws Open run beside every Blocked pill, but the Run page has no drawn state for a run in progress that the studio neither follows nor can stop | The reason without Open run; the run joins the history once it ends | `TaskRow.CanOpenBlocker` |
| The Run an agent task dialog's description: the kit has none per recipe; the canvas draws one for Extract business rules | One sentence from what the recipe writes | `RunRecipeDialog` |
| Short names for what a recipe writes ("Writes the functional requirements") | The kit's deliverable titles | `RunWords.Writes` |
| Title-bar Back from a New workspace with fields filled: the navigation model says it asks first, but no dialog is drawn | Leaves without asking; nothing is on disk before Create | `NewWorkspaceViewModel` |
| The step heading takes focus on Next, and focus also moves to the first field of each step | The heading takes focus, so it is read; Tab goes on to the first field | `NewWorkspacePage` |
| The Claude Code card's Narrator name: the handoff words only Not found | "Claude Code, [pill]. [the card's line]" for the other states | `ClaudeCard.AnnouncedName` |
| The Claude Code card's action: the keyboard notes say Get Claude Code, Update or Open Settings; the artboards draw Set up Claude Code, Test again and Agent settings | The drawn words | `ClaudeCard` |
| A settings card's control takes the card's Name: on About that would rename Copy versions | Copy versions keeps its own name | `SettingsPage` |
| Is an artboard's 1280 × 800 the window's visible frame, title bar included? | Yes: screenshots size the frame Windows draws, without the invisible resize borders | `Desktop.SetVisibleFrame` |
| The brief's sample shows G0 and D01 approved; only a person approves, so the harness's workspace has no approval | Screenshots show the freshly scanned workspace: no sign-off approved, every deliverable a template | `tests/DelphiModernizer.Studio.Screenshots` |
| Title-bar search is 240 wide under 1100, but at 960 with a workspace open WinUI's TitleBar then hides the title and subtitle | 240 without a workspace; with one, search sizes itself up to 240, which keeps the title | `ShellPage.SizeSearch` |
| The other handoff questions (stale on answered questions, settings defaults, `inventory.md`, Unknown context wording, task ownership, fan-in wording) | Taken up in the milestone that needs them | |
| The Delphi code node names the legacy system ("Acme ERP · Delphi 7"); the manifest holds only the workspace's name | The Delphi version alone | `OverviewWords.DelphiCode` |
| The Delphi code node is 160 wide, as drawn, which leaves 130 for text; in Segoe UI Variable Small at 12 px "3 screens, 1 data module" is a few pixels wider, and the canvas would wrap it into a fourth line the node has no room for | One fact a line, trimmed with an ellipsis; Narrator reads the facts whole | `OverviewPage.xaml`, `LegacyNodeButton` |
| A large codebase's "Group by folder" drop-down and its collapsible folders: the canvas draws neither the other choices nor how a folder opens | Folder headings above 50 code modules, every folder open, no drop-down | `InventoryViewModel.FilteredUnits` |
| Narrator names for rows the handoff does not name: screens, SQL statements, risk markers, diagnostics, matrix rows and sign-off headings | The row's visible columns in order, such as "TFrmMain, MainForm.dfm, Text, 3 controls, 1 event" | Each row's `AnnouncedName` |
| Sign-offs' approval panel width: the handoff says 320, the canvas stylesheet draws 336 | 320, as the handoff says | `SignOffsPage` |
| Run's keyboard order once a run has ended: the handoff lists Open the draft, then Back to Legacy discovery, but the header's Back comes first on the page | Reading order: the header's Back, Run again… on the failed InfoBar, then the end card's Open the draft and Back | `RunPage` |
| Evidence links in a run's transcript: the canvas draws them under a claim; the engine streams plain agent text | The links the kit's evidence pattern finds in each line, under that line and taken out of its text | `RunTranscript.Evidence`, `RunTranscript.Claim` |
| A tool call's summary: the canvas draws "31 lines" for Read and "2 matches" for Grep only | Matches for Grep, lines for every other tool, Done for an empty result | `RunNames.ToolSummary` |
| When does Overview show "Couldn’t read this workspace."? The canvas draws it, but a workspace that fails to open is refused at Start, and an unreadable `.modernizer/approvals.json` or run record is not caught by the page | The InfoBar shows only when the session has no workspace while the page loads, so the screenshot harness cannot reach it | `OverviewViewModel` |
| Start with Claude Code too old: the canvas draws the update dialog over Start; the card's link is Test again, which opens no dialog | Test again on Start; the dialog opens from Overview's and Workstreams' links | `ClaudeCard` |
| The approve, revoke and budget dialogs: the handoff says `DefaultButton = Close`, but WinUI draws the default button as the accent one, and the canvas draws Cancel and Close plain with the primary accent | No default button: Enter never approves or spends money, Escape closes, the primary is accent | `ApprovalDialog`, `BudgetDialog` |
| Run history's "[started] – [ended]": the canvas draws no format | The end's time alone when the run ended the day it started, else its date and time | `RunWords.EndTime` |
