# The `Supports*` properties: are they needed?

> **Superseded 2026-09-17: they are all gone.** This note asked the question when `IMauiElement`
> had 18 of them. The answer turned out to be no, one member at a time, and the last six were
> removed by [../ControlFlow/plan-remove-the-last-supports-members.md](../ControlFlow/plan-remove-the-last-supports-members.md).
> `IMauiElement` now asks no questions at all: every member performs, reads, or reads nullably.
> The only `Supports*` left in the MAUI stack is `IMauiDriver.SupportsGesture(id, gesture)`, which
> is a test's way of asserting what the app declared rather than a route choice.
>
> The reasoning below is kept because it is what established the rule that decided each removal:
> a question was allowed only where both answers occur on the same backend and both branches work
> there. Read it as history, not as a description of the interface.

**The question.** `IMauiElement` has 18 `Supports*` members, and `IMauiDriver` has 4 more. Does
Appium or FlaUI actually need them, or do they mostly answer true?

**Short answer.** They do **not** mostly answer true.

- **Appium** overrides 4 of the 22:
  - `SupportsInvoke`, `SupportsToggle` and `SupportsSelect` always answer true;
  - `SupportsGesture` answers true for five gestures.

  Every other member keeps the interface default, **false**. The Brinell bridge is Windows-only,
  so none of the verb-backed questions can be true on Android or iOS.
- **FlaUI** answers each one per element. Each answer comes from either a UI Automation pattern
  on that element or a verb the app declared for it.

So the properties are not redundant. What they mostly do today is **choose between the Windows
route and the mobile route inside the control object**.

Since [no-physical-input.md](no-physical-input.md), the mobile route throws on Windows, and that
changes the picture. For about half of the members, `false` on Windows now leads straight to a
`NotSupportedException`. Those members exist only so that Appium can take its fallback. The
fallback can move into `AppiumMauiElement`, and then the property can be deleted.

Measured by reading the code on 2026-09-14 (`srcnew/Brinell.Maui`, `Brinell.Maui.Extensions`,
`Brinell.Maui.FlaUI`, `Brinell.Maui.Appium`). Nothing was run.

---

## 1. What each implementation answers

`BridgeDeclares(v)` means the app declared verb `v` on this element through
`GestureAutomation.Verbs`.

| Member | FlaUI answers | Appium answers |
|---|---|---|
| `SupportsInvoke` | Invoke pattern present | **true** |
| `SupportsToggle` | Toggle pattern present | **true** |
| `SupportsSelect` | SelectionItem pattern present | **true** |
| `SupportsFocus` | `BridgeDeclares(Focus)` | false |
| `SupportsSetChecked` | `= SupportsToggle` | false |
| `SupportsSetRangeValue` | RangeValue pattern present | false |
| `SupportsDropdown` | ExpandCollapse pattern present | false |
| `SupportsGesture(g)` | declared on the bridge | **true** for Tap and the four swipes |
| `SupportsAppendText` | `BridgeDeclares(AppendText)` | false |
| `SupportsClearFocus` | `BridgeDeclares(Unfocus)` | false |
| `SupportsStateReads` | `BridgeDeclares(GetState)` | false |
| `SupportsScrollContent` | Scroll pattern on the element or an ancestor | false |
| `SupportsScrollVerbs` | `BridgeDeclares(ScrollPosition)` | false |
| `SupportsScrollToIndex` | `BridgeDeclares(ScrollToIndex)` | false |
| `SupportsSelectIndex` | `BridgeDeclares(SelectIndex)` | false |
| `SupportsSelectByText` | `BridgeDeclares(SelectByText)` | false |
| `SupportsSetDate` | `BridgeDeclares(SetDate)` | false |
| `SupportsSetTime` | `BridgeDeclares(SetTime)` | false |
| Driver `SupportsGesture(id, g)` | declared on the bridge | false |
| Driver `SupportsStateReads(id)` | `GetState` declared on `id` | false |
| Driver `SupportsToolbarVerb` | any target declares `InvokeToolbarItem` | false |
| Driver `SupportsFlyoutVerbs` | any target declares the flyout verbs | false |

The Appium class comment already says this is intentional (step 107): "a member this platform has
no route for keeps the interface's default".

---

## 2. What a caller does when the answer is false

This is the column that decides whether a property earns its place. "Throws" means the route the
caller falls back to raises `NotSupportedException` or `BrinellException`.

| Member | Caller | False on Windows | False on Android/iOS |
|---|---|---|---|
| `SupportsFocus` | `FocusableControlBase.FocusCore` | `Click()`: Tap verb, else **throws** | `Click()`, a tap |
| `SupportsClearFocus` | `FocusableControlBase.BlurCore` | `SendKeys(Tab)` **throws** | Tab key |
| `SupportsAppendText` | `Entry.AppendCore` | `SendKeys(text)` with `Keys` **throws** | types the text |
| `SupportsSetChecked` | `Switch`, `CheckBox` `SetCheckedCore` | `Toggle()` **throws** when there is no Toggle pattern | `Toggle()`, a tap |
| `SupportsSetRangeValue` | `RangeControlBase`, `Slider` `SetValueCore` | keyboard or `SendKeys` **throws** | keyboard, or clear and type |
| `SupportsToolbarVerb` | `ToolbarButton.ClickCore` | `element.Click()` **throws** | tap |
| `SupportsSetDate` | `DatePicker.SetDateCore` | **throws** | **throws** as well |
| `SupportsSetTime` | `TimePicker.SetTimeCore` | **throws** | **throws** as well |
| `SupportsStateReads` | `Picker`, `Image`, `ProgressBar`, `Stepper`, `CollectionObjectBase` | reads the UIA tree instead, which is a real route | reads the tree, the only route |
| `SupportsSelectIndex` / `SupportsSelectByText` | `SelectorControlBase` | opens the dropdown through ExpandCollapse, which is real and needs no bridge | taps the picker, then the item |
| `SupportsDropdown` | `SelectorControlBase`, `Picker` flyout members | click route **throws**; `OpenFlyout` throws; `IsFlyoutOpen` is false | tap route; `OpenFlyout` throws |
| `SupportsScrollToIndex` | `CollectionObjectBase` | Scroll pattern, then scroll-into-view | swipe |
| `SupportsScrollContent` | `ScrollHelper`, `CollectionObjectBase.ScrollToTop` | swipe **throws** | swipe. It can't report "reached the end", so `ScrollToTop` takes one step instead of looping |
| `SupportsFlyoutVerbs` | `ShellFlyout` | finds the chrome and invokes it, which is a real route | finds the chrome and taps it |
| `SupportsInvoke` / `SupportsSelect` | `Extensions`: `SelectionList`, `GenericBrowser`, `EditableField` | picks Select or Invoke, else `Click()` **throws** | always true, and all three are `Click()` |
| `SupportsScrollVerbs` | only `FlaUIMauiElement` internally, plus `ScrollVerbTests` | n/a | n/a |
| `SupportsToggle` | no control object; only `FlaUIMauiElement` and unit-test mocks | n/a | n/a |
| Driver `SupportsGesture(id, g)` | UI tests only | n/a | n/a |

Rows are grouped by what the false branch does.

---

## 3. What that means, member by member

### A. Fallback-only switches: can move into the element and be deleted (8)

On Windows the false branch always throws. On Android and iOS it performs the mobile route. The
property therefore holds no information a control object needs. It only decides which platform's
code runs, and that decision already belongs to the element, because "the control knows what, the
element knows how" is the stated design.

| Member | Move the mobile route into |
|---|---|
| `SupportsFocus` | `AppiumMauiElement.Focus()` = tap |
| `SupportsClearFocus` | `AppiumMauiElement.ClearFocus()` = Tab key |
| `SupportsAppendText` | `AppiumMauiElement.AppendText()` = `SendKeys(text)` |
| `SupportsSetChecked` | `AppiumMauiElement.SetChecked()` = read `Checked`, tap if different. On FlaUI it is already `Checked != target ? Toggle()` and equals `SupportsToggle` |
| `SupportsSetRangeValue` | `AppiumMauiElement.SetRangeValue()` = today's keyboard route. This needs the keyboard code moved out of `Slider` and `RangeControlBase` |
| `SupportsSetDate` | nothing to move, because both branches throw. The control just calls `element.SetDate()`, and FlaUI's throw names the verb. **Android and iOS have no `SetDate` route at all today.** |
| `SupportsSetTime` | same as `SetDate` |
| Driver `SupportsToolbarVerb` | `IMauiDriver.InvokeToolbarItem`: Appium implements it as find and tap. `ToolbarButton` then always calls the driver |

The control then makes one call, for example `element.Focus()`. On Windows it either works or
throws with the verb to declare; on mobile it taps. The Windows behaviour does not change at all:
today's exception is simply raised one frame later, from the same element.

**Cost.** A few small Appium methods, and moving the typing and keyboard code out of the
cross-platform control bases. The unit-test mocks in `testsnew/Brinell.Maui.Tests/Semantic` that
set these flags need updating. These mocks were not inventoried.

### B. Real route choices: keep (10)

The false branch is a **different working route on the same platform**, so the control object
genuinely has to ask.

| Member | Why it has to stay |
|---|---|
| `SupportsStateReads` (element and driver) | An app without `GetState` still gets the tree read on Windows. Stepper and CollectionView depend on the choice. |
| `SupportsSelectIndex`, `SupportsSelectByText` | Without the verb, Windows still opens the dropdown through ExpandCollapse. |
| `SupportsDropdown` | Chooses between the dropdown and the tap route, and gives `IsFlyoutOpen` an honest false. |
| `SupportsScrollToIndex` | Without the verb, the Scroll pattern and scroll-into-view still work. |
| `SupportsScrollContent` | Separates "can report that it moved" from "swipe and hope". `ScrollToTop` changes its loop on it. |
| Driver `SupportsFlyoutVerbs` | Without the verbs, the chrome route still works on Windows. |
| `SupportsInvoke`, `SupportsSelect` | On Windows the two are different operations: selecting a row versus pressing a button. `Extensions` picks between them for candidates it doesn't know. They are only needed on FlaUI; Appium's constant true is correct. |

### C. Not used by control objects: candidates to narrow (4)

| Member | Used by | Suggestion |
|---|---|---|
| `SupportsToggle` | `FlaUIMauiElement` only, plus mocks | Drop from the interface once A has removed `SupportsSetChecked`. `Toggle()` performs or throws already. |
| `SupportsScrollVerbs` | `FlaUIMauiElement.ScrollTo` internally, plus one UI test | Make it FlaUI-only, or keep it as a diagnostic. No control asks. |
| `SupportsGesture(g)` (element) | `FlaUIMauiElement.Click` internally | Keep. It is the one question both platforms answer meaningfully, and the gesture tests read it. |
| Driver `SupportsGesture(id, g)` | UI tests | Keep for the same reason. It is the by-id form for elements that have no tree node. |

---

## 4. Totals

| Group | Count | Outcome |
|---|---:|---|
| A. Fallback-only, push down into the element | 8 | delete the property |
| B. Real route choice | 10 | keep |
| C. Unused by controls | 4 | 2 narrowed, 2 kept |
| **All** | 22 | 18 on the element, 4 on the driver |

So **8 of the 22 can go**, and 2 more can leave the interface, without losing anything. The rest are genuine questions, and on
Appium almost all of them answer false. They are not true.

---

## 5. Found along the way

- **The `IMauiElement.ScrollToIndex` remark is now wrong.** It says "An index past the end is not an
  error ... Nothing moves and nothing throws." Since [fix-remaining-skips.md](fix-remaining-skips.md)
  C1, FlaUI throws `ArgumentOutOfRangeException` naming the count. The remark should say so.
- **DatePicker and TimePicker cannot be set on Android or iOS.** `SetDateCore` and `SetTimeCore`
  throw unless the Windows-only verb is declared. This is a gap in the Appium driver, not a
  question of properties.
- **Appium `SupportsInvoke`, `SupportsToggle` and `SupportsSelect` answer true for every element**,
  including a label. That is harmless: all three are a tap, and the callers catch a failed
  candidate. It also means the flags cannot tell a tappable element from any other on mobile.

---

## 6. If this is taken forward

One step per group, smallest first. Each step changes no Windows behaviour, so the Windows gate is
the existing tiers.

1. **SetDate and SetTime.** Delete the two properties and call the element directly. Verify with
   `Tests.DateTimes`.
2. **Focus, ClearFocus and AppendText.** Add the three Appium methods, delete the properties and
   update the mocks. Verify with `Stage=Background`, the Maui unit tests, and a build of the
   Android head.
3. **SetChecked and then Toggle.** Verify with `Tests.Toggle`.
4. **SetRangeValue.** This is the largest step, because the keyboard code moves into
   `AppiumMauiElement`. Verify with `Tests.Range`.
5. **ToolbarVerb.** Add Appium `InvokeToolbarItem` as a find-and-tap. Verify with `ToolbarVerbTests`.
6. **Fix the `ScrollToIndex` remark.**

None of the Android steps can be verified on a device from here. Per the memory notes, the Android
Range and Picker tiers were already failing before any change.

---

## 7. Done 2026-09-14: groups A and C

**Removed: 10 members.** The group A members, plus `SupportsToggle` and `SupportsScrollVerbs`
from group C, all in one pass rather than the six steps above. Both `SupportsGesture` members stay.

| Removed | Where the route lives now |
|---|---|
| `SupportsFocus` | `FlaUIMauiElement.Focus`: Focus verb, else the Tap verb when declared, else throw (the old control fallback, kept). `AppiumMauiElement.Focus`: tap. |
| `SupportsClearFocus` | FlaUI: Unfocus verb or throw. Appium: `SendKeys(Tab)`. |
| `SupportsAppendText` | FlaUI: AppendText verb or throw. Appium: `SendKeys(text)`. |
| `SupportsSetChecked` | Both elements: read `Checked`, toggle if different. `Switch` and `CheckBox` always call `SetCheckedDirectly`. |
| `SupportsSetRangeValue` | FlaUI: RangeValue pattern or throw. Appium: the arrow-key routine moved out of `Slider`, with its 0/100/1 assumptions. The base class's clear-and-type fallback is gone; no range control reached it. |
| `SupportsSetDate`, `SupportsSetTime` | The controls call the element. FlaUI's messages no longer mention the removed calendar and clock routes. |
| Driver `SupportsToolbarVerb` | `ToolbarButton` always calls `InvokeToolbarItem`. New `AppiumMauiDriver.InvokeToolbarItem`: find by accessibility id, tap. |
| `SupportsToggle` | Private `HasTogglePattern` in FlaUI. |
| `SupportsScrollVerbs` | Private `DeclaresScrollVerbs` in FlaUI. |

**Tests.**
- **Deleted.** Three UI tests that only asserted a removed flag:
  - `DateTimeVerbTests.BothPickers_OfferTheSemanticRoute`;
  - `ScrollVerbTests.Scroller_OffersTheSemanticRoute`;
  - `ToolbarVerbTests.SupportsToolbarVerb_IsTrue_WhenThePageDeclaresIt`.

  Their premise, that a missing verb silently falls back, stopped being true: a missing verb now
  throws in the real tests.
- **Updated.** The unit-test mocks: `CapabilityNegotiationTests` now verifies `SetChecked(true)` on
  the mobile-shaped element rather than `Toggle()`.
- **Also fixed:** the `IMauiElement.ScrollToIndex` remark from section 5.

**Two things found while verifying, both from the CollectionView work, not from this change.**
- **The mobile test project did not compile.** `CollectionIndexProbeTests` reached into
  `FlaUIMauiElement` by reflection. It now reads `IMauiElement.PositionInSet` / `SizeOfSet`.
- **`ItemWhere_ScrollsToFindOffscreenRow` had a race.**
  - `LogicalIndexOf` read `PositionInSet` twice. That once threw when a row recycled between the
    reads, but it was also slow enough to hide the real problem.
  - With a single read, the walk read rows while the list was still recycling. It marked an index
    as seen beside another item's name, and never came back to it.
  - Fix in `CollectionObjectBase`: after `ScrollToIndex`, wait for progress and then for the
    realized logical indexes to stop changing. Don't mark a row as seen if its position changed
    while the predicate read it.
  - Collection tier: 3/3 runs at 16/16.

**Verified.**
- The solution, the Appium driver and both UI test projects build.
- Maui unit tests: 123 passed, 1 skipped.
- Full Windows suite: **296 tests, 295 passed, 1 skipped (stress), 0 failed**, 2.9 min.
- **Not run:** Android and iOS. The new Appium members are compiled only.
  - `SetChecked` behaves differently on an element whose checked state reads null. It used to
    toggle and accept; it now toggles and then fails verification. On Android that is only a
    non-checkable view.
