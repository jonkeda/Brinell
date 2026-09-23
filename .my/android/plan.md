# Android: plan

Status: **todo**, drafted 2026-09-19. No step has started.

This plan fixes the Android UI failures that the stale-readiness work left as "known driver gaps".
The design decisions are in section 3. Each step's status and evidence go in section 4 as the
work proceeds. Terms like R0, R2, R3 and `ActOnce` are from
[../stale-readiness/design.md](../stale-readiness/design.md).

## 1. Goal

Every MAUI UI test that runs on Android either passes or has a written reason why it cannot. A
reason names a platform limit; "driver gap" alone is not a reason. On 2026-09-19, the Range and
Selection subset was 10 of 31 passing, and the rest of the Android suite has never been
baselined.

Rules this plan keeps:

- **App bugs stay failures** (R0). A route that makes a test pass without doing what a user does is
  not a fix.
- **No coordinates and no sleeps in control objects.** The existing `WaitHelper.Pause` calls in
  the Appium range route go when that route is replaced.
- **Windows must not change.** The Windows suite stays green (353 tests: 351 passed, 2 gated
  skips).
- **Core is not changed.** This is MAUI-only work, like the stale-readiness project.

## 2. What fails, and why

Measured on 2026-09-19 on `emulator-5554` (Android API 36, Appium 3.1.2, UiAutomator2). The tree
was read with `uiautomator dump`, and the Slider through a scratch Appium session.

| # | Tests | Failure | Root cause | Evidence |
| --- | --- | --- | --- | --- |
| A1 | Picker, 5 tests | "shows 0 item(s)" / "no item with text 'Option 1' among the 0" | The item locator is `//*[@resource-id='android:id/select_dialog_listview']/*`. MAUI opens an **AppCompat** dialog, and its list is `com.brinell.samples.maui:id/select_dialog_listview`: the app's package, not `android:`. The rows are `CheckedTextView` elements with id `android:id/text1`. The route was written "compiled, not yet run on a device" | Dump of the open dialog: 5 rows, "Option 1" to "Option 5" |
| A2 | Slider, 6 tests | `WebDriverArgumentException`: "Cannot convert '' to float" | `AppiumMauiElement.SetRangeValue` clicks, then sends `Keys.Home` / `Keys.End` / arrow keys. UiAutomator2 treats keys sent to a `SeekBar` as a progress number, and the key characters parse as ''. The route also assumes 0-100 with a step of 1 | See the Slider probe below |
| A3 | Stepper, 10 tests | "Stepper 'TestStepper' does not expose its current value" | Android publishes nothing to read the value from. Windows reads it through the app bridge (`ReadState`), and Appium's `ReadState` is the interface default, `null`. The node has no range info, and its `text` is empty | The Stepper is a `LinearLayout` with two `Button` children, text "－" / "＋" (fullwidth), content-desc "−" / "+". The value appears only in the page's own label |

| A4 | ProductCollection, 4 tests | "did not reach the state waited for within 5000 ms. 1 attempt; last: Pending(scrolled) at 9886 ms" and "Could not scroll item 60 into view ... materialized items: 6, furthest reached: 5" | Finding a row by scrolling makes **one** sweep per attempt, and that sweep alone outlasts the call's whole budget, so the poll never gets a second look. On Windows a sweep is cheap; on Android each `mobile: scrollGesture` plus its settle is seconds. Root cause not yet confirmed: it may also be that a sweep does not advance the realized window | The failures name 1 attempt and an elapsed time past the budget (9886 ms of 5000, 16857 ms of 15000, 8001 ms of 5000) |
| A5 | CarouselView 5, IndicatorView 3 | "Expected Position to be '1'", "Expected Count to be '5'", "Expected 5 items" | Position and item count are read from the app through the Windows bridge (`ReadState`), which Android has no equivalent of, so every read answers null and every assertion fails. The same shape as A3 | All eight are assertion failures on Position or Count, not lookups: the control itself is found |
| A6 | CollectionIndexProbe, 1 test | "AppiumMauiElement does not implement ScrollToIndex" | The Appium element keeps the interface default for `ScrollToIndex`. No route written | The exception names the member |
| A7 | MediaElement, 11 tests | "Element not found within MediaElement 'AutomationId:TestMediaElement'. Child locator: AutomationId:TimeRemainingElement" (also ProgressSlider, PlayPauseButton) | MediaElement's transport on Android is ExoPlayer's own control bar, whose views carry ExoPlayer resource ids. The component addresses its parts by the MAUI `AutomationId`s the Windows player exposes, and none of them exist in the Android tree. To confirm with a dump | Every failure is a child lookup inside the component, by a part id |
| A8 | Expander, 6 tests | "Expander 'TestExpander' does not report IsExpanded, so tapping it could not be made idempotent. Declare GetState with a sink that answers it" and "Expected Expanded to be 'False'" | As A3 and A5: the open/closed state comes from the app's `GetState` verb, which is a Windows bridge route | The message names the missing declaration; the app does declare it, and Android cannot answer it |
| A9 | RatingView, 5 tests | "The element is no longer in the UI tree: the platform removed or replaced it" (3), a wrong exception type, a value mismatch | Tapping a star replaces the star elements, so the handle the call acted through goes stale before its effect can be read. Whether the app really rebuilds them or the driver only thinks so is not yet established. `RatingView_ReadsInitialRangeFromApp` is an A5-shaped read | Three of the five are `StaleElementException` after a tap |
| A10 | DockLayout 3, GridContainer 2, ContainerModule 2 | "Expected container to exist. Locator: AutomationId:TestDockLayout", "Last readiness state: MissingRoot", and two scoping assertions that expected a container **not** to find something | A MAUI layout has no node of its own in Android's accessibility tree unless something makes it important for accessibility, so a container object cannot resolve its root - and a container that cannot resolve its root does not scope, which is why the two "does not find controls outside itself" assertions fail the other way. `BoxView_IsNotAddressableOnWindows` is a Windows-only assertion running on Android | To confirm with a dump of the Container page |
| A11 | DatePicker 7, TimePicker 7 | "AppiumMauiElement does not implement SetDate" / "SetTime" | Both keep the interface default. Android opens a native date or time dialog, which nothing has mapped | The exception names the member |
| A12 | GestureVerb 6, GestureBridge 6, GestureAddressability 1, DrawingView 3 | "Gestures are not implemented for AppiumMauiDriver", "TestSwipeView is neither findable nor on the bridge" | These tests drive the Windows gesture bridge, which Android has no counterpart to (AD-005: on Android a gesture is real touch input). Some are Windows-only by nature and belong in a Windows-only marker rather than in a fix; DrawingView's strokes need W3C pointer sequences this driver does not write | The driver's own message says so |
| A13 | MenuVerb 4, ToolbarVerb 2 | "Menu items are not implemented for AppiumMauiDriver", and two tests asserting the wrong exception type | Menus are a desktop concept carried by the bridge. The two `Assert.Throws` failures are the same cause seen from the other side: the test expects Brinell's refusal and gets the driver's | |
| A14 | AlertRead, 2 tests | "Value of type 'Nullable<AlertContents>' does not have a value" | `ReadAlert` returns null on Android by design (the element says so). A native alert is in the tree and could be read from it; nothing does yet | |
| A15 | 53 tests, across Text 29, Selection 8, Toggle 7, Scroll 5, Dialogs 3, DateTimes 1 | "Could not get back to the hub, so the next page cannot be opened. This is a navigation failure, not a fault in what the test was about", each after 1 ms | **Cascades**, not causes: once the app cannot get back to the hub, every later test in the class fails instantly. The first is in `DatePickerTests`, where a native date dialog is left open (A11 leaves it open by failing inside it). **The whole Picker class is in here**, so this run did not measure A1 at all | The failures take 1 ms and the message says it is a navigation failure |
| A16 | 23 tests, mostly Toggle and Shell, late in the run | "An unknown exception was encountered sending an HTTP request to the remote WebDriver server", and twice "PageHub did not complete within 10000 ms" | **Environment, not the suite**: the Appium session or the emulator stops answering about 1.5 hours in, and everything after it fails on the transport. One test in the tail took 6 m 21 s. The mobile head does not run collections in parallel (its own `AssemblyInfo`), so this is not two sessions fighting - it is one session degrading over a long run | The exception is a transport error, not an assertion or a lookup |

The Slider probe on the Range page:

- Appium's `text` attribute on the `SeekBar` is its raw progress: `2.1474836E7` at value 1.0.
  So MAUI scales the progress to 0 to `int.MaxValue`.
- Sending a plain number sets the progress: `"1073741823"` made the value 50.0, and `"50"` made
  it 0.0. So UiAutomator2 sets progress through the accessibility action `ACTION_SET_PROGRESS`,
  which parses a float.
- There is no attribute for the minimum or maximum.

The 10 tests that pass are the `IsExists` / `IsVisible` / `IsEnabled` checks and
`StepperButtons_ReportInvokeBehavior`.

Not yet known: how the rest of the Android suite fares. `Brinell.Maui.UITests.Mobile` outside
Range and Selection, and `Brinell.Samples.Todo.UITests.Mobile`, have no recorded Android
baseline. Step 0 records one.

## 3. Design

### D1. The app publishes range state through accessibility, in its own units

For the Stepper there is nothing to read, so the app has to say something. The channel is the one
Android already has for this: the node's **range info**, which is what TalkBack announces. Windows
uses the Brinell bridge through `AppSupport`; Android gets the equivalent in `AppSupport`
(`Brinell.Maui.AppSupport`, which already targets `net10.0-android` but has no Android code yet):

- **Stepper.** An accessibility delegate on the handler's platform view (the `LinearLayout`)
  publishes `RangeInfo(float, Minimum, Maximum, Value)`. It is added with
  `StepperHandler.Mapper.AppendToMapping`, and refreshed when `Value`, `Minimum` or `Maximum`
  change.
- **Slider.** A delegate on the `SeekBar` replaces its native range info (0 to `int.MaxValue`)
  with the MAUI values. It handles `ACTION_SET_PROGRESS` by setting `Slider.Value`, clamped to
  the bounds, which is exactly what a user dragging the thumb can do.

This also improves accessibility for real users: TalkBack then announces "5" rather than
nothing, or a meaningless fraction. It sets no content descriptions and uses no hidden labels.

Probe P1 must confirm that UiAutomator2 reports a node's range-info current value as `text` for
a non-`ProgressBar` node. It does for the `SeekBar`: its empty `text` came back as the progress.

### D2. The driver reads and sets ranges the way UiAutomator2 does

- `AppiumMauiElement.RangeValue` is the node's `text` attribute parsed as a float (invariant
  culture; Java prints `2.1474836E7`), but only when the node publishes app units (D4). Otherwise
  it is `null`, never a raw fraction presented as a value.
- `AppiumMauiElement.SetRangeValue(value)` sends the value as a plain number
  (`element.SendKeys(value.ToString(InvariantCulture))`), which becomes `ACTION_SET_PROGRESS`.
  The Home/End/arrow route, the 0-100 assumption and its sleeps are removed. Confirming the
  effect stays with the control (`RangeControlBase` / `Confirm`), not with the driver.
- `RangeMinimum` / `RangeMaximum` stay `null` on Android: UiAutomator2 does not publish them.

### D3. The Stepper sets a value by pressing and watching, not by arithmetic on bounds

`Stepper.SetValueCore` currently needs the bounds and the step up front. On Android only the
value is readable (D1), so the Core method becomes:

1. Read the value.
2. Press the button towards the target.
3. `Confirm` that the value moved.
4. Repeat until the target is reached, or a press changes nothing (a bound).

The step is what one press changed. This works on Windows too, so one algorithm serves both
platforms, and a Stepper that ignores a press fails as `NotConfirmed` (R0). It presses as a user
would. Every press is a new action, so each is confirmed before the next, and the loop is bounded
by the call's budget (`CallRemainingMs`).

`FindChildButton` must also find Android's buttons: its class name locator `"Button"` has to
match `android.widget.Button` (probe P3).

### D4. An app without `AppSupport`

An app that does not include `AppSupport` publishes the Slider's raw 0 to `int.MaxValue`
progress and no Stepper value. The driver must not guess, so the decision is: **fail with a
message naming `AppSupport`**. The alternative was to derive values from a fraction plus bounds
the page object declares; that is left open (Q2) until a real app without `AppSupport` needs it.

To tell app units from raw progress, `AppSupport`'s delegate marks the node (probe P2 picks how;
for example `stateDescription`, if UiAutomator2 publishes it). The driver treats a range as
readable only when the mark is present.

### D5. The Picker's dialog

- The item locator matches the list by id **suffix** (`contains(@resource-id,
  ':id/select_dialog_listview')`), so both the framework and the AppCompat dialog are found. The
  rows are the list's children.
- Every failure after the dialog opened dismisses it (`try` / `finally`), not only the two known
  ones. A dialog left open cascaded into seven failures in the 2026-09-07 baseline.
- Out of scope: pickers whose items do not all fit the dialog (`LongPicker`, 200 items), and
  repeated texts (`DuplicatePicker`). Their tests are in `Tests/Background`, which the mobile
  project excludes. Selecting by index there needs the dialog scrolled with an honest logical
  index; that is listed as Q3.

## 4. Steps

Working rules (from past Android runs):

- **One UI test process at a time.** Two runs fight over the desktop and the device.
- **Restart Appium after any killed run.** An orphaned session force-stops UiAutomator2 300 s
  later, under the next run.
- **An emulator stuck offline:** relaunch it with `-no-snapshot`; restarting adb does not help.
- **The APK needs rebuilding only for app changes** (`AppSupport`, the sample app):
  `run-android-tests.ps1 -Build`. Control objects and drivers run in the test process.
- **Build separately from running.** Build the test project first, then run
  `run-android-tests.ps1` (it runs `dotnet test`).
- **Windows is re-run for every step that touches shared code** (`Brinell.Maui`, the sample app),
  using the smallest tier that can catch a problem: Range and Selection classes.

Status values: `todo`, `doing`, `done`, `blocked`.

| Step | Status | Contents | Done when |
| --- | --- | --- | --- |
| **0. Baseline** | doing | Run all of `Brinell.Maui.UITests.Mobile`, then `Brinell.Samples.Todo.UITests.Mobile`, on the emulator. Classify every failure by signature: A1-A3, a new driver gap, a cascade, or environment. Add rows to section 2 for new causes | Section 5 has both suites' counts; every failing signature has a row in section 2 or a note on why it is environmental |
| **1. Probes** | todo | A scratch Appium session (as in section 2), with no product code changed. **P1:** does `text` return the range-info current value for a `LinearLayout` with range info? (Needs a throwaway delegate in the sample app, or a test build.) **P2:** which marks does UiAutomator2 publish: `stateDescription` as an attribute, `hint`, other? **P3:** does `Locator.ByClassName("Button")` find `android.widget.Button` through the Appium driver? **P4:** does `ACTION_SET_PROGRESS` reach a custom delegate on a `SeekBar` (`performAccessibilityAction`)? | Each probe answered in section 4 notes, and D1, D2 or D4 updated where an answer changes them |
| **2. Picker** | todo | D5: suffix locator, `try` / `finally` dismiss. Remove the "compiled, not yet run" remark | Android Picker tests 8/8, or each remaining failure explained; Windows Selection 8/8 |
| **3. `AppSupport` for Android** | todo | D1: the Stepper and Slider delegates behind `#if ANDROID`, registered wherever `AppSupport` registers its Windows handlers. The mark from P2. Sample app rebuilt | A dump shows the Stepper node with its value, and a TalkBack-style check (`uiautomator dump` range fields, or an accessibility test) shows the MAUI units. Windows unchanged: the sample app builds for Windows, and Range 23/23 |
| **4. Driver range route** | todo | D2 and D4 in `AppiumMauiElement`: `RangeValue` from `text` when marked, `SetRangeValue` as a plain number, the arrow-key route deleted. The failure without `AppSupport` names it | Android Slider tests pass (9/9, or each remaining failure explained). The scratch session shows `GetValue` in MAUI units |
| **5. Stepper press-and-watch** | todo | D3 in `Stepper.tpl.cs`: `SetValueCore` loops press, then `Confirm`, and the step comes from one press. `FindChildButton` works on Android (P3). Regenerate the templates; the `.gen.cs` files are unchanged (R8). New unit pins: bounded by the budget; stops at a bound; a Stepper that ignores a press fails `NotConfirmed` with one press | Android Stepper tests pass (14/14, or each remaining failure explained); Windows Range 23/23; `Brinell.Maui.Tests` green |
| **6. The rest of the baseline** | todo | Whatever step 0 found beyond A1-A3, planned here as sub-steps once known | Every Android failure from step 0 is fixed or has a written reason |
| **7. Prove it** | todo | Full Android runs of both suites, twice each (flakiness). Full Windows `Brinell.Maui.UITests` and the Todo UI suite once | Android: no failure without a written reason, and the same result both times. Windows 351 passed, 2 gated skips; Todo 36 passed, 3 skipped |
| **8. Write it down** | todo | Memory notes (`range-uitest-baseline`, `android-selection-baseline`) updated to the new baseline; `docs/controls` platform notes; `CHANGELOG.md`; the skills (`maui-control`: the Android range route, `AppSupport` required for range state) | Docs updated |

### 4.1 Step notes

**Step 0, baseline (2026-09-19/20).** Full `Brinell.Maui.UITests.Mobile` on `emulator-5554`:
**300 tests, 126 passed, 174 failed**, about 1 h 50 m wall clock. Classified in section 2 as
A1-A16. The Todo suite has not been run yet.

The 174 failures are not 174 problems:

| Kind | Failures | What they are |
| --- | --- | --- |
| Cascades (A15) | 53 | One earlier failure left a dialog or page open; the rest of the class dies at 1 ms |
| Environment (A16) | 23 | The Appium session stopped answering late in the run |
| Real causes (A1-A14) | 98 | The rest |

What this changes about the plan:

- **Two thirds of the run is noise or one earlier failure.** 76 of 174 failures say nothing about
  the code under test. The cascade fix in D5 (dismiss on every failure, not only the known ones)
  is worth generalising beyond the Picker: a native dialog left open by *any* control does this.
- **A1 was not measured.** The Picker class was swallowed whole by the A15 cascade that started in
  `DatePickerTests`. Its 2026-09-19 numbers still stand; step 2 must measure it in its own run.
- **A long Android run is not trustworthy** (A16). From here on, Android runs go **per area**, not
  as one suite, and step 7's "twice each, full suite" needs rethinking on that basis.
- **A1-A3 are a minority.** They are 21 of the 98 real failures. Steps 2-5 leave most of the
  Android suite red, and step 6 is the bulk of the work rather than a tail.
- **Four causes are one cause.** A3 (Stepper value), A5 (Carousel and Indicator position), A8
  (Expander state) and part of A9 (RatingView's range) are all "Windows reads this through the
  app's `GetState` bridge, and Android has nothing". D1 answers it for ranges only. Whether it
  answers it for the rest is **Q1**, and the baseline turns that from a hypothetical into the
  question that decides how much of the suite step 7 can bring to green.
- **Some failures should not become fixes.** A12's bridge tests and `BoxView_IsNotAddressableOnWindows`
  are about Windows-only mechanisms. The honest outcome for them is a Windows-only marker with a
  stated reason, which the goal in section 1 allows.

A root cause marked "to confirm" above is a reading of the failure message, not a dump. The
probes in step 1 and a tree dump of the Container and MediaElement pages settle A7 and A10.

## 5. Baseline and measurements

| Measure | Value | Date | Notes |
| --- | --- | --- | --- |
| Android Range + Selection | 31 tests: 10 passed, 21 failed | 2026-09-19 | A1 × 5, A2 × 6, A3 × 10. Identical before and after the stale-readiness review fixes |
| Android Range (older) | 6 passed, 16 failed | 2026-09-05 | before the Stepper's `IsExists` / `IsVisible` / `IsEnabled` checks passed |
| Android Picker (older) | 0 passed, 8 failed | 2026-09-07 | one root failure and seven cascades from a dialog left open; since step 8 of stale-readiness, the 3 checks pass |
| Android full `Brinell.Maui.UITests.Mobile` | 300 tests: 126 passed, 174 failed | 2026-09-19 | 1 h 50 m. 53 cascades (A15), 23 environment (A16), 98 real. Classified as A1-A16 |
| Android `Brinell.Samples.Todo.UITests.Mobile` | not run | | step 0 |
| Windows `Brinell.Maui.UITests` | 353 tests: 351 passed, 2 gated skips | 2026-09-19 | must stay |
| Todo Windows UI | 36 passed, 3 skipped | 2026-09-19 | must stay |

## 6. Open questions

| # | Question | Default until decided |
| --- | --- | --- |
| Q1 | Should `AppSupport`'s Android delegates also publish other state that Windows reads through `ReadState` (for example, the CommunityToolkit views), or only ranges? | Ranges only; the rest when a test needs it |
| Q2 | An app without `AppSupport`: fail (D4), or derive values from a fraction plus bounds the page object declares? | Fail, naming `AppSupport` |
| Q3 | Long and duplicate pickers on Android (`Tests/Background` is excluded from the mobile project): scroll the dialog and keep a logical index, or leave them Windows-only? | Windows-only, and documented |
| Q4 | Does D3's press-and-watch replace the Windows bridge route for `SetValue` too, or only when bounds are unknown? | Replace it: one algorithm, and it is what a user does |
