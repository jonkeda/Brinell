# Presenter Platform Guide

`Brinell.Presenter` is a Windows desktop shell for loading and running UAT
workspaces.

## Projects

- `srcnew/Brinell.Presenter`
- `testsnew/Brinell.Presenter.Uat.Tests`

## User Settings

`PresenterUserSettingsService` stores:

- `LastOpenedFolder`
- up to 10 `RecentFolders`

Default path:

```text
%LOCALAPPDATA%/Brinell.Presenter/user-settings.json
```

Override:

```powershell
$env:BRINELL_PRESENTER_SETTINGS_PATH = "path\to\user-settings.json"
```

## Workspace Loading

Presenter workspaces are folders containing `uat.config.md` and `.uat.md`
scenario files. The workspace service:

- parses `uat.config.md`;
- previews discovery;
- parses and binds `.uat.md` files;
- ignores `bin/` and `obj/`;
- reports diagnostics for missing config, assemblies, target, fixture, app path,
  or working directory.

## Supported Targets

| Target | App path environment |
| --- | --- |
| `MAUI` | `APPIUM_APP_PATH` |
| `WPF` | `WPF_APP_PATH` |
| `WINFORMS` | `WINFORMS_APP_PATH` |
| `BLAZOR` | `BLAZOR_APP_PATH` |
| `HTML` | `HTML_APP_PATH` |
| `STRIDE` | `STRIDE_APP_PATH` |

MAUI supports Presenter AUT placement. Presenter sets:

- `BRINELL_AUT_PLACE_RIGHT`
- `BRINELL_AUT_PLACEMENT_RESULT_FILE`

## Execution

`UatExecutionService` loads registered assemblies, creates the configured
fixture, binds the selected scenario, creates a UAT runtime scope, and returns a
session object that can step through the scenario.

## Build

```powershell
dotnet build srcnew\Brinell.Presenter\Brinell.Presenter.csproj -v:minimal /nr:false
dotnet test testsnew\Brinell.Presenter.Uat.Tests\Brinell.Presenter.Uat.Tests.csproj -v:minimal /nr:false
```
