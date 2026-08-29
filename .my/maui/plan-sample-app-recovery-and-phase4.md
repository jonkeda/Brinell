# Plan: Recover the Sample App and Finish Phase 4

**Date:** 2026-08-29
**Parent:** [maui-control-architecture-plan.md](maui-control-architecture-plan.md)
**Supersedes:** the implementation approach in
[sample-app-navigation-redesign.md](sample-app-navigation-redesign.md). That document's
*design* (hub + registry) stands; its *rollout* is what failed and is replaced here.

**Goal, in the words it was asked in:** make testing on Android easy, and make adding pages
easy. Nothing else in this plan matters more than those two.

---

## 1. Where we actually are

| | State |
|---|---|
| Windows UI suite | **117 passed / 21 failed** (baseline 137 / 1) |
| Windows app | hangs on screen during test runs |
| Android app | starts, then exits within seconds — no `FATAL`, no managed exception |
| Committed | **nothing** — all of it is working-tree only |

Two of my three diagnoses were wrong, and it is worth being precise about that because the
plan below is shaped by it:

- **"It's `AddBackToHub`"** — wrong. That method only runs on a button click, so it cannot
  affect startup on either platform.
- **"It's `ShellAutomationMapper` still running"** — wrong for Android. It is inside
  `#if WINDOWS`, so it does not execute there at all.

**The honest position: the cause of the startup failure is unknown.** Three changes landed
together — the registry, the hub page, and the app-root switch — plus a fixture rewrite and 17
test edits, and none of it was verified by running the app. That is the actual mistake, and it
is what section 2 fixes.

---

## 2. The rule this plan is built on

> **Run the app before running the tests.**

Every failure in this episode was found through a UI test, which is the slowest and least
informative instrument available. A hung Windows app and a silently exiting Android app are
both visible in seconds by launching the app; instead they surfaced as 21 red tests and an
`ElementNotFoundException` about a page object.

So every step below has a manual app-launch gate before any test runs. The gate is cheap
(seconds), needs no emulator or Appium, and would have caught all three defects immediately.

A second rule follows from the same evidence:

> **One change per verification.** The hub, the root switch and the fixture rewrite went in
> together, so when it broke there were three candidates and no way to bisect. Below, each
> lands and is verified alone.

---

## 3. Step 0 — Get back to green

**Revert the working tree to the Shell baseline.** Not because the hub is wrong, but because
an unverified, uncommitted, half-migrated tree is a worse starting point than a known-good
one, and the hub is cheap to reapply from the registry files, which are sound.

```powershell
git stash push -m "hub-attempt-1"      # keeps it recoverable, does not discard
```

**Done when:** the Windows suite is back at 137 passed / 1 failed and the app launches by
hand.

✅ **Done.** Suite 137/1; app launched by hand and stayed up (PID confirmed, ~254 MB). Plan
documents and the untracked hub/registry/iOS/mobile-project files were kept; everything else
returned to `stash@{0}` ("hub-attempt-1").

**Keep from the attempt** (they were never the problem): `Navigation/SamplePage.cs` and
`Navigation/SamplePages.cs`. Re-apply them in step 1.

---

## 4. Step 1 — The hub, behind a switch, verified by hand

Reapply the registry and `HubPage`, but **do not change the app root yet**. Instead make the
root selectable:

```csharp
// App.xaml.cs
MainPage = UseHubNavigation
    ? new NavigationPage(new HubPage())
    : new AppShell();

// A build-time constant, not a runtime flag: the two roots must never both be live.
public const bool UseHubNavigation = false;   // flipped in step 3
```

With the flag off, the app is byte-for-byte the Shell app and the suite must still be 137/1.

**Verification, in this order:**

1. Suite still 137/1 with the flag **off**.
2. Flip the flag locally, `dotnet build -t:Run` on Windows, **look at the app**: does the hub
   render, does a button open a page, does back return?
3. Same on Android: build the APK, `adb install`, `adb shell am start`, confirm the process
   is still alive after 10 seconds and the hub is on screen.
4. Flip the flag back off and commit.

**Step 1 ends with the hub proven to work on both platforms and not yet used by any test.**
That is the state the previous attempt never reached, and skipping it is what cost the day.

If the app fails at (2) or (3), bisect *inside the hub*: empty `BuildPageList`, then a single
hard-coded button, then the registry loop. The failure is in ~60 lines of code, and with a
manual launch each iteration is seconds.

### ✅ Step 1 done on Windows — and it found the real bug in one launch

The manual gate paid for itself immediately. With the flag on, the app **started and stayed
up**, so the hub was never the crash. Inspecting the window told the real story:

```
PID: 58172   MainWindowTitle: ''   UIA WINDOW: '' class=WinUIDesktopWin32WindowClass
```

**The window had an empty title.** `Shell` surfaced its `Title` as the native window title;
`NavigationPage` does not. `MauiTestFixtureBase` attaches to the app *by window title*, so it
waited for a window that could never match — which presents as a **hang**, not an error,
because there is nothing to report except a wait that does not end.

That is the whole of the "Windows hang", and none of my three earlier guesses
(`AddBackToHub`, `ShellAutomationMapper`, a `PopAsync` deadlock) were right. It was never
reachable through a test suite, because the suite could not attach to the app in the first
place.

**Fix:** `App.CreateWindow` sets `window.Title = WindowTitle` explicitly.

Verified by UIA inspection of the running app — 31 elements, the hub present and every
registry button addressable:

```
WINDOW: Brinell MAUI Sample
PageHubTitle, PageHubList, PageHubScroll,
Open_Buttons, Open_DateTime, Open_Display, Open_Range, Open_Selection,
Open_Text, Open_Toggle, Open_Container, Open_Collection, Open_GridCollection
```

The registry, the `Open_{page}` id convention, and the hub rendering are all confirmed
working on Windows before a single test depends on them.

### ✅ Step 1 done on Android — the hub delivers what Shell could not

All three heads build with the flag on. On the emulator, after install and launch, the UI
hierarchy dump shows **every one of the 14 pages addressable, plus the hub root itself**:

```
PageHub, PageHubTitle, PageHubList, PageHubScroll,
Open_Buttons, Open_DateTime, Open_Display, Open_Range, Open_Selection, Open_Text,
Open_Toggle, Open_Container, Open_Collection, Open_GridCollection, Open_Shapes,
Open_Dialogs, Open_Navigation, Open_AutomationProbe
```

Compare that with the Shell build on the same emulator, which exposed **five tabs and a
"More" overflow**, no addressable root, and nine pages simply not on screen. This is the
whole justification for the redesign, measured rather than argued.

**A measurement error worth recording:** the first check reported the app dead. It was not —
`pidof` ran seconds after a fresh install, during first-launch initialization. Re-checking at
t+4s and t+8s showed the process alive and holding window focus, and logcat had said so all
along (`Displayed ... +1s343ms`, no `FATAL`). The earlier "Android exits silently" finding in
the previous attempt's §3.5 was probably this same false negative. **Give a freshly installed
Android app ~10 s before concluding anything about it.**

---

## 5. Step 2 — Back navigation that needs no permission

`ReturnToHub` must not use `IMauiDriver.NavigateBack()`. On Windows that falls back to
Alt+Left — global keyboard input, blocked by the interaction policy — which is defect 2 and
the direct cause of most of the 21 failures.

**On Android it is fine** (`_driver.Navigate().Back()` is the native back button), so this is
a Windows constraint that the design must absorb rather than a cross-platform one.

The plan expected to need an in-page `Button`, on the grounds that a `ToolbarItem` renders
into native chrome that differs per platform. **Measured, and that concern was unfounded** —
the existing `ToolbarItem` is addressable on both:

| Platform | How `BackToHub` appears |
|---|---|
| Windows | `AutomationId="BackToHub"`, alongside the framework's own `NavigationViewBackButton` |
| Android | `content-desc="BackToHub"`, alongside the framework's `Navigate up` |

Both were confirmed by opening a page and dumping the tree — on Windows by invoking
`Open_Text` through UIA, on Android by tapping it and running `uiautomator dump`. **Keep the
`ToolbarItem`**; the substitution would have been change without reason.

Note each platform also supplies its own back affordance (`NavigationViewBackButton` /
`Navigate up`). `BackToHub` is preferred anyway: one id, same name everywhere, no dependence
on framework chrome that may be renamed or restyled.

---

## 6. Step 3 — Migrate the tests, in two halves

The previous attempt migrated the fixture and 17 test call sites at once. Split it:

**3a. Fixture only.** Flip `UseHubNavigation` to `true` and rewrite `MauiFixture.Open` plus
the `NavigateToX` helpers. Leave `AppShellPage` and its members in place so nothing else
breaks. Run the suite.

**3b. Test call sites.** Replace the 17 `AppShell.XTab.Click()` calls with
`Open(SamplePage.X)`. Run the suite.

If 3a is green and 3b is red, the fault is in the call sites; if 3a is red, it is in the
fixture. One bisect step, for free.

**Two things the previous attempt learned the hard way, already accounted for:**

- **Page objects must not be cached in the fixture.** Container objects cache their resolved
  root element, and that only worked because Shell retained page instances. The hub builds a
  fresh page per open. Create page objects per access.
- **"No test changes" was false.** 17 call sites use `AppShell` directly. Budget for it.

**Done when:** Windows is back to 137 passed / 1 failed — the same one being the phase-7
`Switch_ClickTwice_TogglesOff`.

### Status: Shell removed, 120 passed / 16 failed (was 1 passed / 39 failed)

**The 3a/3b split did not work and was abandoned.** Running the fixture on the hub while the
tests still clicked Shell tabs produced 39 failures in 8 minutes — every tab click waiting out
its timeout against a tab that no longer existed. The two halves are not separable: the app
has one root, so the fixture and the call sites must move together. Recorded because the plan
asserted otherwise.

**Shell is now fully removed** (per the decision to stop maintaining two roots):

| Removed | Why |
|---|---|
| `AppShell.xaml` + code-behind | replaced by the hub |
| `App.UseHubNavigation` flag | one root, no dual state |
| `ShellAutomationMapper` + its `MauiProgram` call | mapped `ShellContent.AutomationId` onto WinUI tabs; nothing to map |
| `AppShellPage`, the fixture's `AppShell` property | no Shell to drive |
| `TabBarCapacityProbeTests` | measured how many Shell tabs Windows could reach — a limit that no longer exists |
| `AutomationProbePage`'s module-link buttons | existed only because the tab bar was full |

**A third defect found and fixed — the same class as the other two.** `TryGoBack` could never
find its button: `HubPage.Name` is `"PageHub"`, and `RequiresLoadedPage` defaults to `true`,
so the lookup was gated on the hub being loaded — which is false exactly when a page is open
and the back button is needed. Setting `RequiresLoadedPage => false` on the hub page object
(the [RCA-002](rca/rca-002-page-precondition-discarded-slow-failures.md) opt-out) took the
Display tier from **14 failed / 3m31s to 4 failed / 34s**.

That is three defects in a row caused by something Shell was quietly providing: page-instance
retention, a policy-exempt back gesture, and now a page root that was always present.

**16 failures remain, undiagnosed**, in Container/Collection scoping, Toggle, SearchBar,
ActivityIndicator and one Toolbar test. Several pass in isolation, so ordering or timing is
implicated rather than the hub itself.

### ⏸ PAUSED HERE — and the remaining 16 are probably not a hub problem

Paused on the judgement that **the cause lies elsewhere**, not in the navigation redesign.
The evidence supports that reading:

- The hub is verified working end to end on **both** platforms — window titled, all 14 pages
  addressable, `BackToHub` present, pages open and close.
- The three defects found so far were each in *framework or fixture* code that Shell had been
  masking, not in the hub: page-object caching, the `NavigateBack` keyboard policy, and the
  `RequiresLoadedPage` gate. **A fourth of the same kind is more likely than a hub fault.**
- The failures cluster in container/collection *scoping* and toggle *state* — the same areas
  as [RCA-001](rca/rca-001-container-module-tests-navigation-stack.md) and
  [RCA-002](rca/rca-002-page-precondition-discarded-slow-failures.md), both of which turned
  out to be one shared cause wearing many faces.

**Where to look first when resuming**, in order of suspicion:

1. **A fourth "Shell was providing this" dependency.** The pattern has held three times. Look
   for anything that assumed a long-lived page instance or an always-present page root —
   `ContainerObjectBase`'s root caching is the obvious candidate, since the hub now supplies a
   fresh page on every open and container roots are cached per object.
2. **`Open`'s missing settle.** `Open` clicks and returns without waiting for the new page to
   be loaded. An earlier attempt at `_hub.WaitLoaded(false, …)` made things worse, but the
   right wait is probably on the *target* page being loaded, not on the hub being gone.
3. **Only then the tests themselves.** Several pass in isolation, so start by running one
   failing class alone, then with its neighbours, and compare.

**State:** nothing committed. `stash@{0}` ("hub-attempt-1") still holds the first attempt.
The Shell baseline is recoverable with `git checkout -- .` plus restoring the deleted Shell
files from git.

---

## 7. Step 4 — Android, which is the point of all this

Environment (proven working, from the parent plan's phase 4):

```powershell
emulator -avd Medium_Phone -no-snapshot-load
appium --port 4723                       # uiautomator2 driver
$env:APPIUM_PLATFORM = "android"
dotnet test testsnew\Brinell.Maui.UITests.Mobile --filter "Control=Label"
```

Four fixes from the earlier run are already in place and must stay: the `APPIUM_PLATFORM`
override, the `Appium.Net` assembly name, `EmbedAssembliesIntoApk`, and `appWaitPackage`.

Expected remaining work, in order:

1. **Page `AutomationId`s are named for tabs.** Every page carries `AutomationId="ButtonsTab"`
   and so on — named for a Shell tab that no longer exists. Rename to match the page objects
   (`ButtonsPage`, `TogglePage`, …). This is a mechanical rename across 14 XAML files and
   their page objects, and it removes a genuine source of confusion.
2. **Run the basics smoke set** (Button, Label, Entry, CheckBox, Switch) and triage each
   failure into a parent-plan §4.5 tier. Expect control-level divergence — that is the
   finding phase 6 needs, not a defect to patch here.

**Done when:** the smoke set passes on Android, or each failure is a recorded control-level
divergence with a named cause and a tier.

---

## 8. Step 5 — Make adding a page genuinely one line

The other half of the request. After step 4, adding a page must be:

```csharp
new(SamplePage.MyThing, "My Thing", "What it covers", () => new MyThingPage()),
```

Everything else follows: the hub renders the button, the id is derived, `Open(SamplePage.MyThing)`
works, and both platforms get it at once.

**Two guards worth adding, both cheap:**

- A unit test in `Brinell.Maui.Tests` asserting every `SamplePage` enum member has a registry
  entry. `SamplePages.Find` already throws with a clear message; a test makes it a build-time
  failure rather than a runtime one.
- A note in the sample app's README describing the one-line addition, so the next person does
  not reinvent tab plumbing.

**Done when:** a page can be added and reached by a test without touching XAML, the fixture,
or any navigation code.

---

## 9. What gets removed, and when

**Not before step 4 passes.** `AppShell.xaml`, its code-behind, the route registrations,
`AppShellPage`, and `ShellAutomationMapper.Configure()` all stay until Android is green, so
that reverting remains a flag flip rather than an archaeology exercise.

Then, per the previous sub-plan's §5, Shell coverage does not simply vanish: a
`ShellNavigationPage` is added as a **subject**, reached from the hub like any other page, so
`ShellContent` and the Shell control objects stay tested by a page that tests Shell.

---

## 10. Risks

| Risk | Response |
|---|---|
| The startup failure recurs and is still unexplained | Step 1's manual gate catches it in seconds with only ~60 lines of new code in play — the previous attempt had three changes and a test suite between the fault and the signal |
| Another "Shell was silently providing this" surprise | Two are known (page-instance retention, policy-exempt back). Step 1 and step 3 each verify by hand before trusting a suite |
| Renaming 14 page `AutomationId`s breaks Windows tests | It is mechanical and compiler-checked on the page-object side; do it as its own commit with a suite run |
| Scope drift into phase 6 adapters | Android divergences get recorded and tiered, not fixed here |

---

## 11. Why this will work where the last attempt did not

The design was never the problem — the hub reached the app on Android and looked for exactly
the right button. What failed was rollout discipline: three coupled changes, no manual
verification, and a test suite used as the primary debugger.

This plan changes only that: a flag so the two roots can coexist, a manual app launch before
every test run, and one change per verification. The hub itself is reapplied nearly as
written.
