---
title: RCA — Why BackToHub is a hand-rolled locator instead of a control object
description: How the suite's one raw element lookup got there, what it cost, and whether the reasoning that put it there still holds
status: rca
---

# RCA — Why `BackToHub` is a hand-rolled locator instead of a control object

## The question

`MauiFixture` contains the only raw locator in the suite:

```csharp
var backToHub = Locator.ByAccessibilityId("BackToHub");
```

Everything else in `Brinell.Maui.UITests` goes through a control object. `AGENTS.md` discourages
exactly this, and the codebase argues against it in its own comments — `ContainerTestPage` says
declaring children matters because otherwise a test "builds a control — or a raw locator — on the
spot, which is how element finding leaks back into tests".

So why does this one exist, and should it?

## Short answer

It was a deliberate, documented decision taken to fix a different defect
([rca-page-readiness-gate.md](rca-page-readiness-gate.md), Option A), and it was the right call
at the time. **The reasoning behind it contains one wrong step, which nobody has revisited since**:
it concluded that a control object requires a *page object*. It does not. It requires a *scope*,
and the two gates that made the original arrangement fail both key off whether that scope has a
page — not off whether it is one.

That wrong step is why the fix took the shape it did, and why it kept a cost that did not have to
be paid.

## How it got here

### 1. It used to be a control object

`HubPage` declared `BackToHub` as a `Button<HubPage>` and exposed `TryGoBack()`. The page object
carried an apologetic comment explaining that it resolved one element deliberately *not* on the
hub, and that gating lookups on the hub being loaded would break it — "the hub reports not-loaded
precisely when the back button is needed".

That comment was an accurate prediction of a defect, written as if it were a design note.

### 2. Two gates turned the prediction into a failure

| | Gate 1 — resolution | Gate 2 — readiness |
|---|---|---|
| Where | `PageObjectBase.CanResolveElements` → `EnsureLoaded` | `ViewBase.RunPoll` |
| Throws | `ElementNotFoundException` "is not loaded" | `PageLoadException` "did not become ready" |
| Introduced | pre-existing | `3784e70 Revisited busy page checking` |

Gate 2 arrived and every test after the first died with `MissingRoot` on `PageHub`. Both gates
were right: the control was scoped to a page it is not in.

### 3. The fix replaced the control object rather than rescoping it

Option A of that RCA moved the lookup to the app root and used `Context.TryFindElement` directly.
It named its own cost:

> **Cost:** a raw `TryFindElement` in the fixture, which `AGENTS.md` discourages in *tests*.
> This is fixture plumbing rather than a test body, and the alternative is inventing a page
> object for a page that has no identity.

It worked — the suite went from one test deep to 183 passing — and it was correctly chosen as the
smallest change that resolved a contradiction rather than working around it.

## Root cause of the present state

**"The alternative is inventing a page object" is a false dilemma.**

`ViewBase<TScope>` does not require a page object. It requires an `IMauiScope<TScope>`, and both
gates are conditional on that scope having a `Page`:

```csharp
// ControlObjectBase
protected IPageObject? Page => Scope.Page;      // nullable

// ViewBase.RunPoll — gate 2
if (Page != null && !Page.WaitReady(timeout)) { throw new PageLoadException(...); }
```

- **Gate 2 skips itself** when `Scope.Page` is null.
- **Gate 1 does not exist** off `PageObjectBase`; it is that class's override of
  `CanResolveElements`.

So a scope that resolves from the driver root and reports **no page** would give
`Button<AppRoot>` precisely the semantics `FindBackToHub` hand-rolls today — root-scoped,
ungated, tolerant of absence — while keeping everything a control object brings.

Nothing like that exists. `PageObjectBase` and `ContainerObjectBase` are the only implementations
of `IMauiScope`, and both have a page by construction. The missing abstraction is why the choice
looked binary.

## What the hand-roll actually cost

Not hypothetical — each of these was paid.

| Cost | Where it landed |
|---|---|
| **Lost the activation ladder.** A control object clicks through `ClickCore`, which tries an automation pattern first. `IMauiElement.Click()` is a raw mouse click. | This one line became **396 of the 400** physical-input uses the step 3 audit recorded. |
| **Blocked off-screen execution.** A raw click needs a clickable point on the desktop. | `BRINELL_AUT_PLACE=offscreen` was unusable for a whole stage ([steps.md](../extension/steps.md) step 2). |
| **Blocked background mode.** | Step 15 could not be attempted until the bridge could navigate. |
| **Re-implemented polling by hand, wrongly at first.** `FindBackToHub` polls in its own loop; the first version was unpaced and put thousands of round trips through the app. | Fixed during step 2. |
| **No postcondition.** A control object throws where it fails. This returned null and let the caller carry on. | Three investigations started in the wrong place, the most recent during stage B — the stack silently grew to four pages deep and every symptom pointed at whichever test ran next. |

## The complication: the ladder does not work on this element

Restoring a control object naively would re-break it. A MAUI `ToolbarItem` renders into native
window chrome, and its automation peer accepts an `Invoke` that goes nowhere. Measured four ways
during steps 2 and 3:

| Attempt | On-screen result |
|---|---|
| `IMauiElement.Click` (current) | stable, ~9 s |
| `InvokePattern` alone | 4 failures in 4 |
| Invoke, then click if it returned false | 3 failures in 4 |
| `ActivationHelper.TryActivateByPattern` (the shared ladder) | 1 failure in 3, runtime 5 s → 58 s |

The third row is the trap: the fallback never fires, because `Invoke` *reports success*. So any
control object used here must skip the ladder deliberately rather than rely on falling through
it.

That is expressible. `ClickableControlBase.TryActivateByPattern` is `virtual`, and its own
doc comment says controls needing a different rung should override it.

## What changed in stage B

On Windows this path is **no longer reached**. `ReturnToHub` asks the page to pop itself through
the automation bridge, and the click is only the fallback for platforms with no bridge — Android
and iOS, where a `ToolbarItem` click works normally.

So the urgency is gone, and the discipline question is not: Android and iOS still run this code,
and it is still the only raw locator in the suite.

## Options

### Option A — An app-root scope, and a control object on it

Add a scope (in the test project or in `Brinell.Maui`) that resolves through
`Context.TryFindElement`, reports `Page => null`, and returns itself as `Self`. Then:

```csharp
public ToolbarButton<AppRoot> BackToHub => new(AppRoot, Locator.ByAccessibilityId("BackToHub"));
```

with a small `Button` subclass overriding `TryActivateByPattern` to return false, carrying the
measurement above as its comment.

- Restores logging, retry semantics, failure messages and the Is/Wait/Assert surface.
- Removes the raw locator from the fixture.
- Gives every future control that legitimately lives outside a page root — toolbar items,
  flyouts, dialog chrome, platform pickers — somewhere correct to live. This is the general
  fix, not a special case for one button.
- **Cost:** a new abstraction, and the honest question of whether one button justifies it. It
  does not on its own; the class of controls does.

### Option B — Leave it, and say so properly

Keep the hand-rolled lookup and replace the "alternative is inventing a page object" note with
what is actually true: it is root-scoped because there is no root scope, and here is what that
costs.

- Zero risk, and stage B removed the expensive consequence on Windows.
- **Cost:** the suite keeps one lookup that contradicts its own stated rules, and the next
  control outside a page root repeats the whole analysis.

### Option C — Make the affordance a real `Button` in page content

Change `HubPage.AddBackToHub` to add a `Button` to the page rather than a `ToolbarItem`. Every
problem here disappears: it is addressable by `AutomationId`, it lives inside the page, the
pattern ladder drives it, and no bridge or raw click is needed.

- Simplest by a distance.
- **Rejected as the primary fix.** It changes the app under test to suit the harness, and a
  `ToolbarItem` that cannot be activated semantically is a real thing real apps have — the sample
  app earns its keep by containing it. Removing it would delete the only coverage of a case the
  capability catalogue lists twice (steps 18 and 26).

## Recommendation

**Option A, but not as part of stage B.**

It is the right shape and it generalises, which Option C does not and Option B does not. But it
adds an abstraction to the framework, and stage B is already carrying a contract change, a
provider change and a fixture rewrite. Landing a new scope type on top of that would mean a green
suite proving four things at once — the same argument the readiness RCA used to keep Options A
and B separable, and it was right then.

Until it lands, take Option B's documentation half now: the comment in `MauiFixture` should say
that the lookup is root-scoped because there is no root scope, not that a page object was the only
alternative.

**The natural home for Option A is step 26** (menu and flyout verbs), which is already about
controls that live outside a page root and will need somewhere to put them.

---

## Outcome — Option A applied

Taken now rather than deferred to step 26, and it turned out much smaller than the
recommendation assumed.

### It was nearly all there already

`IMauiTestContext` is itself an `IMauiElementScope`. It already resolves from the driver root,
already answers `Page => null`, and already describes itself in its own remarks as "test context
root scope". The single thing it cannot do is satisfy `IMauiScope<TScope>`, which is
self-referencing: a scope has to name its own type so controls can return it for chaining, and
the context cannot without leaking its implementation type into every control declared on it.

So `AppRoot` is a name for something that existed, not a new mechanism — an adapter with one real
line in it:

```csharp
public sealed class AppRoot : ObjectBase, IMauiScope<AppRoot>
{
    public AppRoot Self => this;
    public IPageObject? Page => null;   // the load-bearing line
    // everything else delegates to the context
}
```

That also sharpens the root cause. The abstraction was not missing because it was hard or
because nobody had thought of it. It was missing because CRTP made the obvious candidate
ineligible, and the shape of that obstacle is invisible from the call site — which is exactly how
"the alternative is inventing a page object" came to look true.

### Delivered

| | |
|---|---|
| `srcnew/Brinell.Maui/Pages/AppRoot.cs` | the scope: driver-rooted, no page, always ready |
| `srcnew/Brinell.Maui/Controls/Buttons/ToolbarButton.cs` | `Button` that declines the pattern ladder, carrying the four measurements as its reason |
| `MauiFixture.BackToHub` | `ToolbarButton<AppRoot>`, replacing `FindBackToHub` and its hand-rolled polling loop |
| `Tests/Navigation/AppRootScopeTests.cs` | three tests |

`FindBackToHub` is gone, and with it the suite's only raw locator and its only hand-written retry
loop.

### The tests exist for a specific reason

Since stage B, Windows never takes this path — the fixture asks the app to pop itself through the
bridge, and the click is the fallback for platforms with no bridge. Left alone, `AppRoot` and
`ToolbarButton` would be code that compiles on the only platform that can run it here and is
exercised only on the platforms that cannot.

`AppRootScopeTests` drives it deliberately: that it resolves while a page is open (the regression
guard for the original outage), that it reports honest absence at the hub, and that clicking it
returns to the hub. **14 passed in 2 s** with the Buttons area alongside.

### What this does not change

`ToolbarButton` still clicks rather than invoking, and that is not a compromise pending a better
idea — it is the measured behaviour of the control. Driving a MAUI `ToolbarItem` through Invoke
reports success and raises nothing, so the pattern ladder is not merely useless here, it is
actively harmful: it prevents the click that would have worked. The control object records that
in one place instead of the fixture having to know it.
