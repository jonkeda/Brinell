# Construction UI test migration - modules

Every Construction module and its state in the migration. One row per module; one
`Exact.Construction.UITests/Modules/<Module>/` **folder** per module (all inside the single
existing test project - no per-module `.csproj`).

Priority: **PoC** = Todos (built first, locks the pattern), **P1** = has existing tests /
folder in the project, **P2** = high-traffic module, ship a smoke suite as soon as its
controls are ready, **P3** = reserved, add when picked up.

Source scan: `Exact.Construction/Modules/` (app),
`Exact.Construction.UITests/Modules/` (tests). Authentication, Contacts, Dashboard and
Startup folders already exist in the test project.

---

## Todos (PoC)

Views: `ProjectTodosOverviewView.xaml`, `TodoInspectionView.xaml`, `TodoEditorView.xaml`.

- **Priority**: PoC
- **Phase**: 2
- **State**: -
- **Controls used**: T1-T10 (see [controls.md](controls.md)), plus a page-object-side
  `TodoListCollection` / `TodoItemContainer` (T11).
- **Page objects to build**:
  - `ProjectTodosOverviewPage : OverviewPagePatternObject<ProjectTodosOverviewPage>` -
    exposes `Search`, `RetainedAlert`, `TodoList`, `Empty`, `Error`, `LoadingOverlay`,
    `RefreshingIndicator`, `InspectItem(index)`.
  - `TodoInspectionPage : PageObjectBase<TodoInspectionPage>` - Edit / Complete / Reopen /
    Delete / Close buttons; Name + Description + DueDate + Priority + Status + Assignees
    labels.
  - `TodoEditorPage : EditPagePatternObject<TodoEditorPage>` - binds the five form inputs.
- **Containers**:
  - `TodoListCollection` (in `Containers/`), `TodoItemContainer`.
- **Tests to build** (`Tests/`):
  - `TodoOverviewSmokeTests` - navigate to Todos, list renders, empty vs content vs error
    states resolve, search filters items.
  - `TodoInspectionSmokeTests` - tapping a row opens the inspection sheet with the right
    values; buttons show per state.
  - `TodoEditorSmokeTests` - editor loads values, Name is required, saving posts to the
    mock backend, DateTime and Selection round-trip.
- **Backend seed**: WireMock scenario with one open todo (Alpha), one completed (Beta).
- **Exit gate**: 3 smoke test classes green on Windows against the real Construction app +
  WireMock backend.

---

## P1 modules (folders already in the test project)

### Authentication

- **Priority**: P1
- **Phase**: 3
- **State**: -
- **Source tests**: `LoginSmokeTests.cs` (`Modules/Authentication/Tests/`)
- **Source pages**: `LoginPage.cs`
- **Controls used**: seeds S1-S6, plus module-level Entry/Button (no custom).
- **Migration**: the `Modules/Authentication/` folder already exists. This module owns the
  login-screen tests; every other module reuses the shared `Auth/`
  (`ConstructionAuthenticationManager`) via the inherited fixture. Fill in any missing
  `LoginPage` / `LoginSmokeTests` under `Modules/Authentication/{Pages,Tests}`.

### Startup

- **Priority**: P1
- **Phase**: 3
- **State**: -
- **Source pages**: `StartupPage`, `WelcomePage`, `CachedEntitySyncProgressPage`.
- **Source tests**: none upstream.
- **Migration**: `Modules/Startup/` exists. Complete the three page objects. Add a
  `StartupSmokeTests` that walks the initial startup sequence from launch to Welcome or
  Dashboard, whichever the config lands on.

### Dashboard

- **Priority**: P1
- **Phase**: 3
- **State**: -
- **Source pages**: `DashboardPage.cs`.
- **Source tests**: none upstream; the fixture uses `DashboardPage` for tab navigation.
- **Migration**: `Modules/Dashboard/` exists. Complete `DashboardPage`. Add a
  `DashboardNavigationSmokeTests` that clicks each visible tab and asserts the target
  page's root AutomationId shows up.

### Contacts

- **Priority**: P1
- **Phase**: 3
- **State**: -
- **Source pages**: `ContactListPage`, `ContactDetailPage`, `ContactCreateEditPage`,
  `ContactFilterPage`.
- **Source containers**: `ContactListCollection`, `ContactItemContainer`.
- **Source tests**: `ContactListSmokeTests`, `ContactAddSmokeTests`.
- **Controls used**: S1, S3-S7; C6 `RoundButtonControl` (Add button), C7
  `TabMenuItemControl` (tab navigation), C11 `FilterSheetControl` (filter).
- **Migration**: `Modules/Contacts/` exists with `Pages/`, `Containers/`, `Tests/`.
  Complete all four pages, both containers, both test classes. Keep the currently-commented
  `WaitForVisibleContactNames` and `ClearFavoriteState` code as-is (marked TODO). Contacts
  is also where C11 FilterSheet gets its first real consumer - resolve the modal root scope
  here.

---

## P2 modules (no upstream tests, ship smoke first)

Ordered roughly by expected value / test density.

### Projects

- **Priority**: P2
- **Phase**: 5
- **State**: -
- **Source module folder**: `Modules/Projects/`
- **Notes**: hosts the Todos view (`ProjectTodosOverviewView` is a tab of the Project
  detail page). Navigation to Todos in the PoC phase already exercises Projects
  transitively; a dedicated smoke suite still lands here in phase 5.

### Hours

- **Priority**: P2
- **Phase**: 5
- **State**: DONE - `HoursOverviewSmokeTests` green 2/2 on Windows (app + WireMock).
- **Delivered**: library controls `NumberFormInputControl`, `DateFormInputControl`
  (`Exact.Core.Construction.UITest/Controls/FormInputs/`); page objects `HoursOverviewPage`
  (`OverviewPagePatternObject`), `HoursEditPage` (`EditPagePatternObject`, footer
  Save/Cancel/Delete not header actions); containers `HoursListCollection`,
  `HoursItemContainer`; tests `HoursOverviewSmokeTests` (overview loads + week selector +
  list; Add opens the edit form). Hours mock already existed.
- **Notes**: heavy use of Number and Date form inputs - the first module that pulls in
  those additional form input controls.

### Settings

- **Priority**: P3
- **Phase**: 6
- **State**: BLOCKED - page objects + smoke test built, but the More→Settings navigation
  hop is not drivable under the committed Brinell bundle. `SettingsSmokeTests` is
  `[Fact(Skip=…)]`. See `known-issues.md` #4.
- **Delivered**: `MorePage` (opens the More overflow, polls `IsOverflowOpen`; best-effort
  gear/menu-row click); `SettingsPage` (root `SettingsPage`, `UserName`/`UserEmail`/
  `AppVersion` labels, `Synchronize`/`Interval`/`UploadQueue`/`Logout` buttons);
  `SettingsNavigation.GoToSettings`; `SettingsSmokeTests` (skipped).
- **Blocker**: the More header gear `MoreView_Settings` sits in the WinUI custom title-bar
  drag region (synthetic physical clicks swallowed) and the menu-list cell's MAUI tap
  gesture ignores synthetic input; the bundle's `IMauiElement` has no coordinate-independent
  `Invoke()`/`Focus()`. Bottom tabs (native GridView) confirm clicks otherwise land.
  Unblock = refresh Brinell bundle to gain `Invoke()` (needs collection-control API
  reconcile) **or** an app-side change to the entry. See `known-issues.md` #4.

### Journal

- **Priority**: P2
- **Phase**: 5
- **State**: -
- **Notes**: first module that likely needs `RichTextFormInputControl` (phase 6 gate) - if
  the smoke test doesn't touch the editor, the smoke can land in phase 5 ahead of C14b.

### Materials / Journal / Purchases / Equipment / Employees / Files / Garbage

- **Priority**: P2
- **Phase**: 5
- **State**: NOT PAGES - no standalone UI. Source scan of `Exact.Construction/Modules/*/Views`
  shows the app has NO `Materials`, `Journal`, `Purchases`, `Equipment`, `Employees`,
  `Files` or `Garbage` module views. Their WireMock stubs
  (`MockBackendHostManager.{Materials,Journals,Equipment,Employees,Files,Garbage}.cs`) seed
  **reference data** consumed elsewhere: Settings toggles (`settings_material_creation`,
  `settings_garbage_creation`), the project delivery flows under `Modules/ProjectsOther/`
  (e.g. `ProjectDeliveryDocumentView` MaterialsLabel/Switch), and `SelectionFormInputView`
  pickers (Employees on `ProjectCreateView`, `TodoEditorView`). There is nothing to build a
  dedicated overview page object / smoke test for. Cover this data indirectly via the pages
  that host it (Settings, ProjectsOther delivery flows).

---

## P3 modules (reserved)

Each becomes its own `Exact.Construction.UITests/Modules/<Module>/` folder when it's
picked up. No folder is created eagerly.

| Module | Notes |
| --- | --- |
| Planning | Calendar / gantt heavy - verify UIA route before scoping |
| Progression | |
| Blueprint | Custom drawing surface; may need bridge admission per AD-008 |
| Maintenance | |
| Qhse | |
| Departments | |
| EmployeesDayOff | |
| CustomAttributes | Dynamic form; extra care around AutomationId stability |
| Garbage | |
| OrganizationInfo | |
| ProjectsOther | |
| Rights | Feature toggles - likely a Settings-style module |
| Settings | First consumer of C15 (Settings-only cells) |
| UserInfo | |
| Developer | Internal-only; unlikely to grow a UI test suite |
| CachedEntities | Sync progress module; some pages already ported under Startup |

---

## Per-module folder template

Every module folder has the same skeleton inside `Exact.Construction.UITests/Modules/`:

```text
Exact.Construction.UITests/Modules/<Module>/
  <Module>Collection.cs                  # xUnit [CollectionDefinition]
  <Module>Fixture.cs                     # derives from ConstructionFixture (host project)
  Pages/                                 # module page objects
  Containers/                            # module-specific collections + item containers
  Tests/                                 # test classes; each has [Collection("<Module>")]
```

Available to every module (no per-module references needed - it is one project):

- `Exact.Core.Construction.UITest` (ControlObjects, page-pattern bases) via the project
  reference already in the host `.csproj`.
- The host project's own `ConstructionFixture`, `Auth/` (login), `MockBackend/`,
  `Diagnostics/`, `Configuration/`.
- Brinell (`Brinell.Maui`, `Brinell.Maui.FlaUI`, …) via `eng/brinell/Brinell.references.props`.

Rules the module template enforces:

- One test class per user-facing flow (list smoke, add smoke, edit smoke) - not one god
  class per module.
- Test methods say what the user does, not what element they click. Interaction detail
  belongs in page objects / control objects.
- No `IMauiElement`, `Locator`, or `FindElement` inside a test method. See
  [.github/skills/maui-ui-test/references/forbidden-apis.md](../../.github/skills/maui-ui-test/references/forbidden-apis.md).

---

## Progress table

| Module | Priority | Phase | State | Notes |
| --- | --- | --- | --- | --- |
| Todos | PoC | 2 | - | 3 smoke classes |
| Authentication | P1 | 3 | - | port LoginSmoke |
| Startup | P1 | 3 | - | 3 pages ported + new smoke |
| Dashboard | P1 | 3 | - | tab navigation smoke |
| Contacts | P1 | 3 | - | port List+Add, resolves C11 modal scope |
| Projects | P2 | 5 | - | |
| Hours | P2 | 5 | - | needs Number/Date form inputs |
| Journal | P2 | 5 | - | may defer RichText scenarios to phase 6 |
| Materials | P2 | 5 | - | |
| Purchases | P2 | 5 | - | |
| Equipment | P2 | 5 | - | |
| Employees | P2 | 5 | - | |
| Files | P2 | 5 | - | |
| Planning | P3 | 6 | - | |
| Progression | P3 | 6 | - | |
| Blueprint | P3 | 6 | - | |
| Maintenance | P3 | 6 | - | |
| Qhse | P3 | 6 | - | |
| Departments | P3 | 6 | - | |
| EmployeesDayOff | P3 | 6 | - | |
| CustomAttributes | P3 | 6 | - | |
| Garbage | P3 | 6 | - | |
| OrganizationInfo | P3 | 6 | - | |
| ProjectsOther | P3 | 6 | - | |
| Rights | P3 | 6 | - | |
| Settings | P3 | 6 | - | first C15 consumer |
| UserInfo | P3 | 6 | - | |
| Developer | P3 | 6 | - | probably skip |
| CachedEntities | P3 | 6 | - | some pages under Startup |
