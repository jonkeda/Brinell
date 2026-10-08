# Brinell.NativeAndroid to the MAUI and Html standard: plan

The work plan and its status. What to build is in [design.md](design.md). Status: **doing**,
since 2026-10-07. The decisions **?N1**-**?N9** (design section 10) were taken as proposed when
the owner said to implement the plan (2026-10-07).

## 1. Goal

A test author writes a native Android page object and test the way they write a MAUI or Html
one, and gets the same behaviour:

- the same page, container, collection and control shapes, and the same member names for the
  same behaviour (a UAT control verb binds on all three stacks);
- every public control member is one synchronous call that resolves, gates on readiness, acts
  **once** and confirms (R0), within one budget, with one log pair;
- a replaced view, a recycled row, a closed session and a covering dialog each fail with their
  own message instead of a timeout;
- controls generated from `.tpl.cs` by the same generator, with no Android branch;
- a sample app and a UI suite that prove it on an emulator, and a UAT suite that runs `.feature.md`
  scenarios on the native head.

## 2. Steps

Order: the stack stands alone first (1-2), then the tests that force each behaviour (3), then
the behaviour (4-9), each step green before the next. The sample app (S1) can start after step 2
and must be ready before step 3's device tests. Each step is a commit or a short series, and
`dotnet build Brinell.sln` passes after each.

Status values: `todo`, `doing`, `done`, `blocked`.

| Step | Status | Contents | Done when |
| --- | --- | --- | --- |
| **0. Baseline** | done (2026-10-07, see 2.1) | Record the starting point in section 4: unit test count (16/16 on 2026-10-07); the public API (types and members, `GenAPI` or a reflection dump into `baseline/api.txt`) so step 2's removals are deliberate; confirm again that nothing outside the project references it. Add `Brinell.NativeAndroid.Tests` to CI's unit test list in `.github/workflows/ci.yml` (?N9). Delete the stray `MauiOptions.cs` and `NativeAndroidDriverOptions.FromConfiguration(MauiOptions)` | Baseline written; CI runs the project; build green |
| **1. Shared Appium layer** | done (2026-10-07, see 2.1) | Per ?N1 (proposed option A): a `srcnew/Brinell.Appium` project with `AppiumErrors` (public or `InternalsVisibleTo` both stacks), the `Live` helper, `AndroidHost`, `AndroidRecorder` with its `[assembly: ScreenRecorder]`, the locator-to-`By` mapping and the Appium options builder. `Brinell.Maui.Appium` references it and loses its copies. **No behaviour change**: a move, verified by the same tests passing identically. AD-026 gets the project in its examples | `Brinell.Maui.Tests`, `Brinell.Recording.Tests` green and identical; the MAUI Android baseline subset unchanged (section 4); a recorded MAUI Android scene still records (recorder discovery from the `recorders` folder still finds `AndroidRecorder`) |
| **2. NativeAndroid stands alone** | done (2026-10-07, see 2.1) | Own interfaces (design section 2): `INativeAndroidElement`, `INativeAndroidDriver`, `INativeAndroidElementScope`, `INativeAndroidScope<T>`, `INativeAndroidPage`, `INativeAndroidTestContext`, `INativeAndroidContainer<,>`. Element, driver and context implement them and stop implementing Core's `IElement<>`, `IDriver<>`, `IElementScope<>`, `IPageObject<>`, `ITestContext<>`, `IContainerControl<>`. Finds make **one attempt**: `FindElement(locator, timeoutMs)`, `WebDriverWait` and the `TryFindElement(..., timeoutMs)` overloads go. `InstanceKey` from the Appium element id. The page base carries Core's `[TestPage]`. Folders as `Brinell.Maui` (`Interfaces/`, `Controls/Base/`, `Containers/`, `Pages/`, `Context/`, `Testing/`). `InternalsVisibleTo` for `Brinell.NativeAndroid` and its tests in `Brinell.Core/AssemblyInfo.cs` (the only `Brinell.Core` change, ?N7). **Otherwise no behaviour change** | Solution builds; unit tests green; grep finds no `IElement<`, `IDriver<`, `IElementScope<`, `IPageObject<`, `ITestContext<`, `IContainerControl<` or `ControlObjectBase<` in `srcnew/Brinell.NativeAndroid` |
| **S1. Sample app** | done (2026-10-07, see 2.1) | Per ?N2: `samples/Brinell.Samples.NativeAndroid.App`, `net10.0-android`, AndroidX and Material views, no MAUI (design section 8): one screen per control family, a login screen whose button can be set to stay disabled, two long `RecyclerView`s (stable ids, and none), a busy state with the ?N3 signal, a permission request, an `AlertDialog` and a bottom sheet, a deep link per screen. Every view has a resource id. Added to `Brinell.sln` and to the run guide | `dotnet build -f net10.0-android` builds the APK; it installs and starts on the emulator by hand; each gallery screen opens by deep link |
| **3. Pin it down** | done, with a gap (2026-10-07, see 2.1) | Failing tests that force each following step, as Html's step 2. **Unit tests** (mocked element and driver, `Brinell.NativeAndroid.Tests`): (a) `Click` finds the element again on each call; (b) a caller's 300 ms budget is kept; (c) one log pair per call; (d) `Get`, `Wait*`, `Assert*` re-resolve per attempt; (e) an action that throws is not repeated; (f) a read below a call cannot outlast it. **R0 guards:** a button that never enables makes `Click` fail with "disabled" within its budget; a `SetText` with no effect returns `NotConfirmed` with `SendKeys` called once; a view recreated every 100 ms passes **and** logs a near-miss warning; the session closed mid-call raises `AppUnavailableException` without waiting out the budget. **Device tests:** create `testsnew/Brinell.NativeAndroid.UITests` with its fixture (Appium session, install and start the sample, reset between classes) and one smoke class, then run the smoke class five times and classify every failure (framework race, app bug, environment, driver gap) | The unit tests fail for the stated reason; the smoke baseline is in section 4 |
| **4. Stale signal** | done (2026-10-07, see 2.1) | Appium errors map once (design section 3 table) in the element's `Live` wrapper and the driver: `StaleElementException`, `AppUnavailableException` (session gone, app process gone), `ElementNotReadyException` with its reason, `RouteUnavailableException` for a `mobile:` command the server does not have. Every catch-all and empty catch goes, including `ScrollIntoView`'s. `PageLoadException`, `ElementNotFoundException` and `AssertionException` are no longer raised by this stack | Mapping unit tests; grep finds no `catch` without a named exception type in `srcnew/Brinell.NativeAndroid`; step 3's session-closed test passes |
| **5. Calls** | done (2026-10-07, see 2.1) | `ViewBase` (`.tpl.cs`) on `Brinell.Core.Calls`, with MAUI's `RunDo`, `RunDoWithElement`, `RunWait` (R8). Both private `Poll` loops and every `Thread.Sleep` deleted. `RunDoWithElement` resolves and gates within the budget, then acts once. One `LogEntry`/`LogExit` pair per public call; near-miss warnings; `BRINELL_CALL_LOG` and the context `Name` in the log line | Step 3's (a)-(f) pass; grep finds no `Thread.Sleep` in `srcnew/Brinell.NativeAndroid` |
| **6. Generated controls** | done (2026-10-07, see 2.1) | Every base and control becomes `.tpl.cs` with `*Core` methods plus a generated `.gen.cs`; the 14 bases fold into MAUI's set (design section 5 table) and implement Core's capability interfaces. The control catalog is renamed to Android's widgets and the MAUI-named classes go (?N4). `tools/Scripts/CreateNativeAndroid.Bat` beside `CreateHtml.Bat`, or one script with all inputs. Coordinate gestures (`Swipe(x1, y1, x2, y2)`, `Hover`) replaced by named, element-relative ones (design section 6). The page base's factory list follows the new catalog | `brinell controls generate` over `srcnew/Brinell.NativeAndroid/Controls` rewrites no `.gen.cs` (in step); the generator has no Android branch; the controls' unit tests pass; no `Task`-returning public member on a control, page or container |
| **7. Scope readiness** | done (2026-10-07, see 2.1) | `ScopeReadiness` and the `ProbeReadiness` chain; `AsksParent`; content readiness; `AppRoot` as the app's window (foreground package, no covering system dialog); the page's `ReadyLocator` and activity; the busy signal (?N3); dialogs as scopes under `AppRoot`; `AppRoot.WaitReady` a call with its own budget. `ScopeNotReadyException` throughout | Scope tests pass; a busy page delays a child call within its budget; a step behind an open dialog fails naming the dialog |
| **8. Confirm** | done (2026-10-07, see 2.1) | Act-then-confirm `*Core`s: `SetText` (text read back), `Check`/`Uncheck` (checked state), `SetValue` on a `SeekBar` (progress), `SelectItem` on a `Spinner` (selected text), `Expand`, `PullToRefresh` (refresh indicator seen, then gone). Each reads its effect once per attempt and never repeats the action | A replaced view gives the stale message and the action runs once; the "no effect" R0 test from step 3 passes |
| **9. Collections and containers** | done in part (2026-10-07, see 2.1) | `CollectionObjectBase`, `ItemObjectBase`, `ClickableItemBase`, `SelectableItemBase`; `ItemKey` for rows (design section 7: declared key view, content description, first text, position with a warning); a recycled row is stale, never acted on by position; materialize by scrolling with `MaterializeAttempts`. `RecyclerView`, `ListView`, `ViewPager2`, the `Spinner` popup. `ContainerObjectBase`, `ComponentObjectBase` (`TextInputLayout`, `Toolbar`, `TabLayout`, `BottomNavigationView`, `NavigationView`) | Collection tests pass on both sample lists; clicking a row after its key changed re-finds it by key; a row 200 items down is found by scrolling within its budget |
| **T1. UI suite** | done, reports open (2026-10-07, see 2.1) | `Brinell.NativeAndroid.UITests`: one class per control family against the gallery, page objects and controls only (AGENTS.md "Never In Tests"), traits as the MAUI UI tests. `test-timings.md` and `accessibility-audit.md` written per run (AD-007). The run guide `docs/run/native-android.md` (emulator, Appium, the APK, the commands) | Full suite green in 5 of 5 runs on the emulator; no class marked slower without a reason |
| **U1. UAT suite** | done, VS Code check open (2026-10-07, see 2.1) | `NativeAndroidTestFixtureBase` registered in `UatTargetConventions` as `NATIVEANDROID` (?N5); the platform's UAT suite inside `Brinell.NativeAndroid.UITests` (`uat.config.md`, `Features/`, `Uat/`, AD-026): a login journey and a list journey written with the control verbs only. `Category=UAT` runs them alone. The language server and `brinell` find the suite and its pages | `--filter "Category=UAT"` green; the VS Code explorer lists the suite's pages and controls; a phrase that works on MAUI for the same capability binds here |
| **R1. Recording** | done, silent (2026-10-07, see 2.1) | The fixture is `IRecordable` with `RecordingTarget.AndroidDevice`; the element reports taps, long presses, swipes and typing through `Interactions` (AD-024 touch marks) | A narrated scene of the login journey records on the emulator with its touches marked |
| **10. Prove it** | done (2026-10-07, see 2.1) | On a branch: (a) the sample's login button stays disabled: the login test fails with "disabled for N ms", not a silent timeout; (b) re-introduce "action retried" in `ViewBase`: a step 3 test catches it; (c) bind `RecyclerView` rows by position only: the recycled-row test fails naming the key change; (d) the full UI suite against the step T1 timing baseline | (a)-(c) fail against their bugs and name what they saw; no new failures or slower classes against the baseline |
| **11. Write it down** | done, topic kept (2026-10-07, see 2.1) | In `.docs/`: widen AD-010 and AD-013 from "MAUI and Html" to the three stacks, and the call-model and control-object contracts likewise; the invariants list; AD-026's examples (`Brinell.Appium`). In `docs/`: rewrite `platform-guides/native-android.md`, `controls/index.md`, `architecture/structure.md`, the run guide, `README.md`. Skills per ?N6 (proposed `android-control` and `android-ui-test`, with `.claude/skills/` stubs) and `AGENTS.md`'s skills list. `.my/core/plan.md` step 6 and `migration.md`: NativeAndroid now stands alone, so its core move has MAUI's shape; its baseline is this topic's, not "Range 6/16, Picker 0/8" (those are MAUI Android numbers). `CHANGELOG.md`. Then extract and delete this topic | Docs merged; links valid; the `.my/README.md` row removed |
| **C1. Compose (later, optional)** | todo | Per ?N8: Compose views found through `testTagsAsResourceId`, Compose idling in readiness, a Compose screen in the sample. Only when a suite needs it | Planned separately when it is picked up |
| **T2. Todo native head (later, optional)** | todo | The Todo app as a native Android head on the shared Todo `Core`, `Contracts` and `Infrastructure`, with resource ids equal to the MAUI AutomationIds; the Todo `.feature.md` files run on it (AD-014) with the ?N5 head tag | Every Todo scenario that passes on MAUI passes on the native head, or carries a head tag and a reason |

### 2.1 Step notes

Each done step gets a dated note: what it changed, anything that surprised, the numbers.

- **Step 0** (2026-10-07). API inventory in [baseline/api.txt](baseline/api.txt) (642 lines).
  `Brinell.NativeAndroid.Tests` in CI's list. `MauiOptions.cs` deleted. Surprise: the platform
  guide documented `NativeAndroidDriverOptions.FromEnvironment()`, which did not exist; the only
  loader was `FromConfiguration(MauiOptions)`. `FromEnvironment()` now exists as documented, with
  a `Func<string, string?>` overload for tests; 3 tests added (19 in all).
- **Step 1** (2026-10-07). `srcnew/Brinell.Appium` (net8-10, like the rest of `srcnew/`) holds
  `AppiumErrors` (now public, with `IsUnknownCommand` for `RouteUnavailableException`),
  `AndroidHost` and `AndroidRecorder`, moved with `git mv`. `ScreenRecorders` knows the package
  as `Brinell.Appium`. **The locator mapping did not move**: MAUI's is about MAUI's AutomationId
  and switches on `MauiPlatform`, so each stack keeps its own (design section 9 corrected).
  `testsnew/Brinell.Appium.Tests` (AD-026, one test project per library) took `AppiumErrorsTests`
  and `AndroidHostTests` from `Brinell.Maui.Tests` (187 = 174 + 13, identical) and adds 5 tests,
  one of them that `ScreenRecorders.Discover()` offers `AndroidRecorder` from the new package.
  Both test projects are in CI's list. Not checked here: a recorded MAUI Android scene (needs the
  emulator; checked in step R1).
- **Steps 2 and 4-8, built together** (2026-10-07). Nothing consumes the project, so the new layers
  were built once, in step order, and the old classes deleted as their replacements landed,
  rather than re-typing the old hand-written controls in step 2 and replacing them in step 6.
  `Interfaces/` (`INativeAndroidElement`, `INativeAndroidDriver`, `INativeAndroidElementScope`,
  `INativeAndroidScope<T>`, `INativeAndroidPage`, `INativeAndroidTestContext`,
  `INativeAndroidContainer<,>`, `GestureDirection`); `Appium/` (`AppiumNativeAndroidElement` with
  `Live`, `AppiumNativeAndroidDriver` on `mobile:` commands with an implicit wait of zero,
  `NativeAndroidTestContext`); `ObjectBase`; `Pages/` (`PageObjectBase` with `[TestPage]`, activity,
  ready view, busy view `brinell_busy` and declared busy indicators; `AppRoot`); `Containers/`;
  `Collections/`; `Controls/Base` (`ViewBase` and six capability bases, `.tpl.cs`/`.gen.cs`); 24
  Android-named controls; `tools/Scripts/CreateNativeAndroid.Bat`. The generator needed no change.
  `Brinell.Core` changed only by two `InternalsVisibleTo` lines. Greps: no `IElement<`,
  `IPageObject<`, `ControlObjectBase<`, `Thread.Sleep` or untyped `catch` in
  `srcnew/Brinell.NativeAndroid`. Found on the way: `NativeAndroidByExtensions` sent `By.Id`, which
  W3C turns into CSS (MAUI met the same); it now sends UiAutomator2's `id` strategy.
- **Step 3** (2026-10-07): 65 unit tests (`CallModelTests`, `ScopeReadinessTests`,
  `CollectionTests`, `AppiumErrorMappingTests`, locator tests). **Gap:** they were written with the
  new code, not before it, so they never failed on the old code (which could not compile against
  them). Step 10 re-introduces the bugs to show they catch them. The device smoke baseline is the
  first full run in step T1.
- **S1** (2026-10-07). `samples/Brinell.Samples.NativeAndroid.App`: five screens with deep links
  (`brinell-native://login|controls|invoices|dialogs|navigation`, `login?stuck=1` for the R0
  proof), fixed activity names, 200 invoices in a `RecyclerView`. Found: a Debug build's Fast
  Deployment leaves the assemblies out of an APK installed by adb or Appium
  (`EmbedAssembliesIntoApk`); Material toolbars read `app:title`; API 35+ draws edge to edge
  (`fitsSystemWindows`); a spinner reports its first item selected on layout. **The emulator:**
  `Brinell_Perf` with its default GPU drew the app black in screenshots; `-gpu swiftshader_indirect`
  draws it (the run guide must say so).
- **Measured on the emulator, and built in** (2026-10-07):
  - UiAutomator2 evaluates an XPath under a view in a document **rooted at that view**, from the
    document node: `./*` is the view itself, `.` matches nothing, `//X` stays in the subtree.
    `ToAndroidChildBy` rewrites `.` to `/*` and `./X` to `/*/X`.
  - Views outside the screen are not in the tree, so a control on a page that is not found is
    scrolled in with `UiScrollable.scrollIntoView`, throttled like a scroll into view.
  - A popup (spinner options, suggestions) is a window without focus; UiAutomator2 shows it only
    with `enableMultiWindows`, now on by default.
  - `SendKeys` on a `SeekBar` sets its progress (Android's set-progress action) and the app hears
    it. On a `RatingBar` the same action moves the bar **without telling the app**, so a rating is
    set by a tap inside the star's step, and Android's progress is in steps (3.5 stars = 7).
  - UiAutomator2 sets text without focusing a field; an `AutoCompleteTextView` filters only when
    focused, so it is tapped first.
  - A keyed list item is found with `UiScrollable.scrollIntoView` on that list (to its start, then
    forward), then by hand-scrolling; hand-scrolling alone took longer than the budget for an item
    put back above the visible rows.
- **Step 9, in part**: `CollectionObjectBase`, `ItemObjectBase`, `ItemKey` (declared key view,
  description, first text, position), `RecyclerView`/`ListView` with `ListItem`, `ContainerObjectBase`,
  `Toolbar`, `AlertDialog`, `PermissionDialog`, `Snackbar`, `TabLayout`, `BottomNavigationView`,
  `RadioGroup`. **Not built:** `ClickableItemBase`/`SelectableItemBase`, `ComponentObjectBase`,
  `ViewPager2`, `TextInputLayout`, `NavigationView`/drawer, `BottomSheet`, `DatePicker`/`TimePicker`,
  `WebView`, the near-miss warning for a position key on a scrolled list.
- **T1** (2026-10-07): `testsnew/Brinell.NativeAndroid.UITests`, 28 tests in five classes on one
  Appium session (`GalleryFixture`, `enforceAppInstall` so the APK just built is the one tested).
  Full suite green once, 209 s. **Open:** the 5-of-5 runs, `test-timings.md` and
  `accessibility-audit.md` (MAUI's fixture writes them; this one does not yet), the run guide.
- **U1** (2026-10-07): `NATIVEANDROID` in `UatTargetConventions`; `uat.config.md`, two features,
  `NativeAndroidUatScenarioTests` on the same session, the UI tests' pages named with
  `[TestPage("Sign In")]` (no second set of pages). **Found:** UAT took a property for a control
  only by name suffix (`Button`, `CheckBox`, `Text`, ...), MAUI's and Html's names; Android's
  `Spinner`, `RadioGroup`, `TextView` were not controls, and a page's `Self` was one.
  `UatReflectionRuntime` now also takes any type with Core's `IElementObject<>` and skips page
  types.
- **R1** (2026-10-07): the element reports taps, long presses, swipes and typing through
  `Interactions`, and the context is `IRecordable` with the device's serial. `BRINELL_RECORD=1`
  recorded both UAT scenarios, three runs in a row; the sign-in video's timeline has its five
  touches (clear, type, clear, type, tap) with the views' bounds. **Found and fixed in
  `AndroidRecorder`** (MAUI's recordings too): `pkill -2 screenrecord` failed when another user's
  `screenrecord` was running (now `pkill -2 -f <its file>`), and adb could not pull to a path past
  260 characters (now via the temp folder). **Open:** a narrated scene, with a video script and a
  voice.
- **U1, the tools** (2026-10-07): `brinell scenario preview` binds both features through
  `Brinell.Bdd.Workspace` with no diagnostics, every step to a built-in verb. **Open:** looking at
  the suite in the VS Code explorer, which is for a person.
- **Step 10** (2026-10-07). (a) The stuck sign-in button fails as `ElementNotReadyException`,
  `Disabled`, within its 2000 ms budget, and is never tapped (a UI test, green). (b) `ViewBase`'s
  `ActOnce` changed to retry any failed action: `E_Click_WhenTheActionThrows_TapsOnceAndLogsTheError`
  and the `Hover` guard fail. (c) A row found by where it was, not its key: the unit test fails, and
  on the device the "Invoice 4" row reads '€ 50,00', Invoice 5's amount, after a delete. Each
  restored after. (d) is T1's five runs. **Found:** UiAutomator2's 10 s idle wait before every
  command made a `UiScrollable` sweep on an animating screen never return, and a whole run hung.
- **T1, repeat runs** (2026-10-07): a first set of runs gave 29/30, 30/30, 29/30, the one failure
  each time a snackbar gone before the Undo tap reached it: Android shows a long snackbar for
  2.75 s, and every attempt of the call first asked the page (about 0.6 s). `Snackbar` no longer
  asks its page (`AsksParent => false`), and the gallery shows its snackbars for 8 s. A second set:
  30, 30, 29, 30 and an aborted fifth, slower each time (666 s to 936 s): the permission test only
  answered a prompt that showed within 5 s (now it answers it, the tap waiting), and the emulator
  had run for two hours. **On a restarted emulator, the final code: 30/30 in five runs out of five,
  189-204 s each.** The run guide says to restart a slowing emulator. **Open:** `test-timings.md`
  and `accessibility-audit.md`, which MAUI's fixture writes and this one does not.
- **Step 11** (2026-10-07): AD-010, AD-013, AD-026, the call-model and control-object contracts,
  the invariants, `docs/platform-guides/native-android.md`, `docs/run/native-android.md`,
  `docs/README.md`, `structure.md`, `controls/index.md`, `README.md`, `CHANGELOG.md`, the
  `android-control` and `android-ui-test` skills, `AGENTS.md`, `.my/core/`. **The topic is kept,**
  not deleted: open are T1's repeat runs, the VS Code check, a narrated scene, the controls not
  built (design section 11), and the cost of a readiness probe (about 0.5 s per attempt; a full run
  takes about 6 minutes).

## 3. Decisions needed before a step can start

| Step | Needs |
| --- | --- |
| 1 | ?N1 |
| 2 | ?N7 |
| S1 | ?N2, ?N3 (the app shows the busy signal) |
| 6 | ?N4 |
| U1 | ?N5 |
| 11 | ?N6 |

## 4. Baselines

Recorded in step 0 and step 3; never edited after.

- `Brinell.NativeAndroid.Tests`: 16 passed, 0 failed (2026-10-07, before step 0).
- MAUI Android subset (for step 1's "unchanged"): take it from the current MAUI Android run
  before step 1 starts; it is MAUI's, not NativeAndroid's.
- Device smoke baseline: step 3.
- UI suite timings: step T1.

## 5. Verification

From the Brinell root:

```powershell
dotnet build Brinell.sln -v:minimal /nr:false
dotnet test testsnew\Brinell.NativeAndroid.Tests\Brinell.NativeAndroid.Tests.csproj -v:minimal /nr:false
dotnet test testsnew\Brinell.Maui.Tests\Brinell.Maui.Tests.csproj -v:minimal /nr:false
```

The UI suite needs a running emulator, an Appium server with UiAutomator2, and the sample APK
built; the run guide written in step T1 has the commands. One UI test process at a time, and no
build while one runs. Rebuild the sample app before a UI run when anything under `samples/`
changed.

## 6. Not in scope

- iOS native, a native iOS stack, or XCUITest.
- A Scraper source for Android (the Scraper's own open item "a UI Automation source" covers
  sources other than the browser).
- The Presenter running native suites.
- Moving NativeAndroid onto `ICore*`: that is `.my/core/` step 6, after this topic.
- Running the UI suite in CI.
