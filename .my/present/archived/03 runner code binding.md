# Runner Code Binding

This document defines how the UAT runner can know which code to execute for a parsed Markdown step.

The runner should not execute arbitrary text from the UAT file. It should parse each step, match it to a known command binding, resolve page objects and controls, then call typed Brinell code.

## Core Flow

```text
Markdown UAT
  -> Parse document
  -> Validate grammar
  -> Build scenarios and steps
  -> Match each step to a command binding
  -> Resolve page object and control references
  -> Execute the command handler
  -> Record trace, result, screenshot, and diagnostics
```

Example step:

```md
When I tap Sign in
```

Resolved execution:

```text
Step text:     I tap Sign in
Command:       TapControl
Page object:   LoginPage
Control:       SignInButton
Handler:       MauiInteractionCommands.Tap(...)
```

## Recommended Model

Use a Brinell UAT command catalog.

The catalog is an explicit list of phrases or phrase patterns that the runner is allowed to execute.

```text
Phrase pattern                         Handler
-------------------------------------  --------------------------------
I am on the {page} page                MauiPageCommands.AssertPageOpen
I tap {control}                        MauiInteractionCommands.Tap
I enter {value} into {control}         MauiInputCommands.EnterText
I should see {text}                    MauiAssertCommands.AssertTextVisible
I should see the {page} page           MauiPageCommands.AssertPageOpen
```

The UAT file stays readable:

```md
Given I am on the Login page
When I enter "ada@example.com" into User name
And I tap Sign in
Then I should see the Dashboard page
```

The runner calls real code:

```text
MauiPageCommands.AssertPageOpen("Login")
MauiInputCommands.EnterText("User name", "ada@example.com")
MauiInteractionCommands.Tap("Sign in")
MauiPageCommands.AssertPageOpen("Dashboard")
```

## Binding Types

The runner can support several binding types.

| Binding type | Purpose | Example |
| --- | --- | --- |
| Page binding | Resolves a page name to a Brinell page object. | `Login` -> `LoginPage` |
| Control binding | Resolves a control name to a control object on a page. | `Sign in` -> `LoginPage.SignInButton` |
| Command binding | Resolves step text to executable code. | `I tap {control}` -> `TapControl` |
| Table binding | Passes step tables to handlers as structured data. | `I enter credentials` + `Field/Value` table |
| Data binding | Resolves named data tables. | `"Premium customer"` -> `DataTable` |

## Option 1: Explicit Registry

The simplest safe model is a registry built in code.

```csharp
var catalog = new UatCommandCatalog();

catalog.Register("I tap {control}", MauiInteractionCommands.Tap);
catalog.Register("I enter {value} into {control}", MauiInputCommands.EnterText);
catalog.Register("I should see the {page} page", MauiPageCommands.AssertPageOpen);
```

Advantages:

- Very explicit.
- Easy to debug.
- No accidental exposure of helper methods.
- Good first version.

Tradeoff:

- Every new phrase must be registered manually.

## Option 2: Attribute Discovery

Command handlers can be marked with attributes and discovered at startup.

```csharp
[UatStep("I tap {control}")]
public Task TapAsync(UatExecutionContext context, string control)
{
    return context.CurrentPage.Controls.Resolve(control).TapAsync();
}

[UatStep("I should see the {page} page")]
public Task AssertPageOpenAsync(UatExecutionContext context, string page)
{
    return context.Pages.Resolve(page).AssertOpenAsync();
}
```

Advantages:

- Still explicit.
- Less central registration code.
- Easy to scan and list in the runner UI.

Tradeoff:

- Needs reflection or source generation.
- Attribute patterns must be validated for duplicates and ambiguity.

## Option 3: Page Object Reflection

The runner could inspect page objects and infer commands from public methods.

Example inference:

```text
LoginPage.SignInButton.TapAsync()
  -> "I tap Sign in"

DashboardPage.AssertOpenAsync()
  -> "I should see the Dashboard page"
```

Advantages:

- Fast to prototype.
- Low amount of binding code.

Tradeoff:

- Too easy to expose methods that were not meant as UAT commands.
- Method renames can break UAT files.
- Harder to produce friendly authoring errors.
- Ambiguous when several pages contain similarly named controls.

This can be useful internally, but should not be the main authoring contract.

## Option 4: Generated Catalog

A source generator or build-time tool can read page objects and attributes, then generate a command catalog.

```text
Page objects + [UatStep] attributes
  -> generated UatCommandCatalog.g.cs
  -> runner loads typed catalog
```

Advantages:

- Compile-time validation.
- Good diagnostics.
- Strong editor support later.

Tradeoff:

- More infrastructure.
- Better after the manual catalog proves the shape.

## Recommended First Implementation

Use a hybrid of explicit command catalog plus optional attribute discovery.

```text
Version 1
  Explicit command catalog
  Page registry
  Control registry
  Table values passed as structured data
  Diagnostics show resolved handler/page/control

Version 2
  Attribute discovery for command handlers
  Optional generated catalog
  Runner UI command browser
```

## Page Registry

The runner needs a page registry that maps UAT page names to Brinell page objects.

```text
Login              -> LoginPage
Dashboard          -> DashboardPage
Customers          -> CustomersPage
Customer details   -> CustomerDetailsPage
```

Possible code shape:

```csharp
pages.Register("Login", () => app.Pages.Get<LoginPage>());
pages.Register("Dashboard", () => app.Pages.Get<DashboardPage>());
pages.Register("Customers", () => app.Pages.Get<CustomersPage>());
```

The registry should expose one canonical UAT name per page in the first version.

## Control Registry

Controls should be resolved through the current page unless the step explicitly names another page.

```text
Current page: LoginPage
Control name: Sign in
Resolved: LoginPage.SignInButton
```

Possible code shape:

```csharp
controls.Register<LoginPage>("Sign in", page => page.SignInButton);
controls.Register<LoginPage>("User name", page => page.UserNameInput);
controls.Register<LoginPage>("Password", page => page.PasswordInput);
```

If the same control name exists on multiple pages, the current page context decides which one is used.

## Command Handler Shape

Handlers should receive a shared execution context and parsed arguments.

```csharp
public sealed class UatExecutionContext
{
    public UatScenarioRuntime Scenario { get; }
    public IBrinellAppSession App { get; }
    public IUatPageRegistry Pages { get; }
    public IUatControlRegistry Controls { get; }
    public IUatDiagnostics Diagnostics { get; }
}
```

Possible handler signature:

```csharp
public delegate Task<UatStepResult> UatCommandHandler(
    UatExecutionContext context,
    UatStepInvocation invocation,
    CancellationToken cancellationToken);
```

`UatStepInvocation` contains:

```text
OriginalText
EffectiveKeyword
Pattern
Arguments
Table
NamedData
SourceFile
LineNumber
```

## Step Matching

The runner should match steps in this order:

1. Normalize whitespace.
2. Substitute scenario outline parameters.
3. Find command patterns with the same effective keyword.
4. Match exact phrases first.
5. Match parameterized phrase patterns.
6. Reject unknown steps.
7. Reject ambiguous matches.
8. Validate required table shape.
9. Resolve pages and controls.
10. Create an executable step invocation.

Example:

```md
When I enter "ada@example.com" into User name
```

Pattern:

```text
I enter {value} into {control}
```

Invocation:

```text
value: ada@example.com
control: User name
```

## Table Binding

Step tables should be delivered to handlers as structured rows.

```md
When I enter credentials
| Field | Value |
| --- | --- |
| User name | ada@example.com |
| Password | correct-password |
```

Handler:

```text
Command: I enter credentials
Table:
  Field=User name, Value=ada@example.com
  Field=Password, Value=correct-password
```

The handler decides how to map fields to controls.

```text
User name -> LoginPage.UserNameInput
Password  -> LoginPage.PasswordInput
```

## Execution Context

The runner should track current page context.

```text
Given I am on the Login page
  -> asserts LoginPage is visible
  -> sets CurrentPage = LoginPage

When I tap Sign in
  -> resolves Sign in on CurrentPage
  -> calls LoginPage.SignInButton.TapAsync()

Then I should see the Dashboard page
  -> asserts DashboardPage is visible
  -> sets CurrentPage = DashboardPage
```

Page-changing commands should update the current page context after successful execution.

## Diagnostics

Every executed step should produce a trace.

```text
Step:          When I tap Sign in
Pattern:       I tap {control}
Handler:       MauiInteractionCommands.Tap
Page:          LoginPage
Control:       SignInButton
Started:       10:14:23.102
Completed:     10:14:23.411
Result:        Passed
Screenshot:    optional path
AutomationLog: optional path
```

Failures should include:

- Original step text.
- Source file and line number.
- Matched pattern.
- Handler name.
- Resolved page.
- Resolved control.
- Expected value.
- Actual value.
- Screenshot path when available.
- Automation tree or Brinell diagnostics when available.

## Safety Rules

- UAT Markdown cannot call arbitrary methods directly.
- Only registered command bindings are executable.
- Unknown steps fail validation before execution.
- Ambiguous steps fail validation before execution.
- Commands declare whether they require a table.
- Commands declare accepted table columns where possible.
- Reflection may discover bindings, but only explicit UAT bindings.
- The runner logs the resolved code path for every step.

## Minimal Command Catalog

The first MAUI runner can start with these commands:

| Keyword | Pattern | Handler |
| --- | --- | --- |
| Given | `the application is running` | `AppCommands.AssertRunning` |
| Given | `I am on the {page} page` | `PageCommands.AssertPageOpen` |
| When | `I tap {control}` | `InteractionCommands.Tap` |
| When | `I enter {value} into {control}` | `InputCommands.EnterText` |
| When | `I enter {formName}` | `InputCommands.EnterFormTable` |
| Then | `I should see the {page} page` | `PageCommands.AssertPageOpen` |
| Then | `I should see {text}` | `AssertCommands.AssertTextVisible` |
| Then | `I should see these {items}` | `AssertCommands.AssertTableVisible` |

## Minimal Binding Example

UAT:

```md
## Scenario: Valid user can sign in

Given I am on the Login page
When I enter credentials
| Field | Value |
| --- | --- |
| User name | ada@example.com |
| Password | correct-password |
And I tap Sign in
Then I should see the Dashboard page
```

Resolved plan:

```text
1. PageCommands.AssertPageOpen
   page = Login
   resolved page object = LoginPage

2. InputCommands.EnterFormTable
   formName = credentials
   table = Field/Value rows
   resolved controls = UserNameInput, PasswordInput

3. InteractionCommands.Tap
   control = Sign in
   resolved control = LoginPage.SignInButton

4. PageCommands.AssertPageOpen
   page = Dashboard
   resolved page object = DashboardPage
```

The runner knows which code to run because every step has been matched to a registered command binding before execution begins.
