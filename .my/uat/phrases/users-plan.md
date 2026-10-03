# Todo users: plan

Status: **app and tests built** 2026-09-27 (phases 1-3 and the UI tests and UAT, section 6).
Section 8's assignment, "me", the assignee filter and the web head are still to do.

**Revised 2026-09-27:** build the app first. A user gets more fields (status, allowed-until date, role,
and a few more chosen to exercise field types). A todo is **not** linked to a user
yet: assignment, "my" todos and the assignee filter move to [Later](#8-later). So do the UI
tests and the UAT suite, which wait until phrase scopes and namespaces are built
([plan.md](plan.md), phase 8).

This plan now does two things:

1. **Users in the Todo app**: a list of users that anyone can add, edit and deactivate, synced
   through the server, on the MAUI head.
2. **A user form with one field of each kind** a test has to drive: text, multi-line text,
   email, whole number, decimal, date, optional date, time, a fixed list, a choice of a few,
   on/off, and a range. That makes the users pages the place where Brinell's controls and
   AD-016's value kinds get tested on a real app, later.

## 1. Goal

A person opens **Users** from the list, adds "Eva" as a Lead, allowed until 31 December,
38 hours a week, and saves. Eva shows in the list with her role and status. Offline, the
list is readable and editing is disabled. After a sync on another device, Eva is there too.

Rules this plan keeps:

- **Nothing that exists changes behaviour.** Todo MAUI UAT stays at 32 passing, Todo web UAT at
  33, and the Todo UI, unit and integration tests keep their counts. New columns and tables are
  additions with defaults, so existing test sets still load.
- **Journeys first.** Every new behaviour gets a `TOD.xx.y` id in `journeys.md` now, so the tests
  written later have their targets.
- **Testable from the start, tested later.** Every control gets an AutomationId in the existing
  `Page_Element` pattern, and the app has launch settings for every state a test will need
  (section 3, D6). Unit, integration and contract tests come with the code; only the tests that drive
  the UI (UI tests and UAT scenarios) come later, after phrases (Q1).
- **The app and the server share only the wire format** (the `ServerTodoStore` remark), so users
  get their own DTO and their own server table.

## 2. What exists

| Area | Today | Where |
| --- | --- | --- |
| Wire format | `TodoDto(Id, Title, Notes, DueDate, Status, CreatedAt, UpdatedAt)`, enums as names. The client chooses the id. `X-Api-Key` and `X-Todo-Client` headers. | `Contracts/TodoDto.cs` |
| Server | `ServerTodoStore` (SQLite, one `todos` table, one shared list), `ServerRequestLog` | `Todo.Api` |
| App storage | `TodoDatabase`, schema version 2 (`todos`, then `is_deleted`) | `Infrastructure/Data` |
| Sync | Push pending, pull all, and the newer `UpdatedAt` wins | `Core/Sync/SyncPlanner.cs`, `Infrastructure/Sync/SyncService.cs` |
| Todo edit page | `Entry` (title), `Editor` (notes), `Switch` + `DatePicker` (optional due date), status | `Pages/TodoEditPage.xaml` |
| Launch settings | `TODO_DB_PATH`, `TODO_API_BASEURL`, `TODO_NOW`, `TODO_CLIENT_ID`, ... | `Infrastructure/LaunchSettings.cs` |
| Test data | `server` and `local` halves per JSON set, validated by `testdata.schema.json` | `TestSupport/TestData` |
| Fake backend | `TodoServerStub` on WireMock | `TestSupport` |

The todo form already covers `Entry`, `Editor`, `Switch` and `DatePicker`. The user form adds the
controls it does not have, and repeats the common ones only where the value kind differs.

## 3. Decisions

### D1. The user

```csharp
public sealed record UserDto(
    Guid Id,
    string Name,
    string? Email,
    UserStatusDto Status,
    DateOnly? AllowedUntil,
    int HoursPerWeek,
    decimal HourlyRate,
    TimeOnly StartOfDay,
    UserRoleDto Role,
    bool ReceivesReminders,
    int Workload,
    string? Notes,
    DateTimeOffset UpdatedAt);
```

| Field | Type | Rule | Control | Value kind a test writes (AD-016) |
| --- | --- | --- | --- | --- |
| **Name** | text | required, at most 50 characters, unique among users that are not `Inactive`, ignoring case | `Entry` | text |
| **Email** | text | optional; one `@` with text on both sides and a dot after it | `Entry`, `Keyboard="Email"` | text |
| **Status** | `Active`, `OnLeave`, `Inactive` | default `Active`; see D2 | `RadioButton` group | enum (`On Leave`) |
| **Allowed until** | date, optional | none means no end; may be in the past (that is how access ends) | `CheckBox` "Has an end date" + `DatePicker` | date (`31-Dec-2026`, `+30d`) |
| **Hours per week** | whole number | 1-60, default 40, step 1 | `Stepper` with the value in a label | number |
| **Hourly rate** | decimal | 0-10,000.00, two decimals, shown as `1,250.75` | `Entry`, `Keyboard="Numeric"` | number (`1,250.75`) |
| **Start of day** | time | default 09:00 | `TimePicker` | time (`08:30`) |
| **Role** | fixed list: `Member`, `Lead`, `Admin` | required, default `Member`; recorded only, grants nothing (D5) | `Picker` | enum |
| **Receives reminders** | on/off | default on | `Switch` | on/off |
| **Workload** | 0-100 % | default 0, step 5 | `Slider` with the value in a label | number |
| **Notes** | multi-line text | optional, at most 500 characters | `Editor` | text |

Two new value kinds need `BddValues` later: **time** (`08:30`, 24-hour, invariant) and
**on/off** for a `Switch` (`on`/`off`, or the existing bool), added when the tests are written
(Q3). Department was considered and dropped (Q2): `Role` is the one fixed list.

### D2. Status and "allowed until" decide whether a user counts

A user is **available** when `Status` is `Active` **and** `AllowedUntil` is empty or today or
later (the app's clock, so `TODO_NOW` pins it). The list shows each user's state:

| Status | Allowed until | Shown as |
| --- | --- | --- |
| Active | none, or today or later | (nothing) |
| Active | before today | "Expired" |
| OnLeave | any | "On leave" |
| Inactive | any | "Inactive" |

- **Deactivate, never delete.** `Inactive` is the delete: it keeps the user's history for when
  todos reference users (section 8). A user can be set back to `Active`.
- **The name is unique among users that are not `Inactive`**, so a name can be reused once its
  old owner is inactive.
- "Available" is a pure rule in Core (`UserRules.IsAvailable(user, today)`), unit-tested at the
  boundaries (yesterday, today, tomorrow, none; each status).

### D3. Users are managed by the server

- **Changes need a connection.** The app sends them straight to the API
  (`PUT /api/users/{id}`, the client choosing the id as for todos) and caches the result. Offline, Add and Save are disabled and the page
  says why; the list is still readable from the cache. This is Construction's
  server-authoritative cached-entity model (`ICachedEntityRepository<T>` in
  `construction/AGENTS.md`), and keeps the offline sync rules for todos only.
- **The server validates too**: the same rules as the app, `400` with the field name for a bad
  value, `409` for a duplicate name, both as a validation problem document
  (`ValidationProblemDto`). The rules are `ServerUserRules` in Contracts, so the WireMock fake
  answers exactly as the API does. The app shows the server's message on the field.
- **A sync fetches users** (`GET /api/users`) before todos, and replaces the cached list only
  after a complete fetch.
- **Enums travel as names** (`"OnLeave"`, `"Lead"`), dates as `yyyy-MM-dd`, times as
  `HH:mm:ss` (System.Text.Json's default), the rate as a JSON number, as `TodoDto` does today.

### D4. Where it appears in the app

| Page | Change | AutomationIds |
| --- | --- | --- |
| Todo List | a **Users** toolbar item | `TodoList_Users` |
| **User List** (new) | the users (available first, then the rest, each group by name); each row shows name, role and the state from D2; **Add**; an offline notice; tapping a row opens it | `UserListPage`, `UserList_Add`, `UserList_Offline`, `UserList_Users` (the collection), row parts `UserRow_Name`, `UserRow_Role`, `UserRow_State` |
| **User Edit** (new) | the fields of D1 in three section cards (Person, Access, Work), Save, Cancel, and an error label per field that can fail | `UserEditPage`, `UserEdit_Name`, `UserEdit_Email`, `UserEdit_Status` (the radio group; options `UserEdit_Status_Active`, `_OnLeave`, `_Inactive`), `UserEdit_HasEndDate`, `UserEdit_AllowedUntil`, `UserEdit_Hours` (the stepper), `UserEdit_HoursValue`, `UserEdit_Rate`, `UserEdit_StartOfDay`, `UserEdit_Role`, `UserEdit_Reminders`, `UserEdit_Workload`, `UserEdit_WorkloadValue`, `UserEdit_Notes`, `UserEdit_Save`, `UserEdit_Cancel`, `UserEdit_<Field>Error` |

- One edit page for add and edit, as the todo edit page does.
- `UserEdit_AllowedUntil` is disabled while "Has an end date" is off, rather than hidden
  (`IsVisible=False` removes it from the automation tree, and a test must be able to see it is
  disabled).
- The Stepper and Slider each have a label echoing the value, so a test can read the value on
  every platform, as the sample app does.
- Save is enabled while the form is invalid; pressing it shows the errors. That keeps "why can I
  not save" visible to the user and to a test.

### D5. What users do not do yet

- **No link to todos.** No assignee, no "my todos", no filter by user (section 8).
- **No signing in.** No "me", no `TODO_USER`, no `X-Todo-User` header (section 8).
- **No permissions.** `Role` is recorded and shown, and grants nothing.

### D6. Storage, launch settings and test data

- **App schema version 3:** a `users` table with a column per field and `updated_at`. The
  migration is additive, and `MigrateAsync(2 -> 3)` is covered like the existing versions.
- **Server:** a `users` table in `ServerTodoStore` (`CREATE TABLE IF NOT EXISTS`, like `todos`)
  and the routes in `TodoApiHost`.
- **Test data schema:** top-level `users` (the server's) and `localUsers` (the device's cached
  copy; left out, the device has synced the server's; `[]`, it never fetched them). The old sets
  have no users and stay valid.
- **New test set `team`**: Daan (Lead, no end date), Eva (Member, allowed until a date after
  the pinned 10 March clock, 38 hours), Finn (Member, allowed until a date **before** the clock,
  so "Expired"), Gus (Admin, Inactive), Hana (Member, On leave, reminders off). Rates
  include one with thousands (`1,250.75`). Todos as in `one-todo`.
- `TodoServerStub` (WireMock) serves and accepts users, so the offline and server-error cases can
  be arranged later without the real API.

## 4. Journeys

Add to `journeys.md`, with the tiers from its legend. The H (UI) and UAT columns are filled in
later; U, I and C come with the build.

| Id | Subtask | Tier |
| --- | --- | --- |
| **TOD.11 Manage users** | | |
| TOD.11.1 | Users lists available users first, then the others, each with its state (Expired, On leave, Inactive) | U + H |
| TOD.11.2 | Add with valid values shows the new user; the server has them | H + C |
| TOD.11.3 | Each field refuses a bad value with a message on that field: empty or too-long name, duplicate name, bad email, hours outside 1-60, a rate with three decimals or over the limit, notes over 500 characters | U + H |
| TOD.11.4 | Editing a user changes every field shown on the list and on reopening | H |
| TOD.11.5 | "Has an end date" off disables the date and saves no end date; on, the date is saved | U + H |
| TOD.11.6 | A user whose end date has passed shows as Expired; tomorrow's does not (pinned clock) | U + H |
| TOD.11.7 | Setting a user Inactive keeps them in the list as Inactive; their name can be reused | U + H + C |
| TOD.11.8 | Offline, Add and Save are disabled and the page says so; the list still shows | U + H |
| TOD.11.9 | A user added on one device shows on another after its sync | I + H |
| TOD.11.10 | The server refuses what the app would refuse, naming the field (a client that skips the app's checks) | C |

## 5. Steps

Commands run from the Brinell root unless marked otherwise. The Todo solution is
`samples/Todo/Brinell.Samples.Todo.slnx`.

### Phase 1 - Domain, wire and server (no UI)

| # | Step | Tests |
| --- | --- | --- |
| 1.1 | `UserDto`, the two enums (status, role), and the routes in `Contracts`. | Contract tests (C): the round trip of every field, enums as names, date/time/decimal formats. |
| 1.2 | `ServerTodoStore` users; `TodoApiHost` routes; the validation rules on the server (`400` naming the field, `409` for a duplicate). | Contract tests against the in-process API (TOD.11.10, TOD.11.7). |
| 1.3 | Core: `User`, `UserValidation` (one rule per field, returning the field and the message), `UserRules.IsAvailable`, list ordering and state text. | Unit (U): each rule at its boundaries (50/51 characters, 1/60/0/61 hours, 2/3 decimals, 10,000.00/10,000.01), duplicate names ignoring case and ignoring inactive users, availability on yesterday/today/tomorrow/none for each status, ordering. |
| 1.4 | Infrastructure: schema v3, `SqliteUserRepository`, `TodoApiClient` users calls, sync fetching users first and replacing the cache only after a complete fetch. | Integration (I): migration 2 -> 3, a sync that brings a new user (TOD.11.9), an interrupted fetch leaving the old cache. |
| 1.5 | Test data schema, the `team` set, `TodoServerStub` users. | `TestDataTests` load every set; the old sets are unchanged. |

Verify: Todo unit, integration and contract tests, then the Todo MAUI UAT (32) and web UAT (33),
unchanged.

### Phase 2 - The MAUI app

| # | Step | Evidence |
| --- | --- | --- |
| 2.1 | ViewModels: `UserListViewModel`, `UserEditViewModel` (Fody, errors per field, Save enabled while invalid, offline state). | Unit tests on the VMs: errors appear on Save, the end date follows the checkbox, offline disables Add and Save. |
| 2.2 | XAML: the Users toolbar item, User List and User Edit pages, AutomationIds from D4. | Launch on Windows: visual tree has every AutomationId, the disabled date is in the tree, a screenshot of both pages in light and dark with the `team` set, and one of the edit page with every error showing. |
| 2.3 | Android build of the app (no Android tests). | The app builds for `net10.0-android`; one launch on the emulator if one is attached, otherwise stated as not run. |

Verify: the existing Todo UI tests (36, 3 skipped) and both Todo UAT suites, unchanged.

### Phase 3 - Docs

`samples/Todo/README.md` (the users feature, the `team` set, a note that users have no UI tests
yet), `journeys.md` (section 4), and `CHANGELOG.md`.

## 6. Progress

| Step | Status | Evidence |
| --- | --- | --- |
| 1.1-1.5 | done 2026-09-27 | `UserDto`, `ValidationProblemDto`, `ServerUserRules` (Contracts); `/api/users` in `TodoApiHost`; `User`, `UserValidation`, `UserRules`, `UserService` (Core); schema v3, `SqliteUserRepository`, `TodoApiClient : IUserApi`, users first in `SyncService` (Infrastructure); `team.json`, `TodoServerStub` users, schema regenerated. Unit 193 passed (Todo), integration and contract 59 passed. |
| 2.1 | done 2026-09-27 | `UserListViewModel`, `UserEditViewModel` with their unit tests. |
| 2.2 | done 2026-09-27 | Launched on Windows with `team` seeded: the list shows Daan, Eva, then Finn (Expired), Gus (Inactive), Hana (On leave); Eva's edit page loads every field (rate `1,250.75`, `38 hours`, `35 %`); Save with a blank name, `eva@` and rate `1,5` shows the three errors on their fields; a valid Ivo reaches the real API with every field and shows in the list. Every AutomationId of D4 is in the tree, the disabled end date included. **On Windows the Stepper is two buttons, `UserEdit_HoursMinus` and `UserEdit_HoursPlus`; `UserEdit_Hours` itself is not in the tree.** Dark mode not checked. |
| 2.3 | done 2026-09-27 | Builds for `net10.0-android`; not launched (no emulator attached). |
| 3 | done 2026-09-27 | README, journeys (TOD.11.1-11.10, U/I/C tiers; `JourneyCoverage` green), CHANGELOG. |
| Verify | done 2026-09-27 | Todo UI 36 passed (3 skipped), MAUI UAT 32, web UAT 33, web UI 10 (1 skipped), mixed UI 4 (1 skipped), mixed UAT 4; `Brinell.sln` and the Todo solution build. |
| Tests | done 2026-09-27 | `UserListPage`, `UserEditPage` and the `UserList` collection in `Todo.UITests`, in the `Users` namespace; `UsersJourneyTests` (8, TOD.11.1-11.9, H); `TOD.11-manage-users.uat.md` (8 scenarios, `Using | Users`) and the expected failure `ExpectedFailures/TOD.11-without-using.uat.md` (UATB010); `UserPhrases` (`Users`). The date and time pickers publish `SetDate`/`SetTime` through the gesture bridge: they have no UI Automation route on Windows. Q3: `on`/`off` reads as a bool (time already did); AD-016 lists both. Kept in the existing Todo UI test and UAT projects rather than two new ones: one fixture and one collection more, no second app launch per run. |
| Later | todo | Section 8: assignment, "me", the assignee filter, the web head. |

## 7. Decided questions

Answered 2026-09-27.

| # | Question | Decision |
| --- | --- | --- |
| Q1 | Which tests wait? | Only the tests that drive the UI: the H-tier UI tests and the UAT scenarios. Unit, integration and contract tests come with the code. |
| Q2 | `Role` and `Department`, two fixed lists? | Department is dropped. `Role` is the one fixed list in a `Picker`. |
| Q3 | Time and on/off as `BddValues` kinds | Yes, added when the users tests are written, with AD-016 amended: `08:30` (24-hour, invariant) and `on`/`off`. |
| Q4 | Is an `OnLeave` or expired user editable? | Yes. Every user is editable; the state only affects "available". |
| Q5 | The web head | Later, with the tests. The Razor pages use the same `data-testid` values as the AutomationIds, so the scenarios will serve both heads (AD-014). |
| Q6 | Any other field types? | No. Left out: a colour (no MAUI colour picker), an image or avatar (file picking is OS UI outside the app), a multi-select list (assignment will bring a collection of CheckBoxes, section 8), a date and time together (no MAUI control; `UpdatedAt` shows as a read-only label, which covers reading one). |

## 8. Later

These were in the first draft of this plan and wait until the users app exists and phrase scopes
and namespaces are built. The decisions below are the starting point, not settled.

- **Assignment.** `TodoDto.AssigneeIds` (a list, sorted by name when shown), syncing with the todo
  under the newer-`UpdatedAt` rule. An unknown id is kept and shown as "Unknown user". The API
  keeps assignees when the field is **missing** and replaces them when present, even `[]`. Only
  available users can be assigned; an inactive or expired assignee stays on the todo, marked.
  The edit page gets an "Assigned to" section with a CheckBox row per available user
  (`TodoEdit_Assignees`), which exercises Brinell's collection and toggle controls.
- **"Me".** A **Sign in as** action on a user row, `TODO_USER` as a launch setting, "Signed in as"
  on the list (`TodoList_Me`), and `X-Todo-User` recorded by the API. No passwords. The
  multi-app `Apps` table's `User` column fills `TODO_USER`, so `Daan` in a step is both the app
  and the signed-in user.
- **The assignee filter.** A second Picker (`TodoList_Assignee`): Everyone, Mine, Unassigned, each
  available user, and perhaps each role. It combines with the status filter.
- **Journeys TOD.12 (assign a todo) and TOD.13 (filter by assignee)**, as in the first draft at
  [7162ac7](https://github.com/exact-mobile-apps/test-tooling/blob/7162ac7/.my/uat/phrases/users-plan.md#4-journeys).
- **The users test projects.** `Brinell.Samples.Todo.Users.UITests` (page objects `UserListPage`,
  `UserEditPage`, H-tier tests) and `Brinell.Samples.Todo.Users.Uat` (scenarios for TOD.11-13 with
  `Using | Users`, phrases in a `Users` namespace), plus the expected-failure proof that a file
  without `Using` fails binding. They are the first real use of phrase namespaces
  ([plan.md](plan.md), phase 8); that work, and its AD (AD-019), belongs with the phrases plan
  rather than here.
- **The web head**, for parity (Q5).

## 9. Risks

- **The user form exercises controls the Todo app has not used**: `RadioButton`, `Stepper`,
  `Slider`, `TimePicker`, `CheckBox` enabling a `DatePicker`. Some may show gaps in Brinell's
  controls, especially on Android (Picker is still broken there, [the Android plan](https://github.com/exact-mobile-apps/test-tooling/blob/ec96de1f/.my/android/plan.md) A1; Stepper
  and Slider are A2-A3). That is the point of the fields; the gaps are fixed in Brinell's controls
  when the tests are written, never worked around in the app.
- **`decimal` and `TimeOnly` in SQLite** have no native type. Store the rate as integer cents and
  the time as minutes since midnight, and test the round trip in 1.4.
- **The schema version 3 migration** runs on every existing device database. It is additive and
  tested.
- **A date in the past is a valid value** for "allowed until". The DatePicker must not set a
  minimum date, or ending someone's access would need a date in the future.
