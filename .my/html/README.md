# Brinell.Html to the MAUI standard

Start here.

| File | What it is | Authoritative for |
| --- | --- | --- |
| [plan.md](plan.md) | Steps, status, baseline numbers | **what to do next, and what is done** |
| [design.md](design.md) | Rules (R0-R9), classes, what changes, open decisions | **what to build** |
| [differences-from-maui.md](differences-from-maui.md) | Where Playwright and the Blazor sample force a departure from `../stale-readiness/design.md` | **only** the deltas; everything else follows the MAUI design |
| [sample-app.md](sample-app.md) | Blazor sample updates needed to pin the design's rules with tests | The sample app changes |
| [todo-blazor.md](todo-blazor.md) | The Todo app as a Blazor head (plan steps T1, T2), and the later project of several apps in one scenario | The Todo web app; the constraints that keep multi-app scenarios possible |
| [skills.md](skills.md) | Instruction and skill files to add or update (`.github/skills/html-control`, `.github/skills/html-ui-test`, `AGENTS.md`, `copilot-instructions.md`) | The docs changes |
| [review-html-vs-maui.md](review-html-vs-maui.md) | Review after the plan: how alike Html, Blazor and MAUI are, whether the generator can make Html controls (a tested probe), and how far the UAT runs on Html | The follow-ups after the plan |

## Origin

This is the **Html** slice of the "move down" project drafted in
[`../stale-readiness/move-down.md`](../stale-readiness/move-down.md). MAUI was made right first
on its own interfaces (steps 0-9, done 2026-09-19). Html is next in the move-down order after
NativeAndroid and WPF/WinForms, but its work can be planned now because:

- Html has the same shape of bug MAUI fixed - `RunDoWithElement` retries actions
  (move-down section 6, X2), which is an R0 violation;
- Playwright is a fundamentally different driver from FlaUI/Appium, so its adaptation of the MAUI
  design needs its own document.

The MAUI design is the source of truth for the rules (R0-R9), the class shapes and the boundary
with Core. This plan **does not** re-derive them; it adopts them and notes the Html-specific
departures in [differences-from-maui.md](differences-from-maui.md).

## Scope

- **In:** `srcnew/Brinell.Html`, `srcnew/Brinell.Html.Playwright`, `srcnew/Brinell.Blazor`,
  `samples/Brinell.Samples.Blazor.App`, `testsnew/Brinell.Html.*`, `testsnew/Brinell.Blazor.*`,
  the Todo web head (`samples/Todo/src/…Web` and its tests), the two new skills.
- **Later, not here:** several apps in one scenario (MAUI and web against one backend,
  multi-user). [todo-blazor.md](todo-blazor.md) section 4 keeps it possible.
- **Out:** Core. This project does **not** change `Brinell.Core` (R9). The proven shape moves
  down to Core with the last stack, as its own project (design 4.5 of stale-readiness).
- **Out:** WPF, WinForms, NativeAndroid, Stride. Each has its own move-down slice.

## Status

Accepted and built, 2026-09-26: see `plan.md` (status, step notes, baseline). What the design
turned out to need in practice is in the step notes (`plan.md` 2.1), not back-ported into
`design.md`.
