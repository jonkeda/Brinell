---
title: Stage A Results — The Bridge Exists
description: What steps 4 to 12 answered, including the four findings that change later steps
status: result
---

# Stage A Results — The Bridge Exists

The architecture is proven. A custom UI Automation pattern, hung off a sidecar window, drives a
MAUI control from a test process — including controls that publish no `AutomationId` on Windows
and that no existing Brinell mechanism could reach at all.

| What | Where | State |
|---|---|---|
| Contract and interop | `srcnew/Brinell.Uia.Contracts` | 43 tests green |
| Bridge spike harness | `testsnew/Brinell.Uia.TestHost` | plain Win32, no MAUI |
| Spikes and contract tests | `testsnew/Brinell.Uia.Tests` | 43 green, under a second |
| MAUI provider | `samples/Brinell.Maui.AppSupport/Uia` | 3 target frameworks build |
| FlaUI client | `srcnew/Brinell.Maui.FlaUI/Bridge` | — |
| End-to-end tests | `testsnew/.../Tests/Gestures` | 7 green, 6 s |
| Bridge diagnostics | `testsnew/.../Tests/Diagnostics` | 2 reporting tests |

---

## Step 4 — Child HWND raw provider: **yes**

A 1×1 `WS_CHILD` window parented to the app's top-level window answers `WM_GETOBJECT` with a
raw fragment root, and the fragment joins the app's automation tree. Proven twice: against a
bare Win32 host, and inside the MAUI sample app on WinUI 3.

Measured inside the real app, raw view, directly below the window:

```
class='WinUIDesktopWin32WindowClass' id='' name='Brinell MAUI Sample'
  class='Microsoft.UI.Content.DesktopChildSiteBridge' id='' name=''
  class='BrinellUiaBridge'  id='BrinellUiaBridge.Root'      name='BrinellUiaBridge'
    class='BrinellUiaTarget' id='BrinellUia.TestSwipeView'   name='TestSwipeView'
    class='BrinellUiaTarget' id='BrinellUia.TestRefreshView' name='TestRefreshView'
```

**And it does not disturb the app's tree.** The 26 container and diagnostics tests pass with the
bridge attached, `AutomationProbeTests` included — the guard against the tree collapse that
`AutomationRemainingHandlers.cs` records. D-6 is not needed; the alternatives (no `WS_VISIBLE`,
owner window instead of child) were not required.

## Step 5 — Pattern round-trip, int and string: **yes, both**

An `int` triple and a UTF-16 string both survive the trip in each direction, out of process.
Verified with values chosen to expose a bad offset or a narrowing codec: `Pan(1234567, -89)`
comes back as `107:1234567:-89`, and `"Grüße, Ελλάδα, 日本語, 🚀 end"` round-trips intact,
surrogate pair included. 200 consecutive exchanges hold.

**So the method table can carry strings**, which is what steps 14 and 20-26 need, and what
decided step 7.

**Two of seven hand-declared IIDs were wrong**, and the symptom was a bare `E_NOINTERFACE` that
named nothing:

| Interface | Correct IID |
|---|---|
| `IUIAutomationRegistrar` | `8609c4ec-4a1a-4d88-a357-5a66e060e1cf` |
| `IUIAutomationPatternInstance` | `c03a7fe4-9431-409f-bed8-ae7c2299bc8d` |

Both were wrong only in their last six bytes. They are now read off the machine's registry
rather than transcribed, and the recipe is in a comment at the top of `UiaInterfaces.cs`.

**Pattern ids are per-process, and the trap is that they often agree.** Both processes were
handed `50000` on the same run — each process's first custom pattern starts there. An
implementation that hardcoded `50000` would appear to work and would break the first time
anything else registered a pattern first. Each side looks its own id up; the GUID is the
contract.

## Step 6 — Raw-view findability: **yes, and one consequence**

| View | Bridge visible |
|---|---|
| Raw | yes |
| Control | **no** |
| Content | **no** |

`IsControlElement=false` and `IsContentElement=false` are honoured, so Narrator, Voice Access and
Magnifier — all of which walk the control and content views — never see the bridge. D-5 stands as
designed.

**The consequence for step 12: `FindFirstDescendant` does not reach a raw-view-only element.**
Measured, and it changed the client: every bridge lookup walks the raw view explicitly. Had this
gone unmeasured, the client would have reported every app as having no bridge.

## Step 7 — The frozen contract

**Two methods, and it stays two.** UI Automation dispatches by index into the table given at
registration, so a third method is a silent misroute the day a client and a provider are built
from different revisions.

| Index | Method |
|---|---|
| 0 | `Invoke(int verb, int arg1, int arg2)` |
| 1 | `Exchange(int verb, string argument, out string result)` |

**No custom properties and no custom events**, departing from the design. Both were considered:
a property would let a client read capabilities through a cache request rather than a call,
which matters when reading one property off a thousand elements. The bridge is never asked about
more than a handful, so the saving is nil while the cost is real — each property adds a
descriptor, a type tag and a marshalling path that fails silently when wrong. Capabilities are
read with `GetCapabilities` instead.

**Verbs are numbered in ranges of a hundred**, one per capability area, append-only. Ranges let a
client ask an older provider "do you do text at all" about a verb that provider has never heard
of. `GestureKind` became `BrinellVerb`, with gestures as the 100 range.

| Range | Area | Range | Area |
|---|---|---|---|
| 1–99 | meta | 500–599 | navigation |
| 100–199 | gestures | 600–699 | state reads |
| 200–299 | focus | 700–799 | date and time |
| 300–399 | text | 800–899 | selection |
| 400–499 | scrolling | 900–999 | dialogs and menus |

GUIDs, method order and verb numbers are pinned by `ContractTests`, written as literals rather
than as references — a test asserting `PatternGuid == BrinellUiaIds.PatternGuid` would pass
whatever anyone changed it to.

## Steps 8–12 — What was built

Step 9's "single hardcoded child" was skipped: step 11 replaces it immediately, and building the
registry directly avoided writing code whose only purpose was to be deleted.

**The bridge machinery is framework-neutral and lives with the contract.** `Brinell.Uia.Contracts`
holds the window, the fragment tree, the pattern and the marshalling; the only MAUI-aware piece
is `IBrinellVerbTarget`'s implementation. That keeps the copyable unit whole: an app that cannot
take a project reference copies one folder.

**Linked, not referenced.** `Brinell.Maui.AppSupport` compiles the contract sources rather than
referencing the project, preserving its "copy me" property. `Brinell.Maui.FlaUI` references it
normally.

**The test-facing API is `MauiGesture`, not `BrinellVerb`.** Wire numbers are a Windows detail;
on Android the same gesture is real touch input. The mapping lives in the FlaUI driver, the only
place that knows about both.

**Gestures are addressed by `AutomationId`, not by element** — `IMauiDriver.PerformGesture(id,
gesture)`. Both controls proven here publish no `AutomationId` on Windows at all, so there is no
element to hang a method on. The element-level `IMauiElement.SupportsGesture` remains for
elements that are addressable. Both are default interface members, so the Appium head compiles
unchanged.

---

# Four findings that change later steps

## 1. `using:` in shared XAML does not reach a referenced assembly

`xmlns:uia="using:Brinell.Maui.AppSupport.Uia"` compiled with **zero warnings** and then killed
the app at runtime:

```
XamlParseException: Type GestureAutomation not found in xmlns using:Brinell.Maui.AppSupport.Uia
```

`using:` without an assembly means *this* assembly. An app that **references** AppSupport must
write `clr-namespace:Brinell.Maui.AppSupport.Uia;assembly=Brinell.Maui.AppSupport`; an app that
**copies** the sources uses `using:`. Both routes are supported, and they need different markup —
step 30's documentation must say so.

The crash presented as `Exception code: 0xc000027b` in `Microsoft.UI.Xaml.dll` and reached the
test as `MissingRoot` on every element, which is indistinguishable from the automation tree
collapsing. That is why `BRINELL_APP_CRASH_LOG` now exists.

## 2. Provider property getters must marshal to the UI thread, not only verbs

UI Automation reads `BoundingRectangle` and `IsOffscreen` from its own threads whenever anything
walks the tree — far more often than it calls a method. Reading a WinUI element's layout from the
wrong thread does not throw something catchable: it kills the app from inside
`Microsoft.UI.Xaml.dll`. The design's threading note covered dispatch; it has to cover every
callback that touches the framework.

## 3. The incoming page loads before the outgoing page unloads

An element that registers on `Loaded` and unregisters on `Unloaded` produces, on every
navigation: new element registers, old element unregisters — and the second call removes the
first one's registration, because they share an `AutomationId`. The bridge worked on the first
visit to a page and was empty on every visit after.

`Unregister` now takes a predicate and removes only the caller's own registration. Anything else
built on `Loaded`/`Unloaded` pairs has the same hazard.

## 4. `SwipeView.Open` succeeds and reveals nothing to UI Automation

The verb reaches `SwipeView.Open(OpenSwipeItem.LeftItems)` and returns success. The app's
automation tree is identical before and after — no `SwipeItem`, named or unnamed. A MAUI
`SwipeItem` is a `MenuItem` rather than a `View`, and WinUI's `SwipeControl` publishes nothing
for it.

**So step 18's gesture surface must be backed by a `SwipeGestureRecognizer` with a command**,
whose outcome a test can observe, rather than by `SwipeView`'s built-in items. The
`SwipeView` case is recorded as a reported measurement, following the `AutomationProbeTests`
precedent rather than failing on a platform limitation.

---

# A defect the bridge found on its first run

`ContainerViewModel.IsRefreshing`'s setter called `Refresh()`, and `RefreshView` already runs its
`Command` whenever `IsRefreshing` becomes true. One pull-to-refresh counted **two** refreshes.

It had gone unnoticed because no test could reach the gesture: the `RefreshView` is not
addressable on Windows, and the only route to a refresh was `TriggerRefreshButton`, which runs
the command directly and so fired once. The first gesture that reached the control reported
`refreshed 2` from a single swipe. The same double-count would occur on a real pull on Android.

Fixed in the sample app. This is the case for the programme in miniature: the bug was not in the
test framework, and it was invisible until a gesture could be driven at all.

---

# Diagnostics added

Both are off unless the variable is set, and both exist because an app under test launched by a
test run has no console, no debugger, and no way to say what happened.

| Variable | What it records |
|---|---|
| `BRINELL_UIA_LOG` | every publish, registration and verb dispatch, with its HRESULT |
| `BRINELL_APP_CRASH_LOG` | unhandled exceptions in the sample app |

Plus `FlaUIMauiDriver.DescribeGestureBridge()`, which prints the raw tree below the app window.
It separates the three ways a gesture goes missing — no bridge window, a bridge window whose
provider never answered, or a fragment root with nothing on it — which otherwise reach a test as
the same sentence.

---

# What Stage A does not claim

- **One gesture verb has an observed effect**, not seven. `SwipeDown` on `RefreshView` runs the
  full circuit; `SwipeRight` on `SwipeView` is accepted and unobservable. The dispatcher ladder
  has rungs for taps and swipe recognizers, and **no test exercises them yet** — step 18.
- **No verb outside the gesture range is implemented.** Focus, text, dates, scrolling and the
  rest return `UIA_E_NOTSUPPORTED` by name. Those are steps 13, 14 and 20–26.
- **Nothing here is security-gated.** The bridge compiles into any build that includes the
  sources. Step 27 removes it from a shipping build, which is the only real control UI
  Automation offers.
- **The soak and lifetime tests are step 28.** `UiaDisconnectProvider` is called on window close
  and on `Shutdown`, and nothing yet proves the provider count does not grow.
