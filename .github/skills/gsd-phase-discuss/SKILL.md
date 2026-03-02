---
name: gsd-phase-discuss
description: "Interactive discussion to gather user preferences and decisions for a phase — produces CONTEXT.md"
---

# /gsd-phase-discuss

Gather implementation decisions through adaptive questioning before planning. Produces CONTEXT.md that downstream agents (researcher, planner) use to know what to investigate and what choices are locked.

**This skill does NOT delegate to a sub-agent.** It drives the questioning flow itself.

## Invocation

```
/gsd-phase-discuss <phase-number>
```

- Phase number is required
- `$ARGUMENTS` contains the phase number

## Process

### Step 1: Parse Arguments

Extract phase number from `$ARGUMENTS`. Error if missing.

### Step 2: Validate Phase Exists

```
Call gsd_find_phase with the phase number.
```

If phase not found:
```
Phase [X] not found in roadmap.
Use /gsd-progress to see available phases.
```
Stop.

### Step 2.5: Read Config

Read `.planning/config.json`. Extract:

- `workflow.question_docs` — default `false` if file is absent or key is missing

Store as `$QUESTION_DOCS_MODE`. This flag controls Steps 6 and 7.

### Step 3: Check Existing Context

If CONTEXT.md already exists in the phase directory, use `manage_todo_list` to ask:
- **Update it** — Review and revise existing context
- **View it** — Show what's there, then offer update/skip
- **Skip** — Use existing context as-is (exit)

If no CONTEXT.md but plans already exist, warn the user:
> Phase [X] already has [N] plan(s) created without user context. Your decisions here won't affect existing plans unless you replan.

Offer: "Continue and replan after" / "View existing plans" / "Cancel"

### Step 4: Load Phase Context

Call `gsd_roadmap_get_phase` for the phase number. Extract:
- Phase name and goal
- Phase boundary (what it delivers)
- Dependencies
- Success criteria

### Step 5: Analyze Domain and Generate Gray Areas

Based on the phase goal, determine what kind of thing is being built and identify 3-4 **phase-specific** gray areas (not generic categories).

**Domain heuristics:**
- Something users **SEE** → layout, density, interactions, empty states, responsive behavior
- Something users **CALL** → response format, pagination, error handling, caching, auth
- Something users **RUN** → output format, flags, modes, error handling, progress
- Something users **READ** → structure, tone, depth, flow, navigation
- Something being **ORGANIZED** → criteria, grouping, naming, exceptions

**Generate concrete gray areas, not abstract labels:**
```
Phase: "User authentication"
→ Session handling, Error responses, Multi-device policy, Recovery flow

Phase: "CLI for database backups"
→ Output format, Flag design, Progress reporting, Error recovery
```

**Key question:** What decisions would change the outcome that the user should weigh in on?

**Do NOT ask about** (Claude handles these):
- Technical implementation details
- Architecture patterns
- Performance optimization
- Scope (roadmap defines this)

### Step 6: Present Gray Areas

**If `$QUESTION_DOCS_MODE` is `false`** (inline chat mode — existing behavior):

State the domain boundary first:
```
Phase [X]: [Name]
Domain: [What this phase delivers]

We'll clarify HOW to implement this.
(New capabilities belong in other phases.)
```

Then present gray areas as a multi-select list. Each option has a concrete label and 1-2 questions it covers. **No "skip" or "you decide" option** — user ran this command to discuss.

**If `$QUESTION_DOCS_MODE` is `true`** (question-doc mode):

1. Determine the set of gray areas using the domain heuristics from Step 5 (generate 3–4 phase-specific areas)
2. For each gray area, generate 4 targeted questions (the same questions you would ask in deep-dive interactive mode)
3. Write `.planning/phases/{NN}-{name}/{NN}-QUESTIONS.md` using the template at `.github/skills/gsd-phase-discuss/templates/question-doc.md`:
   - Set frontmatter: `phase: "{NN}"`, `round: 1`, `status: pending`, `skill: "gsd-phase-discuss"`, `created: "{ISO timestamp}"`
   - Use `## {Area Name}` headings to group questions by gray area
   - For each question: pre-check the most likely answer with `[x]`, include 2–3 options
   - Always include an empty `**Notes:**` field below each question's checkboxes
   - Append the footer block verbatim at the end of the document:
     ```
     ---
     **What to do next:** Edit the checkboxes above, then reply in chat:
     - Type `submit` or paste this file's path to process your answers (follow-up questions may follow)
     - Type `next` to skip follow-up questions and go straight to the next step
     - Type `quit` to cancel
     ```
4. Output to the user:
   > I've written the question document at `.planning/phases/{NN}-{name}/{NN}-QUESTIONS.md`.
   > Open it, edit the checkboxes, then reply `submit` in chat.
5. **Stop here.** Do not continue to Step 7. Yield back to the user.

### Step 7: Deep-Dive Each Selected Area

**If `$QUESTION_DOCS_MODE` is `false`** (inline chat mode — existing behavior):

For each selected area:

1. **Announce the area:** "Let's talk about [Area]."
2. **Ask 4 targeted questions** — each with 2-3 concrete options plus "You decide" where reasonable
3. **After 4 questions, check:** "More questions about [area], or move to next?"
   - If "More" → ask 4 more, check again
   - If "Next" → proceed to next selected area
4. **Record each answer as a locked decision**

After all initially-selected areas complete:
- Summarize what was captured
- Ask: "We've discussed [list]. Which gray areas remain unclear?"
- Options: "Explore more gray areas" / "I'm ready for context"
- If "Explore more" → identify 2-4 additional areas, loop back

**Scope creep handling:** If user mentions something outside the phase domain:
> "[Feature] sounds like a new capability — that belongs in its own phase. I'll note it as a deferred idea. Back to [current area]..."

Track deferred ideas internally.

**If `$QUESTION_DOCS_MODE` is `true`:** Step 7 is skipped — all questions were written in the question document during Step 6.

### Step 7b: Resume from Question Document (question-doc mode only)

This step runs when the human resumes after editing a question doc. It replaces the deep-dive loop for question-doc mode.

**Trigger detection:** If the human's first message after the skill paused is any of:
- The path to the question doc (e.g. `.planning/phases/04-.../04-QUESTIONS.md`)
- The word `submit` or `done`
- A paste of the file contents

Enter the resume flow below.

**Special keywords:**
- `quit`, `cancel`, or `stop` → acknowledge and stop; leave the question doc on disk as-is
- `next` or `continue` → skip writing a follow-up doc; use all pre-checked recommendations as the answers; note assumptions explicitly; jump directly to Step 8

**Resume flow:**

1. Read `.planning/phases/{NN}-{name}/{NN}-QUESTIONS.md`
2. For each `### Q{N}` block:
   - Lines matching `- [x]` or `- [X]` → selected answers
   - Text after `**Notes:**` until the next `---` → free-text answer
   - If no boxes checked → record `AI recommendation assumed: {pre-checked option text}`
3. Build a decisions map: `{ question: [selected options], notes: text }`
4. Assess completeness: can all key decisions be derived from the answers?
   - **Sufficient** → proceed to Step 8 (Write CONTEXT.md)
   - **Insufficient** (key decisions cannot be resolved):
     1. Write follow-up doc: `.planning/phases/{NN}-{name}/{NN}-QUESTIONS-2.md`
        (frontmatter `round: 2`, `status: pending`; include only unresolved questions)
     2. Output location to user and stop (yield back)

**Error handling — doc absent or unreadable:**
> The question document at [path] could not be read. Please paste its contents directly
> into chat, or type `quit` to cancel.

### Step 8: Write CONTEXT.md

Create the file using the `gsd_scaffold` MCP tool, then fill in the content:

1. Call `gsd_scaffold` with `type: "context"` and `phase: {phase_num}` — this creates `{phase_dir}/{padded_phase}-CONTEXT.md` with correct YAML frontmatter
2. Edit the scaffolded file to add the full content based on the template at `.github/skills/gsd-phase-discuss/templates/context.md`

Include:
- Phase boundary from ROADMAP.md (fixed, not negotiable)
- All locked decisions organized by discussed area
- Claude's Discretion section (areas where user said "you decide")
- Specific Ideas (references, "I want it like X" moments)
- Deferred Ideas (scope creep captured but not acted on)

### Step 9: Commit

```
Call gsd_commit with message: "docs({phase_num}): gather phase context"
Include the CONTEXT.md file.
```

### Step 10: Suggest Next Steps

```
## ▶ Next Up

**Phase [N]: [Name]** — [Goal from ROADMAP.md]

`/gsd-phase-research [N]` — research domain before planning
`/gsd-phase-plan [N]` — skip research, plan directly
```

## Key Principles

- **User = visionary, Claude = builder.** Ask about vision and preferences, not technical details.
- **Phase boundary is FIXED.** Discussion clarifies HOW, not WHETHER to add more.
- **Decisions must be specific.** "Card-based layout with author, timestamp, content" not "some kind of layout."
- **Downstream awareness:** CONTEXT.md feeds researcher (what to investigate) and planner (what's locked).
