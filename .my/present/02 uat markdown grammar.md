# UAT Markdown Grammar

## Document

```text
document          ::= suite-heading metadata-section? background-section? data-section* executable-section+
suite-heading     ::= h1-uat
metadata-section  ::= h2-metadata metadata-table
background-section ::= h2-background step-block
data-section      ::= h2-data markdown-table
executable-section ::= scenario-section | scenario-outline-section
```

## Headings

```text
h1-uat            ::= "# UAT:" text
h2-metadata       ::= "## Metadata"
h2-background     ::= "## Background"
h2-data           ::= "## Data:" text
h2-scenario       ::= "## Scenario:" text
h2-outline        ::= "## Scenario Outline:" text
h3-examples       ::= "### Examples"
```

`text` is non-empty heading text trimmed of leading and trailing whitespace.

## Metadata

```text
metadata-table    ::= markdown-table with columns "Field" and "Value"
metadata-row      ::= field-name value
field-name        ::= text
value             ::= text
```

Valid metadata fields:

```text
App
Area
Target
Adapter
Tags
```

Additional metadata fields are parsed as custom metadata.

## Tags

```text
tag-line          ::= tag+
tag               ::= "@" tag-name
tag-name          ::= letter (letter | digit | "-" | "_")*
```

Tag lines may appear immediately before `## Scenario:` or `## Scenario Outline:`.

Example:

```md
@smoke @maui
## Scenario: Valid user can sign in
```

## Background

```text
background-section ::= h2-background step-block
```

Background steps are prepended to each scenario in the same file.

## Scenario

```text
scenario-section  ::= tag-line* h2-scenario step-block
```

Example:

```md
## Scenario: Create a customer

Given I am on the Customers page
When I tap New Customer
And I enter customer details
| Field | Value |
| --- | --- |
| Name | Ada Lovelace |
| City | London |
Then I should see "Ada Lovelace" in the customer list
```

## Scenario Outline

```text
scenario-outline-section ::= tag-line* h2-outline step-block examples-section
examples-section         ::= h3-examples markdown-table
outline-parameter        ::= "<" parameter-name ">"
parameter-name           ::= letter (letter | digit | "-" | "_" | " ")*
```

Each row in the examples table creates one executable scenario.

Example:

```md
## Scenario Outline: Login result is shown

Given I am on the Login page
When I enter credentials
| Field | Value |
| --- | --- |
| User name | <user> |
| Password | <password> |
Then I should see "<result>"

### Examples

| user | password | result |
| --- | --- | --- |
| ada@example.com | correct-password | Dashboard |
| locked@example.com | correct-password | Account locked |
```

## Steps

```text
step-block        ::= step+
step              ::= step-line step-table?
step-line         ::= step-keyword whitespace step-text
step-keyword      ::= "Given" | "When" | "Then" | "And" | "But"
step-text         ::= non-empty text until line end
step-table        ::= markdown-table immediately following step-line
```

`And` and `But` inherit the nearest previous primary keyword from `Given`, `When`, or `Then`.

## Step Tables

```text
step-table        ::= markdown-table
markdown-table    ::= table-header table-separator table-row+
table-header      ::= "|" table-cell ("|" table-cell)* "|"
table-separator   ::= "|" table-separator-cell ("|" table-separator-cell)* "|"
table-row         ::= "|" table-cell ("|" table-cell)* "|"
table-cell        ::= text
```

A table immediately following a step belongs to that step.

Example:

```md
When I add order lines
| SKU | Quantity | Price |
| --- | ---: | ---: |
| PEN-001 | 2 | 1.50 |
| PAD-010 | 1 | 4.25 |
```

## Named Data Tables

```text
data-section      ::= h2-data markdown-table
h2-data           ::= "## Data:" data-name
data-name         ::= text
```

Example:

```md
## Data: Premium customer

| Field | Value |
| --- | --- |
| Name | Ada Lovelace |
| Email | ada@example.com |
| Customer type | Premium |
```

## Parameter Substitution

```text
outline-parameter ::= "<" parameter-name ">"
```

Substitution applies to:

- Step text.
- Step table cells.

Each `outline-parameter` must match a column name in the scenario outline `Examples` table.

## Complete Example

```md
# UAT: Login

## Metadata

| Field | Value |
| --- | --- |
| App | Example.Maui |
| Area | Authentication |
| Target | MAUI |
| Adapter | FlaUI |
| Tags | smoke, login |

## Background

Given the application is running

@smoke @maui
## Scenario: Valid user can sign in

Given I am on the Login page
When I enter credentials
| Field | Value |
| --- | --- |
| User name | ada@example.com |
| Password | correct-password |
And I tap Sign in
Then I should see the Dashboard page

## Scenario Outline: Login validation is shown

Given I am on the Login page
When I enter credentials
| Field | Value |
| --- | --- |
| User name | <user> |
| Password | <password> |
And I tap Sign in
Then I should see "<message>"

### Examples

| user | password | message |
| --- | --- | --- |
| locked@example.com | correct-password | Account locked |
| ada@example.com | wrong-password | Invalid credentials |
```

## Parser Output Model

```text
UatDocument
  Title: string
  Metadata: map<string, string>
  Background: Step[]
  DataTables: DataTable[]
  Scenarios: Scenario[]

Scenario
  Name: string
  Tags: string[]
  Steps: Step[]
  Examples: ExampleRow[]?

Step
  Keyword: Given | When | Then | And | But
  EffectiveKeyword: Given | When | Then
  Text: string
  Table: MarkdownTable?

MarkdownTable
  Columns: string[]
  Rows: MarkdownTableRow[]
```

## Validation

```text
document must contain exactly one h1-uat
document must contain at least one executable-section
metadata-section must contain a metadata-table
background-section must contain at least one step
data-section must contain one markdown-table
scenario-section must contain at least one step
scenario-outline-section must contain at least one step
scenario-outline-section must contain one examples-section
examples-section must contain one markdown-table
markdown-table rows must have the same column count as the header
outline parameters must match examples column names
step-table must be attached to exactly one step
tag-line must be immediately followed by a scenario heading
```
