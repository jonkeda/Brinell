# Sub-plan: Redesign the Sample App Away From Shell

**Date:** 2026-08-29
**Parent:** [maui-control-architecture-plan.md](maui-control-architecture-plan.md)
**Scope:** `samples/Brinell.Samples.Maui.App`, `testsnew/Brinell.Maui.UITests/MauiFixture.cs`,
`testsnew/Brinell.Maui.UITests/Pages/AppShellPage.cs`
**Not in scope:** the Brinell framework itself. No control object, driver, or generator change
belongs in this plan — if one turns out to be needed, that is a finding for the parent plan.

---

## 1. Why

Phase 4's Android run reached the app and then stopped at Shell, twice:

| Divergence | Windows | Android |
|---|---|---|
| Tab addressing | `ControlTypeAndName` locator (a UIA concept) | tab is a `content-desc`; the driver rejects that strategy outright |
| Tab reachability | all 10 tabs on screen | `BottomNavigationView` shows **5 + "More"**; the rest are behind an overflow menu |

The second is the one that matters. `DisplayTab` is not merely *addressed* differently on
Android — it is **not on screen**. Every test that reaches a page through a tab is blocked by
a platform-specific menu interaction that has nothing to do with the control under test.

There is a second, older cost. [RCA-001](rca/rca-001-container-module-tests-navigation-stack.md)
was caused entirely by Shell: `GoToAsync` **pushes** onto a navigation stack, clicking an
already-selected tab does not pop it, and 9 of 10 tests failed for 7 minutes as a result. The
fix works, but it exists only because navigation carries hidden state.

**The sample app is not a Shell demo.** It exists so Brinell's control objects can be
exercised. Navigation is plumbing to reach a page — and right now that plumbing is the single
largest source of cross-platform divergence and test flakiness in the repo.

### What this is not

This is **not** "Shell is bad" or "remove Shell support". `Shell`, `ShellContent`,
`FlyoutItem`, and `Tab` control objects stay in `Brinell.Maui` and stay supported — real apps
use Shell and Brinell must test them. What changes is that the *sample app* stops using Shell
as its primary navigation, so control tests stop paying a navigation tax to reach a page.

---

## 2. Goal

> Opening any page must be one action, identical on Windows, Android and iOS, with no
> navigation state left behind.

Concretely, the fixture call a test makes today:

```csharp
_appShell.DisplayTab.Click();          // 10 tabs on Windows, 5 + overflow on Android
```

becomes:

```csharp
_hub.Open(SamplePage.Display);         // same on every platform
```

Four properties follow, and they are the acceptance criteria:

1. **Uniform** — the same interaction on all three platforms; no overflow, no per-platform
   locator strategy.
2. **Flat** — no navigation stack, so no pushed route can outlive a test (RCA-001's cause).
3. **Addressable** — every navigation affordance carries an `AutomationId` that survives to
   `resource-id` on Android and `AutomationId` on Windows.
4. **Cheap to extend** — adding a page is one entry in one list, not a `ShellContent` plus a
   route registration plus a fixture property.

---

## 3. Design: a hub page with a page registry

### 3.1 Shape

```
MainPage (NavigationPage root)
  └─ HubPage                 AutomationId="PageHub"
       └─ CollectionView     AutomationId="PageHubList"
            └─ Button        AutomationId="Open_Display"     ← one per page, generated
                             AutomationId="Open_Buttons"
                             ...
```

Opening a page is `Navigation.PushAsync`; returning is `PopAsync`. Both are single, explicit
operations with no tab bar, no overflow, and no route table.

### 3.2 The registry is the single source of truth

```csharp
public enum SamplePage { Buttons, DateTime, Display, Range, Selection, Text, Toggle,
                         Container, Collection, Shapes, Dialogs, Navigation, AutomationProbe }

public static class SamplePages
{
    public static IReadOnlyList<SamplePageEntry> All { get; } =
    [
        new(SamplePage.Buttons, "Buttons", () => new ButtonsPage()),
        new(SamplePage.Display, "Display", () => new DisplayPage()),
        // ...
    ];

    public static string AutomationIdFor(SamplePage page) => $"Open_{page}";
}
```

The hub builds its list from `SamplePages.All`, so a new page is **one line**. The
`AutomationIdFor` convention is shared by the app and the test fixture — the id is derived, not
written twice and kept in sync by hand.

### 3.3 Why a hub and not the alternatives

| Option | Rejected because |
|---|---|
| Keep Shell, add an adapter (parent §4.5 tier 3) | Solves addressing, not reachability. The overflow menu is still there, and every control test still pays for it. An adapter is the right answer for *apps that use Shell*; it is the wrong answer for the app whose only job is to expose controls. |
| `TabbedPage` | Same overflow problem on Android, plus a per-platform tab strip. |
| Flyout / hamburger | One extra interaction before every page, and the flyout itself diverges per platform. |
| Deep link per test (`app://display`) | Fast, but not what a user does — the test would stop exercising the navigation it claims to. |
| **Hub page + `PushAsync`** | **Uniform, flat, addressable, one-line extension.** A list of buttons is the most boring possible UI, which is exactly what plumbing should be. |

The last row is the point: the hub is deliberately dull. Anything more interesting would
itself become a source of platform divergence.

### 3.4 Deliberately kept

- **`NavigationDemoPage` keeps its Shell/Toolbar/Menu content.** `NavigationControlTests` has
  ~14 tests covering `Toolbar`, `Menu`, and `TabMenu` control objects. Those controls are part
  of Brinell and must stay tested. The change is that this page is *reached* through the hub
  like any other — it stops being the navigation mechanism and becomes a subject.
- **`AppShell.xaml` is retained, unreferenced**, in one commit, so the diff shows exactly what
  the app stopped doing. It is deleted in the last step, not the first.

---

> **The design below stands; its rollout failed.** Recovery and the corrected rollout are in
> [plan-sample-app-recovery-and-phase4.md](plan-sample-app-recovery-and-phase4.md). Read that
> to resume; read this for the design rationale and the §3.5 record of what broke.

## 3.5 STATUS — parked mid-implementation, uncommitted

**Do not resume by re-reading the design. Read this section first.** Steps 1–2 are done and
step 3 is partially done, in the working tree, unverified. The Windows suite is **regressed**:
117 passed / 21 failed against a baseline of 137 / 1.

### Built and working

- `Navigation/SamplePage.cs`, `Navigation/SamplePages.cs` — the registry. Adding a page is
  genuinely one entry.
- `Pages/HubPage.xaml` + `.xaml.cs` — buttons built from the registry, ids derived by the
  shared `Open_{page}` convention.
- `App.xaml.cs` — root is `NavigationPage(HubPage)`. **All three platforms build.**
- `testsnew/.../Pages/HubPage.cs` — hub page object with `OpenButton`, `TryGoBack`.
- `MauiFixture` — rewritten around `Open(SamplePage)`; all existing `NavigateToX` helpers kept
  their names and reset behaviour.
- 17 test call sites migrated from `AppShell.XTab.Click()` to `Open(SamplePage.X)`.
- **RCA-001's `EnsureProbeModuleLinksReachable` workaround deleted** — the flat hub removes
  the pushed-route state it existed to recover from.

### Two defects the rewrite exposed

**1. Cached page objects held stale container roots — FIXED.** The fixture cached page
objects, and container objects cache their resolved *root element*. That only ever worked
because Shell retained page instances; the comment said so outright ("cached so the form and
collection keep their container-root caches"). The hub builds a fresh page per open, so every
cached root was stale. Page objects are now created per access. Recovered 6 tests, 27 → 21
failures.

**2. `ReturnToHub` used `Driver.NavigateBack()` — FIX WRITTEN, NOT VERIFIED.** This is the
cause of the remaining 21, and the diagnosis is concrete:

```
WindowsInteractionPolicyException : The 'NavigateBack' action requires global keyboard input,
but BRINELL_WINDOWS_ALLOW_GLOBAL_KEYBOARD_INPUT is not enabled.
  at MauiFixture.ReturnToHub()  at MauiFixture.Open(SamplePage)  at SwitchTests..ctor
```

On Windows `NavigateBack` falls back to **Alt+Left**, which the interaction policy blocks by
default, so every test after the first in a class threw *in its constructor*. Toggle shows it
worst only because it has the most tests per class. Text, Container, Navigation and Collection
fail for the same reason — they are one bug, not five.

The written fix: a **"Back" `ToolbarItem` with `AutomationId="BackToHub"`**, added by
`HubPage.AddBackToHub` to every page it opens, plus `HubPage.TryGoBack` on the page object.
A click needs no keyboard permission and is the same gesture on all three platforms.

**It compiles — app and tests both build — but the test run was never completed.** Two
attempts were interrupted because the app hung on screen. Whether the hang is the
`ToolbarItem` command, something else in the hub, or unrelated is **unknown and is the first
thing to establish on resuming.**

### Android was tried too — a third defect, undiagnosed

Worth trying because defect 2 is Windows-only: `AppiumMauiDriver.NavigateBack()` is
`_driver.Navigate().Back()`, the native back button, with no keyboard policy to violate.

**The hub design works on Android.** The failure message names the hub and its button —
`Page 'PageHub' is not loaded, so 'AutomationId:Open_Display' cannot be found` — so the
fixture reached the app, found the hub page object, and looked for the right control. The
registry, the id convention and the `Open` primitive all behaved.

**But the app itself now exits at startup on Android.** After a clean uninstall/reinstall of
the freshly built APK:

- `am start` reports `Status: ok`, `LaunchState: COLD`
- seconds later there is **no process**, and focus is back on the launcher
- logcat shows **no `FATAL`, no `AndroidRuntime`, no managed exception** — the earlier
  Fast Deployment abort message is gone, so that is not this

This did not happen before the hub. The Shell build launched and stayed up on this same
emulator. So something in `HubPage` — most likely the same code path that hangs the Windows
app — kills startup on Android, silently.

**The Windows hang and the Android silent exit are probably one bug.** Both appeared with the
same commit; both involve the hub's page construction or its `ToolbarItem`. Diagnose them
together rather than separately.

### Resume here

**Start with the app, not the tests.** Both platforms now misbehave at startup — Windows
hangs, Android exits silently — and no test result means anything until the app is stable.

1. **Run the app by hand on Windows** (`dotnet build -t:Run` or launch the exe) with no test
   harness. Does the hub render? Does clicking a button open a page and Back return? This is
   the fastest signal and needs no emulator.
2. **Suspect `HubPage.AddBackToHub` first.** It is the newest code and the only part doing
   anything unusual: a `ToolbarItem` whose `Command` calls `await PopAsync()`. If that
   deadlocks or throws during page construction it would explain a hang on one platform and a
   silent exit on the other. Try a plain `Button` inside the page content instead of a
   `ToolbarItem`, or make the command synchronous with a fire-and-forget pop.
3. Failing that, bisect the hub: comment out `AddBackToHub`, then the whole
   `BuildPageList`, until the app starts cleanly. The registry and `App.xaml.cs` root change
   are simple enough to be unlikely culprits, but rule them out rather than assume.
4. Only once the app is stable on Windows: re-run `Tests.Toggle`, then the full filtered
   suite. The 21 failures share one cause and should collapse together.
5. Then Android — the emulator path is proven working and documented in the parent plan's
   phase 4.

### Lessons already paid for

- **"No test changes" was wrong.** 17 call sites used `AppShell.XTab.Click()` directly. The
  compiler caught it immediately, but the sub-plan should not have claimed otherwise.
- **The design under-estimated what Shell was providing**: page-instance retention (defect 1)
  and a policy-exempt back gesture (defect 2). Both were invisible until removed.

---

## 4. Steps

Each step ends with the Windows suite at its current baseline (137 passed / 1 failed excluding
the phase-7 parked tests). **Windows must stay green throughout** — this plan may not trade
Windows coverage for Android progress.

### Step 1 — Registry and hub, alongside Shell

Add `SamplePage`, `SamplePages`, `HubPage`. Do **not** remove Shell yet. Both exist; nothing
references the hub. This step is additive and cannot break anything.

**Done when:** the app still starts on Shell, and the hub page compiles and renders when
navigated to manually.

### Step 2 — Switch the app root, keep Shell reachable

`App.CreateWindow` returns `new NavigationPage(new HubPage())`. `AppShell.xaml` stays in the
project but is no longer the root.

**Done when:** the app opens on the hub on Windows and Android, and every page opens from it.

### Step 3 — Move the fixture to the hub

`MauiFixture` gains `Open(SamplePage)`; `HubPage` gains a page object. The tab-based
navigation helpers (`NavigateToMain`, `NavigateToAutomationProbe`,
`NavigateToContainerModule`, …) are rewritten in terms of `Open`, keeping their names and
their reset behaviour so no test changes.

**This is where RCA-001's fix becomes unnecessary.** `EnsureProbeModuleLinksReachable` and its
`NavigateBack` loop exist solely to unwind pushed Shell routes. With a flat hub the recovery
is `PopAsync` back to the hub — or nothing at all, since each `Open` starts from the hub.
Delete the workaround and note it in RCA-001 rather than leaving dead recovery code.

**Done when:** the Windows suite matches baseline with no test-body changes.

### Step 4 — Android verification

Re-run the smoke set. This is the step the whole sub-plan exists for.

**Done when:** the basics smoke set (Button, Label, Entry, CheckBox, Switch) passes on
Android — or each remaining failure is a control-level defect with a named cause, *not* a
navigation one.

### Step 5 — Remove Shell from the app

Delete `AppShell.xaml`, its code-behind, and the route registrations. `AppShellPage` and its
`ShellContent` members go with them.

**Done when:** `grep -rn "Shell" samples/Brinell.Samples.Maui.App` returns only
`NavigationDemoView`'s deliberate Shell-control content.

---

## 5. What this costs, honestly

**Shell navigation stops being covered by the sample app.** Today `AppShellPage` exercises
`ShellContent` on Windows, and after step 5 nothing does. That is a real reduction in coverage
of a real Brinell control, and it should not be waved away.

Two mitigations, and the second matters more:

1. The `Shell`, `ShellContent`, `Tab` and `FlyoutItem` control objects remain, with their unit
   tests in `Brinell.Maui.Tests`.
2. **A dedicated `ShellNavigationPage` is added to the sample app as a *subject*** — a small
   page hosting a nested Shell, reached from the hub like everything else. Shell coverage then
   comes from a page that tests Shell, rather than as a side effect of how every other test
   reaches its page.

Mitigation 2 is a step 5 deliverable, not an aspiration. Without it this plan trades a
navigation problem for a coverage hole.

---

## 6. Risks

| Risk | Response |
|---|---|
| Regressing Windows while chasing Android | Every step ends at the Windows baseline; step 1 is purely additive |
| A long hub list needing scrolling on a small screen | `CollectionView` scrolls, and Brinell's collection support already handles that — it is a supported case, not a new one |
| Android reveals *more* divergence past navigation | That is the plan working. Each finding is triaged into a parent-plan §4.5 tier, not patched in the sample app |
| Shell coverage silently lost | §5 mitigation 2 is a step 5 deliverable with its own done-condition |
| Sample app reshaped to suit the test framework (parent §4.4 warns against this) | The hub is *simpler* markup than the Shell it replaces, is platform-neutral, and removes a workaround rather than adding one. Worth stating explicitly in review |

---

## 7. Open questions

1. **Does `MainWindow.xaml` / `MainPage.xaml` still have a role?** Both exist and their
   relationship to `AppShell` is unclear. Resolve during step 2 rather than carrying two
   unused roots forward.
2. **Should the hub group pages by category** (Controls / Containers / Modules)? Only if the
   flat list becomes unusable — grouping adds a second interaction and undoes goal 1.
3. **Does the parent plan's phase 6 still need a Shell adapter?** If the sample app no longer
   navigates by Shell, the two divergences recorded there lose their in-repo reproduction.
   The adapter is still likely right for *user* apps, but the evidence for it would come from
   the new `ShellNavigationPage` (§5) rather than from every test.
