# 03 — Database checks

A todo showing on the screen does not prove it was **persisted** — the app might
hold it only in memory and lose it on restart. Database checks assert the durable
state: the row exists, it carries a client id and a "pending" flag (`TOD.02.6`), a
deleted todo leaves a tombstone that survives a restart (`TOD.06.4`, `TOD.09.3`),
`CreatedAt` never changes while `UpdatedAt` does (`TOD.04.5`).

The app under test owns its SQLite file. In UAT there are two honest ways to look
inside it.

---

## Two ways to read the app's database

| Way | How | Best for |
| --- | --- | --- |
| **A. Separate read connection** | open the app's `DatabasePath` from the test with a read-only connection | UAT: the app is a live process; you cannot share its objects |
| **B. In-process repository** | resolve the app's `ITodoRepository` from the DI scope | Integration ([TodoHost](../../../samples/Todo/tests/Brinell.Samples.Todo.IntegrationTests/TodoHost.cs)), where the test *is* the composition |

In UAT the app runs in its own process, so way **B** is not available — the test
cannot reach into another process's objects. Use way **A**: the fixture told the
app where its database is (`LaunchSettings.DatabasePath`), so the test knows the
same path and opens its own connection.

```csharp
[UatPhraseClass]
public sealed class DatabasePhrases : UatPhraseClassBase
{
    private readonly TodoWorld _world;   // holds DatabasePath, chosen by the fixture
    public DatabasePhrases(TodoWorld world) => _world = world;

    private TodoDbReader Open() => TodoDbReader.OpenReadOnly(_world.DatabasePath);
}
```

`TodoDbReader` is a thin test-only reader (SQLite read-only, `Mode=ReadOnly`) that
returns plain records — it does not take a dependency on the app's infrastructure
assembly, so the check cannot accidentally pass by reusing app code that is itself
under test.

---

## Rule 1 — read the database only after a UI sentinel

The app writes asynchronously. Reading the file the instant after `I tap Save`
races the write. **Wait for the screen to confirm the write landed** (the list
shows the row, the pending marker appears), then read. Never sleep.

```gherkin
When I enter "Buy milk" into Title
And I tap Save
Then I should see "Buy milk"                       # UI sentinel: the write is done
And the database should hold a todo titled "Buy milk"
And that todo should be marked pending
```

```csharp
[UatPhrase(UatEffectiveStepKeyword.Then, "the database should hold a todo titled {title}")]
public void AssertRowExists(string title)
{
    using var db = Open();
    Assert.Contains(db.AllTodos(), t => t.Title == title);
}

[UatPhrase(UatEffectiveStepKeyword.Then, "that todo should be marked pending")]
public void AssertPending()
{
    using var db = Open();
    Assert.All(db.AllTodos().Where(t => t.IsDirty), t => Assert.Equal(SyncState.Pending, t.SyncState));
}
```

---

## Rule 2 — assert invariants the UI cannot show

Some durable facts never reach a control: the client-generated id, the UTC
storage of dates, the tombstone. These are the highest-value database checks
because *only* the database can prove them.

```gherkin
When I create the todo "Call Sam"
Then the stored todo "Call Sam" should have a client id
And its due date should be stored in UTC
```

```csharp
[UatPhrase(UatEffectiveStepKeyword.Then, "the stored todo {title} should have a client id")]
public void AssertClientId(string title)
{
    using var db = Open();
    var todo = db.AllTodos().Single(t => t.Title == title);
    Assert.False(string.IsNullOrEmpty(todo.ClientId));
}

[UatPhrase(UatEffectiveStepKeyword.Then, "its due date should be stored in UTC")]
public void AssertUtc(/* title captured from context */)
{
    using var db = Open();
    Assert.All(db.AllTodos().Where(t => t.DueUtc is not null),
        t => Assert.Equal(DateTimeKind.Utc, t.DueUtc!.Value.Kind));
}
```

> `TODO.03.2` and `TOD.04.5` put UTC round-trip and the `CreatedAt`/`UpdatedAt`
> rule in **Integration**, not UAT — they never need a screen. Only lift a
> database check into UAT when it is *paired* with a UI action whose durability
> is the point (create, then prove it persisted before a restart).

---

## Rule 3 — the tombstone survives a restart

Delete must not merely hide the row; it must leave a tombstone the next launch
respects (`TOD.06.4`). In UAT the "restart" is relaunching the app — a fixture
capability, exposed as a phrase — after which the read connection proves the row
is gone and the tombstone remains.

```gherkin
Given I create the todo "Temp"
And I open the todo "Temp"
When I tap Delete
And I confirm the dialog
And I relaunch the app
Then the database should have no live todo titled "Temp"
And a tombstone for "Temp" should remain
```

```csharp
[UatPhrase(UatEffectiveStepKeyword.When, "I relaunch the app")]
public Task Relaunch(CancellationToken ct) => _world.RelaunchAsync(ct);   // fixture stops + starts the app

[UatPhrase(UatEffectiveStepKeyword.Then, "a tombstone for {title} should remain")]
public void AssertTombstone(string title)
{
    using var db = Open();
    Assert.Contains(db.AllTodos(includeTombstones: true),
        t => t.Title == title && t.IsDeleted);
}
```

Relaunching, not just rebuilding a container, is what a UI UAT can prove that an
integration test cannot: the *app* — its startup, its migrations, its read of the
same file — honours the tombstone.

---

## Cautions

- **Read-only, always.** The test opens the app's database to *observe*, never to
  mutate. A writing test connection can corrupt the app's view or deadlock on the
  file lock.
- **One writer.** SQLite tolerates a read-only reader alongside the app, but do
  not run schema changes or `VACUUM` from the test.
- **Do not reuse the app's own repository types** to read — a check that runs the
  code under test can pass for the wrong reason. A minimal independent reader is
  the point.
- **Path comes from the fixture**, not a guess. The same `DatabasePath` the
  fixture set on the app is the only correct path.

---

## Checklist

- [ ] The read happens **after** a UI sentinel, never after a sleep.
- [ ] The connection is read-only and independent of the app's infrastructure code.
- [ ] The check proves something durable the screen cannot show (id, UTC, tombstone), or is paired with a real UI action whose persistence is under test.
- [ ] Pure persistence rules with no screen live in integration tests, not UAT.
- [ ] Restart-survival uses a real app relaunch, exposed as a phrase.
