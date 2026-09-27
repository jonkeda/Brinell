# Todo users, assignment and "my" todos: plan

Status: **todo**, drafted 2026-09-26. No step has started.

This plan does four things:

1. **Users in the Todo app.** A list of users that anyone can add to, rename and deactivate.
2. **Assignment.** A todo can have any number of assigned users.
3. **An assignee filter** on the list: Everyone, Mine, Unassigned, or a single user. It works
   together with the status filter.
4. **A separate UI test project and UAT project** for the users feature. Its phrases and pages
   live in their own `Users` namespace, which scenarios bring in with `Using`. This is the first
   real use of [phrase-namespaces.md](../uat/phrases/phrase-namespaces.md), which is not built yet,
   so this plan builds the minimum of it.

It works alongside [plan.md](plan.md), the plan for several apps, heads and named users in one
scenario. Neither plan needs the other, but together they let `Daan` in a step be both the app's
signed-in user and the app Daan is using (section 3, D9).

Decisions are in section 3. Each step's status and evidence go in section 6.

## 1. Goal

These scenarios run on the MAUI head (Windows), each in the new `Brinell.Samples.Todo.Users.Uat`
suite:

```markdown
| Field | Value |
| --- | --- |
| Journey | TOD.12 |
| TestData | team |
| Using | Users |
```

```gherkin
@TOD.12.1
## Scenario: Assigning a todo to Eva shows her on the row

Given I am signed in as "Daan"
And I am on the Todo List page
When I open the todo "Buy milk"
And I tap Edit
And I assign the todo to "Eva"
And I tap Save
Then the todo "Buy milk" should be assigned to "Eva"

@TOD.13.2
## Scenario: Mine shows only my todos

Given I am signed in as "Eva"
And I am on the Todo List page
When I select "Mine" from Assignee
Then Todos should have todo "Book dentist"
And Todos should not have todo "File taxes"
```

The same steps without `Using | Users` fail at **bind time**, with an error that names the
`Users` namespace as the one that has `I am signed in as {user}`.

Rules this plan keeps:

- **Nothing that exists changes behaviour.** Todo MAUI UAT stays at 32 passing, Todo web UAT at
  33, and the Todo UI, unit and integration tests keep their counts. The new columns and tables
  are additions with defaults, so existing test sets still load.
- **Journeys first.** Every new behaviour gets a `TOD.xx.y` id in `journeys.md` and a test at the
  lowest tier that can see it (the pyramid rule at the top of that file).
- **AutomationIds** follow the existing `Page_Element` pattern (`UserList_Add`, `TodoEdit_Assignees`).
- **The app and the server share only the wire format** (the `ServerTodoStore` remark), so users
  get their own DTO and their own server table.
- **Brinell rules hold:** no driver calls in tests, no sleeps, xUnit `Assert`, and one UI test
  process at a time.

## 2. What exists

| Area | Today | Where |
| --- | --- | --- |
| Wire format | `TodoDto(Id, Title, Notes, DueDate, Status, CreatedAt, UpdatedAt)`. The client chooses the id. `X-Api-Key` and `X-Todo-Client` headers. | `Contracts/TodoDto.cs` |
| Server | `ServerTodoStore` (SQLite, one `todos` table, one shared list), `ServerRequestLog` | `Todo.Api` |
| App storage | `TodoDatabase`, schema version 2 (`todos`, then `is_deleted`) | `Infrastructure/Data` |
| Sync | Push pending, pull all, and the newer `UpdatedAt` wins | `Core/Sync/SyncPlanner.cs`, `Infrastructure/Sync/SyncService.cs` |
| Filter | `TodoFilter { All, Open, InProgress, Done }`, one Picker (`TodoList_Filter`) | `Core/Rules/TodoListRules.cs`, `TodoListViewModel.FilterIndex` |
| Launch settings | `TODO_DB_PATH`, `TODO_API_BASEURL`, `TODO_NOW`, `TODO_CLIENT_ID`, ... | `Infrastructure/LaunchSettings.cs` |
| Test data | `server` and `local` halves per JSON set, validated by `testdata.schema.json` | `TestSupport/TestData` |
| Fake backend | `TodoServerStub` on WireMock | `TestSupport` |
| UAT namespaces | **Not built.** Every phrase and page in a suite is visible to every file, and duplicate page names are a diagnostic. | `Brinell.Uat` |

## 3. Decisions

### D1. A user is small and managed by the server

```csharp
public sealed record UserDto(Guid Id, string Name, bool IsActive, DateTimeOffset UpdatedAt);
```

- **`Name`** is required, at most 50 characters, and unique among active users, ignoring case.
  Scenarios and the assignee filter address users by name.
- **Deactivate, never delete.** A deactivated user keeps their assignments and name on existing
  todos, shown as "Eva (inactive)". They drop out of the assign list and the filter. Deleting a
  user would need the server to rewrite every todo that names them, which is a second sync problem
  for no user gain.
- **Changes to users need a connection.** The app sends them straight to the API
  (`POST`/`PUT /api/users`) and caches the result. Offline, Add, Rename and Deactivate are disabled
  and the page says why. The list is still readable from the cache. This follows Construction's
  server-authoritative cached entities (`ICachedEntityRepository<T>` in `construction/AGENTS.md`),
  and keeps the offline sync rules for todos only.
- **A sync fetches users** (`GET /api/users`) before it fetches todos, and replaces the cached list
  only after a complete fetch.

### D2. A todo has any number of assignees

`TodoDto` gains `IReadOnlyList<Guid> AssigneeIds`, which defaults to empty. `TodoItem` gains the
same. Assignment is part of the todo, so it syncs with the todo under the existing rule (the newer
`UpdatedAt` wins), and changing it makes the todo `Pending`.

- The order of assignees is not meaningful. They are stored and shown sorted by name.
- An id the app does not know (a user created elsewhere and not synced yet) is kept. It shows as
  "Unknown user" until the next sync fetches the user. It is never dropped: a device that has not
  synced users must not un-assign someone.
- **Server compatibility:** an old client that sends no `assigneeIds` must not clear them. The API
  keeps the stored assignees when the field is **missing**, and replaces them when it is present,
  even as `[]`. A contract test pins this.

### D3. "Me" is the signed-in user, chosen in the app

There are no passwords and no login. The API key stays the only secret. "Me" is a local setting:

- The Users page has a **Sign in as** action on each row. The signed-in user shows in the list
  header ("Signed in as Daan", `TodoList_Me`).
- A new launch setting, **`TODO_USER`** (a name), selects a user at start, as `TODO_NOW` pins the
  clock. Tests use it, and the multi-app plan fills it from the `Apps` table (D9).
- Nobody signed in: **Mine** is disabled in the filter, and a new todo has no assignees.
- **A new todo is assigned to its creator by default.** Open question Q2.
- The API receives `X-Todo-User` (the user id) beside `X-Todo-Client`, so the request log can say
  who made a change. It is recorded only and gives no authorisation. It lets the multi-app plan's
  "the server received it from Daan" check a user rather than a device.

### D4. The assignee filter is a second picker

`TodoListRules` gains `AssigneeFilter`:

| Choice | Shows |
| --- | --- |
| Everyone (default) | All todos |
| Mine | Todos assigned to the signed-in user |
| Unassigned | Todos with no assignees |
| *each active user, by name* | Todos assigned to that user |

- It combines with the status filter (**both** must match). `Arrange(items, statusFilter,
  assigneeFilter)` stays pure and is unit-tested exhaustively.
- **A second Picker, `TodoList_Assignee`**, not one merged picker: the status filter's four entries
  and its `SelectByText` tests stay exactly as they are.
- The Empty state text says which filters are on ("No todos assigned to you"), so an empty Mine
  view is not confused with an empty list. A new `ListState` is not needed.
- The list remembers the assignee choice across page appearances, as it remembers the status
  filter today.

### D5. Where it appears in the app

| Page | Change | AutomationIds |
| --- | --- | --- |
| Todo List | "Users" toolbar item; "Signed in as" line; Assignee picker; each row shows its assignees' names | `TodoList_Users`, `TodoList_Me`, `TodoList_Assignee`, `TodoRow_Assignees` |
| Todo Detail | "Assigned to" line | `TodoDetail_Assignees` |
| Todo Edit | An "Assigned to" section card with one CheckBox row per active user | `TodoEdit_Assignees` (the section), rows keyed by user name |
| **User List** (new) | The users (active first, then inactive), Add, a row per user with Sign in as, Rename and Deactivate/Activate, and an offline notice | `UserListPage`, `UserList_Add`, `UserList_Offline`, `UserList_Users` (the collection) |
| **User Edit** (new) | Name, Save, Cancel, and a validation error | `UserEditPage`, `UserEdit_Name`, `UserEdit_NameError`, `UserEdit_Save`, `UserEdit_Cancel` |

The edit page's assignee list is a collection of toggle rows. This deliberately exercises
Brinell's collection and toggle controls on a real page: `CheckItem("Eva")` and
`AssertItemChecked`, with no locators in tests.

### D6. Storage and test data

- **App schema version 3:** a `users` table (`id`, `name`, `is_active`, `updated_at`) and
  `todo_assignees(todo_id, user_id)`, plus a `settings(key, value)` row for the signed-in user.
  The migration is additive, and `MigrateAsync(2 → 3)` is covered like the existing versions.
- **Server:** the `users` and `todo_assignees` tables in `ServerTodoStore`, and the new routes in
  `TodoApiHost`.
- **Test data schema:** `server.users[]`, `assigneeIds` on server todos, `local.users` (whether
  the device has synced users), and `local.signedInAs`. The old sets have no users and stay valid.
- **New test set `team`**: users Daan, Eva, Finn (active) and Gus (inactive). Buy milk assigned to
  Daan; Book dentist to Eva; Plan trip to Daan and Eva; File taxes to Gus; Water plants to nobody.
  The due dates reuse the pinned 10 March clock.
- `TodoServerStub` (WireMock) serves users, and `TodoDataPhrases` gains
  `the server should have user {name}` and `the todo {title} should be assigned to {user} on the server`.

### D7. A users UI test project and a users UAT project

```text
samples/Todo/tests/
  Brinell.Samples.Todo.Users.UITests/      page objects + H-tier UI tests for the users feature
    Pages/UserListPage.cs, UserEditPage.cs
    Tests/UserManagementTests.cs, AssignmentTests.cs, AssigneeFilterTests.cs
    AssemblyInfo.cs   [assembly: UatNamespace("Users")]
  Brinell.Samples.Todo.Users.Uat/          the users UAT suite
    uat.config.md     Head maui, Using: (none project-wide)
    Phrases/UserPhrases.cs     [UatPhraseClass(Namespace = "Users")]
    Scenarios/TOD.11-manage-users.uat.md
    Scenarios/TOD.12-assign-a-todo.uat.md
    Scenarios/TOD.13-filter-by-assignee.uat.md
    Runtime/TodoUsersUatFixture.cs
```

- **Why a separate UI test project:** the users pages are one team's slice of the app. Their
  page objects, tests and namespace ship together. This is the "phrases in separate projects"
  case in `phrase-namespaces.md` section 3.
- **The existing Todo pages and phrases stay global,** with no namespace, so the 32 existing
  scenarios need no `Using`.
- The users UI tests reuse `TodoAppFixtureBase` (the `team` set, `TODO_USER`) through a project
  reference to `Todo.UITests`. They do not copy it.
- The Todo list and edit page objects gain the new controls (`Assignee`, `Me`, `Assignees`) in
  `Todo.UITests`, because those pages are the Todo team's. Only the new pages live in the users
  project.

### D8. The minimum of UAT namespaces

This is the recommendation in `phrase-namespaces.md`, cut down to what D7 needs. Folder-based
visibility and callable phrases stay out.

| Piece | Behaviour |
| --- | --- |
| `[assembly: UatNamespace("Users")]` | Every phrase class and page in the assembly belongs to `Users`. |
| `[UatPhraseClass(Namespace = "Users")]` | One class belongs to `Users`, and overrides the assembly's namespace. |
| No namespace | **Global**, which is today's behaviour. Always visible. |
| `Using` metadata row (comma-separated) | The file also sees those namespaces. One per file, not per scenario. |
| `## Using` in `uat.config.md` | Every file of the suite sees those namespaces. |
| Binding | A pattern or page of a namespace the file does not use does not match. A step that only such a pattern would match fails with `UATB006` ("... is in namespace 'Users'; add `Using \| Users`"). |
| Ambiguity | Two visible matches are an error, as today. Namespaces never shadow each other. |
| Unknown `Using` | Error `UATB007`: no loaded assembly provides that namespace. |
| Unused `Using` | Warning `UATB008`. |
| Pages | Page names are unique **within** a namespace. The same name in two namespaces is allowed, and ambiguous only in a file that uses both. |
| Report | The bind report and `__result.json` name each step's namespace (`Users: I am signed in as {user}`). |
| Control verbs | They have no namespace of their own. They belong to their page's namespace, because they are reached through a page. |

The spec catalog and the runtime catalog both filter by namespace, so the parity tests keep holding.

**New ADR, AD-016 "Phrases and pages belong to namespaces a file uses".** It covers only the
pieces above, and lists folder-based visibility as not decided.

### D9. How this meets the multi-app plan

- The `Apps` table's `User` column in [plan.md](plan.md) D4 becomes the app's `TODO_USER`. So in
  `When Daan taps Save`, "Daan" is both the app and the signed-in user.
- A `{user}` phrase argument (plan.md D3) is checked against the Todo app's users when the phrase
  asks for it (`the todo {title} should be assigned to {user}`).
- The mixed suite's scenario (c) in plan.md becomes "Daan assigns a todo to Eva on Windows, and
  Eva's phone shows it under Mine".

Neither plan waits for the other. The users UAT suite here is one-head and one-app.

## 4. Journeys

Add these to `journeys.md`, with the tiers from its legend:

| Id | Subtask | Tier |
| --- | --- | --- |
| **TOD.11 Manage users** | | |
| TOD.11.1 | Users lists active users first, then inactive ones marked inactive | U + H |
| TOD.11.2 | Add with a valid name shows the new user; the server has them | H + C |
| TOD.11.3 | An empty, too-long or duplicate name is refused with a message | U + H |
| TOD.11.4 | Rename changes the name on every todo that user is assigned to | H |
| TOD.11.5 | Deactivate removes the user from the assign list and the filter; their todos keep the name, marked inactive | U + H |
| TOD.11.6 | Offline, user changes are disabled and the page says so; the list still shows | U + H |
| TOD.11.7 | Sign in as a user shows "Signed in as ..." on the list; `TODO_USER` does the same at start | H |
| **TOD.12 Assign a todo** | | |
| TOD.12.1 | Assigning users on the edit page shows them on the row and the detail | H |
| TOD.12.2 | A new todo is assigned to whoever is signed in | U + H |
| TOD.12.3 | Assignment syncs, and the other device shows it after its sync | I + H |
| TOD.12.4 | An old client that sends no `assigneeIds` does not clear them | C |
| TOD.12.5 | An assignee the device does not know shows as "Unknown user" and survives a sync | I |
| TOD.12.6 | Assignment conflict: the newer `UpdatedAt` wins, as for every other field | U + I |
| **TOD.13 Filter by assignee** | | |
| TOD.13.1 | Everyone, Mine, Unassigned and each active user filter the rows | U + H |
| TOD.13.2 | Mine shows only the signed-in user's todos; with nobody signed in, Mine is disabled | U + H |
| TOD.13.3 | The assignee and status filters combine | U + H |
| TOD.13.4 | An empty filtered list says which filter is on | U + H |
| TOD.13.5 | The assignee choice survives opening a todo and coming back | H |

## 5. Steps

Commands run from the Brinell root unless marked otherwise. The Todo solution is
`samples/Todo/Brinell.Samples.Todo.slnx`.

### Phase 1 - Domain, wire and server (no UI)

| # | Step | Tests |
| --- | --- | --- |
| 1.1 | `UserDto`, the users routes, `TodoDto.AssigneeIds`, and the `X-Todo-User` header in `Contracts`. | Contract tests (C): the round trip; the missing and present `assigneeIds` rule (TOD.12.4). |
| 1.2 | `ServerTodoStore` users and assignees; `TodoApiHost` routes; name uniqueness enforced on the server too (`409`). | Contract tests against the in-process API. |
| 1.3 | Core: `User`, `TodoItem.AssigneeIds`, `UserValidation`, `AssigneeFilter`, `TodoListRules.Arrange(items, status, assignee, me)`, and the Empty text per filter. | Unit (U): every filter combination, validation boundaries (50/51 characters, case-insensitive duplicates), ordering of assignee names. |
| 1.4 | Infrastructure: schema v3, `SqliteUserRepository`, the signed-in setting, `TodoApiClient` users calls, sync fetching users first, `TODO_USER` in `LaunchSettings`. | Integration (I): migration 2 → 3, a sync that brings a new user, an unknown assignee surviving (TOD.12.5), conflicts (TOD.12.6). |
| 1.5 | Test data schema, the `team` set, `TodoServerStub` users, and the new `TodoDataPhrases`. | `TestDataTests` load every set; the old sets are unchanged. |

Verify: Todo unit, integration and contract tests, then the Todo MAUI UAT (32) and web UAT (33),
unchanged.

### Phase 2 - The MAUI app

| # | Step | Evidence |
| --- | --- | --- |
| 2.1 | ViewModels: `UserListViewModel`, `UserEditViewModel`, and the list and edit VMs' assignee members. | Unit tests on the VMs. |
| 2.2 | XAML: User List and User Edit pages, the list header, the Assignee picker, row assignees, the edit page section, and detail. AutomationIds from D5. | Launch on Windows, visual tree and a screenshot of each new or changed page. Checked in light and dark. |
| 2.3 | `Todo.UITests` page objects: `TodoListPage.Assignee`, `.Me`, `.Users`, row `Assignees`, and `TodoEditPage.Assignees`. | Existing Todo UI tests: same counts. |

### Phase 3 - UAT namespaces in Brinell (`Brinell.Uat`, `Brinell.Core.Testing`)

| # | Step | Tests (in `Brinell.Uat.Tests`) |
| --- | --- | --- |
| 3.1 | `UatNamespaceAttribute` (assembly), `UatPhraseClassAttribute.Namespace`, and the namespace recorded on each discovered page and phrase. | Discovery assigns namespaces. A class overrides its assembly. |
| 3.2 | `Using` in file metadata and `## Using` in config (parser and writer). | Parse and round trip. |
| 3.3 | The binder filters by visible namespaces; `UATB006`, `UATB007`, `UATB008`; pages unique per namespace. | A step outside the used namespaces fails, naming the namespace. Unknown and unused usings. The same page name in two namespaces is fine until both are used. **Every existing suite binds identically** (a snapshot of command ids per file). |
| 3.4 | The report names the namespace; spec and runtime catalog parity. | Parity tests; report contents. |
| 3.5 | AD-016, the `invariants.md` line, the namespaces section in `docs/guides/uat-phrases-and-flows.md` and `uat-template-guide.md`; `phrase-namespaces.md` status → "partly built", pointing here. | Doc links valid. |

Verify: `srcnew\Brinell.sln` build, then `Brinell.Uat.Tests`, `Brinell.Core.Tests`,
`Brinell.Presenter.Uat.Tests`, and both Todo UAT suites unchanged.

### Phase 4 - The users test projects

| # | Step | Evidence |
| --- | --- | --- |
| 4.1 | `Brinell.Samples.Todo.Users.UITests`: `UserListPage` and `UserEditPage` (ControlObjects only), `[assembly: UatNamespace("Users")]`, and H-tier tests for TOD.11, TOD.12.1-2 and TOD.13 (Journey and Pyramid traits). Fixtures on `team`, with and without `TODO_USER`, and offline. | The new project's run: all pass, 3 runs in a row. `accessibility-audit.md` has no new findings. |
| 4.2 | `Brinell.Samples.Todo.Users.Uat`: config, `TodoUsersUatFixture` (the Todo UAT fixture's composition plus the users assembly), `UserPhrases` in `Users`, and the three scenario files with `Using \| Users`. | All scenarios pass. |
| 4.3 | **Namespace proof in the sample:** a scenario file with no `Using` fails binding with `UATB006`, run as an expected failure (`RunExpectedFailureUatFileAsync`). A second one with `Using \| Users, Nothing` fails with `UATB007`. | Both expected failures pass. |
| 4.4 | Solution, `TestRunner.ps1`, and `JourneyCoverage` (TOD.11 to TOD.13 covered). | `JourneyCoverage` green. |

### Phase 5 - The web head (parity)

| # | Step | Evidence |
| --- | --- | --- |
| 5.1 | Razor pages and components for D5, with the same `data-testid` values as the AutomationIds. | Screenshot of each page. |
| 5.2 | `Todo.Web.UITests` page objects for users, and a `Users` namespace for the web users pages. The users scenarios run on the web head as well (AD-014: one file serves every head), from a web users UAT suite that links the MAUI one's files. | The same scenarios pass on both heads. |

Phase 5 can wait. Open question Q1.

### Phase 6 - Docs

`samples/Todo/README.md` (the users feature and the new projects), `uat-walkthrough.md` (one
walk-through of a scenario with `Using`), `journeys.md` (section 4), and `CHANGELOG.md` for the
Brinell parts (namespaces).

## 6. Progress

| Step | Status | Evidence |
| --- | --- | --- |
| - | - | - |

## 7. Open questions

| # | Question | Proposal |
| --- | --- | --- |
| Q0 | The request had a point 5 with nothing after it. What was it? | - |
| Q1 | The web head: in this plan (phase 5), or later? | Later. The MAUI head proves the feature and the namespaces; the web head is parity work that follows the same steps. |
| Q2 | Should a new todo be assigned to its creator by default? | Yes, when someone is signed in. That matches "my todos", and it can be unticked. |
| Q3 | One assignee or several? | Several, as asked. One would make the edit page a Picker and the filter simpler, but "assign users" is plural. |
| Q4 | Should the server enforce anything by user (for example, only an assignee can edit)? | No. `X-Todo-User` is recorded, not enforced. Permissions are a feature of their own. |
| Q5 | Folder-based phrase visibility (`phrase-namespaces.md` section 4)? | Not in this plan. `Using` and namespace attributes first; folders once a suite has enough scenarios to need them. |

## 8. Risks

- **Bind-time filtering could change what existing files bind to.** Every existing phrase and
  page is global, so nothing should change. Step 3.3's per-file snapshot proves it, rather than
  relying on "should".
- **A second Picker on the list page** adds a dialog on Android, where Picker is still broken
  (`.my/android/plan.md` A1). The filter tests follow the status filter's lead and are Windows-only
  until A1 is fixed.
- **A collection of CheckBox rows** on the edit page may reveal gaps in the collection and toggle
  controls. If it does, fix the control in Brinell (AGENTS.md: behaviour lives in controls). Do not
  work around it in the test.
- **The schema version 3 migration** runs on every existing device database. The additive
  migration and its test cover this. The server table is created with `IF NOT EXISTS`, like
  `todos`.
