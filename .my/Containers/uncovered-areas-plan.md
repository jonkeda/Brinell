# Plan: cover the areas lost with `UITests2` / `App2`

**Status:** Phases A and B done; C and D not started
**Scope:** `Brinell.Maui` — Media, Navigation, and the data-management collection page
**Companion to:** [common-controls-rollout-plan.md](common-controls-rollout-plan.md), which
covers containers and collection *controls*. This plan covers what neither that plan nor
the live test project reaches.

---

## 1. Why this exists

`Brinell.Maui.UITests2` and `Brinell.Samples.Maui.App2` were deleted after Phase 1 of the
rollout plan. They were a stale parallel copy — never in the solution, unable to navigate,
with only 14 of ~250 `Tests2` tests even discovered and 12 of those failing. **Nothing
working was lost.**

But they were the only place several control areas were exercised at all. Those areas are
now genuinely uncovered, and the rollout plan does not reach them.

### What is already covered elsewhere — do not duplicate

| Area | Covered by |
|---|---|
| Containers (`Grid`, `Border`, `ContentView`, `ScrollView`, layouts) | rollout plan Phases 1–2, `AutomationProbeTests` |
| `CollectionView` + item scoping | `ProductCollectionTests` (15 tests, live) |
| `ListView`, `CarouselView`, `IndicatorView`, `TableView` | rollout plan Phases 3–4 |
| `Button`, `Entry`, `Label`, pickers, toggles, ranges | live `UITests` (~110 tests) |
| Shell tab navigation | `AppShellPage` + every fixture navigation, implicitly |

### What this plan covers

| Area | Controls | Live coverage today |
|---|---|---|
| Media | `WebView`, `HybridWebView`, `BlazorWebView`, `MediaElement` | **none** |
| Navigation | `Toolbar`, `Menu`, `FlyoutItem`, `TabMenu` | ~~none~~ **done in Phase B** — `FlyoutItem` excluded, see 5.1 |
| Data management | a realistic `CollectionView` screen: filter, sort, select, mutate | **none** |

## 2. Two corrections to the earlier framing

I described the lost areas as "`CarouselView`, `ListView`, `TableView`, `PaginatedList`,
`DataGrid`, plus Media and Navigation." Two of those were wrong, and both are worth
stating plainly because they change what gets built.

### 2.1 There is no MAUI `DataGrid`

`DataGridPageTests` (21 tests) was the largest single file in the deleted project, which
made "DataGrid" look like a significant gap. It is not a control. The only `DataGrid` in
this repo is `Brinell.WinForms/Controls/DataGridView.cs`, a different platform entirely.

`DataGridPage` was a **sample page name** — a "Data Management" screen built from a
`CollectionView` plus filter/sort/selection controls. So the gap is not a missing control
binding; it is a missing *scenario*: a realistic, mutating, filterable collection screen.
That is worth rebuilding (section 5), but as a scenario test, not as control coverage.

### 2.2 `PaginatedList` was removed, not covered — **done, Phase A**

It was referenced by nothing — no other source, no test, no sample. Its only inbound edge
was its own base class.

It was also the fourth `List<TScope,TItem>` consumer, so removing it was a prerequisite for
deleting `List.cs` in the rollout plan's Phase 3, which now has three consumers to re-base
rather than four.

## 3. Ordering

Phase A is a deletion and should land first — it is small and it unblocks other work.
Phases B–D are independent of each other and of the rollout plan; any can be dropped.

| Phase | What | Size | Depends on |
|---|---|---|---|
| A | Remove `PaginatedList` | ~~1 hour~~ **done** | nothing |
| B | Navigation coverage | ~~1–2 days~~ **done** | nothing |
| C | Data-management scenario page | 2–3 days | rollout Phase 3 (`ListView` re-base) is *not* required |
| D | Media coverage | 2–3 days | decisions in 6.1 and 6.2 |

**Recommended order: A → B → C, with D last or dropped.** D carries the most unknowns
(see section 6) and the least certain value; B is the cheapest real coverage; C rebuilds
the one genuinely valuable scenario that was lost.

## 4. Phase A — remove `PaginatedList` — **DONE**

| Step | Result |
|---|---|
| Delete `PaginatedList.cs` | done |
| Delete the emptied `Controls/Collection/` folder | done |
| Build the solution | **succeeded**, no errors |
| Update the rollout plan's Phase 3 consumer count | done — three, not four |

The survey held: a full sweep across `.cs`, `.csproj`, `.md`, and `.xaml` found no code
reference anywhere. `List.cs` now has exactly three consumers, all in `Brinell.Maui`:
`CollectionView`, `ListView`, `CarouselView`.

Three stale documentation references were also corrected, since two of them presented
`PaginatedList` as an available control:

- `docs/implementation/MAUI_EXTENSIONS_PROJECT_MIGRATION.md` — historical migration record,
  annotated rather than rewritten
- `samples/Brinell.Samples.Maui.App/.my/maui-control-coverage-analysis.md` — a live coverage
  table; the row, the numbered entry, and the folder summary were removed, and the list
  renumbering it left behind was repaired

## 5. Phases B and C — sample pages and tests

Both follow the conventions established by `GridCollectionDemoView`, which are load-bearing
rather than stylistic:

- files under `Views/` and `Pages/`, namespace `...Views2.TestViews`
- `x:DataType` on the view and on every `DataTemplate`
- a `ShellContent` tab in `AppShell.xaml` with an `AutomationId`
- two `MauiXaml Update` entries in the `.csproj`
- **a Reset button** restoring known state, wired into fixture navigation
- **a count/state label** giving tests a logical value to wait on rather than polling
  realized elements
- **repeating, non-unique `AutomationId`s** in item templates, with no reindexing

That last one is not optional. Unique per-row ids make item-scoping tests pass without
testing anything. App2 got this exactly backwards — its rows had *no* id at all while the
old `List<>` looked them up by `Task_{index}`, ids maintained by a `ReindexTasks()` that
nothing consumed. Do not reproduce that shape.

### 5.1 Phase B — Navigation — **DONE**

**20 tests, all passing, stable across two runs.**

| Artefact | Where |
|---|---|
| Sample page | `Pages/NavigationDemoPage.xaml`, `Views/NavigationDemoView.xaml`, `ViewModels/NavigationDemoViewModel.cs` |
| Tab | `NavigationTab` in `AppShell.xaml` |
| Page object | `testsnew/.../Pages/NavigationDemoPage.cs` |
| Probe | `Tests/Navigation/NavigationProbeTests.cs` — 3 tests |
| Tests | `Tests/Navigation/NavigationControlTests.cs` — 17 tests |

#### The probe result, which shaped the tests

Measured before writing any assertions, exactly as Phase 0 did for layouts:

| Page chrome | By AutomationId | By Name |
|---|---|---|
| `ToolbarItem` (Refresh, About) | **yes** | NO |
| `MenuBarItem` / `MenuFlyoutItem` | **NO** | **NO** |

Two findings, one better and one worse than expected:

- **`ToolbarItem` *is* addressable by AutomationId.** The plan warned it might not be,
  citing dotnet/maui#3996. It is — but only by id, never by name.
- **`MenuBarItem` and `MenuFlyoutItem` are not addressable at all** — neither by id nor by
  name. Page-level menu bars do not reach the UIA tree in this configuration. This is the
  same class of negative as `SwipeView`/`RefreshView` in Phase 0: recorded, not fought.

All five in-page surfaces resolve, so the demo view drives every test.

#### What is covered

- **`Toolbar`** (6 tests) — exists, `GetTitle`, `ClickToolbarItem`, `GoBack`, fluent
  return, and two scoping tests. The demo declares **two** toolbars sharing child ids, so
  `Toolbar_ItemSearch_IsScopedToItsOwnToolbar` fails if item search ever resolves
  page-wide, and `Toolbar_DoesNotReachItemsOutsideItself` pins the no-fallback boundary.
- **`Menu`** (4 tests) — exists and starts closed, `Open` reveals items, `ClickMenuItem`
  fires and dismisses, double-open toggles.
- **`TabMenu`** (5 tests) — exists, `Select` by caption, selection moves, `TrySelect`
  reports false for an unknown caption, `Select` throws for one.
- **Reset** (1 test) — the fixture contract that makes the class order-independent.

`TabMenu` needed markup built to its own contract: it locates `TabMenuView` and matches
sibling `TabMenuView_Caption` text against paired `TabMenuView_Button` elements. No such
view existed anywhere in the repo, so the demo supplies one. Its ids repeat across tabs by
design — the control pairs them positionally.

#### `FlyoutItem` is not covered — deliberately

`FlyoutItem` locates itself by XPath on `@Name`, because (per its own XML doc) its
AutomationId does not propagate to the Windows UIA tree. It targets a **Shell flyout**,
and the sample sets `Shell.FlyoutBehavior="Disabled"` — every tab is a `ShellContent` in a
`TabBar`, so no flyout exists to test.

Covering it means enabling the flyout on the sample's Shell, which changes navigation for
**every existing test** in the project. That is a disproportionate risk for one control,
and it is a change to shared infrastructure rather than an additive page. Left uncovered;
worth revisiting only if the sample's Shell is restructured for another reason.

#### The absence-assertion defect bit again

Two menu tests failed first time with `ElementNotFoundException` from
`WaitExists(false, ...)` — the element is resolved before the comparison, so the call
throws for exactly the state it is being asked about. This is
[`.my/fixes/waitexists-absence-assertions.md`](../fixes/waitexists-absence-assertions.md),
now observed in a second independent place.

**That defect has since been fixed** (see §8 of the fixes document). The temporary
`WaitUntilMenuClosed()` helper was deleted and both call sites now use
`WaitExists(false, ...)` / `AssertExists(false)`, verified against the real app. Phase C's
empty-state tests can use the fluent form directly.

#### Verification

| Check | Result |
|---|---|
| `dotnet build Brinell.sln` | succeeded |
| `Tests.Navigation` | **20 passed, 0 failed** |
| `Tests.Navigation` re-run | 20 passed — stable |
| Tier 2 (Container + Collection) | 26 passed, 2 skipped — no regression |

### 5.2 Phase C — data management scenario

**Page:** `DataManagementPage` / `DataManagementView`, tab id `DataManagementTab`.

This rebuilds what App2's `DataGridPage` was reaching for, done properly. A
`CollectionView` of records plus:

- a search/filter `Entry` that narrows the list
- a sort toggle
- selection (single, and a multi-select mode if `CollectionView` selection is in scope)
- add / delete / clear / reset commands
- a count label reporting the **logical** (data-source) count

**Tests** (`Tests/Collection/DataManagementTests.cs`), roughly 15–20:

- filtering narrows the rendered rows; clearing the filter restores them
- filtering to no matches shows the empty state
- sorting reorders rows — assert on row *content*, not on index alone
- selecting a row reports selection; selecting another moves it
- add appends and the count label follows
- delete removes the right row and the remaining rows stay correctly scoped
- reset restores the seeded state
- row controls resolve within their row (repeating ids)

> **Stay inside the realized window.** Deep scrolling a virtualized `CollectionView` does
> not work on Windows/FlaUI — see `sample-app-ui-tests-design.md` §8.1 and
> `.my/fixes/`. Seed a list that fits, or assert on logical count from the label rather
> than on realized rows. Do not write a test that scrolls to index 60; it will be skipped
> like the two in `ProductCollectionTests`.

> **Empty-state assertions work.** `AssertExists(false)` and `AssertVisible(false)` report
> absence rather than throwing, as of the fix recorded in
> [`.my/fixes/waitexists-absence-assertions.md`](../fixes/waitexists-absence-assertions.md)
> §8. Use the fluent form; no `IsExists()` workaround is needed.

### 5.3 Phase D — Media

**Page:** `MediaPage` / `MediaView`, tab id `MediaTab`.

Contents: a `WebView` pointed at bundled local content, and — subject to 6.1 — a
`MediaElement`. `HybridWebView` and `BlazorWebView` are subject to 6.2.

**Tests** (`Tests/Media/`), roughly 8–10:

- webview exists, reports its URL, navigates, reports the new URL
- webview reports document title or a known element from the loaded page
- media element (if in scope) reports playing state, play/pause transitions, position

## 6. Open questions — need answers before the phase they gate

### 6.1 `MediaElement` needs a package the sample does not have

`Brinell.Maui/Controls/Media/MediaElement.cs` binds a control that MAUI does not ship in
the box: `MediaElement` comes from **`CommunityToolkit.Maui.MediaElement`**, a separate
package from `CommunityToolkit.Maui` which the sample already references.

Options: (a) add the package and cover it; (b) leave `MediaElement` uncovered and say so;
(c) drop the control binding if nobody uses it.

**Recommendation: (a)**, but confirm first — it adds a dependency and a native media stack
to the sample app, which may slow the UI suite's startup. If the answer is (b), the
control's XML docs should say it is untested. **Gates Phase D.**

### 6.2 `HybridWebView` and `BlazorWebView` may not be testable here

`BlazorWebView` needs a Blazor component tree; `HybridWebView` needs a hybrid app host.
Both are substantial sample-app additions for one control binding each, and this repo
already has `Brinell.Samples.Blazor.App` and `Brinell.Blazor.UITests` for Blazor work.

**Recommendation:** cover plain `WebView` only in Phase D, and leave the other two
explicitly uncovered with a note pointing at the Blazor projects. **Gates Phase D scope.**

### 6.3 How far should `WebView` testing go?

`IMauiElement` has DOM accessors (`GetDomAttribute`, `GetDomProperty`, `GetCssValue`) that
suggest reaching into web content. Whether the FlaUI adapter can actually cross the
WebView boundary on Windows is **unverified**.

**Recommendation:** probe it the way Phase 0 probed layouts — one page, one test, record
the answer — before writing tests that assume it works. If the boundary is opaque, cover
`WebView` at the control level only (exists, URL, navigate) and document the limit.

### 6.4 Is Media coverage wanted at all?

Media is the weakest case in this plan: four controls, one of which needs a new package,
two of which need a different app shape, and one whose depth is unverified. The rollout
plan's Phases 2–5 are higher value per day.

**Recommendation:** treat Phase D as genuinely optional. If the answer is "not now",
mark the four Media controls as knowingly untested in their XML docs rather than leaving
the gap silent — an untested control that *looks* covered is worse than one that is
labelled.

## 7. Verification

Per `AGENTS.md`, match the test tier to the change:

| Tier | Command |
|---|---|
| One area | `--filter "FullyQualifiedName~NavigationTests"` (~7 s) |
| Related | `--filter "FullyQualifiedName~Tests.Navigation\|FullyQualifiedName~Tests.Media"` (~11 s) |
| Full | no filter — phase completion or shared-infrastructure edits only |

Baselines to compare against, not to fix in passing:

- `Brinell.Maui.UITests`: **31 pre-existing failures** (DatePicker, TimePicker, Image,
  ProgressBar, Stepper, Switch)
- `Brinell.Maui.Tests`: **8 pre-existing failures** (`ViewBase` `RunPoll`)

Any *increase* is a regression from this work. Establish the baseline by stashing before
calling a failure yours.

## 8. What this plan deliberately does not cover

- **`CarouselView`, `ListView`, `TableView`** — rollout plan Phases 3–4 own these.
- **`DataGrid`** — not a MAUI control (2.1).
- **`PaginatedList`** — being removed (2.2, Phase A).
- **`Shell` / `ShellContent` / `Tab`** — exercised implicitly by every fixture.
- **Fixing the 31 + 8 pre-existing failures** — a separate decision, still open.
