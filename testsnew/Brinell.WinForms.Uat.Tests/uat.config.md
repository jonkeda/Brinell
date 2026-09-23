# UAT Config

## Runtime

| Field | Value |
| --- | --- |
| Target | WINFORMS |
| Fixture | WinFormsUatFixture |
| AppPath | ../../samples/Brinell.Samples.WinForms.App/bin/Debug/net10.0-windows/Brinell.Samples.WinForms.App.exe |
| WorkingDirectory | ../.. |

## Assemblies

| Kind | Assembly |
| --- | --- |
| Pages | Brinell.WinForms.Uat.Tests.dll |

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
