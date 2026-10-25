# UAT Config

## Runtime

| Field | Value |
| --- | --- |
| Target | STRIDE |
| Fixture | StrideUatFixture |
| AppPath | ../../samples/Bruinell.Samples.Stride.App/bin/Debug/net10.0-windows/Bruinell.Samples.Stride.App.exe |
| WorkingDirectory | ../.. |

## Assemblies

| Kind | Assembly |
| --- | --- |
| Pages | Bruinell.Stride.UITests.dll |

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
