# UAT Config

## Runtime

| Field | Value |
| --- | --- |
| Target | WPF |
| Fixture | WpfUatFixture |
| AppPath | ../../samples/Bruinell.Samples.Wpf.App/bin/Debug/net10.0-windows/Bruinell.Samples.Wpf.App.exe |
| WorkingDirectory | ../.. |

## Assemblies

| Kind | Assembly |
| --- | --- |
| Pages | Bruinell.Wpf.UITests.dll |

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
