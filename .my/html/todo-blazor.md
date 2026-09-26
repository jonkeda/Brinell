# The Todo app in Blazor, and more than one app in a scenario

Status: **draft**, 2026-09-26. Plan steps **T1** and **T2** in [plan.md](plan.md) build and test the
app. Section 4 describes a later project: several apps in one scenario. It is not built here, but
Html work in this plan must not rule it out.

## 1. Why

- **A real app for Html.** `Brinell.Samples.Blazor.App` is a control gallery. MAUI's call
  model was proved on the Todo app: its toolbar Cancel bug started the whole project, and TOD.04.4
  is its R0 proof. Html needs an app of the same kind, with pages, a backend, a local store, sync,
  and journeys.
- **Two clients, one backend.** In a later stage the MAUI app and the Blazor app run together,
  and the tests check that the backend received the right messages from each (section 4).

## 2. The app

`samples/Todo/src/Brinell.Samples.Todo.Web`: a Blazor Web App with interactive **server**
rendering. WebAssembly stays out, as in [plan.md](plan.md) section 6.

| Reused as is | From |
| --- | --- |
| Models, rules, view models, the sync planner | `Brinell.Samples.Todo.Core` (plain `net10.0`, no MAUI) |
| DTOs, routes, `X-Api-Key` | `Brinell.Samples.Todo.Contracts` |
| SQLite repository, API client, sync service, `LaunchSettings` | `Brinell.Samples.Todo.Infrastructure` |
| MVVM base (`ParentViewModel`, commands) | `Brinell.Samples.Shared` |

| New in the Web project | Notes |
| --- | --- |
| `INavigator` | Implemented on `NavigationManager` |
| `IDialogs` | A modal component for the delete and discard confirmations. Its buttons get `data-testid`s so page objects can find them |
| `IConnectivityState` | Nothing new: `AddTodoServices` without device connectivity uses the network-state file only, or always online |
| Pages `List`, `Detail`, `Edit` | Same behaviour and the same three states on List (Empty, Loading, Error) as the MAUI app |
| Components `TodoStatus`, `SectionCard` | Web versions of `TodoStatusView` and `SectionCard`. The Html ControlObjects are `TodoStatus<TScope>` (a component) and `SectionCard<TParent>` (a container) |
| Busy signal | `<body data-busy>` written from the page view model's `IsBusy` (JS interop after each render in which it changed). This is the Todo version of [sample-app.md](sample-app.md) item 3 and [design.md](design.md) 7.2 |
| Rows | `data-item-key="@todo.Id"` on every list row (design 7.6, `Logical` key) |

**One host process is one device.** It reads the same `LaunchSettings` environment variables as
the MAUI app (`TODO_DB_PATH`, `TODO_API_BASEURL`, `TODO_API_KEY`, `TODO_NOW`,
`TODO_SYNC_ON_START`, `TODO_NETWORK_STATE_FILE`), plus the URL it listens on. Its SQLite file is
the "device" store, so the offline-first and sync behaviour is the same as MAUI's. Two host
processes are two devices. Section 4 depends on this.

**Test ids equal the MAUI AutomationIds.** Where the MAUI app sets `AutomationId="TitleEntry"`,
the web page sets `data-testid="TitleEntry"`. The two page-object sets then use the same names, and
a UAT scenario can run against either app (see ?T2 below).

## 3. The tests (step T2)

| Project | What |
| --- | --- |
| `tests/Brinell.Samples.Todo.Web.UITests` | Tier 3 (hermetic: `TodoServerStub` on WireMock, a seeded database, a pinned clock) and tier 4 (live `Todo.Api`) for the web head. Html page objects and ControlObjects only. Built on the finished call model (plan steps 1-7), so its page objects are written once |
| `TodoWebFixture` | Starts the web host **as its own process**, on a free port, with the test set's launch settings. Waits for the host's health endpoint and then for the page. No fixed `5180`, no `BLAZOR_APP_URL` |
| UAT | The existing `Scenarios/*.uat.md` run with `Target: Html` against the web head. See ?T2 |

Test data follows [AD-012](../../.docs/decisions/ad-012-test-data-is-typed-validated-and-chosen-by-the-scenario.md)
unchanged: the same `TestSupport/TestData/*.json` test sets. Their local half seeds the web host's
SQLite file and their server half goes into `TodoServerStub`.

Journeys: the same `TOD.xx.y` ids and `Pyramid` traits, plus a `Head` trait (`Maui`, `Web`).
`JourneyCoverage` counts a subtask as covered in a tier when **any** head covers it. It checks
the pyramid shape per head, so adding the web head cannot flip the pyramid.

**R0 proof for Html (plan step 8).** Put back the Todo `CanExecuteChanged` bug in the shared view
model on a branch. TOD.04.4 must then fail on **both** heads, and each failure must name what it
saw. Because the view model is shared, one bug gives a matched pair of proofs.

## 4. Several apps in one scenario (later project; do not rule it out now)

Examples: the MAUI app and the web app sync through one backend, and the backend has each
client's messages. Alice creates a todo, Bob syncs and sees it. Two users edit the same todo and
the conflict rule decides.

### 4.1 What it needs

| Need | Shape |
| --- | --- |
| **Named app sessions** | A scenario holds N sessions: name, stack (MAUI, Html), app path or host, launch settings, user. Each session has its own test context, driver and page objects. |
| **One backend, shared** | One `TodoServerStub` (hermetic) or one `Todo.Api` (live) for all sessions. The server half of the test set is loaded once. Each session gets its own local half. |
| **Who sent it** | Every API request carries `X-Todo-Client` (set from the `TODO_CLIENT_ID` launch setting, **built in T1**). WireMock records it, and `Todo.Api` logs it (`RunningTodoApi.Requests`). `WaitForRequest` can filter on it, so "the backend received Alice's PUT" is a wait on a request, not a count. |
| **Who the user is** | `Todo.Api` has one shared list today. Multi-user needs a user identity (per-user API key, or an `X-Todo-User` header the API scopes by) and a per-user list on the server. This is a change to `Contracts` and the API, see ?T3. |
| **UAT** | `uat.config.md` gains an `Apps` table (Name, Target, Fixture, AppPath or Url, User, local TestData). A step switches the current session (`Given Alice uses the web app`, or a leading `As Alice,`). A single-app file keeps working unchanged. |
| **Artifacts** | [AD-007](../../.docs/decisions/ad-007-shared-artifact-layout.md) layout, with one subfolder per session under the suite, for screenshots and call logs. |
| **Failures name the session** | A failure message and its log pair say which app it came from (`[alice-web] Click Save ...`). |
| **Sync across apps** | No sleeps ([AD-004](../../.docs/decisions/ad-004-wait-for-state.md)). Bob sees Alice's todo only after Alice's PUT has been observed and Bob has synced and waited for the row. |

### 4.2 Constraints on Html work now

These cost little now and a lot to retrofit, so steps 1-7 keep to them:

1. **No process-wide state in Html, Playwright or Blazor.** Each `PlaywrightTestContext` is
   independent. Several can share one browser, each with its own `IBrowserContext`, so cookies and
   storage stay apart. There are no statics for "the current page" or "the current context". A
   grep today finds none in `srcnew/Brinell.Html*` or `srcnew/Brinell.Blazor`, so keep it that
   way.
2. **A context has a name.** `HtmlTestContextOptions.Name`, used in `ControlCall`'s log line and
   in exception messages. MAUI has no such name yet; add it on the MAUI side when section 4
   starts.
3. **Hosts start on a free port with explicit launch settings.** Nothing reads a fixed URL or
   process-global environment variables at test time. This is also the fix for today's
   `BlazorSampleTestBase` (plan step 5).
4. **Brinell.Uat's static `_currentFile`** (`UatScenarioTestBase`) is noted as a blocker for
   section 4. It is not changed in this plan.

MAUI and Html can run in one test process: FlaUI drives the Windows app without physical input
([AD-005](../../.docs/decisions/ad-005-physical-input-is-opt-in.md)), and Playwright drives the
browser over its own protocol. Two MAUI windows on one desktop should also work for the same reason,
but that needs to be proved.

## 5. Open questions

| # | Question | Default |
| --- | --- | --- |
| ?T1 | Build the web app before step 2 (so the baseline and the R0 proof can use it) or after step 7 | **App before step 2 (T1). Tests after step 7 (T2).** The app does not depend on the framework. Its page objects do, so they are written once, on the finished model |
| ?T2 | Do the Todo `.uat.md` files run unchanged on both heads | Yes, if the test ids match and the page object names match. Head-specific steps (a Picker's native dialog, a browser back button) go in a head-tagged scenario |
| ?T3 | Multi-user identity in `Todo.Api`: per-user key or a user header | Decide when section 4 starts. `X-Todo-Client` (who sent it) is added in T1, because it costs one header and the two-client backend checks need it |
| ?T4 | Where the web app lives | `samples/Todo/src/…Web` in the Todo solution, next to the MAUI app, so both heads share the one set of journeys and test data |
