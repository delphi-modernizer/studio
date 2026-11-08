# Changelog

## Unreleased

### Added

- The shell: Mica window, title bar with Back, pane toggle and search, the navigation pane with filled glyphs and
  badges, F6 regions, a back stack that keeps each page, light, dark and high-contrast themes.
- The Studio Icons font and the app icon, built from the design's icon package.
- Shared controls: StatusPill, FactLabel, EvidenceLink, EmptyState, PageHeader and the no-workspace state.
- Every page's frame and its no-workspace state; Open folder opens a workspace and lands on Overview.
- Application layers over the engine for workspaces, inventory, overview, traceability, sign-offs and workstreams.
- Start with recent workspaces and the Claude Code card, and the Claude Code dialog.
- The New workspace wizard: name and folder, a read-only preview of the legacy source, the target platform, the
  essential context with Unknown as an answer, and creation step by step.
- Overview's flow from the Delphi code through the teams and the Principal Architect to the sign-offs, with the next
  step, open questions and recent agent runs.
- Inventory by tab, Deliverables by sign-off, Traceability with its problems and matrix, the sign-offs' checks and
  approvals, and Settings.
- Approve and revoke sign-offs from a dialog that shows your git identity, requires a note, never approves on Enter,
  and keeps the note when the engine refuses.
- Sign-offs list the files each one covers, mark those changed since approval, and show every approval and revocation.
- Workstreams lists the seven roles with their deliverables, agent tasks and run history; a task whose deliverable
  another run writes shows Blocked with the reason and a link to that run.
- Run an agent task asks for the task's scope, or the sign-off by name, and a budget, then runs it in the background.
- The Run page follows a run live, with fact labels, collapsed tool calls and guard refusals, what it writes and
  touches, Stop, and a new run when the budget is reached; the Workstreams item counts the runs in progress.
- New workspace's preview shows how many files the scan has read, with a progress bar.
- A run from an earlier session opens with its full transcript.
- A task is blocked when a run the studio did not start, such as `dmz run`, writes the same deliverable.
- The navigation pane, Settings included, is one tab stop with arrow keys inside and a rule above Settings; title-bar
  search takes its 360 px at full width.
- Status pills read their word alone and announce a change where the page asks for it; live changes are read politely,
  and only a refused approval is read at once.
- New workspace reads its step heading ("Step 2 of 5, Legacy source") on Next and Back, and each creation step as it
  starts and ends; Settings cards read their header and description, and a Claude Code test reads its result.
- Start hides Last opened under 1100 and reads the Claude Code card and recent workspaces as the handoff words them.
- Overview, Inventory, Deliverables and Traceability lay out at 960 × 576; Inventory drops its secondary columns under
  1100, and large tables scroll virtualised.
- Each table is one keyboard stop and Inventory's headers sort; Narrator reads the diagram as a group with its nodes,
  SelectorBar items, sorted headers, rows, evidence links and redacted values, and Overview names the teams whose
  agents are running.
- Sign-offs, Workstreams and Run lay out at 960 × 576 and follow the handoff's keyboard order and Narrator names; each
  sign-off's number sits on the board's tile.
- Run's transcript is one list: arrows move by item, Enter or Space expands a tool call, and Tab reaches the item's
  evidence links.
- Every brush in the shared styles and on Sign-offs is a theme resource that resolves in high contrast, where the next
  sign-off's border and hovered rows stay readable.
- Paths trim at the start in narrow columns, so the file name stays visible, and Narrator reads the whole path.
- A screenshot harness for QA captures every artboard state of the studio on a Windows PC, or in a workflow QA starts
  by hand, driven through the app, `dmz` and workspace files: light and dark at 1280 × 800 and 960 × 576, and Overview
  and Sign-offs in high contrast. A manifest pairs each screenshot with its canvas board, and the harness fails when the
  app does not start or a state cannot be reached.
- A stand-in Claude Code for the harness that replays the engine's stream-json for completed, failed, stopped,
  budget-reached and starting runs, a guard refusal included.
- CI builds the app it uploads on the engine 0.1.0 packages from GitHub Packages, and tests against the engine's
  v0.1.0 tag.

### Changed

- Info bars on every page colour their icon by severity and keep their action at the right end, beside the text.
- Start, New workspace and Settings keep one width from state to state; Start keeps its action tiles under 1100 with
  16 between the actions and Recent workspaces, and says a too-old Claude Code must be updated to run agents.
- New workspace: the target choices span the card and the chosen one has an accent border, step 4 has a Field and
  Value header with rows 16 apart, the summary labels are body size, every step shows done while creating, a step
  opens at the top of the page, and the folder box underlines in red when the folder can't be used.
- Settings: controls are 240 wide, "$" sits inside the budget box, Effort shows Claude Code's default as a
  placeholder, and an unusable claude.exe underlines the path in red with the whole message wrapped below.
- The Claude Code dialog lists what agents add with round bullets and a hanging indent.
- Title-bar search is 240 wide under 1100 when no workspace is open.
- The no-workspace state sits centred in a 520-tall area with its description at body size, and can show Clone from
  Git… turned off; empty cards keep their caption at 44 characters.
- Table rows show their dividers, the Workstreams badge is the accent fill, and a status pill can take its own glyph.
- Busy rings are the design's 12 px half arc, still when Windows turns animations off; a Starting pill joins Running.
- Fact labels are 18 tall with their whole word visible, and a path can take a colour of its own.
- High contrast draws no text backplates, so on-accent text reads on the accent.

### Fixed

- New workspace's step title no longer disappears after Next, and step 5 reads "1 field filled".
