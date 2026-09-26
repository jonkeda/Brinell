# Splitting phrases

Status: discussion
Date: 2026-09-26
Context: the Todo UAT work that added a journey file per journey (commit `fdc048e`)

## The problem

Phrases work. Scenario files read as user intent:

```gherkin
Given the server fails with status 500
When I tap Sync
Then the list should show the Error state
And the todo "Pay the bills" should be waiting to sync
```

But everything the page graph can't say ended up in one class,
`Uat/Phrases/TodoScreenPhrases.cs`: 19 phrases covering list rows, list state, the dialog,
server faults, the network and app restarts. `TodoDataPhrases.cs` holds another 5. With ten
journeys that is already hard to scan, and Construction will have far more.

So the question is how to split them, and whether there is one good split or several.

## What the engine allows today

This limits which splits are possible without engine work:

| Fact | Consequence |
| --- | --- |
| Phrase classes are found by namespace scan (`[TestModuleScan(..., NamespacePrefix = "...Uat.Phrases")]`). Any number of `[UatPhraseClass]` classes are picked up. | Splitting into more classes and files is **free**, with no engine change. |
| The phrase catalog is **one flat list**. A phrase binds by its text alone, wherever the scenario is. Duplicates are reported by `AddDuplicatePhraseDiagnostics`. | A split is **code organisation only**. It doesn't scope anything. Two classes can't both own `"I tap Retry"`. |
| Control verbs (`[UatStep]` on a control) are already page-scoped: `{control}` resolves against the **current page**. | Behaviour that belongs to a control, placed on the control, is split by page automatically. |
| A control verb has `{control}` plus at most **one** `{value}` (`UatCatalogBuilder`). | "the todo {title} should show status {status}" has two values, so it *can't* be a control verb today. That's why it ended up in a phrase class. |
| `ContentDialog` and `StateContainer` have no `[UatStep]`s. | The dialog and list-state phrases are in a phrase class only because nothing lower owns them. |

The last two rows matter most. Much of `TodoScreenPhrases` isn't really "phrases": it's
control behaviour that had nowhere else to go.

## Where today's phrases actually belong

The current 24 phrases, grouped by **what they touch**:

| Group | Phrases | Touches |
| --- | --- | --- |
| Row of a list | open the todo {title}; the todo {title} should show status / a due date / no due date / be waiting to sync / be synced / have a read-only status | a collection item, i.e. a control |
| List state | the list should show the {state} state | the `StateContainer`, a control |
| Dialog | the dialog should ask {title}; I choose {button} in the dialog; no dialog should be shown | `ContentDialog`, a control |
| Backend | the server also has todos; the server fails with status {n}; fails once with status {n}; answers after {n} seconds; recovers; the server should have todos | the fake API, outside the app |
| Device | the device is offline; is back online | the network-state file, outside the app |
| App lifecycle | I restart the app; I restart the app with the server unreachable | the process, outside the app |
| Stored data | the database should have todos / have only todos / not have a todo with a blank title | the app's SQLite file, outside the app |

The split is sharp: **11 phrases touch a control on screen, 13 touch something outside the app.**
Those two kinds want different homes.

## The options

### A. By page

`TodoListPhrases`, `TodoDetailPhrases`, `TodoEditPhrases`.

- For: matches how testers think ("what can I say on this screen?"); mirrors the page objects.
- Against: most phrases here aren't page-bound. The dialog belongs to both detail and edit.
  Restart, network and server have no page at all. And because the catalog is flat, "by page"
  would be a folder convention the engine doesn't enforce. A list phrase would still bind while
  the scenario is on the detail page and then fail at run time.
- Verdict: right for **control behaviour**, but the mechanism is already there: put the verb on
  the control, and the page it sits on scopes it. Not worth it as a phrase-class convention.

### B. By scenario or journey

`TOD01Phrases`, `TOD08Phrases`, ...

- For: easy to find "the steps this journey needs".
- Against: phrases are shared vocabulary. "the server fails with status" is used by TOD.08 and
  would be by any future error journey. Per-journey classes either duplicate it (a duplicate
  diagnostic) or pick an arbitrary owner. It also rewards writing new phrases per journey
  instead of reusing ones.
- Verdict: **no.** Journeys are where phrases are *used*, not where they *live*.

### C. By keyword (Given / When / Then files)

- Against: one concept ("the server fails" / "the server recovers" / "the server should have")
  ends up in three files. Keywords are grammar, not subject.
- Verdict: **no.**

### D. By what the phrase touches (subject)

One class per thing outside the screen, named after it:

```text
Uat/Phrases/
  BackendPhrases.cs      the server also has / fails / recovers / should have
  DevicePhrases.cs       offline / back online
  AppLifecyclePhrases.cs restart / restart with the server unreachable
  StoredDataPhrases.cs   the database should have ...
```

- For: each file answers one question ("how do I arrange the server?"). The groups are stable:
  a new journey adds phrases to an existing subject more often than it creates a subject. It
  lines up with the fixture's own members (`Backend`, `Server`, `Network`, `Restart`,
  `DatabasePath`).
- Against: needs a judgement call for a phrase that touches two things. Rule: file it by what
  it *arranges or checks*, not by what it happens to go through.
- Verdict: **the right split for phrases that stay phrases.**

### E. Push down into controls first

Before splitting a phrase class, ask whether the phrase is really control behaviour:

| Phrase today | Better home |
| --- | --- |
| the dialog should ask {title}; I choose {button} in the dialog; no dialog should be shown | `[UatStep]`s on `ContentDialog` in `Brinell.Maui`: `{control} should ask {value}`, `I choose {value} in {control}`. Then every app has them, not only Todo. |
| the list should show the {state} state | a `[UatStep]` on `TodoListState` (or generically on `StateContainer`) that maps a state name to the view id: `{control} should show the {value} state` |
| the todo {title} should show status {status}, ... | needs an **engine feature**: an item-scoped verb. See below. |

- For: follows the rule the phrase guide already states: "When a verb belongs to a control,
  declare it there with `[UatStep]`". Page scoping comes for free, and generic controls give the
  verbs to every app.
- Against: row verbs can't move until the engine can address an item.
- Verdict: **do this first.** It removes 4 of the 24 phrases right away and 7 more once item
  verbs exist.

### F. By reach: app-specific or reusable

Some of the "outside the app" phrases aren't about Todo at all:

| Phrase | Needs | Could live in |
| --- | --- | --- |
| the server fails with status {n}, fails once, answers after {n} seconds, recovers | a `MockApiServer` and the path(s) to fail | `Brinell.Mocking` or `Brinell.Uat`, parameterised by an `IUatBackend` the fixture exposes |
| the device is offline / back online | a way to switch the network | shared, behind an `IUatNetwork` |
| I restart the app | restart plus re-scope (the `RescopeAfterRestart` fix) | shared, behind an `IUatRestartable` fixture. The re-scope trick then lives in one place instead of every app |
| the server also has todos; the database should have todos | the app's DTOs and database | app-specific (Todo), like today |

- For: Construction would reuse the first three rows unchanged. The fiddly restart re-scope is
  written once.
- Against: needs small fixture interfaces, and a decision on which package owns them.
- Verdict: **a second step**, after D. D's subject classes are exactly the units that would move.

## The engine feature behind the row phrases

Seven row phrases are a phrase class only because a control verb can't name an item:

```gherkin
Then the todo "Buy milk" should show status "Open"
```

Two ways to make that a control verb, in order of preference:

1. **Item placeholder.** Let a `[UatStep]` on a collection declare an `{item}` placeholder that
   the engine resolves through the collection (`Row(title)`), then invoke the verb on the item:

   ```csharp
   // on TodoList
   [UatStep(UatEffectiveStepKeyword.Then, "the todo {item} should show status {value}")]
   public TodoList AssertRowStatus(string item, string value) => ...
   ```

   Small change in `UatCatalogBuilder` (allow a second named argument) and in the binder.

2. **Item as control path.** `Todos["Buy milk"].Status should show status "Open"`. More general,
   but the scenario text reads like code, which is what the phrases are meant to avoid.

Option 1 keeps the scenarios exactly as they are now.

## Recommendation

A layered answer, since the split isn't one-dimensional:

1. **Controls first (E).** Dialog verbs onto `ContentDialog` and the list-state verb onto the
   state container. Page scoping follows from where the control is.
2. **Subject classes for the rest (D).** `BackendPhrases`, `DevicePhrases`,
   `AppLifecyclePhrases`, `StoredDataPhrases`. The row phrases go into a temporary
   `TodoRowPhrases` until item verbs exist.
3. **Item verbs in the engine** (option 1 above), then move `TodoRowPhrases` onto `TodoList`.
4. **Promote the reusable subjects (F)** once Construction needs them: backend faults, network,
   restart.

Not by scenario, not by keyword. "By page" happens by itself through the controls and doesn't
need to be a phrase-class convention.

After steps 1-3, 13 phrases remain, all outside the screen. After step 4, Todo keeps only what is
genuinely Todo: arranging server todos and checking the database, 5 phrases instead of 24.

## Open questions

| Question | Why it matters |
| --- | --- |
| Should the catalog ever be *scoped* (a phrase only valid on a page)? | A flat catalog keeps phrases simple, but a large app will hit text collisions. Scoping would need the binder to track the page statically through `I am on` / `I should be on` steps, because binding happens before running. |
| Which package owns the shared subjects (F)? | `Brinell.Uat` has no MAUI or mocking dependency today. Probably a small `Brinell.Uat.Mocking` or interfaces in `Brinell.Uat` with implementations next to the fixtures. |
| One naming rule for phrase text per subject? | E.g. every backend phrase starts with "the server", every stored-data phrase with "the database". Then the file a phrase lives in can be read off its text, and the catalog listing groups itself. |
| Does the Presenter / catalog listing show the class a phrase comes from? | If it does, the subject classes double as the vocabulary's table of contents for scenario writers. |
