---
mode: agent
description: "Append new phase to roadmap"
---

Add a new integer phase to the end of the current milestone in the roadmap.

**Arguments:** `$ARGUMENTS` (phase description — required)

## Process

### 1. Parse Arguments

All arguments become the phase description.
- `/gsd-phase-add Add authentication` → description = "Add authentication"

If no arguments:
```
ERROR: Phase description required
Usage: /gsd-phase-add <description>
Example: /gsd-phase-add Add authentication system
```
Exit.

### 2. Initialize

Call `gsd_init_phase_op` MCP tool with phase "0".

Check `roadmap_exists`. If false:
```
ERROR: No roadmap found (.planning/ROADMAP.md)
Run /gsd-project-new to initialize.
```
Exit.

### 3. Add Phase

Call `gsd_phase_add` MCP tool with the description.

The tool handles:
- Finding highest existing integer phase number
- Calculating next phase number (max + 1)
- Generating slug from description
- Creating phase directory `.planning/phases/{NN}-{slug}/`
- Inserting phase entry into ROADMAP.md

Extract: `phase_number`, `padded`, `name`, `slug`, `directory`.

### 4. Update State

Read `.planning/STATE.md`. Under "## Accumulated Context" → "### Roadmap Evolution", add:
```
- Phase {N} added: {description}
```

Create the "Roadmap Evolution" section if it doesn't exist.

### 5. Confirm

```
Phase {N} added to current milestone:
- Description: {description}
- Directory: .planning/phases/{NN}-{slug}/
- Status: Not planned yet

Roadmap updated: .planning/ROADMAP.md

---

## ▶ Next Up

**Phase {N}: {description}**

`/gsd-phase-plan {N}`

---

Also available:
- `/gsd-phase-add <description>` — add another phase
- Review roadmap
```
