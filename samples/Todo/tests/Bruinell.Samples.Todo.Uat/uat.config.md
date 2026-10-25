# UAT Config

## Runtime

| Field | Value |
| --- | --- |
| Target | MAUI |
| Fixture | TodoUatFixture |
| Head | maui |
| AppPath | ../../src/Bruinell.Samples.Todo.App/bin/Debug/net10.0-windows10.0.19041.0/win-x64/Bruinell.Samples.Todo.App.exe |
| WorkingDirectory | ../../../.. |
| DefaultTestData | one-todo |

## Assemblies

| Kind | Assembly |
| --- | --- |
| Pages | bin/Debug/net10.0-windows/Bruinell.Samples.Todo.Uat.dll |
| Controls | ../../../../srcnew/Bruinell.Maui/bin/Debug/net10.0/Bruinell.Maui.dll |
| Commands | ../../../../srcnew/Bruinell.Uat/bin/Debug/net10.0/Bruinell.Uat.dll |
| TestData | ../Bruinell.Samples.Todo.TestSupport/bin/Debug/net10.0/Bruinell.Samples.Todo.TestSupport.dll |

## Using

The subjects outside the screen every scenario may arrange or check (phrase namespaces).

| Namespace |
| --- |
| Backend |
| Device |
| AppLifecycle |
| StoredData |

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
