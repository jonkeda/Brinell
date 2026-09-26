# UAT Config

## Runtime

| Field | Value |
| --- | --- |
| Target | MAUI |
| Fixture | TodoUatFixture |
| Head | maui |
| AppPath | ../../src/Brinell.Samples.Todo.App/bin/Debug/net10.0-windows10.0.19041.0/win-x64/Brinell.Samples.Todo.App.exe |
| WorkingDirectory | ../../../.. |
| DefaultTestData | one-todo |

## Assemblies

| Kind | Assembly |
| --- | --- |
| Pages | bin/Debug/net10.0-windows/Brinell.Samples.Todo.Uat.dll |
| Controls | ../../../../srcnew/Brinell.Maui/bin/Debug/net10.0/Brinell.Maui.dll |
| Commands | ../../../../srcnew/Brinell.Uat/bin/Debug/net10.0/Brinell.Uat.dll |
| TestData | ../Brinell.Samples.Todo.TestSupport/bin/Debug/net10.0/Brinell.Samples.Todo.TestSupport.dll |

## Discovery

| Field | Value |
| --- | --- |
| RequireExplicitUatAttributes | false |
| AllowNameInference | true |

## Reporting

| Field | Value |
| --- | --- |
| ScreenshotOnFailure | true |
| IncludeRuntimeTrace | true |

## Settings

| Field | Value |
| --- | --- |
| Root | TestSettings |
| DefaultFile | testsettings.json |
| LocalFile | testsettings.local.json |
| ScenarioConvention | scenarios/{ScenarioId}.json |
