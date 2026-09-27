# UAT Config

The mixed suite: scenarios that use apps of several heads. Its apps are the ones in `apps.md`,
the table the mixed UI tests read too. It runs only scenarios tagged `@mixed`.

## Runtime

| Field | Value |
| --- | --- |
| Target | MIXED |
| Fixture | TodoMixedUatFixture |
| Head | mixed |
| AppsFile | apps.md |
| WorkingDirectory | ../../../.. |

## Assemblies

| Kind | Assembly |
| --- | --- |
| Pages | bin/Debug/net10.0-windows/Brinell.Samples.Todo.UITests.dll |
| Pages | bin/Debug/net10.0-windows/Brinell.Samples.Todo.Web.UITests.dll |
| Commands | bin/Debug/net10.0-windows/Brinell.Samples.Todo.Mixed.Uat.dll |

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

## Skip Rules

| Tag | EnvironmentVariable |
| --- | --- |
| android | BRINELL_ANDROID |
