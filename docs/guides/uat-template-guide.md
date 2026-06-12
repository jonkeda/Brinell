# UAT Template Guide

Brinell UAT tests execute markdown scenarios using a `uat.config.md` file and
one or more `.uat.md` scenario files.

## Project Layout

```text
testsnew/Brinell.<Platform>.Uat.Tests/
  uat.config.md
  Scenarios/
    scenario-name.uat.md
  ExpectedFailures/
    known-failure.uat.md
  TestSettings/
    settings.json
```

## Config Shape

```markdown
# UAT Config

## Runtime

| Field | Value |
| --- | --- |
| Target | MAUI |
| Fixture | Appium |

## Reporting

| Field | Value |
| --- | --- |
| OutputDirectory | $(BrinellTestResults)/uat |
| ScreenshotOnFailure | true |
| IncludeRuntimeTrace | true |

## Settings

| Field | Value |
| --- | --- |
| Root | TestSettings |
| DefaultFile | testsettings.json |
| LocalFile | testsettings.local.json |
| ScenarioConvention | scenarios/{ScenarioId}.json |

## Skip Rules

| Tag | EnvironmentVariable |
| --- | --- |
| hardware | BRINELL_UAT_HARDWARE |
| live-api | BRINELL_UAT_LIVE_API |
```

`$(BrinellTestResults)` resolves through the shared Brinell artifact provider.
If `OutputDirectory` is omitted, UAT should use the suite `uat/` folder.

## Scenario File Shape

```markdown
# UAT: MAUI Main Page Greeting

## Metadata

| Field | Value |
| --- | --- |
| App | Brinell.Samples.Maui.App |
| Area | Main Page |
| Target | MAUI |
| Tags | smoke, maui, greeting |
| Mode | Automated |
| Requires | Deterministic |
| Priority | Smoke |
| Evidence | none |

@smoke @maui @greeting @automated @deterministic
## Scenario: Greeting appears when a name is entered

Given I am on the Main page
When I clear Name
And I enter "Alice" into Name
And I tap Greet
Then Greeting should contain "Hello, Alice!"
And Greeting should be visible
And Name should be enabled
```

The scenario parser supports this document shape:

- The file must contain exactly one `# UAT: <title>` heading.
- `## Metadata` is optional for parsing, but the format tests expect `App`,
  `Area`, `Target`, `Tags`, `Mode`, `Requires`, `Priority`, and `Evidence`.
  The table columns must be `Field` and `Value`.
- `## Background` is optional and contains shared `Given`, `When`, `Then`,
  `And`, or `But` steps.
- `## Data: <name>` is optional and must contain a markdown table.
- Tags are written as `@tag` lines and must be immediately followed by
  `## Scenario:` or `## Scenario Outline:`.
- `## Scenario: <name>` contains one or more step lines. Numbered lists and
  generic `## Steps` / `## Expected` sections are not part of the parser
  format.
- `## Scenario Outline: <name>` expands one scenario per row in a required
  `### Examples` table. Step text and step table cells can reference example
  columns with `<columnName>` placeholders.

For how step text binds to executable commands, see
[UAT Phrases And Flows](uat-phrases-and-flows.md).

Scenario steps may include an immediate markdown table:

```markdown
## Scenario: Valid user can sign in

Given I am on the Login page
When I sign in with credentials
| Field | Value |
| --- | --- |
| User name | ada@example.com |
| Password | correct-password |
Then I should see "Welcome Ada"
```

Scenario outlines use `### Examples`:

```markdown
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

## Authoring Rules

- Use stable user-facing intent in steps.
- Keep locator names in commands or page objects, not prose.
- Put repeated command behavior in Brinell commands or controls.
- Keep expected failures in `ExpectedFailures/` with a clear reason.
- Keep generated output under `TestResults/`, not under source folders.

## Settings

UAT settings are resolved through `JsonTestSettingsProvider`.

Default lookup:

```text
TestSettings/
  testsettings.json
  testsettings.local.json
  scenarios/<ScenarioId>.json
```

`testsettings.local.json` is optional. Scenario settings are optional and are
selected from a scenario tag that starts with `uat-`, for example `@uat-login`.

Settings files must be JSON objects. The provider reads a top-level `settings`
object and supports an `include` array for required included files.

## Reporting

Each scenario writes a result JSON file to the configured reporting directory.
If `IncludeRuntimeTrace` is true, the report includes step traces, diagnostics,
discovery data, and command catalog data. Scenario result files are also
registered in the shared artifact manifest as `uat-scenario` artifacts.

See [Reporting And Artifacts](reporting-artifacts.md).

## Skip Rules

Skip rules connect scenario tags to environment variables. A scenario tagged
with `@hardware` is skipped unless its configured environment variable is
enabled with `1`, `true`, `yes`, or `on`.
