---
mode: agent
description: "Generate tests for a completed phase based on UAT criteria and implementation"
---

Generate unit and E2E tests for a completed phase. Uses the phase's `SUMMARY.md`, `CONTEXT.md`, and `VERIFICATION.md` as specifications.

**Arguments:** `$ARGUMENTS` — phase number, e.g. `3` or `3.1`, plus optional instructions

```
Examples:
  /gsd-tests-add 3
  /gsd-tests-add 12 focus on edge cases in the pricing module
```

## Process

**Step 1 — Gather phase context**

Read:
- `.planning/phases/{NN}-*/NN-*-SUMMARY.md` — what was implemented
- `.planning/phases/{NN}-*/NN-CONTEXT.md` — user decisions and preferences
- `.planning/phases/{NN}-*/NN-VERIFICATION.md` — verification results and known edge cases

**Step 2 — Classify implementation files**

For each implementation file changed in the phase, classify as:
- **Unit (TDD)** — pure logic, utilities, services (no browser/DOM)
- **E2E** — UI components, user workflows, browser interactions
- **Skip** — config files, type declarations, auto-generated files

Present the classification table for user approval before proceeding:

```
| File | Type | Rationale |
|------|------|-----------|
| src/foo.ts | Unit | Pure utility logic |
| src/Bar.tsx | E2E | UI component |
```

**Step 3 — Present test plan**

List planned test cases with descriptions grouped by file. Get user approval before generating.

**Step 4 — Generate tests (RED phase)**

Write failing tests first. Each test must:
- Have a descriptive name stating the expected behavior
- Assert the exact outcome from the phase deliverables
- Be runnable with the project's test runner

**Step 5 — Verify GREEN**

Run the test suite and confirm all new tests pass. Report any that don't and ask how to proceed.

**Step 6 — Commit**

Commit with: `test(phase-{N}): add unit and E2E tests`

## Context

Phase: $ARGUMENTS

@.planning/STATE.md
@.planning/ROADMAP.md
