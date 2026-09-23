# Stale readiness: complexity review

> **Background.** The outcome box below records what was decided. Its tier recommendation
> (section 4) was **not** adopted; [../plan.md](../plan.md) follows the full design.

A critical look at [design.md](../design.md) and the documents behind it. It asks
three questions:

1. How complex is it?
2. Is it overengineered?
3. Is it a good way to fight flaky tests?

Status: review, 2026-09-19. Estimates are marked as estimates. Everything else was measured in
the repo or taken from recorded runs.

> **Outcome (2026-09-19):**
>
> - Consistency, diagnostics, the Core base interfaces and scope readiness are **wanted**, so
>   nothing marked "defer" is deferred.
> - Later the same day, the base interfaces and bridges were **dropped**. MAUI gets its own
>   interfaces, Core is not changed, and the shape moves down later (design R9). That also
>   removes the review's main complexity objection: step 0b no longer touches every stack.
> - The two "drop" items are removed (`Deadline` is internal; there is no `Carry`).
> - The full diagnostics are kept.
> - Section 3.4's point 4 becomes rule R0 in [design.md](../design.md): app bugs stay
>   failures, and near-misses are reported. Section 3.4's point 1 (measuring) is in its step 1.

---

## 1. How complex is it

### 1.1 Size

| Measure | Count |
| --- | --- |
| Core MAUI code in scope (`ViewBase.tpl.cs`, `Containers/*`, `Pages/*`, `MauiTestContext`, `ObjectBase`) | ~3,500 lines |
| New types | ~22: 4 base interfaces; 3 exceptions; `ScopeReadiness` + enum; `Deadline`; `ItemKey` + enum; `ControlCall`, `Poller`, `AttemptContext`, `Observation` + enum, `ObservationLog`; `Confirmation` + enum |
| Members removed or renamed | ~30 (section 10 of the design) |
| Classes that change behaviour | `ViewBase`, `RootedScopeBase`, `PageObjectBase`, `ContainerObjectBase`, `CollectionObjectBase`, `ItemObjectBase`, `AppRoot`, `MauiTestContext`, both drivers, 6 special containers, 8 act-then-confirm controls, and shared Core code |
| Mechanical edits | ~95 mocked find setups; ~10 test-page `IsLoaded` overrides; 4 skill files; the rename |
| Build steps | 10 (0, 0b, 1-8) |
| Tests that must stay green | 131 unit test methods in `Brinell.Maui.Tests`; about 480 UI test methods (`Brinell.Maui.UITests` + Todo), which take minutes to hours to run; plus every other stack's tests for step 0b |
| Estimated diff | 3,000-5,000 lines, excluding the mechanical edits (**estimate**) |

### 1.2 Where the complexity comes from

The work grew one document at a time, and each step was sound on its own:

| Document | Question asked | What it added |
| --- | --- | --- |
| plan.md | Why does the toolbar Cancel fail one run in two? | nested budget, stale signal |
| resolve-ready-element.md | Do we still need `ResolveReadyElement`? | log unit, confirm budget |
| find-element-trace.md | How do lookups work? | the context's wait, sweeps, lookup shape |
| scopes-analysis.md | Do scopes fit together? | readiness chain, root rules, collections |
| overall-design.md, section 4.1 | How do we avoid breaking other stacks? | Core base interfaces and bridges |

Added together, this is **a rewrite of the framework's core**: the call model, the readiness
model, the lookup model and the Core interfaces. The problem that started it was one control
waiting on a dead element.

### 1.3 Concepts a contributor must learn

Today a contributor learns about:

- the `Run*` helpers;
- `Until`;
- page readiness;
- `Poll`.

After the change they learn about:

- `ControlCall`, `Poller`, `AttemptContext` and `Observation`;
- `ProbeReadiness` and `AsksParent`;
- `ProbeContentReadiness`;
- `Confirm`;
- `ItemKey`;
- `InstanceKey` and `Live`;
- base interfaces versus old interfaces.

The new model is more consistent, but it is **not smaller**. A control author writing an ordinary
control never sees most of it, which is what the generator and the unchanged `Run*` names buy. A
container, collection or driver author sees all of it.

---

## 2. Is it overengineered?

Each piece was checked against two things: **evidence** (a measured failure, or a bug the code
plainly has) and **cost**.

### 2.1 The pieces, sorted

| Piece | Evidence | Cost | Verdict |
| --- | --- | --- | --- |
| `EnsureVisible` as one attempt (no nested budget) | **Measured**: Todo Cancel failed one run in two | small (one method, 4 call sites) | **Needed** |
| A Brinell stale signal on FlaUI (11 catches that never fire on Windows) | **Code fact**: FlaUI never raises Selenium's type | medium (one helper per driver, 11 catches) | **Needed** |
| `MauiTestContext.FindElement` as one attempt, and `TryFindElement` no longer catching everything (F1, F4) | **Code fact**: 3 s hidden wait on the toolbar's own path; errors read as "absent" | small | **Needed** |
| Action inside the log unit | **Code fact**: an exit line that says `Success` when the action throws | small | **Needed** (diagnostics) |
| FlaUI driver `while` bug (F7) | **Code fact**, latent | tiny | **Needed** (fix the loop only) |
| Collection budgets: one deadline through `TryMaterializeMore` (D1, D2) | **Code fact**: nested `DefaultWait` polls; `timeoutMs` ignored; `ScrollToTop` unbounded | medium if done with a deadline parameter; large if done as `MaterializeAttempt` | **Needed, the small way** |
| Failure messages from the last observation | No failure seen, but diagnosis time *is* the cost of flakiness | medium (`ObservationLog`) | **Worth it, simplified**: keep "last observation plus replacement count", drop the 9-way exception mapping |
| One loop instead of four (`Poller`) | Code smell; four slightly different loops | medium | **Worth it**, but only as a refactor inside the existing helpers, not as a new public concept |
| `ItemKey` (a row never acts on another item) | **No failure seen.** The risk is real: recycling is documented in `FindItem`'s own comment | medium | **Defer.** Write the phase 0 test first; build it only if the test shows the problem on a real list |
| Scope readiness chain (`ProbeReadiness`, `AsksParent`, `ProbeContentReadiness`) | **Nothing measured.** A3 (dialog on a busy page) is unverified. Nothing overrides `WaitContentReadyCore` | large (every scope, the gate, Core types) | **Overengineered for now.** Verify A3. If it is real, the minimal fix is "`ContentDialog` has no page", as `Popup` already does |
| Scope members moved onto the call model (Q8) | Consistency; no failure seen | large | **Defer**, except where D1/D2 already touch them |
| Root "alive" rule and `InstanceKey` | The plan's phase 4 question is still unmeasured | medium | **Measure first.** Needed only if a dead root passes `TagName` on FlaUI |
| `Confirm` with three results | Plan 3.4; nothing measured beyond toolbar items, which confirm nothing | small-medium | **Simplify**: let `Until` treat `StaleElementException` as "not confirmed" and word the message. No new type |
| Core base interfaces and bridges (`IElementBase` and the others) | **None.** They fix no flakiness. They exist only to let MAUI drop timeout overloads it can simply stop calling | large, and it touches **every stack** in step 0b | **Overengineered for this goal.** A good idea for the later platform refactor; out of this work |
| Renames (`ContainerRoot` → `Root` and similar), removing `DriverRootScope`, `Q10` waiting semantics | Tidiness; Q10 is a behaviour change without a failure behind it | small each, but they add up and churn the diff | **Defer.** `ItemObjectBase` is the exception: you asked for it, and it is cheap on its own |
| Public `Deadline` for a possible "option A" | Speculative | small | **Drop.** Keep it internal |
| `AttemptContext.Carry` slots | Needed only by `MaterializeAttempt` | small | **Drop** with `MaterializeAttempt` |

### 2.2 The answer

**Yes, as a whole it is overengineered for the problem it set out to solve.**

About a third of it addresses measured failures or plain code bugs. Another third improves
consistency and diagnostics, which is valuable but was not asked for. The last third (Core base
interfaces, the scope readiness chain, item keys, renames) prepares for problems nobody has
observed yet.

The design is not *wrong*. Its model is coherent, and section 3.2 shows it converges on a proven
one. It is too much **at once**, and too much **before measuring**. Its own build order says
"measure first" (step 1), yet it decides the answers before those measurements exist.

A sign it has gone too far: to change how MAUI resolves one element, **step 0b builds and
re-tests every stack in the solution**.

---

## 3. Is this a good way to fight flaky tests?

### 3.1 What flakiness this repo actually has

From the recorded runs and notes:

| Source | Example | Does the design address it? |
| --- | --- | --- |
| **Framework race**: an element replaced during a wait | Todo Cancel, one run in two (TodoApp plan, phase 4) | **Yes**: the core of the plan |
| **App bugs** that show up as flaky tests | `CanExecuteChanged` raised before the task was set ("the second Cancel does nothing") | **No, and it should not.** These must keep failing |
| **Environment**: other topmost windows | occluded-screenshot test fails its cover check | No |
| **Environment**: two UI test processes at once | failures "are fictional and runs stall for an hour" | No |
| **Infrastructure**: an orphaned Appium session | UiAutomator2 force-stopped 300 s into the next run | No |
| **Infrastructure**: emulator stuck offline | needs `-no-snapshot` | No |
| **Driver gaps**: Android | Range 6/16 and Picker 0/8 fail before any change | No: these fail every time, so they are not flaky, just unsupported |
| **Pre-existing**: WPF | 2 Login/IsBusy tests fail with and without fixes | No |

**One of the eight known sources is what the design is built around.** Most of this repo's lost
time went to environment and infrastructure problems. A better readiness model does not touch
them.

### 3.2 Is the approach sound for the flakiness it does target?

**Yes. It is the model the mature tools use:**

- **Find again on every attempt; never hold an element across waits.** Playwright locators are
  lazy and resolved per action. Selenium's guidance on stale elements says the same.
- **Actionability checks inside one retry loop.** Playwright's auto-wait checks visible, stable,
  enabled and receiving events, all within the action's one timeout, never nested.
- **Never retry an action; retry only the lookup and readiness around it.** Playwright retries
  only up to the point of acting.
- **Say what was last observed.** Playwright's timeout errors list the "call log" of what each
  attempt saw.

So the *direction* is right, and the core of it (one loop, one budget, find again each attempt,
act once, a named stale signal, a useful message) is exactly what those tools converged on.

### 3.3 Risks of the approach, as flakiness policy

- **Retrying can hide app bugs.** Every extra "wait until ready" is a place where a real defect
  (a command that never re-enables, a list that re-renders every second) turns into a slow pass
  instead of a failure. The design limits this by never repeating actions, and by failing on
  `ItemChanged` rather than retrying through it. The chain readiness and the wider waiting (Q10)
  push the other way.
- **There is no flake measurement.** The only rate on record is "one run in two", for a single
  test. Without a repeat-run tool and a failure classification (framework race, app bug,
  environment, driver gap), no one can say whether a change reduced flakiness, or which kind.
- **Proving it is expensive.** The UI suites take minutes to hours, must run one process at a time,
  and Android starts from a failing baseline. A large behaviour change across about 480 UI tests is
  hard to attribute. A small one can be measured.

### 3.4 What would fight flakiness better

In order of return on effort:

1. **Measure.** A repeat-run script: run a suite N times, collect the failures, classify each one.
   This gives a flake rate per test and per cause, and makes every later change checkable.
2. **Harden the environment.** These are already known from memory, just not automated:
   - a pre-flight check that no other UI test process is running;
   - an Appium restart after a killed run;
   - an emulator started with `-no-snapshot`;
   - no other topmost windows during the occlusion test.

   That covers four of the eight sources.
3. **Fix the measured framework race, narrowly.** This is "plan-lite" in section 4.
4. **Keep app bugs failing.** The Todo `CanExecuteChanged` bug was found *because* a test failed.
   The framework should make such failures clear, never absorb them.
5. **Then, with numbers**, decide whether the deferred pieces (the scope chain, item keys, the
   rest of the call model) are worth building.

---

## 4. Recommendation: split the design into tiers

| Tier | Contents | Why | Rough size |
| --- | --- | --- | --- |
| **0: Measure** | Repeat-run script with failure classification; the environment pre-flight checks | Addresses most of the recorded flakiness, and gives a baseline for everything after | small |
| **1: Plan-lite** (do now) | `EnsureVisible` as one attempt inside the existing `RunPoll`. `StaleElementException` in Core, mapped by both drivers, with the 11 catches switched. `MauiTestContext.FindElement` / `TryFindElement` as one attempt. The action inside the log unit. The F7 loop fix. One deadline passed through `TryMaterializeMore` and the scroll loops. Messages that give the last observation and the replacement count | Fixes the measured race and the plain bugs. No new public concepts, no interface changes, generator untouched | a few hundred lines (**estimate**) |
| **2: Consolidate** (when tier 0 shows framework flakiness remains) | One internal poll loop; scope public members as call units; `Until` stale handling | Consistency and diagnostics, justified by measured failures | medium |
| **3: Architecture** (a separate project) | Core base interfaces and bridges (the platform refactor); the scope readiness chain (only if A3 or similar is shown); `ItemKey` (only if the recycling test fails); renames and `DriverRootScope` removal | Good ideas without a flakiness case; they belong to a platform refactor with its own plan | large |

The `ItemObjectBase` rename can go in whenever convenient. It is small and independent.

**What to keep from the documents:**

- The analysis documents are right, and useful as a map.
- `overall-design.md` stays as the **target** for tier 2 and tier 3.
- `plan.md`'s phases, cut to tier 1, become the work to do now, with Tier 0 in front of them.

## 5. The three questions, answered

1. **How complex?** A rewrite of the core call, readiness, lookup and interface models: ~22 new
   types, ~30 removed or renamed members, every stack touched in one step, and 3,000-5,000 lines
   (**estimate**).
2. **Overengineered?** Yes, as a single piece of work. About a third of it is needed. The rest
   improves consistency, or prepares for problems not yet seen, and should wait for evidence or
   for the platform refactor.
3. **A good way to fight flakiness?** The direction is right: find again on every attempt, one
   budget, act once, clear failures. That is how the mature tools do it. As a *strategy* it
   aims at one of this repo's eight known flakiness sources. Measuring and hardening the
   environment would return more, sooner. Pair the narrow fix (tier 1) with measurement (tier 0),
   and let the numbers decide the rest.
