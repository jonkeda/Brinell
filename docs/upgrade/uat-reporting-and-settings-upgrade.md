# Upgrade: UAT Reporting And Test Settings

This guide explains how to update a Brinell UAT project for the newer shared
reporting layout and file-based test settings support.

It is written as an implementation checklist for another coding agent working in
a different repository.

## What Changed

Brinell now has two shared conventions that UAT projects should use instead of
project-specific runtime code:

| Area | New owner | Main types |
| --- | --- | --- |
| Test artifacts and reports | `Brinell.Core.Artifacts` | `DefaultTestArtifactPathProvider`, `ITestArtifactPathProvider`, `TestArtifactManifestWriter` |
| UAT settings | `Brinell.Core.Settings` and `Brinell.Uat` | `TestSettings`, `JsonTestSettingsProvider`, `TestSettingsRequest`, `TestSettingsRootAttribute`, `TestSettingsSectionAttribute` |

The target shape is:

- Screenshots, logs, UAT JSON results, summaries, and manifests go under the
  shared `TestResults/<run-id>/suites/<suite>/...` layout.
- `uat.config.md` no longer needs a hard-coded UAT output directory.
- UAT projects define test settings in JSON files under `TestSettings/`.
- Local usernames, passwords, API keys, and device addresses live in gitignored
  `*.local.json` or `*.secrets.json` files.
- UAT phrase methods can receive typed settings objects as parameters.

## Reporting Upgrade

### Current Anti-Patterns

Search for project-specific report paths:

```powershell
rg "artifacts/uat|TestResults/Screenshots|TestResults/Logs|Reports|OutputDirectory|CaptureUatFailureScreenshot|BODYCAM_UAT_REPORTS|UAT_REPORTS|ARTIFACT" -n
```

Common things to remove or simplify:

- Hard-coded `artifacts/uat` in `uat.config.md`.
- Fixture-specific `CaptureUatFailureScreenshot(...)` methods.
- Project-specific screenshot or report directories.
- Ad hoc manifest/summary files owned by individual UAT projects.

### UAT Config

Preferred `## Reporting` section:

```markdown
## Reporting

| Field | Value |
| --- | --- |
| ScreenshotOnFailure | true |
| IncludeRuntimeTrace | true |
```

Do not set `OutputDirectory` unless the project has a good temporary reason.
When omitted, Brinell writes UAT reports to:

```text
TestResults/<run-id>/suites/<test-assembly>/uat
```

If a project needs an explicit path, use the Brinell token:

```markdown
| OutputDirectory | $(BrinellTestResults)/uat |
```

### Screenshot Capture

If the UAT test class inherits from `UatScenarioTestBase<TFixture>`, do not add a
project override just to capture failure screenshots. The base class now checks
whether the fixture is or exposes an `IScreenshotService`.

Remove code like this when it only delegates to the fixture:

```csharp
protected override string? CaptureEvidenceOnFailure(UatBoundScenario scenario) =>
    Fixture.CaptureUatFailureScreenshot(scenario.Source.Name);
```

Keep an override only when the project genuinely needs custom evidence logic.

### Artifact Publishing

CI should publish one run folder:

```text
TestResults/<run-id>
```

For local and CI runs, Brinell creates:

```text
TestResults/
  <run-id>/
    manifest.json
    summary.md
    suites/
      <test-assembly>/
        runner/
        logs/
        screenshots/
        uat/
        coverage/
        traces/
        videos/
        downloads/
        snapshots/
        attachments/
```

The reporting override environment variables are only for artifact location and
CI coordination:

| Variable | Purpose |
| --- | --- |
| `BRINELL_TEST_RESULTS_DIR` | Optional root override for test artifacts. |
| `BRINELL_TEST_RUN_ID` | Optional shared run id across test projects. |
| `BRINELL_TEST_SUITE` | Optional suite folder override. |

Do not use these variables for application or scenario settings.

Compatibility note: older local scripts may still set `BRINELL_ARTIFACT_ROOT`,
`BRINELL_ARTIFACT_RUN_ID`, or `BRINELL_ARTIFACT_SUITE`. Brinell currently honors
those names as fallback aliases, but new scripts and docs should use the
`BRINELL_TEST_*` names.

## Settings Upgrade

### Current Anti-Patterns

Search for environment-variable settings and fixture-held configuration:

```powershell
rg "Environment.GetEnvironmentVariable|SetEnvironmentVariable|USERNAME|PASSWORD|API_KEY|ApiKey|CameraIp|Host|Skip Rules|EnvironmentVariable" -n
```

Settings such as hardware flags, live API flags, camera hostnames, usernames,
passwords, API keys, and scenario-specific tuning should move to JSON files.

### UAT Config

Add a `## Settings` section to `uat.config.md`:

```markdown
## Settings

| Field | Value |
| --- | --- |
| Root | TestSettings |
| DefaultFile | testsettings.json |
| LocalFile | testsettings.local.json |
| ScenarioConvention | scenarios/{ScenarioId}.json |
```

This tells Brinell where to find settings. The settings themselves should not be
stored in `uat.config.md`.

### Project File

Copy settings files to the test output:

```xml
<ItemGroup>
  <None Include="TestSettings\**\*.json" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

### Gitignore

Add protection for local overlays and secrets:

```gitignore
**/TestSettings/testsettings.local.json
**/TestSettings/**/*.local.json
**/TestSettings/**/*.local.yaml
**/TestSettings/**/*.local.yml
**/TestSettings/**/*.secrets.json
**/TestSettings/**/*.secrets.yaml
**/TestSettings/**/*.secrets.yml
**/TestSettings/secrets/*
!**/TestSettings/secrets/*.example.json
```

### Settings Files

Create a committed default file:

```text
<uat-project>/TestSettings/testsettings.json
```

Example:

```json
{
  "$schema": "https://brinell.local/schemas/testsettings.schema.json",
  "settings": {
    "capabilities": {
      "hardware": false,
      "liveApi": false,
      "manual": false,
      "semiAutomated": false
    },
    "uat": {
      "startupMode": "deterministic",
      "resetAppSettingsBeforeScenario": true
    },
    "hardware": {
      "a9Camera": {
        "host": "",
        "username": "",
        "password": ""
      }
    }
  }
}
```

Create an example secret file:

```text
<uat-project>/TestSettings/secrets/project.local.secrets.example.json
```

Example:

```json
{
  "settings": {
    "hardware": {
      "a9Camera": {
        "host": "",
        "username": "",
        "password": ""
      }
    }
  }
}
```

Developers create the real ignored file locally:

```text
<uat-project>/TestSettings/secrets/project.local.secrets.json
```

They can include it from ignored `testsettings.local.json`:

```json
{
  "include": [
    "secrets/project.local.secrets.json"
  ],
  "settings": {
    "capabilities": {
      "hardware": true
    }
  }
}
```

### Typed Settings

Create project-level typed settings classes. Put them in the UAT project, not in
the fixture.

```csharp
using Brinell.Core.Settings;

[TestSettingsRoot]
public sealed class ProjectTestSettings
{
    public ProjectCapabilitySettings Capabilities { get; init; } = new();

    public ProjectHardwareSettings Hardware { get; init; } = new();
}

public sealed class ProjectCapabilitySettings
{
    public bool Hardware { get; init; }

    public bool LiveApi { get; init; }
}

public sealed class ProjectHardwareSettings
{
    public A9CameraSettings A9Camera { get; init; } = new();
}

[TestSettingsSection("hardware.a9Camera")]
public sealed class A9CameraSettings
{
    public string Host { get; init; } = string.Empty;

    public string Username { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;
}
```

Phrase methods can now request settings by type:

```csharp
[UatPhrase(UatEffectiveStepKeyword.Given, "the A9 camera is available")]
public Task A9CameraIsAvailable(A9CameraSettings camera)
{
    var host = camera.Host;
    var username = camera.Username;
    var password = camera.Password;
    ...
}
```

They can also request the root settings:

```csharp
public void AssertDeterministicMode(ProjectTestSettings settings)
{
    Assert.False(settings.Capabilities.LiveApi);
}
```

Or use the raw settings tree as an escape hatch:

```csharp
public void AssertHost(TestSettings settings)
{
    var host = settings.GetRequired<string>("hardware.a9Camera.host");
}
```

### Scenario-Specific Settings

Brinell derives a scenario id from the first `@uat-*` tag.

For this scenario:

```markdown
@smoke @hardware @uat-006-2
## Scenario: UAT-006.2 A9 or Vue990 can provide a frame
```

Brinell attempts to load:

```text
TestSettings/scenarios/uat-006-2.json
```

The file is optional. Use it when one scenario needs specific non-secret data.

Example:

```json
{
  "settings": {
    "hardware": {
      "a9Camera": {
        "host": "192.168.168.1"
      }
    }
  }
}
```

## Skip Rules

Important: settings-based skip rules are designed but not implemented yet.

The old table still works:

```markdown
## Skip Rules

| Tag | EnvironmentVariable |
| --- | --- |
| hardware | PROJECT_UAT_HARDWARE |
| live-api | PROJECT_UAT_LIVE_API |
```

Do not migrate this table to `Setting` / `EnabledWhen` until Brinell UAT has
implemented settings-based skip evaluation.

Use file settings for phrase behavior now. Treat skip-rule migration as a later
slice.

## Tests To Add Or Update

At minimum, add or update tests that prove:

- `uat.config.md` parses the new `## Settings` section.
- UAT spec-format tests bind all scenarios with the typed settings parameter.
- A phrase can receive `TestSettings`.
- A phrase can receive a `[TestSettingsRoot]` type.
- A phrase can receive a `[TestSettingsSection("...")]` type.
- Scenario convention files such as `TestSettings/scenarios/uat-123-4.json`
  override default settings.
- Failure screenshots and UAT result JSON are written under the Brinell artifact
  provider layout.

Useful commands:

```powershell
dotnet test <brinell-core-tests>.csproj --no-restore -p:UseSharedCompilation=false
dotnet test <brinell-uat-tests>.csproj --no-restore -p:UseSharedCompilation=false
dotnet build <uat-project>.csproj --no-restore -p:UseSharedCompilation=false
dotnet test <uat-project>.csproj --filter "Layer=SpecFormat" --no-build
```

Run the full Appium or UI-backed UAT suite only when the target app and driver
runtime are available.

## Migration Checklist

1. Search for hard-coded artifact/report paths and replace them with Brinell
   artifact provider defaults.
2. Remove UAT `OutputDirectory` from `uat.config.md` unless it is intentionally
   using `$(BrinellTestResults)`.
3. Remove screenshot-capture overrides that only delegate to the fixture.
4. Add the `## Settings` section to `uat.config.md`.
5. Add `TestSettings/testsettings.json`.
6. Add `TestSettings/secrets/*.example.json`.
7. Add gitignore patterns for local and secret settings overlays.
8. Include `TestSettings/**/*.json` in the UAT project output.
9. Add typed settings classes in the UAT project.
10. Replace phrase code that reads environment variables with typed settings
    parameters.
11. Keep legacy environment-variable skip rules for now.
12. Run framework tests, UAT spec-format tests, and a build of the UAT project.

## Common Failure Symptoms

| Symptom | Likely cause | Fix |
| --- | --- | --- |
| `Required test setting 'x.y' was not found.` | Missing setting in `testsettings.json`, local overlay, or scenario file. | Add the key or adjust the typed settings path. |
| Typed settings parameter is treated as a phrase argument. | Type is not named `*Settings` and has no settings attribute. | Add `[TestSettingsRoot]` or `[TestSettingsSection("...")]`. |
| Settings file not found in test output. | Project does not copy `TestSettings/**/*.json`. | Add the csproj `None Include` item. |
| Secret file appears in `git status`. | Gitignore pattern is missing or too narrow. | Add the `TestSettings` secret patterns above. |
| UAT JSON reports still go to `artifacts/uat`. | `OutputDirectory` is still hard-coded. | Remove it or use `$(BrinellTestResults)/uat`. |
| Failure screenshots are missing. | Fixture does not expose `IScreenshotService` and no custom capture override exists. | Expose `ScreenshotService` or implement a real override. |

## Not Implemented Yet

Do not ask another agent to rely on these until Brinell adds them:

- YAML settings parsing.
- Settings-based skip rules.
- Scenario-level Markdown `### Settings` directives.
- Secret-value redaction analyzer.
