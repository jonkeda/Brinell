# UAT Config

The `service` head: the Todo scenarios tagged `@service`, run against the app's services without a
UI (`TodoServiceFixture`). The scenario files are the MAUI suite's, linked.

## Runtime

| Field | Value |
| --- | --- |
| Target | SERVICE |
| Fixture | TodoServiceFixture |
| Head | service |
| DefaultTestData | one-todo |

## Assemblies

| Kind | Assembly |
| --- | --- |
| Steps | bin/Debug/net10.0/Brinell.Samples.Todo.IntegrationTests.dll |
| Commands | ../../../../srcnew/Brinell.Uat/bin/Debug/net10.0/Brinell.Uat.dll |
| TestData | ../Brinell.Samples.Todo.TestSupport/bin/Debug/net10.0/Brinell.Samples.Todo.TestSupport.dll |

## Using

What every service scenario may say: the subjects past the screen, and the service head's own steps.

| Namespace |
| --- |
| Backend |
| Device |
| AppLifecycle |
| StoredData |
| Todos |
| Sync |

## Reporting

| Field | Value |
| --- | --- |
| IncludeRuntimeTrace | true |
