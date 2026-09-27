# Phrases: split, scope and namespace

Status: phases 1-8 done (see the commits per phase); callable phrases were built before. The
"later" row waits for Construction to need it. When this plan is closed, its decisions are AD-019
and the guide's "Where Phrases Live"; section 6's open questions move to their own notes.
Date: 2026-09-27
Replaces three discussion docs, last at 5b73a4b:
[splitting-phrases.md](https://github.com/exact-mobile-apps/test-tooling/blob/5b73a4b/.my/uat/phrases/splitting-phrases.md),
[phrase-scopes-and-composition.md](https://github.com/exact-mobile-apps/test-tooling/blob/5b73a4b/.my/uat/phrases/phrase-scopes-and-composition.md),
[phrase-namespaces.md](https://github.com/exact-mobile-apps/test-tooling/blob/5b73a4b/.my/uat/phrases/phrase-namespaces.md).
First consumer: the Todo users UI tests and UAT ([users-plan.md](users-plan.md#8-later)).

## 1. The problem

Phrases work: scenario files read as user intent. But the catalog is **one flat list**, and
everything the page graph can't say goes into one class. In Todo that's `TodoScreenPhrases` (19
phrases: rows, list state, the dialog, server faults, network, restarts) plus `TodoDataPhrases`
(5). The real target is Construction: many modules, many teams, many testers. At that size a flat
list fails three ways:

- **Collisions.** "I add a line" means one thing on Invoice Edit and another on Purchase Order
  Edit. In a flat list only one of them can exist.
- **Discoverability.** A tester can't scan hundreds of phrases for the one that fits here.
- **Ownership.** A team can't change its phrases without checking every other team's scenarios.

The answer has three parts, built in order: **split** phrases into the right homes, give every
phrase a **scope**, and let scenarios reach other teams' phrases through **namespaces**.

## 2. What holds today

| Fact | Consequence |
| --- | --- |
| Phrase classes are found by namespace scan; any number of `[UatPhraseClass]` classes are picked up. | Splitting into more classes is free. |
| The catalog is flat: a phrase binds by its text alone. Duplicates are a diagnostic (`AddDuplicatePhraseDiagnostics`). | A split today is code organisation only. It scopes nothing. |
| Control verbs (`[UatGiven]`/`[UatWhen]`/`[UatThen]` on a control or control interface) resolve `{control}` against the **current page**. | Behaviour placed on a control is page-scoped already. |
| A control verb has `{control}`, optionally `{user}` (AD-015), and at most **one** value (`UatCatalogBuilder`). | "the todo {title} should show status {status}" can't be a control verb yet. |
| `ContentDialog` and `StateContainer` carry no verbs. | The dialog and list-state phrases sit in a phrase class only because nothing lower owns them. |
| Binding happens before running (`UatBinder.Bind` binds the whole document). | Any scope that depends on "the current page" must be known **statically**. |
| Callable phrases are built (AD-017): file-local and global `Scenarios/*.phrases.md`, expanded inside the binder's walk, with `UatCallFrame` marking steps that came from a call. | Callables slot into scopes with no rework (sections 5-6). |
| Page names are global; a duplicate is a diagnostic. | "Detail" and "Edit" will collide in a large app, like phrases. |

Todo's 24 phrases, by what they touch:

| Group | Phrases | Touches | Home (section 3) |
| --- | --- | --- | --- |
| Row of a list | open the todo {title}; the todo {title} should show status / a due date / no due date / be waiting to sync / be synced / have a read-only status | a collection item | control verb, once item verbs exist |
| List state | the list should show the {state} state | `StateContainer` | control verb |
| Dialog | the dialog should ask {title}; I choose {button} in the dialog; no dialog should be shown | `ContentDialog` | control verb |
| Backend | the server also has todos; fails with status {n}; fails once; answers after {n} seconds; recovers; should have todos | the fake API | subject class |
| Device | the device is offline / back online | the network-state file | subject class |
| App lifecycle | I restart the app; ... with the server unreachable | the process | subject class |
| Stored data | the database should have todos / only todos / not a todo with a blank title | the app's SQLite file | subject class |

11 touch a control on screen, 13 touch something outside the app. Those want different homes.

## 3. Decisions

### D1. Five kinds of phrase, each with one home

| Kind | Written by | Written in | Scope |
| --- | --- | --- | --- |
| **Control verb** | framework / app devs | C#, keyword attributes on a control | the page the control is on |
| **Page phrase** | app test devs | C#, keyword attributes on a page-object method | that page |
| **Subject phrase** | app test devs | C#, one phrase class per thing outside the screen | global (via project-wide usings, D6) |
| **Namespace phrase** | app test devs | C#, `[UatPhraseClass(Namespace = "...")]` | a namespace |
| **Callable phrase** | **testers** | markdown, `## Phrase:` (AD-017) | file, folder namespace or global |

The first four need a developer and can hold logic. Callables need no developer and can't.

Rejected splits: **by scenario or journey** (phrases are shared vocabulary; journeys are where
they're used, not where they live), **by keyword** (one concept across three files), and **by page
as a phrase-class convention** (by page happens by itself, through controls and page phrases).

### D2. Controls first, but only small, generic verbs

Before a phrase goes into a class, ask whether it is control behaviour. A control verb is the most
reusable kind of phrase, so it must be the least specific. The test is "one control, one action".
It doesn't matter whether the control lives in Brinell or in the app.

| Belongs on a control | Doesn't |
| --- | --- |
| `I tap {control}`, `{control} should equal {value}` | anything naming a domain concept ("invoice", "work order") |
| `I choose {value} in {control}` (dialog) | anything spanning controls (a page phrase) |
| `{control} should show the {value} state` | anything needing fixture state (a subject phrase) |

Because one scenario file serves every head (AD-014), a verb added to a MAUI control also needs its
Html/Blazor counterpart.

### D3. Item verbs: an `{item}` placeholder

A verb on a collection may declare `{item}`, which the engine resolves through the collection (its
row lookup), then invokes the verb on that item. So the row phrases become control verbs, and the
scenarios don't change:

```csharp
// on TodoList
[UatThen("the todo {item} should show status {value}")]
public TodoList AssertRowStatus(string item, string value) => ...
```

Rejected: the item as a control path (`Todos["Buy milk"].Status`). It's more general, but reads
like code.

### D4. Page phrases, with the returned page as "page after"

A page's own workflows are page phrases: `I create a todo {value}` is Add, set Title, Save, back on
the list. The page object already has the method. Making it a phrase takes one attribute:

```csharp
[UatWhen("I create a todo {value}")]
public TodoListPage CreateTodo(string title) => Add().EnterTitle(title).SaveNew();
```

The return type does real work: it tells the binder which page is current after the step, without
running it (D7). Guardrail: a page phrase is **one user intent**. "Create a todo and open it and mark
it done" is three steps, or a callable.

### D5. Scopes and visibility

A step can bind to a phrase that is visible from where it stands. That is the union of:

1. the **current page**: its control verbs and page phrases;
2. the **current file**: file-local callables;
3. the file's **folder and every ancestor folder**. This is implicit, like `.editorconfig`;
4. the namespaces the file **uses** (D6);
5. **global**: generic control verbs and the project-wide usings.

**No shadowing.** If a step matches phrases in two visible scopes, that is an ambiguity error at
bind time, never "innermost wins". In prose a tester reads, one sentence must never mean two things
in one place.

### D6. Namespaces are logical names; `Using` reaches siblings

A namespace is **a name that owns a set of phrases** (and pages, D8). It's a short logical name
(`Accounts`, `Invoicing`, `Backend`), not a CLR namespace: testers read it, and a CLR name changes
when code moves.

**A folder is a namespace.** `Scenarios/Invoicing/Credit notes/` is `Invoicing/Credit notes`. That
folds the old "module scope" into namespaces: there's one hierarchy, the folders. C# joins a
namespace by declaring the same name:

| Kind | Namespace from |
| --- | --- |
| C# phrase class | `[UatPhraseClass(Namespace = "Accounts")]`, or `[assembly: UatNamespace("Accounts")]` for a whole project |
| Markdown callable | the folder of its `*.phrases.md` |
| Page phrase / control verb | the page. Pages are scopes: you don't use a page, you're *on* it |
| Global | none, always visible, kept small (subject phrases and generic control verbs need a reason to be there) |

Folders alone can't share between siblings. If Invoicing needs Accounts' "an account {name} with
payment terms {days} days", moving it up to the common ancestor makes it global. Done a few dozen
times, that collapses back into the flat list. So there are three mechanisms:

| Mechanism | Covers |
| --- | --- |
| Folder hierarchy | **implicit**: own folder plus ancestors, with nothing written |
| `Using` metadata row in the file | **explicit**: siblings and other projects |
| `## Using` in `uat.config.md` | **project-wide**: the subject phrases every file needs (like C# global usings) |

```markdown
| Field | Value |
| --- | --- |
| Journey | INV.03 |
| TestData | one-account |
| Using | Accounts |
```

Rules for `Using`:

- **Explicit, not "everything visible":** the reader knows where a step comes from. Collisions
  stay local to files that use both namespaces. "Who depends on Accounts?" is a search.
- **Not transitive.** A file sees what it names.
- **One per file**, not per scenario: a file is one journey.
- **Checked against what's loaded.** Phrase assemblies join the `## Assemblies` table in
  `uat.config.md`, each announcing its namespace. A `Using` that nothing provides is a bind-time
  error.
- Namespaces and assemblies line up but aren't the same: one `Exact.Construction.Uat` assembly with
  `Namespace` per class works as well as one assembly per team. If a C# phrase calls into another
  team's page objects, that's a project reference, not a `Using`.

### D7. Static page tracking

"The current page" is worked out by reading the steps in order. This table is the one definition
of the page after a step:

| Step | Page after it |
| --- | --- |
| `Given I am on the X page` / `Then I should be on the X page` | X |
| a page phrase returning a page type | that page |
| a call to a callable | the page its inner steps end on, tracked through them in the caller's page; unchanged if they name none |
| a control verb whose method returns a page type | that page |
| a tap on a control with `[UatNavigatesTo(typeof(X))]` | X |
| another control verb marked `Navigates = true` (`I tap`, the dialogs' `I choose`) | **a guess**: the same page, until a step does not fit it |
| any other step | unchanged |

For the guess:

1. **Rule:** after a verb that may leave the page, the next page-scoped step either fits the page
   it was on, or is an error (`UATB008`) naming the verb, asking for an `I should be on ...`. Todo
   scenarios already assert the page after every navigation.
2. **Where free:** `[UatNavigatesTo(typeof(TodoListPage))]` on a button that always goes one place.
3. **Not now:** falling back to run-time binding. It loses the early unbound-step error.

Built as the stricter "unknown after every tap", every tap followed by a check on the same page would
need an assert first; the guess keeps those scenarios as they are and still names the step that
lost the page.

### D8. Pages get namespaces too

The namespace that owns Accounts' phrases also owns its pages. So `Given I am on the Account Detail
page` resolves "Account Detail" among the pages of the visible namespaces. The rule is the same:
ambiguity is an error.

### D9. Callables bind where they are defined

The steps *inside* a callable bind in the **definition's** scope (lexical scoping). Otherwise every
caller would repeat the callee's usings, and changing a callable's inside would break its callers.
The **page**, though, is the caller's: a call is bound where it's called, so one callable can bind
to different page phrases at two call sites (as today).

What already holds, so callables written now keep working:

- A callable that names a page ends with one (`UATC006`), and a per-row body that names a page ends
  where it started. Both read `UatCommandPattern.NamesPage`, with no tracking.
- The start page is not declared. A callable that must start on a page begins with
  `Given I am on ...`.

### D10. No qualified steps, for now

When two used namespaces both have "I archive it", first don't use both, then rename one ("I archive
the account"). A qualified step (`When [Accounts] I archive it`) is easy to add later and hard to take
away. Add it only if those two prove insufficient.

## 4. Phases

Each phase is useful on its own. Phases 1-3 need no engine scoping; 4-7 build scopes; 8 is the first
real consumer.

| # | Phase | Decisions | Done when |
| --- | --- | --- | --- |
| 1 | Control verbs for dialog and list state | D2 | the three dialog phrases and list state are control verbs, and removed from `TodoScreenPhrases` |
| 2 | Item verbs | D3 | `{item}` in the catalog and binder; the seven row phrases live on `TodoList` |
| 3 | Subject classes | D1 | `TodoScreenPhrases` is gone; `BackendPhrases`, `DevicePhrases`, `AppLifecyclePhrases`, `StoredDataPhrases` |
| 4 | Page phrases | D4 | keyword attributes on page-object methods; the returned page type recorded as page after |
| 5 | Static page tracking | D5, D7, D9 | page-scoped binding for control verbs and page phrases; ambiguity across scopes is an error; the page followed through a callable's steps |
| 6 | Folder namespaces | D5, D6, D9 | a scenario sees its folder and ancestors; module `*.phrases.md` files; a callable's body binds in its definition's folder |
| 7 | `Using`, config usings, namespaced pages | D6, D8 | the `Using` row, `## Using`, assemblies announcing namespaces, pages resolved by namespace; the tooling in section 5 |
| 8 | Todo users UI tests and UAT | all | [users-plan.md section 8](users-plan.md#8-later): `Using \| Users`, plus the expected-failure proof that a file without it fails binding |
| later | Promote reusable subjects | - | backend faults, network and restart move behind small fixture interfaces (`IUatBackend`, `IUatNetwork`, `IUatRestartable`) when Construction needs them |

### Phase 1: control verbs for dialog and list state

- `ContentDialog` (`Brinell.Maui`): `{control} should ask {value}`, `I choose {value} in {control}`,
  `no {control} should be shown` (or `{control} should not be shown`, whichever reads right in the
  scenarios). The same on the Html dialog for the web head.
- `StateContainer`: `{control} should show the {value} state`, mapping a state name to its view id.
- Todo scenarios keep their text, or change once, with the change listed in the commit.
- Tests: Brinell.Uat catalog tests for the new verbs; Todo MAUI UAT (32) and web UAT (33) unchanged.

### Phase 2: item verbs

- `UatCatalogBuilder` allows a second named placeholder, `{item}`, on collection controls; the binder
  resolves it through the collection's row lookup.
- The row phrases move onto `TodoList` (MAUI) and its web counterpart. "open the todo {title}" is an
  item verb too.
- Tests: catalog and binder tests for `{item}` (found, not found, ambiguous row); Todo UAT unchanged.

### Phase 3: subject classes

- File by what a phrase *arranges or checks*, not by what it goes through.
- `TodoDataPhrases` splits between `BackendPhrases` (server todos) and `StoredDataPhrases`.
- After phases 1-3, 13 phrases remain, all outside the screen.

### Phase 4: page phrases

- The catalog accepts keyword attributes on page-object methods and records the returned page type.
- Todo: `I create a todo {value}` on `TodoListPage` (and web), replacing multi-step sequences where
  the scenarios repeat them.

### Phase 5: static page tracking

- The binder walks the flat invocation list and keeps the page after each step, using the D7 table.
  `UatCallFrame` already says which steps came from a call.
- Control verbs and page phrases bind only against the current page. When the page is unknown, a
  page-scoped step gets a bind error that names the step that lost it.
- `[UatNavigatesTo]` on buttons where it's free.
- Built: `UatPageScope` and the page walk in `UatBinder`; `Navigates` on `UatPhraseAttribute`;
  `UATB008` (no page, or lost at a named step) and `UATB009` (does not fit the page, or no such
  page). A missing control is now a bind error, so the MAUI expected-failure file uses
  `RunExpectedBindFailureUatFile`. Todo: `[UatNavigatesTo]` on Add, Edit and Back on both heads.
  A fixture phrase with the text of a page phrase is no longer dropped, so the pair is ambiguous.

### Phase 6: folder namespaces

- The callable loader's "only the root" rule becomes "the root and the file's folder chain".
  `UatCallableSet` gets a scope per callable. Matching and binding don't change.
- `[UatPhraseClass(Namespace = ...)]` and `[assembly: UatNamespace]`; a C# class with a namespace is
  visible to scenarios in that folder and its descendants.
- Built: `UatNamespaces` (visibility, a file's namespace) and `UatScope` in the binder's walk; a
  callable carries its `Namespace` and, for a scenario file's own, `LocalTo`; a pattern carries
  its `Namespace`. `UatScenarioFiles.PhraseFilesFor` is the folder chain (root first).
  `UATB010` for a step that matches only phrases the file does not see, naming their folder.
  `brinell-data` checks phrase files in subfolders and lists namespaces (`in Invoicing`).
- Changed from the plan: `Load` reads **every** phrase file under the root, not only the chain,
  and the binder filters by scope. Otherwise a call to a sibling's callable was "binds to
  nothing" (`UATB001`) while a sibling's C# phrase was `UATB010`; now both name the folder.
  Parse errors still fail only the scenarios that see the file.
- Not done here: Todo keeps its phrase files at the root until phase 8 has modules.

### Phase 7: `Using`, config usings, namespaced pages

- The `Using` metadata row; `## Using` in `uat.config.md`; the phrase assemblies in `## Assemblies`.
- Pages resolved among the visible namespaces.
- Todo moves its subject phrases behind `## Using` (`Backend`, `Device`, `AppLifecycle`,
  `StoredData`).
- An AD (AD-019, the next free number) records D5-D10: scopes, no shadowing, folder = namespace,
  explicit non-transitive `Using`, lexical callables. The guide `uat-phrases-and-flows.md` gets a
  "Where phrases live" section.
- Built: the `Using` row (`UatMetadataFields.Using`, comma-separated) and `## Using`
  (`UatConfig.Usings`, carried by `UatCatalogSet.Usings`); `UatScope` sees a used namespace as
  if the file were in it. `UATB011` (a `Using` naming nothing) and `UATB012` (a warning: no
  step needs it). `UatNamespaceAttribute` moved to `Brinell.Core.Testing` and applies to a class
  too; `TestPageDescriptor.Namespace`; pages are keyed `Invoicing/Detail` in the catalog and the
  runtime, and the binder writes the resolved key into the step. The bind report names each
  step's scope (`UatStepInvocation.Scope`). Todo's subject classes are in `Backend`, `Device`,
  `AppLifecycle` and `StoredData`, used by all three Todo configs. AD-019, and the guide's
  "Where Phrases Live".
- Changed from the plan: phrase assemblies are **not** a new `Phrases` row in `## Assemblies`.
  `[assembly: UatNamespace]` announces the namespace, and the fixture's module scan loads the
  assembly, as it already loads every phrase class; a config row would be a second list to keep
  in step. The Presenter has no step completion, so section 5's completion and "who uses this
  namespace" are `brinell-data phrases <file>` and `brinell-data references <namespace>`, the
  data an editor's completion needs. The config's usings are checked once per suite by
  `brinell-data validate`, not in every file; a generic catalog (`IsGeneric`, the Presenter's workspace check)
  checks no `Using`.

### Phase 8: Todo users UI tests and UAT (built)

- Page objects `UserListPage` and `UserEditPage` (and the `UserList` collection) in
  `Todo.UITests`, marked `[UatNamespace("Users")]`; the Users toolbar item on the todo list goes
  there (`[UatNavigatesTo]`). `UserPhrases` (`the server also has a user`, `the server should
  have users`) in the `Users` namespace.
- `TOD.11-manage-users.uat.md`: eight scenarios for TOD.11.1-11.9, `Using | Users`, on the
  `team` set. `ExpectedFailures/TOD.11-without-using.uat.md` is the same start without the
  `Using` row, and must fail binding with UATB010 naming `Users`.
- `UsersJourneyTests`: the H tier of TOD.11.1-11.9.
- Changed from users-plan section 8: the tests live in the existing Todo UI test and UAT projects,
  not two new ones. The web suite leaves the TOD.11 file out: users have no web head yet.

## 5. Tooling

Namespaces only help if people can see them:

- **The bind report names the scope** each step bound to (`Accounts: an account {name} ...`).
- **Unknown `Using`**: error. **Unused `Using`**: warning.
- **Presenter step completion** offers only the phrases visible at the cursor, grouped by
  namespace. The catalog view is grouped by scope. This feeds the VS Code proposal's IntelliSense
  (`.my/uat/tools/`).
- **"Who uses this namespace?"**: a `references`-style check listing the files with
  `Using | Accounts`.

## 6. Open questions

| Question | Notes |
| --- | --- |
| Folder name as the namespace, or a declared name per folder? | Folder names have spaces and get renamed. A `Namespace` field in the folder's `*.phrases.md` decouples them, at the cost of one more thing to keep in sync. Built with the folder name (phase 6), compared ignoring case. |
| Can a namespace phrase be used on any page, or only its own pages? | Namespace and page scope are independent axes. Start with "any page". |
| Callables on a page scope | Could a callable belong to a page, like a page phrase? Now possible (phase 5 is built); not needed by Todo yet. |
| Presenter UAT flake | `run-selected-sample` asserts "Passed", which the per-scenario status matches while the run goes on; the next scenario's reset races the run's end. Fails about 1 in 3 at `ddaa395` too. **Fixed**: the step waits for "Passed: 1/1 scenarios", the run's last status (4/4 runs green). |
| Deprecated aliases | If Accounts renames a phrase, users break. Should a phrase keep its old text as a deprecated alias for a while? |
| A naming rule per subject | Every backend phrase starts "the server", every stored-data phrase "the database". Then the class can be read off the text. |
| Which package owns the shared subjects (later phase)? | `Brinell.Uat` has no MAUI or mocking dependency. Interfaces in `Brinell.Uat`, implementations beside the fixtures, or a small `Brinell.Uat.Mocking`. |
| A call with many parameters | Five `{x}` in a heading is hard to read; a `Field / Value` table would clash with a step's one table. Not now. |
| `## Background` as a callable | Not done; it keeps its own code. |
