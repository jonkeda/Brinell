# Documentation and comments

Two changes, one idea: **write down what cannot be re-derived, delete what can.**

- Code comments shrink to what ends up in a help file for someone *using* Brinell.
- A new `.docs/` holds what someone must know to *change* Brinell correctly: decisions,
  invariants, contracts. Not a prose retelling of the code - that can be regenerated from the
  code at any time, so storing it only creates something that goes stale.

Status: **todo**. Nothing here is built. One phase of it happened by hand already: 116 `<remarks>`
blocks were removed from the ten files touched by the stale-readiness second review.

## 1. The measurements this rests on

Taken 2026-09-20 across `srcnew/` (excluding `bin`, `obj`):

| | Count |
| --- | ---: |
| `<summary>` | 2573 |
| `<param>` | 1098 |
| `<inheritdoc>` | 543 |
| `<returns>` | 326 |
| **`<remarks>`** | **551** |

`<remarks>` is where the prose lives, so it is what this plan is about. Broken down:

| Kind | Visibility | Count | What happens to it |
| --- | --- | ---: | --- |
| plain prose | public | 292 | judge one by one (phase 3) |
| plain prose | internal / private | 132 | delete (phase 2) |
| plain prose | protected | 89 | delete (phase 2) |
| names a rule id or links a doc | any | **38** | **harvest into `.docs/` first** (phase 1) |

"Names a rule id or links a doc" means the block mentions `R0`-`R9`, `AD-0xx`, `Q1`-`Q10`,
`F1`-`F9`, `S1`, `X1`, `D1`, or points at `.my/`, `design.md`, `plan.md` or `docs/`. Those 38 are
the only blocks that can be carrying something not written down elsewhere. They are the real work;
the other 513 are a script.

By project: `Brinell.Maui` 225, `Brinell.Maui.FlaUI` 102, `Brinell.Uia.Contracts` 95,
`Brinell.Maui.Appium` 55, `Brinell.Maui.CommunityToolkit` 41, `Brinell.Core` 23, the rest 10.

### The finding that changes the goal

**No project sets `GenerateDocumentationFile`.** Not one `.csproj`, not `Directory.Build.props`.
So today no XML doc comment reaches a help file, a NuGet package, or any consumer that does not
have the source open. The 2573 `<summary>` blocks are in-editor decoration for people working
inside this repo.

The stated reason for keeping comments - "used in help files for other developers using Brinell
later" - is therefore **not true yet, and cannot be until this is switched on**. Phase 0 is that
switch. Without it, every comment kept is kept on a promise.

## 2. The comment policy

What stays in code:

| Keep | Where | Why |
| --- | --- | --- |
| `<summary>`, one or two lines | `public` and `protected` members of the shipped projects | this is the help file: IntelliSense for a test author who never opens the file |
| `<param>`, `<returns>`, `<exception>` | same, where the name does not already say it | same |
| `<inheritdoc />` | overrides and interface implementations | free, and it cannot drift |
| `// why`, one line, with a pointer | anywhere a decision is genuinely surprising | e.g. `// R0: an action is never repeated; see AD-009` |

What goes:

- `<remarks>` that restates what the code does. If it can be got by reading ten lines, it costs
  more than it gives.
- Any XML doc on `private` and `internal` members. Nothing consumes it.
- History and rationale in prose. That is `.docs/`'s job, where it is reviewed and correctable in
  one place.

**The drift argument, concretely.** During the stale-readiness second review the `<remarks>` on
`RunAssertWithElement` claimed the element "is found again when it turns out to be gone, or when it
was found but not visible". The code only re-found it on a stale read. The comment sat directly on
top of the most serious defect in the file and described behaviour the file did not have. 551
blocks cannot be kept true; that one was not.

## 3. `.docs/`: what it is and what it is not

**It is:** the durable layer an agent or a developer reads before changing Brinell. Decisions,
invariants, contracts, and a map of where things live.

**It is not:** a description of the code. That is the one kind of document worth *not* writing,
because it can be regenerated from the code on demand and is wrong the moment it is not.

### How the four trees divide

Adding `.docs/` to a repo that already has `docs/`, `docs2/` and `.my/` is only an improvement if
the split is obvious. Proposed:

| Tree | Holds | Reader | Lifetime |
| --- | --- | --- | --- |
| **`.docs/`** | decisions, invariants, contracts: what must stay true | someone **changing** Brinell | long; changes when a decision changes |
| `docs/` | getting started, platform guides, the control catalogue, how to run things | someone **using** Brinell | long; changes when the surface changes |
| `.my/` | plans, analyses, reviews, working notes for a piece of work in flight | whoever is doing that work | until the work lands |
| `docs2/` | archive | nobody, until it is needed | frozen |

`docs/architecture/decisions.md` (AD-001 … AD-010, 178 lines) **moves** into `.docs/` and is split
one file per decision. It does not get copied - two homes for a decision is how they diverge.
`docs/architecture/` keeps `overview.md`, `stack.md`, `structure.md` and `testing.md`, which are
orientation for users, and links across to `.docs/`.

### Shape

```
.docs/
  README.md                    the index: one line per entry, nothing else
  decisions/
    ad-001-core-stays-platform-neutral.md
    ...
    ad-010-maui-ahead-of-core.md
  contracts/
    call-model.md              one call, one budget, one log pair (R0-R9 in force)
    control-object.md          the .tpl.cs / .gen.cs contract, ActOnce, Confirm
    bridge.md                  GestureAutomation.Verbs: what an app must publish
  invariants.md                the short list that must not be broken, each linking its decision
```

A `.docs/` file is **at most one screen**. If it needs more, it is describing code and belongs
nowhere, or it is two decisions.

Every entry states: what was decided, why, what it rules out, and how to tell it has been broken.
Not how the code implements it.

### The leading dot

`.docs` is hidden on Unix listings and some editor trees, and a few tools skip dot-directories by
default. `.my/` already sets the precedent here and is tracked fine, and `git check-ignore`
confirms nothing excludes it. Worth one minute's thought before committing to it; `docs/decisions/`
would be the alternative with no downside except that the split from `docs/` is less visible.

## 4. Skills and instruction files

The distinction that matters: **a skill is a workflow you invoke; an instruction file is a rule
that is always on.** The comment policy is a rule, not a workflow, so it must not be a skill - it
would only apply when someone remembered to ask for it.

### Always-on rules

**`.github/instructions/csharp-comments.instructions.md`** (new)

```markdown
---
applyTo: "**/*.cs"
---
```

The policy from section 2, in about fifteen lines. VS Code and Copilot apply `applyTo` files
automatically to matching edits, so the rule reaches every C# change without being invoked. This
directory does not exist yet; it is additive and does not disturb `copilot-instructions.md`.

**`AGENTS.md` → "Docs Rules"** (edit). Today it says only that active docs live in `docs/` and
historical in `docs2/`. It gains the four-tree split, the comment policy in two lines, and the
rule that a decision is recorded once, in `.docs/`.

**`.github/copilot-instructions.md`** (edit). Same two lines, so Copilot and Claude Code agree.

### Workflows worth a skill

**`.github/skills/decision-record/SKILL.md`** (new) - invoked when a decision gets made, which is
exactly when nobody feels like writing it down. It should:

- decide whether this *is* a decision (it rules something out) or just a change (it does not);
- find whether an existing AD already covers it, and amend rather than add - `AD-004` and `AD-009`
  both grew this way during stale-readiness, correctly;
- write the four fields: decided, why, ruled out, how you would notice it broken;
- add the one-line pointer to `.docs/README.md`;
- say where the evidence lives (`.my/<project>/`), and not repeat it.

**`.github/skills/docs-upkeep/SKILL.md`** (new, small) - invoked when a piece of work lands. Walks
the closing checklist: does a decision need recording, does `docs/` describe a surface that moved,
does anything in `.docs/` now contradict the code, and does the `.my/` plan get marked done. This
is the step that was done by hand at the end of stale-readiness and would otherwise be forgotten.

**Not a skill:** anything that generates prose about the code. If a description of a subsystem is
ever wanted, ask for it then - it takes a few minutes from the source and is correct that day,
which a stored copy would not be.

### Existing skills that need a line

`maui-control` and `maui-ui-test` both show examples carrying `<remarks>`. They set the house style
for every control and test written from them, so they need updating in the same pass or the policy
regrows from its own documentation.

## 5. Order of work

Each phase ends green before the next starts. Verification per `Brinell/AGENTS.md`: comments cannot
change behaviour, so the bar is **the same tests passing identically**, not more of them.

**Phase 0 - make the kept comments matter.** Set `GenerateDocumentationFile` in
`srcnew/Directory.Build.props` for the shipped projects, and decide what to do with the warning
flood that follows (CS1591 on every undocumented public member). Recommended: emit the file,
suppress CS1591 for now, and treat "public member with no summary" as a later cleanup rather than a
build gate. *Without this phase the rest is decoration.* Also decide `.docs` vs `docs/decisions`
(section 3) before anything is written.

**Phase 1 - harvest the 38.** Read each decision-bearing block. Each one either is already in a
decision record (delete it, leave a `// see AD-00x` if the site is surprising) or is not (write it
into `.docs/` first, then delete). This is the phase that must not be scripted, and the only phase
where something can be lost.

**Phase 2 - script the 221 non-public blocks.** `<remarks>` on `private`, `internal` and
`protected` members, mechanically. [strip-remarks.py](strip-remarks.py), written for the second
review and used on ten files there, already does this safely: it refuses to run on an unterminated
block, preserves line endings, and leaves every other tag alone. It still needs a visibility
filter - today it takes whole files, which is why phase 3's public blocks must be judged first in
any file it is pointed at. Per project, in this order, building and running that project's tests after each:
`Brinell.Core` → `Brinell.Maui` → `Brinell.Maui.FlaUI` → `Brinell.Maui.Appium` →
`Brinell.Maui.CommunityToolkit` → the rest.

**Phase 3 - judge the 292 public blocks.** These are the only ones with a claim to being help-file
content. Per block: fold anything a *user* needs into the `<summary>`, and drop the rest. Expect
most to go. Two places deserve care rather than speed:

- **`Brinell.Uia.Contracts` (95 blocks)** is the contract the app under test implements, not
  something Brinell calls. Its comments may be the only description of the bridge protocol an app
  author has. Harvest it into `.docs/contracts/bridge.md` before touching it, and keep richer
  summaries there than elsewhere.
- **`Brinell.Core`** is what every stack sees, and the move down
  (`.my/stale-readiness/move-down.md`) is about to change it. Do `Brinell.Core` early (phase 2) but
  leave its public surface until the move down settles, or the work is done twice.

**Phase 4 - stop it regrowing.** Land the instruction file, the `AGENTS.md` and
`copilot-instructions.md` edits, and the two skills. Update the examples in `maui-control` and
`maui-ui-test`. Until this phase lands, every phase before it is temporary.

**Phase 5 - `.docs/` proper.** Move and split `decisions.md`, write `invariants.md` and the three
contract files, point `docs/architecture/` at them, and update `docs/README.md`.

## 6. Cost, honestly

Phases 0, 2, 4 and 5 are a day's work between us, most of it mechanical or writing.

Phase 3 is 292 judgement calls and phase 1 is 38 careful ones. They do not have to happen at once,
and they are the phases where rushing loses something. A reasonable version: do phases 0, 1, 2 and
4 as one piece of work - that is where the whole win is, because phase 4 stops the regrowth and
phase 2 removes two-fifths of the volume - then let phase 3 happen per file as files are touched
for other reasons.

## 7. Decisions needed before starting

1. **`.docs` or `docs/decisions`?** (section 3). Recommendation: `.docs/`, as asked, with the
   four-tree split written into `AGENTS.md` the same day so the trees do not blur.
2. **Does `decisions.md` move or stay?** Recommendation: move. Two homes diverge.
3. **Phase 0's warning policy:** suppress CS1591, or accept a documentation debt list and work it
   down? Recommendation: suppress now, revisit after phase 3, when what is left is what we meant to
   keep.
4. **Is phase 3 worth doing at all up front**, or only as files are touched? Recommendation: the
   latter, per section 6.
