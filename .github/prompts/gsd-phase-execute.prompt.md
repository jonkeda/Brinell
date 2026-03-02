---
mode: agent
description: "Execute phase plans with atomic commits, deviation handling, and verification"
---

Execute all plans in a phase using wave-based ordering. Each plan is implemented with atomic commits per task, automatic deviation handling, checkpoint protocols, and post-execution verification.

**Arguments:** `$ARGUMENTS` (phase number — required, plus optional flags)

Flags:
- `--gaps-only` — Execute only gap-closure plans (from verify-work fix plans)
- `--auto` — Auto-advance through checkpoints and to next phase on completion

If no phase number provided:
```
ERROR: Phase number required
Usage: /gsd-phase-execute <phase-number> [flags]
Example: /gsd-phase-execute 1
Example: /gsd-phase-execute 3 --gaps-only
```

## Process

Read and follow the complete workflow defined in `.github/skills/gsd-phase-execute/SKILL.md`.

The skill covers:

1. **Initialize** — Load state, find phase directory, discover PLAN.md files, skip completed plans
2. **Parse flags** — Handle `--gaps-only` and `--auto`
3. **Wave analysis** — Group plans by wave number, determine execution order
4. **Execute plans** — For each plan: spawn executor agent with fresh context, implement tasks with atomic commits, handle deviations, respect checkpoints
5. **Post-execution verification** — Run verifier agent, produce VERIFICATION.md
6. **Update state** — Record completion in STATE.md, update roadmap phase status
7. **Present results** — Show execution summary, suggest `/gsd-phase-verify {N}`

**After this command:** Run `/gsd-phase-verify {N}` for user acceptance testing.
