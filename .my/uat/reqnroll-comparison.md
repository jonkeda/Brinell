# Reqnroll (SpecFlow) and Bruinell.Uat compared

Status: **analysis**, 2026-09-27.

Reqnroll is the open-source continuation of SpecFlow, the .NET implementation of Cucumber. The
Reqnroll side of this doc comes from its documentation as known in mid-2026, not from a trial run.
Check a row against the current docs before acting on it. The Bruinell side comes from the code
and `.docs/`. Neither repo uses Reqnroll or SpecFlow today.

## 1. The two in one paragraph each

**Reqnroll** runs standard Gherkin `.feature` files:

- each step line matches a C# **step definition**, found by Cucumber expression or regex;
- the definitions are plain glue code that the team writes for every phrase;
- test classes are generated for MSTest, NUnit, xUnit or TUnit;
- hooks, dependency injection and table helpers make up the rest.

It's general-purpose. It knows nothing about the application or the UI.

**Bruinell.Uat** runs Markdown scenario files (`*.uat.md`):

- it binds each step against a **catalog built from the page objects and controls**: every page
  and control has a name, and control verbs come with Bruinell;
- only phrases the page graph can't express need C# (phrase classes), or a tester writes them as
  callable phrases in Markdown;
- values are typed and checked;
- the whole document binds before anything runs;
- it runs on xUnit through `UatScenarioTestBase`, or step by step in the Presenter.

It's made for UI acceptance tests of apps with Bruinell page objects.

## 2. Side by side

| Area | Reqnroll | Bruinell.Uat |
| --- | --- | --- |
| File format | Gherkin `.feature` | Markdown `.uat.md`, with a strict grammar ([uat-grammar.md](../../docs/specs/uat-grammar.md)); prose, links and a metadata table allowed |
| Structure | Feature, Rule, Background, Scenario, Scenario Outline, several Examples blocks (each with tags) | `# UAT:`, Metadata, Background, Scenario, Scenario Outline with **one** `### Examples`, `## Data:`, `## Phrase:` |
| Keywords in other languages | yes, `# language: nl` (Gegeven / Als / Dan), 70+ languages | English keywords only; phrase text can be any language |
| Where phrases come from | hand-written step definitions | control verbs and page flow **built in**; page and control names from the page objects; phrase classes; callable phrases |
| Phrase matching | Cucumber expressions (`{int}`, `{string}`, optional text, alternatives) or regex | literal text with `{placeholder}`s only; no optional words, alternatives or regex (AD-016) |
| Argument types | parameter types + `[StepArgumentTransformation]`; parsing follows the binding culture | typed by the method's parameter, culture-invariant, one right way to write each value (AD-016); relative dates against the fixture's clock; app types through `[BddArgumentType]` |
| Tables | `Reqnroll.Assist`: `CreateInstance`, `CreateSet`, `CompareToInstance`, `CompareToSet` with a diff | a table binds to a record or a list of records by the method's parameter; comparing is the phrase's job |
| Composition | a step can call other steps in C# (discouraged) | callable phrases: named, parameterised step sequences in Markdown, no control flow (AD-017) |
| Scoping | `[Scope(Tag, Feature, Scenario)]` on bindings | the page scopes control verbs; scopes and namespaces are built ([AD-019](../../.docs/decisions/ad-019-phrases-have-scopes-and-namespaces.md)) |
| When problems show | undefined or ambiguous steps show when the test runs (the IDE extension warns earlier) | **bind time**: the whole file binds before the first step. `UAT`/`UATB`/`UATA`/`UATC` codes. `bruinell-data validate` checks every file in CI without starting the app |
| Hooks | Before/After TestRun, Feature, Scenario, ScenarioBlock, Step; ordered; filtered by tag | `BeforeFile`, `BeforeScenario`, `ConfigureScenarioContext` and evidence overrides on the test base; no step hooks; not filtered by tag |
| State and DI | context injection, `ScenarioContext`, `FeatureContext`, DI plugins (MS.DI, Autofac) | phrase classes are scenario services from the fixture's composition scan; the fixture holds state |
| Test data | none built in; the ExternalData plugin feeds Examples from CSV/Excel/JSON | named, typed, validated test sets chosen by `TestData` (AD-012); a schema; the `bruinell-data` tool; mock bodies are contract types (BRUINELL001) |
| Several apps or users | not a concept | the step's subject picks the app: `Eva taps Save` (AD-015); one file serves every head (AD-014) |
| Test frameworks | MSTest, NUnit, xUnit, TUnit | xUnit |
| Parallel runs | through the test framework, per feature | one UI process at a time (a real app on the desktop) |
| Skipping | `@ignore`; tags map to test categories and traits | skip rules: a tag plus an environment variable; head tags (`@maui`, `@web`) |
| Pending steps | `ScenarioContext.Pending()`; undefined steps print a C# snippet | unbound is a bind error; `bruinell-data snippet` prints the stub; expected-failure files prove that a file *fails* to bind |
| Reports | Cucumber Messages and an HTML formatter; many third-party reporters read Messages | `<scenario>__result.json`, screenshots on failure and the shared artifact layout (AD-007); timings and an accessibility audit from the MAUI Windows driver |
| Retry of flaky scenarios | community plugins | none, on purpose (AD-009: app bugs stay failures) |
| Traceability | tags | the `Journey` field, and the journey coverage check that every journey has a test in each tier |
| IDE | Visual Studio extension, Rider plugin, Cucumber extensions for VS Code | Bruinell BDD for VS Code and Visual Studio (`docs/guides/vscode-extension.md`, `docs/guides/visual-studio-preview.md`); the Presenter works without an IDE |
| Demo and stepping | none | the **Presenter**: loads a UAT project, runs a scenario step by step with pacing, places the app, shows the catalog and pages (AD-018) |
| Ecosystem | large: Cucumber tooling, formatters, plugins, SpecFlow migration | this repo |

## 3. What Reqnroll has that Bruinell.Uat doesn't

Each row ends with a verdict: **add**, **later**, or **no** (with the reason).

| # | Reqnroll has | Worth it for Bruinell? | Verdict |
| --- | --- | --- | --- |
| R1 | **Step hooks** (`BeforeStep`/`AfterStep`) and **hooks filtered by tag** (`[BeforeScenario("hardware")]`) | Yes. A screenshot or log after each step, a per-tag setup (`@offline` starts offline), and "reset the server before `@backend` scenarios", without a base-class override per suite. | **add**: `[UatBeforeScenario("tag")]`, `[UatAfterStep]` on phrase classes, run in a stated order |
| R2 | **Several Examples blocks**, each with a name and tags | Yes, a small parser change. Positive and negative rows stay apart, and a head tag can sit on one block (`@web` rows only on the web). | **add** |
| R3 | **Keywords in other languages** (`# language: nl`) | Maybe. Construction's testers may want to write Dutch. Phrase text can already be Dutch; the keywords can't. The catalog is per language anyway, so a file is one language. | **decide** with Construction's testers; cheap if wanted (a keyword table per language in the parser) |
| R4 | **Set and instance comparison with a diff** (`CompareToSet`) | Yes. Every "should have todos" phrase writes its own comparison and message. A `BddTable.AssertSet(expected, actual)` in `Bruinell.Uat` gives one readable diff (missing, extra, different cells), using the same value reader. | **add** |
| R5 | **Cucumber Messages output** | Yes, for reach. Allure, ReportPortal and Azure DevOps read it. A Messages writer next to `__result.json` costs little and lets Construction use a standard dashboard. | **add**, after the IDE work settles the result format |
| R6 | An **HTML report** | Partly. The scenario files already read as documentation on GitHub. A report with results beside the steps is still useful. Build it from the preview renderer in the extension proposals, so there's one rendering. | **later** |
| R7 | **Rule** (grouping scenarios under a business rule, with its own Background) | Weak. One file is one journey, and scenario ids (`TOD.02.1`) already group. | **no**, for now |
| R8 | **Parallel execution** | Not for MAUI on Windows (one real app on the desktop). Possible for the web head, and for Android on several emulators. | **later**, web head first |
| R9 | **Cucumber expressions**: optional text and alternatives (`a/an`, `todo(s)`) | No. One sentence, one meaning, one spelling is what keeps binding strict and completion useful (AD-016). | **no** |
| R10 | **Scoped bindings by tag or feature** | The need is real; the mechanism isn't. Tag scoping hides where a step comes from. | **no**: page scope, folder namespaces and `Using` ([AD-019](../../.docs/decisions/ad-019-phrases-have-scopes-and-namespaces.md)) cover it better |
| R11 | **Several test frameworks** | No. Bruinell is xUnit throughout (AD-006). | **no** |
| R12 | **Retry plugins** | No (AD-009). | **no** |
| R13 | **Examples from external files** (ExternalData) | The need is covered by typed test sets. Examples from a test set's table might be handy later. | **later**, if asked |
| R14 | **IDE support** | Yes; that's the gap users feel most. | **add**: the extension proposals |
| R15 | **Portability**: `.feature` files work with any Cucumber tool | A real cost of the Markdown format. Generic Gherkin editors and linters don't read `.uat.md`. Fenced ```` ```gherkin ```` blocks get highlighting on GitHub and in editors, and the extensions cover the rest. | **no**; accept the cost |
| R16 | **Importing** existing SpecFlow/Reqnroll suites | Only if a team arrives with one. A `.feature` → `.uat.md` converter is mechanical for the structure. Step definitions don't carry over; they become phrase classes or disappear into control verbs. | **later**, on demand |

## 4. What Bruinell.Uat has that Reqnroll doesn't

| # | Bruinell.Uat has | Why it matters |
| --- | --- | --- |
| B1 | **Phrases from the page graph.** `{page}` and `{control}` resolve against page objects; control verbs (`I tap {control}`, `{control} should equal {value}`) and page flow are built in; names are inferred (`AddButton` → **Add**). | Most UI steps need **no glue code**. In Reqnroll every phrase is a step definition someone writes and keeps up. |
| B2 | **Bind before run** for the whole document, with coded diagnostics, and `bruinell-data validate` in CI without the app. | A typo, an unknown control or a bad date fails in seconds, not after two minutes of app start-up, and the IDE can show the same errors (one checker). |
| B3 | **One right answer per value** (AD-016): culture-invariant numbers and dates, `1,5` refused with the fix, relative dates on the fixture's clock, `--fix` for the fixable ones. | Scenarios mean the same on every machine. Reqnroll's parsing follows a configured culture, and a typo can become a wrong value. |
| B4 | **Callable phrases** written by testers in Markdown (AD-017). | Testers build their own vocabulary without a developer, and it can't grow into a program. |
| B5 | **Typed, validated test data** chosen by the scenario (AD-012), with a JSON schema and the `bruinell-data` tool; **mock bodies are contract types** (BRUINELL001). | Arranging data is declarative and checked. Reqnroll leaves it to each team. |
| B6 | **One scenario file, every head** (AD-014), with head tags. | The MAUI app and the web app pass the same acceptance text. |
| B7 | **Several apps and users in one scenario** (AD-015): `Eva taps Save`, `Daan uses the app`. | Multi-user journeys (assign, sync between devices) are ordinary scenarios. |
| B8 | **Markdown files**: prose, headings, links and a metadata table beside the steps; readable on GitHub as they are. | The scenario file is the documentation. No separate living-doc tool is needed. |
| B9 | **The Presenter**: step-by-step runs with pacing and app placement, discovery of pages and controls, the command catalog; no IDE needed (AD-018). | Demos to users, and testers without Visual Studio. |
| B10 | **Evidence built in**: screenshots on failure, a value that is almost right reported with its one right spelling, and one artifact layout (AD-007); on MAUI Windows also timings and an accessibility audit. | A failure explains itself without rerunning. |
| B11 | **Journey traceability**: the `Journey` field plus the coverage check across unit, integration, contract, UI and UAT tiers. | "Is every journey tested, at every level?" is a test, not a spreadsheet. |
| B12 | **Expected-failure files** and **skip rules** tied to environment variables. | Proves the checker catches what it should; hardware and live-API scenarios are skipped explicitly, not silently. |
| B13 | **Static page tracking and phrase namespaces** ([AD-019](../../.docs/decisions/ad-019-phrases-have-scopes-and-namespaces.md)). | Scales to Construction's size without the global step-definition pool Reqnroll has. |

## 5. Summary

- **Bruinell.Uat's strengths come from knowing the UI and checking early**: B1-B3, B6 and B7. Those
  are what Reqnroll structurally can't do, because it's a general Gherkin runner. The price is the
  Cucumber ecosystem (R15), and that is accepted.
- **Worth taking from Reqnroll**:
  1. step and tag-filtered hooks (R1);
  2. several Examples blocks (R2);
  3. a table set comparison with a diff (R4);
  4. Cucumber Messages output (R5);
  5. IDE support (R14);
  6. a decision on Dutch keywords (R3).

  None of them conflicts with an AD.
- **Deliberately not taken**: flexible matching (R9), retries (R12), tag-scoped bindings (R10) and
  more test frameworks (R11). Each is ruled out by an existing AD or by the phrases plan.

Suggested order, smallest first: R2, R4, R1, R5, then R3 once Construction answers. Each is a small
plan of its own. R14 follows the extension proposals.
