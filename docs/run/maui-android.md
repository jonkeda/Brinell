# Run MAUI Android Tests

Working directory: Brinell root.

## Prerequisites

- Android SDK and emulator or device.
- Appium server.
- Appium UiAutomator2 driver.

## Build App

```powershell
dotnet build samples\Brinell.Samples.Maui.App\Brinell.Samples.Maui.App.csproj -f net10.0-android -v:minimal /nr:false
```

## Environment

```powershell
$env:APPIUM_SERVER_URI = "http://127.0.0.1:4723"
$env:APPIUM_PLATFORM = "android"
$env:APPIUM_DEVICE_NAME = "emulator-5554"
$env:APPIUM_APP_PATH = "path\to\sample.apk"
```

## Run

```powershell
dotnet test testsnew\Brinell.Maui.UITests\Brinell.Maui.UITests.csproj -v:minimal /nr:false
```
