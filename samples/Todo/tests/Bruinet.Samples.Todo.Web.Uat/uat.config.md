# UAT Config

## Runtime

| Field | Value |
| --- | --- |
| Target | BLAZOR |
| Fixture | TodoWebUatFixture |
| Head | web |
| WorkingDirectory | ../../../.. |
| DefaultTestData | one-todo |

## Apps

| Name | ClientId |
| --- | --- |
| Alice | web-alice |
| Bob | web-bob |

## Assemblies

| Kind | Assembly |
| --- | --- |
| Pages | bin/Debug/net10.0/Bruinet.Samples.Todo.Web.Uat.dll |
| Controls | ../../../../srcnew/Bruinet.Html/bin/Debug/net10.0/Bruinet.Html.dll |
| Commands | ../../../../srcnew/Bruinet.Uat/bin/Debug/net10.0/Bruinet.Uat.dll |
| TestData | ../Bruinet.Samples.Todo.TestSupport/bin/Debug/net10.0/Bruinet.Samples.Todo.TestSupport.dll |

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
