---
name: gsd-phase-verify
description: "Interactive UAT verification — walk through deliverables, diagnose failures, create fix plans"
---

# Verify Work

Validate built features through conversational user acceptance testing. Walk through each deliverable one at a time, record pass/fail, diagnose failures, and create fix plans.

**Philosophy:** Show expected, ask if reality matches. User tests, Claude records.

## Invocation

```
/gsd-phase-verify [phase]
```

## Steps

### 1. Initialize

1. Parse phase number from arguments
2. Call `gsd_state_load` to load project state
3. Check for active UAT sessions (`*-UAT.md` files across phases)

**If active sessions exist and no phase specified:** Display session list with progress, let user choose to resume or start new.
**If phase specified and session exists for that phase:** Offer resume or restart.
**If no sessions and no phase:** Prompt for phase number.

**Read config:**
Read `.planning/config.json`. Extract:
- `workflow.verification_docs` — default `false` if file is absent or key is missing

Store as `$VERIFICATION_DOCS_MODE`. This flag controls Steps 3–5.

### 2. Extract Testable Deliverables

1. Call `gsd_find_phase` to locate the phase directory
2. Read all `*-SUMMARY.md` files in the phase directory
3. If VERIFICATION.md exists, incorporate its findings
4. Read PLAN.md `must_haves` and ROADMAP.md success criteria
5. Extract **user-observable** outcomes from accomplishments:
   - Focus on things the user can see, click, or interact with
   - Skip internal/non-observable items (refactors, type changes)

For each deliverable, create a test:
- **name:** Brief test name
- **expected:** What the user should see/experience (specific, observable)

### 3. Create UAT File

**If `$VERIFICATION_DOCS_MODE` is `false`** (chat mode — existing behavior):

Write `{phase_dir}/{phase_num}-UAT.md` using the [UAT template](templates/UAT.md):
- Frontmatter: `status: testing`, phase, source SUMMARY files, timestamps
- Current Test section pointing to test 1
- All tests with `result: [pending]`
- Summary with counts
- Empty Gaps section

**If `$VERIFICATION_DOCS_MODE` is `true`** (doc mode):

1. Extract all testable deliverables as in chat mode (same logic from Step 2)
2. Write `{phase_dir}/{phase_num}-UAT.md` with:
   - Frontmatter: `status: pending-review`, `mode: verification-doc`, `phase`, `started: {ISO timestamp}`
   - An intro block: "Check each test case below. Checked = pass, unchecked = fail."
   - For each deliverable, a `### Test {N}: {name}` block containing:
     - `**Expected:** {observable behavior}`
     - A single checkbox: `- [ ] Pass`
     - An empty `**Notes:**` field
   - Footer block at the end (after all tests)
3. Output to the user:
   > I've written the verification document at `{path}`. Open it, check the boxes for
   > each test that passes, then reply `submit` in chat.
4. **Stop here.** Do not continue to Steps 4–5. Yield back to the user.

### 4. Walk Through Each Deliverable

**If `$VERIFICATION_DOCS_MODE` is `true`:** Step 4 is skipped — tests are reviewed by the human in the verification document written in Step 3.

**If `$VERIFICATION_DOCS_MODE` is `false`** (chat mode — existing behavior):

```
╔══════════════════════════════════════════════════════════════╗
║  CHECKPOINT: Verification Required                           ║
╚══════════════════════════════════════════════════════════════╝

**Test {N}: {name}**

{expected behavior}

──────────────────────────────────────────────────────────────
→ Type "pass" or describe what's wrong
──────────────────────────────────────────────────────────────
```

Wait for user response (plain text, no structured prompts).

### 5. Process Responses

**If `$VERIFICATION_DOCS_MODE` is `true`:** Step 5 is skipped.

**If `$VERIFICATION_DOCS_MODE` is `false`** (chat mode — existing behavior):

| User Says | Action |
|-----------|--------|
| "yes", "y", "pass", "ok", "next", empty | Mark as **pass** |
| "skip", "can't test", "n/a" | Mark as **skipped** |
| Anything else | Mark as **issue**, infer severity |

**Severity inference** (never ask — always infer):

| User describes | Severity |
|----------------|----------|
| Crash, error, exception, fails, unusable | blocker |
| Doesn't work, nothing happens, wrong, missing | major |
| Works but..., slow, weird, minor | minor |
| Color, font, spacing, alignment, visual | cosmetic |
| Default if unclear | major |

For issues: append to Gaps section in YAML format for `/gsd-phase-plan --gaps` consumption.

Update Summary counts and `Current Test` section after each response.

### 5b. Resume from Verification Document (doc mode only)

This step runs when the human resumes after filling in the UAT.md. It processes results from doc mode.

**Trigger detection:** If the human's first message after the skill paused is any of:
- The path to the UAT file
- The word `submit` or `done`
- A paste of the file contents

Enter the resume flow below. If the user types `quit` or `cancel`, acknowledge and stop.

**Resume flow:**

1. Read `{phase_dir}/{phase_num}-UAT.md`
2. Confirm frontmatter `mode: verification-doc` is present; if absent, warn and skip to Step 6 (treat as complete chat-mode session)
3. For each `### Test {N}` block:
   - `- [x] Pass` or `- [X] Pass` → `result: pass`
   - `- [ ] Pass` (unchecked) → `result: issue`
   - Text after `**Notes:**` until next `---` → notes / failure description
   - Infer severity from notes using the same severity-inference table above
4. Update the UAT.md:
   - Change `status` to `complete`
   - Populate `## Summary` with pass/fail counts
   - Append issues to `## Gaps` in the existing YAML format
5. Commit UAT.md via `gsd_commit`
6. Continue to Step 8 (Handle Failures) using the parsed results

**Error handling — doc absent or unreadable:**
> The verification document at [path] could not be read. Please paste its contents
> directly into chat or type `quit` to cancel.

### 6. Resume Support

UAT.md is designed to survive `/clear`:
1. Read frontmatter → know phase and status
2. Read Current Test → know where we are
3. Find first `[pending]` result → continue from there

### 7. Complete Session

When all tests processed:
1. Update frontmatter: `status: complete`
2. Clear Current Test: `[testing complete]`
3. Commit UAT.md via `gsd_commit`
4. Present summary with pass/fail counts

### 8. Handle Failures

**If issues found (issues > 0):**

Automatically proceed to diagnosis:
1. Read `.github/agents/gsd-debugger.agent.md` and follow its instructions
2. For each gap in UAT.md:
   - Investigate root cause using the [diagnosis workflow](references/diagnosis-workflow.md)
   - Debug agent reads code, forms hypotheses, tests them
   - Returns root cause with evidence and files involved
3. Update UAT.md gaps with diagnosis: `root_cause`, `artifacts`, `missing`, `debug_session`
4. Update UAT status to `diagnosed`
5. Commit updated UAT.md

### 9. Create Fix Plans (for diagnosed failures)

After diagnosis:
1. Create gap-closure PLAN.md files with `gap_closure: true` in frontmatter
2. Plans target specific root causes from diagnosis
3. Each plan has targeted fix tasks + verification

### 10. Route to Next Step

| Outcome | Suggestion |
|---------|------------|
| All passed | `/gsd-phase-plan {next}` or `/gsd-milestone-audit` |
| Failures with fix plans | `/gsd-phase-execute {N} --gaps-only` |
| Undiagnosable issues | Suggest manual investigation |
