# Migrate all MAUI controls to the parts-and-shortcuts format - plan

Goal: bring every control, container and component in `srcnew/Brinell.Maui` and
`srcnew/Brinell.Maui.CommunityToolkit` to the format `MediaElement` now uses (see
[component-operations-plan.md](component-operations-plan.md)), so that **every public call is
one unit of work**: one readiness check, one poll, one log entry, and the real error when it
fails.

Status: done (2026-09-18, see section 8). No backward compatibility: members are renamed or removed
outright and their callers fixed in the same change.

## 1. The format, as rules to check each control against

| # | Rule | Breaks it |
| --- | --- | --- |
| R1 | A Core method reads and acts on **its own element** only. It never calls a public member of another control (a part, `Button(id)`, an item). | Nested unit of work: two readiness checks, two polls, timeouts that add up. |
| R2 | A Core method never calls a `Run*` helper (`RunWait`, `RunWaitWithElement`, ...). Waiting inside a Core method uses one element-level helper (F1) that keeps the last error. | A second poll with its own timeout, logged as if the test called it. |
| R3 | A part with behaviour of its own gets its own control class; a component forwards to it through `*Shortcut` methods. | Behaviour spread over the component, re-reading the part's element by hand. |
| R4 | A member that needs two parts is hand-written as **plain calls in sequence**, with no `Run*` wrapper, and a remark naming the parts. | - |
| R5 | A class is a `.tpl.cs` template if it declares Core methods or shortcuts; otherwise it stays a plain `.cs` (an override-only class like `ToolbarButton` has nothing to generate). | Hand-written trios that the generator could write. |
| R6 | Public members take ids and values, not `Locator`s: tests must not build locators. | `IsShowing(Locator)` makes the test build one. |

Hand-written members that call the control's **own** Core methods under a single `Run*` call
are fine (one unit of work): the tolerance and whole-day comparisons in `RangeControlBase`,
`ProgressBar`, `DatePicker`, `TimePicker`.

## 2. Survey (2026-09-18)

Searched both projects for part properties, calls to public control members inside control
code, `Run*`/`WaitHelper`/`Poll` inside controls, and raw sub-element lookups. The result is
short: most controls already conform, because they read their own element.

### Needs changes

| Control | File | Breaks | Change |
| --- | --- | --- | --- |
| `ToggleControlBase` | `Controls/Base/ToggleControlBase.tpl.cs` | R2: `ToggleCore` (via `WaitForStateChange`) and `SetCheckedDirectly` call `RunWaitWithElement` inside a Core method. Affects `Switch`, `CheckBox`, `RadioButton`. | Wait with F1 on `IsCheckedCore(element)`. |
| `AvatarView` | `CommunityToolkit/Controls/Views/AvatarView.tpl.cs` | R1: `GetTextCore` calls `Text.IsVisible()` and `Text.GetText()`; `IsShowingImageCore` calls `Image.IsVisible()`. Each generated `AssertText` tick runs two nested units. | The part is renamed for what it shows: `Initials`, of the new type `AvatarInitials<TScope> : Label<TScope>` with `[AbsenceTolerant] GetInitialsCore` (null while an image replaces them; needs F2). Shortcuts: `GetInitialsShortcut() => Initials.GetInitials()`, `IsShowingImageShortcut() => Image.IsVisible()`. The component's `GetText`/`WaitText`/`AssertText` become `GetInitials`/`WaitInitials`/`AssertInitials`. No Core methods left. |
| `Popup` | `CommunityToolkit/Controls/Views/Popup.tpl.cs` | R1 + R2: `CloseWithCore` calls `Button(id).Click()` and then `RunWait(...)`, inside the generated `RunDoWithElement`. | Hand-written `CloseWith(buttonAutomationId, timeoutMs)`: `Button(id).Click(timeoutMs)`, then `WaitOpen(false, timeoutMs)`, throwing when it stays open (R4: button and popup root). |
| `StateContainer` | `CommunityToolkit/Controls/Layouts/StateContainer.tpl.cs` | R5 + R6: six hand-written members, because the generator does not forward extra parameters for `Is*`; the `Locator` overloads make tests build locators. | `IsShowingCore(IMauiElement? element, string viewAutomationId)` generates `IsShowing(id)` / `WaitShowing(id, ...)` / `AssertShowing(id, ...)` once F3 lands. The `Locator` overloads go. |
| `ContentDialog` | `Controls/Dialogs/ContentDialog.cs` | R5: hand-written `GetTitle`, `GetButtonTexts`, `GetMessage` return plain values with no Wait/Assert, and read `ContainerRoot` / `FindElements` in public members. | Becomes `ContentDialog.tpl.cs`: `GetTitleCore(element) => element?.Name`; `[GenerateComparisons(SequenceEquals \| HasItem \| Count)] GetButtonTextsCore(element)`; `GetMessageCore(element)` (reads the app's alert report, throws `NotSupportedException` as today). `DialogButton(text)` and `PromptInput` stay as parts. |
| `Stepper` | `Controls/Range/Stepper.tpl.cs` | R2 (minor): `SetValueCore` waits with `WaitHelper.WaitFor`, which drops the last error. | F1. It stays a **simple control**: its `{id}Minus` / `{id}Plus` buttons are implementation detail. The raw lookups in `StepperTests` are plan.md P3's business. |
| `Expander`, `RatingView`, `DrawingView`, `MediaPlayPauseButton` | CommunityToolkit | R2 (minor): `WaitHelper.WaitFor` in a Core method; element-level, so no nesting, but the last error is dropped. | F1. |

### Already conform (no change)

- **Base templates**: `ViewBase`, `FocusableControlBase`, `ClickableControlBase`,
  `ClickableItemBase`, `SelectableItemBase`, `SelectorControlBase`, `RangeControlBase` (its
  hand-written `WaitValueWithin` runs its own Core under one `RunWaitWithElement`).
- **Simple controls**: `Button`, `ImageButton`, `Entry`, `Editor`, `SearchBar`, `Label`,
  `Image`, `ActivityIndicator`, `ProgressBar`, `TitleBar`, `Slider`, `Picker`, `DatePicker`,
  `TimePicker`, `GraphicsView`, `WebView`, `HybridWebView`, `BlazorWebView`, all seven shapes,
  `TabItem`.
- **Containers and collections**: `Border`, `BoxView`, `ContentView`, `Frame`, `Grid`,
  `IsoPaneView`, `RefreshView`, `ScrollView`, `SwipeView`, `CarouselView`, `CollectionView`,
  `IndicatorView`, `ListView`, `TableView`, `Menu`, `TabMenu`, `Toolbar`, `ShellFlyout`
  (`Close` is hand-written as calls in sequence: R4).
- **Plain `.cs`, nothing to generate (R5)**: `ToolbarButton` (overrides `ClickCore` only),
  `MenuItem`, `ToolbarItem`, `ShellTab`, `ShellTabs`, `ShellFlyoutItem` (constructors only),
  `Shell` (holds the tabs and the flyout; no root, no behaviour), `TabMenuMarkup` (internal
  helper), `DateTimeFormats`.
- **`MediaElement`, `MediaPlayPauseButton` (bar F1), `MediaTimeLabel`**: done.

### Checked and left as they are

- `DatePicker` / `TimePicker` read a child (`DateText`, `FlyoutButton`) with
  `element.FindElements` inside a Core method. That is a raw read of the control's own
  subtree, not a public call on another control: R1 holds. A part would buy nothing.
- `Menu.OpenCore` finds its trigger with `element.FindElement(...)`: same reasoning.

## 3. Framework and generator changes first

**F1. `Until` for Core methods.** A protected helper on `ViewBase` and `RootedScopeBase`:

```csharp
/// Polls a read of an element this Core method already holds. No readiness check, no log
/// entry: the generated wrapper around the Core method did both. On timeout, returns false
/// with the last exception kept for the caller's message.
protected bool Until<T>(Func<T?> read, Func<T?, bool> done, int? timeoutMs, out Exception? lastError)
```

Plus an overload without `lastError`. Callers throw their own message and append
`lastError?.Message`. Replaces `WaitHelper.WaitFor` and `RunWaitWithElement` in Core methods
(`ToggleControlBase` x2, `Stepper`, `Expander`, `RatingView`, `DrawingView`,
`MediaPlayPauseButton`). Unit tests in `Brinell.Maui.Tests`: returns true when the read
succeeds late; returns false with the last exception after the timeout; never probes page
readiness (the mock records it); writes no log entry.

**F2. `[AbsenceTolerant]` on `Get*Core`.** Today it affects `Is*` only. For a getter it makes
`Get*` resolve with `TryFindElement` (null when absent, no wait), and `Wait*`/`Assert*` use
`RunWaitWithOptionalElement` / `RunAssertWithOptionalElement`. Needed by `AvatarInitials`. Generator
tests for the Get, Wait and Assert shapes, and for comparison variants.

**F3. Extra parameters on `Is*Core`.** Today only `Get*Core` forwards parameters after the
element. Give `Is*Core` the same: `IsShowingCore(IMauiElement? element, string viewAutomationId)`
emits `IsShowing(string viewAutomationId)`, `WaitShowing(string viewAutomationId, bool? expected = true, int? timeoutMs = null)`,
`AssertShowing(string viewAutomationId, bool? expected = true, string? message = null, int? timeoutMs = null)`.
Shortcuts for `Is*` then allow parameters too (today `ShortcutMethod` rejects them, because no
part member could take them). Generator tests for both.

**F4. Enforce R1 and R2 in the generator.** `ControlObjectAnalyzer` reports, as it does for
near-miss Core methods:
- a Core method body that calls `Run*` (`RunWait`, `RunDo`, `RunGet*`, `RunAssert*`, `RunSet*`);
- a Core method body that invokes a member on a property or method of the class whose
  declaration is `=> new(this, ...)` (a part), or on `Button(...)`, `Label(...)`, `Entry(...)`,
  `CheckBox(...)`, `Child<T>(...)` (the container's child factories).

Syntax only, like everything else in the generator. It is a **warning** (decided 2026-09-18):
generation continues, and the generator CLI prints each one, so `CreateMaui.Bat` shows it,
naming the Core method, the call, and the fix ("move it to the part and add a shortcut, or
hand-write the member as calls in sequence"). This keeps the next control from reintroducing
the pattern without blocking a build. Near-miss Core methods and malformed shortcuts stay
errors.

## 4. Steps

Each step builds and passes its tests before the next starts. One UI test process at a time.

1. **F1** with unit tests. Replace every `WaitHelper.WaitFor` / `RunWaitWithElement` in Core
   methods (list in F1). Regenerate: no `.gen.cs` should change (the Core signatures are the same).
   Run `Brinell.Maui.Tests`; UI tier 1 for `Control=Switch|Control=CheckBox|Control=RadioButton`,
   `FullyQualifiedName~Tests.Range`, and the toolkit controls touched.
2. **F2 + F3** in the generator, with generator tests. Regenerate everything and diff against
   a snapshot of all `.gen.cs` (as in step 1 of the component plan): **nothing** may change,
   because no template uses the new shapes yet.
3. **AvatarView**: `Initials` part of type `AvatarInitials`, shortcuts. Update `AvatarViewTests`:
   `GetText`/`AssertText` become `GetInitials`/`AssertInitials`, and `.Text.AssertText("BR")`
   becomes `.Initials.AssertInitials("BR")`.
4. **Popup**: hand-written `CloseWith`. `PopupTests` keep their calls.
5. **StateContainer**: `IsShowingCore(element, id)`; delete the six hand-written members and
   the `Locator` overloads. `StateContainerTests` pass ids instead of `Locator.ByAutomationId`.
6. **ContentDialog**: `.tpl.cs` with three getters. Update `AlertReadTests`
   (`dialog.GetTitle()` keeps its name; add `AssertTitle` / `AssertButtonTextsHasItem` where the
   test now compares by hand).
7. **F4** last, once nothing in the tree breaks it: add the checks as warnings, regenerate
   everything, and confirm `CreateMaui.Bat` prints none. Generator tests for each check
   (warning reported, output still generated), including a hand-written cross-part member
   (allowed: it is not a Core method).
8. **Verify**: solution build; `Brinell.Generator.Tests`; `Brinell.Maui.Tests`; UI tier 1 per
   control touched; then the **full UI suite**, because `ToggleControlBase` and `ViewBase` are
   shared by most of it. Compare `test-timings.md` with the previous run: Toggle and
   AvatarView should get faster, nothing slower. Android: Toggle tests if an emulator is up;
   Range and Picker have known Android baselines.
9. **Docs**: `docs/controls/community-toolkit.md` rows for AvatarView, Popup, StateContainer;
   `CHANGELOG.md`; the rules in [plan.md](plan.md) section 3 get R1 - R6 and the F1 helper.

## 5. Expected public API changes

| Control | Before | After |
| --- | --- | --- |
| `AvatarView` | `Text` part (`Label<AvatarView<TScope>>`); `GetText` / `WaitText` / `AssertText` | `Initials` part (`AvatarInitials<AvatarView<TScope>>`: a `Label`, plus `GetInitials`); `GetInitials` / `WaitInitials` / `AssertInitials` |
| `StateContainer` | `IsShowing(Locator)`, `WaitShowing(Locator, ...)`, `AssertShowing(Locator, ...)` and string overloads | string overloads only, generated; `WaitShowing`'s `expected` becomes `bool?` |
| `ContentDialog` | `GetTitle()`, `GetButtonTexts()`, `GetMessage()` | same names with `timeoutMs`, plus `WaitTitle`, `AssertTitle`, `AssertButtonTexts` (sequence), `AssertButtonTextsHasItem`, `AssertButtonTextsCount`, `WaitMessage`, `AssertMessage` |
| `Popup` | `CloseWith(id)` generated, returns the popup | hand-written, same signature and return |
| everything else | - | no change |

## 6. Out of scope

- Making `Stepper` a component with public `IncrementButton` / `DecrementButton` parts. It
  would change its base from `RangeControlBase` to `ComponentObjectBase` and lose the range
  API unless every member were re-forwarded. Revisit only if a test needs the buttons, which
  plan.md P3 argues it should not.
- A `Comparison.Within(tolerance)` variant to replace the hand-written tolerance members. They
  already conform (one unit of work); generating them needs a comparison with its own parameter.
- plan.md P2 (removing the public `Find*` surface): separate change. It will touch
  `MediaElement`'s element-finding overrides and `ContentDialog`.

## 7. Decisions (2026-09-18)

1. `AvatarView`'s text part shows initials, so it is named for them: the `Initials` part and
   `GetInitials` / `WaitInitials` / `AssertInitials` replace `Text` and `GetText` / `WaitText` /
   `AssertText`.
2. F4 reports a **warning**, printed by the generator CLI; it does not fail generation.

## 8. Done (2026-09-18)

All nine steps. Beyond the plan:

- **`AvatarImage` part.** The first run of step 3 failed `AssertShowingImage(false)`: forwarding
  to `Image.AssertVisible(false)` never passes for a missing image, because `IsVisibleCore`
  answers null, not false, for an absent element. `AvatarImage : Image` declares
  `[AbsenceTolerant] IsShownCore(element) => element?.Visible == true`, and the shortcut forwards
  to `IsShown`.
- **`WebView`.** Its `GetUrlCore` / `GetPageTitleCore` carried an `[AbsenceTolerant]` that had
  no effect on getters until F2. Removed, so `GetUrl` still waits for the WebView as before.
- **F1 plumbing.** `WaitHelper.WaitFor(read, done, timeoutMs, pollingMs, out lastError)` in
  `Brinell.Core`; `Until` wraps it on both bases. Timeouts pass `lastError` as `InnerException`.
- **F4.** `ControlObjectGenerator.Warnings`, printed by the CLI as `Warning: <file>: ...` with a
  count; exit code unchanged. The current tree produces none.

**Verification:** solution build 0 errors. `Brinell.Generator.Tests` 156/156 (+10 for F4, +5 for
F2/F3 and the state-shortcut change). `Brinell.Maui.Tests` 114 passed, 1 skipped (+5 `UntilTests`).
`Brinell.Core.Tests` 16/16. UI tier 1: 68/68 (Toggle, Range, Expander, RatingView, DrawingView,
MediaElement) and 19/19 (AvatarView, Popup, StateContainer, Dialogs). **Full UI suite: 334
passed, 1 skipped** (the opt-in stress test). Timings: Toggle classes faster than baseline;
`Background/ScrollVerbTests` was flagged once as slower (one scroll test at 2 s) and not on a
rerun of the class alone; it drives the driver directly and nothing here touches scrolling.
Android not run.

## 9. Open issue found

- **`IsVisible` on a missing element.** `ViewBase.IsVisibleCore` returns null for an absent
  element, and the generated `WaitVisible(false)` / `AssertVisible(false)` compare that with
  false, so they never pass for an element that is not there, although `[AbsenceTolerant]` and
  `RunWaitWithOptionalElement` exist precisely to ask about invisibility. Returning false for a
  missing element would fix it, but it also changes `IsClickableCore` (null -> false) and every
  `IsVisible()` caller that distinguishes "absent" from "hidden". Not changed; needs a decision.
