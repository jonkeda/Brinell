# Testing

Working directory for commands in this document: Brinell root.

## Build

```powershell
dotnet build srcnew\Brinell.sln -v:minimal /nr:false
```

## Unit Tests

Run focused unit tests by project:

```powershell
dotnet test testsnew\Brinell.Core.Tests\Brinell.Core.Tests.csproj -v:minimal /nr:false
dotnet test testsnew\Brinell.Maui.Tests\Brinell.Maui.Tests.csproj -v:minimal /nr:false
dotnet test testsnew\Brinell.Uat.Tests\Brinell.Uat.Tests.csproj -v:minimal /nr:false
```

## UI Tests

UI tests require platform setup:

- WPF/WinForms/FlaUI tests require Windows desktop access.
- MAUI Appium tests require Appium and a target app/device.
- Playwright tests require browser installation.
- Stride tests require the sample game/app runtime.

Run the platform-specific guide before running live UI tests.

## UAT Tests

UAT test projects combine:

- `uat.config.md`;
- one or more `.uat.md` scenario files;
- a fixture, an xUnit collection and a `UatScenarioTestBase<TFixture>` class.

A scenario step binds to a phrase, and the built-in phrases are `[UatWhen]` / `[UatThen]` attributes on the
`Brinell.Core` control interfaces, discovered by the engine rather than listed anywhere
([AD-011](../../.docs/decisions/ad-011-uat-vocabulary-is-attribute-declared.md)). An app adds
verbs of its own the same way.

A UAT project drives a real app, so it costs what a UI run costs: build the app first, and run
one UI or UAT process at a time.

An app with a MAUI and a web head has one set of scenario files; each head's UAT project runs the
scenarios tagged for it (`Head` in `uat.config.md`, `@maui` / `@web` tags). A scenario can also
use several apps of one head, such as two devices on one backend
([AD-014](../../.docs/decisions/ad-014-one-scenario-file-serves-every-head.md)).

See [UAT Template Guide](../guides/uat-template-guide.md),
[UAT Phrases And Flows](../guides/uat-phrases-and-flows.md), and the worked example in
[One Todo UAT, end to end](../../samples/Todo/uat-walkthrough.md).

## Artifact Layout

Brinell artifacts should use:

```text
TestResults/<run-id>/suites/<suite-name>/
```

Common subfolders:

- `runner/`
- `logs/`
- `screenshots/`
- `uat/`
- `traces/`
- `videos/`
- `downloads/`
- `attachments/`

Prefer the shared artifact provider instead of hardcoded output folders.
