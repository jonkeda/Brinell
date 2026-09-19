# Stale readiness

Start here.

| File | What it is | Authoritative for |
| --- | --- | --- |
| [plan.md](plan.md) | Steps, status, decisions taken, baseline numbers | **what to do next, and what is done** |
| [design.md](design.md) | The accepted design: rules (R0-R9), classes, what changes for whom, open decisions | **what to build** |
| [implementation-review.md](implementation-review.md) | A review of the built code against the plan, and what was fixed | nothing; the fixes are recorded in plan.md 2.1 |
| [move-down.md](move-down.md) | First draft of the next project: moving the proven shape from MAUI to Core, stack by stack | nothing yet; a draft for its own plan |
| [background/](background/) | How we got here: analyses, the first plan and design, the review | nothing; context only |

When a background document disagrees with `design.md` or `plan.md`, **the background document is
out of date**. Each one says so at the top, and lists what changed since.

Background, in the order it was written:

1. [original-plan.md](background/original-plan.md): the problem (the toolbar Cancel), the mechanism, and the first phases.
2. [resolve-ready-element.md](background/resolve-ready-element.md): whether `ResolveReadyElement` is still needed; the log unit; the confirm budget.
3. [find-element-trace.md](background/find-element-trace.md): the lookup path, findings F1-F9.
4. [controls-design-v1.md](background/controls-design-v1.md): the first class design (controls only).
5. [scopes-analysis.md](background/scopes-analysis.md): context, root scopes, pages, containers and collections (A-E, P1-P10), with the Q1-Q10 answers.
6. [review.md](background/review.md): complexity, overengineering and flakiness value, and the outcome.
