# ADR 0002: The engine from source until its packages are published

- Status: Proposed (open question 1 of the handoff)
- Date: 2026-11-07

## Context

The studio needs the engine's libraries. They can come as project references into an engine checkout (a submodule or a
sibling folder) or as packages from GitHub Packages. No engine release exists yet, and GitHub Packages needs a token
even for public packages.

## Decision

One MSBuild property, `EngineSource`, chooses. A checkout found at `.engine/` or `../engine/` builds from source; CI
checks the engine out at the commit `engine.lock` pins. Without a checkout, packages at `EngineVersion` are used.

## Consequences

- Today's CI needs no token and can test a change that spans the engine and the studio.
- Moving to packages after the first engine release is changing the lock and a default, not the projects.
