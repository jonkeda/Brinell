# Several apps, several heads, named users: plan

Status: **done except the Android run** (2026-09-27): every step is built and green on Windows and web; the Android test and scenario are written and gated, and have not run on an emulator yet (section 6).

This plan does four things:

1. **Mixed heads.** One UAT scenario drives any number of MAUI Windows apps, an Android app and any
   number of web apps, all on one backend.
2. **Named subjects.** Every step whose phrase starts with `I` can also be written with a user's
   name: `When Daan taps OK` runs `I tap OK` as Daan.
3. **Users point to apps.** A user's name picks that user's app, so naming the user is enough to
   switch apps.
4. **The same for UI tests.** A C# xUnit test drives several apps through the same parts (section 4).

Decisions are in section 3. Each step's status and evidence go in section 6 as the work proceeds.
Phrase namespaces (`Using`) are **not** part of this plan. They belong to
[users-plan.md](../uat/phrases/users-plan.md), and D5 leaves room for them.

## 1. Goal

A scenario like this runs as written, in one test, with no sleeps:

```gherkin
@mixed @sync
## Scenario: A todo Daan creates on Windows reaches Eva's phone and Finn's browser

Given Daan is on the Todo List page
And Eva is on the Todo List page
And Finn is on the Todo List page
When Daan taps Add
And Daan sets Title to "Pack for the trip"
And Daan taps Save
And Daan taps Sync
Then the server should have received the todo "Pack for the trip" from Daan
When Eva taps Sync
Then Eva should see "Pack for the trip"
When Finn taps Sync
Then Todos should have todo "Pack for the trip"
```

The same journey as a UI test:

```csharp
[Collection(TodoAppsCollection.Name)]
public sealed class SyncAcrossAppsTests(TodoAppsFixture apps)
{
    [Fact]
    [Trait("Journey", "TOD.07.11")]
    [Trait("Pyramid", "H")]
    public void ATodoDaanCreatesOnWindows_ReachesFinnsBrowser()
    {
        var daan = apps.Maui("Daan");        // TodoMauiDevice, Windows
        var finn = apps.Web("Finn");         // TodoWebDevice

        daan.List.CreateTodo("Pack for the trip").Sync();
        apps.Server.WaitForTodoFrom(daan, "Pack for the trip");

        finn.List.Sync().Todos.AssertRow("Pack for the trip");
    }
}
```

Both read the same cast from one shared `apps.md` (D13):

```markdown
## Apps

| Name        | User | Head | Platform | ClientId    |
| ----------- | ---- | ---- | -------- | ----------- |
| Daan's PC   | Daan | maui | Windows  | maui-daan   |
| Gus's PC    | Gus  | maui | Windows  | maui-gus    |
| Eva's phone | Eva  | maui | Android  | android-eva |
| Finn's web  | Finn | web  |          | web-finn    |
| Hana's web  | Hana | web  |          | web-hana    |
```

Rules this plan keeps:

- **A single-app file does not change.** Every existing scenario, fixture and phrase binds and
  runs as it does today, with the same step count and the same results. Todo MAUI UAT stays at 32
  passing, Todo web UAT at 33.
- **No phrase is declared twice.** `I tap {control}` gains the `Daan taps OK` form from the engine,
  not from a second attribute. AD-011 stays as it is.
- **No sleeps and no coordinates** (AD-004, AD-005). Apps wait for each other on observed state:
  a request the backend recorded, or a row that appeared.
- **Core does not reference Uat**, and MAUI and Html keep no process-wide state
  (`.my/html/todo-blazor.md` section 4.2).
- **The forbidden APIs** (`AGENTS.md` "Never In Tests") apply per app: no `daan.Context.Driver`,
  no `FindElement`. Tests go through page objects.
- **One UI test process at a time.** A mixed suite is one process that holds several apps. It
  never runs next to another UI run.

## 2. What exists

Built on 2026-09-26 for **one head** (`.my/html/todo-blazor.md` section 4):

| Piece | Where | What it does |
| --- | --- | --- |
| `Apps` table | `UatConfig.Apps`, `UatAppConfig(Name, Values)` | One row per app. `Name` is required; the fixture reads the other columns. |
| `IUatSessionHost.UseSession(name)` | `UatSessions.cs` | Starts the app the first time, makes it current, and returns the `IServiceProvider` its pages resolve through. |
| `{app} uses the app` | `UatSessionSteps` | Registered as a Given and a When. Checks the name against `Apps` and swaps the context's scope. |
| Failures name the app | `UatScenarioRunner.NameTheSession` | Prefixes `[Alice]` to a failed step's message. |
| Several web devices | `TodoWebUatFixture`, `TOD.07-two-devices.uat.md` | Each app is a `TodoWebDevice` on one WireMock backend, with its own database, host, browser context and `X-Todo-Client`. |
| Android backend URL | `AndroidHost.EmulatorUrl` | Rewrites `localhost` to the host address as the emulator sees it. |
| Failure screenshot | `ScreenshotTestAttribute` | One `IScreenshotService` per async flow. |

What stops the goal today:

| # | Gap | Why it blocks |
| --- | --- | --- |
| G1 | **One catalog per runtime.** `UatRuntime` builds one `UatReflectionRuntime` from one composition. | The MAUI `TodoListPage` and the web `TodoListPage` are different types with the same page name. One catalog cannot hold both, and AD-014 rules it out explicitly. |
| G2 | **Binding does not know which app a step runs on.** `UatBinder.Bind(document, catalog)` binds every step against the one catalog. | Once there are several catalogs, a step has to bind against the catalog of the app it will run on. |
| G3 | **One MAUI app per fixture.** `MauiTestFixtureBase` launches its app in its constructor. `TodoAppFixtureBase` arranges the database, network file and launch settings in `CreateTestContextOptions`. | There is no MAUI counterpart of `TodoWebDevice`, so a fixture cannot start a second MAUI app or an Android app next to a Windows one. |
| G4 | **The platform is global.** `BrinellMauiConfiguration.Maui.Platform` and `BRINELL_AUT_PLACE` apply to the whole process. | A Windows app and an Android app in one process need a platform and a window placement per app. |
| G5 | **Page state is per scenario, not per app.** `UatExecutionContext.CurrentPageName` is one value. | Alice on the edit page and Bob on the list page overwrite each other's current page. |
| G6 | **`I` is literal text.** `I tap {control}` matches only text that starts with `I`. | `Daan taps OK` binds to nothing. |
| G7 | **Evidence covers one app.** Both UAT and `ScreenshotTestAttribute` capture the current app only. | When Bob's step fails, Alice's screen, which is often the cause, is not captured. |
| G8 | **No app machinery outside UAT.** Sessions are a UAT concept, and the Todo web fixture keeps its apps in a private dictionary. | A C# UI test has nothing to start, find, reset and capture several apps with. |

## 3. Decisions

### D1. A step's subject is part of the step, parsed at bind time

> **Replaced 2026-09-27** by [named-steps-plan.md](named-steps-plan.md): the named form is now
> declared (`{user} taps {control}` beside `I tap {control}`), and the verb inference below is
> gone. The subject rules (names from `Apps`, longest wins, `UATB005`) still hold.

When a step starts with a known user or app name, that name is its **subject**. The binder strips
the subject, turns the verb back into the first person, puts `I` in front, and matches the result
against the ordinary catalog.

| Written | Subject | Matched as |
| --- | --- | --- |
| `Daan taps OK` | Daan | `I tap OK` |
| `Daan tap OK` | Daan | `I tap OK` (the base form is accepted too) |
| `Daan is on the Todo List page` | Daan | `I am on the Todo List page` |
| `Daan should be on the Todo List page` | Daan | `I should be on the Todo List page` |
| `Daan has ...` / `Daan does ...` | Daan | `I have ...` / `I do ...` |
| `Eva's phone sets Title to "x"` | Eva's phone (an app name) | `I set Title to "x"` |

Turning the verb back is done by **trying candidates**, not by a grammar. From the first word after
the subject the binder tries, in order: the word as written; irregular forms (`is`→`am`, `has`→`have`,
`does`→`do`, `goes`→`go`); dropping `s`; dropping `es`; `ies`→`y`. Modal verbs (`should`, `must`,
`can`, `will`) are left alone. The step binds when **exactly one** candidate matches a phrase that
starts with `I`. When none match, the error lists the candidates tried. When more than one match,
the error names them all.

- **Why at bind time:** a wrong name, or a step no app can run, fails before any app starts, as an
  unbound step does today (`UATB001`).
- **Why names come from the `Apps` table and nowhere else:** `Todos should have todo "x"` starts
  with a capitalised word that is a control. Only names the config declares count as subjects.
  Binding reports an error (`UATB005`, new) when a user or app name is also a page name.
- **Longest name wins:** `Eva's phone` beats `Eva`.
- **A step that matches as written is not rewritten.** Rewriting is tried only when the step starts
  with a subject, and only against phrases that start with `I`.
- **Only phrases that start with `I` gain a subject form.** `the server should have received ...`
  has no actor, and `Daan the server ...` binds to nothing.
- **There is no bracket syntax** (`[Daan] tap OK`). Subjects read as English.

### D2. The subject sticks, like `uses the app`

A step with a subject makes that app current for the rest of the scenario. A later step with `I`,
or an `And` with no subject, runs on it:

```gherkin
When Daan taps Add
And I tap Save         # Daan's app
```

`{app} uses the app` stays and does the same thing. The guide advises naming the user on every
step of a scenario with several users. The report shows the app each step ran on.

Before the first switch, a scenario runs on the fixture's own app, which is today's behaviour. A
`Background` step runs on that default app unless it names a subject.

### D3. `{user}` in a phrase is the acting user

A phrase can take the user as a parameter by naming it `{user}`, which is now a **reserved
parameter name**:

```csharp
[UatThen("{user} should have {count} todos on the server")]
public void AssertServerCount(string user, int count) => ...
```

- `Then Daan should have 3 todos on the server` → `user = "Daan"`, and Daan's app becomes current.
- `Then I should have 3 todos on the server` → `user` is the current app's `User`. The step fails
  when no app is current.
- A phrase with `{user}` anywhere other than first place gets the argument but does not switch
  apps: `the server should have received the todo {title} from {user}`. The argument is checked
  against the `Apps` table's users and app names at bind time.

### D4. Users map to apps through the `Apps` table

The `Apps` table gains two optional columns the **engine** reads. The other columns stay the
fixture's to read.

| Column | Meaning | Default |
| --- | --- | --- |
| `User` | Who uses this app. Several apps may have the same user. | The `Name`. So today's `Alice` / `Bob` tables work unchanged, with each name serving as both user and app. |
| `Head` | Which page and phrase catalog the app uses (`maui`, `web`). | The suite's `Head`. |

A subject resolves as follows:

1. It is an app `Name`: that app.
2. It is a `User` with one app: that app.
3. It is a `User` with several apps: the one of those that was used last in this scenario. At bind
   time, if none has been used yet, the step fails and asks for `Daan uses Daan's phone` first.

The rules are implemented once, in `TestAppSet.Resolve` (D12), and used by the UAT binder and by UI
tests. Rule 3 needs the scenario's memory, so `Resolve` takes a "last used" hint.

### D5. One catalog per head, chosen per step

A session host can name the composition each app's head discovers pages and phrases from.
`UatRuntime` groups the apps by head, builds one `UatReflectionRuntime` and one `UatCommandCatalog`
per head, and passes the binder a `UatCatalogSet` (head → catalog, plus the apps). The binder:

- tracks the current app **statically** through the scenario, from subjects (D1), `uses the app`
  steps and the default in D2;
- binds each step against that app's head catalog;
- records the app on the invocation (`UatStepInvocation.App`, new).

The runner switches apps **before** a step whose `App` differs from the current one. A step never
runs against another head's page types, which is the "Broken when" condition of AD-014. A
**one-head** suite has one entry in the set, and nothing it does changes.

**Room for namespaces.** What a step can see is decided in **one** place,
`UatCatalogSet.CatalogFor(app)`. Pages are keyed by head and name. When namespaces arrive
([users-plan.md](../uat/phrases/users-plan.md) D8), that one place becomes "this app's head, and the file's
usings", and the key gains a namespace. That is an addition, not a rework.

### D6. Per-app scenario state

`UatExecutionContext` keeps a small state per app: its current page and its service provider.
`CurrentPageName` and the service-provider item read and write the current app's slot. A scenario
with no apps has one slot, so it behaves as it does today.

### D7. A mixed suite is its own head, `mixed`

- `Head | mixed` in a suite's `uat.config.md` means "apps of several heads". It runs only
  scenarios tagged `@mixed`.
- A one-head suite skips `@mixed` scenarios, with a reason that names the tag.
- `mixed` joins `UatTagConventions.Heads`.
- Mixed scenarios live in the mixed suite's own `Scenarios` folder. They are not linked into the
  MAUI or web suites, because they cannot run there. AD-014 still holds for every one-head
  scenario.
- Mixed and one-head suites share phrase **wording**, not phrase classes: each app's head brings
  its own `TodoScreenPhrases`.

### D8. Each MAUI app is a device, like each web app

`TodoAppFixtureBase` today does two jobs: it is a single-app xUnit fixture, and it knows how to
arrange a Todo app. The arranging moves into **`TodoMauiDevice`**, the MAUI twin of
`TodoWebDevice`:

- `Start(name, testSet, backendUrl, clientId, platform, placement, adjust)` seeds the device's own
  database, writes its network file, builds its `LaunchSettings` and creates a `MauiTestContext`
  with **per-device** `MauiDriverOptions`, including the platform.
- It provides `List`, `Restart`, `RestartWith`, `UseTestSet`, `Network`, `Context` and
  `ScreenshotService`.

`TodoAppFixtureBase` keeps its public members and behaviour, so the 32 MAUI UAT scenarios and the
Todo UI tests are the regression check.

In `Brinell.Maui`:

- `MauiDriverOptions` already carries the platform, so no global needs to change. A device just
  stops reading `Configuration.Maui.Platform`.
- **Window placement gets a per-launch option.** `MauiDriverOptions.Placement` overrides
  `BRINELL_AUT_PLACE` for one app, so two Windows apps can be tiled instead of stacked.
  `BRINELL_AUT_PLACE` stays the default.
- `MauiTestContext` gets a **name** (the app's name), used in `ControlCall` log lines and
  exception messages, as `HtmlTestContextOptions.Name` does.

### D9. One backend, reachable by every app

The mixed fixture starts one `MockApiServer` and gives every app its URL:

- Windows and web apps get `localhost`.
- The Android app gets `AndroidHost.EmulatorUrl(url)`, which is how the single-app Android runs
  already work. The WireMock server must accept that address. Step 4.4 checks this: it either
  listens on all interfaces, or the fixture uses `adb reverse`.
- The test set's server half loads once. Each app gets its own local half, meaning its own seeded
  database.
- Every app sends its `ClientId` as `X-Todo-Client`, so "the server received it from Daan" is a
  wait on a recorded request.

### D10. Limits of the first version

| Limit | Why |
| --- | --- |
| **One Android app per scenario** | One emulator runs one instance of an app package. A second needs a second emulator with its own `udid` and `systemPort`, which later becomes `Device` and `SystemPort` columns. |
| **Users share one todo list** | `Todo.Api` has one list. "Daan" and "Eva" are two clients of one list, not two accounts. Real users are out of scope. |
| **Steps run in sequence** | A scenario is a script: "at the same time" is two steps with a wait between them. |
| **Apps start one at a time** | Serial start is simpler, and it keeps FlaUI's one-driver-at-a-time start-up assumption. Measure before changing. |
| **Hermetic backend only** | The live `Todo.Api` fixture is one-app today. |

### D11. Evidence from every app

When a step or a UI test fails, **every started app** is captured, not only the current one. Files
go into one subfolder per app under the suite's artifacts (AD-007), and the UAT result JSON lists
each path. Both paths use `TestAppSet.CaptureAll` (D12). A fixture without a set keeps today's
single capture.

For UI tests, `ScreenshotTestAttribute` gains `SetApps(TestAppSet)`. When a test with apps
registered fails, it calls `CaptureAll`.

### D12. Apps live in Core, and UAT and UI tests share them

**`Brinell.Core.Testing` owns apps; `Brinell.Uat` owns words.** Core does not reference Uat.

| Part | Where | UAT uses it for | UI tests use it for |
| --- | --- | --- | --- |
| `TestAppDefinition` | `Brinell.Core.Testing` | A row of the `Apps` table | A row of the same table |
| `ITestApp` | `Brinell.Core.Testing` | A session | `daan`, `finn` |
| `TestAppSet` | `Brinell.Core.Testing` | Sessions, subject resolution, `CaptureAll` | `apps.Maui("Daan")`, reset between tests, failure evidence |
| Device per app | the app's test support (`TodoMauiDevice`, `TodoWebDevice`) | The app behind a session | The app behind a variable |
| Named contexts | `MauiTestContext` and `HtmlTestContext` | `[Daan's PC]` in failures | The same |
| Per-app placement | `MauiDriverOptions.Placement` | Windows tiled | Windows tiled |
| Subject grammar, current app, catalog per head | `Brinell.Uat` | Yes | **No**: in C#, apps are variables with their own page types |

```csharp
namespace Brinell.Core.Testing;

public sealed record TestAppDefinition(
    string Name, string User, string Head, IReadOnlyDictionary<string, string> Values);

public interface ITestApp : IAsyncDisposable
{
    TestAppDefinition Definition { get; }
    IScreenshotService Screenshots { get; }
    ValueTask ResetAsync(CancellationToken cancellationToken = default);   // online, on its first page
}

public sealed class TestAppSet : IAsyncDisposable
{
    public TestAppSet(IEnumerable<TestAppDefinition> definitions,
                      Func<TestAppDefinition, CancellationToken, Task<ITestApp>> start);

    public IReadOnlyList<TestAppDefinition> Definitions { get; }
    public IReadOnlyList<ITestApp> Started { get; }
    public ITestApp Get(string nameOrUser);                                 // started on first use
    public TApp Get<TApp>(string nameOrUser) where TApp : ITestApp;          // throws naming the head
    public TestAppDefinition Resolve(string nameOrUser, IEnumerable<string>? lastUsed = null);
    public Task ResetStartedAsync(CancellationToken cancellationToken = default);
    public IReadOnlyList<string> CaptureAll(string testClass, string testName, string description);
    public ValueTask DisposeAsync();                                         // reverse start order
}
```

- **Errors name the choices:** `No app or user 'Dan'. Apps: Daan's PC (Daan), Eva's phone (Eva),
  Finn's web (Finn).`
- **Start is lazy.** A test that uses two apps of a five-app fixture starts two. Apps a test did
  not use stay stopped, and only started apps are reset.
- On the UAT side, `IUatAppsHost { TestAppSet Apps; TestComposition CompositionFor(ITestApp) }`
  gives a fixture sessions, subject resolution and `CaptureAll` without writing `UseSession`.
  `TodoWebUatFixture`'s private dictionary is replaced by a `TestAppSet`, which proves the adapter
  on the one multi-app suite that exists.

### D13. One `apps.md` for both suites

The mixed UAT suite and the mixed UI tests read **one** table. Two lists of the same cast would
drift, which is the same argument as AD-014.

- The markdown table reader moves from `Brinell.Uat` into `Brinell.Core` as a plain table reader
  with no UAT meaning. `UatConfigParser` uses it from there.
- `TestAppDefinitions.FromMarkdown(path)` reads an `Apps` table, in Core.
- The mixed UAT suite's `uat.config.md` names the file (`AppsFile | ../Brinell.Samples.Todo.TestSupport.Apps/apps.md`)
  instead of repeating the table. An inline `Apps` table still works, as it does today.

### D14. The same member names on both heads

`daan.List.CreateTodo(...)` and `finn.List.CreateTodo(...)` must mean the same thing. AD-013
already says Html controls have MAUI's shape. This plan extends that to the Todo **page objects**:
the same member for the same action on both heads. For example, MAUI's `Add()` and the web's
`AddTodo()` become one name. There is no shared `ITodoListPage` interface, and no head-neutral C#
test bodies: UAT scenarios are the head-neutral form, and a C# mixed test says exactly which heads
it uses.

## 4. The UI-test side

### 4.1 `TodoAppsFixture`

It lives in a new project, `samples/Todo/tests/Brinell.Samples.Todo.TestSupport.Apps`
(`net10.0-windows`). That project references `Todo.UITests` (MAUI pages and `TodoMauiDevice`) and
`Todo.Web.UITests` (web pages and `TodoWebDevice`). The devices stay next to their head's page
objects.

```csharp
public sealed class TodoAppsFixture : IAsyncLifetime
{
    public TestAppSet Apps { get; }          // from apps.md
    public TodoServerStub Server { get; }    // the one backend, with WaitForTodoFrom(app, title)

    public TodoMauiDevice Maui(string nameOrUser) => Apps.Get<TodoMauiDevice>(nameOrUser);
    public TodoWebDevice Web(string nameOrUser) => Apps.Get<TodoWebDevice>(nameOrUser);

    public Task StartTestAsync();            // backend reset; every started app reset
}
```

- **The start function switches on `Head` and `Platform`:** a `TodoMauiDevice` on Windows or
  Android, or a `TodoWebDevice`. Each app gets its own seeded database, the backend URL
  (`AndroidHost.EmulatorUrl` for Android), and its `ClientId` as `X-Todo-Client`. On Windows each
  app also gets a **placement slot**: the first MAUI window goes left, the second right.
- `Get<TodoMauiDevice>("Finn")` when Finn is a web app **throws**, naming Finn's head.
- The one-app fixtures (`TodoAppFixture`, `TodoWebFixture`) stay as they are.

### 4.2 The mixed UI test project

`samples/Todo/tests/Brinell.Samples.Todo.Mixed.UITests` has one xUnit collection
(`TodoAppsCollection`) and no parallelism.

| Test | Apps | Journey |
| --- | --- | --- |
| A todo created on Windows reaches a browser after it syncs | Daan (Windows), Finn (web) | TOD.07.11 |
| Two Windows apps side by side: an edit in one reaches the other | Daan, Gus (both Windows) | TOD.07.12 |
| Conflict: Daan and Finn edit the same todo, and the newer one wins on both | Daan, Finn | TOD.07.13 |
| Offline: Finn goes offline, Daan edits, Finn comes back and syncs | Daan, Finn | TOD.07.14 |
| A todo created on Android reaches Windows | Eva (Android), Daan | TOD.07.15 |

The Android test is skipped unless `BRINELL_ANDROID=1`, and the reason is shown. It is not an
`if` in the test.

## 5. Steps

Each step ends green on the tiers named, with no new warnings. Commands run from the Brinell root.

### Phase 1 - Engine: subjects, users and a catalog per head (`Brinell.Uat` only)

| # | Step | Tests (new unless noted) |
| --- | --- | --- |
| 1.1 | `UatAppConfig` gains `User` (defaults to `Name`) and `Head` (defaults to the suite's). | Config parsing with and without the columns. |
| 1.2 | **Subject rewrite in the binder** (D1): candidate verb forms, the unique-match rule, `UATB005` for a name that collides with a page, and a "no first-person phrase" error listing the candidates tried. | `Daan taps OK` → `I tap OK`; `is`/`has`/`does`; `advances`→`advance` and not `advanc`; `presses`→`press`; `tries`→`try`; modals; the ambiguity error; `Todos should have ...` with no app named Todos stays untouched; longest name wins. |
| 1.3 | **Static current-app tracking** (D2, D4): the binder follows subjects and `uses the app` steps, and sets `UatStepInvocation.App`. The runner switches apps before a step whose `App` changes. | A subject switches and sticks. `And I ...` runs on the last subject. A user with two apps and none used yet fails at bind time. A single-app file binds to exactly the same invocations as before. |
| 1.4 | **`{user}` reserved parameter** (D3). | Both placements. `I ...` with no current app fails. An unknown user in a later position fails at bind time. |
| 1.5 | **Per-app state** (D6). | Two apps on different pages keep their own `CurrentPageName`. |
| 1.6 | **Catalog per head** (D5), with the one visibility point. | A fake host with two heads whose `Main` pages are different types: each step resolves its own head's page type. A one-head host builds one catalog. |
| 1.7 | **`mixed` head** (D7). | A one-head suite skips `@mixed`. A mixed suite runs only `@mixed`. |
| 1.8 | **Report**: `app` per step in `__result.json`. | Report contents. |

Verify: build `srcnew\Brinell.sln`, `Brinell.Uat.Tests`, `Brinell.Core.Tests`,
`Brinell.Presenter.Uat.Tests`, then the Todo MAUI UAT (32) and the Todo web UAT (33), unchanged.

### Phase 2 - Todo: the subject form on one head

| # | Step | Evidence |
| --- | --- | --- |
| 2.1 | Rewrite `TOD.07-two-devices.uat.md` with subjects: `Given Bob is on the Todo List page`, `When Alice taps Add`, and so on. | Todo web UAT: 33 passed. |
| 2.2 | `the server should have received the todo {title} from {user}` (D3). | Same run. |

### Phase 3 - Apps in Core (D12, D13)

| # | Step | Tests |
| --- | --- | --- |
| 3.1 | Move the markdown table reader to Core. `TestAppDefinition`, `TestAppDefinitions.FromMarkdown`, `ITestApp` and `TestAppSet`. | `Brinell.Core.Tests`: lazy start, reset of started apps only, resolution rules, the wrong-kind `Get<T>` error, reverse-order disposal, reading an `Apps` table. |
| 3.2 | `ScreenshotTestAttribute.SetApps` (D11). | Core tests with fake apps. |
| 3.3 | `IUatAppsHost`, `AppsFile` in `uat.config.md`, and resolution through `TestAppSet.Resolve`. | `Brinell.Uat.Tests`. |
| 3.4 | `TodoWebDevice` implements `ITestApp`. `TodoWebUatFixture` swaps its dictionary for a `TestAppSet`. | Todo web UAT: 33 passed, TOD.07 included. |

### Phase 4 - MAUI: a device per app (D8, D14)

| # | Step | Evidence |
| --- | --- | --- |
| 4.1 | `MauiTestContext` name, and `MauiDriverOptions.Placement`. | `Brinell.Maui.Tests`. Maui UI tier 1 (`AutomationProbeTests`). |
| 4.2 | Extract `TodoMauiDevice` from `TodoAppFixtureBase` and implement `ITestApp`. A mechanical refactor: the same tests pass identically. | Todo UI tests, then the Todo MAUI UAT: 32 passed. |
| 4.3 | The same member names on both heads (D14). | Todo UI tests and web UI tests, same counts. |

### Phase 5 - The mixed suites

| # | Step | Evidence |
| --- | --- | --- |
| 5.1 | `Brinell.Samples.Todo.TestSupport.Apps`: `TodoAppsFixture`, `apps.md`, `Server.WaitForTodoFrom`. | Builds. |
| 5.2 | **Two Windows apps side by side**, first as the TOD.07.12 UI test. | Passes 5 times in a row. Screenshot of both windows. |
| 5.3 | `Brinell.Samples.Todo.Mixed.UITests`: the section 4.2 tests, without Android. | All pass, 3 runs in a row. A forced failure captures every app. |
| 5.4 | `Brinell.Samples.Todo.Mixed.Uat`: `Head | mixed`, `AppsFile`, and a fixture that is `TodoAppsFixture` plus `CompositionFor`. Scenarios: (a) the section 1 goal without Android; (b) two Windows apps and a web app; (c) a conflict; (d) offline. | Discovery reports two catalogs. All pass, 3 runs in a row. |
| 5.5 | **Android in the mix**: Eva's phone on `emulator-5554` and the backend reachable from it (D9). Tagged `@android` in UAT and gated in the UI tests, so both skip unless `BRINELL_ANDROID=1`. | Passes with the emulator; skips with the reason shown without it. |
| 5.6 | Both mixed projects in the solution, `TestRunner.ps1` (run on their own) and the journey coverage (TOD.07.11 to TOD.07.15). | `JourneyCoverage` green. |

### Phase 6 - Docs

| # | Change |
| --- | --- |
| 6.1 | **AD-014 amended**: apps of different heads are allowed in a `mixed` suite, where each app binds against its own head's catalog. |
| 6.2 | **New AD-015 "A step's subject picks its app"**: D1 to D4. Ruled out: a second attribute per phrase for the third person (it breaks AD-011's "declared once"); names outside the `Apps` table (they collide with controls); a non-sticky subject; bracket syntax. |
| 6.3 | `.docs/invariants.md`: one line each for AD-014 (amended) and AD-015. |
| 6.4 | `docs/guides/uat-phrases-and-flows.md`: a section called "Several users and apps". `docs/guides/uat-template-guide.md`: `Head | mixed`, `@mixed`, `Apps`, `AppsFile`. `docs/architecture/testing.md`: `TestAppSet`, and several apps in a UI test. `samples/Todo/README.md` and `uat-walkthrough.md`: the mixed suites. |
| 6.5 | `CHANGELOG.md` under Unreleased. `.my/html/todo-blazor.md` section 4: status points here. |

## 6. Progress

| Step | Status | Evidence |
| --- | --- | --- |
| 1.1-1.8 | Done 2026-09-26 | `UatCatalogSet` (the one visibility point, `CatalogFor`), subjects and `UATB005`-`UATB007` in `UatBinder`, per-app state and `SwitchApp` on `UatExecutionContext`, `IUatSessionHost.CompositionFor`, `UatRuntime.CreateCatalogSet`, the `mixed` head, `app` per step in `__result.json`. New Core overload `TestComposition.ForFixture(fixture, options)` and `TestCompositionDiscoveryOptions.FromScans` for a composition per head. Tests: `UatSubjectTests` (29), `UatHeadsTests` (4), one more in `UatSessionTests`. Uat.Tests 171 passed, Core.Tests 16, Presenter.Uat.Tests 49, Todo web UAT 33, Todo MAUI UAT 32. |
| 2.1-2.2 | Done 2026-09-26 | TOD.07.3 written with subjects (`Alice taps Add`, `Bob taps Sync`); `... from {user}`. The result JSON shows each step on Alice's or Bob's app. `TodoWebUatFixture.UseSession` now opens the list only when an app starts, so switching back leaves an app where it was. Todo web UAT 33 passed. |
| 3.1-3.4 | Done 2026-09-26 | `MarkdownTables` (Core, `Utilities`), used by `UatConfigParser`; `TestAppDefinition`, `ITestApp`, `TestAppSet`, `TestAppCapture` (Core, `Testing`); `ScreenshotTestAttribute.SetApps`; `IUatAppsHost` and `AppsFile`. `TodoWebDevice` is an `ITestApp`, and `TodoWebUatFixture` keeps its apps in a `TestAppSet`. Tests: `TestAppSetTests` (11), `UatAppsHostTests` (2). Core.Tests 27, Uat.Tests 173, Todo web UAT 33. |
| 4.1 | Done 2026-09-27 | `MauiTestContextOptions.Name` and `IMauiTestContext.Name` (in each call-log entry, through `ControlCall`); `MauiDriverOptions.Placement` travels in the launch environment, and `WindowPlacement` gains `left-half` and `right-half`. Maui.Tests 186 passed (1 skipped), Maui UI tier 1 3 passed. |
| 4.2 | Done 2026-09-27 | `TodoMauiDevice` (an `ITestApp`) and `TodoMauiLaunch`, the launch code both it and `TodoAppFixtureBase` use. **Deviation:** the fixture base keeps its own lifecycle rather than wrapping a device, because `MauiTestFixtureBase` launches from its constructor; the shared part is the helper. Todo UI tests 36 passed (3 skipped: the live tier), Todo MAUI UAT 32. |
| 4.3 | Done 2026-09-27 | Web `AddTodo()` is now `Add()`, as on MAUI. The other differences are head-specific parts (section cards, dialog titles), and both heads' Save and Cancel have the UAT names `Save` and `Cancel`. Web UI tests 10 passed (1 skipped: live). |
| 5.1 | Done 2026-09-27 | `Brinell.Samples.Todo.TestSupport.Apps`: `apps.md` (five apps, with `ClockOffsetMinutes` so two edits are not at one instant), `TodoAppsFixture` (one backend, lazy start by `Head` and `Platform`, left and right halves for the first two Windows apps, `WaitForTodoFrom`, `Run`, which captures every app on a failure and throws `AppsTestFailure` listing the screenshots). `WaitForTodoFrom` is on the fixture, not on `Server`: the client id per app is the fixture's knowledge. |
| 5.2-5.3 | Done 2026-09-27 | `Brinell.Samples.Todo.Mixed.UITests`: TOD.07.11 (Windows to browser), TOD.07.12 (two Windows apps side by side, an edit), TOD.07.13 (conflict, newer edit wins on both), TOD.07.14 (offline browser: Sync is disabled, then the todo arrives), TOD.07.15 (Android, gated). 4 passed, 1 skipped, in 4 runs out of 4. A forced failure captured Daan's PC, Gus's PC and Finn's web, one folder each; Gus's clock read 10:05. **Finding:** `ScreenshotTestAttribute.SetApps` stores an `AsyncLocal`, which a collection fixture's `InitializeAsync` cannot hand to the test's flow, so the mixed tests capture through `TodoAppsFixture.Run` instead. `SetApps` stays for a test that registers in its own flow. |
| 5.4 | Done 2026-09-27 | `Brinell.Samples.Todo.Mixed.Uat` (`Head | mixed`, `AppsFile | apps.md`), `TodoMixedUatFixture : TodoAppsFixture, IUatAppsHost` with a module set per head, and `TodoMixedPhrases` (`... from {user}`, `{user} is offline`, `{user} is back online`). Scenarios TOD.07.11, .12, .14: 3 passed, 3 runs in a row. The conflict (.13) is covered by the UI test only: editing a todo needs head-specific phrases. |
| 5.5 | Written, not run | TOD.07.15 in both projects, gated by `BRINELL_ANDROID=1` (UI: `AndroidFact`; UAT: a skip rule on `@android`). No emulator was attached (`adb devices` empty), so both only skipped, with the reason shown. |
| 5.6 | Done 2026-09-27 | The three projects are in `Brinell.Samples.Todo.slnx`, and TOD.07.11-15 are in `journeys.md`. `JourneyCoverage` is green again (7 passed): it loads the unit and integration tests as before, and reads the UI tests (Windows, web, mixed) as metadata from their build output (`MetadataLoadContext`), since two of them are `net10.0-windows`. The removed `UITests.Mobile` is gone from the catalog and the slnx, and the solution builds. The mixed tests' `Pyramid` trait is `UiHermetic` (it was `H`). `samples/TestRunner.ps1` has no Todo entries to extend. |
| 6.1-6.5 | Done 2026-09-27 | AD-014 amended; AD-015 new and listed in `.docs/README.md`; `invariants.md`; `uat-phrases-and-flows.md` ("Several Users And Apps", source files); `uat-template-guide.md` (`mixed`, `Apps` columns, `AppsFile`, `IUatAppsHost`); `testing.md` ("Several Apps In One Test"); Todo `README.md` and `uat-walkthrough.md`; `CHANGELOG.md`; `.my/html/todo-blazor.md` points here. |
| Final | 2026-09-27 | `Brinell.sln` builds. Core 27, Uat 174, Presenter.Uat 49, Maui 186 (1 skipped), Html 39, Blazor 173. Maui UI full suite 350 passed, 2 skipped, 1 failed: `OccludedScreenshotTests` fails its own instrument check ("the occluding window did not cover the screen"), alone too; it reads screen pixels and uses no app placement, so it is the environment case in AGENTS.md, not established against a pre-change baseline. Todo UI 36 (3 skipped), web UI 10 (1), MAUI UAT 32, web UAT 33, mixed UI 4 (1), mixed UAT 4 (the Android scenario counts as a skip-pass). |

## 7. Decided questions

| # | Question | Decision |
| --- | --- | --- |
| Q1 | A literal `[user]` form beside `Daan taps OK`? | No. Subjects (D1) need no new syntax. `{user}` is only a phrase parameter (D3). |
| Q2 | Should the subject stick, or apply to one step? | It sticks (D2). |
| Q3 | Two Android apps in one scenario? | Later: two emulators (D10). |
| Q4 | Real users on the Todo backend? | Out of scope. |
| Q5 | Accept the base form (`Daan tap OK`)? | Yes (D1). |
| Q6 | One shared `apps.md` for the mixed UAT and UI test projects? | Yes (D13). |
| Q7 | Should the devices move into `TestSupport.Apps`? | No. They stay next to their head's page objects (4.1). |
| Q8 | Start apps in parallel? | No, not in v1 (D10). |
| Q9 | Head-neutral C# test bodies? | No (D14). |
| Q10 | Should the one-app fixtures become a `TestAppSet` of one? | Not in this plan (4.1). |

## 8. Risks

- **Two FlaUI-driven windows of one app.** FlaUI attaches by process, so this should work, but the
  Todo app has never run twice at once. A named mutex, a shared settings file or a fixed port
  would show up in step 5.2.
- **Android makes the run slow** (`.my/android/plan.md` A16). Mixed Android scenarios stay few and
  opt-in.
- **Verb candidates can bind a step the author did not mean.** Requiring exactly one match, and
  treating only names from `Apps` as subjects, keep this narrow. The bind error lists what was
  tried.
