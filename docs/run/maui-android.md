# Run MAUI Android Tests

Working directory: Brinell root.

## Prerequisites

- Android SDK with an emulator image, or a device.
- Node, Appium 3 and the UiAutomator2 driver (`appium driver install uiautomator2`).

## Emulator

One AVD with room to work: a small one crashed for want of memory.

```powershell
avdmanager create avd -n Brinell_Android -k "system-images;android-36;google_apis_playstore;x86_64" -d pixel_7
```

In `%USERPROFILE%\.android\avd\Brinell_Android.avd\config.ini`: `hw.ramSize=8192`,
`disk.dataPartition.size=16G`, `vm.heapSize=512`, `hw.gpu.mode=host`. Boot it cold the first time
(`emulator -avd Brinell_Android -no-snapshot-load`); if a later boot hangs on "Loading snapshot", do
the same again.

Once per emulator, turn off stylus handwriting: Gboard's "Try out your stylus" sheet otherwise
covers the first prompt dialog and takes the taps meant for it.

```powershell
adb shell settings put secure stylus_handwriting_enabled 0
```

## Build the apps

Both: the hub app and the Shell app each have a fixture, and each fixture installs its own.

```powershell
dotnet build samples\Brinell.Samples.Maui.App\Brinell.Samples.Maui.App.csproj -f net10.0-android -v:minimal /nr:false
dotnet build samples\Brinell.Samples.Maui.ShellApp\Brinell.Samples.Maui.ShellApp.csproj -f net10.0-android -v:minimal /nr:false
```

## Appium

Load only the Android driver: loading every installed driver took a minute (the Windows driver
alone 41 s). Wait for port 4723 before running.

```powershell
appium --address 127.0.0.1 --port 4723 --relaxed-security --use-drivers uiautomator2
```

## Environment

```powershell
$env:APPIUM_SERVER_URI = "http://127.0.0.1:4723"
$env:APPIUM_PLATFORM = "android"
$env:APPIUM_DEVICE_NAME = "emulator-5554"
```

Leave `APPIUM_APP_PATH` unset: it overrides the app of every fixture, and the Shell fixture then
starts the hub app's package and fails.

## Run

The mobile head, not the Windows one. Start with one class, then widen.

```powershell
dotnet test testsnew\Brinell.Maui.UITests.Mobile -v:minimal /nr:false --filter "FullyQualifiedName~ButtonTests"
dotnet test testsnew\Brinell.Maui.UITests.Mobile -v:minimal /nr:false
```

Tests about the Windows bridge, menus, and logical row indexes skip here with their reason
(`WindowsOnlyFact`).

To see where a run waits, start Appium with `--log-timestamp --log-no-colors` and read the gaps
between a response (`<--`) and the next request (`-->`): those are the client's own waits.
