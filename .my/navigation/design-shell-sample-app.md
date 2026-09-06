# Design: a Shell sample app, and coverage for Shell / ShellContent / Tab / FlyoutItem

## Why this supersedes the delete recommendation

`redesign-navigation-as-collections.md` proposed deleting these four controls because nothing
referenced them. That was right about the *evidence* and wrong about the *conclusion*: they are
unreferenced because the app that exercised them was removed, not because Shell stopped mattering.
Shell is how a large share of real MAUI apps navigate. Untested support is worse than none, so the
choice is to cover them properly or drop them — and this document takes the first.

## The thing that must not be repeated

Shell was removed from the sample app for concrete, measured reasons. Any design that adds it back
has to answer each of them or it will resurrect the same failures.

| What happened | Evidence |
|---|---|
| Clicking an already-selected tab does **not** pop that tab's navigation stack, so a pushed page stayed on screen and leaked into the next test | `rca-001`: the fixture's "recovery" was a no-op; 9 tests × 47 s ≈ 7 minutes of identical waits |
| Android's `BottomNavigationView` shows **5 tabs plus More** — the rest are behind an overflow menu and simply not on screen. Windows showed all 10 | architecture plan §tab overflow |
| A tab is addressed by `ControlTypeAndName` on Windows and by `content-desc` on Android | same |

The first is the dangerous one: it is not a Shell defect, it is how Shell works. A test suite must
never treat "click the tab" as "return to a known state".

## Decision 1: a separate app, not the existing one

**`samples/Brinell.Samples.Maui.ShellApp`**, alongside the current app rather than inside it.

- The existing app's page hub is what makes the rest of the suite reliable — one click to any
  page, a fresh page instance each time, no navigation stack to leak. Putting Shell back into it
  would trade that away for every suite, to test one navigation style.
- The two navigation models want opposite things from a fixture. The hub wants "return to hub,
  open page". Shell wants "select tab, pop to root". One app cannot demonstrate both honestly.
- It is cheap: `Brinell.Samples.Shared` already holds the view models and commands, and
  `Brinell.Maui.AppSupport` the automation handlers. The Shell app references both and adds only
  XAML plus a handful of pages.

`MauiTestFixtureBase.GetDefaultAppPath(platform)` is abstract, so a second fixture pointing at a
second app needs no framework change — this is exactly the extension point it exists for.

## Decision 2: the app is small on purpose, and the overflow is deliberate

**Four tabs**, comfortably under Android's five-tab limit, so ordinary tab tests exercise tabs
rather than the overflow menu.

**Plus one flyout with six items**, which *does* exceed what a bottom bar shows — because the
overflow is a real Shell behaviour and the one place `FlyoutItem` earns its existence. It is
tested deliberately, in its own tests, rather than being tripped over by every other test.

This is the distinction the old app got wrong: it had ten tabs and met the overflow by accident in
tests that were about something else.

```
AppShell
├── TabBar
│   ├── Tab "Home"      → HomePage        (AutomationId ShellHomePage)
│   ├── Tab "Controls"  → ControlsPage    (AutomationId ShellControlsPage)
│   ├── Tab "Detail"    → DetailPage      (AutomationId ShellDetailPage)  ← pushes a sub-page
│   └── Tab "Status"    → StatusPage      (AutomationId ShellStatusPage)
└── Flyout
    └── six FlyoutItems, of which the last two are only reachable by scrolling the flyout
```

`DetailPage` exists to push a route (`Detail/Sub`) so the stack-leak case is covered on purpose,
with a test that asserts the pop rather than a fixture that hopes for it.

## Decision 3: reset pops the stack, it does not re-click the tab

The fixture's `ReturnToShellRoot()`:

1. If the current tab's stack is deeper than its root, pop it — `Shell.Current.GoToAsync("..")`
   equivalent, driven through the app's own back affordance, not a tab click.
2. Then select the target tab.
3. Then assert the tab's root page is loaded, by its own `AutomationId`, with a plain lookup.

Step 3 is the rule from the perf work: **a readiness check must never scroll and never assume.**
Step 1 is the rule from RCA-001: **a tab click is not a reset.**

## Decision 4: the controls follow the collection redesign

Shell is a collection of tabs; a flyout is a collection of items. Rather than keep the current
locator-passing API, these adopt the model in
[redesign-navigation-as-collections.md](redesign-navigation-as-collections.md):

```csharp
Shell.Tabs["Controls"].Click();
Shell.Tabs["Controls"].AssertSelected();
Shell.Tabs.AssertItemCount(4);
Shell.Flyout.Open();
Shell.Flyout["Settings"].Click();
```

replacing `NavigateTo(title)`, `GetTab(title)`, `IsTabSelected(title)`,
`WaitTabSelected(title, expected, timeout)` and `AssertTabSelected(title, expected, message,
timeout)` — five container methods that exist only because a tab is not an object today.

`ShellContent` keeps `Route`/`Title` and its `IsSelectedCore`; it is the item type behind
`Shell.Tabs`.

A tab is **clicked**, not selected — one verb across every item type, per the decision recorded in
that document. Selection stays observable through `IsSelected()` / `AssertSelected()`.

## Decision 5: the platform difference is an adapter, not a branch

A tab is `ControlTypeAndName` on Windows and `content-desc` on Android. That belongs in the
element/driver layer — the same place the Android off-screen rule and the navigation-bar nudge
now live — not in a control object and never in a test. `Shell.Tabs`' item strategy resolves per
platform; the test says `Shell.Tabs["Controls"]` on both.

## Tests

A new `Tests/Shell/` suite against the new fixture, one file per control:

| File | Covers |
|---|---|
| `ShellTabTests` | tab count, select by title, selected state, selecting the current tab is harmless |
| `ShellContentTests` | route and title, `IsSelected`, navigating by route |
| `ShellStackTests` | push a sub-page, pop it, and the case RCA-001 found: re-selecting the current tab does **not** pop — asserted as the documented behaviour it is |
| `ShellFlyoutTests` | open, item count, click an item, reach an item that needs the flyout scrolled |

`ShellStackTests` is the one that matters most. It turns the trap that cost seven minutes of
mystery into a test that states the behaviour out loud.

## Order of work

1. **The app**: `ShellApp` with four tabs and a flyout, referencing `Shell.Samples.Shared` and
   `AppSupport`. Nothing else. Confirm it launches on both platforms.
2. **The fixture**: `ShellFixture` + its own xUnit collection, with `ReturnToShellRoot` as above.
3. **`ShellTabTests`** — the smallest suite that proves the fixture is sound.
4. **The collection redesign of `Shell`/`ShellContent`/`Tab`** (steps 1–3 of the other document
   apply here too).
5. **`ShellStackTests`**, then **`ShellFlyoutTests`** — the two that exercise the known-hard parts.

Stop after step 3 if the fixture proves unreliable: that is the signal that Shell's stack model
resists per-test isolation, and it is better learned on four tabs than on ten.

## What was built, and what the app taught us

All five steps are done on **both platforms**: the app, the fixture, and all three suites -
**13 tests, 13 passing on Windows and 13 on Android**, the same tests unchanged.

### Deviations from the design above, each forced by something real

**The four tabs live inside a flyout item, not a `TabBar`.** MAUI disables the flyout while a
`TabBar` is the active item, so an app cannot show both at once - and this app exists to test
both. The tabs are `Tab` elements inside a "Main" `FlyoutItem`, with six more flyout items
beside it. One consequence is worth stating: they render as a top nav strip rather than
Android's `BottomNavigationView`, so the 5-tabs-plus-More overflow this design was careful to
avoid does not arise here at all - and is therefore not covered by anything.

**Pages mark themselves with a layout, not with a `ContentPage`.** The design asked whether a
tab root page exposes its own `AutomationId`. The question is moot: every page's content is a
`VerticalStackLayout` carrying the id, which the AppSupport handlers already surface on
Windows and Android exposes natively. Nothing had to be discovered.

**An `AutomationId` on Shell chrome goes nowhere.** Set on `Tab` and `FlyoutItem`, it never
reaches the platform: Windows reports WinUI's own `navViewItem` and `navItem` instead. This is
the design's Decision 5 confirmed rather than assumed, and it is why `ShellChrome` exists - one
file naming what each platform draws, so no control object branches and no test ever sees a
platform.

**The item type is a tab, not a `ShellContent`.** The design named `ShellContent` as the item
behind `Shell.Tabs`. What the platform draws, and what a test clicks, is the tab, so the item
is `ShellTab`.

**`Shell` is neither a control nor a container.** It has no element of its own that any
platform exposes - the tree is panes and hosts the app never named - so an object rooted at
"the Shell" would be rooted at nothing. `Shell` holds two collections, each rooted at what the
platform actually draws: `Shell.Tabs` and `Shell.Flyout`.

**Selection works on real tabs.** `IsSelected` reported nothing useful for the other sample's
button-built tab bar. Here the current tab reports `Selected=True` and the others `False`,
which is the coverage that suite could not give - exactly as this design predicted.

### What the fixture got wrong twice, and how

Both were found by failing tests, and both were assumptions about the platform rather than
about the app:

- **"No tab strip means we are in a flyout section."** The strip's host element survives into a
  flyout section on Windows and simply reports no items, so the reset never fired and three
  tests failed in their constructor. The question is how many tabs there are, not whether the
  strip exists.
- **"A shut flyout has no items."** Windows creates the pane on first opening and then keeps
  it, hidden. Openness is a question of visibility, and the test that asserted an empty item
  count was asserting a platform artifact that answers differently on the first run than on the
  second.

### What was deleted

`Shell` (with a `GetSelectedTab` that always returned null and an `IsLoaded` that always
returned true), `Tab`, `ShellContent` and `FlyoutItem` - and with them `NavigateTo(title)`,
`GetTab(title)`, `IsTabSelected(title)`, `WaitTabSelected(...)` and `AssertTabSelected(...)`.

One framework-wide change came with them: **test parallelization is off for the UI test
assembly**. Two fixtures now launch two different apps, and xUnit runs collections in parallel
by default, which would put both on screen at once.

## Android: what the dump said, and what it cost

The design said to dump the tree before writing tests. That is what happened, and it decided
every Android locator rather than confirming a guess.

| | Windows | Android |
|---|---|---|
| A tab | `TabItem`, named by title | a **bottom-navigation** frame layout, titled by content description |
| Tab strip | `TopNavMenuItemsHost` | nothing named - rooted at the app's content frame instead |
| Selected tab | `Selected` = true | `selected` = true |
| Flyout item | `ListItem`, named by title | a view group with a content description **and** the app's own `AutomationId` |
| Flyout while shut | items linger, hidden | items leave the tree |
| Flyout opener | button named "Open Navigation" | content description "Open navigation drawer" |
| Dismissing it | a light-dismiss layer | the back gesture |

Two surprises worth keeping:

**Android renders those tabs as a bottom navigation bar** even though they are `Tab`s inside a
`FlyoutItem`, not a `TabBar`. So the platform difference the design worried about is real here
after all - it simply did not need handling, because four tabs stay under the five that bar
shows.

**An `AutomationId` set on a `FlyoutItem` reaches Android but not Windows.** The same markup,
the same property, one platform honours it. That asymmetry is the reason `ShellChrome` addresses
flyout items by what both platforms do expose.

### Three framework changes the Android run forced

None of these are Shell-specific; all were missing before and simply had not been noticed.

- **`IMauiElement.Name`** - the accessible name, per platform: UIA Name, Android's content
  description (falling back to text), iOS's label. An Android tab has no text and no id and
  answers to nothing else, so without this a tab could not be named at all.
- **A third pass in the string key.** `Tabs["Home"]` now tries the automation id, then the
  caption, then the accessible name. The first two both come back empty on an Android tab.
- **Appium reports a missing attribute as the four characters `null`.** An element with no
  resource id was answering `"null"` when asked for its automation id, which is not absence and
  would have matched a key of that name.

### Result

**13 tests, 13 passing on Android** - the same tests as Windows, unchanged, which was the
acceptance condition this design set for step 3. Android took 50 s against Windows' 1 m 9 s.

### Three more things Windows taught us, after Android was green

**Chrome is pressed through its pattern, not clicked.** The flyout's opener sits in the window's
title-bar strip, where a synthetic pointer click is intercepted before it reaches the button -
the flyout simply never opened, with no error to say so. Asking the element to invoke itself
needs no coordinates. This is the ladder every control click already walks, applied to chrome
the app did not draw.

**The flyout does not cache its root.** The platform creates and destroys that pane as it opens
and closes, and a dead UI Automation element does not always announce itself: it can keep
answering for its type while reporting no children, which reads as a flyout that opened and
stayed empty. `CacheContainerRoot` is overridden to false there - the extension point already
existed for exactly this.

**Openness is host visibility *and* item count.** Neither half works alone, and each half was
measured on the platform that needs it: Windows keeps a dismissed pane's items in the tree, so
items alone read as open forever; Android's host is the app content frame, always visible, and
it is the items that come and go. Asking whether the *items* are visible looks tidier and is
wrong - Windows hosts an open pane in a window of its own, where its items report themselves
off-screen.

### One failure that was not real

Five Windows tests failed for a while with `Access is denied` and elements "missing" that were
plainly there. The cause was two `dotnet test` processes running at once - one started to get
more detail on the other's failures. They each launch their own app, and clicks land on
whichever window is topmost. The tell was the duration: a two-minute suite reporting **1 h 11 m**.
Run alone, the same code passed 13/13.

xUnit parallelism is off inside the assembly, which says nothing about two separate processes.

## What would make this a bad idea

Stated plainly, because it is a real possibility:

- **If per-test isolation cannot be made reliable**, a Shell suite will be flaky in the way the
  old one was, and flaky tests are worse than absent ones. The mitigation is the small app and
  the explicit pop; the check is step 3.
- **A second app is a second thing to build, install and keep working** — on Android that means a
  second APK and the reinstall hazard already found (`enforceAppInstall`). Budget for it, or
  accept a slower Android run.
- Neither is a reason not to start, but both are reasons to keep the app small.

## Android is in scope from the start

Android is where Shell differs most, so testing it later would mean discovering the design's real
constraints after the design was fixed. Four things follow.

### Probe before writing tests

Shell's Android rendering is not guessable, and every navigation problem this session was solved
by dumping the tree rather than reasoning about it. So step 1 of the work ends with
`adb shell uiautomator dump` against the Shell app, answering:

- How is a tab exposed — `content-desc`, `resource-id`, text, or none of them? MAUI maps a
  `ShellContent` title to a content description on Android, so `AutomationId` may not appear at
  all, and `Shell.Tabs["Controls"]` has to resolve by whatever is actually there.
- Is the flyout a navigation drawer, and what opens it? On Android the flyout is a drawer behind
  a hamburger whose only handle is usually a stock content description; on Windows it is a pane.
  Same control object, two different ways in.
- Do the four tab root pages expose their own `AutomationId`? `AutomationPageHandler` is
  `#if WINDOWS`, so the Windows fix for page markers does not apply here. Android exposed
  `PageHub` from a `ContentPage`, but that has not been confirmed for a Shell tab root.

The answers go in the design before any test is written. A wrong guess here is what produced the
`ScrollView`-marker detour.

### The adapter surface, named up front

| Concern | Windows | Android |
|---|---|---|
| Tab identity | `ControlTypeAndName` | content description |
| Flyout opening | pane, already present | drawer, opened by a hamburger |
| Tab reachability | all tabs on screen | 5 + **More**; the design's four stay clear of it, the flyout deliberately does not |

All three live in the element/driver layer, alongside the Android off-screen rule and the
navigation-bar nudge. None reaches a control object, and none reaches a test.

### The Android costs already known

- **A second APK.** `enforceAppInstall = true` must be set on the Shell fixture too, or a run can
  silently test the previous build — a hazard that already cost one full misdiagnosis.
- **Build and install time.** A second Android app roughly doubles the per-run Android setup.
  Keeping the app to four tabs and five pages is what keeps that tolerable.
- **Emulator stability.** The emulator died four times during this session's work and the
  UiAutomator2 instrumentation crashed once. A Shell suite adds a second app to that surface, so
  treat a single green Android run as unproven — the repeat rule from
  `.my/scroll/plan-android-performance.md` applies.

### Revised order of work

1. `ShellApp` — four tabs, one flyout. Launch on **both** platforms, then **dump the Android
   tree** and record what tabs and the flyout actually look like.
2. `ShellFixture` with `ReturnToShellRoot`, and the tab item strategy written against the dump.
3. `ShellTabTests` on Windows, then the same tests unchanged on Android. **Unchanged is the
   test** — if they need per-platform branching, the adapter is in the wrong place.
4. The collection redesign of `Shell` / `ShellContent` / `Tab`.
5. `ShellStackTests`, then `ShellFlyoutTests`, each on both platforms.

The stop condition still holds, and now applies per platform: if step 3 cannot be made reliable on
Android, stop and reconsider rather than papering over it. Learning that on four tabs is the point
of the small app.
