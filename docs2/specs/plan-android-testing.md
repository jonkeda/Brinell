# Android Testing Plan

**Status:** Draft (not yet executed)

## Prerequisites

1. Add `net10.0-android` TFM to sample app
2. Install `android` workload: `dotnet workload install android`
3. Install UiAutomator2 Appium driver: `appium driver install uiautomator2`
4. Set up Android emulator or connect device

## Build

```powershell
dotnet build samples/Brinell.Samples.Maui.App -f net10.0-android
```

## Run Tests

```powershell
$env:APPIUM_PLATFORM = "android"
$env:APPIUM_DEVICE_NAME = "emulator-5554"
$env:APPIUM_APP_PATH = "path/to/com.brinell.samples.apk"
dotnet test testsnew/Brinell.Maui.UITests
```

## Known Issues

- **ScrollIntoView:** `windows: scroll` doesn't work on Android. Need `mobile: scrollGesture` + `UiScrollable` (see SPEC-scrollintoview-android)
- **Different attribute names:** Android uses `content-desc` instead of `AutomationId`
- **Activity name:** Must extract from APK manifest for Appium capabilities
- **Performance:** Android emulator is slower; may need increased timeouts

## CI/CD

GitHub Actions with `android-emulator-runner` action for emulator management.
