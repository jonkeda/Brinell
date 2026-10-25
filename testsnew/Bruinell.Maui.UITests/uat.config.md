# UAT Config

## Runtime

| Field | Value |
| --- | --- |
| Target | MAUI |
| Fixture | MauiFixture |
| AppPath | ../../samples/Bruinell.Samples.Maui.App/bin/Debug/net10.0-windows10.0.19041.0/win-x64/Bruinell.Samples.Maui.App.exe |
| WorkingDirectory | ../.. |

## Assemblies

| Kind | Assembly |
| --- | --- |
| Pages | bin/Debug/net10.0-windows/Bruinell.Maui.UITests.dll |
| Controls | ../../srcnew/Bruinell.Maui/bin/Debug/net10.0/Bruinell.Maui.dll |
| Commands | ../../srcnew/Bruinell.Uat/bin/Debug/net10.0/Bruinell.Uat.dll |

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

## Skip Rules

| Tag | EnvironmentVariable |
| --- | --- |
| hardware | BRUINELL_UAT_HARDWARE |
| live-api | BRUINELL_UAT_LIVE_API |
