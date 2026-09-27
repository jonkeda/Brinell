# Plan: scenarios below the UI, then Brinell.Bdd

Status: **plan**, not started. Date: 2026-09-27.
Design and decisions D1-D6 (D6, the Construction pilot, is out of this plan): [proposal.md](proposal.md). This file is the work, phase by phase.

Each phase ends green and is its own commit, named `Bdd phase N: ...` like the phrases plan.

**Order.** The `service` head is proven on Todo first (1-3), then the words change (4), so any
later suite (Construction's, D6) is written once in the final words and file names. The extraction
of `Brinell.Bdd` (6) is the large, internal step. The Construction pilot is not part of this plan.

| Phase | What | Size | Needs |
| --- | --- | --- | --- |
| 1 | The `service` head in the engine | S | - |
| 2 | The Todo service suite | M | 1 |
| 3 | Journey coverage counts scenarios, on every tier they run (D4) | S | 2 |
| 4 | The words: `[BddGiven]` and step definitions (D3); `*.feature.md` and `# Feature:` (D5) | M | 2 |
| 5 | One shared features folder for the Todo suites | S | 4 |
| 6 | Extract `Brinell.Bdd` | L | 1-5 green |
| 7 | Hooks as attributes, set comparison | M | 6 |
| 8 | Run the service suite in parallel | S | 2 |
| 9 | Close: ADs, docs, skills, delete this topic | S | all |

**Progress and deviations**

- Phase 1 done. The binder also leaves a scenario for another head unbound (it is skipped when it
  runs), so a shared file with UI scenarios binds on `service`; UATB013 fires by the matched
  pattern, not by leaving page steps out of the catalog.
- Phase 2 done. The both-tiers scenario is a new **TOD.09.3 Stored todos survive a restart**
  (`@service @maui`), not TOD.07.2: every MAUI page phrase needs `I am on ...` first, which the
  service head refuses, so a both-tiers scenario uses only steps both heads declare as step classes.
  The DataValidator finds a service suite's fixture through a `Steps` assembly row.
- Phase 3 done. The TOD.07.1 scenario also carries `@TOD.11.9` (users are fetched first), since the
  deleted C# twin covered both.

---

## Phase 1: the `service` head in the engine

Everything in `srcnew/Brinell.Uat`; no move, no rename.

1. **Opt-in heads.** `UatTagConventions`
   ([UatConfig.cs](../../srcnew/Brinell.Uat/UatConfig.cs)) gains `Service = "service"`, added to
   `Heads`, and an `OptInHeads` set `{ mixed, service }`. `UatConfig.EvaluateSkip` generalises the
   `mixed` special case: on a suite whose head is opt-in, a scenario without that head's tag is
   skipped ("this suite runs only @service scenarios"). The existing rule for tagged scenarios is
   unchanged, so `@service` alone is skipped on `maui` and `web`, and `@service @maui` runs on both.
2. **No skipped noise.** `UatScenarioSource.GetScenarioTheoryData` leaves out scenarios the suite's
   head would skip for a head reason (not for a skip rule, which must still show as skipped). A
   service suite over the shared folder then lists only its own cases. Check first whether
   `Mixed.Uat` relies on seeing skipped cases; if it does, apply this to `service` only.
3. **No screen on `service`.** When the suite's head is `service`, `UatRuntime.CreateCommandCatalog`
   registers none of the intrinsic page steps, and discovery of `IPageObject` types is not run.
   The binder reports a step that would match a page step, a control verb or a page phrase as
   **UATB013**: "'I tap Sync' is a UI step; the service head has no screen", with the step's line
   (UATB001-012 are taken). It matches the step against the built-in page-step and control-verb
   patterns, so it needs no page model.
4. **Tests** in [Brinell.Uat.Tests](../../testsnew/Brinell.Uat.Tests): `UatHeadsTests` for the skip
   matrix (untagged / `@service` / `@service @maui` / `@mixed` against each suite head),
   `UatScenarioSourceTests` for the theory-data filter, `UatBinderTests` for UATB013 (a page step,
   a control verb, a page phrase; a phrase-class step binds fine).

Verify (Brinell root):

```powershell
dotnet test testsnew\Brinell.Uat.Tests\Brinell.Uat.Tests.csproj -v:minimal /nr:false
```

## Phase 2: the Todo service suite

In `samples/Todo`. The headless host exists; this phase makes it a UAT fixture.

1. **`TodoHost` gets a network.** A temp `NetworkStateFile` per host, passed as
   `LaunchSettings.NetworkStateFile`, exposed as `Network`, disposed with the host. The app already
   reads it (`NetworkStateFileConnectivity`); check that `AddTodoServices` wires it when the setting
   is present.
2. **`TodoServiceFixture`** in
   [IntegrationTests](../../samples/Todo/tests/Brinell.Samples.Todo.IntegrationTests), implementing
   `ITodoArrangement`:
   - `UseTestSet(name)` per file, like the UI fixtures' `BeforeFile`; `StartTest()` starts a fresh
     `TodoHost` per scenario (new database from the test set, backend reset, online). Headless, so
     that is cheap; do it rather than the UI fixtures' "don't reset the database" compromise.
   - `Composition`: `TestComposition.ForFixture` over the `Backend`, `Device`, `StoredData`, `Users`
     step classes from TestSupport and the service step classes below.
   - `TodoHost` stays `internal`; the fixture is in the same project.
3. **Service step classes** (`Steps/`), each with a namespace and a doc line, using the words the
   UI heads use where the same thing happens:

   | Namespace | Steps | Backed by |
   | --- | --- | --- |
   | `Todos` | I create a todo {title}; I set the status of {title} to {status}; I delete the todo {title}; the todo {title} should be waiting to sync / should be synced | `TodoService`, the repository |
   | `Sync` | I sync; the sync should succeed with {count} pushed; the sync should fail with {message} | `ISyncService` |
   | `AppLifecycle` | I restart the app | `TodoHost.Restart()`; same text as the UI heads' `AppLifecyclePhrases` |

4. **Requests step, every head.** `BackendPhrases` gains
   `the server should have received, in order` with a `Method | Resource` table over
   `MockApiServer.Requests()`. It is head-neutral, so it lands in TestSupport and the UI heads get it
   too.
5. **Suite.** A config with `Head | service`, `Fixture | TodoServiceFixture`, no `AppPath`, no
   Pages/Controls assemblies, `## Using` as the MAUI suite plus `Todos` and `Sync`.
   `TodoServiceScenarioTests : UatScenarioTestBase<TodoServiceFixture>` with
   `[Trait("Pyramid", "Integration")]` and `[Trait("Category", "BDD")]`. It links
   `Brinell.Samples.Todo.Uat/Scenarios/**` the way `Web.Uat` does, until phase 5.
6. **First features.**
   - TOD.07.1 (sync order) as `@service`. Its C# twin
     `SyncTests.Sync_FetchesUsers_ThenPushes_ThenPulls` goes in phase 3, once the scenario counts.
   - TOD.09.3 (restart keeps the data) as `@service`.
   - TOD.07.2 as `@service @maui` to prove D4: change its `I tap Sync` to `I sync`, with a MAUI page
     phrase on the Todo List page that taps Sync, and check every step binds on both heads.
7. **Expected failure.** `ExpectedFailures/TOD.07-ui-step-on-service.uat.md` must fail binding with
   UATB013, run by the service suite.

Verify (Brinell root):

```powershell
dotnet test samples\Todo\tests\Brinell.Samples.Todo.IntegrationTests\Brinell.Samples.Todo.IntegrationTests.csproj -v:minimal /nr:false
dotnet test samples\Todo\tests\Brinell.Samples.Todo.Uat\Brinell.Samples.Todo.Uat.csproj --filter "FullyQualifiedName~TodoUatScenarioTests" -v:minimal /nr:false
```

The second run is the MAUI UI suite (one UI process, nothing building meanwhile): TOD.07.2's
changed step and the new requests step must pass there too. Rebuild the sample app first.

## Phase 3: coverage counts scenarios, on every tier (D4)

Today the coverage report reads only xUnit traits; UAT scenarios are not counted at all.

1. [JourneyCoverage](../../samples/Todo/tests/Brinell.Samples.Todo.JourneyCoverage) references
   `Brinell.Uat` (net10.0, no UI) and parses every scenario file with `UatMarkdownParser`. No
   binding, no fixture.
2. A scenario covers the subtasks named by its id tags (`@TOD.07.2`). Its tiers come from its head
   tags: `service` → Integration; `maui`, `web`, `mixed` or none → UiHermetic; `smoke` and `review`
   → the gates. A scenario with `@service @maui` counts for **both** Integration and UiHermetic.
   Only heads that have a suite in the sample count.
3. `CoverageReport` lists scenarios beside test methods (`TOD.07-sync-with-the-backend: TOD.07.2`).
4. Tests: a `@service @maui` scenario covers both tiers; an untagged one covers UiHermetic only;
   `EveryTest_SaysWhichTierItIsIn` extends to "every scenario has an id tag".
5. Delete `SyncTests.Sync_FetchesUsers_ThenPushes_ThenPulls` (now covered by the feature).

Verify: `dotnet test samples\Todo\tests\Brinell.Samples.Todo.JourneyCoverage\Brinell.Samples.Todo.JourneyCoverage.csproj`.

## Phase 4: the words (D3, D5)

What a step-definition author or a tester writes changes now, in one go, with no aliases. Engine
type names (`UatScenarioTestBase`, `UatTable`, `UatPhrase*`, `UatCallable*`) wait for phase 6,
which renames them as it moves them.

### 4a. Attributes and step definitions (D3)

1. The attributes stay in `Brinell.Core.Testing` (proposal 3.3, option A), renamed:
   `UatGiven/UatWhen/UatThen` → `BddGiven/BddWhen/BddThen`, `UatPhraseAttribute` →
   `BddStepAttribute`, `UatPhraseClass` → `BddStepDefinitions`, `UatNamespace` → `BddNamespace`,
   `UatPhraseClassBase` → `BddStepDefinitionsBase`. `[UatNavigatesTo]` stays `Uat` (it is about
   pages). AD-011's rule "no other way to declare a step" holds for the new names.
2. Where they are written: the six control interfaces in `Brinell.Core/Interfaces`,
   `ContentDialog.tpl.cs` and `StateContainer.tpl.cs` (regenerate their `.gen.cs` with the
   generator), 19 files in `samples/` and `testsnew/`, the Presenter, `Brinell.DataValidator`'s
   `snippet` output, generator templates if they emit attributes, and diagnostic texts that name an
   attribute. Search for the string names as well as the types.
3. Docs and skills in the same commit: [uat-phrases-and-flows.md](../../docs/guides/uat-phrases-and-flows.md)
   (and its name), [uat-template-guide.md](../../docs/guides/uat-template-guide.md),
   [uat-grammar.md](../../docs/specs/uat-grammar.md), the glossary from proposal 4.2 as a `docs/`
   page, and `.github/skills/*` that show attributes.

### 4b. Files and headers (D5)

`*.uat.md` → `*.feature.md`, `# UAT:` → `# Feature:`, `*.phrases.md` → `*.steps.md`,
`## Phrase:` → `## Step:`, `## Phrase for each row:` → `## Step for each row:`. One commit, no
period where both are accepted.

1. The parser accepts only the new forms. A file with an old header or extension gets a diagnostic
   whose fix `brinell-data validate --fix` applies (rename the file, rewrite the headers).
2. Run `--fix` over `samples/` and `testsnew/`. Update the globs in every `.csproj`
   (`Scenarios\**\*.uat.md`), `UatScenarioSource`, `UatScenarioFiles`, `UatMarkdownParser`, the
   Presenter's `UatWorkspaceService` and shell, and `Brinell.DataValidator` (`ScenarioCheck`,
   `ReferenceCheck`, `DataValidatorApp`).
3. UI suites keep `uat.config.md`; a service suite's config is `bdd.config.md`. The config parser
   takes either name: the name says which kind of suite it is, it is not a second spelling.
4. Construction's `_specs/**/*.uat.scenarios.md` are specs, not Brinell features: leave them.
5. The VS Code and Visual Studio proposals in [uat/tools/](../uat/tools/) use the new names.

Verify: full build, `Brinell.Core.Tests`, `Brinell.Maui.Tests`, `Brinell.Uat.Tests`,
`Brinell.Generator.Tests`, the Todo service suite, `brinell-data validate` over `samples/`
(0 errors), and the MAUI UAT suite once (every scenario file was renamed).

## Phase 5: one shared features folder

1. Move `Brinell.Samples.Todo.Uat/Scenarios/` to `samples/Todo/tests/Features/` (a folder, not a
   project). `ExpectedFailures/` stay with the suite that owns them.
2. `Uat`, `Web.Uat`, `Mixed.Uat` (its `@mixed` files move too) and IntegrationTests link
   `..\Features\**` into their output `Scenarios/` folder, so `UatScenarioSource` and the configs are
   unchanged.
3. The Presenter's Todo workspace (AD-018), `brinell-data validate` and the JourneyCoverage parse
   (phase 3) point at the new folder.
4. Update [samples/Todo/README.md](../../samples/Todo/README.md) and
   [uat-walkthrough.md](../../samples/Todo/uat-walkthrough.md).

Verify: the four suites list the same cases as before the move (compare `--list-tests`); the MAUI
UAT suite once.

## Phase 6: extract `Brinell.Bdd`

A refactor: verified by **the same tests passing identically**, counted before and after.

1. **The seam first, in place.** In `UatBinder`, move page tracking (`_pages`, `PageState`,
   `ResolvePage`, `Fits`, `PageOf`, `PageProblem`) behind an internal `IStepScopeProvider`
   (visible steps at a point in the walk) and `IStepWalkObserver` (the walk tells it what each bound
   step did). Folder, file, `Using` and global scopes stay in the binder. Commit with the tests unchanged.
2. **Split discovery.** `UatReflectionRuntime` (1,308 lines) into step-definition discovery and
   invocation (step classes, argument binding, callables) and page/control discovery
   (`IPageObject`, `IControlObject`, name inference, control verbs). Same commit rule.
3. **Move.** New `srcnew/Brinell.Bdd` (net8/9/10 like `Brinell.Uat`, references `Brinell.Core` and
   MS DI). Moves: parser and models, values, argument types, tables and table binding, catalog and
   binder core, callables, namespaces, config document and writer, feature files and source,
   execution and runner, diagnostics formatter, test settings extensions, and a
   `BddFeatureTestBase<TFixture>` (today's `UatScenarioTestBase` without the screenshot). Moved
   types get `Bdd` names as they move (`BddTable`, `BddStep*`, `BddCallableStep*`).
   `Brinell.Uat` keeps page scope, page/control discovery, control assertions, name inference,
   target conventions, sessions and apps (`IUatSessionHost`, `IUatAppsHost`), UI heads, and
   `UatScenarioTestBase<T> : BddFeatureTestBase<T>` adding screenshots.
4. **Subjects.** AD-015's `{user}` without apps (actors) moves to Bdd only if phase 2 needed it;
   otherwise it stays in Uat, noted for later.
5. **Tests.** New `testsnew/Brinell.Bdd.Tests`; move `UatMarkdownParserTests`, `UatValuesTests`,
   `UatArgumentTests`, `UatTableBindingTests`, `UatCallableTests`, `UatNamespaceTests`,
   `UatUsingTests`, `UatConfigParserTests`, `UatConfigWriterTests`, `UatExecutionTests`,
   `UatScenarioSourceTests` and the non-page part of `UatBinderTests`.
6. **Consumers.**
   - `Brinell.Samples.Todo.TestSupport` and the Todo service suite reference `Brinell.Bdd` only.
   - Presenter, `Brinell.DataValidator`, the `*.Uat.Tests` projects: references and usings.
7. **Package.** `Brinell.Bdd` gets `GeneratePackageOnBuild` like `Brinell.Uat`; `Brinell.Uat`'s
   package depends on it.

Verify (Brinell root), before and after, comparing the counts:

```powershell
dotnet build srcnew\Brinell.sln -v:minimal /nr:false
dotnet test testsnew\Brinell.Uat.Tests\Brinell.Uat.Tests.csproj -v:minimal /nr:false
dotnet test testsnew\Brinell.Bdd.Tests\Brinell.Bdd.Tests.csproj -v:minimal /nr:false
dotnet test testsnew\Brinell.Maui.Uat.Tests\Brinell.Maui.Uat.Tests.csproj -v:minimal /nr:false
```

Plus the Todo service suite and the MAUI Todo UAT suite once (the fixture base moved).

## Phase 7: hooks and set comparison

From the [Reqnroll comparison](../uat/reqnroll-comparison.md), R1 and R4, in `Brinell.Bdd`:

1. `[BddBeforeFeature]`, `[BddBeforeScenario("tag")]`, `[BddAfterScenario]`, `[BddAfterStep]` on
   step-definition classes, run in a stated order (framework hooks, then by class name), with tag
   filters. The test base's `BeforeFile`/`BeforeScenario` overrides stay for fixtures.
2. `BddTable.AssertSet(expected, actual)` with one diff (missing, extra, different cells), used by
   `StoredDataPhrases.DatabaseHasOnly` and the server steps.

## Phase 8: parallel service suite

One host per scenario already (phase 2). Remove the collection serialisation for the service
suite only, check each host has its own ports and files, and measure the run time before and after.
The UI suites stay one process at a time.

## Phase 9: close

1. **ADs** in `.docs/decisions/`:
   - AD-020 *A tier is a head*: `service`, opt-in heads (`mixed`, `service`), UATB013, a scenario on
     several heads counts for each tier. Amend AD-014 to point to it.
   - AD-021 *Bdd under Uat*: what belongs to each assembly; UI knowledge never enters `Brinell.Bdd`.
   - AD-011 rewritten for the `Bdd` names; AD-017 and AD-019 for `*.steps.md` and `## Step:` (a
     decision is recorded once).
   - [invariants.md](../../.docs/invariants.md): `Brinell.Bdd` references no page or control type.
2. **Docs**: a guide "Scenarios below the UI" (when a scenario goes to `service`, how to write
   service step definitions, the both-tiers rule); [docs/README.md](../../docs/README.md);
   [journeys.md](../../samples/Todo/journeys.md)'s tier legend says a scenario can carry I.
3. **[uat/complex/README.md](../uat/complex/README.md)**: "an integration test wearing a UAT
   costume" becomes "tag it `@service`".
4. **[subjects-plan.md](../uat/phrases/subjects-plan.md)**: its promoted interfaces land in
   `Brinell.Bdd`; `TodoServiceFixture` implements them.
5. Delete `.my/unit/` and its README row.

---

## Risks

| Risk | Mitigation |
| --- | --- |
| The binder seam (phase 6.1) is tangled: page tracking also drives callables' page checks (`PageProblem`). | Do 6.1 alone and commit it; if the seam needs more than two interfaces, stop and write down why before moving files. |
| Service and UI step texts drift ("I sync" on one, "I synchronise" on the other). | A `@service @maui` scenario binds on both suites, so drift is a bind error in one of them. Keep at least one such scenario per journey that has both tiers. |
| The service suites become the place for plumbing tests. | The guide's rule (proposal 2.6): Gherkin when someone other than the developer reads it; contract shapes, tombstones and retry timing stay C#. |
| Phase 4 touches generated controls. | Regenerate `.gen.cs` with the generator, never by hand; run `Brinell.Generator.Tests`. |
