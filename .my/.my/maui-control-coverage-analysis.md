# MAUI Control Objects Coverage Analysis (Verified)

**Date:** 2026-08 (revised)  
**Framework:** .NET 10 MAUI  
**Purpose:** Track three dimensions per control — does a Brinell control object exist, does the sample app use the control, and does a UI test exercise it  
**Source:** [Microsoft Learn - .NET MAUI Controls](https://learn.microsoft.com/en-us/dotnet/maui/user-interface/controls/?view=net-maui-10.0)

---

## Summary

Three independent dimensions, verified against the tree rather than remembered:

| Dimension | Count |
|---|---|
| Distinct MAUI control types with a Brinell control object | **52** |
| ...of those, present in `Brinell.Samples.Maui.App` | **42** |
| ...of those, driven by a **passing** UI test | **22** |
| ...reached only by an addressability probe | 3 more (`Border`, `ContentView`, `ScrollView`) |

**No — the three dimensions do not line up.** 10 controls have no sample markup, and a
further 20 are in the sample but have no passing test. Only 22 of 52 are genuinely
exercised.
| Brinell extension controls (non-standard) | 7 |
| UI tests in `Brinell.Maui.UITests` | 171 (2 skipped) |
| Unit tests in `Brinell.Maui.Tests` | 71 |

The gap that matters is not the control-object count — it is that **half the control
objects are never exercised against a running app**. A control object with no sample
markup cannot be tested, and an untested control object is an assumption.

---

## Coverage matrix

Legend: **Object** = a Brinell control object exists · **Sample** = the control appears in
`Brinell.Samples.Maui.App` XAML · **Tested** = a UI test drives that control object.

A control can be in the sample without being tested (the markup exists but no test targets
it), and cannot be tested without being in the sample.

| Category | Control | Object | Sample | Tested | Notes |
|---|---|---|---|---|---|
| **Buttons** | Button | ✅ | ✅ | ✅ 6 | |
| | ImageButton | ✅ | ❌ | ⚠️ | `ImageButtonTests` (5) drive it through `Button<>`, not `ImageButton<>` |
| **Collection** | CollectionView | ✅ | ✅ | ✅ 15 | via `ProductCollection`; 2 skipped, see below |
| | CarouselView | ✅ | ❌ | ❌ | rollout plan Phase 3 |
| | ListView | ✅ | ❌ | ❌ | rollout plan Phase 3 |
| | IndicatorView | ✅ | ❌ | ❌ | rollout plan Phase 3 |
| | TableView | ✅ | ❌ | ❌ | rollout plan Phase 4 |
| **Container** | Grid | ✅ | ✅ | ✅ 10 | `GridContainerTests` + `AutomationProbeTests` |
| | ContentView | ✅ | ✅ | ✅ | probe only — addressability, not behaviour |
| | ScrollView | ✅ | ✅ | ✅ | probe only |
| | Border | ✅ | ✅ | ✅ | probe only; needs its own automation handler |
| | BoxView | ✅ | ✅ | ❌ | no children by design — nothing to scope |
| | Frame | ✅ | ✅ | ❌ | **not addressable on Windows**; deprecated, use Border |
| | SwipeView | ✅ | ✅ | ❌ | **not addressable**; peer override collapses the UIA tree |
| | RefreshView | ✅ | ✅ | ❌ | **not addressable**; same cause as SwipeView |
| | IsoPaneView | ✅ | ❌ | ❌ | project-specific, provenance unconfirmed |
| **Dialogs** | ContentDialog | ✅ | ❌ | ❌ | 4 unit tests only |
| **DateTime** | DatePicker | ✅ | ✅ | ✅ 9 | |
| | TimePicker | ✅ | ✅ | ✅ 10 | |
| **Display** | Label | ✅ | ✅ | ✅ 3 | most-used control in the sample (113 uses) |
| | Image | ✅ | ✅ | ✅ 2 | |
| | ActivityIndicator | ✅ | ✅ | ✅ 4 | |
| | ProgressBar | ✅ | ✅ | ✅ 6 | |
| | TitleBar | ✅ | ❌ | ❌ | window decoration |
| **Graphics** | GraphicsView | ✅ | ❌ | ❌ | |
| **Media** | WebView | ✅ | ❌ | ❌ | uncovered-areas plan Phase D |
| | MediaElement | ✅ | ❌ | ❌ | needs `CommunityToolkit.Maui.MediaElement` |
| | HybridWebView | ✅ | ❌ | ❌ | needs a hybrid host |
| | BlazorWebView | ✅ | ❌ | ❌ | see `Brinell.Samples.Blazor.App` |
| **Navigation** | Toolbar | ✅ | ✅ | ✅ 6 | |
| | Menu | ✅ | ✅ | ✅ 4 | |
| | TabMenu | ✅ | ✅ | ✅ 5 | Brinell composite; sample supplies its contract markup |
| | ShellContent | ✅ | ✅ | ✅ | drives every fixture navigation |
| | Shell | ✅ | ✅ | ⚠️ | implicit only |
| | Tab | ✅ | ❌ | ❌ | |
| | FlyoutItem | ✅ | ❌ | ❌ | sample sets `FlyoutBehavior="Disabled"` |
| **Range** | Slider | ✅ | ✅ | ✅ 9 | |
| | Stepper | ✅ | ✅ | ✅ 13 | |
| **Selection** | Picker | ✅ | ✅ | ✅ 8 | |
| **Shapes** | Ellipse, Line, Path, Polygon, Polyline, Rectangle, RoundRectangle | ✅ ×7 | ❌ | ❌ | no interactive behaviour to assert |
| **Text** | Entry | ✅ | ✅ | ✅ 10 | |
| | Editor | ✅ | ✅ | ✅ 11 | |
| | SearchBar | ✅ | ✅ | ✅ 10 | |
| **Toggle** | CheckBox | ✅ | ✅ | ✅ 4 | |
| | RadioButton | ✅ | ✅ | ✅ 6 | |
| | Switch | ✅ | ✅ | ✅ 6 | |

### Brinell extensions (7)

| Control | Object | Sample | Tested |
|---|---|---|---|
| IconCommandButton | ✅ | ❌ | ❌ |
| Link | ✅ | ❌ | ❌ |
| RoundButton | ✅ | ❌ | ❌ |
| Expander | ✅ | ❌ | ❌ |
| GenericBrowser | ✅ | ❌ | 7 unit |
| SelectionList | ✅ | ❌ | 1 unit |
| EditableField | ✅ | ❌ | 7 unit |

### Container and collection infrastructure

Not MAUI controls, but the substance of recent work:

| Type | Sample | Tested |
|---|---|---|
| `ContainerObjectBase` | ✅ | ✅ 10 UI + 32 unit |
| `CollectionObjectBase` | ✅ | ✅ 15 UI |
| `ItemContainerBase` | ✅ | ✅ (via `ProductRow`) |
| `ScrollHelper` | ✅ | ✅ (via collection + probe) |

---

## The two gaps, explicitly

### 10 controls have no sample markup

| Control | Why |
|---|---|
| `ImageButton` | oversight — `ImageButtonTests` exists but drives `Button<>` instead |
| `ContentDialog` | `DialogsView` was added but raises dialogs from code-behind; no page object yet |
| `TitleBar` | window decoration |
| `Tab`, `FlyoutItem` | Shell surfaces; the sample sets `FlyoutBehavior="Disabled"` |
| `WebView`, `MediaElement`, `HybridWebView`, `BlazorWebView` | Media module not built — see the uncovered-areas plan Phase D |
| `IsoPaneView` | provenance unconfirmed; not a standard MAUI control |

### 20 controls have markup but no passing test

| Group | Controls | Why |
|---|---|---|
| Container module | `Border`, `ContentView`, `ScrollView`, `Grid`* | `ContainerModuleTests` is written but does not pass — a fixture cannot return to the tab root after route navigation |
| Unaddressable on Windows | `Frame`, `SwipeView`, `RefreshView`, `BoxView` | no AutomationPeer; markup exists for the planned Android/iOS phase |
| Collection module | `CarouselView`, `ListView`, `IndicatorView`, `TableView` | markup added; page object and tests not written |
| Shapes | all 7 | markup added; tests not written |
| Graphics | `GraphicsView` | markup added; test not written |
| Shell | `Shell` | exercised implicitly by every fixture, never asserted |

\* `Grid` does have passing coverage via `GridContainerTests` and `AutomationProbeTests`;
it is listed here only because its *module* tests do not pass. `Border`, `ContentView`, and
`ScrollView` have probe-level coverage (addressability) but no behavioural test.

---

## Known platform limits

Measured on Windows/FlaUI, not assumed. These bound what *can* be tested:

| Limit | Effect |
|---|---|
| Layouts need automation handlers | `Grid`, stack layouts, `ContentView`, `Border` expose no `AutomationId` without the handlers in `samples/Brinell.Maui.AppSupport`. Without them, container objects silently fail to resolve. |
| `Frame`, `SwipeView`, `RefreshView` unaddressable | No handler can fix them — overriding the WinUI peers on SwipeView/RefreshView collapses the entire UIA tree. |
| `MenuBarItem` unaddressable | Page-level menu bars never reach the UIA tree. `ToolbarItem` *is* addressable by AutomationId. |
| CollectionView recycles rows | Only ~30 of 63 rows exist in the tree at once, even at 100% scroll. Two virtualization tests are skipped for this — it is a data-model mismatch, not a scrolling gap. |

---

## Genuinely missing control objects

This section previously listed 24 "missing" controls. Most of them have since been
implemented — `IndicatorView`, `BoxView`, `ContentView`, `Frame`, `IsoPaneView`, all seven
Shapes, `GraphicsView`, `HybridWebView`, `BlazorWebView`, and `TitleBar` all have control
objects today. Verified against `srcnew/Brinell.Maui/Controls/`, not remembered.

What is actually absent:

| Control | Why it is not built |
|---|---|
| **Map** | Needs a separate NuGet and location permissions; testing it is a project of its own. |
| **GestureRecognizer** family (Tap, Pan, Pinch, Swipe, Drag, Drop, PointerGesture) | Not controls but behaviours attached to controls. They need pointer input, which is policy-gated on Windows, and AGENTS.md keeps direct mouse movement out of the public test API. Would need a dedicated design. |
| **TwoPaneView** | Foldable-device layout. `IsoPaneView` exists and may be the same thing under a project-specific name — unconfirmed. |

**The real gap is not missing control objects.** It has moved. Sample markup now covers 42 of 52 — the recent module pages closed most of
that gap. **The bottleneck is now tests**: 20 controls sit in the sample with no passing
test against them. Adding
a control object is cheap; proving it works against a running app is the expensive part,
and that is where the coverage actually stops.

---

## Brinell Extensions (Non-Standard, 7 total)

Controls created by Brinell that extend or wrap MAUI functionality:

1. **ImageButton** - Enhanced button with image support
2. **IconCommandButton** - Command button with icon/label template
3. **Link** - Hyperlink-style button
4. **RoundButton** - Circular button variant
5. **Expander** - Expandable container (MAUI 9+)
6. **GenericBrowser**, **SelectionList**, **FlyoutItem**, **Menu**, **Tab**, **TabMenu** - Custom navigation/selection UI components
7. **EditableField** - Generated control for edit scenarios

> `PaginatedList` was an eighth entry here. It had no callers and derived from the
> deprecated `Controls/List.cs`; it has been removed.

---

## Recommendations

The priorities below are reordered around the finding in the matrix: **control objects are
was sample markup, and is now tests.** 42 of 52 controls have markup; only 22 have a
passing test.

### Priority 1 — make existing control objects testable

These already have control objects and need only sample markup plus tests:

- [ ] **`ListView`, `CarouselView`, `IndicatorView`** — rollout plan Phase 3
- [ ] **`TableView`** — rollout plan Phase 4; two-level section/cell model, may not fit
      `CollectionObjectBase`
- [ ] **`ContentDialog`** — 4 unit tests but no sample page
- [ ] **`ImageButton`** — has a control object, but `ImageButtonTests` drive it through
      `Button<>`. Either add markup and test the real object, or delete the object.

### Priority 2 — scenario coverage

- [ ] **Data-management page** — filter, sort, select, mutate over a `CollectionView`.
      uncovered-areas plan Phase C.

### Priority 3 — Media

- [ ] **`WebView`** — plain webview against bundled local content
- [ ] **`MediaElement`** — needs `CommunityToolkit.Maui.MediaElement`, a new dependency
- [ ] **`HybridWebView` / `BlazorWebView`** — need a different app shape; probably belong
      with `Brinell.Samples.Blazor.App` rather than here

### Priority 4 — accept as untested, and label them

Some control objects will never be worth a sample page. Better to say so in their XML docs
than to leave the gap silent:

- [ ] **Shapes** (7) — no interactive behaviour to assert
- [ ] **`BoxView`** — no children by design
- [ ] **`GraphicsView`, `TitleBar`, `Tab`** — specialised or window-level
- [ ] **`Frame`, `SwipeView`, `RefreshView`** — not addressable on Windows at all; their
      docs should say so, since a container that silently never resolves is worse than one
      that does not exist

### Not planned

- [ ] **`Map`** — separate NuGet, location permissions
- [ ] **Gesture recognizers** — need pointer input, which AGENTS.md keeps out of the public
      test API; needs its own design first

---

## Coverage by category

```
Controls/                    object / sampled / tested
├── Buttons/                     2  /  1  /  1     ImageButton has no markup
├── Collection/                  5  /  5  /  1     markup added; tests not written
├── Container/                   9  /  8  /  1     markup added; ContainerModuleTests not passing
├── DateTimes/                   2  /  2  /  2     complete
├── Dialogs/                     1  /  0  /  0     unit tests only
├── Display/                     5  /  4  /  4     TitleBar absent
├── Graphics/                    1  /  1  /  0     markup added; no test
├── Media/                       4  /  0  /  0     entirely uncovered
├── Navigation/                  7  /  5  /  4     Shell implicit; Tab/FlyoutItem absent
├── Range/                       2  /  2  /  2     complete
├── Selection/                   1  /  1  /  1     complete
├── Shapes/                      7  /  7  /  0     markup added; no tests
├── Text/                        3  /  3  /  3     complete
└── Toggle/                      3  /  3  /  3     complete
                               -----------------
                                 52 / 42 / 22
```

---

## Official MAUI Control Inventory

### Pages (4 types — not in Brinell scope)
ContentPage, FlyoutPage, NavigationPage, TabbedPage

### Layouts (7 types)
AbsoluteLayout, BindableLayout, FlexLayout, Grid, HorizontalStackLayout, StackLayout,
VerticalStackLayout.

Only `Grid` has a control object today. The rollout plan's Phase 2 adds the rest — all
five were confirmed addressable by `AutomationProbeTests`, given the automation handlers.

## Notes

- **Brinell Extensions** provide enhanced versions of standard controls or domain-specific
  convenience wrappers.
- **Gesture recognition** is unsupported; it needs pointer input, which is policy-gated on
  Windows and deliberately kept out of the public test API.
- **Shapes** have control objects but no tests — they have no interactive behaviour to
  assert.
- Control objects follow the generic `<TScope>` pattern for fluent chaining. Containers use
  `<TParent, TSelf>` instead, so a chain stays inside the container.
- **Windows requires automation handlers** for layout and content containers. See
  `samples/Brinell.Maui.AppSupport`. Without them, container objects fail to resolve with no
  diagnostic beyond `ElementNotFoundException`.

### How the numbers in this document were produced

Counted from the tree, not from memory:

- **Object** — files under `srcnew/Brinell.Maui/Controls/` excluding `.gen.cs` and bases
- **Sample** — `grep` for `<ControlName` across `samples/Brinell.Samples.Maui.App/**/*.xaml`
- **Tested** — control types instantiated in `testsnew/Brinell.Maui.UITests/`, with
  `[Fact]` counts per file

Re-run those three sweeps before trusting this document after significant work. The version
before this revision listed 24 controls as missing that had since been implemented.
