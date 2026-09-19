# Todo sample: one app, five test tiers

A small MAUI todo app, tested the way a real app should be: each behaviour at the lowest tier
that can prove it, and every test traced back to a user journey. It shows:

- **Brinell.Maui** driving the app on Windows (FlaUI) and Android (Appium), through page
  objects and ControlObjects only: tests never touch a driver, an element or a sleep.
- **WireMock** (`Brinell.Mocking`) standing in for the backend, with the requests it received
  asserted by waiting for them.
- **A seeded SQLite database** for the app's local state, built with the app's own migrations.
- **A test pyramid** from unit tests to manual charters, kept honest by a report that compares
  the tests with [journeys.md](journeys.md).

## The app

Three pages in a Shell, with a local SQLite store that syncs with `Todo.Api`:

| Page | What it does |
| --- | --- |
| List | All todos, a status filter, Sync, Add. Empty, Loading and Error states (CommunityToolkit `StateContainer`). |
| Detail | One todo in three `SectionCard`s (details, notes, sync state), Next status, Edit, Delete (with confirmation). |
| Edit | Title (required, at most 100 characters), notes, an optional due date, status. Cancel asks before discarding changes. |

Two custom controls, each with a matching ControlObject on the test side:

| App control | Kind | Test side |
| --- | --- | --- |
| `TodoStatusView`: glyph, status name, Next button | Component: its parts' ids repeat in every row, so the ControlObject scopes to the component's root | `TodoStatus<TScope>` |
| `SectionCard`: a header and any content | Container: controls inside it are found within the card | `SectionCard<TParent>` |

## Projects

| Project | What it is |
| --- | --- |
| `src/…Core` | Models, rules, view models, the sync planner. No MAUI, no SQLite, no HTTP. |
| `src/…Contracts` | The DTOs and routes the app and the API share. |
| `src/…Infrastructure` | SQLite repository, API client, sync service, launch settings. |
| `src/…Api` | `Todo.Api`, the backend: a minimal API with its own SQLite file. |
| `src/…App` | The MAUI app (Windows and Android). |
| `tests/…UnitTests` | Tier 1. |
| `tests/…IntegrationTests` | Tier 2: headless, against WireMock and SQLite; the contract tests against the real API. |
| `tests/…TestSupport` | Scenarios (`Scenarios/*.json`), the WireMock fake of the API, temp databases. |
| `tests/…UITests` | Tiers 3 and 4 on Windows: page objects, ControlObjects, fixtures, tests. |
| `tests/…UITests.Mobile` | The same UI test sources, built for Android (Appium). |
| `tests/…JourneyCoverage` | The journey coverage report. |
| `manual/charters.md` | Tier 5. |

## The five tiers

| # | Tier | `Pyramid` trait | What is real | What is fake | Tests (cases) |
| --- | --- | --- | --- | --- | --- |
| 1 | Unit | `Unit` | View models, rules, the sync planner | Repository, API client, navigator, dialogs (NSubstitute); the clock | 70 (114) |
| 2 | Integration | `Integration` | DI composition, SQLite, `HttpClient`, sync | The backend: WireMock | 24 |
| 2b | Contract | `Contract` | The real `Todo.Api`, in process | nothing | 4 |
| 3 | UI hermetic | `UiHermetic` | The app, SQLite, HTTP | The backend: WireMock; the database is seeded; the clock is pinned | 34 (36) |
| 4 | UI live | `UiLive` | The app and the real `Todo.Api` | nothing | 3 |
| 5 | Manual | - | Everything, on real devices | nothing | 5 charters |

Two gates run inside tier 3: **Smoke** (`Gate=Smoke`, under 30 s: the app starts, the list
arrives, a detail opens) and **Reviews** (`Gate=Review`: every screen once, its controls found by
id, a screenshot each).

The counts are the pyramid's shape. Most of it is rule tables (`[Theory]` over validation, the
Overdue rule, sort order, sync merge) that run in milliseconds. The UI tiers check one example
per behaviour: that the screen shows what the rules decide.

## Walkthrough: TOD.02 "Create a todo" through every tier

[journeys.md](journeys.md) splits each journey into subtasks and gives each subtask the lowest
tier that can prove it:

| Id | Subtask | Tier |
| --- | --- | --- |
| TOD.02.1 | Add opens an empty edit form with status Open | H |
| TOD.02.2 | Save with a valid title returns to the list, which shows the new todo | H |
| TOD.02.3 | Empty or whitespace title shows "Title is required" and does not save | U + H |
| TOD.02.4 | Title over 100 characters is refused | U |
| TOD.02.5 | A due date is optional; switching it off clears it | U + H |
| TOD.02.6 | The new todo is stored locally with a client-generated id and "pending" | I |

Every test carries its subtask as a trait, so you can find all of TOD.02.3's tests with
`--filter Journey=TOD.02.3`, in any test project.

### 1. Unit: every variant of the rule

The rule is a pure function, so all its edge cases go here: four ways of being empty, in
microseconds. `UnitTests/Rules/TodoRulesTests.cs`:

```csharp
[Theory]
[Trait("Journey", "TOD.02.3")]
[InlineData(null)]
[InlineData("")]
[InlineData(" ")]
[InlineData("\t \r\n")]
public void ValidateTitle_EmptyOrWhitespace_IsRequired(string? title)
    => Assert.Equal(TodoValidation.TitleRequired, TodoValidation.ValidateTitle(title));
```

The view model's reaction is a unit test too, with the repository and the navigator faked.
`UnitTests/ViewModels/TodoEditViewModelTests.cs`:

```csharp
[Theory]
[Trait("Journey", "TOD.02.3")]
[InlineData("")]
[InlineData("   ")]
public async Task Save_WithoutATitle_ShowsTheError_AndStaysOnThePage(string title)
{
    var edit = Create();
    await edit.LoadAsync(null);
    edit.Title = title;

    await edit.SaveCommand.ExecuteAsync(null);

    Assert.Equal(TodoValidation.TitleRequired, edit.TitleError);
    Assert.True(edit.HasTitleError);
    Assert.Empty(_repository.Items);
    await _navigator.DidNotReceive().GoBackAsync();
}
```

TOD.02.4 (at most 100 characters) stops here: `ValidateTitle_AllowsAtMost100Characters` covers
1, 99, 100, 101 and 500 characters. A UI test would add nothing but time.

### 2. Integration: what reaches the disk

TOD.02.6 needs a real database: the id is made on the device, and a new todo is `LocalOnly`
until it has been synced. `TodoHost` builds the app's real service container on a temporary
SQLite file, with WireMock as the backend. `IntegrationTests/DatabaseTests.cs`:

```csharp
[Fact]
[Trait("Journey", "TOD.02.6")]
public async Task ACreatedTodo_IsStoredLocally_WithItsOwnId_AndLocalOnly()
{
    using var host = await TodoHost.StartAsync();

    var created = await host.Todos.CreateAsync("Buy milk", null, null, TodoStatus.Open);

    var stored = Assert.Single(await host.LocalAsync());
    Assert.Equal(created.Id, stored.Id);
    Assert.Equal(SyncState.LocalOnly, stored.SyncState);
    Assert.Equal(host.Scenario.Now, stored.CreatedAt);
}
```

The **contract tests** (tier 2b, TOD.07.9) keep the fake honest. The same requests go to
the WireMock fake and to the real `Todo.Api` in process, and both must answer alike. If the API
changes, these fail before the hermetic UI tests can pass against a stale fake.

### 3. UI hermetic: one example on the real screen

The fixture seeds a database from a scenario (`TestSupport/Scenarios/three-todos.json`), starts
WireMock with the scenario's server half, and launches the app with launch settings pointing
at both. The test only uses page objects. `UITests/Tests/Journeys/CreateEditJourneyTests.cs`:

```csharp
[Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
[Trait("Journey", "TOD.02.3")]
public Task Save_WithoutATitle_ShowsTitleRequired_AndStays()
{
    var edit = _fixture.List.Add().SaveExpectingError();

    edit.TitleError.AssertText("Title is required");
    edit.AssertLoaded(true);

    edit.CancelNew();
    return Task.CompletedTask;
}
```

One example, not four: the unit tier already proved the variants. This test proves what only
the screen can: the error label is bound, and Save stays on the page. The rest of TOD.02 is here
too: `Add_OpensAnEmptyForm_Open_WithoutADueDate` (02.1),
`Save_AValidTodo_ShowsItInTheList_WaitingToSync` (02.2), and
`DueDateSwitch_EnablesThePicker_AndTheDateIsSaved` (02.5).

The page objects read like the user's steps, and every wait is inside them.
`TodoEditPage.SaveExpectingError()` clicks Save and waits for the error label;
`TodoListPage.CreateTodo(title)` is `Add().EnterTitle(title).SaveNew()`.

When a test must see what the app sent, it waits for the request on WireMock rather than
counting straight after a click. From `SyncJourneyTests.cs`:

```csharp
list.Sync();

var put = _fixture.Backend.WaitForRequest("PUT", AnyTodo,
    request => request.BodyAs<TodoDto>(TodoApi.JsonOptions)?.Title == title);
Assert.Equal(TodoApi.DevelopmentApiKey, put.Headers[TodoApi.ApiKeyHeader]);
list.Todos.AssertPending(title, expected: false);
```

### 4. UI live: the real wire, once

A created todo matters only when it reaches the server (TOD.07.4). The live fixture starts the
real `Todo.Api` and launches the app against it. The test checks the result through the
API's own HTTP endpoint, never through the app. `UITests/Live/LiveSyncTests.cs`:

```csharp
[LiveApiFact]
[Trait("Journey", "TOD.07.4")]
public async Task ATodoCreatedInTheApp_IsOnTheServerAfterSync()
{
    var title = LiveTodoAppFixture.Unique("Created in the app");
    var list = _fixture.List.CreateTodo(title);
    Assert.Null(await _fixture.Api.FindAsync(title));

    list.Sync().Todos.AssertPending(title, expected: false, timeoutMs: TestConstants.PageTimeoutMs);

    var stored = await _fixture.Api.FindAsync(title);
    Assert.NotNull(stored);
    Assert.Equal(Contracts.TodoStatusDto.Open, stored.Status);
}
```

It uses the same page objects as tier 3; only the fixture differs. There are three live tests,
all happy paths, and they skip unless `BRINELL_UAT_LIVE_API=1`.

### 5. Manual: what a machine cannot judge

[manual/charters.md](manual/charters.md) holds time-boxed charters, not scripts. For TOD.02:
CH-2 kills the app mid-save on a real device, and CH-3 checks the edit page with the keyboard up,
at 200 % font size, and with a screen reader. The integration tier proves the deterministic
half of CH-2: a save is all or nothing (`ASave_IsAllOrNothing_ToAReaderOnAnotherConnection`).
When a charter finds something reproducible, it moves down into an automated tier, and the
charter records where it went.

## Test data: scenarios, not a database bridge

A scenario (`TestSupport/Scenarios/*.json`) has a local half and a server half, plus a pinned
"now":

- The **local half** goes into a fresh SQLite file, created by the app's own `TodoDatabase`
  migrations, so the seeded schema can never drift from the app's.
- The **server half** loads into `TodoServerStub`, a stateful WireMock fake of `Todo.Api`. A test
  changes it per test (a 500, a delay, a failure that recovers), and the fixture resets it
  between tests.

The app receives the file path, the backend URL, the clock and the sync switches as launch
settings (`MauiDriverOptions.LaunchSettings`). On Windows they are environment variables of the
launched process; on Android they are intent extras. A bridge that would let a test write to the
app's database while it runs was considered and not needed. Seeding before launch, plus the
backend fake, covers every journey. The cost shows on Android: a file seeded on the host cannot
reach the device's private storage yet.

## Running it

From `samples/Todo`:

```bash
# Tiers 1, 2 and the coverage report: no app, seconds.
dotnet test tests/Brinell.Samples.Todo.UnitTests
dotnet test tests/Brinell.Samples.Todo.IntegrationTests
dotnet test tests/Brinell.Samples.Todo.JourneyCoverage

# Tier 3 on Windows: build the app first; the fixture finds it under src/…App/bin.
dotnet build src/Brinell.Samples.Todo.App -f net10.0-windows10.0.19041.0
dotnet test tests/Brinell.Samples.Todo.UITests                          # everything, ~30 s
dotnet test tests/Brinell.Samples.Todo.UITests --filter Gate=Smoke      # the Smoke gate
dotnet test tests/Brinell.Samples.Todo.UITests --filter Journey=TOD.02.3

# Tier 4: also start the real API, in process on a free port.
BRINELL_UAT_LIVE_API=1 dotnet test tests/Brinell.Samples.Todo.UITests --filter Pyramid=UiLive
```

To run tier 4 against an API that is already deployed, set `TODO_LIVE_API_BASEURL` and
`TODO_LIVE_API_KEY`.

Run one UI test process at a time: two runs fight over the desktop and fail for reasons that
have nothing to do with the app.

To try the app by hand, start `dotnet run --project src/Brinell.Samples.Todo.Api` (it listens
on port 5080) and then run the app.

### On Android

Start an emulator and an Appium server (`127.0.0.1:4723`, UiAutomator2 driver). Then, from
`samples/Todo`:

```bash
dotnet build src/Brinell.Samples.Todo.App -f net10.0-android
APPIUM_PLATFORM=android dotnet test tests/Brinell.Samples.Todo.UITests.Mobile
```

The app reaches WireMock on the host through `10.0.2.2`. Eight hermetic tests are skipped on
Android, each for a stated reason (`[WindowsOnlyFact(reason)]`). The two biggest: the device
cannot open a database file seeded on the host, and Brinell cannot drive a MAUI Picker's native
dialog on Android yet. Charter CH-5 covers those tests by hand until they can run there. The
rest (26 tests) pass on both platforms.

## The journey coverage report

`tests/Brinell.Samples.Todo.JourneyCoverage` reads [journeys.md](journeys.md), reads every
test's traits by reflection (nothing runs, nothing launches), and fails when:

- a subtask has no test in a tier `journeys.md` assigns it to;
- a subtask marked **M** is not named in `manual/charters.md`;
- a `Journey` trait names a subtask that does not exist (a typo);
- a test has no `Pyramid` trait;
- the pyramid inverts: unit cases must outnumber hermetic UI cases, and those must outnumber
  live ones.

It writes the full matrix, with every subtask, tier and covering test, to
`TestResults/<run-id>/suites/TodoJourneyCoverage/journey-coverage.md`.

To add a behaviour: add its subtask to `journeys.md` with the lowest tier that can prove it,
write the test with its `Journey` and `Pyramid` traits, and let the report confirm they match.

## Rules the tests follow

- Tests use page objects and ControlObjects only. No driver, no `IMauiElement`, no `Find*`, no
  `Thread.Sleep`. Waiting belongs to the ControlObjects and page objects.
- Synchronize on what the user would see (a state, a marker, a row), or on a request WireMock
  received. Never on time.
- One example per behaviour in the UI tiers. If a UI test repeats a variant a unit test already
  covers, push it down.
- xUnit `Assert` only.
