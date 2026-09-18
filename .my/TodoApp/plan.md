# Todo showcase app - plan

Goal: a small, realistic .NET MAUI Todo app, with its full test suite, that shows how to test
a MAUI app with Brinell across the whole test pyramid: unit, integration, hermetic UI (UI with a
mocked backend and a seeded database), live UI (a real backend) and manual. Every test traces back
to a user journey.

The app is the vehicle. The tests, and how they are split across the pyramid, are what it
demonstrates.

Related:

- [journeys.md](journeys.md): the journey backbone (`TOD.xx.y`) and the tier each subtask gets.
- Strategy: `../../../.my/testpyramide/test-pyramid-discussion.md` (5 tiers, Smoke, Reviews,
  "push down" rule, traits).
- Spec format: `../../../.my/testpyramide/Contacts/` (5-document bundle, `CON.xx.y` ids).
- Brinell rules: `AGENTS.md`, `docs/architecture/decisions.md` (AD-004, AD-005, AD-008).
- Skills: `maui-control` (component, container, collection), `maui-ui-test` (page objects).

Status: phases 0-2 implemented, 2026-09-18. See "Implementation notes" at the end for what building them changed.

---

## 1. What exists today, and the gaps

| Item | State | Consequence |
| --- | --- | --- |
| `srcnew/Brinell.Mocking` | `MockApiServer` and `ApiStubBuilder` are **empty placeholders**. WireMock.Net 2.15.0 is referenced. | Phase 0 fills them, mostly by lifting the generic parts of Construction's mock backend (next row). |
| **Construction's mock backend** (`construction/Exact.Construction.UITests/MockBackend`, `ConstructionFixture`) | Working WireMock setup, in use: see section 5a. | Prior art to reuse, not redesign. |
| `FlaUIMauiDriver(exe, args)` | Launches with `UseShellExecute=false` and adds `BRINELL_UIA_BRIDGE=1`, so the app **inherits the test process's environment**. Construction relies on this for `MOCK_SERVER_PORT`. | On Windows, settings for the app can go in environment variables set before launch, today. |
| `MauiDriverOptions` | No per-launch `Environment` / launch settings; Appium has `AdditionalCapabilities` only. | Needed for Android (intent extras); on Windows only a tidier alternative to process-global variables. |
| `MauiTestFixtureBase` | Launches the app once per collection. `Context.ResetAppState()` exists, but on FlaUI it only closes the app ("caller may need to recreate the driver"). | Per-test data isolation needs a real relaunch or an in-app reset (see section 6). |
| `Brinell.Maui.AppSupport` | Automation handlers + gesture bridge; copy-or-reference. | The Todo app references it, like the sample app. |
| CommunityToolkit `StateContainer` | Has a Brinell container control (toolkit plan, item 9). | Reused on the list page; no new work. |
| `Brinell.Uat` | Markdown `.uat.md` scenarios run against page objects. | Optional: journeys as executable specs (phase 6). |
| Packages | `Microsoft.Data.Sqlite` 10.0.12, `WireMock.Net`, `NSubstitute`, `Bogus`, CommunityToolkit.Maui 15.0.1 are centrally versioned. | Added: `Microsoft.Extensions.TimeProvider.Testing`. (`Mvc.Testing` was not needed: the API has its own in-process host.) No MVVM package: the MVVM base comes from `Brinell.Samples.Shared` (next row). |
| `samples/Brinell.Samples.Shared` | Pure `net10.0` MVVM library, used by the MAUI, WPF and Blazor samples: `ParentViewModel` (`SetProperty`, `IsBusy`, `BeginBusy`/`EndBusy`, `ViewVisible`, `OnViewAppearing`), `RelayCommand`, `AsyncRelayCommand(<T>)`, `TargetAsyncCommand` (single click), `INavigationService`. | The Todo view models derive from `ParentViewModel` and use its commands (D3). Its `INavigationService` is shaped for WPF (a view model container) and the MAUI sample does not use it; Todo keeps its own Shell navigator (see section 2). |
| Android | Driver baseline has gaps (Range 6/16, Picker 0/8 failing before any change). | Windows first. Android is a later phase and avoids Picker/DatePicker-heavy tests until the baseline improves. |

---

## 2. The app

### Features

| Screen / function | Contents |
| --- | --- |
| **List** | All todos with title, due date, status and a "not synced yet" marker. Status filter (All / Open / In progress / Done). Add button. Sync button (pull-to-refresh on Android). Loading / Empty / Error / Content states. |
| **Detail** | Title, notes, due / created / updated dates, sync state, status (changeable in place). Edit and Delete toolbar items. |
| **Edit / New** | Title (required, max 100 chars), notes, optional due date, status. Save / Cancel; "discard changes?" when cancelling a dirty form. |
| **Delete** | From the detail page, with a confirmation dialog. Soft delete locally (tombstone), then removed on the backend at the next sync. |
| **Persistence** | SQLite on the device. The UI always reads from local storage (offline first). |
| **Backend** | A small ASP.NET Core API that stores todos. The app syncs: it pushes pending local changes, then pulls the server's list. Conflicts: last writer wins on `UpdatedAt`. |

Status model: `Open -> InProgress -> Done -> Open` (the Next button cycles). **Overdue** is not
stored: it is derived and shown when a non-Done todo's due date is before today. That gives the
unit tier a real rule table to cover (see `TOD.05`).

Assumption: one todo list per user, no accounts. The backend takes a fixed API key header so the
integration tier has an auth header to check. Multiple lists or users are out of scope unless
asked for.

### Solution layout

The showcase is self-contained under `samples/Todo/` with its own solution, so a reader can see
"an app plus its test suite" in one place. It references Brinell by `ProjectReference`.

```text
samples/Todo/
  Brinell.Samples.Todo.slnx
  README.md                                  walkthrough: the pyramid, one journey top to bottom
  src/
    Brinell.Samples.Todo.Core/               net10.0, no MAUI: models, rules, view models
                                             (on Brinell.Samples.Shared), ITodoRepository,
                                             ISyncService, INavigator, IDialogs
    Brinell.Samples.Todo.Infrastructure/     net10.0: SQLite repository + migrations, HTTP API
                                             client, sync service, AddTodoServices(DI)
    Brinell.Samples.Todo.Contracts/          net10.0: DTOs + JSON options shared by app and API
    Brinell.Samples.Todo.Api/                ASP.NET Core minimal API (its own SQLite store)
    Brinell.Samples.Todo.App/                MAUI app: Shell, pages, custom views, AppSupport
  tests/
    Brinell.Samples.Todo.UnitTests/          tier 1
    Brinell.Samples.Todo.IntegrationTests/   tier 2 (+ API contract tests)
    Brinell.Samples.Todo.TestSupport/        scenarios, DB seeder, WireMock stubs (tiers 2-4)
    Brinell.Samples.Todo.UITests/            tiers 3 and 4, Smoke, Reviews (Brinell)
    Brinell.Samples.Todo.Uat/                optional: .uat.md journeys (phase 6)
  manual/
    charters.md                              tier 5: exploratory charters + manual checklist
```

Design rules that keep tests low in the pyramid:

- **View models live in `Core`, not in the MAUI project**, and talk to `INavigator` and
  `IDialogs` instead of `Shell` and `DisplayAlert`. Unit tests then cover every state transition
  without MAUI.
- **MVVM comes from `Brinell.Samples.Shared`**, like the MAUI and WPF samples: view models derive
  from `ParentViewModel` and use `SetProperty`, `IsBusy` and the relay commands. Save, Delete and
  Sync use `TargetAsyncCommand` (single click), so a double tap cannot save or sync twice
  (a unit-testable guard, `TOD.07`). The loading state is `IsBusy`, and the page's `ViewVisible` /
  `OnViewAppearing` is where the list reloads after returning from edit.
- **Navigation and dialogs are app-local abstractions in `Core`.** Shared's `INavigationService`
  navigates by view model through a container, which fits WPF but not MAUI Shell. Todo needs
  routes with parameters (`detail?id=`), so `INavigator` is a small Shell-backed interface
  (`GoToAsync(route, parameters)`, `GoBackAsync()`). Shared has no dialog abstraction; `IDialogs`
  (`ConfirmAsync`, `AlertAsync`) lives next to it. If a later sample needs the same, both can move
  into `Brinell.Samples.Shared`.
- **One DI composition, `AddTodoServices(settings)`**, called by both `MauiProgram` and the
  integration tests. The integration tier then tests the real wiring, not a copy of it.
- **`TimeProvider` is injected.** "Overdue" and timestamps are deterministic in every tier; the UI
  tiers pin the clock with a launch setting.
- **All runtime configuration comes from one `LaunchSettings` object**: DB path, API base URL,
  API key, fixed clock, sync-on-start, network-state file. On Windows it is read from environment
  variables; on Android from intent extras. Production builds read defaults only. This replaces
  Construction's `#if WINDOWS` + `CONSTRUCTION_USE_MOCK_BACKEND` switch in `App.xaml.cs` with one
  object that works on both platforms and that the integration tests can build directly.
- **Connectivity goes through `IConnectivity`**, and in test builds that can be overridden by a
  **network-state file** (`TODO_NETWORK_STATE_FILE`, containing `online` / `offline`). This is
  Construction's `NetworkStateOverride` pattern. It lets a UI test switch the app offline mid-test
  without the bridge.

### Screens and AutomationIds

These ids are the contract between the app and the page objects. They are written down before the
XAML, like the Contacts bundle's `02-screenflow-and-ui.md`.

```text
TodoListPage                                        [TodoList_Sync] [TodoList_Add]
+--------------------------------------------------------------------------+
| Filter: [TodoList_Filter  v]         Last sync: TodoList_LastSync         |
| TodoList_State (StateContainer: Loading | Empty | Error | Content)       |
|  Content: TodoList_Items (CollectionView)                                 |
|   +------------------------------------------------------------------+    |
|   | TodoRow_Title            TodoRow_Due    [TodoRow_Status]  (*)    |    |
|   +------------------------------------------------------------------+    |
|  Empty:  TodoList_EmptyLabel "Nothing to do"                              |
|  Error:  TodoList_ErrorLabel  [TodoList_Retry]                            |
+--------------------------------------------------------------------------+
  (*) TodoRow_Pending: shown while the row has unsynced changes

TodoDetailPage                                  [TodoDetail_Edit] [TodoDetail_Delete]
+--------------------------------------------------------------------------+
| TodoDetail_Title                                                         |
| [TodoDetail_Status]   <- status component, Next is enabled here           |
| TodoDetail_Info  (SectionCard)  Due / Created / Updated                   |
| TodoDetail_Notes (SectionCard)  notes text                                |
| TodoDetail_Sync  (SectionCard)  "Synced" | "Waiting to sync" | "Local"    |
+--------------------------------------------------------------------------+

TodoEditPage                                    [TodoEdit_Cancel] [TodoEdit_Save]
+--------------------------------------------------------------------------+
| TodoEdit_Basics (SectionCard)                                             |
|   TodoEdit_Title (Entry)      TodoEdit_TitleError                        |
|   TodoEdit_Notes (Editor)                                                |
| TodoEdit_Schedule (SectionCard)                                           |
|   TodoEdit_HasDue (Switch)    TodoEdit_Due (DatePicker)                  |
| [TodoEdit_Status]                                                        |
+--------------------------------------------------------------------------+
```

Navigation: Shell, root route `todos`, pushed routes `detail?id=` and `edit?id=` (no id = new).

---

## 3. The custom controls

The app has one control of each kind the `maui-control` skill knows. Each gets an app-side view
and a Brinell ControlObject in the UI test project (they are app-specific, so they do not belong in
`srcnew/Brinell.Maui`).

### Component: `TodoStatusView` -> `TodoStatus<TScope>`

A `ContentView` that is one control to the user, made of fixed parts the control names itself:

| Part id (fixed by the control) | View | Meaning |
| --- | --- | --- |
| `StatusGlyph` | Label | `○` Open, `◐` In progress, `●` Done, `⚠` Overdue |
| `StatusText` | Label | "Open", "In progress", "Done", "Overdue" |
| `StatusNext` | Button | Advances the status. Hidden when `IsReadOnly` (list rows). |

It is used read-only in every list row, and interactive on the detail and edit pages. Because it
repeats in rows, the part ids are only unique within the component root, which is exactly what a
component scopes. It needs the AppSupport `ContentView` handler to be addressable on Windows.

ControlObject (`ComponentObjectBase<TScope, TodoStatus<TScope>>`):

- parts: `Glyph` (Label), `Text` (Label), `Next` (Button);
- shortcuts: `GetStatus()` (read from `StatusText`), `Advance()`, `AdvanceTo(TodoStatusKind)`
  (at most three `Next` clicks, waiting for the text each time), `IsReadOnly()`;
- generated comparisons for status (`StatusEquals`, `WaitStatus`), per the generator contract.

No bridge verb: the button answers `InvokePattern` and the labels are text (AD-008, test 1).

Open item: check whether `CreateMaui.Bat` can generate `.gen.cs` for a test project. If it cannot,
write the ControlObject by hand in the component shape, and say so in the README.

### Container: `SectionCard` -> `SectionCard<TParent>`

A `Border` with a header label (`SectionHeader`) and app-chosen content. It is used five times
(`TodoDetail_Info`, `TodoDetail_Notes`, `TodoDetail_Sync`, `TodoEdit_Basics`, `TodoEdit_Schedule`).
The ControlObject is a `ContainerObjectBase` that scopes the controls inside it, so a page object
can say `Info.Label("DueValue")` without the id having to be unique on the page. It also gets a
`Header` read and `IsCollapsed`/`Toggle` if the card collapses (optional).

This is the example of "a region whose content the app chooses".

### Collection: `TodoList` + `TodoRow`

`CollectionObjectBase` + `ItemContainerBase`: row lookup by title, `Count`, `Titles()`,
`Row(title).Status` (the component inside an item), `Row(title).IsPending()`, `Row(title).Open()`.

### Reused: CommunityToolkit `StateContainer`

`TodoList_State` switches between Loading, Empty, Error and Content. The existing Brinell container
lets a test wait for a state (`WaitState("Empty")`) instead of guessing from which labels are visible.
It is also what makes the WireMock failure cases visible (500 -> Error, delay -> Loading).

---

## 4. The test pyramid for this app

Every test carries traits, so a report can compare journeys with tests:

```csharp
[Fact]
[Trait("Module", "Todo")]
[Trait("Journey", "TOD.02.3")]
[Trait("Pyramid", "Unit")]      // Unit | Integration | UiHermetic | UiLive
public void Save_WithEmptyTitle_ShowsTitleRequired() { ... }
```

Smoke and Reviews are extra traits (`[Trait("Gate", "Smoke")]`, `[Trait("Gate", "Review")]`) on
UI tests, not tiers of their own.

| # | Tier | Project | What is real | What is fake | Budget |
| --- | --- | --- | --- | --- | --- |
| 1 | **Unit** | `UnitTests` | View models, rules, sync merge, mapping | Repository, API client, navigator, dialogs (NSubstitute); `FakeTimeProvider` | < 10 ms/test, suite < 10 s |
| 2 | **Integration** | `IntegrationTests` | `AddTodoServices` DI, SQLite (temp file, real migrations), `HttpClient` pipeline, sync | Backend = WireMock (`Brinell.Mocking`); no UI | < 500 ms/test, suite < 30 s |
| 2b | **Contract** | `IntegrationTests` | The real `Todo.Api` in process (`TodoApiHost.StartAsync`, free port) | nothing | same |
| 3 | **UI hermetic** | `UITests` | The compiled app, real SQLite, real HTTP | Backend = WireMock; DB seeded before launch; clock pinned | 2-15 s/scenario, suite < 5 min |
| 4 | **UI live** | `UITests` (`Pyramid=UiLive`) | App + real `Todo.Api` process + real DBs on both sides | nothing | 30-60 s/scenario, nightly |
| 5 | **Manual** | `manual/charters.md` | Everything, on real devices | nothing | per release |

Here "live" means the real backend started by the test run on localhost. It stands in for an ACC
environment; the fixture takes the base URL from settings, so the same tests can point at a
deployed API later.

### Tier 1 - Unit

- `TodoListViewModel`: Loading -> Content / Empty / Error, filter, sync command guard (no double
  sync), refresh after returning from edit.
- `TodoEditViewModel`: validation (empty, whitespace, 100/101 chars), dirty tracking, Save calls
  the repository once, Cancel on a dirty form asks, and does not ask on a clean one.
- `TodoDetailViewModel`: delete asks for confirmation; "No" does nothing; "Yes" deletes and
  navigates back.
- `TodoStatusRules`: cycle order and the derived-Overdue table (status x due date x today, as a
  `[Theory]`).
- `SyncPlanner` (pure): given local rows + server rows, which to push, pull, delete, and who wins a
  conflict. This is the densest rule table and belongs here, not in the UI.

### Tier 2 - Integration (headless: WireMock + SQLite)

A fixture per class builds the real container with `AddTodoServices`, a unique temp DB file and a
`MockApiServer` on a free port; it deletes both on dispose.

- Migrations: fresh DB and v1 -> v2 upgrade keep data; tombstones survive restart.
- Repository: round trip of every field; UTC dates; filter queries.
- Sync against WireMock: push-then-pull order; pending flags cleared only after a 2xx; a 500 keeps
  changes pending; a timeout keeps changes pending and surfaces an error; tombstones become
  `DELETE` and are purged after 204/404; a second sync does not create duplicates; API key header
  present (checked with WireMock's request log).
- "Restart": dispose the container, build a new one on the same DB file, and state is unchanged.
- **Contract tests**: every canned response in `TestSupport` (the JSON that WireMock serves in
  tiers 2 and 3) is also requested from the real `Todo.Api` in process, and both deserialize to the
  same DTO shape. This keeps the mocks honest: if the API changes, the contract tests fail before the
  hermetic tests give false confidence.

### Tier 3 - UI hermetic (Brinell + WireMock + seeded SQLite)

Per test class, the fixture:

1. starts a `MockApiServer` and loads the scenario's server state as stubs;
2. creates a fresh DB file with the app's own migrations and seeds the scenario's local state
   (`TestSupport` references `Infrastructure`, so the schema can never drift from the app's);
3. launches the app with launch settings: `TODO_DB_PATH`, `TODO_API_BASEURL`, `TODO_API_KEY`,
   `TODO_NOW` (fixed clock), `TODO_SYNC_ON_START`, `TODO_NETWORK_STATE_FILE`. As in Construction,
   these are set in `CreateTestContextOptions()` before the base launches the app, and the app
   inherits them;
4. waits for `TodoListPage` and `TodoList_State` to leave Loading.

Tests go through page objects and ControlObjects only (the `maui-ui-test` rules: no driver, no
`IMauiElement`, no `Find*`). They synchronize on UI state and on **request observation**
(`mock.WaitForRequest(PUT /api/todos/{id})`, AD-004), never on sleeps.

What belongs here, one test per behaviour:

- list -> detail -> edit -> save -> detail shows the new values -> back -> list shows them;
- create a todo; it appears in the list with the pending marker, and after Sync the marker is gone
  and WireMock saw one `PUT`;
- one invalid title shows `TodoEdit_TitleError` (the other validation cases stay in unit tests);
- cancel with changes -> "discard?" dialog -> keep editing / discard;
- delete -> confirm -> row gone; delete -> cancel -> row still there;
- status: Next on the detail page cycles; the list row shows the same status;
- StateContainer: empty DB + empty server -> Empty; server 500 -> Error -> Retry after the stub is
  fixed -> Content; delayed server -> Loading is visible, then Content;
- offline (network-state file): Sync is disabled and shows "Offline", edits stay pending; back
  online, Sync clears them;
- filter shows only matching rows.

**Smoke** (`Gate=Smoke`, < 30 s): app launches, list reaches Content, open one detail.
**Reviews** (`Gate=Review`, < 90 s): visit list, detail, edit, empty, error once each; check
key controls exist and have AutomationIds; capture a screenshot per screen into the shared artifact
layout (AD-007); run `AccessibilityAudit` where it applies.

### Tier 4 - UI live (Brinell + real Todo.Api)

Same page objects, different fixture: it starts `Todo.Api` as a process on a free port with a fresh
server DB, and launches the app against it. Only happy paths that prove the real wire:

- create in the app -> sync -> the API (via its own HTTP endpoint, not the UI) has the todo;
- a todo created directly through the API appears after Sync;
- delete in the app -> sync -> the API returns 404 for it.

Tagged `live-api`; skipped unless `BRINELL_UAT_LIVE_API=1` (the existing UAT skip rule), run
nightly.

### Tier 5 - Manual

`manual/charters.md`: what is manual and why, not a copy of the automated tests.

- Real network loss during sync (airplane mode mid-sync on a phone).
- App killed during save; OS tombstoning on Android; rotation on the edit page.
- Soft keyboard covering `TodoEdit_Notes` on small screens.
- Visual/UX: glyph legibility, dark mode, font scaling 200 %.
- Exploratory charter: "try to create duplicates or lose an edit with two devices on one backend".

Each manual finding that can be reproduced is pushed down into an automated tier; the charter
records where it went.

---

## 5. Brinell changes this needs

These are framework changes, made in `srcnew/` with their own tests, before the showcase uses them.

| # | Change | Where | Notes |
| --- | --- | --- | --- |
| B1 | **Launch settings**: `MauiDriverOptions.LaunchSettings` (`IDictionary<string,string>`) and the same in `MauiOptions` / config JSON. FlaUI puts them in `ProcessStartInfo.Environment`; Appium passes them as Android intent extras (`appium:optionalIntentArguments`, `--es key value`). | `Brinell.Maui`, `.FlaUI`, `.Appium` | **Required for Android only.** On Windows, setting process variables before launch works today (Construction does it), so phases 1-6 do not wait for B1. With B1 the settings belong to one launch rather than the whole test process. No backward compatibility: just add it. |
| B2 | **`MockApiServer`**, lifted from Construction's `MockBackendHostManager` (section 5a): start on a free port, `BaseUrl`, ref-counted `Acquire`/`Release`, request log queries (`RequestCount(method, path)`, JSON / form / body predicates, `RequestBodies`). **Added:** `Reset()` between tests, and `WaitForRequest(method, path, predicate, timeout)`. | `Brinell.Mocking` | Replaces the placeholder. Tests in `Brinell.Mocking.Tests`. `WaitForRequest` is the AD-004 wait that Construction's count queries lack. |
| B3 | **`ApiStubBuilder`**: `Get(path).ReturnsJson(object)` (the `ConfigureListStub` shape), `.ReturnsSample(path)` (Construction's `ContractSampleBody`: a recorded response file), `.WithDelay(ms)`, `.Fails(500)`, "fails once, then succeeds" (WireMock scenario states), `.AtPriority(n)` for per-test overrides, `RequireHeader(name, value)`. | `Brinell.Mocking` | A thin fluent layer over WireMock; WireMock types stay available for anything unusual. |
| B3b | **Network-state file helper**: `NetworkStateFile.Create(path)`, `.SetOnline()`, `.SetOffline()`. | `Brinell.Mocking` (or `Brinell.Maui/Testing`) | The test side of Construction's `NetworkStateOverride`. The app side (a watcher behind `IConnectivity`) stays in each app, like the bridge sinks. |
| B4 | **Android host reachability**: helper that gives the backend URL the device should use (`10.0.2.2` on the emulator, or `adb reverse tcp:port`). | `Brinell.Mocking` or `Brinell.Maui.Appium` | Needed only when Android starts (phase 7). |
| B5 | **Fixture relaunch hook**: a supported way for a `MauiTestFixtureBase` subclass to (re)launch with new launch settings per class. | `Brinell.Maui/Testing` | **Not needed for phases 3-4:** a fixture overrides `CreateTestContextOptions()` to set up before launch, and an xUnit class fixture launches per class. Revisit only for per-test relaunch (section 6). |
| B6 | **Database bridge verb** - only if section 6's measurement says so. | `Brinell.Uia.Contracts`, AppSupport | See section 6. |

### 5a. Reusing Construction's WireMock setup

`construction/Exact.Construction.UITests` already runs Brinell UI tests against WireMock. What it
does, and what the showcase keeps or changes:

| Construction | Where | Showcase |
| --- | --- | --- |
| One static `WireMockServer`, ref-counted across fixtures, started in `CreateTestContextOptions()` before launch, stopped in `Dispose` | `MockBackendHostManager.cs`, `ConstructionFixture.InitializeMockBackend` | **Keep** the lifecycle; it moves into `MockApiServer` (B2). |
| Port handed to the app as `MOCK_SERVER_PORT`, inherited through `UseShellExecute=false`; app switches endpoints under `#if WINDOWS` | `MockBackendHostManager`, `Exact.Construction/App.xaml.cs` | **Keep** inheritance on Windows; replace the `#if` with `LaunchSettings` so Android works too (B1). |
| Default fixed port `9090` in config | `construction.maui.config.json` | **Change** to auto-assign (port 0): no clashes with a dev server or a leftover run. |
| One `partial` file of stubs per module (`.Contacts.cs`, `.Projects.cs`, ...), registered once at start | `MockBackend/MockBackendHostManager.*.cs` | **Keep** one stub file per API area in `TestSupport`, but load stubs **per scenario** and `Reset()` between tests. Construction's stubs build up for the whole run, so a test cannot start from a known server state. |
| Stubs built from anonymous objects (`new { id = 101, zip_code = ... }`) | `.Contacts.cs` | **Change** to the real DTOs from `Contracts`, so a renamed field breaks the build rather than the test run. |
| Recorded real responses served verbatim | `ContractSampleBody`, `MockBackend/ContractSamples/Bouw7/*.json` | **Keep**, and go one step further: the contract tests (`TOD.07.9`) check each sample against the real `Todo.Api`, so samples cannot go stale unnoticed. |
| Per-test variation by re-stubbing at a higher priority | `SetRequireHourLogTimes` (priority 1) | **Keep** as `.AtPriority(n)` in B3. |
| Assertions by counting logged requests (JSON / form / body predicates) | `GetRequestCount`, `GetJsonRequestCount` | **Keep** the queries; **add** `WaitForRequest`, because a count read right after a click races the app's async sync. |
| `NetworkStateOverride`: env var names a file; the app's `NetworkService` reads `online`/`offline` from it | `Diagnostics/NetworkStateOverride.cs`, `Common/Services/NetworkService.cs` | **Keep** (B3b + app side). It is the route for the offline journeys. |

Once B2/B3 exist, Construction could replace its host manager with `Brinell.Mocking` and keep only
its module stub files. That is a possible follow-up, not part of this plan.

---

## 6. Test data control: why not a database bridge (yet)

The question is how a UI test arranges the database and checks what was stored. Options:

| Option | Arrange | Assert | Platforms | Verdict |
| --- | --- | --- | --- | --- |
| A. **Seed the DB file before launch** (fixture writes it with the app's migrations, passes `TODO_DB_PATH`) | yes | - | Windows directly; Android by pushing the file into the debuggable app's data dir | **Use** for local-only state (pending changes, tombstones, offline). |
| B. **Seed through the backend** (empty DB, WireMock returns the scenario, app syncs on start) | yes | - | all | **Use by default**: goes through production code, works everywhere. |
| C. **Assert through the UI** (what the user sees; relaunch the app to prove persistence) | - | yes | all | **Use.** Persistence details are already covered in tier 2. |
| D. **Assert through WireMock's request log** (the app sent the right `PUT`) | - | yes | all | **Use**: the outgoing request is what the user's action is supposed to cause. |
| D2. **Control file** the app watches (Construction's network-state file) | runtime state (online/offline) | - | Windows; Android only through `adb` into the app's data dir | **Use** for connectivity. It would also work for "reset the data", but that is the same trade-off as E with more moving parts. |
| E. **Bridge verbs** `SeedData` / `ResetData` (app actions) | yes | - | Windows only (the bridge is UI Automation) | **Not now.** Only to avoid relaunches, and only if relaunching costs too much. |
| F. Bridge verb that **reads the DB** | - | yes | Windows only | **No.** It fails AD-008 test 3: an assertion that needs data the user cannot see belongs in the integration tier. |

So the default is **B + C + D**, with **A** for state the server cannot produce. No new bridge
verb is needed to start.

When E would be needed: if isolation requires a fresh app per test and a relaunch costs more than
about 3 s on Windows, a debug-only `ResetData(scenarioName)` app action saves that time. It passes
AD-008 test 2 (it arranges state; it is not the behaviour under test). Android would still need the
launch-time route, so E is an optimisation, not a replacement. Decide after measuring in phase 4:
record relaunch time vs. test time in the timing report, and add E only if relaunches are more
than about a third of the suite's time.

Scenarios are data, shared by every tier that needs them:

```text
tests/Brinell.Samples.Todo.TestSupport/Scenarios/
  empty.json                  { "server": [], "local": [] }
  three-todos.json            server has 3, local synced copy of the same 3
  pending-local-change.json   local edit not yet pushed
  overdue.json                due dates relative to the pinned clock
```

`TodoScenario.Load("three-todos").SeedDatabase(path)` and `.StubServer(mock)` are used by the
integration fixture and the UI fixture alike, so a scenario means the same thing in both tiers.

---

## 7. Phases

Each phase ends green in its own scope (the smallest tier that can falsify it); the full UI suite
runs at phase completion only. One UI test process at a time.

| Phase | Deliverable | Done when |
| --- | --- | --- |
| **0. Brinell groundwork** | B2, B3, B3b (lifted from Construction, section 5a) with tests in `Brinell.Mocking.Tests`; B5 if needed | `Brinell.Mocking.Tests` prove stubs, recorded samples, delays, fail-then-succeed, `Reset()` and `WaitForRequest`. B1 waits for phase 7. |
| **1. Domain, data, API** | `Core`, `Contracts`, `Infrastructure`, `Api`; `UnitTests`; `IntegrationTests` incl. contract tests; `TestSupport` scenarios | Tier 1 and 2 green; `journeys.md` subtasks for those tiers each have a test with a matching trait. |
| **2. The app** | MAUI app on Windows: Shell, three pages, `TodoStatusView`, `SectionCard`, StateContainer, AppSupport + bridge, `LaunchSettings` | App runs by hand against `Todo.Api`; every id in section 2 exists (checked with the Reviews crawl in phase 3). |
| **3. Test-side objects** | Page objects (`TodoListPage`, `TodoDetailPage`, `TodoEditPage`), `TodoStatus`, `SectionCard`, `TodoList`/`TodoRow`, hermetic fixture | Smoke + Reviews green on Windows. |
| **4. Hermetic UI tests** | Tier 3 tests from `journeys.md` | Tier 3 green; relaunch cost measured, decision on B6 recorded here. |
| **5. Live UI tests** | Live fixture (starts `Todo.Api`), tier 4 tests | Green with `BRINELL_UAT_LIVE_API=1`; skipped cleanly without it. |
| **6. Journeys as specs (optional)** | `Todo.Uat` with `.uat.md` scenarios for the main journeys, bound to the same page objects | The Gherkin-style journey text runs; shows that the spec does not decide the tier. |
| **7. Android** | B1, B4; Appium launch settings; run Smoke + a subset of tier 3 on the emulator | Smoke green on Android; known driver gaps listed rather than worked around. |
| **8. Close-out** | `samples/Todo/README.md` walkthrough, `manual/charters.md`, a journey-coverage report (traits vs `journeys.md`), link from `docs/README.md` / `samples/README.md` | A reader can follow one journey (`TOD.02` create) through all five tiers in the README. |

### Journey coverage report

A small test in `UnitTests` (or a script under `tools/`) reads `journeys.md`, collects the
`Journey` and `Pyramid` traits from the test assemblies, and fails when an automated subtask has no
test in its assigned tier. It also prints the matrix into the artifacts folder. This keeps the
pyramid allocation honest as the app grows.

---

## 8. Open decisions

| # | Question | Recommendation |
| --- | --- | --- |
| D1 | Location: `samples/Todo/` with its own solution, or split across `samples/` + `testsnew/` like the existing sample? | **Done:** `samples/Todo/` with `Brinell.Samples.Todo.slnx`. |
| D2 | Storage library: `Microsoft.Data.Sqlite` with hand-written migrations, or EF Core Sqlite? | **Done:** `Microsoft.Data.Sqlite`: small, no trimming issues on Android, and migrations are visible code to test. |
| D3 | MVVM library? | **Decided:** `Brinell.Samples.Shared` (`ParentViewModel`, relay commands), as the MAUI and WPF samples use. Navigation and dialogs stay app-local (section 2). |
| D4 | Database bridge verb (B6)? | Not now; decide after the phase 4 measurement (section 6). |
| D5 | "Live" backend: local `Todo.Api` process only, or also a deployed instance? | **Done:** local, in process (`TodoApiHost.StartAsync`) or as a process on `http://localhost:5080`; the base URL comes from settings, so a deployed one can be added later. |
| D6 | Multiple todo lists? | **Done:** one list. Add only if the showcase needs a master-detail collection example. |

---

## 9. Implementation notes (phases 0-2)

What building phases 0-2 settled or changed. Test counts are from the runs that closed each phase.

### Phase 0 - `Brinell.Mocking` (44 tests green)

- `MockApiServer` (owned `Start`, ref-counted `AcquireShared`, `Reset`, request queries with
  JSON / form / body predicates, `WaitForRequest(Async)` with a message listing what did arrive),
  `ApiStubBuilder` (`ReturnsJson/Text/Status/Sample`, `EchoesRequestBody`, `RespondsWith` callback,
  `WithHeader`, `WithQuery`, `WithDelay`, `AtPriority`, `FailsFirst`), `MockResponse`,
  `SampleFiles`, `RecordedRequest`, `NetworkStateFile`.
- **Target is `net10.0` only** (was net8/9/10 from `srcnew/Directory.Build.props`); the only
  consumers are .NET 10 test projects.
- **WireMock 2.15 scenario semantics, measured:** a mapping with no `WhenStateIs` matched again
  after the scenario moved on, and a mapping with no `WillSetStateTo` clears the state after it
  matches. `FailsFirst` therefore names every state explicitly, sets the start state with
  `SetScenarioState`, and has the recovered mapping set its own state again.
- `RespondsWith` is what makes a **stateful fake** possible; the Todo tests rely on it (below).

### Phase 1 - domain, data, API (unit 113, integration 27, green)

- **The fake backend is stateful** (`TestSupport/TodoServerStub`). A fake that always returns the
  scenario's list answers the pull after a push without the pushed todo, and the sync correctly
  concludes it was deleted on the server. The fake keeps a list and enforces the API key and
  last-writer-wins, like the real API.
- **Contract tests go further than section 4 planned**: besides the client against the real API
  and the recorded sample's shape, `TheWireMockFake_AnswersLikeTheRealApi` sends the same
  11-request script to the fake and to the real API and requires identical answers.
- `Mvc.Testing` was replaced by `TodoApiHost.StartAsync` (Kestrel on a free port in process),
  which phase 5 can reuse for the live tier.
- `ListState` rule: only a server error or a refused key is an Error state; an unreachable server
  or a timeout keeps the rows on screen and says so in the sync line (offline first).
- Journey gap: `TOD.08.7` (app killed during save; the integration half) has no test yet.

### Phase 2 - the app (Windows build green; Android build: see the phase summary)

- Shell with one root (`todos`) and pushed routes `detail` / `edit`; pages resolved from DI.
- **Toolbar items: the bridge is a fallback here, not a necessity.** The sample app measured that
  invoking a `ToolbarItem` through UI Automation does not raise its command under a
  `NavigationPage`. Under this app's Shell it did: Sync, Edit, Save and Delete all worked through
  plain `InvokePattern` in the manual run. Each page still declares `InvokeToolbarItem` and the
  navigation verbs (`PageAutomation.Declare`); phase 3 decides which route the ControlObjects use.
- **Manual run (Windows, real API, UIA only):** every id in section 2 is in the tree; list ->
  detail (row `SelectionItemPattern`) -> Next x2 (Overdue -> Done, "Waiting to sync") -> Edit ->
  empty title shows `TodoEdit_TitleError` -> save -> list shows the pending marker -> Sync clears it
  and the API has the edit -> Delete -> dialog (`PrimaryButton` / `SecondaryButton`) -> Empty state ->
  Sync deletes it on the server. For phase 3: `Switch` and `DatePicker` surface as `Button` in the
  tree; the WinUI dialog buttons are `PrimaryButton` / `SecondaryButton`.
- The status component is told `IsPastDue` rather than `IsOverdue`, so cycling Next shows Overdue
  correctly without a reload; `TodoStatusRules.Display(status, isPastDue)` is the shared rule.
- `LaunchSettings` is read from environment variables on every head for now; reading Android
  intent extras is part of phase 7 (B1).
