# UAT Phrases And Flows

Brinell UAT scenarios are markdown files whose steps bind to a command catalog.
There is no separate flow-file format. In current code, a flow is the ordered
execution of parsed and bound steps.

A phrase reaches the catalog in one of three ways:

| Origin | Declared with | Use it for |
| --- | --- | --- |
| Control verb | `[UatStep]` on the control method | anything a control does: tap, set, assert |
| Root phrase | `[UatPhrase]` on a fixture method | fixture state, navigation, setup |
| Phrase class | `[UatPhrase]` in a `[UatPhraseClass]` | domain flows needing DI services or scoped state |

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

Step lines must start with one of these exact keywords:

- `Given`
- `When`
- `Then`
- `And`
- `But`

`And` and `But` inherit the previous effective keyword. For example, after a
`When` step, `And I tap Save` is treated as a `When` step for phrase matching.
After a `Then` step, `And Status should be visible` is treated as a `Then`
step.

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
[UatStep(UatEffectiveStepKeyword.When, "I enter {value} into {control}")]
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

## Built-In Phrases

The 18 built-in phrases have two sources. Three page verbs are registered
directly by `UatReflectionRuntime.CreateCommandCatalog()` and
`UatSpecCommandCatalog.RegisterDefault()`, since they do not bind to control
methods. The other fifteen are `[UatStep]` attributes on `Brinell.Core` control
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

To add a verb, put a `[UatStep]` on the control method that performs it. The
attribute lives in `Brinell.Core.Testing`, so a control project needs no
reference to `Brinell.Uat` to declare one. Do **not** add a
`catalog.Register(...)` call for a control verb - that is the hand-written table
AD-011 removed.

From the Todo sample's `TodoStatus` component:

```csharp
using Brinell.Core.Testing;

[UatStep(UatEffectiveStepKeyword.When, "I advance {control} to {value}")]
public TodoStatus<TScope> AdvanceTo(string? expected, int? timeoutMs = null) { ... }

[UatStep(UatEffectiveStepKeyword.Then, "{control} should show status {value}")]
public TodoStatus<TScope> AssertShowsStatus(
    string? expected, string? message = null, int? timeoutMs = null) { ... }
```

and from its `TodoList` collection:

```csharp
[UatStep(UatEffectiveStepKeyword.Then, "{control} should have todo {value}")]
public TodoList AssertRow(string title, bool present = true, int? timeoutMs = null) { ... }
```

which the scenarios then read as:

```markdown
When I advance Status to "Done"
Then Status should show status "Done"
And Todos should have todo "Plan the sprint"
```

Rules for a `[UatStep]` phrase:

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
- The attribute allows multiples: declare each phrase variant on the same method.
- A `(keyword, phrase)` pair is registered once. A verb inherited through several
  interfaces does not collide.

**Reach.** The Core interfaces are always scanned. An app control type is scanned
only when the runtime met it during page discovery - that is, when it is the type
of a control property on a discovered page object. A `[UatStep]` on a control no
page exposes is never registered, and its steps fail to bind with `UATB001`.

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
`Grid`, `Label`, `Display`, `Message`, `Text`, and `Control`.

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

Fixture methods can register custom phrases with `[UatPhrase]`.

```csharp
[UatPhrase(UatEffectiveStepKeyword.Given, "I remember {name} as a trained player")]
public void RememberTrainedPlayer(string name)
{
    RememberedTrainedPlayer = name;
}

[UatPhrase(UatEffectiveStepKeyword.Then, "remembered trained player should be {name}")]
public void AssertRememberedTrainedPlayer(string name)
{
    Assert.Equal(name, RememberedTrainedPlayer);
}
```

Prefer the constructor that includes `UatEffectiveStepKeyword`. The constructor
that only takes a phrase registers the phrase for every effective keyword.

Supported custom phrase method inputs include:

- phrase arguments whose names match `{parameter}` placeholders;
- `UatExecutionContext`;
- `UatStepInvocation`;
- `CancellationToken`;
- `TestSettings`;
- typed settings classes, including classes marked with
  `[TestSettingsSection]`.

Phrase argument values are converted to the target parameter type. Current code
handles common scalar types such as `string`, `int`, `double`, `bool`, enums,
nullable versions of those types, and types supported by `Convert.ChangeType`.

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
control verb comes from `[UatStep]` (see [Custom Control Verbs](#custom-control-verbs)).

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

    [UatPhrase(UatEffectiveStepKeyword.When, "I choose DI flow")]
    public void ChooseDiFlow()
    {
        _state.ActionCount++;
    }

    [UatPhrase(UatEffectiveStepKeyword.Then, "scoped action count should be {count}")]
    public void AssertScopedActionCount(int count)
    {
        Assert.Equal(count, _state.ActionCount);
    }
}
```

Phrase classes are discovered from the fixture's `TestComposition` scan. They
require an active DI scope at execution time. A class is treated as a phrase
class when it has `[UatPhraseClass]` or derives from `UatPhraseClassBase`.

Phrase-class methods can use `[UatPhrase]`. If a phrase-class method has no
attribute, the runtime can infer a phrase from a method name starting with
`Given`, `When`, or `Then`, but explicit attributes are clearer and are required
when the phrase needs parameters.

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

Custom root phrases and phrase classes can read the table by accepting
`UatStepInvocation` and using `invocation.Table`.

Named `## Data: <name>` tables are parsed into the UAT document model. Current
binding and execution code does not consume them automatically.

## Authoring Guidance

- Use built-in control phrases for simple interactions.
- When a verb belongs to a control, declare it there with `[UatStep]`. Reach for
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

- `srcnew/Brinell.Core/Testing/UatStepAttribute.cs` — where built-in control phrases are declared
- `srcnew/Brinell.Core/Interfaces/*.cs` — the `[UatStep]` declarations themselves
- `srcnew/Brinell.Uat/UatCatalogBuilder.cs` — the discovery pass and both projections
- `srcnew/Brinell.Uat/UatMarkdownParser.cs`
- `srcnew/Brinell.Uat/UatCommandCatalog.cs`
- `srcnew/Brinell.Uat/UatBinder.cs`
- `srcnew/Brinell.Uat/UatExecution.cs`
- `srcnew/Brinell.Uat/UatCatalogBuilder.cs`
- `srcnew/Brinell.Uat/UatReflectionRuntime.cs`
- `srcnew/Brinell.Uat/UatScenarioTestBase.cs`
- `srcnew/Brinell.Uat/UatSpecCommandCatalog.cs`
- `srcnew/Brinell.Uat/UatAttributes.cs`
- `srcnew/Brinell.Core/Testing/UatStepAttribute.cs`
