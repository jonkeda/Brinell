# Example: test data + a journey UAT file, end to end (Todo)

Status: example for the two proposals
Date: 2026-09-24
Builds on:

- [test-data-proposal.md](test-data-proposal.md) - `TestData/`, the validator, builders, the step-table binder, checks
- [tod-02-single-journey-uat.md](tod-02-single-journey-uat.md) - one `.uat.md` per journey

Nothing here exists yet. This shows what the pieces would look like with the smallest useful test
set, so the proposals can be judged on a concrete case. Names marked *new* have to be built.

---

## 1. The files involved

```text
srcnew/
  Brinell.TestData/                  engine: ITestDataDefinition, TestDataValidator, strict loader (new)
  Brinell.DataValidator/             the brinell-data tool; loads app assemblies at run time (new)
samples/Todo/tests/
  Brinell.Samples.Todo.TestSupport/
    TestData/
      testdata.schema.json           generated from the DTOs (new)
      one-todo.json                  the test set of this example (new)
    TodoTestDataDefinition.cs        folder, root type, Todo rules (new)
    TodoData.cs                      builder: real DTOs with defaults (new)
  Brinell.Samples.Todo.Uat/
    uat.config.md                    + TestData assembly row, + DefaultTestData
    Runtime/TodoUatFixture.cs        + UseTestData(name) instead of a hard-coded name
    Runtime/TodoDataPhrases.cs       server/database phrases (new)
    Scenarios/TOD.02-create-a-todo.uat.md
```

The app-specific part is one small class. The tool finds it by reflection:

```csharp
// TestSupport/TodoTestDataDefinition.cs
public sealed class TodoTestDataDefinition : ITestDataDefinition<TodoTestSet>
{
    public string Folder => "TestData";

    public IEnumerable<Type> TableTypes => [typeof(TodoDto), typeof(TodoItem)];   // for describe and step-table checks

    public void Rules(TestDataRules<TodoTestSet> rules)
    {
        rules.ForEach(s => s.Server, (todo, report) => TodoValidation.Check(todo.Title, report));   // the app's own rule
        rules.UniqueIds(s => s.Server, t => t.Id);
        rules.Require(s => s.Server, t => t.CreatedAt <= t.UpdatedAt, "updatedAt is before createdAt");
        rules.SyncedMatchesServer();   // a Todo-specific consistency rule
    }
}
```

## 2. The test set: one todo

`three-todos` is more than this journey needs. One synced todo is enough to show that a new todo
appears **next to** existing data, and that a refused save leaves that data unchanged.

```jsonc
// TestData/one-todo.json
// The server and the device both have "Buy milk", synced.
{
  "$schema": "./testdata.schema.json",
  "now": "2026-03-10T09:00:00Z",
  "server": [
    {
      "id": "33333333-0000-0000-0000-000000000001",
      "title": "Buy milk",
      "notes": null,
      "dueDate": "2026-03-12",
      "status": "Open",
      "createdAt": "2026-03-01T08:00:00Z",
      "updatedAt": "2026-03-01T08:00:00Z"
    }
  ],
  "local": [
    // Shortcut: only id + syncState means "the same as the server record with this id".
    { "id": "33333333-0000-0000-0000-000000000001", "syncState": "Synced" }
  ]
}
```

- `server` is read into `TodoDto`, and `local` into the app's `TodoItem`. Both are read
  **strictly**, so an unknown field is an error.
- The local shortcut is filled in from the server record before validation. Seeding then writes a
  complete `TodoItem`.

## 3. Checking the data before any test runs

Suppose someone (a person or a model) edits the file and makes three mistakes:

```jsonc
    { "id": "33333333-0000-0000-0000-000000000001", "titel": "Buy milk", "status": "Opne",
      "createdAt": "2026-03-01T08:00:00Z", "updatedAt": "2026-02-01T08:00:00Z" }
```

```powershell
# from samples/Todo/tests/Brinell.Samples.Todo.Uat (reads the assembly paths from uat.config.md)
brinell-data validate TestData/one-todo.json
```

```text
TestData/one-todo.json:8: DATA001 server[0]: unknown field 'titel' on TodoDto. Did you mean 'title'?
TestData/one-todo.json:8: DATA002 server[0]: required field 'title' is missing.
TestData/one-todo.json:8: DATA003 server[0].status: 'Opne' is not a TodoStatusDto (Open, InProgress, Done).
TestData/one-todo.json:9: DATA010 server[0]: updatedAt 2026-02-01 is before createdAt 2026-03-01.
TestData/one-todo.json:14: DATA011 local[0]: Synced, but differs from the server record with the same id (title).
5 errors. 0 warnings.
```

`--format json` prints the same errors as JSON objects for a model. The xUnit gate runs the same
validator over every file in `TestData/`:

```csharp
public sealed class TestDataTests
{
    public static TheoryData<string> Files => new(TodoTestData.Names);

    [Theory]
    [MemberData(nameof(Files))]
    public void TestSet_IsValid(string name)
    {
        var result = TestDataValidator.Validate(TodoTestData.Load(name));
        Assert.True(result.IsValid, result.Format());
    }
}
```

Other commands a model would use while writing a scenario:

```powershell
brinell-data describe TodoItem
#  Id         Guid            required
#  Title      string          required, 1..100 chars (TodoValidation)
#  Status     TodoStatus      Open | InProgress | Done
#  DueDate    DateOnly?       yyyy-MM-dd, or relative in step tables: today, -1d, +3d
#  SyncState  SyncState       Synced | Pending | LocalOnly
#  ...
brinell-data list
#  one-todo      server 1, local 1   used by: Scenarios/TOD.02-create-a-todo.uat.md
#  three-todos   server 3, local 3   used by: ThreeTodosFixture, uat.config.md (DefaultTestData)
```

## 4. The UAT file picks the test set

The file says `TestData | one-todo` in its metadata (section 5). The fixture has no hard-coded name
any more. It only knows how to load a set:

```csharp
// Runtime/TodoUatScenarioTests.cs - one new hook, called before the first scenario of a file
protected override void BeforeFile(UatDocument document)
    => Fixture.UseTestData(document.Metadata.GetValueOrDefault("TestData") ?? Config.DefaultTestData);
```

```csharp
// Runtime/TodoUatFixture.cs
public void UseTestData(string name)
{
    if (name == Loaded.Name)
    {
        return;                                    // same set: StartTest's per-scenario reset is enough
    }

    var testSet = TodoTestData.Load(name);
    CloseApp();
    testSet.SeedDatabase(freshPath);               // a new database file for the new set
    Server.ResetTo(testSet.Server);                // the typed fake gets the new server half
    RestartApp(Settings with { DatabasePath = freshPath, FixedNow = testSet.Now });
}
```

`uat.config.md` holds the default for files that do not name one:

```markdown
| Field | Value |
| --- | --- |
| Fixture | TodoUatFixture |
| DefaultTestData | three-todos |
```

The runner sorts the files by test set, so a run with ten files on two test sets restarts the app
once, not ten times. A name that has no file fails in `brinell-data validate`, before any run:

```text
Scenarios/TOD.02-create-a-todo.uat.md:10: DATA020 TestData 'one-todos' does not exist. Did you mean 'one-todo'?
```

## 5. The journey file

`Scenarios/TOD.02-create-a-todo.uat.md` is the file from the TOD.02 proposal, with a `TestData` line
and data checks added. The new steps are the `the database should have ...` lines.

````markdown
# UAT: TOD.02 Create a todo

## Metadata

| Field | Value |
| --- | --- |
| App | Brinell.Samples.Todo.App |
| Area | Create |
| Target | MAUI |
| Journey | TOD.02 |
| TestData | one-todo |
| Tags | maui, create |

## Background

```gherkin
Given I am on the Todo List page
When I tap Add
Then I should be on the Todo Edit page
```

@TOD.02.1 @maui @create
## Scenario: TOD.02.1 Add opens an empty edit form with status Open

```gherkin
Then Title should equal ""
And Status should show status "Open"
And Title Error should not be visible
```

@TOD.02.2 @smoke @maui @create
## Scenario: TOD.02.2 Saving a valid title returns to the list, which shows the new todo

```gherkin
When I set Title to "Write the report"
And I tap Save
Then I should be on the Todo List page
And Todos should have todo "Write the report"
And Todos should have todo "Buy milk"
And the database should have todos
  | Title            | Status | SyncState |
  | Write the report | Open   | LocalOnly |
```

@TOD.02.3 @smoke @maui @create @validation
## Scenario Outline: TOD.02.3 An empty or whitespace title shows "Title is required" and does not save

```gherkin
When I set Title to "<title>"
And I tap Save
Then Title Error should contain "Title is required"
And I should be on the Todo Edit page
And the database should not have a todo with a blank title
And the database should have todos
  | Title    | SyncState |
  | Buy milk | Synced    |
```

### Examples

| title |
| --- |
|  |
|    |
````

What the data adds to the scenarios:

- **TOD.02.2** used to prove only that the new row renders. Now it also shows that the existing
  todo is still there, and that the new todo was stored as `LocalOnly` (created locally and not yet
  sent to the server). The list row is the **UI sentinel**: the database is read only after the row
  is visible, so the check never races the write.
- **TOD.02.3** "does not save" can now be checked directly: no blank todo was stored, and the
  baseline todo is unchanged. The Edit page is still on screen, so the check does not depend on
  navigation.
- **One small test set is enough** because the one-todo baseline is never changed. Each scenario
  either adds its own todo or adds nothing. This is D5 in practice.
- **The checks are order-free.** The local database is not reset between scenarios, so by the
  time 02.3 runs, 02.2 has already stored "Write the report". That is why 02.3 does not say "the
  database should have **only** Buy milk": that check would fail after 02.2. See section 8.

## 6. A delta on the server (another journey, same test set)

The same one-todo set, with one todo added on the server by the scenario itself. This is TOD.07.3,
"a todo created on the server appears after Sync". It belongs in `TOD.07-sync.uat.md`, and is shown
here only to demonstrate the step table as a delta:

```gherkin
@TOD.07.3 @maui @sync
## Scenario: TOD.07.3 A todo created on the server appears after Sync

Given I am on the Todo List page
And the server also has todos
  | Title        | Status     | DueDate |
  | Call plumber | InProgress | +2d     |
When I tap Sync
Then Todos should have todo "Call plumber"
And the database should have todos
  | Title        | Status     | SyncState |
  | Call plumber | InProgress | Synced    |
```

- The table lists only the columns that matter. `TodoData` fills in `Id` (deterministic),
  `CreatedAt`/`UpdatedAt` (from `now`) and `Notes`.
- `+2d` is resolved against the test set's `now` (`2026-03-10`), which gives `2026-03-12`.
- Adding to the server half is safe per scenario, because the typed fake is reset in `StartTest`.

## 7. The C# behind it

**Builder** (TestSupport), which returns the real contract type:

```csharp
public sealed class TodoData(DateTimeOffset now)
{
    private int _next;

    public TodoDto Todo(string title, TodoStatusDto status = TodoStatusDto.Open, DateOnly? dueDate = null)
        => new(NextId(), title, Notes: null, dueDate, status, CreatedAt: now, UpdatedAt: now);

    private Guid NextId() => new($"99999999-0000-0000-0000-{++_next:D12}");
}
```

**Phrases** (UAT project). These only glue step tables to the builder, the typed fake and the
database reader. The table binder is generic and lives in `Brinell.Uat`:

```csharp
[UatPhraseClass]
public sealed class TodoDataPhrases(TodoUatFixture fixture) : UatPhraseClassBase
{
    [UatPhrase(UatEffectiveStepKeyword.Given, "the server also has todos")]
    public void ServerAlsoHas(UatTable table)
    {
        var data = new TodoData(fixture.Loaded.Now);
        foreach (var row in table.Bind<TodoRow>(fixture.Loaded.Now))   // unknown column -> error
        {
            fixture.Server.Store(data.Todo(row.Title, row.Status, row.DueDate));
        }
    }

    [UatPhrase(UatEffectiveStepKeyword.Then, "the database should have todos")]
    public void DatabaseHas(UatTable table)
        => table.AssertContains(TodoDbReader.OpenReadOnly(fixture.DatabasePath).Todos());

    [UatPhrase(UatEffectiveStepKeyword.Then, "the database should not have a todo with a blank title")]
    public void DatabaseHasNoBlankTitle()
        => Assert.DoesNotContain(TodoDbReader.OpenReadOnly(fixture.DatabasePath).Todos(), t => string.IsNullOrWhiteSpace(t.Title));

    // Exact check, for files whose fixture resets the local half per scenario (section 8, option 3).
    [UatPhrase(UatEffectiveStepKeyword.Then, "the database should have only todos")]
    public void DatabaseHasOnly(UatTable table)
        => table.AssertExactly(TodoDbReader.OpenReadOnly(fixture.DatabasePath).Todos());

    private sealed record TodoRow(string Title, TodoStatusDto Status = TodoStatusDto.Open, DateOnly? DueDate = null);
}
```

`AssertContains` / `AssertExactly` compare **only the listed columns**, and on failure they print
the expected rows, the actual rows and the cells that differ:

```text
the database should have only todos - 1 extra row
  expected                     actual
  Title     | SyncState        Title     | SyncState
  Buy milk  | Synced           Buy milk  | Synced
                               ""        | LocalOnly     <- extra
```

**No anonymous bodies.** The only WireMock setup in this example is `fixture.Server.Store(TodoDto)`
on the typed fake. Nothing calls `ReturnsJson(new { ... })`.

## 8. What this example shows

| Proposal point | Where it shows up |
| --- | --- |
| `TestData/` folder, JSON, strict and schema-checked | Section 2 |
| The CLI and the gate share one validator | Section 3 |
| The `.uat.md` file picks the test set; the fixture loads it | Section 4 |
| One generic tool loads the app assembly at run time | Sections 1 and 3 |
| One file per journey, subtask id in the heading and tag | Section 5 |
| Tests add, never change or remove, the baseline | Sections 5 and 6 |
| Deltas as step tables bound to real DTOs | Section 6 |
| Service/database checks with the same table binder | Sections 5 and 7 |
| Real DTOs in WireMock only | Section 7 |

**Exact database checks need a reset.** When the database is not reset between scenarios, an
"only these todos" check depends on which scenarios ran before it. There are three options:

1. **Order-free checks** (what section 5 does): "has these rows" and "has no row like this". These
   never look at rows that other scenarios added. They are weaker, but always true.
2. **Rely on the order of the file**, e.g. put 02.3 before 02.2. Don't: a scenario that passes only
   after another one has run is fragile, and it breaks as soon as scenarios run one at a time (the
   per-scenario theory cases from the TOD.02 proposal, gap 1).
3. **Reset the local half per scenario** (restore the cached DB copy and restart the app). Then
   "the database should have only todos" is safe. It is exact and order-free, and costs a restart
   per scenario.

Recommendation: **1 by default, 3 opt-in** for a file that needs exact checks.
