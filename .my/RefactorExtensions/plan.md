# Plan — Refactor extensions (steps 100-108)

**Status: all stages implemented and verified 2026-09-13; K6 as option B.** See § Results at the
end, and [k6-hierarchy-decision.md](k6-hierarchy-decision.md).

This plan follows the quiet run (steps 1-49). The gesture bridge now does most of the interaction
on Windows. That leaves routes, helpers and base-class shapes that were built before it existed.
Each step below starts with the question as it was asked, then what the code shows today, then a
recommendation and how to check it.

Each step's answer is based on the code as it is on 2026-09-13, with file and line references.
Where an answer depends on a measurement nobody has taken yet, the step says so.

---

## 0. Summary

| Step | Question | Short answer | Risk | Stage |
|---|---|---|---|---|
| 100a | Is `TryFindElementAfterScroll` still needed? | **Yes, but only on Android, and it is on the wrong interface.** Containers never scroll with it, which is a latent Android bug | Medium | K4 |
| 100b | `ScrollingOnceResolver` | Merges into 100a as a lookup option; not a separate concept | Low | K4 |
| 100c | `IsVisibleAfterScrollCore` | The question is real; the implementation takes the slow route and does not use the bridge | Medium | K4 |
| 101a | `FindChildCore`: compound controls should be containers | **Agreed.** The positional fallback searches the whole scope and guesses | Medium | K5 |
| 101b | `FindChildByControlTypeCore` should be a locator | **Agreed.** Nothing outside `ViewBase` calls it. Its generated public wrapper throws the result away | Low | K1 |
| 102a | Is there a better hierarchy? | **Decided:** decide last, after K2-K5 and a two-way prototype | High | K6 |
| 102b | Interfaces with default implementations? | **Not as the public surface.** C# default members cannot be called through the class type, which breaks `page.Switch.Toggle()` | — | K6 |
| 103 | `PointerLongPress`, `PointerDrag` | Move them out of the driver into one physical-input class. Use the bridge's `LongPress`/`Swipe*` verbs when the app declares them | Low | K3 |
| 104 | Move window handling out of the driver? | **Yes.** About 800 of the driver's 1,905 lines are window work: attach, foreground, placement, watchdog | Low | K1 |
| 105a | Split `Selected` into selected and checked? | **Yes.** Both layers mix them up today, and the toggle state is read twice | Medium | K2 |
| 105b | Is `Swipe` still needed? | Not on Windows under a quiet run. Android and `CarouselView`/`SwipeView` still use it | Medium | K3 |
| 105c | Are the scrolling methods still needed? | Keep three routes: bridge verbs, the UIA Scroll pattern as the no-bridge route, and nothing else. `ScrollIntoView`'s percent loop and the `Try` naming go | Medium | K3 |
| 106 | Do we still need `PhysicalInput`? | **Yes.** It is what enforces the quiet run. It should get smaller after 103/105 | Low | K3 |
| 107 | Should Appium and FlaUI implement every `*PatternElement`? | **Decided:** controls always talk to `IMauiElement` through semantic members; no UIA vocabulary on Appium | High | K2 |
| 108 | `TrySetStateByPattern` belongs in the controls | **Decided:** move it into the controls that have a set-state command, and remove the try-then-fall-back | Low | K2 |

### Stages, in execution order

| Stage | Steps | Why this order |
|---|---|---|
| **K1 — mechanical** | 104, 101b | No behaviour change. It shrinks the files the later stages edit |
| **K2 — element semantics** | 105a → 107 → 108 | Settles what an element can be asked. Everything above the element depends on it |
| **K3 — input routes** | 103, 105b, 105c → 106 | Needs K2's `Supports*` members. 106 is decided once the physical call sites are known |
| **K4 — lookup** | 100a, 100b, 100c | Needs 105c's scroll route |
| **K5 — compound controls** | 101a | Needs K4's lookup inside a container |
| **K6 — hierarchy** | 102a, 102b | Last, because K2-K5 decide what the bases still have to carry |

**Standing rules**, carried over from steps 1-49:
- one UI test process at a time, and no builds while it runs;
- rebuild the sample app before a UI run;
- no `Try` actions (step 34): a question and a command;
- run the smallest test tier that can prove the change wrong;
- the full suite runs once per stage.

Android baselines are known to be red before any change: Range 6/16 fail and Picker 0/8 pass. Compare
against those, not against zero.

---

## 100 — Scroll

### 100a. `IMauiElementScope.TryFindElementAfterScroll` — is it still needed?

**What it does.** A plain lookup, then `IMauiDriver.TryFindByScrollingWithin(container, locator)`
if the plain lookup finds nothing.

**What the code shows.**
- `FlaUIMauiDriver.TryFindByScrollingWithin` returns `null` ([FlaUIMauiDriver.cs:1886](../../srcnew/Brinell.Maui.FlaUI/FlaUIMauiDriver.cs#L1886)).
  Windows keeps an off-screen element in the tree with `IsOffscreen=true`, so a plain lookup
  already finds it. **On Windows this method does nothing.**
- `AppiumMauiDriver.TryFindByScrollingWithin` does real work on Android only: it runs a
  `UiScrollable(...).scrollIntoView(...)` sweep. Android leaves views outside the viewport out of
  the tree, so there **it is needed**.
- There are four implementations: `MauiTestContext`, `AppRoot`, `DriverRootScope` and
  `ContainerObjectBase`. **The container version is just `TryFindElement(locator)`** and nothing
  overrides it ([ContainerObjectBase.cs:201](../../srcnew/Brinell.Maui/Containers/ContainerObjectBase.cs#L201)).
  So on Android, a control declared inside a container (`ScrollView`, `Border`, a list item) is
  never scrolled into the tree. Only controls on the page root are. This is a latent Android bug,
  hidden because most scrolling tests declare their controls on the page.

**Recommendation.** Keep the behaviour and remove the interface member.
1. Take `TryFindElementAfterScroll` off `IMauiElementScope`.
2. Give `ViewBase` one private resolver: a plain lookup through the scope, then
   `Driver.TryFindByScrollingWithin(scopeRoot, Locator)`. Here `scopeRoot` is the container's root
   element, or `null` for a page. That fixes containers for free, because the driver already
   accepts a container.
3. Open question for the step: `ViewBase` reaches the driver through its scope. Check that
   `IMauiScope` exposes the context or driver. If it doesn't, add a `ScrollRoot` (element or null)
   to the scope instead of a lookup method.
4. **Agreed (2026-09-13):** done only after an Android run.

**Verify.**
- Windows: tier 2 `Scroll` + `Display` areas. They should behave the same, because the path is a no-op there.
- Android: `ScrollTests` plus one new test, a control inside a `ScrollView` container placed below
  the fold, which should fail before the change and pass after. **This is the only step in the
  plan that needs an Android run to count as done.**

### 100b. `ScrollingOnceResolver`

**What it does.** Gives a polling helper a resolver that sweeps once, then does plain lookups.
It is used only by `WaitExists` and `AssertExists` ([ViewBase.tpl.cs:468](../../srcnew/Brinell.Maui/Controls/Base/ViewBase.tpl.cs#L468)).

**Finding.** The rule is right: sweeping on every poll tick costs orders of magnitude more. But
it is a lookup *policy*, and it lives as a factory method next to the thing it wraps.

**Recommendation.** Fold it into 100a's resolver as an option,
`Resolve(ScrollPolicy.None | Once | Always)`, and have every `AfterScroll`/`Exists` member go
through it. That also fixes 100c's inconsistency, below.

**Verify.** `ViewBase` unit tests in `Brinell.Maui.Tests`, with a fake driver that counts sweeps:
one sweep per `WaitExists(false)`, not one per tick.

### 100c. `IsVisibleAfterScrollCore`

**What it does.** Returns true if the element is visible. Otherwise it resolves the element with
`FindElement()`, calls `ScrollIntoView`, and reads visibility again
([ViewBase.tpl.cs:614](../../srcnew/Brinell.Maui/Controls/Base/ViewBase.tpl.cs#L614)).

**Callers.** Five tests: `ActivityIndicator`, `Image`, `ProgressBar`, `SearchBar` and `ScrollTests`.

**Findings.**
1. **Absence is slow.** For a missing element, `FindElement()` polls for the full `ElementFind`
   timeout before it gives up. `AssertVisibleAfterScroll(false)` pays that on every tick.
2. **It is inconsistent with `Exists`.** The generated `Wait`/`Assert` pass `TryFindElement`,
   while `WaitExists`/`AssertExists` use `ScrollingOnceResolver`. The same question
   ("could the user see it?") gets two lookup policies.
3. **The scroll route predates the bridge.** On Windows, `element.ScrollIntoView()` is the
   ScrollItem pattern plus a percent loop (105c). The bridge's `ScrollTo(automationId)` on the
   enclosing scroller is the route that actually settles (step 21, and the settle wait added in
   stage J).

**Recommendation.** Keep the member. It is platform-neutral and it is the question tests should
ask. Re-implement it on 100a's resolver with `ScrollPolicy.Once`, then "reveal". Revealing asks
the enclosing scroller `SupportsScrollVerbs ? ScrollTo(id)` and otherwise calls
`element.ScrollIntoView()`. Mark the `Core` method so the generator's `Wait`/`Assert` use the same
resolver.

**Verify.** Tier 2 on those five test classes. Time `AssertVisibleAfterScroll(false)` against a
missing element before and after; it should drop from the `ElementFind` timeout to one sweep.

---

## 101 — Child elements

### 101a. `FindChildCore` — compound controls should be containers

**What it does.** Looks for a child by id under the control's element. If that fails, it searches
the *whole scope* for visible matches whose centre lies inside the control's bounds
([ViewBase.tpl.cs:511](../../srcnew/Brinell.Maui/Controls/Base/ViewBase.tpl.cs#L511)).

**Callers.** `RoundButton`, `IconCommandButton` and `EditableField`, all in `Brinell.Maui.Extensions`.

**Agreed, and here is why.**
- A compound control is a scope with named parts. That is exactly what `ContainerObjectBase`
  already models: a cached root, child lookup under that root, and declared child controls
  (`Child<TControl>(id)`, `Button(id)`).
- The positional fallback is a guess of the kind steps 1-49 kept removing. It relies on geometry,
  it can match a sibling that happens to overlap, and it runs on every click.
- `RoundButton.ClickCore` tries two ids and then falls back to itself (`?? element`). That is a
  three-rung ladder, which the "controls know how to click" design removed everywhere else.

**Recommendation.**
1. Measure first. The remark says "some MAUI handlers reparent the native child out of the logical
   subtree". Record, per control and per platform, whether the direct-child lookup succeeds. If it
   always does, the fallback is dead code. If not, the reparenting becomes the container's
   *root locator* (where the part really lives), decided once, not a geometric search per call.
2. Make the three compound controls containers with declared parts. For example, `RoundButton`
   exposes `Button<RoundButton> Native`, and its `Click` is `Native.Click()`. The legacy id
   becomes a second declared part only if step 1 finds an app that still uses it.
3. Delete `FindChildCore`.

**Verify.** `Brinell.Maui.Extensions` unit tests. Any UI tests that use the three controls, found by grep.

### 101b. `FindChildByControlTypeCore` — a separate locator

**What the code shows.**
- **Nothing calls the `Core` method** except its generated wrapper.
- **The generated wrapper throws away its result.** `ViewBase.gen.cs` emits
  `public TScope FindChild(string)` and `public TScope FindChildByControlType(string)`. Both run
  the lookup inside `RunDoWithElement` and discard what they found, so they are public no-ops that
  appear in IntelliSense on every control. The generator treated a query as an action. Nothing
  calls them.

**Recommendation.**
1. Delete `FindChildByControlTypeCore`, and mark `FindChildCore` `[SkipGeneration]` until 101a
   removes it. That removes both public no-ops.
2. If "the part of this template with control type X" is needed again, express it as a locator
   relative to a parent: a `Locator.ByControlType(x)` resolved under a container root. Lookups
   already take a root, so this is a locator, not a method. Only add it when a caller needs it.
3. Generator follow-up: a `Core` method that returns an element and is not `[AbsenceTolerant]`
   should not get a `Do`-shaped wrapper. Add a generator diagnostic so this cannot happen quietly again.

**Verify.** Root solution build, `Brinell.Generator.Tests`, `Brinell.Maui.Tests`.

---

## 102 — Base control classes

### Today

```
ControlObjectBase
└─ ViewBase<TScope>                       Label, Image, shapes, RefreshView, SwipeView, …
   └─ FocusableControlBase                DatePicker, TimePicker, Entry(→Editor, SearchBar)
      ├─ ClickableControlBase             Button, ImageButton
      │  └─ ToggleControlBase             Switch, CheckBox, RadioButton
      ├─ RangeControlBase                 Slider, Stepper
      └─ SelectorControlBase              Picker
ComponentObjectBase / ItemContainerBase
└─ ClickableItemBase<TCollection,TSelf>   MenuItem, ToolbarItem
   └─ SelectableItemBase                  TabItem, ShellTab
RootedScopeBase
└─ ContainerObjectBase<TParent,TSelf>     Border, Grid, ContentView, ScrollView, ContentDialog
   └─ CollectionObjectBase                ListView, ShellTabs, ShellFlyout, TabMenu…
ScrollHelper (static)                     — exists because a container cannot also inherit scrolling
```

### 102a. Is there a better hierarchy?

**Problems visible today.**
1. **A linear chain of capabilities.** A toggle *is a* clickable *is a* focusable. As a result
   `ToggleControlBase` overrides `ClickCore` to call `Toggle`, because it inherited a click it
   should not have, and `RadioButton` overrides it again.
2. **Two parallel families.** Controls (`TScope`) and items (`TCollection, TSelf`) each carry their
   own copies of the `Run*`/`Wait`/`Assert` plumbing.
3. **Containers that are not containers.** `RefreshView` and `SwipeView` are `ViewBase`, while
   `ScrollView` and `Border` are `ContainerObjectBase`. In MAUI, all four host content.
4. **`ScrollHelper` is a workaround for single inheritance** (its own remark says so).

**Decided (2026-09-13): decide last, from evidence.**
1. **Inventory.** For each concrete control, list which base members it actually uses or
   overrides. K2-K5 remove a lot: pattern casts, `FindChildCore`, `TryFindElementAfterScroll`,
   `Try*` scroll helpers. So take the inventory *after* them.
2. **Prototype one capability (Toggle) two ways**, and compare the generated output and the call sites:
   - **(A) Shallow base plus behaviours.** `ViewBase` is the only control base. Capabilities are
     small classes (`ToggleBehaviour`, `RangeBehaviour`, `ScrollBehaviour`) that hold the `Core`
     logic, and the generator wires them onto controls that declare them.
   - **(B) Keep the inheritance chain and fix the bad links.** Toggle stops being clickable, and
     `RefreshView`/`SwipeView` become containers.
3. Choose by: lines removed, generator complexity, and whether `ScrollHelper` can go away.

### 102b. Interfaces with default implementations?

**Not as the public surface, for a concrete C# reason.** A default interface member can only be
called through the interface type. With `Switch<TScope> : IToggleControlObject<TScope>` and a
default `Toggle()` on the interface, `page.TestSwitch.Toggle()` **does not compile** unless every
control re-declares the member. That breaks the fluent API the generator exists to produce.
A default member also cannot reach `protected` state such as `Locator`, `RunDoWithElement` or the
scope.

**Where they do fit, and are already used:** on `IMauiElement` (`SupportsX => false`,
`X() => throw NotSupported`). There the caller always holds the interface type, and the default
means "this platform has no route". 107 extends that use.

So for 102, default members are an *element*-layer tool, not a *control*-layer one. At the control
layer the choice is between options A and B above.

---

## 103 — Pointers: `FlaUIMauiDriver.PointerLongPress`, `PointerDrag`

**What the code shows.**
- Both are `internal` on the driver ([FlaUIMauiDriver.cs:306](../../srcnew/Brinell.Maui.FlaUI/FlaUIMauiDriver.cs#L306), [:327](../../srcnew/Brinell.Maui.FlaUI/FlaUIMauiDriver.cs#L327)).
  Each one records `PhysicalInput.Used`, calls `EnsureRootWindowFocused()` (a foreground grab),
  and moves the real mouse.
- **Only callers:** `FlaUIMauiElement.LongPress` → `PointerLongPress`, and
  `FlaUIMauiElement.Swipe` (the non-vertical path) → `PointerDrag`.
- The bridge already has `LongPress` (102) and `SwipeLeft/Right/Up/Down` (103-106).
  `ClickableControlBase.LongPressCore` still calls `element.LongPress`, and that goes to the mouse.

**Recommendation.**
1. **Route through the bridge first, with one route per call.** `FlaUIMauiElement.LongPress`
   checks `SupportsGesture(LongPress)`: if true it performs the verb, otherwise it uses physical
   input. `PhysicalInput` then refuses that under a quiet run, and the refusal message names the
   missing declaration. `Swipe` works the same way (105b).
2. **Move the pointer primitives off the driver** into one `internal sealed class PhysicalPointer`
   (FlaUI) that owns `EnsureRootWindowFocused`, `PhysicalInput.Used` and the mouse calls.
   `Click`/`DoubleClick`/`RightClick`/`Hover` move there too. The driver keeps no `Mouse.` calls,
   and a grep for `Mouse.` becomes the physical-input inventory.
3. AD-008's second admission test ("is the physical path the thing under test?") stays the reason
   the primitives exist. They are reached only from `PhysicalInputFact` tests.

**Verify.**
- `QuietDefaultTests` and `ForegroundWatchdogTests`.
- Tier 2b (`Stage=Background`).
- `grep -rn "Mouse\." srcnew/Brinell.Maui.FlaUI` should find only `PhysicalPointer.cs`.

---

## 104 — `FlaUIMauiDriver`: move window handling into its own class?

**Yes.** The driver has 1,905 lines. Its `#region Internal` (lines 179-867) is almost entirely
window work, and the watchdog adds another ~120 lines (1017-1134):

| Concern | Members today | Proposed home |
|---|---|---|
| Attaching and staying attached | `AttachToWindow`, `RootElement` + `_rootGate`, `IsStillAvailable`, `RootReattachments` | `AppWindow` — "the handle is the identity; the element is a cache" (freeze RCA) |
| Staying in the background | `StartForegroundWatchdog`, `WatchTheForeground`, `RefuseActivation`, `RestoreForegroundWindow`, `SendAppBehind`, `ForegroundGrabs`, `EnsureRootWindowFocused` | `QuietWindow` |
| Placement | `ReadRequestedPlacement`, `TryApplyRequestedWindowPlacement`, `ComputeRequestedBounds`, `ComputeOffScreenBounds`, work areas, monitor enumeration, `WriteAutPlacementReport` | `WindowPlacement` |
| P/Invoke | `GetForegroundWindow`, `SetWindowPos`, `GetWindowLongPtr`, `EnumDisplayMonitors`, … scattered | `NativeMethods` |
| Capture | already separate | `WindowCapture` (exists) |

What stays on the driver: launch, element finding, navigation, dialogs and the bridge verbs. That
is what `IMauiDriver` is.

**Not in scope:** sharing this with the WPF and WinForms drivers (340 and 297 lines, and none of
the placement or watchdog code). Their quiet default stays Allowed. Sharing is a separate decision
if those stacks ever go quiet.

**Verify.**
- A pure move, so the root solution builds.
- `ForegroundWatchdogTests`, `QuietDefaultTests`, `AppRootScopeTests`.
- The opt-in stress walk (`BRINELL_STRESS=1`), because `RootElement` moves.
- Public members of `FlaUIMauiDriver` that tests read (`ForegroundGrabs`, `RootReattachments`)
  forward to the new classes.

---

## 105 — `FlaUIMauiElement`

### 105a. `Selected` — split into selected and checked?

**Yes. The two are mixed up at both layers.**
- `FlaUIMauiElement.Selected` reads SelectionItem, and *falls back to Toggle*
  ([FlaUIMauiElement.cs:65](../../srcnew/Brinell.Maui.FlaUI/FlaUIMauiElement.cs#L65)). A checked
  CheckBox reports `Selected == true`.
- `ToggleControlBase.IsCheckedCore` reads Toggle, then falls back to `element.Selected`, which
  reads Toggle again, and that is how RadioButton works.
- `SelectableItemBase.IsMarkedSelected` reads `element.Selected` (which already includes Toggle),
  then Toggle again.

So a CheckBox can be "selected", a tab can be "checked", and nobody can tell which one a platform
actually reported.

**Recommendation.**
1. `IElement.Selected` means *chosen, one of a group*: SelectionItem on Windows, `selected` on
   Android. Remove the Toggle fallback.
2. Add `bool? Checked` to `IMauiElement` (not `IElement` yet, to keep WPF/WinForms/Html out of
   this step): Toggle on Windows, `checked` on Android. `null` means the element has no checked state.
3. The controls then say which one they mean:
   - `ToggleControlBase.IsCheckedCore` → `element.Checked`;
   - `RadioButton` overrides → `element.Selected`, or `Checked` on Android. Measure which one
     MAUI's RadioButton publishes before choosing;
   - `SelectableItemBase` → `Selected`, and asks `Checked` only for the Android radio-style tab
     bar its remark describes.
4. This takes the read side of `ITogglePatternElement` out of the controls (107).

**Verify.**
- `Toggle`, `Selection` and `Navigation` (tabs) tier 2 on Windows.
- `ProductCollectionTests` (5 uses of `.Selected`).
- `Brinell.Maui.Tests` `CapabilityNegotiationTests`/`TabMenuTests`.
- The Android toggle and tab tests, compared against their baseline.

### 105b. `Swipe` — still needed?

**What it does on Windows.** A mostly-vertical swipe becomes 5 **mouse-wheel** clicks at the
element's centre. Anything else becomes a **mouse drag**. Both are physical input
([FlaUIMauiElement.cs:655](../../srcnew/Brinell.Maui.FlaUI/FlaUIMauiElement.cs#L655)).

**Callers.**
- `MauiElementGestureExtensions` (`SwipeLeft/Right/Up/Down/Relative`) ← `CarouselView`,
  `SwipeView`, and `AppiumMauiElement.PerformGesture`;
- `ScrollHelper.TrySwipeForward/Back` ← `ScrollView`, `CollectionObjectBase`, as the fallback after
  the Scroll pattern.

**Recommendation.**
- **Windows: no longer needed as a route.**
  - Scrolling goes through `ScrollTo`/`ScrollToIndex` or the Scroll pattern (105c).
  - Carousel and SwipeView gestures go through the bridge's `Swipe*` verbs via `PerformGesture`.
  - `FlaUIMauiElement.Swipe` becomes "if the bridge declares the matching swipe verb, perform it;
    otherwise use physical input through `PhysicalPointer`". It is refused under a quiet run, with
    a message naming the declaration. The mouse-wheel special case is deleted.
- **Android: still needed.** A swipe is how Android scrolls and gestures, so `IElement.Swipe` stays.
- **`ScrollHelper`'s swipe fallback** goes away on Windows along with 105c. On Android it stays the
  route. It should be a platform decision the element makes (`SupportsScrollContent`), not a
  "try the pattern, then swipe" ladder in shared code.
- `CarouselView`/`SwipeView` call `PerformGesture(MauiGesture.SwipeLeft)` instead of the extension.
  Appium already maps `PerformGesture` to the extension, so Android does not change.

**Verify.**
- `Collection` (Carousel) and `Container` (SwipeView) tier 2 with nothing set. They should pass
  with `PhysicalInput` counting zero.
- `grep -rn "SwipeLeft()\|SwipeRight()" srcnew/Brinell.Maui/Controls` should find nothing.

### 105c. Scrolling — are the scrolling methods still needed?

**Five scrolling members on the element today:**

| Member | Route | Callers | Verdict |
|---|---|---|---|
| `ScrollTo(id)`, `ScrollToIndex(i)`, `ReadScrollPosition()` | bridge verbs | `ScrollView`, `CollectionView`, scroll tests | **Keep** — the primary route |
| `TryScrollContent(v, h)` | UIA Scroll pattern on self or an ancestor, waits for the percent to move | `ScrollHelper`, `ProductCollectionTests`, `ScrollPatternProbeTests` | **Keep the route, rename** to `SupportsScrollContent` + `ScrollContent(v, h)` (step 34: no `Try`). It is the no-bridge route and is not physical input |
| `ScrollIntoView()` | ScrollItem pattern, then a geometry step, then a scroll-to-top-and-walk percent loop with `Thread.Sleep(50)` | `ViewBase.EnsureVisible`, `IsVisibleAfterScrollCore`, `CollectionObjectBase`, `ScrollHelper` | **Keep the ScrollItem call; delete the loop.** The loop is the "scroll a bit, poll a percentage, guess" pattern that `ScrollTo` replaced. A caller with a scroller that declares `ScrollTo` uses that; otherwise ScrollItem, and if that does not reveal the element, the result is honestly "not revealed" |

**Also:** `ScrollPatternProbeTests` was a probe from before the bridge. Delete it once
`ScrollContent` has a real test, or keep one assertion from it as that test.

**Verify.** `Scroll`, `Collection` and `Display` tier 2, and `NavigationStressTests` (scroll
settle). Compare the timing report against `timing-baseline.json`; deleting the percent loop
should only ever make things faster.

---

## 106 — `PhysicalInput`: do we still need it?

**Yes.** It is what makes "quiet" a guarantee rather than a hope: a physical-input call under a
quiet run throws instead of taking the desktop. `QuietDefaultTests` asserts exactly that. The WPF
and WinForms drivers also record through it.

**What should change, after 103 and 105:**
1. **Fewer call sites.** Once `PhysicalPointer` exists, the MAUI FlaUI stack should record in two
   places: `PhysicalPointer` and keyboard typing in `SendKeys`. Update
   `.my/extension/physical-input-inventory.md` to match.
2. **Check every knob for a caller.** Policy values `Allowed`/`Audited`/`Refused`, the
   `BRINELL_PHYSICAL_INPUT_LOG` log, the per-site counters, `QuietByDefault()`. Remove any that no
   test and no document uses.
3. **Keep the second argument** ("the verb that replaces this"). It is what turns a refusal into
   an instruction.

**Verify.** `Brinell.Core.Tests` `PhysicalInputTests` (28 uses), and the full MAUI suite with nothing set.

---

## 107 — `*PatternElement`: should Appium and FlaUI implement them, so controls always use them?

**What the code shows.**
- Eight interfaces live in `Brinell.Core`: Invoke, Toggle, SelectionItem, Value, Range,
  ExpandCollapse, Focus, LegacyIAccessible. WPF and WinForms each have their *own copy* of
  `IExpandCollapsePatternElement`.
- `FlaUIMauiElement` implements all eight. `AppiumMauiElement` implements three: Toggle,
  SelectionItem, Value.
- MAUI controls test for them with `element is IXPatternElement { SupportsX: true }` in about 25
  places: `RangeControlBase` ×5, `SelectorControlBase` ×5, `Picker` ×4, `DatePicker` ×3,
  `TimePicker` ×4, `ToggleControlBase` ×2, `Entry`, `Slider`, `ProgressBar`,
  `SelectableItemBase`, `FocusableControlBase`, and in Extensions `EditableField` and
  `GenericBrowser`.

**Why not "every element implements every pattern interface".**
- It would give Appium `Expand()`, `InvokePattern()` and `DoDefaultActionPattern()`, which are UIA
  concepts, each hard-wired to `false`. Controls would still branch on `Supports*`. The cast goes
  away and the UIA vocabulary spreads to every platform.
- `IMauiElement` has spent steps 20-34 moving the other way, to semantic members named for what
  the control does (`Invoke`, `Toggle`, `Select`, `SetDate`, `SelectIndex`, `ScrollTo`), with the
  platform choosing the route.

**Decided (2026-09-13): the goal is the same ("controls always ask `IMauiElement`"), reached through meaning.**
1. **Classify each of the ~25 sites** into one of three kinds:
   - **Already covered by a semantic member**, e.g. `IInvokePatternElement.InvokePattern()` →
     `Invoke()`, `SelectItemPattern()` → `Select()`. Switch the site over.
   - **A state read or write that has no semantic member yet.** Add one to `IMauiElement` with a
     `Supports*`/default-throw pair: `Checked` (105a), `SetChecked(bool)` (108),
     `RangeValue`/`SetRangeValue` + bounds, `Value`/`IsReadOnly`, `IsExpanded`/`Expand`/`Collapse`.
     FlaUI implements it from the pattern; Appium implements it from UiAutomator attributes where
     Android has them.
   - **Genuinely Windows-only** (LegacyIAccessible's `DoDefaultAction`, walking the calendar
     flyout). Keep the interface, but only in `Brinell.Maui.FlaUI`-aware code, or delete it if
     bridge verbs have replaced the route (the DatePicker/TimePicker flyout walk vs
     `SetDate`/`SetTime`).
2. **Then remove the pattern interfaces from `Brinell.Core`**, or move them to a Windows-only
   assembly. Delete the duplicate `IExpandCollapsePatternElement`s in the same pass, if WPF and
   WinForms can share one.
3. Work in batches, one control family at a time, starting with Toggle because 105a and 108 need it.

**Verify.** One family per batch, each with its own tier 2 area. The Appium unit tests
(`Brinell.Maui.Tests` `Semantic/*`) prove the semantic members exist on both elements.
`grep -rn "PatternElement" srcnew/Brinell.Maui/Controls` should shrink batch by batch, ending at zero.

---

## 108 — `TrySetStateByPattern`: should be in the controls

**It already is.** It is a `private` method on `ToggleControlBase`
([ToggleControlBase.tpl.cs:134](../../srcnew/Brinell.Maui/Controls/Base/ToggleControlBase.tpl.cs#L134)),
not on the element. So the request probably means one of these:
- **(a)** it should not be a `Try` that casts to a UIA pattern and falls through to `ToggleCore`
  on any `false`, which is a two-rung ladder; or
- **(b)** it should move down from the shared base to the controls that actually have a
  set-state command (`Switch`, `CheckBox`), and not apply to `RadioButton`.

**Decided (2026-09-13): into the controls, and the try-then-fall-back goes.**

- After 105a/107: `IMauiElement` gains `bool SupportsSetChecked => false` and
  `void SetChecked(bool)`. FlaUI implements them with `SetToggleState`; Appium leaves the default,
  because Android has no set-state command.
- **`ToggleControlBase` loses set-state entirely.** Its `SetCheckedCore` only reads, compares and
  calls `ToggleCore`, which already verifies. It no longer knows a set-state command exists.
- **`Switch` and `CheckBox` override `SetCheckedCore`** with one decision, asked once:
  - if `element.SupportsSetChecked`, call `SetChecked`, then verify the state;
  - otherwise call `ToggleCore`.

  These are two routes chosen up front, not a chain. A `SetChecked` the platform accepts without
  changing anything **throws**, as `ToggleCore` does; it never falls through to a toggle.
- `RadioButton` overrides `SetCheckedCore`: `true` means `Select()`, and `false` is not supported.
  You uncheck a radio button by choosing another one, and saying so is more useful than a toggle
  that does nothing.
- `TrySetStateByPattern` is deleted.

**Verify.** `Toggle` tier 2 (Switch, CheckBox, RadioButton), with a test for a `SetChecked` the app
accepts but does not apply. The TestHost can simulate that the way `HostTargets` returns chosen HRESULTs.

---

## Decisions (answered 2026-09-13)

1. **108** — into the controls (`Switch`, `CheckBox`, `RadioButton`), and the try-then-fall-back is
   removed. See step 108.
2. **107** — yes: semantic members on `IMauiElement`, not pattern interfaces on every platform.
3. **102** — yes: the hierarchy is decided last, after the K2-K5 inventory and the Toggle prototype.
4. **100a** — ok: step 100a needs an Android emulator run before it counts as done.

Nothing is left blocking. K1 (104, 101b) can start.

---

## Results (2026-09-13)

Baseline for every UI run: `OccludedScreenshotTests.Screenshot_OfOccludedWindow_ShowsTheApp` fails
with "the occluding window did not cover the screen" - **on a clean worktree at HEAD too**, alone
and in the suite. It is the desktop, not this work, and is excluded from the counts below.

### K1 — mechanical (104, 101b) — done

- `FlaUIMauiDriver` 1,905 → 959 lines. New `Windowing/`: `AppWindow` (attach, self-healing root),
  `QuietWindow` (watchdog, `WS_EX_NOACTIVATE`, send behind), `WindowPlacement`, `NativeMethods`,
  and `PhysicalPointer` (all mouse input, and bringing the app to the front). The dead
  `TryInvokeBackButton` and its geometry helpers were deleted.
- `FindChildByControlTypeCore` deleted. **Generator fix:** `ActionGenerator` no longer claims a
  `*Core` method that returns a value, with a test - that is what had produced the public no-op
  `FindChild`/`FindChildByControlType` on every control.
- Verified: solution build; unit tests; `Stage=Background` + watchdog + quiet default + root scope
  62/64 (2 skipped); stress walk 1/1.

### K2 — element semantics (105a, 107, 108) — done

- `IMauiElement` gained `SupportsInvoke/Toggle/Select`, `SupportsFocus`/`Focus`, `Checked`,
  `SupportsSetChecked`/`SetChecked`, `Value`, `IsReadOnly`, `RangeValue/Minimum/Maximum/SmallChange`,
  `SupportsSetRangeValue`/`SetRangeValue`, and a dropdown group (`SupportsDropdown`,
  `IsDropdownOpen`, `OpenDropdown`, `CloseDropdown`, `ReadDropdownItems`, `SelectedItemText`).
- `FlaUIMauiElement` and `AppiumMauiElement` implement no pattern interface. Seven Core pattern
  interfaces deleted; `IRangePatternElement` stays for WPF/WinForms, which also keep their own
  `IExpandCollapsePatternElement`.
- `Selected` on Windows is SelectionItem only; `Checked` is the toggle state.
- 108 as decided: `ToggleControlBase` only toggles; `Switch` and `CheckBox` choose set-state or
  toggle up front; a set-state that does nothing throws; `RadioButton` refuses to be unchecked.
- **Removed capability, per the plan's 107 kind 3:** the WinUI calendar and clock flyout walks in
  `DatePicker`/`TimePicker`. Without the `SetDate`/`SetTime` verb they now throw naming it.
- Extensions activation ladders: SelectionItem → Invoke → LegacyIAccessible → click became one
  question per element; the LegacyIAccessible rung is gone.
- Verified: unit tests (mocks rewritten to the semantic members, plus a test for a set-state that
  does nothing); full UI suite 280 passed, 16 skipped.

### K3 — input routes (103, 105b, 105c, 106) — done

- `LongPress` and `Swipe` take the gesture verb where declared, the pointer otherwise. The
  mouse-wheel swipe is gone. `CarouselView` uses `PerformGesture`.
- `IElement.TryScrollContent` → `IMauiElement.SupportsScrollContent` + `ScrollContent` (false =
  did not move). `ScrollHelper` is one route per element. `ScrollIntoView` on Windows is ScrollItem,
  then the nearest ancestor's `ScrollTo` verb; the percent-guessing loop is deleted.
  `ScrollPatternProbeTests` (a probe that never failed) deleted.
- 106: `PhysicalInput` unchanged - every knob has users. Keyboard routes record once, not twice.
  `Mouse.` appears only in `PhysicalPointer.cs`. Inventory document updated.
- Verified: full UI suite 279 passed, 16 skipped (one test fewer). The quiet default refuses
  physical input, so a leftover physical route would have failed a test.

### K4 — lookup (100a, 100b, 100c) — done

- `IMauiElementScope.TryFindElementAfterScroll` → `ScrollingRoot` (null = driver picks). A
  container passes its parent's; `ScrollView` answers with its own root.
- `ViewBase.TryFindElement(ScrollLookup)` and `Resolver(ScrollLookup)` replace the scope method and
  `ScrollingOnceResolver`. `FindElement()` - the route every action takes - sweeps once too.
- `IsVisibleAfterScrollCore` no longer resolves with `FindElement` (a full find timeout per tick);
  its trio is hand-written on `ScrollLookup.Once`.
- Verified: new unit tests (`ScrollLookupTests`, 5); Windows Scroll/Display/Text/Collection/
  Background 128/130; new UI test `ScrollView_FindsAChildBelowTheFold_WithoutBeingScrolledToIt`.
- **Android, as agreed:** the new test passes on the `Brinell_Perf` emulator. With the sweep
  disabled the class fails at a button below the fold ("not found within container") - the same
  defect. Two Android problems found on the way, neither caused by this work:
  - `MauiFixture`/`ShellFixture` asked for `CurrentWindowHandle`, which UiAutomator2 does not
    implement, so **every Android test has failed in its fixture since stage B**. Fixed:
    `ParallelismProbe.Enter(side, driver)`.
  - A stale incremental Android build crashed the app at launch (`No view found for id
    jumpToStart`); a clean build fixed it.
  - Still broken on Android and not touched: the fixture's return to the hub ("already at the
    navigation root"), so a whole Android class does not run past its first test.

### K5 — compound controls (101a) — done

- `RoundButton`, `IconCommandButton` and `EditableField` are containers with named parts, looked
  for under their own root. `FindChildCore` and its whole-page positional fallback are deleted.
  The buttons ask whether the native part exists, then click one part; the template root is never
  invoked.
- **Not measured:** whether any app's handler reparents these parts out of the root. No app in
  this repository uses the templates. A part that is reparented now fails with "not found within
  container" instead of being found by position.
- Verified: unit tests (EditableField 6/6 unchanged); full UI suite 280 passed, 16 skipped.

### K6 — hierarchy (102) — done, option B

Chosen by the user after the inventory in [k6-hierarchy-decision.md](k6-hierarchy-decision.md).
Toggles derive from `FocusableControlBase` and keep `Click`, but no longer have the pointer API;
`RefreshView` and `SwipeView` are containers. Full UI suite 281 passed, 16 skipped, 0 failed.
Details, and the Android-workload problem that appeared on this machine during the stage, in § 7
of that document.

