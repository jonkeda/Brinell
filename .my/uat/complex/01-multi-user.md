# 01 — Multi-user UAT

The hard truth first: **one UAT run drives one app instance with one signed-in
user.** The scenario runner taps controls on a single screen; it has no second
pair of hands. So "multi-user" in UAT is never "open two apps and race them"
unless you deliberately pay for that (see §4). The deterministic way to prove a
multi-user behaviour is to make the *second* user a fact about the **backend**,
and let the one app the runner drives observe the consequence.

Three strategies, in order of preference:

| Strategy | The second user is… | Determinism | Use when |
| --- | --- | --- | --- |
| **A. Backend-mediated** | rows/changes seeded into the mock backend | high | the app only *sees* the other user's effect after a sync/refresh |
| **B. Identity switching** | the same app signed out and back in as user B | high | testing per-user isolation on one device |
| **C. Two live heads** | a second real app instance | low | genuinely concurrent, gesture-level interaction (rare) |

---

## Strategy A — backend-mediated (recommended)

User A is the person the runner drives. User B is a change you write into the
[MockApiServer](../../../srcnew/Brinell.Mocking/MockApiServer.cs) between steps.
When A syncs, the app pulls B's change and it appears on A's screen. This is
exactly `TOD.07.3` — "a todo created on the server appears after Sync".

```gherkin
Given I am on the Todo List page
And user "bob" creates the todo "Review PR" on the server
When I tap Sync
Then a GET to "/api/todos" should have been sent
And I should see "Review PR"
```

Backed by a phrase class that owns the fake backend's per-user state:

```csharp
[UatPhraseClass]
public sealed class MultiUserPhrases : UatPhraseClassBase
{
    private readonly TodoServerStub _server;   // the fake backend's state (see TodoHost)

    public MultiUserPhrases(TodoServerStub server) => _server = server;

    [UatPhrase(UatEffectiveStepKeyword.Given, "user {user} creates the todo {title} on the server")]
    public void UserCreatesServerTodo(string user, string title)
        => _server.AddTodo(owner: user, title: title);   // no call to the app; pure backend
}
```

The mirror case — A's change must reach B — is asserted **on the wire**, because
B is not a screen the runner can read. After A saves and syncs, assert the app
sent the write the server (and therefore B) would receive:

```gherkin
Given I am on the Todo List page
When I tap Add
Then I should be on the Todo Edit page
When I enter "Shared task" into Title
And I tap Save
And I tap Sync
Then a PUT or POST carrying title "Shared task" should have been sent
```

```csharp
[UatPhrase(UatEffectiveStepKeyword.Then, "a PUT or POST carrying title {title} should have been sent")]
public async Task AssertUpsertSent(string title, CancellationToken ct)
{
    // Waits for the request to arrive, never a sleep (AD-004).
    var request = await _backend.WaitForRequestAsync(
        r => r.Method is "PUT" or "POST" && r.BodyMatchesJson(b => b.GetProperty("title").GetString() == title),
        ct);
    Assert.NotNull(request);
}
```

### Why this is enough

A multi-user *contract* is really two one-sided facts: "the app renders what the
server holds" and "the app sends what the server needs". Both are observable from
one app plus the backend. You do not need a second UI to prove them, and adding
one only adds flakiness.

### Naming the actors

The examples above say "I" for the person the runner drives and name the *other*
user ("bob") only where they are a backend fact. That is the least-ceremony
reading and the recommended default:

```gherkin
Given I am on the Todo List page
And Bob creates the todo "Review PR" on the server
When I tap Sync
Then a GET to "/api/todos" should have been sent
And I should see "Review PR"
```

If you prefer both users named for symmetry — "Alice is on the list page… Bob
creates on the server… Alice syncs" — you *can*, but only if you keep one rule:
**exactly one name is the driven actor** (bound to the single app the runner
controls); every other name is server-only. Bind that name once, then a
`{user}`-prefixed phrase set routes each step:

```gherkin
Given Alice is the current user
And Alice is on the Todo List page
And Bob creates the todo "Review PR" on the server
When Alice taps Sync
Then a GET to "/api/todos" should have been sent
And Alice should see "Review PR"
```

```csharp
[UatPhraseClass]
public sealed class ActorPhrases : UatPhraseClassBase
{
    private readonly UatWorld _world;          // per-scenario state (owns the driven actor)
    private readonly TodoListPage _list;       // the one driven app's page objects
    private readonly TodoServerStub _server;   // the fake backend

    public ActorPhrases(UatWorld world, TodoListPage list, TodoServerStub server)
        => (_world, _list, _server) = (world, list, server);

    // Binds a name to the single app the runner drives. Every other name is server-only.
    [UatPhrase(UatEffectiveStepKeyword.Given, "{user} is the current user")]
    public void SetDrivenActor(string user) => _world.DrivenActor = user;

    [UatPhrase(UatEffectiveStepKeyword.Given, "{user} is on the {page} page")]
    public Task ActorIsOn(string user, string page, CancellationToken ct)
        => Driven(user).NavigateTo(page, ct);

    [UatPhrase(UatEffectiveStepKeyword.When, "{user} taps {control}")]
    public Task ActorTaps(string user, string control, CancellationToken ct)
        => Driven(user).Tap(control, ct);

    [UatPhrase(UatEffectiveStepKeyword.Then, "{user} should see {text}")]
    public Task ActorShouldSee(string user, string text, CancellationToken ct)
        => Driven(user).AssertVisible(text, ct);

    // Any name may act on the server, driven or not — it never touches the app.
    [UatPhrase(UatEffectiveStepKeyword.Given, "{user} creates the todo {title} on the server")]
    public void ServerActorCreates(string user, string title)
        => _server.AddTodo(owner: user, title: title);

    // The guard that keeps it honest: only the bound actor may drive the app.
    // A second live actor is a bug, not a feature — fail loudly instead of silently.
    private TodoListPage Driven(string user)
    {
        if (!string.Equals(user, _world.DrivenActor, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"'{user}' is not the driven actor ('{_world.DrivenActor}'). One UAT run " +
                "drives one app; make the other user a server-side fact, not a second pair of hands.");
        return _list;
    }
}
```

So the answer to "should `I` become `{user}`?" is: **only for the one driven
actor, and only if the guard above is in place.** The `{user}` prefix buys
readability, not a second live user — `Bob taps Sync` in a scenario where Alice
is current must throw, or you have quietly written a two-head test (Strategy C)
that the single-fixture runner cannot honour. If you find yourself wanting two
names to both *tap*, that is the signal to move the second user to the backend.

---

## Strategy B — identity switching (per-user isolation)

To prove user B cannot see user A's local data, drive **one** app but change who
is signed in. The sign-out/sign-in is a domain phrase; the assertion is a
built-in `should not see`.

```gherkin
Given I am signed in as "alice"
And I create the todo "Alice private"
When I sign out
And I sign in as "bob"
Then I should not see "Alice private"
```

```csharp
[UatPhrase(UatEffectiveStepKeyword.Given, "I am signed in as {user}")]
[UatPhrase(UatEffectiveStepKeyword.When, "I sign in as {user}")]
public async Task SignIn(string user, CancellationToken ct)
{
    _world.CurrentUser = user;
    await _session.SignInAsync(user, ct);   // the app's real auth path, or a test seam
}

[UatPhrase(UatEffectiveStepKeyword.When, "I sign out")]
public Task SignOut(CancellationToken ct) => _session.SignOutAsync(ct);
```

`I create the todo {title}` here is a small convenience phrase that runs the
create flow through the page objects, so the scenario reads at the domain level.
`I should not see` is the built-in absent-text assertion (a custom
`AssertTextAbsent` if the built-in set does not include it — see
[../todos/journeys.md](../todos/journeys.md)).

---

## Strategy C — two live app heads (advanced, caveated)

Only when the behaviour is genuinely concurrent at the UI — e.g. A and B editing
the same record and the app must show a live conflict. This means launching a
second app instance with a second backend identity, driven by a second page-object
graph. It is:

- **expensive** — two launches, two automation sessions, double the startup cost;
- **flaky** — cross-process timing the runner cannot serialise;
- **out of scope** for the current single-fixture UAT runner, which owns one
  `IMauiTestContext`.

If you truly need it, it is a bespoke integration harness, not a `.uat.md`
scenario. Prefer Strategy A: seed B's concurrent edit into the backend, then have
A sync and assert the conflict banner renders (`TOD.07.6` shows the merge rule
itself belongs in a **Unit** test; only the *rendered* banner earns a UAT).

---

## Checklist

- [ ] Is the second user only *observed* by the first? → Strategy A.
- [ ] Are you proving data isolation on one device? → Strategy B.
- [ ] Did you assert A→B via the **wire**, not a second screen?
- [ ] Did every cross-user step **wait** for a request or a control, never sleep?
- [ ] Is the merge/permission *rule* covered by a unit test, leaving UAT to check
      only what the screen shows?
