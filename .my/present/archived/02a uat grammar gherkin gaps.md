# UAT Grammar Gherkin Gaps

This document lists Gherkin features that are not part of the first Brinell UAT Markdown grammar.

The goal is not to reject Gherkin. The goal is to keep the first Brinell UAT format small, Markdown-native, MAUI-first, and easy to bind to Brinell page objects.

Reference baseline: the Cucumber Gherkin reference describes primary keywords such as `Feature`, `Rule`, `Example`/`Scenario`, steps, `Background`, `Scenario Outline`, and `Examples`, plus secondary syntax such as doc strings, data tables, tags, comments, and localisation.

## Summary

| Gherkin feature | In first UAT grammar? | Reason |
| --- | --- | --- |
| `Feature:` keyword | No | Replaced by Markdown heading `# UAT:`. |
| `.feature` file shape | No | UAT files are normal Markdown. |
| `Rule:` keyword | No | Defer until business-rule grouping is needed. |
| `Example:` alias for `Scenario:` | No | Use one spelling: `## Scenario:`. |
| `Scenario Template:` alias | No | Use one spelling: `## Scenario Outline:`. |
| `Scenarios:` alias for `Examples:` | No | Use one spelling: `### Examples`. |
| Multiple `Examples` tables per outline | Not in v1 | Keep expansion and reporting simple first. |
| Tags on feature/rule/examples | Not in v1 | Start with scenario tags and metadata tags. |
| Tag inheritance | Not in v1 | Avoid hidden selection behavior in the first runner. |
| Free-form Gherkin descriptions | Not in v1 | Markdown prose is allowed in docs, but executable UAT parsing should stay strict. |
| Doc strings | Not in v1 | Tables cover the first MAUI input needs. |
| `*` step keyword | No | Use explicit `Given`, `When`, `Then`, `And`, `But`. |
| Gherkin comments with `#` | No | `#` is already Markdown heading syntax. |
| Localised Gherkin keywords | No | English keywords only in the first parser. |
| Cucumber step-definition behavior | No | Brinell uses its own command catalog and page-object binding. |
| Hook syntax and runtime tags | No | Runner setup should be explicit Brinell configuration. |

## `Feature:` Keyword

Gherkin files start with `Feature:`.

The Brinell UAT grammar uses:

```md
# UAT: Login
```

This keeps the file valid Markdown and makes the suite name visible as the document title.

Potential later support:

```md
Feature: Login
```

Only add this if importing existing `.feature` files becomes important.

## `.feature` File Shape

Gherkin is normally stored in `.feature` files.

The Brinell UAT grammar uses `.md` files because:

- Product owners and testers can read and edit them naturally.
- Markdown tables are familiar.
- The same file can contain light documentation around executable scenarios.
- The MAUI runner can preview the file without a special editor.

## `Rule:` Keyword

Gherkin supports `Rule:` to group scenarios under a business rule.

The first Brinell grammar does not include it. Use tags or scenario naming for now:

```md
@locked-account
## Scenario: Locked users cannot sign in
```

Possible later Markdown-native shape:

```md
## Rule: Locked users cannot sign in

### Scenario: Locked user sees locked message
```

This should wait until the runner needs rule-level reporting.

## Alias Keywords

Gherkin has several aliases:

- `Example:` can mean `Scenario:`.
- `Scenario Template:` can mean `Scenario Outline:`.
- `Scenarios:` can mean `Examples:`.

The first UAT grammar intentionally avoids aliases. There should be one authoring style:

```md
## Scenario: Create customer
## Scenario Outline: Login result is shown
### Examples
```

This makes parsing, validation, docs, and UI labels simpler.

## Multiple `Examples` Tables

Gherkin allows a scenario outline to have more than one `Examples` section.

The first Brinell grammar supports one `### Examples` table per scenario outline.

Reason:

- One table is easier to explain in the runner UI.
- One table makes generated scenario names predictable.
- Tags on separate examples groups are not supported yet anyway.

If needed later, support can be added as:

```md
### Examples: Mobile

| user | result |
| --- | --- |
| ada | Dashboard |

### Examples: Desktop

| user | result |
| --- | --- |
| ada | Dashboard |
```

## Tags And Tag Inheritance

Gherkin supports tags above `Feature`, `Rule`, `Scenario`, `Scenario Outline`, and `Examples`. Tags can be inherited by child elements.

The first Brinell grammar supports:

- Scenario tags with `@tag`.
- Suite-level metadata tags in the `## Metadata` table.

It does not support:

- Tags on `Feature`.
- Tags on `Rule`.
- Tags on `Examples`.
- Tag inheritance.

Reason: inherited tags are useful, but they can make a runner UI confusing at first because the visible scenario may not show all tags that affect selection.

Preferred v1 style:

```md
## Metadata

| Field | Value |
| --- | --- |
| Tags | maui, smoke |

@login @happy-path
## Scenario: Valid user can sign in
```

## Free-Form Descriptions

Gherkin allows free-form descriptions under `Feature`, `Scenario`, `Background`, `Scenario Outline`, and `Rule`.

The first Brinell grammar should be stricter. A UAT file may contain Markdown prose in non-executable sections, but executable parsing should only treat known sections and steps as runnable.

Reason:

- Strict parsing gives better validation errors.
- The runner can show exactly which lines are executable.
- Prose should not accidentally look like a step.

Possible later support:

```md
## Scenario: Valid user can sign in

This verifies the normal login path for an active user.

Given I am on the Login page
When I enter valid credentials
Then I should see the Dashboard page
```

If added, descriptions should be captured for reporting only.

## Doc Strings

Gherkin supports doc strings for passing large text blocks to a step.

The first Brinell grammar does not include doc strings.

Reason:

- The first MAUI cases are mostly page navigation, form entry, button actions, and assertions.
- Tables handle structured data better.
- Markdown code fences conflict visually with documentation examples inside Markdown files.

Use tables for v1:

```md
When I enter note details
| Field | Value |
| --- | --- |
| Title | Site visit |
| Body | Customer requested a follow-up call. |
```

Possible later support could use a named data block instead of raw Gherkin doc strings:

````md
## Data: Long note body

```text
Customer requested a follow-up call.
Please bring the installation report.
```
````

That needs careful Markdown parsing, so defer it.

## `*` Step Keyword

Gherkin allows `*` as a step keyword.

The first Brinell grammar does not support it. Use explicit step words:

```md
Given I am on the Login page
When I tap Sign in
Then I should see validation errors
```

Reason: explicit step words make the runner timeline clearer.

## Comments

Gherkin supports comments with `#`.

The Brinell UAT grammar does not use Gherkin comments because `#` already means Markdown heading.

If comments are needed, prefer normal Markdown prose outside executable sections, or possibly Markdown comments later:

```md
<!-- This scenario is waiting for the new login screen. -->
```

The first parser should ignore Markdown comments if they appear.

## Localisation

Gherkin supports localised keywords for many spoken languages.

The first Brinell UAT grammar supports English keywords only:

- `Given`
- `When`
- `Then`
- `And`
- `But`

Reason: binding, diagnostics, and examples are much simpler while the command catalog is still young.

## Cucumber Step Definitions

In Cucumber, steps match registered step definitions, typically regular expressions or Cucumber expressions, and those step definitions invoke code.

Brinell should use the same broad idea, but not the same runtime model.

The first Brinell UAT grammar should bind steps to a Brinell command catalog:

```text
"I tap {control}" -> MauiInteractionCommands.Tap(control)
"I should see the {page} page" -> MauiPageCommands.AssertPageVisible(page)
```

That catalog can call page-object methods internally. The Markdown should not expose raw method calls as its normal syntax.

## Hooks

Cucumber has runtime hook concepts such as before/after hooks, often filtered by tags.

The first UAT grammar does not include hook syntax.

Brinell setup should be represented through:

- Runner configuration.
- Metadata.
- Explicit setup steps.
- Brinell fixtures or app launch profiles.

Do not add hidden hook behavior to the Markdown until the runner has a clear diagnostics story for it.

## What We Keep From Gherkin

The first UAT grammar still keeps the most useful Gherkin ideas:

- Scenario-oriented acceptance tests.
- `Given`, `When`, `Then`, `And`, `But`.
- `Background`.
- `Scenario Outline`.
- `Examples`.
- Step-attached tables.
- Tags before scenarios.

The key difference is that Brinell UAT Markdown is stricter and more Markdown-native than full Gherkin. That is a good trade for the first MAUI runner.

## References

- Cucumber Gherkin Reference: https://cucumber.io/docs/gherkin/reference/
- Cucumber API Reference: https://cucumber.io/docs/cucumber/api/
