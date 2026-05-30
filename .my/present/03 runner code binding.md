# Runner Code Binding

This document defines how the UAT runner maps parsed Markdown steps to executable Brinell code.

## Binding Goal

The runner executes only known UAT commands.

```text
Markdown step
  -> parsed step model
  -> command pattern match
  -> page/control resolution
  -> typed Brinell handler call
  -> result and diagnostics
```

UAT Markdown does not call arbitrary methods directly.

## Core Concepts

```text
UAT command
  A supported phrase pattern such as "I tap {control}".

Command handler
  Typed code that executes a matched command.

Page registry
  Maps canonical UAT page names to PageObject types.

Control registry
  Maps canonical UAT control names to ControlObject members on a page.

Capability
  A supported control action such as Tap, EnterText, Select, Check, or AssertValue.

Discovery config
  Runner configuration that tells the runner which assemblies to scan.
```

## First Command Set

The first runner should start with a small built-in command set.

| Keyword | Pattern | Required binding |
| --- | --- | --- |
| Given | `the application is running` | App session |
| Given | `I am on the {page} page` | PageObject |
| When | `I tap {control}` | Tappable ControlObject |
| When | `I enter {value} into {control}` | Text input ControlObject |
| When | `I enter {formName}` | Table-driven form handler |
| When | `I select {value} in {control}` | Selectable ControlObject |
| When | `I check {control}` | Checkable ControlObject |
| When | `I uncheck {control}` | Checkable ControlObject |
| Then | `I should see the {page} page` | PageObject |
| Then | `I should see {text}` | Text visibility assertion |
| Then | `I should see {value} in {control}` | Control value assertion |
| Then | `I should see these {items}` | Table assertion handler |

Projects can add more commands through explicit command bindings.

## Page And Control Names

The runner exposes one canonical UAT name per page and one canonical UAT name per control.

Aliases are not part of the first version.

Default names are inferred from type and member names:

```text
LoginPage              -> Login
CustomerDetailsPage    -> Customer Details
SignInButton           -> Sign In
UserNameInput          -> User Name
PasswordField          -> Password
RememberMeCheckbox     -> Remember Me
CountryPicker          -> Country
```

Suffixes stripped during name inference:

```text
Page
Button
Input
Field
TextBox
Checkbox
Toggle
Picker
Dropdown
List
Grid
Label
Text
```

`[UatName]` overrides the inferred name:

```csharp
public sealed class LoginPage : PageObject
{
    [UatName("Sign in")]
    public ButtonControl SubmitButton { get; }

    [UatName("User name")]
    public TextInputControl EmailInput { get; }
}
```

## Name Scope

Page names are global.

```text
Login       -> LoginPage
Customers   -> CustomersPage
```

Control names are scoped to a page.

```text
Login.Sign in       -> LoginPage.SubmitButton
Customers.Search    -> CustomersPage.SearchInput
```

Short control names resolve against the current page context.

```md
Given I am on the Login page
When I tap Sign in
```

If there is no current page, or if the control cannot be resolved on the current page, validation fails.

## Capability Binding

Generic commands bind only to controls with matching capabilities.

```text
I tap {control}
  requires Tap capability

I enter {value} into {control}
  requires EnterText capability

I select {value} in {control}
  requires Select capability

I check {control}
  requires Check capability

I should see {value} in {control}
  requires readable value or text assertion capability
```

Capabilities may be represented by interfaces, base classes, or method attributes.

## UAT Attributes

The first attribute set:

```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Method)]
public sealed class UatNameAttribute : Attribute
{
    public UatNameAttribute(string name) { }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class UatPhraseAttribute : Attribute
{
    public UatPhraseAttribute(string phrase) { }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class UatActionAttribute : Attribute
{
    public UatActionAttribute(string actionName) { }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
public sealed class UatIgnoreAttribute : Attribute
{
}
```

No `[UatAlias]` attribute is included in the first version.

## ControlObject Method Attributes

ControlObject methods can expose reusable capabilities.

```csharp
public sealed class ButtonControl : ControlObject
{
    [UatAction("tap")]
    [UatPhrase("I tap {control}")]
    public Task TapAsync(CancellationToken cancellationToken = default)
    {
        ...
    }
}
```

```csharp
public sealed class TextInputControl : ControlObject
{
    [UatAction("enter")]
    [UatPhrase("I enter {value} into {control}")]
    public Task EnterTextAsync(string value, CancellationToken cancellationToken = default)
    {
        ...
    }
}
```

The runner exposes a method only when a resolved control supports that method or capability.

## PageObject Method Attributes

PageObject methods can expose page-specific commands.

```csharp
[UatName("Login")]
public sealed class LoginPage : PageObject
{
    [UatPhrase("I sign in with credentials")]
    public Task SignInAsync(UatTable table, CancellationToken cancellationToken = default)
    {
        ...
    }

    [UatPhrase("I should see the Login page")]
    public Task AssertOpenAsync(CancellationToken cancellationToken = default)
    {
        ...
    }
}
```

Page-specific commands are used for actions that are richer than a single generic control operation.

## Custom Command Classes

Projects can define command classes for domain-specific actions.

```csharp
public sealed class CustomerUatCommands
{
    [UatPhrase("I create a customer")]
    public Task CreateCustomerAsync(
        UatExecutionContext context,
        UatStepInvocation invocation,
        CancellationToken cancellationToken)
    {
        ...
    }
}
```

Custom commands are discovered from configured command assemblies.

## Assembly Discovery

The runner discovers pages, controls, and commands from configured assemblies.

```text
Page assemblies
Control assemblies
Command assemblies
App bootstrap assembly
```

Discovery steps:

1. Load configured assemblies.
2. Find PageObject types.
3. Find public ControlObject properties on PageObjects.
4. Skip members marked `[UatIgnore]`.
5. Apply `[UatName]` when present.
6. Infer names when no `[UatName]` exists and name inference is enabled.
7. Find methods marked `[UatPhrase]` or `[UatAction]`.
8. Add built-in generic commands for known capabilities.
9. Add project-specific command handlers.
10. Validate page names, control names, command phrases, and table requirements.

## Configuration

Assembly registration should live outside ordinary UAT files.

The runner resolves configuration in this order:

```text
Runner defaults
  -> runner profile
  -> nearest uat.config.md from parent folder search
  -> UAT file metadata for labels and tags
```

The UAT file metadata should describe the test, not the code loading setup.

## `uat.config.md`

The runner looks for `uat.config.md` in the loaded UAT folder. If it is not found, it walks parent folders until a config file is found or the search root is reached.

Example:

```md
# UAT Config

## Runtime

| Field | Value |
| --- | --- |
| Target | MAUI |
| Adapter | FlaUI |
| AppBootstrap | Example.App.UITests.MauiAppBootstrap |

## Assemblies

| Kind | Assembly |
| --- | --- |
| Pages | Example.App.UITests.Pages |
| Controls | Example.App.UITests.Controls |
| Commands | Example.App.UITests.UatCommands |

## Discovery

| Field | Value |
| --- | --- |
| RequireExplicitUatAttributes | true |
| AllowNameInference | true |
```

## Step Matching

The runner matches steps before execution.

```text
1. Normalize whitespace.
2. Substitute scenario-outline parameters.
3. Select command patterns for the effective keyword.
4. Match exact phrases.
5. Match parameterized phrase patterns.
6. Bind arguments.
7. Bind the step table when present.
8. Resolve page and control names.
9. Validate required capabilities.
10. Create an executable step invocation.
```

Unknown, ambiguous, or invalid steps fail validation before execution begins.

## Step Invocation

Each executable step becomes a `UatStepInvocation`.

```text
UatStepInvocation
  SourceFile
  LineNumber
  OriginalText
  Keyword
  EffectiveKeyword
  MatchedPattern
  Arguments
  Table
  ResolvedPage
  ResolvedControl
  Handler
```

## Execution Context

Handlers receive an execution context.

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

Handler signature:

```csharp
public delegate Task<UatStepResult> UatCommandHandler(
    UatExecutionContext context,
    UatStepInvocation invocation,
    CancellationToken cancellationToken);
```

## Page Context

The runner tracks the current page.

```text
Given I am on the Login page
  -> assert LoginPage is visible
  -> set CurrentPage = LoginPage

When I tap Sign in
  -> resolve Sign in on LoginPage
  -> call SubmitButton.TapAsync()

Then I should see the Dashboard page
  -> assert DashboardPage is visible
  -> set CurrentPage = DashboardPage
```

Commands that navigate or assert a new page update the current page after success.

## Table Binding

Step tables are passed to handlers as structured data.

```md
When I enter credentials
| Field | Value |
| --- | --- |
| User name | ada@example.com |
| Password | correct-password |
```

The matched handler receives:

```text
Table columns: Field, Value
Rows:
  User name = ada@example.com
  Password = correct-password
```

Handlers declare accepted table shapes where possible.

## Diagnostics

Every executed step records a trace.

```text
Step:          When I tap Sign in
Pattern:       I tap {control}
Handler:       ButtonControl.TapAsync
Page:          LoginPage
Control:       SubmitButton
Started:       10:14:23.102
Completed:     10:14:23.411
Result:        Passed
Screenshot:    optional path
AutomationLog: optional path
```

Failure diagnostics include:

```text
Source file
Line number
Original step text
Matched pattern
Handler
Resolved page
Resolved control
Expected value
Actual value
Screenshot path
Automation tree path
Runtime log path
```

## Validation Rules

```text
Only registered command bindings are executable.
Raw method-call syntax in UAT Markdown is rejected.
Unknown steps fail validation.
Ambiguous steps fail validation.
Duplicate page names fail discovery.
Duplicate control names fail within the same page scope.
Duplicate command phrases fail discovery.
Control commands require matching capabilities.
Required step tables must be present.
Unexpected table shapes fail validation when the command declares a schema.
Configuration must identify at least one page assembly.
```

## Minimal Resolved Example

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
   page name: Login
   page object: LoginPage

2. InputCommands.EnterFormTable
   form name: credentials
   table: Field/Value
   page object: LoginPage
   controls: EmailInput, PasswordInput

3. ButtonControl.TapAsync
   control name: Sign in
   page object: LoginPage
   control object: SubmitButton

4. PageCommands.AssertPageOpen
   page name: Dashboard
   page object: DashboardPage
```

The runner knows which code to run because discovery builds a finite command catalog, and every parsed UAT step must match one catalog entry before execution.
