# Do we still need `ResolveReadyElement`? - discussion

> **Background.** Its conclusions are in [../design.md](../design.md): R1, R3 and section 8
> (`ResolveReady`, the action inside `ControlCall`). What changed since:
>
> - The `TryOnElement` sketch now starts with the scope-readiness step (design 8).
> - Option B for the confirm budget is accepted (design R3).
> - Its section 5 plan edits are replaced by plan.md steps.
> - Its section 6 open points are settled: `doEnsureVisible` and the `EnsureClickableCore` calls
>   are removed.

Question: once the plan in [original-plan.md](original-plan.md) is done, can actions and setters drop
`ViewBase.ResolveReadyElement` and go through one of the existing `Run*` helpers instead?

Status: discussion, 2026-09-19. It assumes phases 1-3 of the plan are in: `EnsureVisible` is one
attempt, every helper finds the element again on each attempt, and staleness is reported as
`StaleElementException`.

**Short answer:** no `Run*` helper can replace it without either repeating the action or
renaming it. We still need a step that finds a ready element and then acts once. After the plan,
though, it no longer needs its own loop. It becomes a thin wrapper over the same private poll
the queries use. While looking at this I found two gaps the plan does not cover: the action runs
outside its log entry, and the confirmation step starts a second budget. Section 4 covers both.

---

## 1. What it does today

`RunDoWithElement` (48 generated calls, including all 5 in `ClickableControlBase`) and
`RunSetWithElement` (9 generated, plus `Entry.Enter`) both do this:

```csharp
var element = ResolveReadyElement(timeoutMs, ensureVisible, caller); // polled, logged
coreOperation(element);                                               // once, not logged
```

`ResolveReadyElement` polls `FindElement` → `EnsureVisible` (unless the control opts out through
`RequiresVisibilityForAction`) → `EnsureReadyForActionCore` (enabled, for clickables and
toggles). It returns the first element that passes all three checks. The action then runs
**exactly once**: a failing `Invoke()`, `Toggle()` or `SendKeys()` is never retried.

It was added in `509c369` ("Wait for readiness fixes") to replace the older pattern, which
`Brinell.Html` still uses (`srcnew/Brinell.Html/Controls/ControlBase.cs:131`): the find, the
visibility check and the action all ran inside one `RunPoll`.

`ContainerObjectBase.ResolveReadyRoot` does the same for containers. It is simpler because a
container root has no visibility or enabled check.

## 2. The candidates

| Replace with | What goes wrong |
| --- | --- |
| `RunDo(() => { var e = FindElement(); EnsureVisible(e); EnsureReadyForActionCore(e); core(e); })` | `RunPoll` treats every exception as transient and runs the lambda again. If the action throws after it took effect (a click whose confirming `Until` fails, text typed and then a stale read), it runs a second time: a double click, text entered twice, a toggle that flips back. This is the pattern `509c369` removed. It would also run `EnsureVisible` inside the same lambda as the action, and after phase 2 that means a scroll right before every retried action. |
| `RunGetWithElement(e => { EnsureReadyForActionCore(e); return e; })`, then act | After the plan this does work: it finds the element on every attempt and makes one visibility attempt. But it has no visibility opt-out (`ToolbarButton` needs one), it presents an action as a `Get`, and it still acts after the poll ends. That is `ResolveReadyElement` under a misleading name, not a simpler design. |
| `RunWaitWithElement(true, e => { ...; ready = e; return true; })` | The same as above, except a timeout returns `false` instead of throwing. We would need an extra check to turn "not ready" into an exception, and the "last observed" message the plan wants (section 2, item 4) would be lost. |
| `Run(caller, value, () => core(FindElement()))` | It does not poll, so an element that appears or becomes enabled a moment late fails immediately. That is exactly what AD-004 exists to prevent. |

The one rule no helper satisfies is that **finding the element is retried, but the action is
not.** Every `Run*` helper with a poll retries its whole lambda, and the one without a poll
(`Run`) retries nothing. An action needs both behaviours, one before the other.

## 3. After the plan: one poll, several uses

Once 3.1 and 3.2 are done, three helpers have the same body and differ only in the check they run
on each attempt and what they return:

| Helper | Each attempt | Returns |
| --- | --- | --- |
| `RunGetWithElement` | find, one visibility attempt, read | the value read |
| `RunWaitWithElement` | find, one visibility attempt, predicate | whether the predicate held |
| `ResolveReadyElement` | find, one visibility attempt (unless opted out), `EnsureReadyForActionCore` | the element |

`RunAssertWithElement` is the exception while decision S1 is open, because it finds the element
once and reads it on every tick.

So the three can share one private primitive, with the helpers built on top of it:

```csharp
/// One attempt per poll interval: find the element, try once to make it visible, then run
/// attempt. Nothing in here waits on its own; the caller's budget is the only one.
private bool TryOnElement<T>(Func<IMauiElement, (bool Done, T Value)> attempt,
    bool ensureVisible, Deadline deadline, out T value, out string lastObserved)
```

- `RunGetWithElement`: `(true, core(e))`.
- `RunWaitWithElement`: `(core(e), default)`.
- `ResolveReadyElement`: `{ EnsureReadyForActionCore(e); return (true, e); }`, with
  `ensureVisible: RequiresVisibilityForAction`.

With this, `ResolveReadyElement` shrinks to three lines. It stays as a named private method
because "ready to be acted on" is a real concept: `EnsureReadyForActionCore` and
`RequiresVisibilityForAction` are its extension points, and a named method is where their
documentation lives. Put the scroll throttling from 3.1 and the "found, then gone (replaced 3
times)" bookkeeping from item 4 of the plan's section 2 in `TryOnElement`, so the queries and the
actions share them.

`ResolveReadyRoot` gets the same treatment on the container side, or stays as it is: it has no
nested budget, so the plan does not require a change there.

## 4. Two gaps the question exposes

### 4.1 The action runs outside its log entry

`ResolveReadyElement` opens and closes the log pair (via `RunPoll`) **before** `coreOperation`
runs. So for `Click()`, `Toggle()` or `Enter()`:

- the exit line reads `Success` even when the action or its confirmation then throws;
- the logged duration leaves out the action and its `Until` confirmation, which can be the
  larger share: up to `timeoutMs` for the 8 act-then-confirm controls.

`TestTiming` times whole test methods, so the per-test baseline is correct. The per-member log
lines are not, and those are what someone reads when a test fails.

Fix: make the public call one `Run(...)` unit that covers both steps, with a resolve poll inside
it that writes no log lines:

```csharp
protected TScope RunDoWithElement(Action<IMauiElement> coreOperation, int? timeoutMs = null,
    bool doEnsureVisible = true, [CallerMemberName] string? caller = null)
    => Run(caller!, (object?)null, () =>
    {
        var element = ResolveReadyElement(Deadline.From(timeoutMs ?? DefaultTimeoutMs),
            doEnsureVisible && RequiresVisibilityForAction);
        coreOperation(element);
        return ContainingScope;
    }, timeoutMs);
```

This also removes the problem in item 3 of the plan's section 1 (nested page-readiness gate and
log lines) on the action path: `Run` checks page readiness once, and the poll inside it neither
checks it again nor logs. In practice that means splitting `RunPoll` into a logging shell and a
poll core that does not log, the same split that `Until` and `WaitHelper.WaitFor` already have.

### 4.2 The confirmation starts its own budget

The generated wrappers pass `timeoutMs` twice: once to `RunDoWithElement` for resolution, and
once to the Core method (`ClickCore(element, timeoutMs)`), whose `Until` starts a **new** budget
of the same length. `Toggle(timeoutMs: 5000)` can therefore take about 10 s: 5 s to find a ready
element and 5 s to confirm that the toggle took effect. This contradicts item 1 of the plan's
section 2 ("one budget per public call").

Two ways out:

| Option | For | Against |
| --- | --- | --- |
| **A. One deadline for the whole call.** Resolve and confirm share it, and the Core method gets the time that remains. | Keeps the rule exactly as written. `Click(timeoutMs: 500)` means 500 ms. | A slow lookup leaves little time to confirm, and that step cannot be retried, so the call fails with "the action had no effect" when the real cause was a slow lookup. The generator must also pass the remaining time into the Core lambda. |
| **B. Two phases, each with the caller's budget:** resolve within `timeoutMs`, then confirm within `timeoutMs`. | Each phase gets a fair budget, so a failure message names the phase that actually failed. No generator change. | The worst case is 2 × `timeoutMs`, so the rule needs rewording. |

**Recommendation: B, documented.** It is today's behaviour. What the plan fixes is a budget that
nobody chose (the hard-coded `DefaultWait` inside `EnsureVisible`), not the resolve-then-confirm
split. Word the phase 6 rule as: "each phase of a public call (resolve, confirm) has the caller's
budget; nothing starts a budget with its own default." If we later need a hard limit for the whole
call, A can be added then.

## 5. Proposed changes to the plan

| Where | Change |
| --- | --- |
| Phase 0 | Add test (e): `Click()` where `ClickCore` throws → the element is invoked once (`Times.Once`), and the log exit is `Error` with that exception's message. Today it fails on the second half. |
| Phase 2 | Build `ResolveReadyElement`, `RunGetWithElement` and `RunWaitWithElement` on one private poll (section 3). Wrap `RunDoWithElement` and `RunSetWithElement` in one `Run` unit (4.1). Split `RunPoll` into a logging shell and a poll core that does not log. |
| Section 6 | New decision S6: the budget for resolve plus confirm. Recommendation B (4.2). |
| Phase 6 | AD-004 and skill R2 wording as in 4.2. |
| Out of scope | `Brinell.Html`'s `RunDoWithElement` / `RunSetWithElement` still run the action inside the poll, so a failed action is retried. Same class of bug; record it as a separate item next to the WPF/WinForms one. |

## 6. Open points

- Does anything pass `doEnsureVisible: false` to `RunDoWithElement`? A search of `srcnew/` finds
  no caller. If the generator never emits it either, drop the parameter and keep only
  `RequiresVisibilityForAction` (no-backward-compatibility rule).
- `ClickCore` calls `EnsureClickableCore`, the same check as `EnsureReadyForActionCore`, a second
  time outside the poll. That check can fail on an element that the poll accepted a moment
  earlier. Once `StaleElementException` exists, a failure there is a real "changed between ready
  and act" signal. Decide whether it stays (and gets a clear message) or goes because the poll
  has already done the check.
