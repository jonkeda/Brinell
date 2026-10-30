# Bruinet.NativeAndroid: the leftovers

What [plan.md](plan.md) left open on 2026-10-07, as a plan of its own. The main work is done and
verified (plan.md section 2.1); this file is the way to close the topic. Status: **draft**,
2026-10-07.

Each item says who can do it: **Claude** (code, docs, runs on the emulator), or **a person**
(an IDE, a voice, another machine, a decision).

## 1. Before anything else

| # | Item | Who | Done when |
| --- | --- | --- | --- |
| L0 | **Commit the work** on `move`, in the order the plan was built: (a) CI fixes (already pushed, `f7a857b4`); (b) step 0 and step 1 (`Bruinet.Appium`, its tests, `MauiOptions` gone, CI list); (c) the NativeAndroid rewrite, its unit tests and the `Bruinet.Core` `InternalsVisibleTo`; (d) the sample app and the UI test project; (e) the `Bruinet.Uat` discovery rule and `NATIVEANDROID` target; (f) the `AndroidRecorder` fixes; (g) docs, skills, `.docs/` | a person decides; Claude commits | The owner says yes; each commit builds; CI green on the pushed branch |

Separate commits matter here: (b), (e) and (f) change behaviour other stacks see, and should be
revertable on their own.

## 2. Code

Ordered by what a suite on a real app (`exact-bouw-android`) would hit first.

| # | Item | Who | Contents | Done when |
| --- | --- | --- | --- | --- |
| L1 | **Readiness costs half a second per attempt** | Claude | Measured: a page probe is 3-4 Appium round trips (package, activity, ready view, busy view) at 100-200 ms each, before the control's own lookup; a call that succeeds at once takes 1-2 s. Options, cheapest first: (a) one `mobile: getCurrentActivity` answers package and activity both (it returns `package/.Activity` on newer servers: check); (b) skip the activity read for a page that names none, and the ready-view read when the activity alone identifies the page; (c) read the busy view and the ready view in one UiSelector query (`resourceIdMatches` with both ids) and tell them apart by id; (d) a page probe result reused within one attempt by every scope that asks it (container, row, control all ask the same page today). Not: caching across attempts, which breaks R4 | The call log for `InvoicesTests.AfterADelete` shows a first-attempt `AssertText` under 600 ms (from about 1,200); full suite under 2.5 min on a fresh emulator; still 30/30 in 5 of 5 |
| L2 | **Controls not built** | Claude | In this order, each with a gallery view and a UI test: `TextInputLayout` (a component: its `EditText`, hint, error, helper text; Material reports the error as a separate `TextView`), `DatePicker` and `TimePicker` (spinner mode by its number pickers; calendar mode and the Material date picker dialog by tapping the day, confirmed by the header text), `SearchView` (its inner `search_src_text` field), `ExposedDropdownMenu` (an `AutoCompleteTextView` with a fixed list), `NavigationView` with `DrawerLayout` (open by the toolbar's navigation button, items as a `ChoiceGroupBase`), `BottomSheet` (a container shown over the page), `ViewPager2` (pages as a collection; `Swipe` between them), `CardView` (a container: only a name), `WebView` (a context switch to `WEBVIEW_*` and an Html scope inside, or out of scope: decide when a suite needs it). `Toast` stays out: recent Android does not put it in the tree | Each control: a `.tpl.cs` generated with no Android branch, a unit test over `FakeApp`, a gallery view, a UI test green 3 runs out of 3 |
| L3 | **Item bases MAUI has** | Claude | `ClickableItemBase` and `SelectableItemBase` (a row that is itself tappable or selectable, without a `Row` button), and `ComponentObjectBase` (a component with named parts that is not a scope). `ListItem` becomes a `ClickableItemBase` | `ListItem.Row.Click()` is `ListItem.Click()`; the invoices tests read the same with fewer words |
| L4 | **A near-miss warning for position keys** | Claude | A row keyed by position (no key view, no description, no text) cannot tell a recycled row from its item. Log a near-miss `Warning` when such a row is used after its list scrolled, as design section 7 said | A unit test: a position-keyed row used after a scroll logs one warning |
| L5 | **Presenter knows `NATIVEANDROID`** | Claude | `BddTargetRegistry` lists MAUI, WPF, WinForms, Blazor, Html and Stride; a native Android suite opened in the Presenter fails with "Runtime Target must be one of". Add `NATIVEANDROID` with `APPIUM_APP_PATH`, and check the Presenter's runner starts the gallery suite | The Presenter opens `Bruinet.NativeAndroid.UITests` and runs the sign-in scenario on the emulator |
| L6 | **Compose (plan step C1)** | Claude, when a suite needs it | `exact-bouw-android` has 6 composable files. Compose views are found through `testTagsAsResourceId` (the app sets it); Compose has its own idling. A Compose screen in the gallery first, to measure | Planned on its own when picked up |
| L7 | **The Todo app as a native head (plan step T2)** | Claude, when wanted | Proves AD-014 on a third head; a second app to keep in step | Planned on its own when picked up |

## 3. Reports

| # | Item | Who | Contents | Done when |
| --- | --- | --- | --- | --- |
| L8 | **`test-timings.md` and `accessibility-audit.md`** | a person decides; Claude builds | Plan step T1 asked for them "as MAUI's". They are not in `Bruinet.Maui` but in its UI test project (`tests/Bruinet.Maui.UITests/TestTiming.cs`, `Tests/Diagnostics/AccessibilityAuditTests.cs`), and Html has neither. **Decide:** (a) copy both into `Bruinet.NativeAndroid.UITests`, the way MAUI has them; or (b) move the timing report into a shared test-support place every `*.UITests` project uses, which AD-026 allows only for a reason in R1-R5 (an xUnit dependency is R3: not `Bruinet.Core`). Proposed: (b) for timings, since every stack wants them, and an accessibility audit per stack, since it reads platform properties (content descriptions, touch target sizes, labels for inputs on Android) | Each run writes `TestResults/<run-id>/suites/Bruinet.NativeAndroid.UITests/attachments/test-timings.md`, with a baseline, and an accessibility audit of the gallery's screens |

## 4. Checks for a person

| # | Item | Who | Steps | Done when |
| --- | --- | --- | --- | --- |
| L9 | **The suite in VS Code** | a person | Open the Bruinet folder in VS Code with the Bruinet extension; open `tests/Bruinet.NativeAndroid.UITests/Features/controls.feature.md`; look at the explorer's suites, pages and controls; run the scenario from the editor with the emulator and Appium up | The explorer lists the suite's five pages with their controls, the preview binds every step, Run passes |
| L10 | **A narrated recording** | a person | Write a `*.video.md` script for the sign-in scenario (see `docs/guides/narrated-recordings.md`), set a voice, record with `BRUINET_RECORD=<script>`; watch the video | The video has the voice-over, the steps' captions and the touch marks in the right places |
| L11 | **The run guide on another machine** | a person | Follow `docs/run/native-android.md` from a clean checkout on a machine that has not run it | The suite runs green without steps the guide does not give; anything missing goes into the guide |

## 5. Not ours, found on the way

| # | Item | Who | Contents | Done when |
| --- | --- | --- | --- | --- |
| L12 | **The Presenter's stitch scenario** | Claude | `PresenterUatScenarioTests`, `narrator-record.feature.md` line 26, expects "Stitched sample-tour-" (a stamped name), the app writes "Stitched sample-tour.mp4". Not run by CI. First establish it on a clean `main` checkout; then fix whichever side changed last (the scenario or `BddVideoComposer`'s naming) | Green on a clean build, and its cause in the commit message |

## 6. Closing the topic

When L0, L1, L5, L8 and L9-L11 are done (L2-L4 may move to a topic of their own, L6, L7 and
L12 already stand alone):

- Decisions found while building go to `.docs/` (most are there: the call-model's native Android
  section). Check nothing in [design.md](design.md) section 11 is missing from it.
- `.my/README.md`'s row goes; this folder is deleted in the same commit, and `.my/core/plan.md`
  step 6's link points at that commit.
