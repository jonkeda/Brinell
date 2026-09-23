# Construction UI test migration — module stepguide (source-backed)

Companion to [stepguide.md](stepguide.md). That guide is phase-oriented and forward-looking;
this one is **scoped to modules and controls whose source already exists today** in the two
UITest projects. TODO / empty-page modules are deliberately excluded — they are added later
via [stepguide.md](stepguide.md) phases 3–6 once their controls and views land.

**All paths from the Construction repo root** (`E:\repos\MainConstruction\construction`).
PowerShell terminals. One UI test process at a time. Kill orphaned app processes before each
run.

## In scope (source exists now)

| Module | App views | Test-project state | This guide adds |
| --- | --- | --- | --- |
| Todos | `Modules/Todos/Views/*` | Pages + containers + 3 smoke classes | done (PoC) |
| Authentication | `Modules/Authentication/*` | `LoginPage` + `LoginSmokeTests` | run to green |
| Contacts | `Modules/Contacts/*` | 4 pages, 2 containers, 2 smoke classes | run to green |
| Dashboard | `Modules/Dashboard/*` | `DashboardPage` only | `DashboardNavigationSmokeTests` |
| Projects | `Modules/Projects/*` | `ProjectsOverviewPage` + `ProjectDetailPage` | `ProjectsOverviewSmokeTests` |
| Startup | `Modules/Startup/*` | 3 page objects, no tests | page objects only (see note) |

**Startup note:** the shared `ConstructionFixture` launches the app once and authenticates
through `Auth/`, so by the time any test runs the splash/welcome sequence is already past.
The `StartupPage` / `WelcomePage` / `CachedEntitySyncProgressPage` objects stay as reusable
building blocks, but there is no reliable, non-flaky standalone Startup flow to assert against
the shared fixture. Startup gets a dedicated suite only if/when a restart-per-test or
cold-launch fixture is introduced (out of scope here).

## Out of scope (no source yet — do NOT create)

Hours, Journal, Materials, Purchases, Equipment, Employees, Files and every P3 module have
app views but **no ControlObjects/page objects** and need library controls (Cells,
DisplayFields, Number/Date inputs, Rich*, Settings) that are not written yet. They are handled
by [stepguide.md](stepguide.md) phases 4–6, not here.

## Shared conventions (already established by the PoC)

- One shared xUnit collection: `[Collection("Construction")]` + `ConstructionFixture`
  (app launched once). No per-module collection.
- Test classes inherit `ConstructionTestBase`; register pages with `RegisterPage` /
  `RegisterRootPage`; wrap bodies in `RunWithRecovery(name, setup, assertions)`.
- Navigate to a top-level tab with `Fixture.NavigateToTab("<caption>")` then
  `page.WaitReady(timeoutMs)`. Project sub-tabs (e.g. Todos) go Projects → open project →
  select sub-tab (see `Modules/Todos/Tests/TodosNavigation.cs`).
- Traits: `[Trait("Category","Smoke")]`, `[Trait("Module","<Module>")]`,
  `[Trait("Page","<PageObject>")]`.
- Mock backend permissions (`MockBackend/MockBackendHostManager.PermissionsAndUser.cs`)
  already grant projects, contacts, dashboard, timeline/todo — so those tabs render.

---

## Steps

Do them in order. After each code step: build the test project; only run the affected
module. Kill orphans first, every run:

```powershell
Get-Process -Name "Exact.Construction*" -EA SilentlyContinue | Stop-Process -Force
```

### Step 1 — Rebuild the Construction app

`dotnet test` builds the **test** project, not the app it launches. Rebuild the app so the
launched binary matches the current mock backend + views:

```powershell
dotnet build Exact.Construction\Exact.Construction.csproj -f net10.0-windows10.0.19041.0
```

### Step 2 — Authentication (run to green)

`LoginSmokeTests` already exists (Dashboard reached; Dashboard + Projects tabs visible). Run:

```powershell
dotnet test Exact.Construction.UITests\Exact.Construction.UITests.csproj `
  --filter "FullyQualifiedName~LoginSmokeTests" -v:minimal /nr:false
```

Green establishes the launch + auth + mock baseline everything else depends on.

### Step 3 — Todos (run to green)

Three PoC classes (`TodoOverviewSmokeTests`, `TodoInspectionSmokeTests`,
`TodoEditorSmokeTests`) plus the `MockBackendHostManager.Todos.cs` seed. Run one class at a
time:

```powershell
dotnet test Exact.Construction.UITests\Exact.Construction.UITests.csproj `
  --filter "FullyQualifiedName~TodoOverviewSmokeTests" -v:minimal /nr:false
# then TodoInspectionSmokeTests, then TodoEditorSmokeTests
```

### Step 4 — Dashboard (`DashboardNavigationSmokeTests`)

Add `Modules/Dashboard/Tests/DashboardNavigationSmokeTests.cs`:

- Landing smoke: `Fixture.WaitForAuthenticated()` → `Fixture.DashboardPage.AssertLoaded(true)`.
- Tabs smoke: assert `HasVisibleTab("Dashboard")` and `HasVisibleTab("Projects")`.
- Navigation smoke: `Fixture.NavigateToTab("Projects")` returns and Dashboard tab is still
  reachable via `NavigateToTab("Dashboard")` + `AssertLoaded(true)`.

Run: `--filter "FullyQualifiedName~DashboardNavigationSmokeTests"`.

### Step 5 — Projects (`ProjectsOverviewSmokeTests`)

Add `Modules/Projects/Tests/ProjectsOverviewSmokeTests.cs`:

- List smoke: `NavigateToTab("Projects")` → `ProjectsOverviewPage.WaitReady`.
- Open smoke: `OpenFirstProject()` → `ProjectDetailPage.WaitReady`, then
  `SelectTodosTab()` resolves (reuses the Todos nav path, proving the sub-tab route).

Run: `--filter "FullyQualifiedName~ProjectsOverviewSmokeTests"`.

### Step 6 — Contacts (run to green)

`ContactListSmokeTests` + `ContactAddSmokeTests` already exist (favorite-scenario helpers are
intentionally commented pending `WaitForVisibleContactNames`). Run each class alone:

```powershell
dotnet test Exact.Construction.UITests\Exact.Construction.UITests.csproj `
  --filter "FullyQualifiedName~ContactListSmokeTests" -v:minimal /nr:false
# then ContactAddSmokeTests
```

### Step 7 — Update progress

Mark Todos, Authentication, Contacts, Dashboard, Projects done in [modules.md](modules.md) and
the phase tables in [plan.md](plan.md). Leave Startup as "pages only".

---

## Working agreement (repeat of the hard rules)

- One UI test process at a time; never two module filters in parallel.
- Kill orphaned `Exact.Construction*` processes before every run.
- Never pipe `dotnet test` through `Select-Object -Last N` (buffers all output, looks hung).
- Rebuild `Exact.Construction` before a UI run if the app changed.
- No `IMauiElement` / `Locator` / `FindElement` inside test methods — extend a page object.
- No back-compat shims; add cleanly.
