---
mode: agent
description: "Analyze codebase and produce structured documentation in .planning/codebase/ (or a named subdirectory with --output <name>)"
---

Analyze existing codebase to produce structured documents in `.planning/codebase/` (or `.planning/codebase/<name>/` when `--output <name>` is given). Runs 4 sequential mapping passes (tech, architecture, quality, concerns) producing 7 documents.

**Arguments:** `$ARGUMENTS` — optional focus description and/or `--output <name>` flag

**Multi-codebase projects:** Use `--output <name>` to write each codebase map to its own subdirectory and prevent overwrites across runs. Example:
```
/gsd-codebase-map Focus on api/ --output api
/gsd-codebase-map Focus on frontend/ --output frontend
```

**Output:** `.planning/codebase/` folder (or named subdirectory) with 7 structured documents:
- `STACK.md` — Languages, runtime, frameworks, dependencies
- `INTEGRATIONS.md` — External APIs, databases, auth providers
- `ARCHITECTURE.md` — Patterns, layers, data flow, entry points
- `STRUCTURE.md` — Directory layout, key locations, naming conventions
- `CONVENTIONS.md` — Code style, naming, error handling patterns
- `TESTING.md` — Test framework, structure, mocking, coverage
- `CONCERNS.md` — Technical debt, bugs, security, fragile areas

## Process

Read and follow the complete workflow defined in `.github/skills/gsd-codebase-map/SKILL.md`.

The skill defines 6 steps:

1. **Check preconditions** — Parse `--output <name>` from `$ARGUMENTS` to set output directory; check if it already has files and offer Refresh/Update/Skip
2. **Create codebase directory** — Create output directory
3. **Execute 4 mapper focuses** — For each focus (tech, arch, quality, concerns): read the agent definition at `.github/agents/gsd-codebase-mapper.agent.md`, explore the codebase for that focus area, and write the corresponding documents using templates from `.github/skills/gsd-codebase-map/templates/codebase/`
4. **Verify all documents** — Confirm all 7 files exist and are non-empty
5. **Commit** — Use `gsd_commit` MCP tool
6. **Present completion** — Show file summary and suggest next steps

If `$ARGUMENTS` contains a focus description (after stripping `--output <name>`), pass it to each mapper focus so they concentrate on that subsystem.
