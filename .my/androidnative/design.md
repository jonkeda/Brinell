# Bruinet.NativeAndroid to the MAUI and Html standard: design

What to build. The steps and their status are in [plan.md](plan.md). Where this says "as MAUI"
or "as Html", the shape is the one recorded in AD-010, AD-013, the
[call-model](../../.docs/contracts/call-model.md) and
[control-object](../../.docs/contracts/control-object.md) contracts, and the code under
`src/Bruinet.Maui` and `src/Bruinet.Html`; this file records only what NativeAndroid
adds or does differently.

## 1. Where it stands (2026-10-07)

`src/Bruinet.NativeAndroid`: 74 C# files, about 3,700 lines. `tests/Bruinet.NativeAndroid.Tests`:
16 unit tests (locator mapping and options), all passing, and **not in CI's unit test list**.
No UI tests, no UAT suite, no sample app. Nothing else in the repo references the project.

| Area | MAUI and Html | NativeAndroid today |
| --- | --- | --- |
| Interfaces | Own set: `IMaui*` / `IHtml*` element, driver, element scope, scope, page, test context, container (AD-010) | Core's generic `IElement<NativeAndroidElement>`, `IDriver<>`, `IElementScope<>`, `IPageObject<>`, `ITestContext<>`, `IContainerControl<>`; one own `INativeAndroidScope<T>` |
| Element identity | `InstanceKey`; a `Live` wrapper maps "element gone" and "session gone" | None. Appium exceptions leak or are swallowed |
| Finds | One attempt, no timeout finds; the call polls | `FindElement(locator, timeoutMs)` with `WebDriverWait`, and finds inside controls with their own timeouts |
| Calls | `Bruinet.Core.Calls` (`ControlCall`, `Poller`, `Deadline`, `Confirmer`, `ScopeGate`); one log pair per call; near-miss warnings | Private `Poll` loops with `Thread.Sleep` in the control base and page base; `catch (WebDriverException)` and an empty `catch (ElementNotFoundException)` |
| Act once (R0) | An action runs once and confirms its effect | Not confirmed; `ScrollIntoView` swallows `WebDriverException` and returns |
| Exceptions | `StaleElementException`, `ElementNotReadyException`, `ScopeNotReadyException`, `AppUnavailableException`, `RouteUnavailableException` from `Bruinet.Core.Exceptions` | `ElementNotFoundException`, `AssertionException`, `PageLoadException` |
| Scope readiness | `ScopeReadiness`, `ProbeReadiness`, `AppRoot`, a busy signal, a parent asked first | `IsReady` is `true` on the context; a page is "loaded" when its `ReadyLocator` is visible |
| Controls | `.tpl.cs` with `*Core` methods, `.gen.cs` from `Bruinet.Generator`, MAUI's `Run*` helpers (R8), Core capability interfaces | Hand-written public members on 14 capability bases; `ControlObjectBase<TScope>` |
| Collections | `CollectionObjectBase`, `ItemObjectBase`, `ItemKey`, waiting `Item(index)`, answering `TryItem` (R7) | `AndroidRecyclerView` with no item model; 5 collection types that are one class each with no members |
| Containers | `ContainerObjectBase`, `ComponentObjectBase` | `AndroidContainerBase` and five flavours (scrollable, expandable, refreshable, ...) |
| Control names | MAUI's control names on MAUI; HTML element names on Html | Mostly **MAUI's** names (`Entry`, `Editor`, `CarouselView`, `FlyoutItem`, `TableView`, `RoundButton`, `MediaElement`, ...), which a native Android app does not have |
| Page objects | `PageObjectBase` with Core's `[TestPage]`, found by UAT discovery | `NativeAndroidPageObjectBase` with about 90 control factory methods; no `[TestPage]` |
| UAT | A fixture base registered in `UatTargetConventions`; a head in `BddTagConventions` | Neither |
| Recording | `IRecordable`; touch marks (`Interactions`, AD-024) | Neither |
| Strays | - | `MauiOptions.cs`, a copy of MAUI's options declared in Core's namespace `Bruinet.Core.Configuration` |

## 2. Target shape

The same layers as MAUI and Html, named for the stack:

| Layer | Type | Notes |
| --- | --- | --- |
| Element | `INativeAndroidElement` | `InstanceKey`; state (`Visible`, `Enabled`, `Selected`, `Checked`, `Focused`, `Text`, `Rect`); Android attributes (`ResourceId`, `ContentDescription`, `ClassName`, `Hint`, `Checkable`, `Scrollable`); actions; `TryFindElement` and `FindElements` only (one attempt) |
| Driver | `INativeAndroidDriver : IDiagnosticDriver, IDisposable` | `AppRoot`, `CurrentActivity`, `CurrentPackage`, `IsIdle()`, `FindElements`, deep links, back, app state (`ActivateApp`, `TerminateApp`, `IsAppRunning`), keyboard shown/hidden, screenshots, page source |
| Element scope | `INativeAndroidElementScope` | As `IHtmlElementScope`: `Context`, `DefaultLocatorStrategy`, `Page`, `ProbeReadiness()`, `IsReady()`, `WaitReady()`, finds |
| Scope | `INativeAndroidScope<TScope>` | Keeps its name; becomes `Self` over the element scope, as `IHtmlScope<T>` |
| Page | `INativeAndroidPage`, `INativeAndroidPage<TSelf>` | As `IHtmlPage`; the title is the activity, or a toolbar title when the page names one |
| Test context | `INativeAndroidTestContext : ITestContext, INativeAndroidElementScope` | `Driver`, `Name`, `NearMiss`, `Timeouts`, `Logger` |
| Container | `INativeAndroidContainer<TParent, TSelf>` | `Parent`, `ContainerRoot` |
| Bases | `ViewBase<TScope>` (`.tpl.cs`), the capability bases, `PageObjectBase<TSelf>`, `ContainerObjectBase`, `ComponentObjectBase`, `CollectionObjectBase`, `ItemObjectBase`, `ItemKey`, `AppRoot` | Folders as `Bruinet.Maui`: `Interfaces/`, `Controls/Base/`, `Controls/<Family>/`, `Containers/`, `Pages/`, `Context/`, `Testing/` |

The class names follow MAUI's (`PageObjectBase`, `ViewBase`, `ClickableControlBase`), in the
`Bruinet.NativeAndroid.*` namespaces, so the skills and the generator read the same on every stack.

What does **not** come over: `Shell` chrome, the gesture bridge (section 6), `MauiPlatform`
switches, `IsoPaneView` and anything else that exists because of MAUI.

## 3. Calls, exceptions and the element

**Calls.** NativeAndroid uses `Bruinet.Core.Calls` as it is, with no copy (AD-010: the leaf
layer has one home). `ScopeGate` and the other internal types need
`[assembly: InternalsVisibleTo("Bruinet.NativeAndroid")]` and `("Bruinet.NativeAndroid.Tests")`
in `src/Bruinet.Core/AssemblyInfo.cs`, beside the MAUI and Html lines. That is the only change
to `Bruinet.Core`.

**Exceptions.** Appium's errors map once, in the element's `Live` wrapper and the driver:

| Appium says | Bruinet raises | The call |
| --- | --- | --- |
| `StaleElementReferenceException` | `StaleElementException` | re-resolves on the next attempt |
| `NoSuchElementException` from a one-attempt find | nothing: the find answers `null` | polls on |
| "invalid session id", "session is either terminated or not started", the server unreachable | `AppUnavailableException` | ends now, without waiting out the budget |
| The app's process gone (`queryAppState` is not running) while a call waits | `AppUnavailableException` | ends now |
| `InvalidElementStateException`, a disabled or obscured target | `ElementNotReadyException` with its `NotReadyReason` | polls on, then fails naming the reason |
| A `mobile:` command the server does not have (`UnknownCommand`, "not supported") | `RouteUnavailableException` | ends now: retrying cannot help |
| Any other `WebDriverException` | itself | ends the call; no catch-all |

`ElementNotFoundException`, `AssertionException` and `PageLoadException` stop being raised by
this stack, as on Html.

**The element.** `NativeAndroidElement` (renamed if **?N1** moves the Appium layer) implements
`INativeAndroidElement` around an `AppiumElement`:

- `InstanceKey` is the Appium element id: UiAutomator2 gives a new id when a view is recreated
  or a `RecyclerView` row is rebound, which is exactly the "replaced" signal the call needs.
- Every read and action goes through `Live`, as `AppiumMauiElement` does.
- A root element (`ForApp`) stands for the window, for `AppRoot` and page-level finds.
- Actions that mark a touch on the video report through `Interactions`, as MAUI does (AD-024),
  so a recorded native scene shows its taps.

## 4. Scope readiness

As MAUI and Html: `ScopeReadiness` from `ProbeReadiness`, the parent asked first (`AsksParent`),
content readiness after it, `ScopeNotReadyException` when the budget ends.

- **`AppRoot`** is the app's window: ready when the app under test is in the foreground (the
  current package is the app's) and no system dialog covers it. A permission dialog, the IME
  picker or an ANR dialog is a not-ready reason that names itself, not a missing element.
- **A page** is ready when its `ReadyLocator` is visible and its activity matches, when the page
  names an activity.
- **Busy.** A native app has no standard busy flag. **?N3** decides the convention. The proposal: a
  view whose content description or resource id is a well-known name (`bruinet_busy`), present
  while the app is busy, read the way Html reads `data-busy`, plus any visible
  `ProgressBar` or `SwipeRefreshLayout` the page declares as its busy indicator. A page with
  neither is never busy.
- **Dialogs** (`AlertDialog`, a bottom sheet, the permission dialog) are scopes of their own with
  `AppRoot` as parent, so a step on the page behind a dialog fails with "a dialog is open: ...".

## 5. Controls

**Generated, as Html.** Every control is a `.tpl.cs` with `*Core` methods and a `.gen.cs` written by
`bruinet controls generate`. `ViewBase` has MAUI's `RunDo`, `RunDoWithElement` and `RunWait`, name
for name and signature for signature (R8), so the generator needs no Android branch (AD-013's
"broken when" applies here too). The bases implement the same Core capability interfaces
(`IClickableControlObject`, `IEditableTextControlObject`, `IToggleControlObject`,
`IRangeControlObject`, `ISelectorControlObject`, ...), so UAT control verbs bind the same on all
three stacks. No async members.

**Bases.** Today's 14 hand-written bases fold into MAUI's set:

| Today | Becomes |
| --- | --- |
| `NativeAndroidControl` | `ViewBase` |
| `AndroidClickableControlBase` | `ClickableControlBase` |
| `AndroidFocusableControlBase` | `FocusableControlBase` |
| `AndroidToggleControlBase` | `ToggleControlBase` |
| `AndroidRangeControlBase`, `AndroidProgressControlBase` | `RangeControlBase` (progress is a read-only range) |
| `AndroidSelectorControlBase` | `SelectorControlBase` |
| `AndroidScrollableControlBase`, `AndroidScrollableContainerBase` | `ScrollableControlBase` and a scrollable `ContainerObjectBase` |
| `AndroidContainerBase`, `AndroidExpandableContainerBase`, `AndroidRefreshableContainerBase` | `ContainerObjectBase`, with expand and refresh as members of the controls that have them |
| `AndroidExpandableControlBase`, `AndroidRefreshableControlBase`, `AndroidSwipeableControlBase` | members on the controls that need them, not bases |
| - | `ClickableItemBase`, `SelectableItemBase` (collections, section 7) |

**The catalog is Android's, not MAUI's** (**?N4**). A native app is made of Android widgets;
MAUI's names describe a different toolkit and mislead a page-object author. Proposed set, by
family:

| Family | Controls |
| --- | --- |
| Buttons | `Button`, `ImageButton`, `FloatingActionButton`, `Chip` |
| Text | `TextView`, `EditText`, `TextInputLayout` (a component: its `EditText`, hint, error, helper), `SearchView`, `AutoCompleteTextView` |
| Toggle | `CheckBox`, `Switch`, `RadioButton`, `RadioGroup`, `ToggleButton` |
| Range | `SeekBar`, `RatingBar`, `ProgressBar` |
| Selection | `Spinner`, `ExposedDropdownMenu` |
| Date and time | `DatePicker`, `TimePicker`, and the Material date picker dialog |
| Collections | `RecyclerView`, `ListView`, `ViewPager2` (pages as items) |
| Containers | `ScrollView`, `NestedScrollView`, `SwipeRefreshLayout`, `CardView`, `ViewGroup` |
| Navigation | `Toolbar` (title, navigation button, menu items, overflow), `TabLayout`, `BottomNavigationView`, `NavigationView` with its `DrawerLayout` |
| Dialogs and feedback | `AlertDialog`, `BottomSheet`, `PermissionDialog`, `Snackbar` (read and its action), `Toast` (read only, best effort, see section 6) |
| Display | `ImageView`, `WebView` (context switch to the web view is a member, not a second driver) |

The MAUI-named classes go. There are no users to keep them for.

**Locators.** `NativeAndroidLocator` stays: resource id first (with or without the package prefix),
content description second, text last. `ByAndroidUIAutomator` becomes a locator strategy, so a
`UiScrollable` query can find a row that is not on screen yet.

## 6. Input, gestures and what Android allows

AD-005 keeps MAUI on Windows free of physical input and says a test of real input belongs on the
Android head. UiAutomator2 drives the device through the accessibility framework and the input
system the user's touches go through; that is the route, and nothing is held back on Android.
Still, as on the other stacks:

- **No coordinates in the public API.** `Swipe(startX, startY, endX, endY)` and `Hover` go.
  Gestures are named and element-relative: `Swipe(direction)`, `Scroll(direction)`, `LongPress`,
  `DoubleTap`, `PullToRefresh`, run as `mobile: swipeGesture` and friends with the element id.
- **No gesture bridge.** AD-008 exists because Windows UI Automation cannot express gestures.
  UiAutomator2 can, so there is nothing to bridge. An action Android cannot express through
  UiAutomator2 raises `RouteUnavailableException` naming the action.
- **Text input** keeps `TextInputMethod`: `Keys` types, `SetValue` sets the value without typing.
  A test about typing behaviour types.
- **Toasts** are not in the view hierarchy on recent Android versions. `Toast` reads one only
  where UiAutomator2 can see it, and says so when it cannot. No test may depend on one.

## 7. Collections

As MAUI and Html: `CollectionObjectBase<TParent, TSelf, TItem>`, `ItemObjectBase`, waiting
`Item(index)` and `SelectItem`, answering `TryItem` and `TrySelectItem` (R7).

`RecyclerView` recycles: the view at position 3 is rebound to another row when the list
scrolls. So:

- **`ItemKey`** comes from the row, in this order: a child the item declares as its key (a
  resource id), the row's content description, the text of its first `TextView`. Position is the
  last fallback, and a key from position alone logs a near-miss warning when the list scrolls.
- A row whose key changed between resolve and act is **stale**: the call re-finds the row by its
  key, never acts on whichever row now sits at that position.
- **Materialize by scrolling**: `MaterializeAttempts` scrolls with `mobile: scrollGesture` on the
  list until the key appears or the end is reached (no more movement), the same budget rules as
  MAUI's `ScrollHelper`.
- `ViewPager2` is a collection of pages; `Spinner` and `ExposedDropdownMenu` open a popup whose
  rows are a collection.

## 8. The app to test against, and the suite

There is no native Android app in the repo (**?N2**). The proposal:

- **`samples/Bruinet.Samples.NativeAndroid.App`**: a .NET for Android app (`net10.0-android`) using
  AndroidX and Material views, no MAUI. It builds with the same SDK, workload and commands as the
  MAUI samples' Android heads, so no Gradle or JDK toolchain joins the repo. A control gallery:
  one screen per family in section 5, a login screen with a button that can be made to stay
  disabled (the R0 proof), a long `RecyclerView` with stable ids and one without, a busy state, a
  permission request, and dialogs.
- **`tests/Bruinet.NativeAndroid.UITests`**: one test class per family, through page objects
  and controls only (the "Never in tests" list in AGENTS.md applies), an Appium fixture that
  installs and starts the sample on the emulator, and the platform's UAT suite inside it
  (`uat.config.md`, `Features/`, `Uat/`, AD-026). UI tests stay local, like every `*.UITests`
  project.
- **Later, optional (plan step T2):** the Todo app as a native head. It would prove AD-014 on a
  third head, the same `.feature.md` files run against native page objects. It is a second app to
  keep in step, so it waits until the gallery suite is green.

**UAT wiring.** `NativeAndroidTestFixtureBase` registers in `UatTargetConventions` as target
`NATIVEANDROID`. The head tag is a decision (**?N5**): `android` is already a platform tag
(MAUI on Android), so the head needs a name of its own, proposed `native`. It only matters once
one app has a native head beside another head.

**Recording.** The fixture is `IRecordable` with `RecordingTarget.AndroidDevice`, so the existing
`AndroidRecorder` (adb screenrecord) records native scenes as it records MAUI's Android ones.

## 9. Shared Appium code

`Bruinet.Maui.Appium` already has what NativeAndroid needs and lacks: `AppiumErrors` (internal),
the `Live` pattern, `AndroidHost` (emulator address, `adb reverse`), `AndroidRecorder`, and the
Android scroll-into-view and picker code. `Bruinet.Maui.Appium` references `Bruinet.Maui`, so
NativeAndroid cannot reference it without taking MAUI in (AD-026 R3 rules that out).

Options (**?N1**):

| | What | For | Against |
| --- | --- | --- | --- |
| A | **A `Bruinet.Appium` project**: `AppiumErrors`, `AndroidHost`, `AndroidRecorder` (built in step 1; the locator mapping stayed per stack, since MAUI's maps MAUI's AutomationId). `Bruinet.Maui.Appium` and `Bruinet.NativeAndroid` both reference it | One copy. AD-026 R4: a layer two stacks build on without each other, and R3: Appium stays out of everything else | One project more; `AndroidRecorder` moves assembly, so the `recorders` folder discovery and `[assembly: ScreenRecorder]` move with it |
| B | Copy the pieces into NativeAndroid | No new project | Two copies of the error mapping and the recorder, which is what AD-010 forbids for the calls layer |
| C | NativeAndroid references `Bruinet.Maui.Appium` | Nothing to move | A native suite gets MAUI. Ruled out by AD-026 R3 |

Proposed: **A**, and the extraction is its own step (plan step 1) with `Bruinet.Maui.Appium`'s
tests and the MAUI Android baseline unchanged, before NativeAndroid uses any of it.

## 10. Decisions

Taken as proposed on 2026-10-07, when the owner said to implement the plan. A step that finds
one wrong says so in its step note and here.

| # | Question | Decided |
| --- | --- | --- |
| **?N1** | Where the shared Appium code lives | A `Bruinet.Appium` project (section 9, option A) |
| **?N2** | What the test app is | A .NET for Android gallery app in `samples/` (section 8); a Kotlin app is closer to `exact-bouw-android` but adds a Gradle toolchain; the Todo native head later |
| **?N3** | The busy convention for native apps | A `bruinet_busy` view (content description or resource id), plus per-page declared progress indicators (section 4) |
| **?N4** | Control names | Android's widget names; MAUI-named classes removed (section 5) |
| **?N5** | UAT target and head tag | Target `NATIVEANDROID`; head `native`, added to `BddTagConventions.Heads` when the first two-head app needs it |
| **?N6** | Skills | Generalize `maui-control` and `maui-ui-test` with a NativeAndroid reference page each, or add `android-control` and `android-ui-test` as Html did. Proposed: two new skills, so each stack's skill names only its own types |
| **?N7** | Own interfaces now, or wait for `.my/core/` L2 | Now, as Html did. Core step 6 then becomes the same mechanical move as MAUI's step 4; `.my/core/migration.md` gets a note |
| **?N8** | Jetpack Compose | Later (plan step C1). `exact-bouw-android` has 280 layout files and 6 composable files; Compose needs `testTagsAsResourceId` in the app and its own readiness (`waitForIdle`) |
| **?N9** | What CI runs | `Bruinet.NativeAndroid.Tests` joins CI's unit test list now. The UI tests stay local: they need an emulator |

## 11. What building it changed (2026-10-07)

Measured on the emulator while building; where this differs from sections 3-9, this wins.

- **Busy signal (section 4):** the resource id `bruinet_busy` only. A content-description or
  XPath lookup on every attempt of every call cost too much; each Appium round trip is 100-200 ms.
- **App in front (section 4):** `getCurrentPackage`, not `queryAppState` (an adb shell call,
  about 470 ms). `IsAppRunning` keeps `queryAppState` for when it is needed.
- **Session settings (section 3):** `enableMultiWindows` on (popups are windows without focus);
  `waitForIdleTimeout` 500 ms, not 10 s (an animating screen is never idle; a `UiScrollable`
  sweep hung) and not 0 (a sweep then overshot its item); `UiScrollable` sweeps bounded by
  `setMaxSearchSwipes(15)`.
- **XPath under a view (section 5):** rooted at the view; `.` and `./X` are rewritten to `/*`
  and `/*/X`.
- **Confirming after a popup (section 5):** the control that opened it is read again by its
  locator, since Android re-issues its node when the popup's window closes.
- **Rating bar (section 6):** set by `TapAt`, a quarter step inside the rating; Android's
  set-progress action moves it without telling the app. Its value is progress times the step.
- **Blur (section 6):** hides the keyboard and confirms it is hidden; Android has no route to clear
  a view's focus.
- **Keyed items (section 7):** found with `UiScrollable.scrollIntoView` on the list first (to its
  start, then forward), then by hand-scrolling.
- **Catalog (section 5):** built: `Button`, `ImageButton`, `FloatingActionButton`,
  `TextView`, `EditText`, `AutoCompleteTextView`, `CheckBox`, `Switch`, `RadioButton`,
  `ToggleButton`, `Chip`, `SeekBar`, `RatingBar`, `ProgressBar`, `Spinner`, `RadioGroup`,
  `TabLayout`, `BottomNavigationView`, `ScrollView`, `SwipeRefreshLayout`, `Toolbar`,
  `AlertDialog`, `PermissionDialog`, `Snackbar`, `RecyclerView`, `ListView`, `ImageView`.
  Not built: `TextInputLayout`, `SearchView`, `ExposedDropdownMenu`, `DatePicker`,
  `TimePicker`, `ViewPager2`, `NestedScrollView` beyond `ScrollView`, `CardView`,
  `NavigationView`, `BottomSheet`, `Toast`, `WebView`.
- **UAT (section 8):** UAT discovery found a control only by its name's suffix; it now also takes
  any type with Core's `IElementObject<>` and skips page types. The suite uses the UI tests' pages.
