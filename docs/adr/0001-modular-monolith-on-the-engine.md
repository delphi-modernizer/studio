# ADR 0001: A WinUI 3 modular monolith on the engine

- Status: Accepted
- Date: 2026-11-07

## Context

The studio is a desktop client over a workspace folder. The team has built two WinUI 3 apps on the same conventions,
kakehashi and Contr for Windows, and the handoff asks to follow Contr: a modular monolith with the engine libraries
where Contr has its daemon SDK.

## Decision

- A host app (startup, shell, platform services), a shared UI library and one module per area, each in Domain,
  Application and UI layers: Workspaces, Overview, Inventory, Workstreams, Deliverables, Traceability, Sign-offs,
  Settings. A layer exists once it holds something; every module has its UI.
- Pages talk to their module's Application layer through an in-process mediator. Application layers call the engine
  and return its `Result<T>`; they know nothing of WinUI, so they build and are tested on any OS.
- Modules never reference one another. What several need lives in the shared projects: the workspace session, the
  agent settings, the Claude Code status query, the roles.
- No server and no database: the workspace's files and git history are the record.

## Consequences

- The WinUI layer is thin and builds only on Windows; CI's Windows job is its only build evidence.
- A module can change without touching another, and the architecture tests keep it that way.
