# UAT Config

## Runtime

| Field | Value |
| --- | --- |
| Target | WPF |
| Fixture | WpfUatFixture |
| AppPath | ../../samples/Brinell.Samples.Wpf.App/bin/Debug/net10.0-windows/Brinell.Samples.Wpf.App.exe |
| WorkingDirectory | ../.. |

## Assemblies

| Kind | Assembly |
| --- | --- |
| Pages | Brinell.Wpf.Uat.Tests.dll |

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
