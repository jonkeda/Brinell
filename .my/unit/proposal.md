# Scenarios below the UI: unit and integration tiers, Brinell.Bdd, Cucumber words

Status: **accepted with changes**, 2026-09-27; the work is in [plan.md](plan.md). Decisions:

| # | Decision |
| --- | --- |
| D1 | "Device check" means device **state** (network, clock, restart, stored data), not a physical device. |
| D2 | `service` is a **head**, using AD-014's mechanism; it runs `@service` scenarios only. |
| D3 | Attributes keep a prefix: **`[BddGiven]` / `[BddWhen]` / `[BddThen]`** (not `[Given]`), so a project can reference Reqnroll too. Section 4.2's other words stand. |
| D4 | A scenario tagged for a UI head and `@service` counts for **both tiers** in the coverage report. |
| D5 | **Rename** the files: `*.feature.md`, `# Feature:`, `*.steps.md`, `## Step:`; one step, no alias period. |
| D6 | **Construction wants a `service` head soon**, over its headless integration host. The plan puts its pilot before the `Brinell.Bdd` extraction. |

Nothing is open.
Area: `srcnew/Brinell.Uat`, `samples/Todo/tests`, `.docs/decisions` (AD-011, AD-014, AD-017, AD-019)

The questions:

1. How can we best support unit and integration tests with the Gherkin from UAT: services,
   database, device checks?
2. Should the Gherkin engine move to a `Brinell.Bdd` project, with UAT as BDD on top of UI tests?
3. Should we use the words Cucumber users know: feature, scenario, step, step definition, hooks?
4. What project structure fits, or can we keep the current one?

Short answers, argued below:

| # | Answer |
| --- | --- |
| 1 | **Integration: yes**, as a new *head* called `service` that runs the same scenario files without a UI (the dual-driver pattern). Most of it exists: the backend, database and device phrases already sit behind `ITodoArrangement`, and `TodoHost` can be one. **Unit: no** Gherkin runner; keep xUnit, and use Gherkin only for decision tables a business owner reads. |
| 2 | **Yes, split**: `Brinell.Bdd` (parse, bind, values, tables, callables, namespaces, run, report) and `Brinell.Uat` on top (pages, controls, page scope, apps, screenshots). About three quarters of `Brinell.Uat` has no UI in it. Do it before Construction writes its first scenario. |
| 3 | **Yes where the idea is the same one**: feature, scenario, step, step definition, background, examples, tags, hooks. **Keep Brinell's own words** where Cucumber has no equivalent: callable phrase, control verb, page phrase, head, subject, journey. |
| 4 | **Keep it, with three changes**: the new `Brinell.Bdd` (+ tests), the Todo scenario files in one neutral folder that every suite links, and a service suite in the existing `Brinell.Samples.Todo.IntegrationTests`. No new sample project. |

---

## 1. What holds today

| Fact | Where | Consequence |
| --- | --- | --- |
| `Brinell.Uat` references only `Brinell.Core` (platform-neutral, AD-001) and MS DI. No MAUI, no FlaUI, no xUnit. | [Brinell.Uat.csproj](../../srcnew/Brinell.Uat/Brinell.Uat.csproj) | The engine can already run in a plain `net10.0` test project. |
| The UI coupling is concentrated: `UatReflectionRuntime` (1,308 lines, most of it page and control reflection), `UatBinder`'s page tracking, `UatPageScope`, `UatDiscovery`'s `IPageObject`/`IControlObject` checks, `UatControlAssertions`, `UatNameInference`, `UatTargetConventions`, the screenshot on failure. The parser (829), values (626), tables (421), callables (600), namespaces, config and results have none. | [srcnew/Brinell.Uat](../../srcnew/Brinell.Uat) | A split is a real refactor of two files (`UatBinder`, `UatReflectionRuntime`) and a move of the rest. |
| Steps that reach past the screen are written **once, head-neutral**: `BackendPhrases`, `StoredDataPhrases`, `DevicePhrases`, `UserPhrases`, each taking an `ITodoArrangement` (test set, fake server, WireMock, network file, database path, clock). The MAUI and web fixtures both implement it. | [TestSupport/Uat](../../samples/Todo/tests/Brinell.Samples.Todo.TestSupport/Uat/ITodoArrangement.cs) | Anything that implements `ITodoArrangement` gets those steps for free. |
| `TodoHost` is the app without its UI: the real `AddTodoServices` composition, a temp SQLite file, a WireMock backend, a pinned clock, `Restart()`. It has everything `ITodoArrangement` asks for except a `NetworkStateFile`, and the app already reads one through `LaunchSettings.NetworkStateFile`. | [TodoHost.cs](../../samples/Todo/tests/Brinell.Samples.Todo.IntegrationTests/TodoHost.cs) | A headless fixture is a small adapter, not a new host. |
| One scenario file serves every head; head tags pick where it runs; a `mixed` suite runs only `@mixed` scenarios (AD-014). | [AD-014](../../.docs/decisions/ad-014-one-scenario-file-serves-every-head.md) | "Which tier runs this scenario" can reuse the head mechanism unchanged. |
| Each journey subtask gets the **lowest tier that gives confidence**; a second tier checks one example only. The coverage check reads `Pyramid` and `Journey` traits. | [journeys.md](../../samples/Todo/journeys.md), [JourneyCoverage](../../samples/Todo/tests/Brinell.Samples.Todo.JourneyCoverage/TestCatalog.cs) | Scenarios below the UI must not become a second copy of every UI scenario; they must carry `Pyramid = Integration` to count. |
| The complex-checks guidance says: if the assertion never looks at a control, it is an integration test wearing a UAT costume. | [uat/complex/README.md](../uat/complex/README.md) | Today that rule pushes such checks *out of Gherkin* into C#. This proposal keeps them in Gherkin and moves them *down a tier* instead. |

---

## 2. Question 1: Gherkin at the unit and integration tiers

### 2.1 The idea: a tier is a head

A scenario says what the system does, in domain words. A **head** is the driver that makes those
words happen. Today there are two UI drivers (`maui`, `web`). Add a third that has no UI:

| Head | Drives | Fixture | Runs scenarios tagged | Pyramid trait |
| --- | --- | --- | --- | --- |
| `maui` | the MAUI app, through page objects | `TodoUatFixture` | `@maui` or no head tag | `UiHermetic` |
| `web` | the Blazor app, through page objects | `TodoWebUatFixture` | `@web` or no head tag | `UiHermetic` |
| `mixed` | apps of several heads | the mixed fixture | `@mixed` only | `UiHermetic` |
| **`service`** (new) | the app's services and ViewModels, no UI | a `TodoServiceFixture` over `TodoHost` | **`@service` only** | `Integration` |

This is the *dual driver* pattern from acceptance-test practice (Farley's four layers: test case,
domain language, protocol driver, system). Brinell already has layers one and two; heads are
layer three.

`service` runs **only** scenarios tagged `@service`, the way `mixed` runs only `@mixed`. Untagged
scenarios are UI scenarios, as today; nothing existing changes meaning.

### 2.2 What a service scenario looks like

The journey TOD.07.1 (sync order) is an integration test in C# today
([SyncTests.cs](../../samples/Todo/tests/Brinell.Samples.Todo.IntegrationTests/SyncTests.cs)). As a
service scenario:

```gherkin
@TOD.07.1 @service
## Scenario: TOD.07.1 A sync fetches users, pushes local changes, then pulls

Given the test data is "pending-local-change"
When I sync
Then the sync should succeed with 2 pushed
And the server should have received, in order
  | Method | Resource |
  | ------ | -------- |
  | GET    | users    |
  | PUT    | todos    |
  | PUT    | todos    |
  | GET    | todos    |
And the database should have todos
  | Title       | SyncState |
  | ----------- | --------- |
  | Order paint | Synced    |
```

- `the database should have todos` and the server steps are the **existing** `StoredDataPhrases`
  and `BackendPhrases`, unchanged.
- `I sync` and `the sync should succeed with {count} pushed` are new step definitions in a
  `TodoServiceSteps` class that calls `ISyncService`. On the MAUI head, `I sync` can be a page
  phrase on the Todo List page that taps Sync; then the **same** scenario can also run with
  `@service @maui`, which is the pyramid's "second tier checks one example".
- `I am on the Todo List page`, control verbs and page phrases do not exist on `service`. A
  scenario tagged `@service` that uses one is a **bind error** that names the step and the head,
  before anything runs (a new code next to UATB010).

### 2.3 Services, database and device checks

| Check | UI head today | Service head |
| --- | --- | --- |
| Backend state and faults | `BackendPhrases` over `ITodoArrangement.Server/Backend` | the same class; `TodoHost.Mock`/`Server` |
| Requests the app sent | ad-hoc C# | new `the server should have received ...` steps over `MockApiServer.Requests()`; works on every head |
| What was stored | `StoredDataPhrases` over `DatabasePath` | the same class; `TodoHost.Database.Path` |
| Device: network | `DevicePhrases` over `NetworkStateFile` | the same class; `TodoHost` passes `LaunchSettings.NetworkStateFile` |
| Device: clock | `IUatClock` from the test set | the same; `LaunchSettings.FixedNow` |
| Device: restart | `AppLifecyclePhrases` (process restart) | `TodoHost.Restart()` behind the same step text |
| Device: capabilities (camera, location, permissions, a real phone) | Android head (in flight in [android/](../android/plan.md)) | not simulated: a scenario that needs real hardware stays on a device head, with a skip rule when none is present |

The one change the Todo sample needs: `ITodoArrangement` moves from `Brinell.Uat` phrases to
`Brinell.Bdd` step definitions (only its `using` changes), and `TodoHost` gets a `NetworkStateFile`
and implements it.

> I read "device check" as the device's state that a scenario arranges or checks (network, clock,
> restart, stored data). If it meant running scenarios on a physical device, that is a head too
> (`android`), and the same mechanism applies; say so and section 2.3's last row grows.

### 2.4 Should the service head drive ViewModels?

A middle ground between services and UI is driving the **ViewModels**: set properties, run
commands, read state. It catches screen logic (validation messages, enabled Save) in milliseconds.
The unit tests already do this by hand
([ViewModels/](../../samples/Todo/tests/Brinell.Samples.Todo.UnitTests/ViewModels)).

Proposal: allowed inside `service` step definitions (`TodoHost.Get<TodoEditViewModel>()`), **not** a
separate "viewmodel head" with its own page-object-like model. A ViewModel head would need a
second page graph that mirrors the UI one and drifts from it. Revisit if Construction shows two
teams writing the same ViewModel steps.

### 2.5 Unit tests

**No Gherkin runner for unit tests.** A unit test checks one rule with many values; xUnit
`[Theory]` does that with less ceremony, and the rule's author is a developer. Gherkin earns its
cost when a non-developer reads or writes the text.

The one exception worth supporting: a **decision table** a business owner owns (the sort order in
TOD.01.5, the validation matrix in TOD.02.3/02.4). That is a `Scenario Outline` whose Examples
rows feed one step on the `service` head. It runs with the integration suite and costs nothing new.
If it proves itself, a pure-function variant can follow (no host, just the rule class).

### 2.6 What this buys, and what it costs

| For | Against |
| --- | --- |
| Testers and developers read one language at every tier; the journey file links to scenarios, not C# method names. | A second way to write an integration test. The rule has to be clear: Gherkin when someone other than the developer reads it, C# when it is plumbing (contract shape, tombstones, retry timing). |
| Service scenarios run in milliseconds, need no desktop, and can run **in parallel** (one `TodoHost` per scenario); UI scenarios cannot. | Step definitions for `service` must be written by hand; there is no page graph to infer them from. |
| A scenario can move tier by changing a tag, when a check turns out not to need the screen. | Bind-time errors for UI-only steps on `service` are new diagnostics to build and test. |
| The coverage check keeps working: the service suite reports `Pyramid = Integration`. | The service suite must emit one test case per scenario with traits, as the UI suites do. |

---

## 3. Question 2: a `Brinell.Bdd` project

### 3.1 Yes, and now

The engine is general; the UI part is a layer on top. Splitting makes that visible and lets a
`net10.0` integration project reference the engine without the page-graph machinery. It is also
the cheapest now: Construction has not written a scenario yet
([construction/](../construction/plan.md) says "Todos not started"), so the public names can still
change without a migration for anyone outside this repo.

### 3.2 What goes where

| `Brinell.Bdd` (no UI) | `Brinell.Uat` (UI on top of Bdd) |
| --- | --- |
| Markdown parser and grammar (`UatMarkdownParser`, models) | Page discovery, `IPageObject`/`IControlObject` reflection (the UI half of `UatReflectionRuntime`) |
| Values and argument types (AD-016: `UatValues`, `UatArgumentTypes`) | Control verbs, name inference (`AddButton` → **Add**), target conventions |
| Tables, table binding, set comparison (`UatTableBinding`) | Page scope and static page tracking (AD-019 scope 1), `[UatNavigatesTo]`, page phrases |
| Step catalog, binder core, diagnostics | The intrinsic page steps (`I am on`, `I should be on`, `I should see`) |
| Callables (AD-017) and namespaces, folders, `Using` (AD-019 scopes 2-5) | Apps and sessions (`IUatSessionHost`, `IUatAppsHost`), UI heads, `{app} uses the app` |
| Subjects and actors (AD-015's `{user}` without apps: two headless users are two `TodoHost`s) | Control assertions, screenshots on failure, the Presenter link |
| Config document, test settings, test data selection | |
| Runner, results JSON, hooks, the xUnit-agnostic test base | |
| Keyword attributes (`[Given]`, `[When]`, `[Then]`) | |

The binder needs an **extension point** for scopes: Bdd owns file, folder, `Using` and global
scopes; Uat plugs in the page scope and the page tracker. That seam is the design work of the
split. The rest is moving files.

### 3.3 Where the attributes live

Control interfaces in `Brinell.Core` carry `[UatWhen]` today, so the attributes must sit at or
below `Brinell.Core`. Two options:

| Option | For | Against |
| --- | --- | --- |
| **A. Keep them in `Brinell.Core.Testing`** (renamed, section 4) | No new assembly; Core is platform-neutral already. | A service test project pulls `Brinell.Core` for three attributes. Harmless: it is neutral and small. |
| B. A new `Brinell.Bdd.Abstractions` that Core and Bdd reference | The cleanest layering. | One more package to version and ship. |

Recommendation: **A**. Move to B only if someone wants Bdd without Core.

### 3.4 "UAT is BDD on top of UI tests"

Yes, and state it in the docs exactly like that: `Brinell.Uat` = `Brinell.Bdd` + the page graph +
UI heads. The Presenter stays a UAT tool (it needs a running app), and AD-018 is unchanged.

---

## 4. Question 3: Cucumber words

### 4.1 Rule

Use Cucumber's word **when the concept is the same**; keep Brinell's word **when it is not**, and
say in the glossary why. A new user with Cucumber or Reqnroll behind them then only learns what is
actually new.

### 4.2 Glossary

| Brinell today | Cucumber / Reqnroll | Same concept? | Proposal |
| --- | --- | --- | --- |
| scenario file, `# UAT: TOD.07 ...` | feature file, `Feature:` | yes | **Feature**: `# Feature: TOD.07 Sync with the backend`; file `*.feature.md` (section 4.3) |
| Scenario, Scenario Outline, Background, Examples, tags | same | yes | keep |
| step | step | yes | keep |
| phrase (a declared text + its method) | step definition | yes | **step definition** in docs and APIs (`[UatPhraseClass]` → `[BddStepDefinitions]`) |
| phrase text, `{placeholder}` | step text, Cucumber expression | close (no optional text, no alternatives: AD-016) | **step text**; keep `{placeholder}` and say it is a strict subset |
| `[UatGiven]` / `[UatWhen]` / `[UatThen]` | `[Given]` / `[When]` / `[Then]` | yes | **`[BddGiven]`/`[BddWhen]`/`[BddThen]`** (D3) |
| `BeforeFile`, `BeforeScenario` overrides | hooks: `[BeforeFeature]`, `[BeforeScenario]`, `[AfterStep]` | yes | **hooks**, as attributes (`[BddBeforeScenario("tag")]`, `[BddAfterStep]`), with tag filters (Reqnroll comparison R1) |
| fixture | (none; context injection, `ScenarioContext`) | no | keep **fixture** (it is xUnit's word, which Brinell users know) |
| callable phrase (`## Phrase:`) | (none; steps calling steps is discouraged) | **no**: a Markdown sequence with parameters, AD-017 | keep, but name it **callable step** so it reads as "a step you define in Markdown" |
| control verb | (none) | no | keep |
| page phrase | (none; closest is a page-object step definition) | no | keep |
| head | (none) | no | keep; the glossary says "the driver a feature runs through" |
| subject (`Daan taps Save`) | (none; Screenplay calls it the **actor**) | close to Screenplay | keep **subject** in the grammar; mention actor |
| journey, `Journey` field | (tags by convention) | no | keep |
| `Using`, namespace | (none; `[Scope]` is tag-based, R10 said no) | no | keep |

### 4.3 The file name and header

`.uat.md` says "acceptance test through the UI". Once a feature can run on `service`, that is
wrong for part of the files. Proposal: `*.feature.md` and `# Feature:`, in one rename with a
`brinell-data validate --fix` rewrite, **no** alias period. One right spelling is the house rule
(AD-016); two accepted headers would be the first exception. `*.phrases.md` becomes
`*.steps.md` for the same reason.

The xUnit suites keep their names (`...Uat`) where they only run UI heads.

### 4.4 What we do not take

Cucumber expressions' optional text and alternatives (R9), tag-scoped step definitions (R10), and
Gherkin's keyword translations until Construction's testers ask (R3). The
[Reqnroll comparison](../uat/reqnroll-comparison.md) section 3 stands; this proposal adopts its
**words**, not its matching rules.

---

## 5. Question 4: project structure

### 5.1 Framework (`srcnew/`, `testsnew/`)

```text
srcnew/
  Brinell.Core/              keyword attributes (Testing/), neutral as today
  Brinell.Bdd/               NEW: parser, values, tables, binder core, callables, namespaces,
                             hooks, runner, results, test base
  Brinell.Uat/               page graph, control verbs, page scope, apps and sessions, UI heads;
                             references Brinell.Bdd
  Brinell.Presenter/         unchanged (references Brinell.Uat)
testsnew/
  Brinell.Bdd.Tests/         NEW: moved from Brinell.Uat.Tests - parser, values, tables,
                             callables, namespaces, using, config, execution
  Brinell.Uat.Tests/         what is left: discovery, page scope, heads, sessions, subjects
                             with apps, name inference, target conventions, test base
  Brinell.*.Uat.Tests        unchanged
```

`UatMarkdownParserTests`, `UatValuesTests`, `UatTableBindingTests`, `UatCallableTests`,
`UatNamespaceTests`, `UatUsingTests` and most of `UatBinderTests` move; the move is verified by
the same tests passing identically.

### 5.2 Todo sample (`samples/Todo/tests/`)

```text
Brinell.Samples.Todo.TestSupport/
  Steps/                     was Uat/: Backend, StoredData, Device, User steps and
                             ITodoArrangement; references Brinell.Bdd, not Brinell.Uat
Brinell.Samples.Todo.Features/   NEW folder, not a project: every *.feature.md and *.steps.md,
                                 linked by each suite (today Web.Uat links from Uat)
Brinell.Samples.Todo.IntegrationTests/
  TodoHost.cs                gains NetworkStateFile; a TodoServiceFixture implements
                             ITodoArrangement over it
  Steps/TodoServiceSteps.cs  NEW: I sync, I create a todo, the sync should succeed ...
  Features/TodoServiceFeatureTests.cs   NEW: runs the @service features, Pyramid=Integration
  bdd.config.md              NEW: Head | service
Brinell.Samples.Todo.Uat/, .Web.Uat/, .Mixed.Uat/   link Features/ instead of owning it
```

The only new *project* is `Brinell.Bdd` (+ its tests). The sample gets a folder and three files.
The UnitTests project is untouched (section 2.5).

### 5.3 Could we keep the current structure unchanged?

Yes, technically: a service fixture can reference `Brinell.Uat` today and run `@service`
scenarios, because the engine has no UI dependency. What that loses: the page-graph code runs
(and can fail discovery) in a suite that has no pages; the words stay "UAT" for tests that are not
acceptance-through-UI; and the Bdd/Uat seam in the binder is never made explicit, so the next
feature that needs it (Construction's headless module tests) repeats this discussion. Worth doing
as a **spike** first (phase 1 below), not as the end state.

---

## 6. Order of work

Each phase ends green and is its own commit. Phases 1-2 prove the idea before anything moves.

| Phase | What | Proves | Size |
| --- | --- | --- | --- |
| 1 | **Spike, no moves**: `service` head in `UatConfig`; `@service`-only rule; `TodoServiceFixture` over `TodoHost` implementing `ITodoArrangement`; TOD.07.1 and one TOD.09 restart feature as `@service`; `Pyramid=Integration` traits so the coverage check counts them | the dual driver works with today's engine | S |
| 2 | Bind error for UI-only steps on `service`; `the server should have received` steps over `MockApiServer.Requests()` (every head) | the head boundary is checked, not hoped for | S |
| 3 | Glossary to `docs/`; decide section 7; ADs drafted (tier-as-head, Bdd/Uat split, words) | the decisions are recorded once | S |
| 4 | Extract `Brinell.Bdd`: binder scope extension point, then move files; `Brinell.Bdd.Tests` | same tests pass identically | L |
| 5 | Rename: `[Given]`/`[When]`/`[Then]`, step definitions, `*.feature.md`, `# Feature:`, `*.steps.md`; `--fix` rewrite; Presenter and grammar spec updated | one spelling everywhere | M |
| 6 | Hooks as attributes with tag filters (Reqnroll R1), set comparison (R4) in Bdd | parity for service suites | M |
| 7 | Parallel service suite (one `TodoHost` per scenario) | the speed argument holds | S |
| later | decision-table variant without a host; a ViewModel head if two teams need it; Cucumber Messages output (R5) | | |

---

## 7. Open questions

1. ~~**Tier as head**~~ (D2). Agree that `service` is a head (AD-014's mechanism) rather than a new
   concept? The alternative is a separate `Tier` metadata field, which duplicates head tags.
2. ~~**Untagged means UI**~~ (D2): agree that `service` runs `@service` only, so no existing file changes
   meaning?
3. ~~**The rename**~~ (D5, yes) (section 4.3): `*.feature.md` / `# Feature:` in one step with `--fix`, or keep
   `.uat.md` for good? It touches every scenario file, the grammar spec, the Presenter, and the
   VS Code / Visual Studio proposals.
4. ~~**Attributes**~~: decided, D3 (`[BddGiven]`).
5. ~~**"Device check"**~~ (D1): device *state* (section 2.3), or running on a physical device? The second
   is the Android head's topic.
6. ~~**Construction**~~ (D6, soon): does its first headless module test want this (a `service` head over the
   DryIoc composition, SQLite and WireMock, like its IntegrationTests project), and when?
7. ~~**The pyramid rule**~~ (D4, both tiers): journeys.md says one tier per subtask plus one example at a second. Should
   a feature tagged `@service @maui` count for both tiers in the coverage report, or only the lower?
