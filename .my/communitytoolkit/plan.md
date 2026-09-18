# CommunityToolkit.Maui control objects - plan

Goal: give `Brinell.Maui.CommunityToolkit` a ControlObject for every CommunityToolkit.Maui
view a test can meaningfully drive, in the generatable `.tpl.cs` / `.gen.cs` format, each
proven by UI tests against the sample app on Windows (and Android where the baseline allows).

The work is done by the `control-source-builder` agent ([agent.md](agent.md)), one control
per run. This file is the agent's input for the CommunityToolkit source; a later source
(another vendor or toolkit) gets its own `.my/<source>/plan.md` in the same shape.

## Current state (2026-09-17)

| Item | State |
| --- | --- |
| `srcnew/Brinell.Maui.CommunityToolkit` | Empty shell: a csproj, no controls. `TabViewControl` was deleted in `12d165d`. |
| csproj | References `CommunityToolkit.Maui` **9.0.0** (net8) and turns off central package management. Not in `srcnew/Brinell.sln`. |
| `Brinell.Maui/Controls/Media/MediaElement.tpl.cs` | Empty stub: constructors only. MediaElement is a toolkit control. |
| Sample app | Calls `UseMauiCommunityToolkit()`, but no page uses a toolkit control. |
| `Brinell.Maui.UITests` | Already references the CommunityToolkit project; no toolkit tests. |
| `tools/Scripts/CreateMaui.Bat` | Generates only under `srcnew/Brinell.Maui/Controls`. |
| Local NuGet cache | Toolkit 15.0.1 targets `net10.0-*`: this is the version to use. |

`TabViewControl` wrapped a `toolkit:TabView`. CommunityToolkit.Maui has no TabView (that was
the Xamarin toolkit), so it is not coming back.

## Control inventory (CommunityToolkit.Maui 15.0.1)

The "likely route" column is a guess. Phase 1 replaces it with what the automation tree
actually shows.

| # | Toolkit type | Brinell control | Base / interface | Likely route | Priority |
| --- | --- | --- | --- | --- | --- |
| 1 | `Views.Expander` | `Expander<TScope>` | `ViewBase` | **Probed:** no pattern. App sink `Tap`, `GetState(IsExpanded)` | P1 |
| 2 | `Views.AvatarView` | `AvatarView<TScope>` | `ViewBase` | **Probed:** tree (child text / image) | P1 |
| 3 | `Views.RatingView` | `RatingView<TScope>` | `RangeControlBase`, `IRangeControlObject` | **Probed:** no RangeValue. App sink `SelectIndex`, `GetState(Rating, MaximumRating, IsReadOnly)` | P1 |
| 4 | `Views.Popup` / `PopupPage` | `Popup<TParent>` | `ContainerObjectBase`, root found from the app | **Probed:** tree, modal page in the main window; buttons Invoke | P1 |
| 5 | `Alerts.Snackbar` | none | - | **Probed:** throws on Windows (unpackaged); OS notification. Skipped | P2 |
| 6 | `Alerts.Toast` | none | - | **Probed:** `COMException` on Windows (unpackaged). Skipped | P2 |
| 7 | `Views.DrawingView` | `DrawingView<TScope>` | `ViewBase` | **Probed:** not in tree by id; declared element. App sink `Pan`, `GetState(LineCount)` | P2 |
| 8 | `Views.MediaElement` (separate package `CommunityToolkit.Maui.MediaElement`) | `MediaElement<TScope>`, moved from `Brinell.Maui` | `ViewBase` | Transport controls through Invoke; state through bridge `GetState` | P2 |
| 9 | `Layouts.StateContainer` / `StateView` | `StateContainer<TScope>` | container | `GetCurrentState` through bridge `GetState`; wait for a state | P2 |
| 10 | `Layouts.DockLayout`, `Layouts.UniformItemsLayout` | none, or a thin container | `ContentView`-style container | Plain containers. Only add them if a scoped container adds value | P3 |
| 11 | `Views.LazyView`, `Views.SemanticOrderView` | none | - | Show nothing of their own. Cover with one "content loads" / "focus order" test at most | P3 |
| - | `CommunityToolkit.Maui.Camera` `CameraView`, `.Maps` `Map` | out of scope | - | Needs hardware, or is Windows-only with keys | - |
| - | Behaviors, converters, Essentials pickers (`FileSaver`, `FolderPicker`, `SpeechToText`) | out of scope | - | Not views; the OS dialogs are outside the app's tree | - |

## Phase 0 - groundwork (one run, no controls)

1. **Toolkit version.** Move `CommunityToolkit.Maui` to 15.0.1 in `Directory.Packages.props`
   and `srcnew/Directory.Packages.props`. The sample app picks it up centrally. Add
   `CommunityToolkit.Maui.MediaElement` only when control 8 starts.
2. **Control library does not need the toolkit.** Control objects wrap automation elements,
   not MAUI types; `Brinell.Maui` itself has no MAUI reference. Drop the `PackageReference`
   and `ManagePackageVersionsCentrally=false` from `Brinell.Maui.CommunityToolkit.csproj` so
   the driver-side library stays MAUI-free. If a build proves a type is needed, keep it,
   centrally versioned.
3. **Solution.** Add the project to `srcnew/Brinell.sln`.
4. **Generator.** Change `CreateMaui.Bat` so `INPUT` is
   `srcnew\Brinell.Maui\Controls;srcnew\Brinell.Maui.CommunityToolkit\Controls` (`--input`
   accepts a `;` list).
5. **Layout.**
   - Controls: `srcnew/Brinell.Maui.CommunityToolkit/Controls/<Family>/<Name>.tpl.cs`,
     namespace `Brinell.Maui.CommunityToolkit.Controls.<Family>`. Families: `Views`,
     `Alerts`, `Layouts`, `Media`.
   - Sample: `samples/Brinell.Samples.Maui.App/Views/CommunityToolkitView.xaml` plus a
     `Pages/CommunityToolkitPage.xaml` host, reachable from the hub and registered as a
     `SamplePage` entry. Split into more than one page once it holds more than about 6
     controls; the page must stay fast to load.
   - Tests: `testsnew/Brinell.Maui.UITests/Pages/CommunityToolkitTestPage.cs` and
     `Tests/CommunityToolkit/<Name>Tests.cs`, with `[Trait("Control", "<Name>")]`.
6. **Verify:** the solution builds, the generator runs clean, and
   `--filter "FullyQualifiedName~AutomationProbeTests"` still passes.

## Phase 1 - probe (one run, no controls)

Place **every** P1-P2 control on the sample page with an `AutomationId` and a status or value
label next to it, and no bridge verbs yet. Then record, per control, what the automation
tree shows:

- Windows (FlaUI): the element that carries the AutomationId, its ControlType, its supported
  patterns, and its children.
- Android (Appium): resource-id / content-desc, class, and children.

Write the result to `.my/communitytoolkit/probe.md`, one section per control, and update the
"likely route" column above. This is where the plan will change most: it decides for each
control whether a real pattern exists (preferred) or whether a bridge verb is justified under
the three AD-008 tests.

## Phase 2 - one control per run

In priority order. Each run follows the agent's control loop:

1. **Sample.** Finish the control's section in the sample XAML: add the `GestureAutomation.Verbs`
   that the probe justified, plus a visible label echoing the state the tests will assert.
2. **Bridge.** If a new verb is needed, add it in `Brinell.Maui.AppSupport` and classify it
   (read, element action, or app action) so `ContractTests` passes. Reuse existing verbs
   (`GetState`, `SetValue`-style, `Tap`, `Pan`) first.
3. **Page object.** Add the property to `CommunityToolkitTestPage`.
4. **Tests first.** Exists/visible, each semantic action, each state read, and the
   wait/assert variants the generator emits. Tests use user intent only: no raw driver calls,
   no sleeps.
5. **Control.** Write `<Name>.tpl.cs`: constructors, `protected virtual *Core` methods with
   the element first, `Ensure*` guards inside the Core bodies. Follow `.claude/skills/convert-control`.
6. **Generate and build** the CommunityToolkit project and the UI test project. Rebuild the
   sample app, because `samples/` changed.
7. **Run** tier 1 only: `--filter "Control=<Name>"`. If a new bridge verb was added, also run
   tier 2b (`Stage=Background`).
8. **Android** (when an emulator is available): run the same filter on
   `Brinell.Maui.UITests.Mobile`. Compare with the Android baselines before calling a failure
   a regression.
9. **Record** in the "Progress" table below: the public API, the route used per member,
   what is hand-written and why, and what was skipped per platform.

**Done for a control** means: generated API compiles; tier-1 tests are green on Windows;
Android is green or has a documented reason; no physical input; no coordinates; no new sleeps.

## Phase 3 - close-out

- Move `MediaElement` out of `Brinell.Maui` if control 8 landed. Otherwise delete the empty
  stub or leave it, but decide which.
- Tier 3: the full `Brinell.Maui.UITests` suite, one process, nothing else running.
- Add `docs/controls/community-toolkit.md` (inventory, routes, platform gaps) and link it from
  `docs/controls/index.md` and `docs/README.md`.
- Add a CHANGELOG entry.

## Risks

- **The toolkit renders templated views.** Expander, AvatarView and RatingView are built from
  plain MAUI children, so the AutomationId may land on a wrapper with no patterns. Phase 1
  exists to find this out before any control code is written.
- **Bridge creep.** Every toolkit control "needs" a verb if you don't look for a pattern.
  AD-008 test 1 comes first, every time.
- **Popups and Toasts live outside the page.** A popup is a separate window or overlay; a
  Windows Toast is a system notification. Scoping may need the context or app element rather
  than the page.
- **Test environment.** Only one UI test process at a time. The Android Range and Picker
  baselines fail before any change. `OccludedScreenshotTests` fails when other topmost
  windows are open.

## Progress

Windows results are tier 1 (`--filter "Control=<Name>"`) on 2026-09-17. Android: no emulator or
device was attached for any run, so every Android cell is "not run".

| # | Control | Phase | Windows | Android | Notes |
| --- | --- | --- | --- | --- | --- |
| 0 | groundwork | done | build 0 errors; generator clean; AutomationProbe 3/3 | not run | Toolkit 15.0.1 needs `Microsoft.Maui.Controls` 10.0.90: raised in both props files. csproj slimmed, in sln, generator input extended. Page split in three: views, alerts, media. |
| - | probe | done | see probe.md | not run | Windows only. Three controls need the app sink, the rest are tree-only. |
| 1 | Expander | done | 7/7 | not run | Tap + GetState through `ToolkitVerbSink` (sample app, not AppSupport: AppSupport has no toolkit reference). AD-008: blocked / tap not under test / state visible. `IsExpandedCore` null on Android. |
| 2 | AvatarView | done | 3/3 | not run | Tree only. `GetText` (Equals, Empty), `IsShowingImage`. |
| 3 | RatingView | done | 6/6 | not run | SelectIndex = tap star i (existing verb, sink-owned). SetValue refuses 0 and fractions: no star to tap. Increment/Decrement inherited. |
| 4 | Popup | done | 3/3 | not run | Tree only. Root found from `AppElement`, not cached. `CloseWith(buttonId)` generated. |
| 5 | Snackbar | skipped | - | not run | Throws on Windows (unpackaged app). No control. |
| 6 | Toast | skipped | - | not run | `COMException` on Windows; crashed the app until the sample caught it. No control. |
| 7 | DrawingView | done | 4/4 | not run | Resolved through `TryFindDeclared`. Pan arranges one line through `IDrawingView.OnDrawingLineCompleted`, so the page's event fires. Real strokes: Android only. |
| 8 | MediaElement | done | 7/7 | not run | Stub removed from `Brinell.Maui`. Transport controls: Invoke, RangeValue. A GetState sink was tried and removed because the toolkit's `CurrentState` stays `Opening`. Hand-written `WaitProgressPasses`, `WaitDurationKnown` (the generator only does equality). English "Pause" name. First full-suite run failed Play once (pressed before the media opened); Play now waits for a duration first, and the second full run was green. |
| 9 | StateContainer | done | 3/3 | not run | Tree only. Hand-written `IsShowing/WaitShowing/AssertShowing(id)`: Is* generation forwards no extra parameters. |
| - | closeout | done | full suite 328 passed, 0 failed, 1 skipped (opt-in stress) | not run | Docs `docs/controls/community-toolkit.md`, linked from the index and README. CHANGELOG entry. |
