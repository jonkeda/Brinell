# Test Settings Storage Design

## Problem

Brinell and application UAT projects still rely on environment variables for
test capabilities, live-service toggles, hardware addresses, usernames, and
passwords. That causes three problems:

- The active test setup is invisible when reading the repo.
- Secret and non-secret settings are mixed together.
- Scenario-specific settings either end up in fixtures or in one large config
  file that every scenario has to understand.

Brinell should own a file-based test settings model that keeps ordinary settings
committed, keeps dangerous values out of git, and lets scenarios load only the
settings they need.

## Design Goals

- Do not use environment variables as the settings source.
- Use JSON or YAML files.
- Allow settings to be split by project, profile, area, and scenario.
- Allow local usernames, passwords, API keys, device addresses, and other
  dangerous values to live in gitignored files.
- Keep fixture classes small. Fixtures can consume resolved settings, but should
  not become the place where settings are stored or composed.
- Avoid one common huge config file. A scenario should be able to opt into a
  small area/scenario settings file.
- Make missing required settings fail with clear diagnostics before a scenario
  performs work.

## Non-Goals

- Do not replace app runtime settings. This design is for test/UAT settings.
- Do not put secrets in `uat.config.md`.
- Do not require every UAT project to define the same settings keys.
- Do not introduce another test runner just to pass settings.
- Do not depend on CI environment variables for secrets. CI may create files
  from its secret store, but Brinell reads the files.

## Recommended Format

Use JSON as the first supported format because .NET can parse it without another
dependency. Allow YAML later by extension:

```text
.json   supported first
.yaml   optional parser support
.yml    optional parser support
```

Both formats should map to the same settings tree. A project should not mix JSON
and YAML for the same layer unless there is a good reason.

## Folder Layout

Each test project owns a `TestSettings` folder beside `uat.config.md`:

```text
src/BodyCam.UAT/
  uat.config.md
  Scenarios/
  TestSettings/
    testsettings.json
    testsettings.local.json
    profiles/
      deterministic.json
      hardware.json
      live-api.json
    areas/
      camera-actions.json
      audio-routing.json
      settings-and-providers.json
    scenarios/
      uat-003-8.json
      uat-006-2.json
    secrets/
      bodycam.local.secrets.json
      bodycam.local.secrets.example.json
```

Committed files:

- `testsettings.json`
- `profiles/*.json`
- `areas/*.json`
- `scenarios/*.json` when they contain non-secret test data
- `*.example.json`

Gitignored files:

- `testsettings.local.json`
- `*.local.json`
- `*.secrets.json`
- `secrets/*` except `*.example.json`

Recommended `.gitignore` additions:

```gitignore
# Brinell test settings overlays and secrets
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

## File Composition

`TestSettings/testsettings.json` is the small committed root. It declares the
default profile and the files that apply to every scenario in the project:

```json
{
  "$schema": "https://brinell.local/schemas/testsettings.schema.json",
  "profile": "deterministic",
  "include": [
    "profiles/deterministic.json"
  ],
  "settings": {
    "capabilities": {
      "hardware": false,
      "liveApi": false,
      "manual": false
    },
    "uat": {
      "startupMode": "deterministic",
      "resetAppSettingsBeforeScenario": true
    }
  }
}
```

`testsettings.local.json` is optional and gitignored. It lets a developer choose
a local profile and add local overlays without changing committed files:

```json
{
  "profile": "hardware",
  "include": [
    "profiles/hardware.json",
    "secrets/bodycam.local.secrets.json"
  ]
}
```

The composition rule is simple:

1. Load `testsettings.json`.
2. Load files listed in `include`, in order.
3. Load `testsettings.local.json` if present.
4. Load files listed by the local file, in order.
5. Load area and scenario files for the scenario being executed.

Later files override earlier files. Object values merge by property. Arrays
replace by default.

## Secret Files

Secret files use the same shape as normal settings, but stay out of git:

```json
{
  "settings": {
    "hardware": {
      "a9Camera": {
        "host": "192.168.168.1",
        "username": "admin",
        "password": "replace-with-real-password"
      }
    },
    "providers": {
      "openAi": {
        "apiKey": "replace-with-real-key"
      }
    }
  }
}
```

The committed example file should show the required keys without real values:

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

Diagnostics must never print secret values. They may print the setting path,
for example `hardware.a9Camera.password`.

## UAT Config Integration

`uat.config.md` should identify where settings live, but should not contain the
settings themselves:

```markdown
## Settings

| Field | Value |
| --- | --- |
| Root | TestSettings |
| DefaultFile | testsettings.json |
| LocalFile | testsettings.local.json |
| ScenarioConvention | scenarios/{ScenarioId}.json |
```

This keeps `uat.config.md` focused on runtime, discovery, reporting, and
settings discovery. The actual settings remain split across JSON/YAML files.

## Replacing Environment Skip Rules

Current UAT configs use environment variables for skip rules:

```markdown
| Tag | EnvironmentVariable |
| --- | --- |
| hardware | BODYCAM_UAT_HARDWARE |
| live-api | BODYCAM_UAT_LIVE_API |
```

Replace that with settings-based rules:

```markdown
## Skip Rules

| Tag | Setting | EnabledWhen |
| --- | --- | --- |
| hardware | capabilities.hardware | true |
| live-api | capabilities.liveApi | true |
| manual | capabilities.manual | true |
```

If a scenario has `@hardware`, Brinell reads the resolved setting
`capabilities.hardware`. If it is not `true`, the scenario is skipped with a
message that names the setting path, not an environment variable.

For secrets, use `RequiredSetting` instead of treating the secret as a boolean:

```markdown
| Tag | RequiredSetting |
| --- | --- |
| live-api | providers.openAi.apiKey |
```

That lets the runtime skip or fail before the scenario starts without exposing
the key.

## Scenario Settings

Scenario settings should not live in fixtures. Brinell should resolve a
scenario-scoped settings object before the first step runs and place it in the
UAT execution context.

Recommended convention:

```text
TestSettings/scenarios/{ScenarioId}.json
```

For this scenario:

```markdown
@bodycam @camera @m50 @automated @deterministic @uat-003-8
## Scenario: UAT-003.8 Camera preview closes and captured still appears after capture settles
```

Brinell derives `uat-003-8` from the tag and attempts to load:

```text
TestSettings/scenarios/uat-003-8.json
```

If the file does not exist, that is fine. Most scenarios should not need their
own settings file.

Area settings can be loaded from document metadata:

```markdown
## Metadata

| Field | Value |
| --- | --- |
| Area | Camera Actions |
```

Brinell maps `Camera Actions` to:

```text
TestSettings/areas/camera-actions.json
```

This gives a scenario this effective stack:

```text
TestSettings/testsettings.json
TestSettings/profiles/deterministic.json
TestSettings/testsettings.local.json
TestSettings/areas/camera-actions.json
TestSettings/scenarios/uat-003-8.json
```

## Explicit Scenario Overrides

Convention should cover most cases, but some scenarios need explicit files. Add
an optional scenario-level settings directive later:

```markdown
### Settings

| File |
| --- |
| TestSettings/scenarios/live-api-transcript.json |
| TestSettings/secrets/bodycam.local.secrets.json |
```

This requires a UAT parser addition, so it should be a second slice. The first
slice can rely on scenario id convention and project/local includes.

## Runtime API Shape

Suggested Brinell Core API:

```csharp
public interface ITestSettingsProvider
{
    TestSettings Resolve(TestSettingsRequest request);
}

public sealed record TestSettingsRequest(
    string ProjectDirectory,
    string SettingsRoot,
    string? Area,
    string? ScenarioId,
    IReadOnlyList<string> ExplicitFiles);

public sealed class TestSettings
{
    public bool TryGetValue<T>(string path, out T? value);
    public T GetRequired<T>(string path);
    public TestSettingsSection GetSection(string path);
}
```

Suggested UAT integration:

```csharp
public static class UatExecutionContextSettingsExtensions
{
    public static TestSettings GetSettings(this UatExecutionContext context);
}
```

The scenario executor resolves settings once:

```text
parse UAT file
bind scenario
resolve scenario settings
store settings in UatExecutionContext.Items
evaluate skip rules against settings
run scenario steps
```

Custom phrase methods can either accept `UatExecutionContext` and call
`context.GetSettings()`, or Brinell can add direct parameter injection later:

```csharp
[UatPhrase(UatEffectiveStepKeyword.Given, "the A9 camera is available")]
public Task A9CameraIsAvailable(TestSettings settings)
{
    var host = settings.GetRequired<string>("hardware.a9Camera.host");
    var username = settings.GetRequired<string>("hardware.a9Camera.username");
    var password = settings.GetRequired<string>("hardware.a9Camera.password");
    ...
}
```

The fixture remains responsible for app/driver lifecycle. It does not own the
settings graph.

## Validation

Validation should happen before scenario execution:

- Missing included file: fail the test configuration.
- Missing optional convention file: ignore it.
- Missing required setting for an enabled scenario: skip or fail according to
  the skip rule.
- Malformed JSON/YAML: fail with file path and line/column if available.
- Secret-looking key in a committed file: warn or fail in a future analyzer.

Suggested required-setting table:

```markdown
## Required Settings

| Tag | Setting | Type |
| --- | --- | --- |
| hardware | hardware.a9Camera.host | string |
| hardware | hardware.a9Camera.username | string |
| hardware | hardware.a9Camera.password | secret |
| live-api | providers.openAi.apiKey | secret |
```

## BodyCam Migration Example

Current settings can move as follows:

| Current source | New file setting |
| --- | --- |
| `BODYCAM_UAT_HARDWARE` | `capabilities.hardware` |
| `BODYCAM_UAT_LIVE_API` | `capabilities.liveApi` |
| `BODYCAM_UAT_MANUAL` | `capabilities.manual` |
| `A9_CAMERA_IP` | `hardware.a9Camera.host` |
| `A9_CAMERA_USERNAME` | `hardware.a9Camera.username` |
| `A9_CAMERA_PASSWORD` | `hardware.a9Camera.password` |
| `BODYCAM_GROK_API_KEY` | `providers.grok.apiKey` |
| `XAI_API_KEY` | `providers.grok.apiKey` |

Example committed profile:

```json
{
  "settings": {
    "capabilities": {
      "hardware": true,
      "liveApi": false
    },
    "hardware": {
      "cameraProvider": "A9"
    }
  }
}
```

Example local secret overlay:

```json
{
  "settings": {
    "hardware": {
      "a9Camera": {
        "host": "192.168.168.1",
        "username": "admin",
        "password": "replace-with-real-password"
      }
    }
  }
}
```

## Implementation Plan

1. Add Brinell test settings primitives in `Brinell.Core`.
2. Add JSON loading, include expansion, path normalization, and object merge.
3. Add gitignore patterns and `*.example.json` guidance.
4. Add `## Settings`, settings-based skip rules, and required-setting parsing to
   `Brinell.Uat`.
5. Resolve settings per scenario in `UatScenarioTestBase` or
   `UatScenarioExecutor`, then expose them through `UatExecutionContext`.
6. Update reflection phrase invocation to optionally inject `TestSettings`.
7. Migrate BodyCam UAT skip rules and hardware/live API settings away from
   environment variables.
8. Add spec-format tests for settings files and skip-rule diagnostics.

## Open Questions

- Should YAML be in the first implementation slice or a later optional package?
  Recommendation: JSON first, YAML later.
- Should missing required settings skip or fail? Recommendation: skip when the
  rule is capability-gated, fail when the scenario explicitly requires a value.
- Should scenario id come from the first `@uat-*` tag or the scenario title?
  Recommendation: first `@uat-*` tag, with title fallback only for non-UAT
  scenarios.
- Should secret detection be a runtime warning or a repository analyzer?
  Recommendation: start with gitignore and examples, then add analyzer support.
