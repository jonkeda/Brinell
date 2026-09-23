# UAT Config

## Runtime

| Field | Value |
| --- | --- |
| Target | STRIDE |
| Fixture | StrideUatFixture |
| AppPath | ../../samples/Brinell.Samples.Stride.App/bin/Debug/net10.0-windows/Brinell.Samples.Stride.App.exe |
| WorkingDirectory | ../.. |

## Assemblies

| Kind | Assembly |
| --- | --- |
| Pages | Brinell.Stride.Uat.Tests.dll |

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
