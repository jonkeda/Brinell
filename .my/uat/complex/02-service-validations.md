# 02 — Service validations

A screen looking right is not proof the app talked to its backend correctly. The
same "Buy milk" row can appear because the app pulled it, or because it never
called the server and showed stale local data. Service validation is asserting
the **calls themselves**: that the right request was sent, with the right method,
path, headers and body, and that the app handles the server's answers —
including 500, 401 and timeouts.

All of it runs against the [MockApiServer](../../../srcnew/Brinell.Mocking/MockApiServer.cs)
the fixture starts before the app and hands to the app as its `ApiBaseUrl`
(see [TodoHost](../../../samples/Todo/tests/Brinell.Samples.Todo.IntegrationTests/TodoHost.cs)).

---

## Rule 1 — wait for the request, never count right after a click

The app sends requests asynchronously. A count read immediately after `I tap
Sync` races the HTTP call and flakes. Use `WaitForRequest` to block until the
request arrives (this is decision **AD-004**); use the count queries only to
assert something did **not** happen or happened **exactly once**, *after* a UI
sentinel says the action finished.

```gherkin
When I tap Sync
Then a GET to "/api/todos" should have been sent
```

```csharp
[UatPhraseClass]
public sealed class ServicePhrases : UatPhraseClassBase
{
    private readonly MockApiServer _backend;
    public ServicePhrases(MockApiServer backend) => _backend = backend;

    [UatPhrase(UatEffectiveStepKeyword.Then, "a {method} to {path} should have been sent")]
    public Task AssertRequestSent(string method, string path, CancellationToken ct)
        // WaitForRequestAsync throws TimeoutException (listing what did arrive) if none comes.
        => _backend.WaitForRequestAsync(method, path, cancellationToken: ct);
}
```

---

## Rule 2 — assert the request's shape, not just that it happened

[RecordedRequest](../../../srcnew/Brinell.Mocking/RecordedRequest.cs) exposes the
method, path, query, headers, and body (`BodyAs<T>`, `BodyMatchesJson`). Assert
what the contract requires.

### The API key header (TOD.07.8)

```gherkin
When I tap Sync
Then every request should carry the API key
```

```csharp
[UatPhrase(UatEffectiveStepKeyword.Then, "every request should carry the API key")]
public void AssertApiKeyOnAllRequests()
{
    foreach (var request in _backend.Requests())
    {
        Assert.True(
            request.Headers.TryGetValue("X-Api-Key", out var key) && key.Length > 0,
            $"{request.Method} {request.Path} was sent without an API key.");
    }
}
```

### The body carries what was typed

```gherkin
When I enter "Ship it" into Title
And I tap Save
And I tap Sync
Then the created todo on the server should have title "Ship it"
```

```csharp
[UatPhrase(UatEffectiveStepKeyword.Then, "the created todo on the server should have title {title}")]
public Task AssertCreatedTitle(string title, CancellationToken ct)
    => _backend.WaitForRequestAsync(
        method: null, path: "*",
        predicate: r => r.Method is "POST" or "PUT"
                     && r.BodyMatchesJson(b => b.GetProperty("title").GetString() == title),
        cancellationToken: ct);
```

---

## Rule 3 — assert the order and the absence

Sync has a contract beyond "it called something": it **pushes then pulls**
(`TOD.07.1`), and a second sync with no changes **sends no writes** (`TOD.07.7`).

```gherkin
When I tap Sync
Then a GET to "/api/todos" should have been sent
And the app should push before it pulls
When I clear the recorded requests
And I tap Sync
Then a GET to "/api/todos" should have been sent
And no write request should have been sent
```

```csharp
[UatPhrase(UatEffectiveStepKeyword.Then, "the app should push before it pulls")]
public void AssertPushBeforePull()
{
    var log = _backend.Requests();
    var firstPull = log.ToList().FindIndex(r => r.Method == "GET");
    var lastWrite = log.ToList().FindLastIndex(r => r.Method is "PUT" or "POST" or "DELETE");
    // No write, or every write precedes the first pull.
    Assert.True(lastWrite < 0 || (firstPull >= 0 && lastWrite < firstPull), "A write was sent after the pull.");
}

[UatPhrase(UatEffectiveStepKeyword.When, "I clear the recorded requests")]
public void ClearRequests() => _backend.ResetRequests();

[UatPhrase(UatEffectiveStepKeyword.Then, "no write request should have been sent")]
public void AssertNoWrites()
    => Assert.Equal(0,
        _backend.RequestCount("PUT", "*")
      + _backend.RequestCount("POST", "*")
      + _backend.RequestCount("DELETE", "*"));
```

> The `GET` assertion between the clear and the count is deliberate: it **waits**
> for the second sync to actually run before the count is read, so the "no writes"
> check is not a race (AD-004). Ordering and idempotence are really **integration** concerns
> ([TodoHost](../../../samples/Todo/tests/Brinell.Samples.Todo.IntegrationTests/TodoHost.cs)
> reads the request log without a UI). Put them in UAT only when a *screen state*
> is the point — e.g. the pending marker disappearing after the push.

---

## Rule 4 — drive the server's answers to test the app's reaction

Stub the response the app must survive, then assert the **screen** it produces.
This is where service validation legitimately becomes UAT: the Error state and
Retry are UI (`TOD.08.1`, `TOD.08.2`).

```gherkin
Given the server fails the next sync with 500
When I tap Sync
Then State should contain "Something went wrong"
And I should see "Retry"
When I tap Retry
Then I should see "Buy milk"
```

```csharp
[UatPhrase(UatEffectiveStepKeyword.Given, "the server fails the next sync with {status}")]
public void FailNextSync(int status)
    // FailsFirst fails the given number of calls, then falls through to the success body,
    // so the Retry recovers with no extra "server recovers" step.
    => _backend.Get("/api/todos").FailsFirst(1, status).ReturnsJson(_world.CurrentServerList());
```

The same shape covers **401** (stub 401 → assert "not authorised", `TOD.08.5`)
and **slow** responses (stub with a delay → assert Loading shows, then content,
`TOD.08.3`). For the slow case the assertion still **waits for the state**, it
does not sleep for the delay.

---

## Checklist

- [ ] Every "was called" assertion goes through `WaitForRequest`, not a bare count.
- [ ] Count/absence assertions run only after a UI sentinel confirms the action finished.
- [ ] Request shape (headers, body) is asserted where the contract requires it.
- [ ] Error/timeout/auth responses are stubbed, and the assertion checks the
      resulting **screen**, not just the stub.
- [ ] Pure order/idempotence lives in integration tests unless a control renders the difference.
