# UAT Template Guide

Brinell UAT tests execute markdown scenarios using a `uat.config.md` file and
one or more `.uat.md` scenario files. A small C# project ties them to a running
app.

A complete working example is
[`samples/Todo/tests/Brinell.Samples.Todo.Uat`](../../samples/Todo/tests/Brinell.Samples.Todo.Uat):
four scenarios against the hermetic Todo app on Windows, walked through step by
step in [One Todo UAT, end to end](../../samples/Todo/uat-walkthrough.md).

## Project Layout

```text
testsnew/Brinell.<Platform>.Uat.Tests/
  uat.config.md
  Scenarios/
    scenario-name.uat.md
  ExpectedFailures/
    known-failure.uat.md
  TestSettings/
    testsettings.json
  Runtime/
    <App>UatFixture.cs
    <App>UatCollection.cs
    <App>UatScenarioTests.cs
```

Put `uat.config.md`, `Scenarios/**/*.uat.md` and `TestSettings/**/*.json` in the
csproj as `CopyToOutputDirectory="PreserveNewest"`; the runtime reads them from
the output folder.

## The Test Project

Three small pieces. The **fixture** launches the app and exposes the page graph:

```csharp
[TestModuleScan(typeof(TodoListPage), NamespacePrefix = "Brinell.Samples.Todo.UITests.Pages")]
public sealed class TodoUatFixture : TodoAppFixture
{
    public TodoUatFixture()
    {
        Composition = TestComposition.ForFixture(this, services =>
            services.AddSingleton<IMauiTestContext>(Context));
    }

    public TestComposition Composition { get; }
}
```

`Composition` is what the runtime discovers pages from and constructs them
through. Without it, discovery falls back to page-typed properties on the
fixture itself.

A **collection** shares one app across every scenario class:

```csharp
[CollectionDefinition(CollectionName)]
public sealed class TodoUatCollection : ICollectionFixture<TodoUatFixture>
{
    public const string CollectionName = "Todo UAT";
}
```

The **test class** derives from `UatScenarioTestBase<TFixture>` and turns each
scenario file into a theory case:

```csharp
[Collection(TodoUatCollection.CollectionName)]
public sealed class TodoUatScenarioTests : UatScenarioTestBase<TodoUatFixture>
{
    public TodoUatScenarioTests(TodoUatFixture fixture) : base(fixture) { }

    public static IEnumerable<object[]> ScenarioFiles => GetScenarioFiles();

    [Theory(Timeout = 120000)]
    [MemberData(nameof(ScenarioFiles))]
    public Task UatFile_Passes(string filePath) => RunUatFileAsync(filePath);

    // Reset the app between scenarios so they stay order-independent.
    protected override void BeforeScenario(UatBoundScenario scenario) => Fixture.StartTest();

    protected override UatRuntimeValidationOptions RuntimeValidation { get; } =
        new(Target: "MAUI", Fixture: "FlaUI");
}
```

`RuntimeValidation` asserts the config at load time: a config whose `Target` or
`Fixture` disagrees, or that registers no assemblies, throws rather than running
scenarios against the wrong head. Override `BeforeScenario` to give each scenario
a clean start - scenarios share one app process.

## Config Shape

```markdown
# UAT Config

## Runtime

| Field | Value |
| --- | --- |
| Target | MAUI |
| Fixture | FlaUI |
| AppPath | ../../src/…App/bin/Debug/net10.0-windows10.0.19041.0/win10-x64/…App.exe |
| WorkingDirectory | ../../../.. |

## Assemblies

| Kind | Assembly |
| --- | --- |
| Pages | ../…UITests/bin/Debug/net10.0-windows/…UITests.dll |
| Controls | ../../../../srcnew/Brinell.Maui/bin/Debug/net10.0/Brinell.Maui.dll |
| Commands | ../../../../srcnew/Brinell.Uat/bin/Debug/net10.0/Brinell.Uat.dll |

## Discovery

| Field | Value |
| --- | --- |
| RequireExplicitUatAttributes | false |
| AllowNameInference | true |

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

What each section carries:

| Section | Field | Meaning |
| --- | --- | --- |
| Runtime | `Target` | The platform head, e.g. `MAUI`. Checked against `RuntimeValidation`. |
| Runtime | `Fixture` | The driver: `FlaUI` for the Windows head, `Appium` for Android. |
| Runtime | `AppPath` | The app to launch, relative to `WorkingDirectory`. |
| Runtime | `WorkingDirectory` | The base the relative paths resolve against. |
| Assemblies | `Pages` | The assembly holding the page objects - the source of control names. |
| Assemblies | `Controls` | The ControlObject assembly for the platform. |
| Assemblies | `Commands` | `Brinell.Uat`, which carries the engine's own verbs. |
| Discovery | `AllowNameInference` | `true` lets `AddButton` be **Add** without `[UatName]`. |
| Discovery | `RequireExplicitUatAttributes` | `true` limits discovery to members carrying UAT attributes. |

`Assemblies` is required by default: `UatRuntimeValidationOptions` fails a config
that registers none. Pass `RequireAssemblies: false` if a project drives the app
purely through its fixture's composition.

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

Scenario discovery scans `Scenarios/**/*.uat.md` recursively, so larger apps
can keep executable specs in module folders such as
`Scenarios/Contacts/00-smoke.uat.md` and
`Scenarios/Projects/01-list-basic.uat.md`.

## Metadata Fields

`UatMetadataFields` names the fields the format tests expect: `App`, `Area`,
`Target`, `Tags`, `Mode`, `Requires`, `Owner`, `Priority`, `Evidence`. The table
is a plain `Field`/`Value` table, so a project can add rows of its own. The Todo
sample adds `Journey`, which ties each scenario to a subtask id in its
[journey checklist](../../samples/Todo/tests/Brinell.Samples.Todo.Uat/todo-user-journeys.md):

```markdown
| Journey | TOD.02.3 |
```

Metadata is documentation and traceability. It does not drive execution - the
`@tag` line above `## Scenario:` is what skip rules read, not the `Tags` row.

For how step text binds to executable commands, see
[UAT Phrases And Flows](uat-phrases-and-flows.md).

## Step Tables

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
