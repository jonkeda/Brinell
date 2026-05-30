# First UAT Markdown Grammar

This document defines a first Markdown grammar for Brinell UAT files.

The format is based on Gherkin because Gherkin already has a good human-readable shape for acceptance tests: features, scenarios, Given/When/Then steps, scenario outlines, examples, and data tables.

This grammar is not full Cucumber. It is a MAUI-first Brinell UAT format that uses Markdown headings and tables so the files stay pleasant to read and predictable to parse.

## Design Stance

- UAT files are Markdown documents first.
- Executable steps use Gherkin-style `Given`, `When`, `Then`, `And`, and `But`.
- Tables are first-class input and assertion data.
- Step text binds to a controlled Brinell command catalog.
- The Markdown should not expose raw page-object method calls as the normal authoring style.
- Version one should not support general-purpose `if` or `for` statements.
- Repetition should use scenario outlines, examples tables, and table-driven steps.
- Conditional behavior should be represented as separate scenarios, tags, preconditions, or explicit setup commands.

## Minimal Example

```md
# UAT: Login

## Metadata

| Field | Value |
| --- | --- |
| App | Example.Maui |
| Area | Authentication |
| Target | MAUI |
| Tags | smoke, login |

@smoke @maui
## Scenario: Valid user can sign in

Given I am on the Login page
When I enter credentials
| Field | Value |
| --- | --- |
| User name | ada@example.com |
| Password | correct horse battery staple |
And I tap Sign in
Then I should see the Dashboard page
```

## File Structure

```text
UAT file
  H1 suite heading
  optional Metadata section
  optional Background section
  zero or more named Data sections
  one or more Scenario or Scenario Outline sections
```

The recommended order is:

1. `# UAT: <suite name>`
2. `## Metadata`
3. `## Background`
4. `## Data: <data name>`
5. `## Scenario: <scenario name>`
6. `## Scenario Outline: <scenario name>`

## Grammar

```text
document          := suite-heading metadata? background? named-data* scenario+
suite-heading     := h1 text starting with "UAT:"
metadata          := h2 "Metadata" metadata-table
background        := h2 "Background" step-block
named-data        := h2 text starting with "Data:" markdown-table
scenario          := tag-line* h2 text starting with "Scenario:" step-block
scenario-outline  := tag-line* h2 text starting with "Scenario Outline:" step-block examples
examples          := h3 "Examples" markdown-table
step-block        := step+
step              := step-keyword step-text step-table?
step-keyword      := "Given" | "When" | "Then" | "And" | "But"
step-table        := markdown-table immediately following a step
tag-line          := one or more @tags before a scenario heading
```

`And` and `But` inherit the previous primary step kind. For example, `And I tap Save` after a `When` is treated as a `When` action step.

## Metadata

Metadata should use a two-column Markdown table.

```md
## Metadata

| Field | Value |
| --- | --- |
| App | Example.Maui |
| Area | Customers |
| Target | MAUI |
| Adapter | FlaUI |
| Tags | smoke, customers |
```

Metadata is for filtering, runner setup, diagnostics, and reporting. It should not be used to hide executable steps.

## Tags

Tags use Gherkin-style `@tag` lines before a scenario.

```md
@smoke @maui @customers
## Scenario: Create a customer
```

Tags can drive selection and environment decisions, such as smoke runs, MAUI-only runs, or tests that require authentication.

## Background

`Background` contains steps that run before every scenario in the file.

```md
## Background

Given the application is running
And I am signed in as "standard-user"
```

Use background for true shared setup only. If a setup step is not needed by every scenario, keep it inside the scenario.

## Scenarios

A scenario is the normal executable unit.

```md
## Scenario: Create a customer

Given I am on the Customers page
When I tap New Customer
And I enter customer details
| Field | Value |
| --- | --- |
| Name | Ada Lovelace |
| City | London |
And I tap Save
Then I should see "Ada Lovelace" in the customer list
```

Step text should describe user intent. The runner resolves the text to page-object commands.

## Scenario Outlines

Scenario outlines are the preferred replacement for simple `for` loops.

```md
## Scenario Outline: Login result is shown

Given I am on the Login page
When I enter credentials
| Field | Value |
| --- | --- |
| User name | <user> |
| Password | <password> |
And I tap Sign in
Then I should see "<result>"

### Examples

| user | password | result |
| --- | --- | --- |
| ada@example.com | correct-password | Dashboard |
| locked@example.com | correct-password | Account locked |
| ada@example.com | wrong-password | Invalid credentials |
```

Values in `<angle brackets>` are substituted from the `Examples` table into step text and step tables.

## Tables As Input

A Markdown table immediately under a step belongs to that step.

```md
When I enter customer details
| Field | Value |
| --- | --- |
| Name | Ada Lovelace |
| Email | ada@example.com |
| Customer type | Premium |
```

The binding for that step receives the table as structured data. The command decides how to map rows to controls.

Recommended table shapes:

- `Field` / `Value` for forms.
- `Column` / `Value` for expected detail fields.
- Domain-specific rows for repeatable inputs, such as order lines.

Example with repeated input rows:

```md
When I add order lines
| SKU | Quantity | Price |
| --- | ---: | ---: |
| PEN-001 | 2 | 1.50 |
| PAD-010 | 1 | 4.25 |
```

The step binding may iterate over the table internally. The Markdown grammar itself should not need a `for` statement.

## Tables As Assertions

Tables can also express expected state.

```md
Then I should see these customers
| Name | Status |
| --- | --- |
| Ada Lovelace | Active |
| Grace Hopper | Active |
```

The assertion command should define whether order matters. If order matters, the step wording should say so:

```md
Then I should see these customers in order
| Name | Status |
| --- | --- |
| Ada Lovelace | Active |
| Grace Hopper | Active |
```

## Named Data Tables

Named data tables are useful when several scenarios use the same input.

```md
## Data: Premium customer

| Field | Value |
| --- | --- |
| Name | Ada Lovelace |
| Email | ada@example.com |
| Customer type | Premium |

## Scenario: Create a premium customer

Given I am on the Customers page
When I create a customer using data "Premium customer"
Then I should see "Ada Lovelace" in the customer list
```

Named data should stay small. Large fixtures should live outside the UAT Markdown and be referenced by metadata or a step command.

## Function Calls And Methods

Short answer: the runner needs function calls internally, but the UAT Markdown should not look like code.

Avoid this as normal UAT syntax:

```md
When LoginPage.SignInButton.Tap()
Then DashboardPage.AssertOpen()
```

Prefer this:

```md
When I tap Sign in
Then I should see the Dashboard page
```

The step binding layer can resolve those lines to methods or command handlers:

```text
"I tap {control}" -> MauiInteractionCommands.Tap(control)
"I should see the {page} page" -> MauiPageCommands.AssertPageVisible(page)
```

This gives the runner real methods to call while keeping the UAT readable.

Reflection can help discover commands, but it should discover explicit UAT bindings rather than every public page-object method. A safer model is:

- Page objects expose normal typed APIs.
- A UAT command catalog exposes stable authoring phrases.
- Reflection can discover attributed commands or registered handlers.
- Diagnostics show the resolved page object, control, and method.

This avoids making UAT files brittle when page-object method names change.

## If Statements

Version one should not support general `if` statements.

Conditional logic inside UAT files tends to hide product behavior. If a scenario has two possible outcomes, write two scenarios.

Prefer:

```md
## Scenario: Standard user sees dashboard

Given I am signed in as "standard-user"
When I open the app
Then I should see the Dashboard page

## Scenario: Locked user sees locked message

Given I am signed in as "locked-user"
When I open the app
Then I should see "Account locked"
```

Avoid:

```md
If the user is locked
Then I should see "Account locked"
Else I should see the Dashboard page
```

Allowed alternatives for version one:

- Tags such as `@requires-auth` or `@requires-online`.
- Runner-level preconditions that skip a scenario when the environment is unsuitable.
- Explicit setup commands such as `Given I dismiss optional onboarding` when the condition is not the behavior under test.

## For Statements

Version one should not support general `for` statements.

Use scenario outlines when the same scenario should run for multiple examples. Use step tables when a single user action naturally operates on multiple rows.

Prefer scenario outline:

```md
## Scenario Outline: User role opens the correct landing page

Given I am signed in as "<role>"
When I open the app
Then I should see the "<page>" page

### Examples

| role | page |
| --- | --- |
| admin | Admin dashboard |
| manager | Team dashboard |
| worker | My tasks |
```

Prefer table-driven step:

```md
When I add order lines
| SKU | Quantity |
| --- | ---: |
| PEN-001 | 2 |
| PAD-010 | 1 |
```

Avoid:

```md
For each order line
  Add the line
End
```

The command handler may loop internally, but the Markdown should describe the user-level action.

## Step Resolution Rules

The runner should resolve each step in a predictable order:

1. Normalize whitespace.
2. Substitute scenario-outline parameters.
3. Match exact registered phrases.
4. Match registered phrase patterns with parameters.
5. Bind attached tables.
6. Validate the target page, control, and command.
7. Fail before execution if a step is unknown or ambiguous.

Unknown steps should be reported as validation errors, not skipped silently.

## Validation Rules

The parser should reject:

- A file without `# UAT:`.
- A file without at least one scenario.
- A scenario without steps.
- A scenario outline without `### Examples`.
- Duplicate example column names.
- Tables with missing header separators.
- Step tables that cannot be bound to the matched command.
- Unknown, ambiguous, or unsupported steps.

The parser may warn about:

- Very large tables.
- Background sections with many steps.
- Scenario names that are duplicated inside one file.
- Step text that appears to contain raw method calls.
- Conditional or loop-like wording.

## First Parser Scope

The first parser should support:

- `# UAT:`
- `## Metadata`
- `## Background`
- `## Data:`
- `## Scenario:`
- `## Scenario Outline:`
- `### Examples`
- Gherkin-style step lines.
- Gherkin-style tags.
- Markdown tables attached to steps.
- Parameter substitution with `<name>`.

The first parser should defer:

- Full Cucumber compatibility.
- Nested flows.
- General `if` statements.
- General `for` statements.
- Arbitrary method-call syntax.
- Large external data sources.

The important first milestone is a readable Markdown UAT that can be parsed, validated, bound to MAUI Brinell commands, and executed either automatically or step by step.
