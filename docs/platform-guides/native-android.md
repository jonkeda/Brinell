# Native Android Platform Guide

`Brinell.NativeAndroid` provides Android-first page objects and controls backed
by Appium/UiAutomator2.

## Projects

- `srcnew/Brinell.NativeAndroid`
- `testsnew/Brinell.NativeAndroid.Tests`

## Options

`NativeAndroidDriverOptions.FromEnvironment()` reads:

| Variable | Purpose |
| --- | --- |
| `APPIUM_SERVER_URI` | Appium server URL |
| `APPIUM_APP_PATH` | APK or app path |
| `APPIUM_APP_PACKAGE` | Android package name |
| `APPIUM_APP_ACTIVITY` | Launch activity |
| `APPIUM_PLATFORM_VERSION` | Android platform version |
| `APPIUM_DEVICE_NAME` | Device or emulator name |
| `APPIUM_AUTOMATION_NAME` | Appium automation name, default `UiAutomator2` |
| `APPIUM_AUTO_GRANT_PERMISSIONS` | Auto-grant runtime permissions |
| `APPIUM_NO_RESET` | Appium no-reset flag |
| `APPIUM_FULL_RESET` | Appium full-reset flag |

Defaults:

- server: `http://127.0.0.1:4723`
- device: `emulator-5554`
- automation: `UiAutomator2`
- auto-grant permissions: true

## Page Objects

Native Android page objects inherit from
`NativeAndroidPageObjectBase<TSelf>`. They expose typed Android controls such as
buttons, text fields, lists, recycler views, dialogs, tabs, toolbars, web views,
and permission dialogs.

Use `ReadyLocator` to define page readiness. `WaitReady()` and `AssertLoaded()`
poll through the native Android context and timeout settings.

## Evidence

`NativeAndroidEvidenceCapture` saves:

- screenshot: `<timestamp>-<name>.png`
- page source: `<timestamp>-<name>.xml`

Prefer writing evidence into a folder from the shared artifact provider.

## Tests

Focused non-UI checks:

```powershell
dotnet test testsnew\Brinell.NativeAndroid.Tests\Brinell.NativeAndroid.Tests.csproj -v:minimal /nr:false
```
