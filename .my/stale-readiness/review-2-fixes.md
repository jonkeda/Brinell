# Stale readiness: fixing the second review

The design for the changes that answer [implementation-review-2.md](implementation-review-2.md).
What to build is here; the findings and their evidence are there, and the accepted design of the
work as a whole is in [design.md](design.md).

Status: **done**, 2026-09-20. Findings 1-5, 7 and 8 are fixed; finding 6 is deliberately not (3.6).
Verified: solution builds, `Brinell.Maui.Tests` 186 passed / 1 skipped, and the full Windows UI
suite green twice (351 passed, 2 gated skips, 0 failed). The results and what the timing report
could not tell us are in [plan.md](plan.md) 2.1, "Second review fixes", and section 4.

## 1. What this changes, in one line each

| # | Finding | Decision | Risk |
| --- | --- | --- | --- |
| 1 | `Assert*` holds one element for the whole call | Settle **S1**: resolve per attempt, same shape as `Get*` | low; one method, covered by the suite |
| 2 | A permanent `NotSupportedException` is waited out | Settle **D3** with a carve-out: a new `RouteUnavailableException` is fatal; plain `NotSupportedException` is still retried | medium; see 3.2 |
| 3 | A declared element's gesture misses the "app exited" guard | Route it through the driver's `WhileTheAppRuns` | low |
| 4 | `SupportsStateReads` is dead code | Remove it | none |
| 5 | A position-keyed row cannot detect recycling | Write the limit into design 7.6 | none; docs |
| 6 | The page probe resolves its root twice | **Not fixed.** See 3.6 | - |
| 7 | `timeoutMs` bounds the poll, not the call | Write it into `maui-ui-test` | none; docs |
| 8 | `AppRoot.WaitReady` is not a call unit | Make it poll its own budget | low |

## 2. The principle the two real fixes share

Both findings are an open decision in design section 11 that was left at its default and never
revisited. Both defaults traded a property the work exists to provide for something cheap:

- **S1** traded "it finds its element again on every attempt" (plan section 1) for one lookup per
  attempt. Step 2 then measured that lookup at **1.1 ms** under a known root. The trade is not
  worth making, and it was never re-examined once the number existed.
- **D3** traded "a failure waiting cannot fix ends the call at once" (R0) for a uniform retry rule.
  It is right for exceptions the framework does not understand. It is wrong for the one exception
  type this layer uses deliberately to mean "there is no route, and there never will be".

So the shape of both fixes is: **settle the decision, record it in design 11 and plan section 3,
and pin the behaviour.**

## 3. The changes

### 3.1 `RunAssertWithElement` resolves per attempt (finding 1)

`ViewBase.RunAssertWithElement` loses its `held` local and takes the body
`Attempt(a, ensureVisible: true, ...)`, which is what `RunGetWithElement` already does. `Attempt`
calls `Locate` and `EnsureVisible` on every tick and translates `Stale`, `NotReady` and
`AssertionException` into observations, so the whole of the old inline lookup, visibility check and
stale handling goes away with it.

Three behaviours change, all towards what the rest of the framework already does:

| | Before | After |
| --- | --- | --- |
| element replaced while alive | compared until the budget ran out | found again on the next attempt |
| element becomes invisible mid-call | read anyway | `NotReady(NotVisible)`, retried |
| `ElementNotReadyException` from the read | `Failed`, same element re-read | released, found again |

`AssertText` and `GetText` now answer about the same element, which they did not before.

**Cost.** One lookup per attempt on a call that previously did one in total: 1.1 ms per tick for a
child under a known root, 22.7 ms for a page root (plan section 4). Asserts that pass on the first
attempt - almost all of them - pay nothing extra, because the first attempt had to resolve anyway.
An assert that polls for a second pays about a lookup per polling interval. The full Windows suite
timing is the check: if `Assert*`-heavy classes move, the number is real.

**Record:** design 11, S1 → settled, "resolve per attempt; the saving S1 protected is 1.1 ms".

### 3.2 A missing route ends the call at once (finding 2)

The blanket rule "`NotSupportedException` is fatal" is wrong here, and it took reading the bridge
to see why. Two different things throw it:

- **A route that does not exist.** The `IMauiElement` and `IMauiDriver` capability defaults ("this
  platform has no semantic route for …"), a bridge-declared element asked for something that lives
  in the UI Automation tree (`FlaUIDeclaredElement.NotInTheTree`), and a locator strategy that
  cannot be matched against an element in hand (`ElementMatch.Matches`). Permanent.
- **A bridge verb that was not delivered.** `GestureUnavailableException`, and the driver's
  `ReadState` / `IsIdle` / navigation-depth / `CurrentRoute` answers. **Not** permanent: an app
  republishes its bridge between pages, so "the bridge did not answer" can change within a call.
  Plan step 8 found exactly this case and answered it by retrying, which is why
  `ElementNotReadyException` from an action means "did not act".

Making all of it fatal would undo step 8. So the fix separates the two by type rather than by rule:

```csharp
// Brinell.Maui/Exceptions/MauiExceptions.cs
public sealed class RouteUnavailableException : NotSupportedException;

// Brinell.Maui/Calls/Poller.cs
public static bool IsFatal(Exception error) => error is AppUnavailableException
    or RouteUnavailableException
    or ScopeNotReadyException { Readiness.IsConfigurationError: true };
```

`RouteUnavailableException` is thrown at the permanent sites only: the ~25 `IMauiElement` defaults,
the two `IMauiDriver` defaults, `ElementMatch.Matches`, and `FlaUIDeclaredElement.NotInTheTree`.
Everything on the bridge keeps throwing plain `NotSupportedException` and keeps being retried, so
step 8's behaviour is untouched.

It derives from `NotSupportedException`, so the two places that deliberately catch one and treat it
as a permanent "no" - `ScrollHelper.ScrollIntoView` and `CollectionObjectBase.TryActivate` - keep
working unchanged.

**The risk, and it is the one to watch.** `Assert.Throws<T>` matches the type exactly, so any test
asserting `Assert.Throws<NotSupportedException>` against one of the changed sites now fails and
must be updated to `RouteUnavailableException`. There are nine such asserts in the tree; some are
on bridge paths and are unaffected. The build and the unit run name the rest, and the UI suite
names the ones in `FocusVerbTests` and `StateReadTests` if those sites moved. **These updates are
part of the change, not a regression** - but each one must be read, not blindly retyped, because a
test that was asserting "no route" and now gets a retry (or the other way round) is telling us the
site was classified wrongly.

**Record:** design 11, D3 → settled, "unknown exceptions are retried; a missing route is not, and
is its own type; a bridge verb that was not delivered is not a missing route".

### 3.3 One bridge route, one guard (finding 3)

`FlaUIDeclaredElement.PerformGesture` called `GestureRunner.Perform` directly. It now calls
`_driver.PerformGesture(_automationId, gesture)`, which is already wrapped in `WhileTheAppRuns`, so
a gesture that found no target because the app exited is `AppUnavailableException` like every other
bridge verb. No new code; it stops bypassing the code that exists.

### 3.4 `SupportsStateReads` goes (finding 4)

Deleted. Nothing calls it, `BridgeVerbRunner`'s own comment says step 8 folded it into `ReadState`,
and it was the one bridge entry point without the `AppHasExited` guard.

### 3.5 The position-key floor is written down (finding 5)

No code. Design 7.6 gains a paragraph saying that `ItemKey.IsHeldBy` can only check a key the
element carries, that a `Position` key answers `true` unconditionally, and that R0's "never act on
another row" therefore holds only for collections whose rows publish a logical index or a
distinguishing automation id - so give the item template an id.

A log warning when `KeyOf` falls back to `Position` was considered and dropped: it would fire once
per row of every such collection, and the place a test author reads is the docs, not the CSV.

### 3.6 The page probe is left resolving its root twice (finding 6) - **not fixed**

The review proposed passing the probe's root to the loaded check. That fix is wrong, and the reason
is worth recording so nobody tries it again.

`PageObjectBase.ProbeContentReadiness(root)` calls `IsLoaded()`, which resolves the root again.
Moving it to an `IsLoadedCore(root)` would mean readiness stops calling `IsLoaded()` - and
**fifteen** page objects override `IsLoaded()` with a real content check
(`public override bool IsLoaded() => PageTitle.IsExists();`). Readiness would silently stop asking
them: every one of those pages would count as loaded the moment its root had bounds, which is the
opposite of what they were written for, and it would not fail a single test loudly.

The actual cost is also smaller than the review implied: the second resolution is a cache hit, so
it is one extra `HasUsableBounds` read per probe, not a second lookup.

Doing this properly means moving the override onto a `Core` method that takes the root, across
every page object. That is a move-down-shaped change (`move-down.md`), not a review fix. The method
keeps a comment saying so.

### 3.7 `timeoutMs` bounds the poll (finding 7)

No code. `maui-ui-test` gains a bullet: `timeoutMs` bounds the wait for the control, not the whole
call; an action that confirms its own effect waits again on a budget of its own, so
`Toggle(timeoutMs: 300)` can take about 600 ms; size a budget by how long the control may take to
become ready.

`RouteUnavailableException` is added to the same skill's "what a failure means" list.

The latent half - `CallRemainingMs` reading 0 inside a Core method after it has acted, because
`AttemptContext.Current` is still the spent poll context - is **left as is**. Giving `Confirm` an
`AttemptContext` of its own would change what every confirmation's scrolls are budgeted against,
which is a design 5 change and wants its own evidence. Nothing reads `CallRemainingMs` after acting
today. It belongs in design 11 as a new open item rather than in this change.

### 3.8 `AppRoot.WaitReady` honours its budget (finding 8)

It delegated to `MauiTestContext.WaitReady`, which is `=> !_disposed`: no poll, no log, and
`timeoutMs` did nothing. It now polls its own `ProbeReadiness` on its own `Deadline` through
`Poller.Until`, the same loop every other scope uses.

It deliberately does **not** open a `ControlCall` log pair. `AppRoot` has no page and no locator,
so the two identity fields a log entry needs are both absent, and the app root has nothing of its
own to wait for yet (design Q4) - the poll exists so the member's signature stops lying, not
because there is state to watch. If an app-level busy signal ever lands in `ProbeReadiness`, this
becomes a `RunProbe` like the others.

## 4. What is not in this change

- The `Stepper.tpl.cs` and `StepperControlTests.cs` compile errors were fixed (two missing
  `using` lines each) because nothing can be built or run otherwise. They belong to the
  `.my/android/` work, not to this one, and should be committed with it.
- No plan, `CHANGELOG` or decision-record edits yet. They land once the fixes verify: design 11
  (S1, D3 settled; the `Confirm` budget added as new), plan section 3, AD-009 (the position-key
  floor, and "a missing route fails at once"), and `CHANGELOG` under Added/Changed.

## 5. Verification

In order, stopping at the first failure:

1. `dotnet build srcnew\Brinell.sln` - names every `Assert.Throws<NotSupportedException>` that has
   to move, and any site the sed pass caught wrongly.
2. `dotnet test testsnew\Brinell.Maui.Tests` - expected **180 passed, 1 skipped**: 176 before plus
   the four new `Pin=review2` pins. Any other movement is a finding.
3. The four new pins, each of which fails on the code before its fix:
   - `Assert_FindsTheElementAgain_WhenItIsReplacedWhileAlive` - the throwaway test from the review,
     kept;
   - `Assert_WaitsForVisibility_OnEveryAttempt`;
   - `RouteUnavailable_FailsAtOnce` - under 300 ms on a 1000 ms budget, and the exception is the
     route one, not a `WaitTimeoutException`;
   - `UnexpectedException_ThatIsNotAMissingRoute_IsStillRetried` - D3's default still holds for
     everything else.
4. `dotnet test testsnew\Brinell.Maui.UITests` **in full**. Tier 3 is the right tier: finding 1
   changes every `Assert*` on every control, and finding 2 changes what ends a call. The bar is
   plan section 4's last full run - **353 tests, 351 passed, 2 gated skips, 0 failed, about 3
   minutes** - with `OccludedScreenshotTests` ruled out as environmental if it fails.
   The sample app is rebuilt first: `samples/` changed under the `.my/android/` work in the same
   tree, and `dotnet test` builds the test project, not the app it launches.
5. The run's `test-timings.md`, read for `Assert*`-heavy classes, against 3.1's cost note.

Android is not run here. The uncommitted work in the tree is mid-flight, and its baseline is its
own project's business.

## State

Already applied to the working tree, unverified:

| File | Change |
| --- | --- |
| `srcnew/Brinell.Maui/Controls/Base/ViewBase.tpl.cs` | 3.1 |
| `srcnew/Brinell.Maui/Exceptions/MauiExceptions.cs` | 3.2, `RouteUnavailableException` |
| `srcnew/Brinell.Maui/Calls/Poller.cs` | 3.2, `IsFatal` |
| `srcnew/Brinell.Maui/Interfaces/IMauiElement.cs`, `IMauiDriver.cs`, `Containers/ElementMatch.cs` | 3.2, throw sites |
| `srcnew/Brinell.Maui.FlaUI/FlaUIDeclaredElement.cs` | 3.2 (`NotInTheTree`), 3.3 |
| `srcnew/Brinell.Maui.FlaUI/FlaUIMauiDriver.cs` | 3.4 |
| `srcnew/Brinell.Maui/Pages/PageObjectBase.cs` | 3.6, comment only |
| `srcnew/Brinell.Maui/Pages/AppRoot.cs` | 3.8 |
| `srcnew/Brinell.Maui/Controls/Range/Stepper.tpl.cs`, `testsnew/Brinell.Maui.Tests/Semantic/StepperControlTests.cs` | section 4, build fixes |
| `testsnew/Brinell.Maui.Tests/Semantic/StaleReadinessPinTests.cs` | four `Pin=review2` pins |
| `.my/stale-readiness/design.md` | 3.5 |
| `.github/skills/maui-ui-test/SKILL.md` | 3.7 |

Still to do: section 5 in full, then the records listed in section 4.
