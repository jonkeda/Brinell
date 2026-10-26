# Promote reusable subjects

Status: plan, not started. Date: 2026-09-27.
Follows the phrases plan's "later" row ([the phrases plan](https://github.com/exact-mobile-apps/test-tooling/blob/027b0e77/.my/uat/phrases/plan.md#4-phases)), whose phases 1-8 are done.
**Start when** Construction's first UAT suite needs a network, restart or backend-fault step
([construction/plan.md](../../construction/plan.md)); until then Todo is the only consumer and
this plan is the design.

## 1. The problem

Phase 3 filed the Todo phrases that reach past the screen into subject classes, and phase 7 put
them in namespaces behind `## Using`. Some of those subjects mean the same thing in every app: the
device goes offline, the app restarts, the server fails or is slow. But they are written for Todo:

- They bind to Todo's fixture (`ITodoArrangement`, `TodoUatFixture`), so another app cannot use
  them without copying them.
- The words carry Todo's details. "The server fails with status 500" fails the **todo list**
  request, and "fails once" and "answers after 3 seconds" answer with **Todo's todos**
  (`BackendPhrases`).
- `AppLifecyclePhrases` exists twice, once per head, and must be kept word for word alike because
  the scenario files are shared (AD-014).

When Construction writes its first scenarios it will need the same steps. Copying them means two
vocabularies for one meaning, and every fix made twice. Promoting them means one set of words,
each app providing the mechanism behind a small interface.

## 2. What holds today

| Subject | Phrases | Bound to | Mechanism | Generic? |
| --- | --- | --- | --- | --- |
| Device | the device is offline / back online | `ITodoArrangement.Network` | `NetworkStateFile` (Bruinet.Mocking) | yes |
| App lifecycle | I restart the app; ... with the server unreachable | `TodoUatFixture`, `TodoWebUatFixture` (one class each) | the fixture's `Restart`, `RestartWith(ApiBaseUrl)`, and a DI rescope on MAUI | yes; "unreachable" is an app setting |
| Backend faults | the server fails with status {n}; fails once; answers after {n} seconds; recovers | `ITodoArrangement.Backend` | `MockApiServer` at priority 1 on `TodoApi.TodosPath` | the fault is; the endpoint and the reply are Todo's |
| Backend data | the server also has todos; should have todos; should have received the todo ... from {user} | `TodoServerStub` | Todo's stateful fake | no: domain |
| Stored data | the database should have todos / only todos / no blank title | `TodoDbReader` | the app's SQLite file | no: domain |
| Users | the server also has a user; should have users | `TodoServerStub` | Todo's fake | no: domain |

Other facts that shape the design:

| Fact | Consequence |
| --- | --- |
| `TestComposition.ForFixture` registers the fixture under every interface it implements, and phrase classes are built from the scenario's DI scope. | A promoted phrase class can take `IUatNetwork` in its constructor; any fixture that implements it gets the phrases. No registration code. |
| `Bruinet.Uat` depends on `Bruinet.Core` only, with no MAUI or mocking reference. | Interfaces and phrases can live there; implementations over `MockApiServer` or `NetworkStateFile` cannot. |
| `Bruinet.Mocking` has `NetworkStateFile`, `MockApiServer` and `ApiStubBuilder` (`Fails`, `FailsFirst`, `WithDelay`, `AtPriority`). `FailsFirst` needs a reply for after the failures, and there is no way to drop only the fault stubs. | "Fails once" and "recovers" cannot be written generically yet: Bruinet.Mocking needs to fall through to the normal stub, and to remove overrides. |
| Construction's UI tests use raw WireMock (`MockBackendHostManager`, static), their own network-state file (`NetworkStateOverride`), and `ConstructionFixture.RestartApp`. Its integration tests have `ConstructionBackend`. | Construction can implement the interfaces over what it has; moving to `Bruinet.Mocking` is a separate choice. |
| Phrases are namespaced, and the global scope is kept small (AD-019). | Promoted phrases keep their namespaces (`Device`, `AppLifecycle`, `Backend`); a suite opts in with `## Using`. |
| Mixed suites run several apps (AD-015); each app's steps resolve in that app's session scope. | A per-app `IUatNetwork` works if each session registers its own. The `{user}` forms are new phrases. |

## 3. Decisions

### S1. Promote the mechanism and the words, never the domain

A subject is promoted when its words mean the same in any app and only the mechanism differs:
the network, a restart, a server fault or delay. What names the app's data (todos, users, hour
logs, the database's tables) stays in the app: its words are the app's words, and its checks need
the app's types.

| Promoted | Stays in the app |
| --- | --- |
| the device is offline / back online | the server also has todos |
| I restart the app / ... with the server unreachable | the server should have users |
| the server fails with status {n}, fails once, answers after {n} seconds, recovers | the database should have todos |

### S2. One small interface per subject

```csharp
public interface IUatNetwork
{
    void SetOffline();
    void SetOnline();
}

public interface IUatRestartable
{
    // The fixture rescopes the scenario after a restart; the context says which scenario.
    void Restart(BddExecutionContext context);
    void RestartWithBackendUnreachable(BddExecutionContext context);
}

public interface IUatBackend
{
    // The request a bare "the server ..." step means: the app's main read (Todo's todo list).
    string DefaultEndpoint { get; }
    IReadOnlyCollection<string> Endpoints { get; }

    void Fail(string endpoint, int status, int? times = null);
    void Delay(string endpoint, TimeSpan delay);
    void Recover();
}
```

- **Small, one per subject**, so an app implements only what it has. A fixture without a network
  switch does not implement `IUatNetwork`, and a scenario that says "the device is offline" fails
  binding there. It does not fail at run time.
- **Endpoints have names** the fixture chooses (`todos`, `hours`, `projects`). A bare step means
  `DefaultEndpoint`; "the {endpoint} request fails with status {status}" names another. So a
  Construction scenario can fail the hours request without a URL in the prose.
- **`Recover` drops faults and delays only**, never the data the server holds. Todo's
  "recovers" re-stores its todos today because resetting the mock drops everything.

### S3. Interfaces and phrases in Bruinet.Uat; implementations in a small bridge

| Where | What | Depends on |
| --- | --- | --- |
| `Bruinet.Uat` (folder `Subjects/`) | `IUatNetwork`, `IUatRestartable`, `IUatBackend`; `DevicePhrases`, `AppLifecyclePhrases`, `BackendFaultPhrases` | Bruinet.Core (as now) |
| `Bruinet.Uat.Mocking` (new, small) | `NetworkStateFileNetwork : IUatNetwork`; `MockApiBackend : IUatBackend` over `MockApiServer`, with endpoints registered as (name, method, path) | Bruinet.Uat, Bruinet.Mocking |
| the app's test support | the fixture implementing the interfaces, directly or through the bridge's classes | its own |

Rejected: **a `Bruinet.Uat.Subjects` package** for interfaces and phrases alone (a package for
three interfaces, and the phrases are UAT vocabulary like the built-ins); **adapters inside
Bruinet.Mocking** (Mocking serves non-UAT tests and should not reference the UAT engine);
**interfaces in Bruinet.Core** (Core has no UAT types and should not gain them).

This answers the phrases plan's open question "Which package owns the shared subjects".

### S4. The words stay; the namespaces stay

The promoted phrases keep today's text, so no Todo scenario changes, and keep their namespaces
(`Device`, `AppLifecycle`, `Backend`), so a suite still opts in with `## Using`. A namespace can
be provided by two classes: Todo's `BackendPhrases` keeps its data phrases in `Backend` next to
the promoted fault phrases. Two classes must not declare the same text: that is `UATB002` where
both are seen, which is the point.

### S5. Named forms for several apps

Each promoted phrase gets its `{user}` form, as the built-ins have (AD-015): "{user}'s device is
offline", "{user} restarts the app". Each resolves in that user's app session, whose
`IUatNetwork` or `IUatRestartable` is that app's. A mixed fixture that shares one network for all
apps says so by registering one instance.

### S6. Bruinet.Mocking learns to fall through and to recover

- `FailsFirst(times, status)` with no reply falls through to the normal stub once it has failed
  that many times (a WireMock scenario state), so "fails once" needs no copy of the app's data.
- A delay the same way: `WithDelay(delay)` with no reply, on a request the app's own stub answers.
  If WireMock cannot delay and fall through, `MockApiBackend` asks the endpoint for its reply
  (`Func<object?>`), and Todo passes `() => Server.Todos`. Decide in step 2, with a test.
- `MockApiServer.ResetOverrides()`: removes the stubs added at a priority above the default,
  leaving the app's stubs and their data.

## 4. Steps

| # | Step | Done when |
| --- | --- | --- |
| 1 | **Interfaces and phrases** in `Bruinet.Uat/Subjects`, with the `{user}` forms. Unit tests with fake implementations: each phrase calls its interface; a fixture without the interface fails binding, not running; `DefaultEndpoint` and a named endpoint. | `Bruinet.Uat.Tests` green; no consumer yet. |
| 2 | **Bruinet.Mocking**: fall-through `FailsFirst`, delay (or the reply fallback, S6), `ResetOverrides`. **`Bruinet.Uat.Mocking`**: `NetworkStateFileNetwork`, `MockApiBackend`. | `Bruinet.Mocking.Tests` and new bridge tests green, against a real `MockApiServer`. |
| 3 | **Todo moves over**: `TodoAppFixture` and `TodoWebUatFixture` implement the three interfaces (the backend through `MockApiBackend` with endpoint `todos`); the UAT fixtures scan the promoted classes. Delete Todo's `DevicePhrases`, both `AppLifecyclePhrases`, and the fault phrases of `BackendPhrases`. | No scenario file changes. Todo MAUI UAT 41, web UAT 33, mixed 4, UI 44 (+3 skipped), the validator clean. `AppLifecycle` phrases exist once. |
| 4 | **Docs**: a "Shared subjects" section in `uat-step-definitions.md` (the interfaces, how a fixture gets the phrases, `## Using`); an AD for S1-S3 (AD-020: promote the mechanism, not the domain); CHANGELOG. | Links valid. |
| 5 | **Construction adopts them**, as a step of [construction/plan.md](../../construction/plan.md): `ConstructionFixture` implements `IUatRestartable` (it has `RestartApp`), `IUatNetwork` (over `NetworkStateFile`, replacing `NetworkStateOverride`), and `IUatBackend` over its WireMock server, with endpoints per module. | Construction's first scenario says "the device is offline" and binds. |

Steps 1-4 can be done before Construction needs them; step 5 is Construction's. Each step is one
commit.

## 5. Open questions

| Question | Notes |
| --- | --- |
| Endpoint names | Free text the fixture chooses, or the module names (`Hours`, `Projects`) so they match namespaces? Start free; revisit with Construction's modules. |
| "With the server unreachable" | Needs a launch setting for the backend address. Todo has `ApiBaseUrl`; Construction's is in its app settings. If an app cannot change it without a rebuild, it does not implement that member: split it into `IUatRestartable` and an optional `IUatBackendAddress`? |
| A pinned clock | "the date is 12-May-2026" would restart the app with another `FixedNow`: app-specific like the backend address. Not promoted until a second app pins its clock. |
| Stored data | A generic "the {table} table should have rows" reads SQLite by table and column, but ties Bruinet.Uat to SQLite and to column names testers should not see. Keep it in the app (S1). |
| Construction on Bruinet.Mocking | Step 5 can implement `IUatBackend` over raw WireMock. Moving `MockBackendHostManager` onto `MockApiServer` is Construction's migration, not this plan's. |
| Per-app network in mixed suites | Todo's mixed suite shares one network file. Is one device offline while the other is online a scenario anyone needs? If so, the mixed fixture gives each session its own `NetworkStateFile`. |
