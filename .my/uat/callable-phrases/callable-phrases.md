# Callable phrases: reuse without a language

Status: built, steps 1-5 and 7 and global phrase files ([design.md](design.md), section 14); steps 6 and 8 wait for scopes (2026-09-27)
Date: 2026-09-26
Split from: [phrase-scopes-and-composition.md](../phrases/phrase-scopes-and-composition.md), section G
Builds on: [AD-016](../../../.docs/decisions/ad-016-phrase-arguments-are-typed-by-the-method.md) (typed arguments, `UatValues`)

Callable phrases let a tester give a sequence of steps a name, and use it with parameters. Where
they are declared and who can see them follows the scope model in the scopes doc (page, file,
module, global).

## The need

Testers repeat themselves. Every Projects scenario starts "create a project, add a work order, log
8 hours". Today there are only two ways to avoid that:

- `## Background`: per file, no parameters, only at the start.
- a C# phrase: needs a developer.

Neither lets a **tester** say "this sequence has a name; use it here with these values".

## The shape

A named, parameterised block of steps, written in scenario language:

````markdown
## Phrase: a project {name} with {hours} unbilled hours

```gherkin
Given I am on the Projects page
When I create a project <name>
And I add a work order to <name>
And I log <hours> hours on the work order
Then I should be on the Project Detail page
```
````

Used like any phrase:

```gherkin
Given a project "Riverside" with 8 unbilled hours
```

The heading declares the parameters as `{name}`, as a C# phrase does. The body uses them as
`<name>`, as a `Scenario Outline` does, because that is what happens: the call is expanded like an
outline with one Examples row, the call's values. The expansion reuses the outline code, so a
value lands in step text, in table cells and in doc strings exactly as an Examples cell does.

It binds like a phrase, and **its inner steps are bound at bind time too**, with the values
substituted, so a typo inside a callable phrase is caught before anything runs, just like
everywhere else.

Where it lives follows the scope model:

| Declared in | Visible to |
| --- | --- |
| the `.uat.md` file, under `## Phrase:` | that file |
| a `Scenarios/Projects/*.phrases.md` file | the Projects module |
| `Scenarios/*.phrases.md` at the root | everything |

## Parameter types

A callable phrase has no C# method, so nothing declares its parameters' types the way AD-016
does for C# phrases. There are two sources, and both are used.

### Inferred from where the value goes (the default)

A parameter's type is the type of the C# parameter it lands in. In the example, `<hours>` fills
`{hours}` in `I log {hours} hours on the work order`, whose method takes a `decimal`, so the
callable's `{hours}` is a number. The binder finds this by binding the body once with the
placeholders left in: a `<hours>` that is a whole argument of an inner step takes that argument's
type.

| `<x>` lands in | Its type |
| --- | --- |
| a whole argument of an inner step | that argument's type |
| several arguments | every one of them: the value must read as each. If no value can (a date and a number), the callable is broken (below) |
| part of an argument (`"<name> copy"`), or only step text | text |
| a table cell | the column's member type, when the table binds to records; text otherwise |
| nothing | the callable is broken: `{x}` is never used |

This is inference from **where the value goes**, not from **what callers write**. Guessing from
calls would make `8` a whole number in one file and `"8"` text in another, and a callable's type
would change when someone adds a call. The inner steps don't change when someone calls it.

### Declared, when you want it

A `Parameters` table directly under the heading declares the types, and gives each an example:

````markdown
## Phrase: a project {name} with {hours} unbilled hours due {due}

| Parameter | Type   | Example     |
| --------- | ------ | ----------- |
| name      | text   | Riverside   |
| hours     | number | 8           |
| due       | date   | 12-May-2026 |

```gherkin
...
```
````

- The type names are the ones `brinell-data phrases` already shows: `text`, `whole number`,
  `number`, `true/false`, `date`, `date and time`, `time`, `duration`, `user`, `list of ...`, an
  enum or app type by its C# name (`TodoStatus`, `Money`), and `table` (see below). What the
  tool lists is what you write.
- The table is optional. It is useful when inference can't see the type (`<due>` only appears
  inside a longer text, but should still be a date), and as documentation: the examples are read
  as their types, so they are checked, and the phrase list and the Presenter show them.
- Where both exist they must agree. A declared `date` that lands in a `decimal` argument is a
  broken callable, not a silent conversion.
- The table can be partial. Parameters it doesn't list are inferred.

### Checked at every call

Every call is expanded and its inner steps are bound with the call's values, so a value is
checked by the same `UatValues` reader, with the same `UATA0xx` codes, as if it had been written
in the inner step. The diagnostic is reported **at the call's line** and names the inner step and
the parameter:

```text
Projects.uat.md:14 UATA003 {hours} = "8,5": a number is written 8.5
  in "a project {name} with {hours} unbilled hours" (Projects.phrases.md:3),
  step 4: And I log <hours> hours on the work order
```

`--fix` works on calls as it does on steps: it rewrites the value in the call, never the body.

## Tables as arguments

A step has at most one table or doc string under it, and a call is a step. So a callable can
take the table under its call. It says where the table goes with a line `<table>` in place of an
inner step's table:

````markdown
## Phrase: the server has todos for {user}

```gherkin
Given <user> is signed in
And the server also has todos
  <table>
When <user> refreshes the list
```
````

```gherkin
Given the server has todos for Eva
  | Title            | Due Date    | In Progress |
  | ---------------- | ----------- | ----------- |
  | Post the letters | 12-May-2026 | false       |
  | Paint the fence  | +2d         | true        |
```

- The table is passed **whole and unchanged** to the inner step, which reads it exactly as if it
  had been written there: a `UatTable`, a record (`Field | Value`), a list of records, or a list
  of values. Its type is inferred the same way as a value's: the inner parameter
  `IReadOnlyList<ServerTodoRow> todos` makes the call's table a list of `ServerTodoRow`, so an
  unknown column or a bad date in a row is reported at that row of the call, before anything
  runs.
- `<table>` can be passed to more than one inner step. The table must then fit each of them.
- A callable that uses `<table>` needs a table under every call (`UATA007` at the call). A call
  with a table to a callable that has no `<table>` is `UATB004`, as for any phrase that takes no
  table.
- `<doc string>` does the same for a `"""` block under the call.
- The heading doesn't name the table, as a C# phrase doesn't name its table parameter. In a
  `Parameters` table it is the row `table` (type `table`, or `list of ServerTodoRow` to declare
  it).
- The body can also **build** a table from parameters: placeholders in an inner table's cells are
  substituted, as in an outline. That is how a callable turns short values in the call into a
  table for a C# phrase.

What is not allowed, because it would be a loop or a variable:

| Not allowed | Why | Instead |
| --- | --- | --- |
| using one row or one cell of the call's table (`<table.Title>`, `<table[1]>`) | picking from data is a lookup | pass the table whole to a C# phrase that takes the list |
| adding or removing rows or columns before passing it on | a transformation | write the table the inner step needs in the call |

Running the body once per row is allowed, but only as its own kind of callable: see the next
section.

## Per-row callables: the body once per row

A callable whose heading says `Phrase for each row` runs its body once for each row of the
call's table. It is a `Scenario Outline` inside a scenario: the Examples table moves to the call,
and repeats a few steps instead of the whole scenario. FitNesse's decision tables work this way,
and they are the one form of repetition that has stayed readable.

````markdown
## Phrase for each row: projects with unbilled hours for {user}

| Parameter | Type   | Example   |
| --------- | ------ | --------- |
| name      | text   | Riverside |
| hours     | number | 8         |

```gherkin
Given <user> is on the Projects page
When <user> creates a project <name>
And <user> logs <hours> hours on <name>
Then <user> should be on the Projects page
```
````

```gherkin
Given projects with unbilled hours for Eva
  | Name      | Hours |
  | --------- | ----- |
  | Riverside | 8     |
  | Harbour   | 12.5  |
```

- **Two kinds of parameter.** A `{x}` in the heading is written once in the call and is the same
  for every row (`{user}` above). Every other `<x>` in the body is a column of the call's table,
  one value per row. Columns match parameters as record columns match members, without case or
  spaces (`Due Date` is `due`). A column named like a heading placeholder is an error.
- **Typed like any parameter.** A column's type is inferred from where `<x>` goes, or declared in
  the `Parameters` table. Every cell is read with `UatValues` and a bad cell is reported at its row
  of the call. An unknown column is an error; so is a missing one.
- **Expanded at bind time.** Two rows are two copies of the body, each bound and checked before
  anything runs.
- **Each row is a call in the trace**, with its inner steps under it:

  ```text
  Given projects with unbilled hours for Eva
    row 1 (Riverside, 8)
      Given Eva is on the Projects page   passed
      ...
    row 2 (Harbour, 12.5)
      When Eva creates a project Harbour  failed
  ```

- **A row can carry its expected result.** The body can end in `Then` steps that use the row's
  values, so one table states the inputs and what should follow from them:

  ```gherkin
  When I open the todo <title>
  Then the due date should be <due>
  And I go back to the list
  ```

- **Each row ends on the page it started on.** Otherwise row 2 starts where row 1 left off, and
  the rows depend on their order. The body either names no page, or its last step names the page
  its first step names. A per-row callable that ends elsewhere is broken (see [Pages](#pages-a-callable-may-end-somewhere-else)).
- **The call's table is the rows**, so the body has no `<table>` to pass on. An inner step can
  still have a table written in the body, with `<x>` in its cells.
- **A call needs a table** with at least one row (`UATA007` at the call otherwise).

Why this is not a loop:

| Allowed | Still not allowed |
| --- | --- |
| rows written in the scenario | a count that comes from the screen or from a step's result |
| rows known at bind time | a row index or counter (`<row>`, `<i>`) |
| every row runs the same steps | skipping, breaking or continuing (`only if`, `until`) |
| the first failing row stops the scenario, as any failing step does | carrying on after a failure (that is `Try`) |
| each row a separate call in the trace | a cell holding a table, so no nesting through data |

Ruled out: the same thing on one step without a callable (`<Name>` in the step text and a table
under it). It is lighter, but `<Name>` already means an Examples column in a `Scenario Outline`,
and nothing on the step line tells a reader that the step repeats. The `for each row` heading
says so where the phrase is declared, and the call reads like any step that takes a table.

## Pages: a callable may end somewhere else

A callable may end on a different page than it started on. Which page the caller is on after the
call is **not defined here**: it is part of static page tracking, in the scopes doc's
[page table](../phrases/phrase-scopes-and-composition.md#the-hard-part-knowing-the-current-page-at-bind-time),
which lists a call as one source of the page after a step. This doc only adds what is specific
to callables:

- **It is bound where it is called.** The call is expanded inside the binder's walk over the
  scenario's steps, not as a text pass before binding, so its inner steps are bound in the
  caller's context, as if they had been written there. The same callable can bind to different
  page phrases from two call sites; each call is checked on its own. This is also why checking at
  every call, above, is the real check: binding the body alone only sees the page-independent
  steps. It is what lets page tracking follow a call with no change to callables.
- **It says where it ends.** A callable whose steps name a page (`I am on ...`,
  `I should be on ...`) must end with one. Otherwise it is broken: "ends on a page it does not
  say".
- **A per-row body ends where it started**: if it names a page, its last step names the page its
  first step names.
- The start page is not declared. A callable that must start on a page begins with
  `Given I am on ...`, as the example does.

The last three look only at which phrases the steps bound to, so they are checked from the start,
before page tracking exists. Callables written now will not break when it lands.

## The danger, and where to draw the line

This is where test languages go wrong. Robot Framework started with keywords (exactly this) and
grew `IF`, `FOR`, `WHILE`, `TRY` and variables, until test suites became programs that nobody
could read and that nobody tested. FitNesse and Cucumber's step-calling-steps went the same way.
Once the scenario language has control flow, it needs a debugger, and testers become
programmers in a worse language than C#.

So the rule has to be **structural, not advisory:**

| Allowed in a callable phrase | Not allowed, ever |
| --- | --- |
| a fixed sequence of steps | `If`, `Else`, `While`, `For`, `Try` |
| parameters substituted into step text, table cells and doc strings | variables assigned by steps, or return values |
| the call's table or doc string passed through whole | picking rows or cells from it |
| the body once per row of the call's table, in a `Phrase for each row` | a count, index, skip or break |
| calling other phrases, including callable ones | recursion (a cycle is a bind-time error) |
| ending on another page, when the last step says which | branching on what the screen shows |

What covers the cases people reach for control flow for:

- **"Do this for each of these values"**: that's a `Scenario Outline` with `### Examples` for
  whole scenarios, or a `Phrase for each row` for a few steps. Both are data, not a loop.
- **"If the dialog appears, close it"**: that means the test doesn't know the app's state, which
  is a test-data problem (the scenario's `TestData`). Or it's a C# phrase that owns the
  uncertainty and names it: `I dismiss the dialog if it is shown`.
- **"Retry until it works"**: never in scenarios. Waiting is the controls' job (the AGENTS.md
  synchronisation rules).

The escape hatch is always **C#**: anything that needs a decision becomes a phrase class method
with a name that says what it decides. The markdown stays sequences.

Two more guardrails:

- **Depth limit** (say, 3). A callable that calls a callable that calls a callable is already
  hard to follow in a failure report.
- **Expanded in results.** The step trace must show the inner steps, indented under the call, so
  a failure points at the real step, not at "a project Riverside with 8 unbilled hours failed".

## Broken callables

As for C# phrases (AD-016), a callable that is declared wrongly does not stop the catalog. It is
marked, everything else loads, and only a call to it fails, naming the callable's file and line.
A callable is broken when:

- a `{x}` in the heading is never used in the body, or a `<x>` in the body is not in the heading;
- a parameter lands in arguments no single value can fit, or disagrees with its declared type;
- an inner step binds to nothing, or to a broken phrase;
- it calls itself, directly or through others, or nests deeper than the limit;
- it states a page but does not end on one;
- a `Parameters` example does not read as its type;
- per row: it uses `<table>`, a column is named like a heading placeholder, or it does not end on
  the page it started on.

The codes continue the `UATA` range; the numbers are assigned when this is built. `brinell-data
phrases` lists callables with their types, where they are declared, and the broken ones as
warnings, as it does for C# phrases.

## `## Background`

Stays. It is a callable phrase with no name and no parameters that runs first. It could be
re-implemented as one later; nothing in a scenario would change.

## Where it sits among the phrase layers

| Layer | Written by | Written in |
| --- | --- | --- |
| Control verbs, page phrases, subject phrases, module phrases | developers | C#: can contain logic |
| Callable phrases | **testers** | markdown: can't contain logic |

That split is the protection against a scenario language turning into a programming language.

## Implementation outline

Callables come before scopes: steps 1-5 and 7 need nothing from them. Step 6 waits for module
scope and step 8 for static page tracking; the combined order is in the
[scopes doc](../phrases/phrase-scopes-and-composition.md#proposed-order).

1. **File-local callables.** Parse `## Phrase:` (heading, optional `Parameters` table, one
   gherkin block, `<table>` / `<doc string>` markers). Register them in the catalog as phrases
   whose binding is an expansion. Expand inside the binder's walk over the steps (not as a text
   pass before it), substituting with the outline code; bind the inner steps; report at the
   call's line. Check the page rules that need no tracking.
2. **Types.** Infer from the body with placeholders left in; read the `Parameters` table; mark
   conflicts as broken.
3. **Tables and doc strings** passed through, checked against the inner step's parameter.
4. **Per-row callables.** `## Phrase for each row:`, columns as parameters, one expansion per row
   at bind time, cells checked at their rows, each row a call in the trace.
5. **Guardrails.** Cycle check, depth limit, expanded step trace under the call.
6. **Module `*.phrases.md` files**, once module scope exists. Global `Scenarios/*.phrases.md`
   files need no scope and come with step 1.
7. **Tool.** `phrases` lists callables (per-row ones with their columns); `validate` and `--fix`
   check calls and their rows.
8. **Pages**, with static page tracking (scopes doc): the tracker follows the page through the
   inner steps. Nothing here changes; the scopes doc's page table defines the result.

## Answered questions

| Question | Answer |
| --- | --- |
| Should callable phrases be allowed to end on a different page than they start? | Yes. The page after a call is defined by static page tracking in the scopes doc; here a callable that names a page must end on one ([Pages](#pages-a-callable-may-end-somewhere-else)). |
| Scopes first? | No. Callables are built first, expanded inside the binder so page tracking follows them later; module phrase files wait for module scope. |
| Parameter types? | Typed from the start: inferred from the inner steps a value goes to, and declared in an optional `Parameters` table ([Parameter types](#parameter-types)). |
| What happens to `## Background`? | It stays. |
| Can a call take a table? | Yes, passed whole to the inner steps with `<table>`; also doc strings ([Tables as arguments](#tables-as-arguments)). |
| Can the body run once per row? | Yes, as a `Phrase for each row`: expanded at bind time, no index, skip or break ([Per-row callables](#per-row-callables-the-body-once-per-row)). |

## Open questions

| Question | Notes |
| --- | --- |
| A call with many parameters | A long heading with five `{x}` is hard to read. A call could supply its values as a `Field | Value` table instead. That clashes with `<table>` (a step has one table), so not now. |
| `Parameters` table or a line in the heading? | The table has room for examples and is checked. A heading form (`{hours: number}`) was ruled out for C# phrases in AD-016. |
| Callables on a page scope | Can a callable belong to a page, like a page phrase? It would need page tracking first. |
