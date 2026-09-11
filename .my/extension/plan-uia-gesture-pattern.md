---
title: Plan — MAUI Gestures Over UI Automation
description: Phased implementation plan for the Brinell gesture pattern bridge
status: plan
---

# Plan — MAUI Gestures Over UI Automation

Step-by-step delivery of the design in
[design-uia-gesture-pattern.md](design-uia-gesture-pattern.md).

Each phase has an **exit criterion** that is a passing command, not a judgement call. No
phase starts before the previous one's exit criterion is met.

## Shape of the work

```
                                            SAMPLE APP            TESTS
Phase 0  Spikes       throwaway    ----      scratch only          probe only
Phase 1  Contracts    1 project    ----      none                  Brinell.Uia.Tests
Phase 2  Skeleton     1 gesture    ----      existing TestSwipeView Tests/Gestures + guard
Phase 3  Registry     N elements   ----      + TestRefreshView      2 elements, lifetime
Phase 4  Vocabulary   7 gestures   ----      NEW Gestures page      1 test per gesture
Phase 5  Test API     ladder       ----      probe counter          3 configurations
Phase 6  Controls     Brinell      ----      probe row              via control objects
Phase 7  Hardening    ship         ----      Release build          gating + a11y audit
```

Phases 0–2 are the risky half. If Phase 0 fails, Phase 1 onward is wasted, which is why it
is separated and time-boxed.

---

## Sample app and test surface

The sample app is not a demo of this feature — it is **the app under test**, and therefore the
provider half of every integration test. A gesture cannot be tested without an instrumented
app, so the sample-app work is load-bearing, not decoration. This section is the single
reference for what gets added where; the phases below refer back to it.

### Two surfaces, deliberately split

**Reuse `ContainerView` for Phases 2–3.** The markup already exists and already has an
observable outcome:

```xml
<!-- samples/Brinell.Samples.Maui.App/Views/ContainerView.xaml, today -->
<SwipeView AutomationId="TestSwipeView">
    <SwipeView.LeftItems>
        <SwipeItems>
            <SwipeItem AutomationId="SwipeDeleteItem" Text="Delete"
                       Command="{Binding RecordCommand}" CommandParameter="Swipe/Delete" />
        </SwipeItems>
    </SwipeView.LeftItems>
    ...
</SwipeView>
```

`RecordCommand` sets `Status`, which renders into `ContainerStatusLabel` — already modelled by
`ContainerTestPage.Status`. So the walking skeleton needs **one attached property in existing
markup and no new page, no new view model, no new page object**. That is the smallest possible
provider fixture for the riskiest phase, and it retires a documented Windows gap on the way.

**Add a `Gestures` page for Phase 4.** The full vocabulary needs recognizer types the sample
app has nowhere — `SwipeGestureRecognizer`, a double-tap `TapGestureRecognizer`, a long-press
target, and an `IBrinellGestureSink` example. That is a new page, added the way this app adds
pages: one entry in the registry.

### App-side files

| File | Change | Phase |
|---|---|---|
| `samples/Brinell.Maui.AppSupport/Uia/**` | The provider: bridge, fragment root, target provider, dispatcher, registry, attached property, sink | 2–4 |
| `samples/Brinell.Maui.AppSupport/BrinellAutomationSupport.cs` | Add `UseBrinellGestureBridge()` beside the existing `AddBrinellAutomationHandlers()` | 2 |
| `samples/Brinell.Samples.Maui.App/MauiProgram.cs` | Call `UseBrinellGestureBridge()` | 2 |
| `samples/Brinell.Samples.Maui.App/Views/ContainerView.xaml` | `GestureAutomation.Gestures` on the existing `TestSwipeView` and `TestRefreshView`; **update the file header comment** | 2–3 |
| `samples/Brinell.Samples.Maui.App/Navigation/SamplePage.cs` | Add `Gestures` to the enum | 4 |
| `samples/Brinell.Samples.Maui.App/Navigation/SamplePages.cs` | One `SamplePageEntry` — the hub builds itself from `All`, so no other navigation wiring | 4 |
| `samples/Brinell.Samples.Maui.App/Pages/GesturesPage.xaml{,.cs}` | ContentPage shell, matching the other pages | 4 |
| `samples/Brinell.Samples.Maui.App/Views/GesturesView.xaml{,.cs}` | The recognizer matrix | 4 |
| `samples/Brinell.Samples.Maui.App/ViewModels/GesturesViewModel.cs` | Records each gesture into an observable status label | 4 |

### Test-side files

| File | Change | Phase |
|---|---|---|
| `testsnew/Brinell.Maui.UITests/Tests/Gestures/**` | The new test area — one tier-1 filter | 2–5 |
| `testsnew/Brinell.Maui.UITests/Pages/ContainerTestPage.cs` | Gesture members on the existing page object; **update the class remarks** | 2–3 |
| `testsnew/Brinell.Maui.UITests/Pages/HubPage.cs` | Add `Gestures` to the mirrored `SamplePage` enum | 4 |
| `testsnew/Brinell.Maui.UITests/Pages/GesturesTestPage.cs` | Page object for the new page | 4 |
| `testsnew/Brinell.Uia.Tests/**` | Contract unit tests, no UI | 1 |
| `testsnew/Brinell.Maui.Tests/**` | Dispatcher ladder unit tests, no UI | 4 |

### Constraint: the mobile head links these sources

`Brinell.Maui.UITests.Mobile` compiles **every `.cs` file** from `Brinell.Maui.UITests`:

```xml
<Compile Include="..\Brinell.Maui.UITests\**\*.cs" Link="..." />
```

It targets plain `net10.0` and references **Appium, not FlaUI**. Today not a single shared test
file mentions FlaUI — the whole suite is written against `IMauiElement` and control objects,
which is what makes "one test, three platforms" true.

Two consequences, and they are firm:

1. **Gesture tests go through control objects, never through `BrinellGestureExtensions`.**
   Those extensions live in `Brinell.Maui.FlaUI` and are invisible to the mobile head; a test
   that calls them directly breaks the Android build, not just its own assertion. This is
   `AD-003` restated with teeth: the pattern is a Windows *route*, not a Windows *API surface*.
2. **A test that genuinely needs FlaUI types must be excluded by name.** The raw-view
   tree-integrity assertion is the only one foreseen. Add it to an `Exclude` in the mobile
   csproj with a comment saying why — a deliberate, visible break in the shared-source
   property rather than a silent one.

Phase 5 exists partly to satisfy this: `SupportedGestures()` and the invoke path need a
platform-neutral surface on `IMauiElement` so shared tests can use them on all three platforms,
with Windows going through the pattern and Android through Appium's own gesture APIs.

### Two comments that will become false

Both currently assert the opposite of what this work makes true. Leaving them is worse than
never having written them, because they are exactly the files a future reader consults:

- `ContainerView.xaml` header: *"SwipeView and RefreshView … Their markup is here anyway
  because the app is planned to run on Android and iOS, where … the gesture scenarios in the
  design become testable."* After Phase 3, they are testable on Windows.
- `ContainerTestPage` remarks: *"`SwipeView`, and `RefreshView` … expose no `AutomationId`
  here, so they are reached through `TryFindByAutomationId` and reported by the probe rather
  than modelled as containers that would never resolve."* After Phase 3, they resolve.

`MauiElementGestureExtensions` carries a third (*"this logic has never run in a passing test"*)
— handled in Phase 5.

### Out of scope

`Brinell.Samples.Maui.ShellApp` is **not** instrumented. One app under test is enough to prove
the design, and the Shell app exists to cover navigation, not gestures. Revisit only if a
gesture scenario turns out to need Shell chrome.

---

## Phase 0 — Spikes (time-boxed, throwaway code)

**Purpose.** Three unknowns can each kill the design. Answer them before writing anything
that is meant to last. Code from this phase is deleted, not merged.

Work in a scratch console app plus the sample MAUI app on a throwaway branch.

### S1 — Can a child HWND of the MAUI window carry a raw provider without disturbing the tree?

The one that could kill the design. WinUI 3 hosts its content in its own child HWND
(`Microsoft.UI.Content.DesktopChildSiteBridge`), and it is not established that adding a
sibling child window leaves the XAML island's UIA tree intact — §0.2 is a standing reminder
that this app's UIA tree is more fragile than it looks.

- Create a 1x1 `WS_CHILD` window parented to the MAUI window's HWND.
- Answer `WM_GETOBJECT` (`lParam == UiaRootObjectId`) with `UiaReturnRawElementProvider`
  returning a minimal `IRawElementProviderSimple` with a distinctive `ClassName`.
- **Pass:** Inspect.exe and a FlaUI probe both find the provider under the app window, **and**
  the existing `AutomationProbeTests` still pass unchanged.
- **Fail:** try `WS_CHILD` without `WS_VISIBLE`; try parenting to the desktop with an owner
  relationship; if neither works, fall back to D-6 (the `ValuePattern` command channel) and
  rewrite the design around it.

### S2 — Does `RegisterPattern` round-trip a call across processes with hand-written interop?

- Hand-declare `IUIAutomationRegistrar`, `UIAutomationPatternInfo`, `UIAutomationMethodInfo`,
  `IUIAutomationPatternHandler`, `IUIAutomationPatternInstance` (verified absent from
  `Interop.UIAutomationClient`, so there is no shortcut).
- Register the same pattern GUID in the sample app and in an xUnit test.
- Test calls `GetCurrentPattern(patternId)`, casts, invokes; app writes to a file.
- **Pass:** the file appears, and the returned pattern ids differ between processes (which
  confirms the ids are process-local and the GUID is the contract).
- **Fail:** most likely a struct-layout or BSTR-marshalling error. Compare against the
  `UIAutomationCore.idl` definitions field by field before changing the design.

### S3 — Are raw-view-only elements findable by FlaUI's default conditions?

- Set `IsControlElement = false` and `IsContentElement = false` on the S1 provider.
- **Pass:** `FindFirstDescendant(cf => cf.ByClassName(...))` still finds it, and it is absent
  from a control-view walk.
- **Fail:** D-5 falls back to `IsControlElement = true` with `ControlType = Custom` and a
  scaffolding-looking `Name`. Record it in the design's accessibility section as a known cost.

### Exit criteria

All three green, or a written amendment to the design saying what changed. Record the answers
in this file under a "Spike results" heading — S1's answer in particular is the kind of thing
someone will otherwise spend a day rediscovering, exactly as `AutomationRemainingHandlers.cs`
was written to prevent.

**Verify:** `dotnet test testsnew\Brinell.Maui.UITests --filter "FullyQualifiedName~AutomationProbeTests" -v:minimal /nr:false`

---

## Phase 1 — Contracts

**Deliverable.** `srcnew/Brinell.Uia.Contracts/`, a small `net10.0` project with no MAUI and
no FlaUI dependency, plus its unit tests.

**Files**

| File | Contents |
|---|---|
| `BrinellGestureIds.cs` | Pattern and property GUIDs, class names, `ProtocolVersion` |
| `GestureKind.cs` | The vocabulary enum and `GestureMask` |
| `IBrinellGestureProvider.cs` / `IBrinellGesturePattern.cs` | The two COM interfaces |
| `Interop/UIAutomationRegistrar.cs` | Hand-declared registrar interop from S2 |
| `Interop/NativeTable.cs` | `IDisposable` helpers that marshal the method and property tables |
| `BrinellGesturePatternRegistration.cs` | Idempotent per-process registration |
| `BrinellGesturePatternHandler.cs` | `CreateClientWrapper` + `Dispatch` |
| `BrinellGestureClientWrapper.cs` | The typed client object |
| `HResults.cs` | `S_OK`, `E_INVALIDARG`, `UIA_E_*` |
| `GestureFailure.cs` | The client-facing failure enum |

**Distribution decision.** `Brinell.Maui.AppSupport`'s csproj carries a deliberate rule:
*"DELIBERATELY NO ProjectReference TO ANYTHING IN THIS REPO — this project is designed to be
copied, not only referenced."* Honour it. Ship the contract as `Contracts/*.cs` files
**linked** into both `Brinell.Uia.Contracts` and `Brinell.Maui.AppSupport` via
`<Compile Include="..\..\srcnew\Brinell.Uia.Contracts\**\*.cs" Link="..." />`, so an app that
copies AppSupport out gets the contract files with it and the GUIDs cannot drift.

**Tests** — `testsnew/Brinell.Uia.Tests/`:

- `Guids_AreStable` — asserts every GUID literally, with a comment saying that regenerating
  one is a breaking protocol change that surfaces as "app not instrumented".
- `GestureMask_RoundTrips` over every `GestureKind`.
- `MethodTable_MatchesDispatchIndices` — the registration method table and the handler's
  `Dispatch` switch are one contract written twice; assert they agree.
- `Registration_IsIdempotent` — two `Current` reads give the same ids; hammer from parallel
  threads.
- `HResultMapping_IsTotal` — every `GestureFailure` is reachable and no HRESULT falls through
  unclassified.

**Open decision, and the point of no return.** The method table freezes here. Changing it later
means a new pattern GUID and a simultaneous breaking migration of the app under test and the
test assembly. [beyond-gestures-uia-candidates.md](beyond-gestures-uia-candidates.md) argues
that almost every follow-on capability — text input, `ScrollTo`, navigation, state reads —
needs a **string** in or out, which `Invoke(int, int, int)` cannot carry, and recommends
registering a second method now:

```csharp
//   0: Invoke   (int verb, int arg1, int arg2)            — gestures
//   1: Exchange (int verb, string arg, out string result) — everything after
```

It costs one `UIAutomationMethodInfo` entry and one branch in `Dispatch`, and stays unused
until the first such capability is built. Decide before writing the table, not after. If it
goes in, extend **spike S2 to round-trip a string**, not just an int.

**Exit:** `dotnet test testsnew\Brinell.Uia.Tests -v:minimal /nr:false` green;
`dotnet build srcnew\Brinell.sln -v:minimal /nr:false` green.

---

## Phase 2 — Walking skeleton

**Deliverable.** One gesture, one element, end to end, in the real sample app. The narrowest
thing that proves the architecture.

- `BrinellGestureBridge` — HWND creation, `WM_GETOBJECT`, `Dispose` with
  `UiaDisconnectProvider`.
- `BrinellGestureFragmentRoot` — `IRawElementProviderSimple` + `Fragment` + `FragmentRoot`,
  with a **hardcoded single child**.
- `BrinellGestureTargetProvider` — the pattern, `SwipeLeft` only, wired to one named
  `SwipeView` in the sample app.
- `GestureDispatcher` — `SwipeView.Open` only, with the dispatcher marshalling and the timeout
  budget already in place (retrofitting the threading model later is far harder than getting
  it right now).

**Sample app** — no new page, no new view model, no new page object. Reuse what is there:

- `BrinellAutomationSupport.cs`: add `UseBrinellGestureBridge()` next to the existing
  `AddBrinellAutomationHandlers()`, following the same "reference it or copy it" contract the
  file already documents.
- `MauiProgram.cs`: one call.
- `ContainerView.xaml`: one attached property on the **existing** `TestSwipeView`.

  ```xml
  <SwipeView AutomationId="TestSwipeView"
             uia:GestureAutomation.Gestures="SwipeRight">
  ```

  `SwipeRight`, not `SwipeLeft` — the items are in `SwipeView.LeftItems`, and a swipe *right*
  is what reveals them. The inversion is the first thing this phase gets to prove, and getting
  it backwards here would be caught by the assertion below rather than by reading the code.

**Observable outcome, already wired.** `SwipeDeleteItem` runs `RecordCommand` with
`"Swipe/Delete"`, which sets `Status`, which renders into `ContainerStatusLabel` — already
modelled as `ContainerTestPage.Status`. So the assertion is on existing, trusted state, not on
something built alongside the thing under test.

**Tests** — new `testsnew/Brinell.Maui.UITests/Tests/Gestures/`:

- `SwipeRight_OnTestSwipeView_RevealsDeleteItem` — swipe, then click `SwipeDeleteItem`, then
  assert `Status` reads `Swipe/Delete`.
- `Bridge_DoesNotDisturbTheExistingTree` — the §0.2 regression guard. Non-negotiable, and it
  goes in *now*, not in Phase 7. This is the one test that touches FlaUI types, so it is also
  the one that gets the `Exclude` entry in `Brinell.Maui.UITests.Mobile.csproj`, with a comment
  saying why.
- Add `SwipeView` gesture members to `ContainerTestPage` and update its class remarks, which
  currently say these controls never resolve.

**Exit:**
```powershell
dotnet test testsnew\Brinell.Maui.UITests --filter "FullyQualifiedName~Tests.Gestures|FullyQualifiedName~AutomationProbeTests" -v:minimal /nr:false
dotnet build testsnew\Brinell.Maui.UITests.Mobile\Brinell.Maui.UITests.Mobile.csproj -v:minimal /nr:false
```
Both green — including the mobile build, which is how a shared-source break is caught in this
phase rather than in CI three phases later. This is the moment the design is proven; everything
after it is breadth.

---

## Phase 3 — Registry and the attached property

**Deliverable.** Any element can opt in, and children track element lifetime.

- `GestureAutomation` attached property: `Gestures="SwipeLeft,SwipeRight"` (parsed to a mask)
  and `Sink` (an `IBrinellGestureSink`).
- `GestureRegistry` — weak references keyed by `AutomationId`; registers on
  `Loaded`/`HandlerChanged`, unregisters on `Unloaded`.
- Fragment root `Navigate` walks live children; `GetRuntimeId` is stable per target.
- `BoundingRectangle` resolves live screen bounds through the handler's `PlatformView`.
- `UiaRaiseStructureChangedEvent` when children are added or removed, so a client's cached
  tree does not go stale.
- Validation: an element with gestures declared but no `AutomationId` logs a warning naming
  the element type and its parent page — that is the difference between a five-minute fix and
  an hour of confusion.

**Sample app**

- `ContainerView.xaml`: add `GestureAutomation.Gestures="SwipeDown"` to the existing
  `TestRefreshView`, giving a second gesture-enabled element on the same page — which is what
  makes "two elements are independently addressable" testable at all.
- Update the `ContainerView.xaml` file header. It currently says the gesture scenarios only
  become testable on Android and iOS; after this phase they are testable on Windows, and the
  comment is the first thing someone reads when deciding whether to bother.

**Tests**

- Unit: registry add/remove/weak-collection; mask parsing including an unknown token.
- Integration, in `Tests/Gestures/`:
  - `TwoElementsOnOnePage_AreIndependentlyAddressable` — swipe the SwipeView, pull the
    RefreshView, assert each observable outcome (`Status`, `RefreshText`) moved and the other
    did not.
  - `RemovedElement_ReportsElementGone` — navigate away, then invoke; assert the failure names
    `ElementGone` rather than acting on a corpse or timing out.
  - `ElementWithoutAutomationId_LogsAWarning` — the validation path, asserted through the app
    log rather than by inspecting internals.
- `ContainerTestPage`: add `PullToRefresh()` alongside the swipe members from Phase 2.

**Exit:** the Gestures filter green with two distinct elements exercised; the mobile head still
builds.

---

## Phase 4 — Full vocabulary and the dispatcher ladder

**Deliverable.** All six gestures from the requirement — `SwipeLeft`, `SwipeRight`, `SwipeUp`,
`SwipeDown`, `LongPress`, `DoubleTap` — plus `Tap`.

- `MauiCapabilities.cs` — the single file naming every public MAUI API the dispatcher
  calls. A MAUI upgrade that breaks one is a compile error here (D-4, §7.2).
- Ladder in order: sink, then public MAUI API, then `TapGestureRecognizer.Command`, then
  `UIA_E_NOTSUPPORTED`.
- `LongPress` — `arg1` carries duration. Routed through the sink or a
  `PointerGestureRecognizer` (`SendPointerPressed`/`SendPointerReleased`, both public); no
  route for a plain long-press-only element, which is a documented gap, not a silent failure.
- `DoubleTap` — `TapGestureRecognizer` with `NumberOfTapsRequired == 2`, via `Command`.
- Full HRESULT mapping; provider entry points wrapped so nothing throws across COM.

**Sample app — the `Gestures` page.** The full vocabulary needs recognizer types the app has
nowhere. Added the way this app adds pages: one `SamplePageEntry` in `SamplePages.All`, and the
hub builds itself from it — no XAML, route, or navigation wiring.

| File | Contents |
|---|---|
| `Navigation/SamplePage.cs` | `Gestures` appended to the enum |
| `Navigation/SamplePages.cs` | `new(SamplePage.Gestures, "Gestures", "Swipe, long press, double tap", () => new GesturesPage())` |
| `Pages/GesturesPage.xaml{,.cs}` | ContentPage shell, mirroring `ContainerPage` |
| `Views/GesturesView.xaml{,.cs}` | The recognizer matrix below |
| `ViewModels/GesturesViewModel.cs` | `RecordCommand` into a `GestureStatusLabel`, plus a `ResetCommand` — the same shape as `ContainerViewModel`, so the assertions read the same way |

The matrix, one addressable element per row, each declaring only the gestures it actually
supports so `SupportedGestures` has something meaningful to report:

| AutomationId | Carries | Declares | Proves |
|---|---|---|---|
| `GestureSwipeTarget` | `SwipeGestureRecognizer` (`Left,Right`) | `SwipeLeft,SwipeRight` | `SendSwiped`, the public route |
| `GestureDoubleTapTarget` | `TapGestureRecognizer` (`NumberOfTapsRequired=2`, `Command`) | `DoubleTap` | the `Command` route around internal `SendTapped` |
| `GestureLongPressTarget` | `PointerGestureRecognizer` | `LongPress` | `SendPointerPressed`/`Released` with `arg1` as duration |
| `GestureSinkTarget` | an `IBrinellGestureSink` in code-behind | `SwipeUp,SwipeDown` | the escape hatch, and that it beats the built-in route |
| `GestureNoDeclarationTarget` | a `TapGestureRecognizer`, **no** attached property | — | an element the bridge deliberately does not expose |

`GestureSinkTarget` earns its place: the sink is the seam the whole maintainability argument
rests on (D-4), and an untested seam is an assumption. `GestureNoDeclarationTarget` is the
negative case — opting in must be a decision, not a default.

**Tests**

- Unit, `Brinell.Maui.Tests`: the ladder against fakes — sink wins; swipe direction maps to
  the correct **inverted** `OpenSwipeItem`; a `SwipeGestureRecognizer` with a non-matching
  `Direction` is skipped; a `Command` returning `CanExecute == false` is skipped, not executed;
  a blocked dispatcher yields `UIA_E_TIMEOUT` within the budget rather than hanging.
- `testsnew/Brinell.Maui.UITests/Pages/GesturesTestPage.cs` — page object, `Name` =
  `"GesturesPage"`, `IsLoaded` on the title label, one member per row above.
- `Pages/HubPage.cs` — `Gestures` appended to the mirrored `SamplePage` enum. The two enums are
  deliberately duplicated and `AutomationIdFor` is derived on both sides, so a mismatch fails
  visibly; keep it that way.
- Integration, one test per gesture, each asserting `GestureStatusLabel` rather than a screenshot:
  - `SwipeLeft_OnSwipeRecognizer_Records` and the three other directions
  - `DoubleTap_ExecutesCommand`
  - `LongPress_WithDuration_Records`
  - `Sink_TakesPrecedenceOverBuiltInRoute`
  - `UndeclaredElement_ReportsNotSupported` — against `GestureNoDeclarationTarget`
  - `SupportedGestures_MatchesDeclaration` for each row

**Exit:** Gestures filter green; `dotnet test testsnew\Brinell.Maui.Tests -v:minimal /nr:false`
green; the mobile head still builds.

---

## Phase 5 — FlaUI surface and the fallback ladder

**Deliverable.** The test-facing API, and the decision about which route a gesture takes.

- `Brinell.Maui.FlaUI/Gestures/BrinellGestureExtensions.cs` — `HasGestureBridge`,
  `SupportedGestures`, the six named methods, `InvokeGesture`, `TryInvokeGesture`.
- `GestureUnavailableException` — carries element, gesture and `GestureFailure`, and its
  message names the fix.
- `FlaUIMauiElement.Swipe`/`LongPress` become **pattern-first**: try the pattern; on
  `NotInstrumented`, fall back to the existing pointer path if `BRINELL_ALLOW_POINTER_INPUT`
  is set, otherwise throw with the actionable message (§3.3).
- Update the `MauiElementGestureExtensions` remarks. They currently say *"this logic has never
  run in a passing test"* because SwipeView and RefreshView are not addressable on Windows.
  After this phase that is no longer true, and a stale comment on the one file a future reader
  will consult is worse than no comment.

**The platform-neutral surface.** This is the phase that satisfies the shared-source constraint.
`IMauiElement` gains `TryInvokeGesture` / `SupportedGestures`, implemented by
`FlaUIMauiElement` through the pattern and by `AppiumMauiElement` through Appium's own gesture
APIs. Shared tests written in Phase 4 keep compiling for Android because they never named a
FlaUI type; the Windows and mobile heads simply take different routes to the same method.

**Tests**

- Integration: with the bridge, the pointer path is never entered (assert via a provider-side
  counter exposed on the `Gestures` page, not by inspecting internals); without it and with the
  env var set, the pointer path runs; without either, the exception names
  `UseBrinellGestureBridge()`.
- The Phase 4 gesture tests are re-run unchanged against the mobile head where an emulator is
  available. They are not expected to pass yet — Appium gesture routing is its own work — but
  they must **compile and run**, because a shared-source break that only appears on Android is
  the failure mode this project's layout exists to prevent.

**Exit:** Gestures filter green in all three configurations; the pointer-fallback test runs with
`BRINELL_ALLOW_POINTER_INPUT` explicitly set and unset; `Brinell.Maui.UITests.Mobile` builds.

---

## Phase 6 — Control objects and documentation

**Deliverable.** Gestures reachable the way Brinell tests are supposed to reach things — via
control objects, per `AD-003: Controls Own Repeated Interaction Behavior`.

- `SwipeView` control object implements `ISwipeableControlObject<TScope>` on Windows through
  the pattern. That interface already exists in `Brinell.Core` and is described as *"primarily
  used for mobile platforms"* — this phase is what makes it true on Windows too, so update its
  doc comment.
- `RefreshView` control object gets `PullToRefresh()`.
- `GesturesTestPage` and `ContainerTestPage` move from raw gesture calls to the control-object
  members, so the sample tests demonstrate the API a user is meant to write. A test that still
  reaches past the control object after this phase is a sign the control object is missing
  something.
- Sample app: `AutomationProbeView` gains a gesture-bridge row, so the probe page reports
  whether the app it is running in is instrumented. The probe is where someone looks when
  nothing resolves, and "no gesture bridge" belongs in that answer.
- Docs:
  - `docs/architecture/decisions.md` — **AD-008: Gestures Go Through UI Automation**, stating
    that the pattern route is preferred and pointer input remains the gated fallback.
  - `docs/platform-guides/maui.md` — a "Gestures on Windows" section: how to instrument an
    app, how to declare gestures, what the failure messages mean.
  - `docs/README.md` — link any new page.
  - `AGENTS.md` — add the Gestures filter to the tier-1 table.
  - `samples/Brinell.Maui.AppSupport/README` note on copying the contract files out.

**Exit:** `dotnet build srcnew\Brinell.sln`, `Brinell.Core.Tests`, `Brinell.Maui.Tests`, and
the **full** MAUI UI suite. This is a phase boundary and the change touches shared
infrastructure, so tier 3 is warranted here — with the known pre-existing failures
(DatePicker, TimePicker, Image, ProgressBar, Stepper, Switch) established as the baseline
first.

---

## Phase 7 — Hardening

**Deliverable.** Safe to have in the tree, safe to ship around.

- **Security gating (§8).** Wrap the provider in `#if BRINELL_UIA_BRIDGE`; define it only in
  the instrumented configuration. `UseBrinellGestureBridge()` additionally checks
  `BRINELL_UIA_BRIDGE=1` at runtime. Add a test asserting a Release build of the sample app
  exposes no fragment root.
- **Versioning (§7.2).** A test that an unknown `GestureKind` from a newer client yields
  `NotSupportedByElement` rather than a crash or a hang.
- **Lifetime.** Assert `UiaDisconnectProvider` runs on window close; a soak test that opens and
  closes the window repeatedly and checks the provider count does not grow.
- **Accessibility audit (§9).** A diagnostic that lists every gesture-enabled element exposing
  neither a keyboard route nor `InvokePattern`, written to the standard
  `TestResults/<run-id>/suites/<suite>/` artifact layout per `AD-007`. This is the output that
  makes the work an accessibility improvement rather than a way to test around a defect.
- **MAUI-version smoke.** Build `Brinell.Maui.AppSupport` against the newest available
  `Microsoft.Maui.Controls` as well as the pinned 10.0.71, to catch a public API moving.

**Exit:** full MAUI UI suite at baseline; `Brinell.Uia.Tests`, `Brinell.Core.Tests` and
`Brinell.Maui.Tests` green; the Release-build test proves the bridge is absent.

---

## Risk register

| Risk | Phase | Signal | Response |
|---|---|---|---|
| A child HWND disturbs the XAML island's UIA tree | 0 (S1) | `AutomationProbeTests` fail with the bridge attached | Window-style variations, then fall back to D-6 |
| Struct layout or BSTR marshalling wrong in the registrar interop | 0 (S2) | `RegisterPattern` returns `E_INVALIDARG`, or `GetCurrentPattern` returns `null` | Compare field-by-field with `UIAutomationCore.idl`; this is the most likely single source of lost time |
| Raw-view-only elements are not findable by FlaUI | 0 (S3) | `FindFirstDescendant` returns null for a provider Inspect.exe can see | `IsControlElement = true` + `ControlType = Custom`; record the a11y cost |
| Cross-apartment marshalling of the client wrapper misbehaves under xUnit | 2 | Intermittent `InvalidCastException` on the pattern cast | Pin the test collection's apartment; register once per process, never per test |
| Provider hangs the UI thread and every a11y client with it | 2, 4 | The app stops responding to Inspect.exe, not just to tests | The timeout budget is in from Phase 2 by design; never remove it |
| A gesture test names a FlaUI type and breaks the Android build | 2, 4 | `Brinell.Maui.UITests.Mobile` fails to compile; the Windows suite is green | Tests go through control objects (`AD-003`); the one FlaUI-typed test is excluded by name with a comment |
| A new sample page breaks the mobile suite it also feeds | 4 | Android run fails on a page it never opened | The `Gestures` page must render on Android; verify with a mobile smoke run before closing the phase |
| GUIDs regenerated during a refactor | any | Integration tests report "app not instrumented" | `Guids_AreStable` from Phase 1 catches it at unit-test speed |
| MAUI makes a used public API internal | 6, 7 | `MauiCapabilities.cs` fails to compile | The sink absorbs it; that is what it is for |

## Working rules for this piece of work

- **One UI test process at a time.** Two concurrent runs fight over the desktop and produce
  fictional failures.
- **Match the tier to the change** (`AGENTS.md`). The Gestures filter is ~seconds; the full
  suite is minutes and is for phase boundaries only.
- **Establish the baseline before reporting a regression.** The MAUI UI suite has known
  unrelated failures.
- **No arbitrary sleeps** (`AD-004`). The `Invoke` call returns when the gesture is delivered;
  waiting for the UI to settle is a `WaitFor` on concrete state.
- **Record negative results in the file where the next person will look**, the way
  `AutomationRemainingHandlers.cs` did. Phase 0's spike results belong in this document.
