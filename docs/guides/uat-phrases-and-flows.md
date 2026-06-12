# UAT Phrases And Flows

Brinell UAT scenarios are markdown files whose steps bind to a command catalog.
There is no separate flow-file format. In current code, a flow is the ordered
execution of parsed and bound steps. Reusable domain flows are implemented as
custom UAT phrases on the fixture or on DI-backed phrase classes.

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

Catalog phrases use `{parameter}` placeholders:

```csharp
catalog.Register(
    UatEffectiveStepKeyword.When,
    "I enter {value} into {control}",
    "InputCommands.EnterText");
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

`UatReflectionRuntime.CreateCommandCatalog()` registers the built-in runtime
phrases. `UatSpecCommandCatalog.CreateDefault()` registers the equivalent
binding-only phrases for spec format tests.

| Keyword | Phrase | Runtime command |
| --- | --- | --- |
| `Given` | `I am on the {page} page` | Opens or asserts the page and sets the current page. |
| `Then` | `I should be on the {page} page` | Asserts the page and sets the current page. |
| `When` | `I tap {control}` | Calls `Click` on the current page control. |
| `When` | `I enter {value} into {control}` | Calls `Enter(value)`. |
| `When` | `I set {control} to {value}` | Calls `SetText(value)`. |
| `When` | `I clear {control}` | Calls `Clear()`. |
| `When` | `I check {control}` | Calls `Check()`. |
| `When` | `I uncheck {control}` | Calls `Uncheck()`. |
| `When` | `I select {value} from {control}` | Calls `SelectByText(value)`. |
| `Then` | `{control} should contain {value}` | Calls `AssertTextContains(value)`. |
| `Then` | `{control} should equal {value}` | Calls `AssertText(value)`. |
| `Then` | `{control} should be visible` | Calls `AssertVisible(true)`. |
| `Then` | `{control} should not be visible` | Calls `AssertVisible(false)`. |
| `Then` | `{control} should be enabled` | Calls `AssertEnabled(true)`. |
| `Then` | `{control} should be checked` | Calls `AssertChecked(true)`. |
| `Then` | `{control} should be unchecked` | Calls `AssertChecked(false)`. |
| `Then` | `{control} should have selected {value}` | Calls `AssertSelectedText(value)`. |
| `Then` | `I should see {text}` | Searches controls on the current page with `GetText()`. |

Control phrases need a current page. Start most UI flows with:

```markdown
Given I am on the Main page
```

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

`[UatAction]` is discovery metadata. It does not create a phrase by itself, and
the current runtime built-in phrases invoke known control method names such as
`Click`, `Enter`, `Clear`, and `AssertTextContains`.

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
- Use custom phrases for domain intent, cross-page actions, or behavior that
  needs fixture state, settings, or DI services.
- Keep the scenario readable as a user workflow.
- Keep locator details in page objects and controls.
- Prefer exact, unambiguous phrases over broad patterns.
- Avoid registering the same phrase for the same keyword in multiple places.
- Start control-heavy flows by opening or asserting the current page.
- Put hardware, live API, or environment-dependent paths behind tags and skip
  rules.

## Source Files

- `srcnew/Brinell.Uat/UatMarkdownParser.cs`
- `srcnew/Brinell.Uat/UatCommandCatalog.cs`
- `srcnew/Brinell.Uat/UatBinder.cs`
- `srcnew/Brinell.Uat/UatExecution.cs`
- `srcnew/Brinell.Uat/UatReflectionRuntime.cs`
- `srcnew/Brinell.Uat/UatScenarioTestBase.cs`
- `srcnew/Brinell.Uat/UatSpecCommandCatalog.cs`
- `srcnew/Brinell.Uat/UatAttributes.cs`
