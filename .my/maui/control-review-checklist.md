# MAUI Control Review Checklist

**Date:** 2026-08-30
**Purpose:** Go through every MAUI control object and tick it off once verified correct.
**Related:** [maui-control-architecture-plan.md](maui-control-architecture-plan.md)

---

## How to use this

Each control is a task with its methods as sub-tasks. Tick a **method** when its behaviour is
confirmed correct; tick the **control** only when every method under it is ticked and the
control-level checks below pass.

**Per control, check:**

- [ ] **Generated** — a `.tpl.cs` exists and the `.gen.cs` is current (`tools/Scripts/CreateMaui.Bat`)
- [ ] **Base is right** — see the Base column; a display-only control should not inherit clickability
- [ ] **No platform branching** — `grep` for `MauiPlatform`, `OperatingSystem.Is`, `#if` in the control
- [ ] **Hand-written members justified** — each one has a comment saying why it cannot be generated
- [ ] **UI test coverage** — a `[Trait("Control", "<Name>")]` class exists

**Per method, check:**

- [ ] Does what its name says on Windows
- [ ] Degrades sensibly where a capability is missing (falls through, does not silently no-op)
- [ ] Returns `null` for "unknown" rather than a default that reads as a real answer
- [ ] Failure message names the control and the locator

**Legend:** ✅ generated · ✋ hand-written (reason required) · ⚠️ known failing · 🔍 no UI test class

---

## Summary

| | Count |
|---|---|
| Controls (excluding base classes) | **60** |
| Base classes | 6 |
| Generated (`.tpl.cs` + `.gen.cs`) | **53 of 60** |
| Not yet converted | **7** — `ContentDialog` (deliberate) + 6 in `Brinell.Maui.Extensions` |
| With a UI test class | **26** |
| Without a UI test class | **34** |

Counted from the tree, not from memory: 59 `.gen.cs` files exist, of which 6 are the base
classes, giving 53 generated controls.

---

## Base classes

Not controls, but every control inherits from one — verify these first, since a defect here
appears in every control below.

### [ ] ViewBase — the root
`base: ControlObjectBase`

- [ ] `IsVisible` / `WaitVisible` / `AssertVisible`
- [ ] `IsEnabled` / `WaitEnabled` / `AssertEnabled`
- [ ] `GetAttribute` / `WaitAttribute` / `AssertAttribute`
- [ ] `ScrollIntoView`
- [ ] `FindChild` / `FindChildByControlType` — compound-control child resolution
- [ ] ✋ `IsExists` / `WaitExists` / `AssertExists` — absence-tolerant, hand-written

### [ ] FocusableControlBase
`base: ViewBase`

- [ ] `Focus` / `Blur`
- [ ] `IsFocused` / `WaitFocused` / `AssertFocused`

### [ ] ClickableControlBase
`base: FocusableControlBase`

- [ ] `Click` — including the activation ladder (SelectionItem → Invoke → pointer)
- [ ] `DoubleClick` / `RightClick` / `Hover` / `LongPress` / `Press`
- [ ] `IsClickable` / `WaitClickable` / `AssertClickable`
- [ ] `IsPressed` / `WaitPressed` / `AssertPressed`

### [ ] ToggleControlBase
`base: ClickableControlBase`

- [ ] `Toggle` — pattern → activation → keyboard ladder
- [ ] `SetChecked` — prefers the platform set-state command
- [ ] `IsChecked` / `WaitChecked` / `AssertChecked`
- [ ] ✋ `Check` / `Uncheck` / `AssertChecked(message)`

### [ ] RangeControlBase
`base: FocusableControlBase`

- [ ] `GetValue` / `SetValue` / `WaitValue` / `AssertValue`
- [ ] `GetMinimum` / `GetMaximum` / `GetStep` + their Wait/Assert
- [ ] `Increment` / `Decrement`
- [ ] ✋ `WaitValueWithin` / `AssertValueWithin` — tolerance comparison

### [ ] SelectorControlBase
`base: FocusableControlBase`

- [ ] `SelectByText` / `SelectByIndex` / `SelectByValue`
- [ ] `GetSelectedText` / `GetSelectedIndex` + Wait/Assert
- [ ] `GetItemCount` / `WaitItemCount` / `AssertItemCount`
- [ ] `GetItemTexts` / `WaitItemTexts` / `AssertItemTexts` — sequence equality
- [ ] `AssertItemTextsHasItem` / `AssertItemTextsCount`

---

## Buttons

### [ ] Button ✅ 🔍-no, tested
`base: ClickableControlBase` · adds nothing of its own — all behaviour inherited

- [ ] Inherits the full clickable surface correctly

### [ ] ImageButton ✅
`base: ClickableControlBase`

- [ ] `GetSource` / `WaitSource` / `AssertSource`
- [ ] `GetAspect` / `WaitAspect` / `AssertAspect`

---

## Text

### [ ] Entry ✅
`base: FocusableControlBase`

- [ ] `SetText` / `GetText` / `Clear`
- [ ] `Enter` / `Append` / `Submit`
- [ ] `WaitText` / `AssertText` + `Contains` / `Empty` / `StartsWith` / `EndsWith`
- [ ] `GetPlaceholder` / `WaitPlaceholder` / `AssertPlaceholder`
- [ ] `IsReadOnly` / `WaitReadOnly` / `AssertReadOnly`
- [ ] ✋ `WaitTextEquals`

### [ ] Editor ✅
`base: Entry` — inherits everything; verify the nested-text path

- [ ] Text get/set via the nested value pattern

### [ ] SearchBar ✅
`base: Entry`

- [ ] `SetSearch` / `SubmitSearch`
- [ ] ✋ `Search`

---

## Toggle

### [ ] CheckBox ✅
`base: ToggleControlBase`

- [ ] ✋ `CheckOn` / `CheckOff`

### [ ] RadioButton ✅
`base: ToggleControlBase`

- [ ] `IsSelected` / `WaitSelected` / `AssertSelected`
- [ ] ✋ `Select` / `AssertSelected` / `AssertNotSelected`

### [ ] Switch ✅ ⚠️
`base: ToggleControlBase` · **`Switch_ClickTwice_TogglesOff` fails — phase 7**

- [ ] `IsOn` / `SetOn` / `WaitOn` / `AssertOn`
- [ ] ✋ `TurnOn` / `TurnOff` / `AssertOn` / `AssertOff`
- [ ] ⚠️ Two consecutive clicks toggle off

---

## Range

### [ ] Slider ✅
`base: RangeControlBase`

- [ ] `GetPercentage` / `WaitPercentage` / `AssertPercentage`
- [ ] ✋ `SlideToMinimum` / `SlideToMaximum` / `SlideToPercentage`

### [ ] Stepper ✅ ⚠️
`base: RangeControlBase` · **11 tests fail — `TestStepper` absent from the UIA tree (phase 7)**

- [ ] ⚠️ Addressable at all on Windows
- [ ] ✋ `IncrementBy` / `DecrementBy` / `SetToMinimum` / `SetToMaximum`
- [ ] ✋ `CanIncrement` / `CanDecrement`

---

## Selection

### [ ] Picker ✅
`base: SelectorControlBase`

- [ ] `GetTitle` / `WaitTitle` / `AssertTitle`
- [ ] Inherited selector surface against a real ComboBox

---

## DateTimes

### [ ] DatePicker ✅ ⚠️
`base: ViewBase` · **6 tests fail — phase 7**

- [ ] `SetDate` / `WaitDateValue` / `AssertDateValue`
- [ ] `WaitMinimumDate` / `AssertMinimumDate` / `WaitMaximumDate` / `AssertMaximumDate`
- [ ] ✋ `WaitDate` / `AssertDate`

### [ ] TimePicker ✅ ⚠️
`base: ViewBase` · **7 tests fail — phase 7**

- [ ] `SetTime` / `GetTimeValue` / `WaitTimeValue` / `AssertTimeValue`
- [ ] ✋ `GetTime` / `GetHours` / `GetMinutes` / `WaitTime` / `AssertTime`

---

## Display

### [ ] Label ✅
`base: ViewBase`

- [ ] `GetText` + `WaitText` / `AssertText`
- [ ] `Contains` / `Empty` / `StartsWith` / `EndsWith` variants

### [ ] Image ✅ ⚠️
`base: ViewBase` · **`Image_IsVisible_ReturnsTrue` fails — phase 7**

- [ ] `GetSource` / `WaitSource` / `AssertSource`
- [ ] `IsLoaded` / `WaitLoaded` / `AssertLoaded`
- [ ] `GetWidth` / `GetHeight` + Wait/Assert
- [ ] ✋ `AssertLoaded(message)`

### [ ] ActivityIndicator ✅
`base: ViewBase`

- [ ] `IsRunning` / `WaitRunning` / `AssertRunning`
- [ ] ✋ `AssertRunning(message)`

### [ ] ProgressBar ✅ ⚠️
`base: ViewBase` · **2 tests fail — phase 7**

- [ ] `GetProgress` / `WaitProgress` / `AssertProgress`
- [ ] `IsIndeterminate` / `WaitIndeterminate` / `AssertIndeterminate`
- [ ] ✋ `WaitProgress` / `AssertProgress` / `AssertIndeterminate` overloads

### [ ] TitleBar ✅ 🔍
`base: ViewBase`

- [ ] Text surface (same shape as Label)

---

## Container

### [ ] Grid ✅ 
`base: ContainerObjectBase` — scoping only, no members of its own

- [ ] Scopes its children; does not reach outside itself

### [ ] Border ✅
`base: ContainerObjectBase`

- [ ] Scopes its child

### [ ] ContentView ✅
`base: ContainerObjectBase`

- [ ] Scopes its content

### [ ] ScrollView ✅
`base: ContainerObjectBase`

- [ ] `ScrollForward` / `ScrollBack`
- [ ] ✋ `ScrollTo(Locator)` / `ScrollTo(string)`

### [ ] RefreshView ✅
`base: ViewBase` · not addressable on Windows — mobile-only in practice

- [ ] `PullToRefresh`
- [ ] `IsRefreshing` / `WaitRefreshing` / `AssertRefreshing`
- [ ] ✋ `AssertRefreshing(message)`

### [ ] SwipeView ✅ 🔍
`base: ViewBase` · not addressable on Windows — mobile-only in practice

- [ ] `SwipeLeft` / `SwipeRight` / `SwipeUp` / `SwipeDown`
- [ ] `Swipe(startX, startY, endX, endY)`

### [ ] BoxView ✅ 
`base: ViewBase` — visual only

- [ ] Exists / visible

### [ ] Frame ✅ 🔍
`base: ViewBase` · not addressable on Windows

- [ ] Exists / visible

### [ ] IsoPaneView ✅ 🔍
`base: ViewBase`

- [ ] Exists / visible

---

## Collection

### [ ] CollectionView ✅ 🔍
`base: CollectionObjectBase<TParent, TSelf, TItem>` · self-referencing generic

- [ ] `GetSelectionMode` / `WaitSelectionMode` / `AssertSelectionMode`
- [ ] `IsMultiSelectEnabled` / `WaitMultiSelectEnabled` / `AssertMultiSelectEnabled`
- [ ] Item scoping — rows with repeating AutomationIds stay distinct

### [ ] CarouselView ✅ 🔍
`base: CollectionObjectBase<TParent, TSelf, TItem>`

- [ ] `GetPosition` / `WaitPosition` / `AssertPosition`
- [ ] `IsLoopEnabled` / `WaitLoopEnabled` / `AssertLoopEnabled`
- [ ] `SwipeNext` / `SwipePrevious`
- [ ] ✋ `GetCurrentItem`

### [ ] ListView ✅ 🔍
`base: CollectionObjectBase` — no members of its own

- [ ] Item scoping

### [ ] TableView ✅ 🔍
`base: ViewBase`

- [ ] `GetIntent` / `WaitIntent` / `AssertIntent`
- [ ] ✋ `HasIntent` — case-insensitive

### [ ] IndicatorView ✅ 🔍
`base: ViewBase` — no members of its own

- [ ] Exists / visible

---

## Navigation

### [ ] Shell ✅ 🔍
`base: ViewBase` · **no longer used by the sample app** — kept for user apps

- [ ] ✋ `NavigateTo` / `GetTab` / `GetSelectedTab`
- [ ] ✋ `IsTabSelected` / `WaitTabSelected` / `AssertTabSelected`
- [ ] ✋ `IsLoaded` / `WaitLoaded` / `AssertLoaded`

### [ ] ShellContent ✅ 🔍
`base: ClickableControlBase` · **known divergence — see plan §6**

- [ ] `IsSelected` / `WaitSelected` / `AssertSelected`
- [ ] ✋ `NavigateTo` / `ClickAndNavigate` / `AssertIsSelected` / `AssertIsNotSelected`
- [ ] ⚠️ Locator strategy works on Android (`ControlTypeAndName` is Windows-only)

### [ ] Tab ✅ 🔍
`base: ClickableControlBase`

- [ ] `IsSelected` / `WaitSelected` / `AssertSelected`

### [ ] FlyoutItem ✅ 🔍
`base: ClickableControlBase` — no members of its own

- [ ] Click selects the item

### [ ] TabMenu ✅
`base: ViewBase`

- [ ] ✋ `Select` / `TrySelect` — walks button, grid, caption surfaces

### [ ] Menu ✅
`base: ViewBase`

- [ ] `Open` / `ClickMenuItem`
- [ ] ✋ `IsOpen`

### [ ] Toolbar ✅
`base: ViewBase`

- [ ] `GetTitle` / `WaitTitle` / `AssertTitle`
- [ ] `ClickToolbarItem` — scoped to its own toolbar
- [ ] ✋ `GoBack`

---

## Media

### [ ] WebView ✅ 🔍
`base: ViewBase`

- [ ] `GetUrl` / `WaitUrl` / `AssertUrl` / `AssertUrlContains`
- [ ] `GetPageTitle` / `WaitPageTitle` / `AssertPageTitle` / `AssertPageTitleContains`
- [ ] `IsCanGoBack` / `IsCanGoForward` + Wait/Assert
- [ ] ✋ `AssertUrlContainsIgnoreCase`

### [ ] MediaElement ✅ 🔍
`base: ViewBase`

- [ ] `IsPlaying` / `IsPaused` / `IsMuted` + Wait/Assert
- [ ] `GetPlaybackState` / `GetPosition` / `GetDuration` / `GetVolume` + Wait/Assert

### [ ] HybridWebView ✅ 🔍
`base: ViewBase` — no members of its own

- [ ] Exists / visible

### [ ] BlazorWebView ✅ 🔍
`base: ViewBase` — no members of its own

- [ ] Exists / visible

---

## Graphics · Shapes

### [ ] GraphicsView ✅ 🔍
`base: ViewBase` — no members of its own

- [ ] Exists / visible

### [ ] Ellipse ✅ 🔍 · [ ] Line ✅ 🔍 · [ ] Path ✅ 🔍 · [ ] Polygon ✅ 🔍 · [ ] Polyline ✅ 🔍 · [ ] Rectangle ✅ 🔍 · [ ] RoundRectangle ✅ 🔍
`base: ViewBase` — all visual only, no members of their own

- [ ] Each exists / visible
- [ ] Each is addressable on Windows (measure — several MAUI shapes are not)

---

## Dialogs

### [ ] ContentDialog ✋ 🔍
`base: ContainerObjectBase` · **deliberately not generated** — no member takes an element first

- [ ] ✋ `DialogButton(text)` / `PromptInput`
- [ ] ✋ `TryClickButtonAndWaitDismissed` — six fallbacks across scoped, popup and parent scopes
- [ ] Popup-window resolution via `FindPopupElement`

---

## Extensions (`Brinell.Maui.Extensions`) — none converted yet

These are the **second external consumer** (plan phase 5). The Toolkit proved the path;
these have not been converted.

### [ ] Expander ✅
`base: ClickableControlBase` — converted in phase 5

- [ ] `Expand` / `Collapse` / `ToggleExpanded`
- [ ] `IsExpanded` / `WaitExpanded` / `AssertExpanded`
- [ ] ✋ `AssertExpanded(message)`

### [ ] Link ✋ 🔍
- [ ] `GetUrl` / `GetLinkText` / `AssertUrlContains` / `AssertLinkTextEquals`
- [ ] Convert to `.tpl.cs`

### [ ] RoundButton ✋ 🔍
- [ ] `ClickCore` override resolves the native inner button
- [ ] Convert to `.tpl.cs`

### [ ] IconCommandButton ✋ 🔍
- [ ] `ClickCore` override resolves the native inner button
- [ ] Convert to `.tpl.cs`

### [ ] EditableField ✋
- [ ] `Open` / `TryOpen` / `SetText` / `TrySetText` / `GetEntryText`
- [ ] Convert to `.tpl.cs`

### [ ] GenericBrowser ✋
- [ ] `SelectItem` / `TrySelectItem` / `ToggleItem` / `TryToggleItem` / `Close` / `TryClose`
- [ ] Convert to `.tpl.cs`

### [ ] SelectionList ✋
- [ ] `SelectByAutomationId` / `TrySelectByAutomationId` / `TrySelectByText`
- [ ] Convert to `.tpl.cs`

---

## Community Toolkit (`Brinell.Maui.CommunityToolkit`)

### [ ] TabViewControl ✅ 🔍
`base: ClickableControlBase` — converted in phase 5 as the external-consumer proof

- [ ] `IsSelected` / `WaitSelected` / `AssertSelected`
- [ ] `TryFindElement` override — AutomationId → Name → AccessibilityId fallbacks

---

## Controls with no UI test class (34)

Ticking these honestly needs a test first. Listed so the gap is visible rather than implied:

`TitleBar`, `SwipeView`, `Frame`, `IsoPaneView`, `CollectionView`, `CarouselView`, `ListView`,
`TableView`, `IndicatorView`, `Shell`, `ShellContent`, `Tab`, `FlyoutItem`, `Expander`,
`TabViewControl`, `WebView`, `MediaElement`, `HybridWebView`, `BlazorWebView`, `GraphicsView`,
`Ellipse`, `Line`, `Path`, `Polygon`, `Polyline`, `Rectangle`, `RoundRectangle`,
`ContentDialog`, `Link`, `RoundButton`, `IconCommandButton`, `EditableField`,
`GenericBrowser`, `SelectionList`.

The 26 that **do** have one: `ActivityIndicator`, `Border`, `BoxView`, `Button`, `CheckBox`,
`ContentView`, `DatePicker`, `Editor`, `Entry`, `Grid`, `Image`, `ImageButton`, `Label`,
`Menu`, `Picker`, `ProgressBar`, `RadioButton`, `RefreshView`, `ScrollView`, `SearchBar`,
`Slider`, `Stepper`, `Switch`, `TabMenu`, `TimePicker`, `Toolbar`.

Several are untestable on Windows by nature (`SwipeView`, `RefreshView`, `Frame` are not
addressable there) — for those, the tick belongs to the Android run, not a Windows test.
