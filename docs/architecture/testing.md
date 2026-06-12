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
- runtime fixtures and commands from the target project.

See [UAT Template Guide](../guides/uat-template-guide.md).

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
