# RCA-002: Page Objects Discard Their Own Load Check, Turning Wrong-Page Errors Into Slow, Anonymous Failures

**Reported:** 2026-08-29
**Severity:** High — this is the mechanism behind the suite's apparent flakiness
**Status:** Root cause confirmed; fix applied
**Component:**
- `srcnew/Brinell.Maui/Pages/PageObjectBase.cs` (`TryFindElement`, `FindElement`, `FindElements`)

**Related:** [RCA-001](rca-001-container-module-tests-navigation-stack.md). RCA-001 fixed two
instances of this defect in the test fixture. This one is the same mistake in the framework,
and is the reason RCA-001 was hard to diagnose at all.

---

## Why this matters more than the tests it fixes

A test suite is an instrument. Its job is to tell you, quickly and unambiguously, what is
wrong. When a wrong-page condition — knowable in about a millisecond — is reported 3 to 47
seconds later as a message about some *element*, the instrument is lying about what happened
and taking a long time to do it.

That is what "flaky" usually means in practice. Not that behaviour is random, but that
**failures are slow, mislabelled, and therefore indistinguishable from hangs.** Over one
working session this defect produced:

- three runs abandoned as "stuck" that were in fact failing normally, just slowly;
- two wrong conclusions drawn from failure lists that named the wrong cause;
- an `AGENTS.md` known-failures list that attributes 13 navigation failures to six unrelated
  control classes.

Fixing the individual tests does not fix this. Fixing the reporting does.

---

## Symptom

When the app is not on the page a page object represents:

- element lookups take the full `ElementFind` timeout (3 s default, 10 s on the slow profile)
  before failing;
- the exception names the **element** that was not found, never the **page** that was not
  loaded;
- when the lookup sits inside a `RunPoll` retry loop, the inner timeout is paid on every
  iteration, compounding to tens of seconds;
- a constructor throw in a shared fixture reports as `1 ms` with an **empty error message**
  against every test in the class.

The last point is what makes it costly: an empty message offers the reader nothing, so the
only way to learn anything is to re-run under a debugger or take a screenshot of the app.

---

## Root Cause

`PageObjectBase` computes exactly the right precondition and then throws the answer away.
All three element-scope methods:

```csharp
IMauiElement IElementScope<IMauiElement>.FindElement(Locator locator)
{
    EnsureLoaded();                       // returns bool — discarded
    return _context.FindElement(locator); // proceeds regardless
}
```

`TryFindElement` and `FindElements` are identical in shape. `EnsureLoaded()` is *already*
cheap and non-polling — it calls `IsLoaded()` with no timeout, which is a single
`FindElements(ByAutomationId(Name))` and a bounds check:

```csharp
public virtual bool IsLoaded(int? timeoutMs = null)
{
    var timeout = timeoutMs ?? 0;
    return timeout > 0 ? Poll(IsVisiblePageRootLoaded, timeout) : IsVisiblePageRootLoaded();
}
```

So the sequence on a wrong page is:

| Step | Time | Outcome |
|---|---|---|
| `EnsureLoaded()` → `false` | ~1 ms | **the correct diagnosis, discarded** |
| `_context.FindElement(locator)` polls at 100 ms | 3 000 ms | cannot succeed — the page is not there |
| `ElementNotFoundException(locator)` | — | names the element; says nothing about the page |

The information needed to fail correctly is produced, then dropped, one line before the
expensive call that cannot possibly succeed.

### The same class already gets this right

`ContainerObjectBase` faces the identical situation — a scope whose root may be absent — and
acts on it:

```csharp
public IMauiElement? TryFindElement(Locator locator)
{
    var root = TryGetContainerRoot();
    if (root == null) return null;        // ← precondition acted on
    ...
}

public IMauiElement FindElement(Locator locator)
    => TryFindElement(locator)
       ?? throw new ElementNotFoundException(
            $"Element not found within container. Container locator: {Locator}, Child locator: {locator}");
```

It short-circuits, and its message names **both** the container and the child. That is the
behaviour `PageObjectBase` should have had. This is not a new design; it is applying the
existing one consistently.

### Why `EnsureLoaded` returns a bool nobody uses

The `_ensuringLoad` re-entrancy guard shows the intent was real — `IsLoaded()` itself
performs element lookups through this same scope, so the flag prevents infinite recursion.
Someone built the precondition properly and then wired it in as a no-op. The return type is
the evidence of the intent; the discarded result is the bug.

---

## Fix

Act on the check, and say what is actually wrong.

- **`FindElement`** — when the page is not loaded, throw immediately with a message naming
  the page, its locator, and the element that was being looked for. No element search is
  attempted, because it cannot succeed.
- **`TryFindElement`** — return `null` immediately. The caller asked a question that already
  has an answer; making them wait 3 s for it serves nobody.
- **`FindElements`** — return empty immediately, for the same reason.

Both fast paths preserve the existing contract: `TryFindElement` still returns null rather
than throwing, `FindElements` still returns a list, `FindElement` still throws
`ElementNotFoundException` — the same type, so no caller's `catch` changes. Only the timing
and the message improve.

### Blast radius, and the escape hatch

This is a behaviour change, not a refactor: code that previously found an element *while the
page root check was false* will now fail fast instead.

The known legitimate case is a scope that deliberately searches outside its own page root —
`ContentDialog` is exactly this, since a WinUI3 dialog renders in a separate popup HWND and
is found via `IMauiDriver.FindPopupElement`, not through the page scope. Any similar case
needs a way to opt out.

`RequiresLoadedPage` is therefore a `protected virtual bool` on `PageObjectBase`, defaulting
to `true`. A page that legitimately resolves elements outside its own root overrides it to
`false` and keeps the old behaviour. The default is strict because the failure mode of being
too lax is a 3-second lie, while the failure mode of being too strict is a clear exception
naming exactly what to override.

On the dialog case specifically: `ContentDialog` is a `ContainerObjectBase`, not a
`PageObjectBase`, and it overrides `FindContainerRootElement` to go through
`IMauiDriver.FindPopupElement`. So it does not route through the changed methods at all, and
no override is needed today. The hatch exists for a *page* that hosts off-root content —
none currently does, and the UI suite contains no dialog tests, so this is a guard against a
future case rather than a present one.

### What this is *not*

It is **not** a timeout change. No timeout is raised, and none is lowered. The suite gets
faster only because failures stop waiting out timeouts they were always going to lose.
Reaching for a longer timeout here — the reflex this whole RCA argues against — would have
made every one of these failures slower and none of them clearer.

---

## Verification

| Check | Before | After |
|---|---|---|
| Wrong-page `FindElement` | 3 000 ms, names the element | immediate, names the page |
| Wrong-page `TryFindElement` | already fast, wrong answer late | immediate `null` |
| Failure message on wrong page | `Element not found with locator: AutomationId:Foo after 3000ms` | `Page 'X' is not loaded, so '...' cannot be found in it.` |
| `Brinell.Maui.Tests` (unit) | 8 failed / 62 passed | **8 failed / 69 passed** — same 8, +7 new ladder tests |
| UI suite, excluding phase-7 parked tests | — | **137 passed / 1 failed / 1m21s** |

The one remaining failure is `SwitchTests.Switch_ClickTwice_TogglesOff`, which pre-dates all
of this work and is tracked in phase 7.

No test changed behaviour as a result of this fix: the same unit tests fail before and after,
and no UI test that passed before now fails. The change is confined to *how fast* and *with
what message* a wrong-page lookup fails.

---

## Lessons

- **A discarded precondition is a latent slow failure.** Any `if`-less call to a method that
  returns a bool deserves suspicion; three of them in one file is a pattern.
- **This is the third instance of one defect in a single session** — twice in `MauiFixture`
  (RCA-001), once here. That frequency suggests a review rule, not three fixes: *a
  precondition check whose result is unused is a defect.*
- **"Flaky" is often "slow and mislabelled."** Before treating failures as nondeterministic,
  check whether they are simply reported too late and under the wrong name.
- **The fix for a slow failure is almost never a longer timeout.** It is failing at the point
  the answer becomes known.
