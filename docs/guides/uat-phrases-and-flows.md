# UAT Phrases And Flows

Brinell UAT scenarios are markdown files whose steps bind to a command catalog.
There is no separate flow-file format. In current code, a flow is the ordered
execution of parsed and bound steps.

Every step is declared with one of three attributes from `Brinell.Core.Testing`:
`[UatGiven]`, `[UatWhen]` or `[UatThen]`. What the step becomes depends on the
method the attribute sits on:

| Origin | Declared on | Use it for |
| --- | --- | --- |
| Control verb | a control method | anything a control does: tap, set, assert |
| Root phrase | a fixture method | fixture state, navigation, setup |
| Phrase class | a method of a `[UatPhraseClass]` | domain flows needing DI services or scoped state |

## Mental Model

```text
.uat.md file
  -> UatMarkdownParser
  -> UatCommandCatalog
  -> UatBinder
  -> UatScenarioRunner / UatStepExecutionSession
```

The parser reads the scenario shape. The catalog owns available phrases. The
binder matches each step to exactly one phrase. The runner executes the bound
invocations in order and stops after a failed or canceled step.

## Step Text

The formal grammar of scenario and phrase files is in [UAT Scenario Grammar](../specs/uat-grammar.md).

Step lines must start with one of these exact keywords:

- `Given`
- `When`
- `Then`
- `And`
- `But`

`And` and `But` inherit the previous effective keyword. For example, after a
`When` step, `And I tap Save` is treated as a `When` step for phrase matching.
After a `Then` step, `And Status should be visible` is treated as a `Then`
step. So there is no `[UatAnd]` or `[UatBut]`: a negative check written after
`But` is declared with `[UatThen]`.

```markdown
Given I am on the User Form page
When I clear First Name
And I enter "Ada" into First Name
Then First Name should contain "Ada"
And Terms should be checked
```

## Phrase Patterns

Every phrase, however it is declared, uses `{parameter}` placeholders and matches
the same way:

```csharp
[UatWhen("I enter {value} into {control}")]
TScope Enter(string? text, int? timeoutMs = null);
```

This matches:

```markdown
When I enter "Ada" into First Name
```

The matched arguments are:

| Argument | Value |
| --- | --- |
| `value` | `Ada` |
| `control` | `First Name` |

Rules:

- Matching is anchored to the full step text.
- Phrase text is case-sensitive.
- Literal whitespace in the phrase matches one or more whitespace characters in
  the scenario.
- Placeholder names must start with a letter and may contain letters, digits,
  `_`, or `-`.
- Captured argument values are trimmed.
- Surrounding double quotes are removed from captured values.
- Use C#-friendly placeholder names when binding directly to method parameters.
- A placeholder's type is its method parameter's; see [Arguments](#arguments).

## Binding Rules

`UatBinder` accepts a step only when it matches exactly one command pattern.

Binding behavior:

- Only patterns with the step's effective keyword are considered.
- Exact phrases beat parameterized phrases.
- If multiple parameterized phrases match, the phrase with more literal
  non-whitespace characters wins.
- If more than one highest-priority phrase remains, binding fails as ambiguous.
- If no phrase matches, binding fails as unknown.
- A command can require a step table or reject step tables.

Common diagnostics:

| Code | Meaning |
| --- | --- |
| `UATB001` | No command binding matches the step. |
| `UATB002` | The step is ambiguous. |
| `UATB003` | The matched command requires a table. |
| `UATB004` | The matched command does not accept a table. |
| `UATA001`-`UATA007` | A value does not fit its parameter (see [Arguments](#arguments)). |
| `UATA010`-`UATA013` | The step binds to a broken phrase (see [Broken Phrases](#broken-phrases)). |
| `UATC001`-`UATC008` | The step calls a broken callable phrase (see [Callable Phrases](#callable-phrases)). |

## Arguments

A phrase says `{name}`; the method's parameter `name` gives the type. Every value a scenario
writes - on the step line, in an Examples cell, in a table cell - is read the same way, whatever
the machine's culture, and checked when the step binds (AD-016):

| Type | Written as | Examples |
| --- | --- | --- |
| `string` | as is; quotes optional, needed for `""`, spaces at the ends, or `, ` in a list item; `\"` inside quotes | `Buy milk`, `"Buy milk"` |
| `int`, `long` | digits, `,` between groups of three | `7`, `1,250` |
| `decimal`, `double` | `.` for decimals, `,` for thousands | `0.5`, `1,250.75` |
| `bool` | `true` or `false` | |
| enum | the member name, any case, spaces allowed | `Open`, `In Progress` |
| `DateOnly` | `dd-MMM-yyyy`, English month; relative to the fixture's clock | `12-May-2026`, `3-jan-2027`, `today`, `+2d` |
| `DateTime`, `DateTimeOffset` | `dd-MMM-yyyy HH:mm` | `12-May-2026 14:30` |
| `TimeOnly` / `TimeSpan` | `HH:mm` / `d h m s` | `09:15` / `1h 30m` |
| nullable | `null`, or an empty cell | |
| `UatUserRole` | a user or app name from the `Apps` table | `Eva` |
| arrays and lists | `, ` between items and a last `and` | `"A", "B" and "C"`, `1,250.5, 3 and 7` |
| a record, a list of records | the table under the step | see below |
| an app type | `[UatArgumentType]` with `static Parse(string)`, or an `IUatArgumentConverter<T>` in the fixture's scan | `1,250.00 EUR` |

`1,5`, `2026-05-12` and `12/05/2026` are refused, with the way to write them. A parameter the
phrase does not name comes from under the step:

```csharp
[UatGiven("the server also has todos")]
public void ServerAlsoHas(IReadOnlyList<ServerTodoRow> todos) { ... }   // a row per todo

[UatWhen("I create the todo")]
public void Create(TodoRow todo) { ... }                                 // a Field | Value table, or one row, or a JSON doc string

[UatThen("the todos should be open")]
public void TodosAreOpen(List<string> titles) { ... }                    // a one-column table

[UatWhen("I write the note")]
public void Write(string note) { ... }                                   // a """ doc string
```

```gherkin
When I create the todo
  | Field    | Value            |
  | -------- | ---------------- |
  | Title    | Write the report |
  | Due Date | 12-May-2026      |
And I write the note
  """
  Dear Eva,
  please buy milk.
  """
```

A step has one table or doc string, so a method takes at most one such parameter. Relative dates
resolve against `IUatClock.Now` when the fixture implements it (the Todo fixtures return their
test set's now), and against the current time otherwise.

## Phrases From Method Names

An empty keyword attribute makes the method name the phrase. Words are split on `_` and on case;
a word in capitals that is a parameter's name is its placeholder, `USER` is `{user}`, and other
words are lowercased (`I` stays):

| Method | Phrase |
| --- | --- |
| `[UatWhen] I_open_the_todo_TITLE(string title)` | When `I open the todo {title}` |
| `[UatWhen] When_I_open_the_todo_TITLE(string title)` | the same; a prefix equal to the attribute's keyword is dropped |
| `[UatThen] The_list_should_show_TITLES(string[] titles)` | Then `the list should show {titles}` |
| `[UatGiven] [UatWhen] The_device_is_offline()` | Given and When `the device is offline` |

In a phrase class a method with no attribute whose name starts with `Given`, `When` or `Then` is
inferred too; on the fixture only an attribute makes a phrase. A phrase with punctuation
(`read-only`) is written in text. A `{user}` form is always written in text, next to the empty
attribute (AD-015):

```csharp
[UatWhen]                                          // I open the todo {title}
[UatWhen("{user} opens the todo {title}")]
public void I_open_the_todo_TITLE(string title) { ... }
```

## Broken Phrases

A declaration that is wrong does not stop the catalog. The phrase is marked, everything else
loads, and a step that binds to it fails naming the method:

| Code | The phrase |
| --- | --- |
| `UATA010` | does not name a parameter that the step must supply |
| `UATA011` | has a method name that cannot be read (a parameter `i`, `TITLESTATUS` with no `_`) |
| `UATA012` | takes two parameters from under the step |
| `UATA013` | has two empty attributes of one keyword, a name prefix that is not its keyword, an empty attribute on a control verb, or a control-verb property (`CommandId`, `Literal`) on a phrase method |

`brinell-data phrases` lists the broken ones as warnings.

## Callable Phrases

A tester can give a sequence of steps a name and call it like a phrase, with no C#
([AD-017](../../.docs/decisions/ad-017-callable-phrases-are-sequences-not-programs.md)). It is
declared under a `## Phrase:` heading, in a scenario file (seen by that file) or in a
`Scenarios/*.phrases.md` file (seen by every scenario under that `Scenarios` folder):

````markdown
# Phrases: Todo

## Phrase: the synced list has todos

```gherkin
Given I am on the Todo List page
And the server also has todos
  <table>
When I tap Sync
Then I should be on the Todo List page
```
````

```gherkin
Given the synced list has todos
  | Title            | Status | Due Date |
  | ---------------- | ------ | -------- |
  | Post the letters | Open   | +2d      |
```

- **The heading** says `{name}`, as a C# phrase does; **the body** says `<name>`, as an outline
  does, and the call is expanded like an outline row. A call is matched for `Given`, `When` and
  `Then` alike. A callable and a C# phrase with the same text are ambiguous (`UATB002`): nothing
  shadows anything.
- **Typed by where the value goes.** In `## Phrase: a todo {title} due {due}` whose body says
  `When I create a todo <title> due <due>`, `<due>` fills a `DateOnly` parameter, so `{due}` is a
  date, and the call's value is read and checked as one at the call's line. A value inside a longer
  argument (`"Report <due>"`) is text, unless a `Parameters` table under the heading says
  otherwise:

  ```markdown
  | Parameter | Type | Example     |
  | --------- | ---- | ----------- |
  | due       | date | 12-May-2026 |
  ```

  Types are written as `brinell-data phrases` shows them: `text`, `whole number`, `number`,
  `true/false`, `date`, `date and time`, `time`, `duration`, `user`, `list of ...`, `table`, or
  the C# name of a type a phrase already takes (`TodoStatus`). A declared type must agree with
  the steps the value goes to.
- **Tables.** A line `<table>` under a body step passes the call's table there whole; the table
  is checked against that step's parameter, and a bad cell is reported at its row of the call.
  `<doc string>` does the same for a `"""` block. Placeholders in a table the body writes are
  filled in.
- **Once per row.** `## Phrase for each row:` runs the body once for each row of the call's
  table. `{x}` in the heading is written once in the call; every other `<x>` is a column:

  ````markdown
  ## Phrase for each row: each todo shows its status

  ```gherkin
  Then the todo <title> should show status <status>
  ```
  ````

  ```gherkin
  And each todo shows its status
    | Title            | Status      |
    | ---------------- | ----------- |
    | Post the letters | Open        |
    | Paint the fence  | In progress |
  ```

- **Where problems are reported.** Everything inside a call is bound where the call is, so a
  problem is reported at the call's line (a row's line, per row), with `via` lines to the body
  steps it came through. `--fix` rewrites the value in the call, never the body.
- **No logic, ever.** A callable is a fixed sequence: no `If`, loop, variable or return value.
  It may call other callables, three deep at most, and never itself (`UATC004`, `UATC005`).
  Anything that decides is a C# phrase whose name says what it decides.
- **Pages.** A callable whose steps name a page (`I am on`, `I should be on`) ends with one; a
  per-row body ends on the page it started on (`UATC006`).
- **The trace** shows each call once, its rows, and its steps indented under it.

A callable that is declared wrongly loads with the rest and fails only its calls, naming its file
and line:

| Code | The callable |
| --- | --- |
| `UATC001` | has a `{x}` its body does not use, or a `<x>` its heading does not name |
| `UATC002` | passes a value to steps that cannot agree on its type, or disagrees with its `Parameters` table |
| `UATC003` | has a `Parameters` row that names no parameter, an unknown type, or an example that does not read |
| `UATC004` | calls itself, directly or through others |
| `UATC005` | is called more than three deep |
| `UATC006` | names a page but does not end on one, or a row ends on another page than it started |
| `UATC007` | runs once per row and passes `<table>` on |
| `UATC008` | has a body step with a value that does not fit, or bound to a broken C# phrase |

What the text alone shows is a parse error: `<table>` outside a phrase section (`UAT024`), a
`## Phrase:` with no text (`UAT025`) or no steps (`UAT026`), a malformed `Parameters` table
(`UAT027`), and a phrase file with anything but phrase sections under one `# Phrases:` heading
(`UAT028`). Phrase files hold no prose, as scenario files do not.

A body step that binds to nothing is not a broken callable: it may bind where it is called (in
another app). The call reports it. `brinell-data validate` reports broken callables as warnings,
and `phrases` lists callables with their types.

## Built-In Phrases

The 18 built-in phrases have two sources. Three page verbs are registered
directly by `UatReflectionRuntime.CreateCommandCatalog()` and
`UatSpecCommandCatalog.RegisterDefault()`, since they do not bind to control
methods. The other fifteen are `[UatWhen]` / `[UatThen]` attributes on `Brinell.Core` control
interfaces. `UatCatalogBuilder` discovers them for the runtime catalog, the
spec catalog and the Presenter's preview, so the phrase and its method cannot
drift apart. To add a control verb, annotate its method rather than registering
another catalog entry. See
[AD-011](../../.docs/decisions/ad-011-uat-vocabulary-is-attribute-declared.md).

Three verbs are the engine's own. They bind to no control method, so they are
registered directly in the runtime and spec catalogs:

| Keyword | Phrase | What it does |
| --- | --- | --- |
| `Given` | `I am on the {page} page` | Opens or asserts the page and sets the current page. |
| `Then` | `I should be on the {page} page` | Asserts the page and sets the current page. |
| `Then` | `I should see {text}` | Searches controls on the current page with `GetText()`. |

A fourth is registered only when the fixture hosts several apps (`IUatSessionHost`):
`{app} uses the app`, as a `Given` or a `When`, makes an app of the config's `Apps` table current.
See [Several Apps In One Scenario](uat-template-guide.md#several-apps-in-one-scenario).

Everything else is discovered. The built-in set is 15 phrases over 13 methods on
six interfaces in `Brinell.Core/Interfaces/`:

| Keyword | Phrase | Declared on | Method called |
| --- | --- | --- | --- |
| `When` | `I tap {control}` | `IClickableControlObject` | `Click()` |
| `When` | `I enter {value} into {control}` | `IEditableTextControlObject` | `Enter(value)` |
| `When` | `I set {control} to {value}` | `IEditableTextControlObject` | `SetText(value)` |
| `When` | `I clear {control}` | `IEditableTextControlObject` | `Clear()` |
| `When` | `I check {control}` | `IToggleControlObject` | `Check()` |
| `When` | `I uncheck {control}` | `IToggleControlObject` | `Uncheck()` |
| `When` | `I select {value} from {control}` | `ISelectorControlObject` | `SelectByText(value)` |
| `Then` | `{control} should equal {value}` | `ITextControlObject` | `AssertText(value)` |
| `Then` | `{control} should contain {value}` | `ITextControlObject` | `AssertTextContains(value)` |
| `Then` | `{control} should have selected {value}` | `ISelectorControlObject` | `AssertSelectedText(value)` |
| `Then` | `{control} should be visible` | `IElementObject` | `AssertVisible(true)` |
| `Then` | `{control} should not be visible` | `IElementObject` | `AssertVisible(false)` |
| `Then` | `{control} should be enabled` | `IElementObject` | `AssertEnabled(true)` |
| `Then` | `{control} should be disabled` | `IElementObject` | `AssertEnabled(false)` |
| `Then` | `{control} should exist` | `IElementObject` | `AssertExists(true)` |
| `Then` | `{control} should not exist` | `IElementObject` | `AssertExists(false)` - for a MAUI element hidden with `IsVisible="False"`, which leaves the Windows UI Automation tree |
| `Then` | `{control} should be checked` | `IToggleControlObject` | `AssertChecked(true)` |
| `Then` | `{control} should be unchecked` | `IToggleControlObject` | `AssertChecked(false)` |

A control phrase reaches a method only if the named control actually implements
it. `I check Terms` on a `Label` fails at execution, not at binding.

Control phrases need a current page. Start most UI flows with:

```markdown
Given I am on the Main page
```

## Custom Control Verbs

To add a verb, put a `[UatWhen]` or `[UatThen]` on the control method that
performs it. The attribute lives in `Brinell.Core.Testing`, so a control project needs no
reference to `Brinell.Uat` to declare one. Do **not** add a
`catalog.Register(...)` call for a control verb - that is the hand-written table
AD-011 removed.

From the Todo sample's `TodoStatus` component:

```csharp
using Brinell.Core.Testing;

[UatWhen("I advance {control} to {value}")]
public TodoStatus<TScope> AdvanceTo(string? expected, int? timeoutMs = null) { ... }

[UatThen("{control} should show status {value}")]
public TodoStatus<TScope> AssertShowsStatus(
    string? expected, string? message = null, int? timeoutMs = null) { ... }
```

and from its `TodoList` collection:

```csharp
[UatThen("{control} should have todo {value}")]
public TodoList AssertRow(string title, bool present = true, int? timeoutMs = null) { ... }
```

which the scenarios then read as:

```markdown
When I advance Status to "Done"
Then Status should show status "Done"
And Todos should have todo "Plan the sprint"
```

Rules for a control verb:

- `{control}` is the control name, resolved on the current page. Exactly one
  other placeholder may appear; its captured value is passed as the method's
  first parameter. With no value placeholder and no literal, the method is called
  with no arguments, so its remaining parameters must be optional.
- For an on/off pair of phrases over one method, set `HasLiteral = true` and
  `Literal` to the boolean to pass, as `AssertVisible` does for
  `should be visible` / `should not be visible`.
- `CommandId` sets the stable command-id suffix; it defaults to the method name.
  The projections prepend their own prefix (`Builtin.` / `Spec.`), so one
  attribute serves both.
- `CommandId`, `Literal` and `HasLiteral` are for control verbs only. On a
  fixture or phrase-class method they fail discovery with an error naming the
  method.
- The attribute allows multiples: declare each phrase variant on the same method.
- A `(keyword, phrase)` pair is registered once. A verb inherited through several
  interfaces does not collide.

**Reach.** The Core interfaces are always scanned. An app control type is scanned
only when the runtime met it during page discovery - that is, when it is the type
of a control property on a discovered page object - together with its base classes,
so a verb declared on a Brinell base (`StateContainer`) reaches an app's subclass. A
step on a control no page exposes is never registered, and its steps fail to bind
with `UATB001`.

**Brinell's own control verbs**, beyond the Core interfaces:

| Control | Verbs |
| --- | --- |
| `ContentDialog` (MAUI) | `{control} should ask {value}` (the title), `I choose {value} in {control}` (a button), `no {control} should be shown` |
| `StateContainer` (CommunityToolkit) | `{control} should show the {value} state`; a subclass overrides `StateViewId` to map a readable name (`Loading`) to its view's AutomationId |

A page exposes them as it exposes any control: the Todo pages have a `Dialog` property,
and the list page names its state container `List`, so the scenarios read
`Then the dialog should ask "Delete todo?"` and `Then the list should show the Empty state`.

## Page And Control Names

Page and control names come from the runtime model, not from the markdown file.

Names can be explicit:

```csharp
[TestPage("Login")]
[UatName("Login")]
public sealed class LoginUatPage : PageObjectBase<LoginUatPage>
{
    [UatName("Role")]
    public ComboBox<LoginUatPage> RoleCombo => ComboBox("cmbRole");
}
```

Names can also be inferred from known suffixes:

| Code identifier | UAT name |
| --- | --- |
| `MainPage` | `Main` |
| `UserFormPage` | `User Form` |
| `FirstNameEntry` | `First Name` |
| `GreetButton` | `Greet` |
| `GreetingLabel` | `Greeting` |

Known suffixes include `Page`, `Button`, `Input`, `Entry`, `Field`,
`TextBox`, `CheckBox`, `Switch`, `Toggle`, `Picker`, `Dropdown`, `List`,
`Grid`, `Label`, `Display`, `Message`, `Text`, and `Control`. A property with no
known suffix is still a control when its type declares a control verb (`Dialog`, of
type `ContentDialog`).

A step may put "the" before a control name: `the dialog` finds `Dialog`. A control whose
name itself starts with "The" is matched first.

## Page Flow

Opening a page with the built-in page phrase:

1. Resolves the page by name.
2. Runs an optional fixture navigation method named `NavigateTo<PageName>`.
3. Gets the page instance from the fixture or the active DI scope.
4. Waits for readiness when the page exposes a supported readiness method.
5. Stores the current page in `UatExecutionContext`.

Readiness methods tried by the runtime:

- `WaitReady(10000)`
- `WaitLoaded(true, 10000)`
- `IsLoaded(10000)`

The first matching method that returns `bool` is used. If none exists, the page
is treated as ready.

## Scenario Flow

For each parsed scenario:

1. Background steps are prepended to the scenario's own steps.
2. Scenario outlines are expanded into one scenario per examples row.
3. Tags from immediate `@tag` lines are evaluated against configured skip rules.
   The metadata `Tags` row documents intent but is not used for skip decisions.
4. `UatScenarioTestBase` resolves scenario settings and attaches them to the
   execution context.
5. A `TestComposition` scope is created when the fixture exposes `Composition`
   or `TestComposition`.
6. Steps execute in order.
7. Execution stops after the first failed or canceled step.
8. A scenario result JSON file is written when the UAT scenario base records the
   result.

Presenter uses the same bound model, but drives it through
`UatStepExecutionSession` so a selected scenario can be stepped with `Next`.

## Custom Root Phrases

Fixture methods can register custom phrases with `[UatGiven]`, `[UatWhen]` or
`[UatThen]`.

```csharp
[UatGiven("I remember {name} as a trained player")]
public void RememberTrainedPlayer(string name)
{
    RememberedTrainedPlayer = name;
}

[UatThen("remembered trained player should be {name}")]
public void AssertRememberedTrainedPlayer(string name)
{
    Assert.Equal(name, RememberedTrainedPlayer);
}
```

Supported custom phrase method inputs include:

- phrase arguments whose names match `{parameter}` placeholders;
- `UatExecutionContext`;
- `UatStepInvocation`;
- `CancellationToken`;
- `TestSettings`;
- typed settings classes, including classes marked with
  `[TestSettingsSection]`.

Phrase argument values are read as the parameter's type; see [Arguments](#arguments).

Supported returns:

| Return | Result |
| --- | --- |
| `void` | Passed when no exception is thrown. |
| `Task` | Passed when the task completes. |
| `bool` | `false` fails the step; `true` passes. |
| `string` | Passed with the string as the step message. |
| `UatStepResult` | Uses the returned result. |
| `Task<UatStepResult>` | Uses the awaited result. |

Exceptions fail the step and are included in the step result.

`[UatAction]` is discovery metadata. It does not create a phrase by itself; a
control verb comes from `[UatWhen]` / `[UatThen]` on a control method (see [Custom Control Verbs](#custom-control-verbs)).

## Phrase Classes

Use phrase classes for reusable domain-level flows that need DI services or
per-scenario scoped state.

```csharp
[UatPhraseClass]
public sealed class CompositionPhrases : UatPhraseClassBase
{
    private readonly CompositionState _state;

    public CompositionPhrases(CompositionState state)
    {
        _state = state;
    }

    [UatWhen("I choose DI flow")]
    public void ChooseDiFlow()
    {
        _state.ActionCount++;
    }

    [UatThen("scoped action count should be {count}")]
    public void AssertScopedActionCount(int count)
    {
        Assert.Equal(count, _state.ActionCount);
    }
}
```

Phrase classes are discovered from the fixture's `TestComposition` scan. They
require an active DI scope at execution time. A class is treated as a phrase
class when it has `[UatPhraseClass]` or derives from `UatPhraseClassBase`.

Phrase-class methods use the same attributes. A phrase-class method with no
attribute whose name starts with `Given`, `When` or `Then` is a phrase too; see
[Phrases From Method Names](#phrases-from-method-names).

## Tables

Markdown step tables attach to the previous step:

```markdown
When I sign in with credentials
| Field | Value |
| --- | --- |
| User name | ada@example.com |
| Password | correct-password |
```

Manually registered command patterns can require or reject tables:

```csharp
catalog.Register(
    UatEffectiveStepKeyword.When,
    "I sign in with credentials",
    "Login.SignInWithCredentials",
    requiresTable: true);
```

Custom root phrases and phrase classes receive the table as a `UatTable`
parameter, or bound to records by a record or list parameter (see [Arguments](#arguments)).

Named `## Data: <name>` tables are parsed into the UAT document model. Current
binding and execution code does not consume them automatically.

## Several Users And Apps

When the config has an `Apps` table (see the template guide), a step can name who does it:
`When Daan taps Save` runs on Daan's app. It binds to a phrase **declared** with `{user}` first,
which a method carries as a second keyword attribute beside its `I` form:

```csharp
[UatWhen("I set {control} to {value}", CommandId = "Control.SetText")]
[UatWhen("{user} sets {control} to {value}", CommandId = "Control.SetText")]
void SetText(string text, int? timeoutMs = null);
```

The engine infers no verb forms: `Daan tap Save` binds to nothing, and so does any `I` phrase
that has no `{user}` form. The built-in ones all have one:

| `I` form | Named form |
| --- | --- |
| `I am on the {page} page` | `{user} is on the {page} page` |
| `I should be on the {page} page` | `{user} should be on the {page} page` |
| `I should see {text}` | `{user} should see {text}` |
| `I tap {control}` | `{user} taps {control}` |
| `I enter {value} into {control}` | `{user} enters {value} into {control}` |
| `I set {control} to {value}` | `{user} sets {control} to {value}` |
| `I clear {control}` | `{user} clears {control}` |
| `I select {value} from {control}` | `{user} selects {value} from {control}` |
| `I check {control}` / `I uncheck {control}` | `{user} checks {control}` / `{user} unchecks {control}` |

- **Declare the named form with the same `CommandId`** (and `Literal`, for a control verb), so
  both forms are one command. `{user}` never reaches a control method as its value; a phrase
  method receives it only when it has a `user` parameter.
- **The subject sticks.** The named app stays current, so `And I tap Sync` after `When Daan taps
  Save` runs on Daan's app. Still, name the user on every step of a scenario with several users.
- **Only names from the table count.** A name that is also a page or control name is an error
  (`UATB005`); a name the table does not have, or a `{user}` step in a suite without the table,
  is `UATB007`. A step that names a user and binds to nothing lists the declared `{user}` phrases.
- **A user with several apps** picks the one used last in the scenario; before any is used, name
  the app (`Daan's phone taps Sync`) or start with `Daan's phone uses the app`.
- **`{user}` elsewhere is only checked:** `the server should have received the todo {title} from
  {user}` receives the user's name and does not switch apps. Said as `I`, it is the current app's
  user.

The rule is [AD-015](../../.docs/decisions/ad-015-a-steps-subject-picks-its-app.md).

## Authoring Guidance

- Use built-in control phrases for simple interactions.
- When a verb belongs to a control, declare it there with `[UatWhen]` or `[UatThen]`. Reach for
  a fixture phrase only when no control owns the behavior - cross-page actions,
  fixture state, settings, or DI services.
- Keep the scenario readable as a user workflow.
- Keep locator details in page objects and controls.
- Prefer exact, unambiguous phrases over broad patterns.
- Avoid registering the same phrase for the same keyword in multiple places.
- Start control-heavy flows by opening or asserting the current page.
- Put hardware, live API, or environment-dependent paths behind tags and skip
  rules.

## Source Files

- `srcnew/Brinell.Core/Testing/UatPhraseAttribute.cs` and `UatKeywordAttributes.cs` — the step attributes
- `srcnew/Brinell.Core/Interfaces/*.cs` — the built-in control-verb declarations
- `srcnew/Brinell.Uat/UatCatalogBuilder.cs` — the discovery pass and both projections
- `srcnew/Brinell.Uat/UatMarkdownParser.cs`
- `srcnew/Brinell.Uat/UatCommandCatalog.cs`
- `srcnew/Brinell.Uat/UatBinder.cs` — binding, subjects, the current app, and expanding calls
- `srcnew/Brinell.Uat/UatCallables.cs`, `UatPreparedCallables.cs` — callable phrases, their types and problems
- `srcnew/Brinell.Uat/UatScenarioFiles.cs` — loading a scenario file with the phrase files it sees
- `srcnew/Brinell.Uat/UatCatalogSet.cs` — the catalogs per head, and what a step can see
- `srcnew/Brinell.Uat/UatValues.cs` — how every scenario value is read and written
- `srcnew/Brinell.Uat/UatPhraseDeclarations.cs` — a method's phrases, parameters and problems
- `srcnew/Brinell.Uat/UatArguments.cs` — the bind-time check and the run-time read
- `srcnew/Brinell.Uat/UatArgumentTypes.cs` — `UatUserRole`, `[UatArgumentType]`, converters
- `srcnew/Brinell.Uat/UatScenarioCheck.cs` — checking files without running them (`brinell-data`)
- `srcnew/Brinell.Uat/UatSessions.cs` — `IUatSessionHost`, `IUatAppsHost`
- `srcnew/Brinell.Uat/UatExecution.cs`
- `srcnew/Brinell.Uat/UatCatalogBuilder.cs`
- `srcnew/Brinell.Uat/UatReflectionRuntime.cs`
- `srcnew/Brinell.Uat/UatScenarioTestBase.cs`
- `srcnew/Brinell.Uat/UatSpecCommandCatalog.cs`
- `srcnew/Brinell.Uat/UatAttributes.cs`
