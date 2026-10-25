# UAT Config

## Runtime

| Field | Value |
| --- | --- |
| Target | HTML |
| Fixture | HtmlUatFixture |
| AppPath | ../../samples/Bruinell.Samples.Blazor.App/Bruinell.Samples.Blazor.App.csproj |

## Assemblies

| Kind | Assembly |
| --- | --- |
| Pages | Bruinell.Html.UITests.dll |

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
