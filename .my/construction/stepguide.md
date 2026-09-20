# Construction UI test migration - step guide

Do the steps in order. Each step is small enough to review in one commit. **All paths are
from the Construction repo root** (`E:\repos\MainConstruction\construction`) unless noted.
PowerShell terminals.

Everything is added inside the two projects that already exist:

- `Exact.Core.Construction.UITest/` - the ControlObject library (Brinell-Construction bridge).
- `Exact.Construction.UITests/` - the single xUnit test host (fixtures, Auth, mock backend,
  and one `Modules/<Module>/` folder per module). **No per-module `.csproj`.**

Brinell is a referenced NuGet package set (`eng/brinell/Brinell.references.props` +
`IncludeBrinell*` flags in the test project). Never copy Brinell sources in.

Prerequisites once, up front:

- Construction repo builds today: `dotnet build Construction.sln -v:minimal /nr:false` clean.
- The Brinell skills used below live in `Brinell/.github/skills/` in the sibling repo.

Reference:

- [plan.md](plan.md) - overview.
- [controls.md](controls.md) - control inventory.
- [modules.md](modules.md) - module inventory.
- [layout.md](layout.md) - target folder layout inside the existing projects.
- [.github/skills/maui-control/SKILL.md](../../.github/skills/maui-control/SKILL.md)
- [.github/skills/convert-control/SKILL.md](../../.github/skills/convert-control/SKILL.md)
- [.github/skills/maui-ui-test/SKILL.md](../../.github/skills/maui-ui-test/SKILL.md)

---

## Phase 0 - baseline (verify, don't create)

The library, the test host, the 8 seed controls and `OverviewPagePatternObject` already
exist. This phase only confirms the starting point is green.

### Step 0.1 - Confirm the library and host build

```powershell
dotnet build Exact.Core.Construction.UITest\Exact.Core.Construction.UITest.csproj -v:minimal /nr:false
dotnet build Exact.Construction.UITests\Exact.Construction.UITests.csproj -v:minimal /nr:false
```

Both green.

### Step 0.2 - Confirm the seed controls are present

`Exact.Core.Construction.UITest/Controls/` must contain the 8 seed folders
([controls.md](controls.md) S1-S8): `Search/`, `Messaging/`, `Loading/`, `TitleBar/`,
`HeaderActions/`, `Lists/`, `Favorites/`, `Collection/`. `PagePatterns/` must contain
`OverviewPagePatternObject.cs`. If any is missing, stop and reconcile with
[controls.md](controls.md) before continuing.

### Step 0.3 - Confirm the auth path works

The login path already exists under `Exact.Construction.UITests/Auth/`
(`ConstructionAuthenticationManager`, `Auth/Pages/`). The Todos PoC and every module reuse
it - do not add a new login flow. Confirm an existing module suite (e.g. Contacts) still
runs its login:

```powershell
dotnet test Exact.Construction.UITests\Exact.Construction.UITests.csproj `
  --filter "Module=Contacts" -v:minimal /nr:false
```

Green = **Phase 0 done.**

---

## Phase 1 - Todos PoC controls

One control per step, added to the existing `Exact.Core.Construction.UITest` library. After
each step: `dotnet build Exact.Core.Construction.UITest\...` + relevant unit tests. Use the
[maui-control](../../.github/skills/maui-control/SKILL.md) skill for the loop.

Source XAML is under `Exact.Construction/Modules/Todos/Views/` and
`Exact.Core.Construction/Controls/`.

### Step 1.1 - T4 `EditPagePatternObject<TSelf>`

`PagePatterns/EditPagePatternObject.cs` under `Exact.Core.Construction.UITest/`. Model after
the existing `OverviewPagePatternObject` but expose `TitleBar`, `HeaderActions`, `Form`
(returns a `FormContainerControl` once T5 lands - stub with `Component("Form")` for now) and
the two implicit Save / Cancel actions.

### Step 1.2 - T5 `FormContainerControl<TScope>`

`Controls/FormSections/FormContainerControl.tpl.cs`. Simple container: only exposes its
child sections through indexed access `Section(int index)` and named lookup
`Section(string title)`.

### Step 1.3 - T6 `FormSectionControl<TScope>`

`Controls/FormSections/FormSectionControl.tpl.cs`. Title label, expand/collapse button
(`Toggle` default id), content region. Small unit test proving `Expand()` and `Collapse()`
observe the expected state via app-side probe.

### Step 1.4 - T7 `TextFormInputControl<TScope>`

`Controls/FormInputs/TextFormInputControl.tpl.cs`. Label, Entry (`Value` id), required marker,
inline error label. Members: `SetText`, `GetText`, `GetError`, `IsRequired`.

### Step 1.5 - T8 `NoteFormInputControl<TScope>`

`Controls/FormInputs/NoteFormInputControl.tpl.cs`. Label + Editor. Same shape as T7.

### Step 1.6 - T9 `DateTimeFormInputControl<TScope>`

`Controls/FormInputs/DateTimeFormInputControl.tpl.cs`. Label + DatePicker + TimePicker.
Members: `SetDate(DateOnly)`, `SetTime(TimeOnly)`, `SetDateTime(DateTime)`, `Clear()`,
`GetValue`. Unit test covering the split (date-only vs date+time).

### Step 1.7 - T10 `SelectionFormInputControl<TScope>`

`Controls/FormInputs/SelectionFormInputControl.tpl.cs`. Label + trigger button that opens
the selection sheet. Members: `Open()` (returns a `SelectionSheetContainer` or similar),
`GetLabel()`, `ClearSelection()`. Decide the sheet-container shape from the source XAML
before writing.

### Step 1.8 - Verify

```powershell
dotnet build Exact.Core.Construction.UITest\Exact.Core.Construction.UITest.csproj -v:minimal /nr:false
```

Run any control unit tests you added (they live in the test host or the library's test
project, wherever the existing seed-control tests live). Green = **Phase 1 done.**

---

## Phase 2 - Todos tests (PoC)

All Todos test artefacts go into the **existing** `Exact.Construction.UITests` project under
`Modules/Todos/`. No new project, no new solution entry. Blueprint: the existing
`Modules/Contacts/` folder and `samples/Todo/tests/Brinell.Samples.Todo.UITests` in Brinell.

### Step 2.1 - Create the module folder + collection/fixture

Create `Exact.Construction.UITests/Modules/Todos/` with:

- `TodosCollection.cs` - xUnit `[CollectionDefinition("Todos")]`.
- `TodosFixture.cs` - inherits `ConstructionFixture`; seeds the WireMock backend with one
  open + one completed todo (mirror the Contacts fixture). Login is handled by the inherited
  fixture through the existing `Auth/` path - do not re-implement it.

### Step 2.2 - Page objects

Under `Modules/Todos/Pages/`:

- `ProjectTodosOverviewPage : OverviewPagePatternObject<ProjectTodosOverviewPage>` -
  `Name => "ProjectTodos"`. Exposes `Search`, `RetainedAlert`, `TodoList`, `Empty`, `Error`,
  `LoadingOverlay`, `RefreshingIndicator`, and `InspectItem(int index)`.
- `TodoInspectionPage : PageObjectBase<TodoInspectionPage>` - `Name => "ProjectTodoDetail"`.
  Buttons: `Edit`, `Complete`, `Reopen`, `Delete`, `Close`. Labels: `Name`, `Description`,
  `DueDate`, `Priority`, `Status`, `Assignees`.
- `TodoEditorPage : EditPagePatternObject<TodoEditorPage>` - `Name => "TodoEditor"`. Binds
  `Name`, `Description`, `DueDate`, `Priority`, `Employees` inputs.

### Step 2.3 - Containers

Under `Modules/Todos/Containers/`:

- `TodoListCollection : CollectionObjectBase<TodoListCollection, ProjectTodosOverviewPage, TodoItemContainer>`.
  Item id shape is set by `TodoListItemViewModel.AutomationId`.
- `TodoItemContainer : ItemContainerBase<TodoListCollection, TodoItemContainer>`. Exposes
  `Name`, `PriorityText`, `DueDateText`, `CompletionText`, `AssigneesText` labels.

### Step 2.4 - Tests

Under `Modules/Todos/Tests/`, all inheriting `ConstructionTestBase`:

- `TodoOverviewSmokeTests`
  - `Todos_ListRenders_ShowsSeededItems`
  - `Todos_Search_FiltersItems`
  - `Todos_EmptyState_ShowsWhenNoResults`
  - `Todos_ErrorState_ShowsOnBackendFailure`
- `TodoInspectionSmokeTests`
  - `Inspection_OpensFromRow_ShowsValues`
  - `Inspection_ButtonsShown_MatchState` (Open → Complete visible; Done → Reopen visible)
- `TodoEditorSmokeTests`
  - `Editor_OpensFromInspection_LoadsValues`
  - `Editor_Save_PostsToBackend`
  - `Editor_NameRequired_ShowsInlineError`
  - `Editor_DueDate_RoundTrips`
  - `Editor_Priority_RoundTrips`

Wire each test with `[Collection("Todos")]`, `[Trait("Category","Smoke")]`,
`[Trait("Module","Todos")]`.

### Step 2.5 - Run

```powershell
dotnet test Exact.Construction.UITests\Exact.Construction.UITests.csproj `
  --filter "Module=Todos" -v:minimal /nr:false
```

Green on Windows against the built Construction app + WireMock. **Phase 2 done.**

Update all three progress tables:
- [controls.md](controls.md) → mark T4-T11 done.
- [modules.md](modules.md) → mark Todos done.
- [plan.md](plan.md) → mark phase 2 done.

---

## Phase 3 - P1 modules

Order: **Authentication → Startup → Dashboard → Contacts**. Each module is a folder under
`Exact.Construction.UITests/Modules/`. Authentication, Contacts, Dashboard and Startup
folders already exist - this phase fills in gaps and converts the loose controls, it does
not create new projects.

### Per-module steps

Repeat for each module (see [modules.md](modules.md) for the per-module control list and
test list):

1. **Prep** - if the module needs a P1 loose-control conversion (Contacts needs C6+C7+C8+C9),
   run [convert-control](../../.github/skills/convert-control/SKILL.md) once per control,
   moving it from `Exact.Construction.UITests/Controls/` into `.tpl.cs`+`.gen.cs` under
   `Exact.Core.Construction.UITest/Controls/`. Update [controls.md](controls.md) progress.
2. **Add / complete the module folder** `Exact.Construction.UITests/Modules/<Module>/` with
   `<Module>Collection.cs`, `<Module>Fixture.cs` (inherits `ConstructionFixture`), `Pages/`,
   `Containers/`, `Tests/` as needed per [layout.md](layout.md).
3. **Reuse the existing Auth** for login (`ConstructionAuthenticationManager`). The
   Authentication module's own tests exercise the login screen itself; every other module
   just relies on the inherited fixture login.
4. **Add page objects / containers** matching the module's views.
5. **Add tests**. For modules with no upstream tests (Startup, Dashboard), add a navigation
   smoke: `<Module>NavigationSmokeTests` that opens the module from Dashboard and asserts
   the root AutomationId shows up.
6. **Run the module alone**:
   ```powershell
   dotnet test Exact.Construction.UITests\Exact.Construction.UITests.csproj `
     --filter "Module=<Module>" -v:minimal /nr:false
   ```
7. **Update [modules.md](modules.md) progress**.

### Phase 3 exit gate

All four modules green on Windows. No loose Construction controls remain (C6-C9 done in
[controls.md](controls.md)).

---

## Phase 4 - P2 library controls (on demand)

Each control follows the same [maui-control](../../.github/skills/maui-control/SKILL.md)
loop and lands in `Exact.Core.Construction.UITest/Controls/`. Trigger when a phase-5 module
needs it. Priority order derived from what phase 5 pulls in first:

1. **C11 FilterSheetControl** - the Contacts port in phase 3 already needs it. Promote to
   phase 3 during Contacts if not already there.
2. **C12 QuickActionsControl** - Projects / Files.
3. **C10 Cells (per shape)** - add per module.
4. **C13 DisplayFields (per shape)** - add per module.

Update [controls.md](controls.md) each time.

---

## Phase 5 - P2 module suites

Same per-module steps as phase 3, all under `Exact.Construction.UITests/Modules/<Module>/`.
Order: **Projects → Hours → Materials → Purchases → Equipment → Employees → Files →
Journal**. Journal deliberately last because it likely needs RichText (phase 6).

Each module's smoke suite is at minimum:

- A navigation smoke (open from Dashboard, assert root page).
- One list smoke (the module's overview page renders, empty/content states resolve).
- One create-or-edit smoke if the module has a create flow.

---

## Phase 6 - P3 controls and modules

Only start after phase 5 is green.

1. **C14 RichContentControl**, **C14b RichEditorControl** - probe the automation tree first
   (do we get anything usable through UIA, or must this be bridge-only?).
2. **C15 Settings/*** - one control per shape actually consumed by the Settings smoke.
3. Ship P3 module folders in [modules.md](modules.md) order as they're picked up.

---

## Phase 7 - Android

Windows-first through phases 0-6. Android is triggered when:

- The [TodoApp plan](../TodoApp/plan.md) reports the Android baseline for Selection and
  DateTime inputs stops failing (currently 0/8 Picker, 6/16 Range).
- The Todos suite on Windows is stable for one week.

Then enable Todos on Android first (the test project already multi-targets through the
Brinell Appium references), the way
[samples/Todo/tests/Brinell.Samples.Todo.UITests.Mobile](../../samples/Todo/tests/Brinell.Samples.Todo.UITests.Mobile)
does today.

---

## Working-agreement reminders

- **Do not run the full Construction matrix casually.** After a control change, run only the
  modules that reference it via `--filter "Module=<Module>"`. After a page-object change,
  run only that module.
- **One UI test process at a time.** Never run two Construction module filters in parallel.
- **Rebuild the Construction app if `Exact.Construction` changed.** `dotnet test` builds the
  **test** project, not the app it launches. A stale app binary passes or fails for reasons
  that no longer exist.
- **Reuse the existing Auth.** Login is `ConstructionAuthenticationManager` via the inherited
  fixture. Do not add a second login path.
- **No back-compat shims.** Per user preference, do not preserve shapes that fight the target
  layout. Add cleanly.
- **No `IMauiElement`, `Locator`, `FindElement` inside test methods** ([forbidden APIs](../../.github/skills/maui-ui-test/references/forbidden-apis.md)).
  Add or extend a page-object / control-object member instead.
