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
