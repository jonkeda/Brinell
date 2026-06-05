# UAT Config

## Runtime

| Field | Value |
| --- | --- |
| Target | MAUI |
| Fixture | Appium |
| AppPath | ../../samples/Brinell.Samples.Maui.App/bin/Debug/net10.0-windows10.0.19041.0/win-x64/Brinell.Samples.Maui.App.exe |
| WorkingDirectory | ../.. |

## Assemblies

| Kind | Assembly |
| --- | --- |
| Pages | ../Brinell.Maui.UITests/bin/Debug/net10.0-windows7.0/Brinell.Maui.UITests.dll |
| Controls | ../../srcnew/Brinell.Maui/bin/Debug/net10.0/Brinell.Maui.dll |
| Commands | ../../srcnew/Brinell.Uat/bin/Debug/net10.0/Brinell.Uat.dll |

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
| hardware | BRINELL_UAT_HARDWARE |
| live-api | BRINELL_UAT_LIVE_API |
