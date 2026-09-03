# Comments: what to keep, what to cut

## Where the codebase stands

Measured across `srcnew/` and `testsnew/` — 11 304 comment lines in total.

| Project | Lines | Comment lines | Density |
|---|---|---|---|
| `Brinell.Core` | 5 029 | 1 920 | **38%** |
| `Brinell.Maui` | 13 616 | 4 423 | **32%** |
| `Brinell.Maui.Extensions` | 1 145 | 253 | 22% |
| `Brinell.Wpf` | 3 348 | 659 | 19% |
| `Brinell.Stride` | 2 532 | 393 | 15% |
| `Brinell.Maui.FlaUI` | 2 232 | 331 | 14% |
| `Brinell.WinForms` | 3 360 | 456 | 13% |
| `Brinell.Html` | 2 968 | 184 | 6% |
| `Brinell.NativeAndroid` | 4 580 | 53 | 1% |
| `Brinell.Uat` / `Brinell.Presenter` | 7 584 | 9 | **0%** |

The spread is the finding. `Brinell.Maui` and `Brinell.Core` carry three to five times the
commentary of the other platform projects doing comparable work, and `Brinell.Uat` and
`Brinell.Presenter` have essentially none. Neither extreme is deliberate — it reflects which files
were worked on hardest and most recently, not which are hardest to understand.

Worst files:

| File | Comments / lines | |
|---|---|---|
| `Controls/Base/ViewBase.tpl.cs` | 283 / 770 | **36%** |
| `Context/MauiTestContext.cs` | 133 / 402 | **33%** |
| `Containers/CollectionObjectBase.cs` | 191 / 605 | 31% |
| `Controls/Range/Stepper.tpl.cs` | 132 / 498 | 26% |
| `Controls/Base/ToggleControlBase.tpl.cs` | 102 / 283 | 36% |

`Core/Interfaces/IElement.cs` is 61% comments, and that one is **fine** — it is an interface where
every member carries XML documentation. Density alone is not the measure.

## The distinction that matters

Two different things both start with a slash, and they have opposite rules.

**API documentation** — `///` on a public type or member. Describes the contract: what it does,
what the parameters mean, what it throws. Should exist on everything public. Should not mention
history, bugs, or measurements. This is most of the 11 304 lines and most of it is fine.

**Explanatory comments** — usually `//`, or a `<remarks>` block. Earns its place only by saying
something a competent reader cannot get from the code. This is where the problems are.

## What to cut

Counted repo-wide:

| Category | Instances |
|---|---|
| Narration of past fixes | 17 |
| `.my/` document pointers | 18 |
| Phase / goal / RCA references | 8 |
| Explicit run timings | 2 |
| TODO / HACK / FIXME | 5 |

### 1. Narration of past fixes

> `// No trailing Tab. It used to commit the value, but SetValue writes through the...`
> `/// Deliberately nothing more. This replaced a ladder of fallbacks — a bounding-rectangle...`
> `/// Every rung had been commented out for some time while the summary above...`

The bug is gone. A reader arriving next year needs to know what the code does and why it is
shaped that way — not what it looked like before, or who was confused by it. Rewrite forwards:
say what holds now, drop what it replaced.

### 2. Measurements

> `/// measured at 3.6 s. Stepping the container from its current position with...`
> `/// Measured across the Buttons, Text, Display and Toggle suites — 70 tests — it is never reached`

Numbers date the moment hardware, emulator or app changes. They are evidence for a decision, and
evidence belongs in `.my/`. Keep the *conclusion* in the code — "this fallback is not reached in
practice" — and leave the number where it can be re-measured.

### 3. Project-history references

> `/// goal 13, and it is the same reason phase 1 made the geometry and search helpers public.`
> `/// See <c>.my/maui/rca/rca-002-page-precondition-discarded-slow-failures.md</c>`

Phases, goals and RCA numbers are artefacts of how the work was scheduled. They mean nothing to
someone reading the class, and they rot when the plan is superseded.

**On `.my/` pointers specifically:** 18 is too many, but the answer is not zero. A pointer is
worth keeping where the reasoning is genuinely long and genuinely needed — a platform quirk with a
measured investigation behind it. Roughly one per subsystem, not one per method.

### 4. Restating the code

> `// Try the next matcher.` above `continue;`
> `// Reset the counter` above `count = 0;`

### 5. TODO / HACK / FIXME

Five of them. Either the work is worth doing — in which case it belongs in `.my/` where it will be
seen — or it is not, and the marker is noise. A TODO in source is a note to a person who has
already left.

## What to keep

**Constraints invisible in the code.** These are the comments worth their space:

- Android drops off-screen elements from the accessibility tree; Windows keeps them with
  `IsOffscreen=true`.
- A WinUI toggle advertises `LegacyIAccessible` and its `DoDefaultAction` reports success without
  changing state.
- A MAUI `ContentPage` is not a rendered view on Android, so its `AutomationId` never reaches the
  tree.

A reader cannot deduce any of these by reading harder. Without them, the code looks arbitrary and
someone will "simplify" it back into a bug.

**Ordering that looks arbitrary but is not.** Why the toggle rung comes last in the activation
ladder; why self is tried before the nested edit.

**Deliberate omissions.** Why something expected is *absent* — the missing rung, the check not
performed — since the code cannot show what is not there.

## The test

> If the comment would have to change when the bug is fixed differently, it is a changelog entry,
> not a comment.

Two supporting questions:

- Would this still be true and useful in a year, after the surrounding code is rewritten?
- Does it say something the code cannot?

## Targets

- `Brinell.Maui` and `Brinell.Core` toward the **13–19%** the other platform projects sit at —
  their subject matter is not five times harder.
- The five worst files first; `ViewBase.tpl.cs` and `MauiTestContext.cs` are also the two being
  restructured in `cleanup-scroll-and-find-architecture.md`, so do both in one pass.
- `.my/` pointers from 18 to roughly one per subsystem.
- TODO/HACK/FIXME to zero, by moving each into `.my/` or deleting it.

Not a target: `Brinell.Uat` and `Brinell.Presenter` at 0%. Nothing here argues for adding
comments to reach a number — only for removing the ones that mislead.

## Keeping it from coming back

The drift has a cause worth naming: comments were written while fixing, as a record of what had
just been understood. That is the right instinct and the wrong destination. The reasoning goes in
`.my/`; the code keeps the conclusion.

A practical rule for review: **when a comment is added in the same commit as the fix it describes,
check whether it is describing the fix or the code.** Describing the fix means it belongs in the
commit message or a `.my/` document.
