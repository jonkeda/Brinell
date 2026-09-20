# Construction UITest — known app-code issues (deferred)

Issues discovered while running the module smoke suites in mock mode. These are
**app-code / data-shape** problems to fix later — the test scaffolding (page
objects + test classes) is being built first, per direction to defer code fixes.

---

## #1 — Project sync/snapshot path hangs when the Projects endpoint returns data (BLOCKER)

**Symptom.** As soon as `GET /project?page=1&per-page=500` returns real rows
(2 mock projects) instead of an empty array, the post-login **cached-entity
sync never completes**: the startup sync popup stays open and every login-based
test fails with:

```
Mock auth mode could not reach dashboard within 60000ms ... screen observed: False
```

Auth/startup goes from ~5s to ~63s (exceeds the 60s dashboard-ready timeout).

**Why it matters for the whole suite.** The startup cached-entity sync runs on
*every* login, so this hang breaks **all** modules (Contacts, Todos, Hours,
Dashboard…), not just Projects.

**Root cause chain (confirmed empirically):**
1. `Project` is registered as a synced cached entity (order 500, Bouw
   `"/project"`, `staleAfter 1h`) in `ProjectsRegistrations.RegisterSyncCache`.
2. `SyncService.SyncReferenceDataAsync` → `CachedEntitySyncService.CachedRefreshAsync`
   → `ProjectBouwRepository.CachedRefreshAsync` pages `"/project?page=N&per-page=500"`.
3. With rows present, `BouwCachedRepositoryBase.CachedRefreshAsync` calls
   `PrepareRemoteSnapshotAsync(remote, ct)` (sets IsFavorite / contact /
   `StatusName`). Something in that snapshot-prep / projection path does not
   complete within the timeout (suspected: a per-project remote call that is not
   mocked and retries, or a slow/blocking projection repair).

**Next investigation step (when we resume app fixes):**
- Instrument `ProjectBouwRepository.PrepareRemoteSnapshotAsync` and
  `RepairProjectionAsync`; capture the WireMock request log during startup to see
  which request stalls/repeats. If it is an unmocked call, add the stub (test
  infra); if it is an app retry/deadlock, fix the app path.

**Verified NOT the cause (ruled out this session):**
- Deserialization — `BouwProjectDto` converters are correct (`UnixEpochDateTimeOffsetConverter`
  for `created_at/updated_at`, `BouwDateConverter` for `start_date/end_date`).
- Status eligibility — `/user/data` `app_rights.project_status` has 11/12/13
  `view=true`; statuses 11 & 13 are `IsOpen` (`ClosesProject==0 && InvoicesProject==0`).
  Overview logged `eligIds=[11,13]`, so the empty list was purely an empty cache.

**Related test-infra bug (fix ready, currently held back):** In
`MockBackendHostManager.Projects.cs` the catch-all `GET /project → []` stub had
default WireMock priority (0), which **outranks** the specific
`/project?page=1&per-page=500` contract-sample stub registered at
`AtPriority(5)` (lower number = higher precedence in WireMock.NET). So the sync
always received `[]` → 0 projects cached → Projects overview always empty.
The one-line fix (`.AtPriority(1000)` on the catch-all) is **reverted for now**
because it triggers issue #1 for the whole suite. Re-apply it together with the
#1 fix. Contacts/Employee/Hour-type pairs work because both their stubs share the
default priority and WireMock breaks the tie by matcher specificity.

**Impact on tests (current state):**
- `ProjectsOverviewSmokeTests.Projects_Overview_Loads` — passes (container present,
  empty list).
- `ProjectsOverviewSmokeTests.Projects_OpenFirstProject_ShowsDetailWithTodosTab`
  — fails: no `ProjectItem_0` (list empty). Will pass once #1 + the priority fix land.

---

## #2 — Bottom tab bar reality (already corrected in tests, keep in mind)

App bottom tabs = `Hours | Planning | Projects | Contacts | More`. Dashboard and
Settings live under the **More** overflow (menu built in
`ConstructionApplicationInfo.cs`; 6 items, `TabMenuView` shows 4 + More).
There is **no** "Dashboard" bottom tab. Login/Dashboard smoke tests were
corrected to assert Hours/Projects/Contacts and to round-trip Projects→Hours.

---

## #3 — Missing app-side AutomationIds on Todo form editors (runtime-blocked)

Inner form editors in `Exact.Core.Construction` FormInput views lack
AutomationIds: DatePicker/TimePicker map to an ambiguous `Custom` control type,
Selection value label + open chevron and inspection/row field labels are not
addressable. DateTime/Selection/row-field reads are best-effort until the app
adds ids. (Carried over from the Todos PoC notes.)

---

## #4 — More → Settings navigation is not drivable by UI automation (tooling + app UX)

**Symptom.** The Settings smoke test (`SettingsSmokeTests`) can reliably **open**
the More overflow (`NavigateToTab("More")`, then poll `MorePage.IsOverflowOpen`),
but **cannot activate any Settings entry** to navigate to `SettingsView`. After
every click strategy the automation tree remains on `MoreView` (verified with
`Driver.GetAutomationTree()` dumps: `SettingsPage` count stays `0`). The test is
therefore `[Fact(Skip=…)]`.

**What was tried (all leave the tree on MoreView):**

| Target | Verb | Element (from tree) | Result |
|---|---|---|---|
| Header gear `MoreView_Settings` | `Click()` | Button, `Rect{410,99,48,48}`, Visible/Enabled/HasUsableBounds all true | no nav |
| Header gear `MoreView_Settings` | `DoubleClick()` (physical) | same | no nav |
| Menu caption `MenuItemCell_Caption` "Settings" | `Click()` | Text/TextBlock `Rect{140,347,50,19}` | no nav |
| Menu row (ListViewItem) | `Click()` + physical `DoubleClick()` at center | ListItem `Rect{212,457,416,56}` | no nav |
| **Control proof:** bottom tab `TabMenuView_Grid` | `Click()` **and** physical `DoubleClick()` | Button (identical peer type) | **navigates** (→ HoursOverview) |

**Root causes (two independent, both outside the test's control):**

1. **Gear is in the WinUI custom title-bar drag region.** The gear renders at
   `Y≈99`, inside the window's extended title bar. Synthetic *physical* mouse
   clicks there are consumed by window-drag rather than delivered to the control.
   The bottom tabs work because they are far from the title bar and are hosted in
   a native `GridView`. A coordinate-independent `Invoke()` would bypass the drag
   region — but the **committed Brinell binary bundle** (`lib/brinell/0.1.0-construction.1`)
   exposes **no** `Invoke()`/`Focus()`/keyboard verb on `IMauiElement`; only
   physical `Click`/`DoubleClick`. (Brinell `srcnew` *does* have `Invoke()`, but
   building `-p:UseBrinellSource=true` fails today because
   `Exact.Core.Construction.UITest/Controls/Collection/PageableCollectionControl.cs`
   was written against the bundle's older `IMauiItemContainer<,>` API, which
   changed in srcnew — `CS0246` / `CS0311`.)
2. **Menu-list cell tap gesture ignores synthetic input.** `MoreView_List` has no
   `ItemTappedCommand`; each row navigates via the `MenuItemCell` root
   `ClickableContainer Command="{Binding TapCommand}"` (renders as a `Custom`
   element with no Invoke peer). Its MAUI `TapGestureRecognizer` does not fire on
   FlaUI synthetic pointer input inside the virtualized WinUI ListView.

**Not the cause (ruled out):** `HandleSettingsCommand` resolving `ICoreSettings`
is fine — Construction re-registers it in
`InfrastructureRegistrations.cs` (~line 152) via a `DelegateFactory` →
`ConstructionSettings`; and `ISettingsViewModel` → `SettingsViewModel` →
`SettingsView` is registered in `SettingsRegistrations.cs:14`. So the command
would navigate **if** its tap actually fired.

**Options to unblock (need a decision):**
- **(A)** Refresh the committed Brinell bundle from `srcnew` so `IMauiElement`
  gains `Invoke()` (UIA InvokePattern — confirmed implemented in
  `Brinell.Maui.FlaUI/FlaUIMauiElement.cs`, and exactly the route that bypasses
  the drag region). **This is a full-suite migration, not a one-file reconcile.**
  The committed bundle (commit `aadfbbd`) is many commits behind `srcnew` HEAD
  (`69b4e20`), and `Invoke()` landed entangled with a large breaking refactor
  ("refactored maui flow", "deleted old controls", "First removal of Support
  properties", "refactor of keyboard/mouse/Focus"). Building
  `Exact.Construction.UITests -p:UseBrinellSource=true` produces ~30+ errors
  across **every** module (Contacts, Hours, Dashboard, Todos, Projects, Settings):
  `Brinell.Maui.Extensions` namespace removed; `ItemContainerBase<,>` removed/
  renamed; `INavigablePage.IsLoaded(int?)`/`PageObjectBase.IsLoaded` signature
  changed; `IMauiScope<>` container-scoping conversion changed; control types
  `RoundButton<>`/`EditableField<>`/`GenericBrowser<>` missing/renamed;
  `IMauiItemContainer<,>` → `IMauiItemObject<,>`. Only the
  `Exact.Core.Construction.UITest` *library* builds on `srcnew` (after a one-line
  constraint change in `PageableCollectionControl.cs`); the *host* project does
  not. There is no compatible intermediate commit — `Invoke()` cannot be
  cherry-picked without the refactor. So Option A = port the whole construction
  UITest suite to the new `srcnew` API, which breaks the currently-green modules
  (Hours 2/2, etc.) until the migration is complete.
- **(B)** App-side: give the gear an `AutomationId`'d invokable peer that is
  outside the title-bar drag region, or add an `ItemTappedCommand` to
  `MoreView_List` so row selection navigates. (App change — deferred per
  "don't fix app code".)
- **(C)** Keep the documented skip (current state). Suite stays green.

The page objects (`MorePage`, `SettingsPage`) and `SettingsSmokeTests` are built
and ready; only the More→Settings hop is blocked.

---

### UPDATE — Option A executed; `Invoke()` does **not** unblock Settings (root cause is app-side)

The full-suite migration to `srcnew` (Option A) was completed on a branch
(`uitests/brinell-srcnew-migration`) and the host now builds and runs in source
mode (`-p:UseBrinellSource=true`) with the previously-green suites all green
(Login 2/2, Dashboard 3/3, Hours 2/2). `IMauiElement.Invoke()` (UIA InvokePattern)
is available. **But driving the Settings gear/row with `Invoke()` still does not
navigate.** Verified empirically (source mode, live app + WireMock), after opening
the More overflow:

| Target | Verb | Result |
|---|---|---|
| Header gear `MoreView_Settings` | `Invoke()` | **returns success (no exception) but is a NO-OP** — tree stays on `MoreView`, `SettingsPage` count stays `0` |
| Header gear `MoreView_Settings` | `Select()` / `Toggle()` | `NotSupportedException` (no such route) |
| Menu row (`ListItem` containing "Settings" caption) | `Invoke()` | returns success but **no navigation** |
| Menu caption `MenuItemCell_Caption` "Settings" | `Invoke()` | `NotSupportedException` |
| **Control proof:** bottom tabs `TabMenuView_Grid` | `Invoke()` | **navigates** (they are CollectionView items whose invoke fires the item command) |

**Refined root cause.** `MoreView_Settings` and the `MoreView_List` "Settings" row
are `ClickableContainer`s — a plain `Grid` whose command is driven by
CommunityToolkit `TouchBehavior` (real **pointer** events). Its WinUI automation
peer exposes an `InvokePattern`, but `IInvokeProvider.Invoke()` does **not** raise
the `TouchBehavior`'s pointer command, so activation is a silent no-op. Coordinate
independence (the reason Option A was chosen) is irrelevant: there is no automation
route that fires a `TouchBehavior` command. The bottom tabs differ only because
they are CollectionView items whose container-level invoke fires the item command.

**Conclusion.** Options **(A)** and **(C)** are settled: Invoke is available but
cannot drive `ClickableContainer` navigation. Un-skipping `SettingsSmokeTests`
requires **Option (B), an app-side change** — e.g. make the gear a real `Button`
(or `ImageButton`), expose a custom `AutomationPeer` whose `Invoke` runs the
command, or add an `ItemTappedCommand` to `MoreView_List`. The test remains
`[Fact(Skip=…)]` with this refined reason. `MorePage.OpenSettings` now uses the
`Invoke → Select → Click` ladder (correct once the app exposes an invokable
surface), and the nav helper `ConstructionFixture.TrySelectTab` was updated for
srcnew's Click semantics (raw physical `Click()` throws on Windows; tabs are now
activated by matching `TabMenuView_Grid` on its caption and `Invoke()`-ing it).

