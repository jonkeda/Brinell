# UAT Template Guide

Brinell UAT files use Markdown so product-level acceptance checks can be
reviewed without reading C# test code.

## Scenario Template

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
| Owner | QA |
| Priority | Smoke |
| Evidence | screenshot, transcript |

@smoke @maui @greeting @automated @deterministic @uat-001-1
## Scenario: UAT-001.1 Greeting appears when a name is entered

Given I am on the Main page
When I clear Name
And I enter "Alice" into Name
And I tap Greet
Then Greeting should contain "Hello, Alice!"
And Greeting should be visible
And Name should be enabled
```

Supported sections:

- `# UAT: ...`
- `## Metadata`
- optional `## Background`
- optional `## Data: ...`
- `## Scenario: ...`
- `## Scenario Outline: ...`
- `### Examples`
- tag lines immediately before a scenario
- `Given`, `When`, `Then`, `And`, `But`

## Standard Metadata

| Field | Purpose |
| --- | --- |
| App | Application under test |
| Area | Product area covered by the file |
| Target | Platform target, such as MAUI, WPF, HTML, or Stride |
| Tags | Human-readable file-level tags |
| Mode | Automated, Semi-automated, Manual, Hardware, or Live API |
| Requires | Deterministic, Hardware, Live API, HeyCyan, A9, or USB Camera |
| Owner | Person or team responsible for sign-off |
| Priority | Smoke, Critical, Normal, or Exploratory |
| Evidence | Expected evidence type: screenshot, transcript, log, or artifact |

Brinell accepts additional metadata fields. Keep new fields stable once reports
depend on them.

## Standard Tags

Common tags:

```text
@smoke @regression @manual @hardware @live-api
@maui @windows @android @ios
@deterministic @openai-live
@uat-003 @uat-003-6
```

Scenario IDs can be represented in the title, in a tag, or both:

```markdown
@uat-003-6
## Scenario: UAT-003.6 Sub-button hides action rows during capture
```

## Background Reset Pattern

Use `## Background` for deterministic setup shared by every scenario in a file:

```markdown
## Background

Given the app is running in deterministic UAT mode
And app settings are reset
And the transcript is empty
```

The phrases must bind to commands supplied by the project runtime.

## Data Tables

Use `## Data:` for named deterministic inputs:

```markdown
## Data: CameraFrames

| Name | Asset | Description |
| --- | --- | --- |
| Office | assets/camera/office.jpg | Indoor person-facing frame |
```

## Config Template

```markdown
# UAT Config

## Runtime

| Field | Value |
| --- | --- |
| Target | MAUI |
| Fixture | Appium |
| AppPath | ../../samples/App/bin/Debug/app.exe |
| WorkingDirectory | ../.. |

## Assemblies

| Kind | Assembly |
| --- | --- |
| Pages | ../App.UITests/bin/Debug/net10.0/App.UITests.dll |
| Commands | ../../srcnew/Brinell.Uat/bin/Debug/net10.0/Brinell.Uat.dll |

## Discovery

| Field | Value |
| --- | --- |
| RequireExplicitUatAttributes | false |
| AllowNameInference | true |

## Reporting

| Field | Value |
| --- | --- |
| OutputDirectory | artifacts/uat |
| ScreenshotOnFailure | true |
| IncludeRuntimeTrace | true |

## Skip Rules

| Tag | EnvironmentVariable |
| --- | --- |
| hardware | BRINELL_UAT_HARDWARE |
| live-api | BRINELL_UAT_LIVE_API |
```

`Reporting` and `Skip Rules` are optional. When a scenario has a tag listed in
`Skip Rules`, call `UatScenarioRunner.RunAsync(scenario, config)` to return a
skipped scenario result unless the mapped environment variable is enabled with
`1`, `true`, `yes`, or `on`. Bridges can also call `UatConfig.EvaluateSkip`
directly when they need to map the decision to framework-specific skip output.
