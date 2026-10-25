# Run MAUI Android Tests

Working directory: Bruinell root.

## Prerequisites

- Android SDK with an emulator image, or a device.
- Node, Appium 3 and the UiAutomator2 driver (`appium driver install uiautomator2`).

## Emulator

One AVD with room to work: a small one crashed for want of memory.

```powershell
avdmanager create avd -n Bruinell_Android -k "system-images;android-36;google_apis_playstore;x86_64" -d pixel_7
```

In `%USERPROFILE%\.android\avd\Bruinell_Android.avd\config.ini`: `hw.ramSize=8192`,
`disk.dataPartition.size=16G`, `vm.heapSize=512`, `hw.gpu.mode=host`, `hw.cpu.ncore=8` (the
emulator gives 6). With the default 4 cores the Debug sample app sometimes did not finish starting
within Android's limit: "Application Not Responding ... failed to complete startup", after which
every test of the session failed. With 6 it starts in 1.6 s. Boot it cold the first time
(`emulator -avd Bruinell_Android -no-snapshot-load`); if a later boot hangs on "Loading snapshot", do
the same again.

Once per emulator, turn off stylus handwriting: Gboard's "Try out your stylus" sheet otherwise
covers the first prompt dialog and takes the taps meant for it.

```powershell
adb shell settings put secure stylus_handwriting_enabled 0
```

And turn animations off: transitions are waited out otherwise (the scroll-heavy classes took
12 m 05 s with them off against 12 m 58 s on, 2026-10-21).

```powershell
adb shell settings put global window_animation_scale 0
adb shell settings put global transition_animation_scale 0
adb shell settings put global animator_duration_scale 0
```

## Build the apps

Both: the hub app and the Shell app each have a fixture, and each fixture installs its own.

```powershell
dotnet build samples\Bruinell.Samples.Maui.App\Bruinell.Samples.Maui.App.csproj -f net10.0-android -v:minimal /nr:false
dotnet build samples\Bruinell.Samples.Maui.ShellApp\Bruinell.Samples.Maui.ShellApp.csproj -f net10.0-android -v:minimal /nr:false
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

Each fixture reinstalls its app when its session starts, because MAUI does not bump versionCode
between builds and a run could otherwise test the previous build. That is about 13 s of a 32-43 s
start. A run that has just installed both fresh builds can skip it:

```powershell
adb install -r samples\Bruinell.Samples.Maui.App\bin\Debug\net10.0-android\com.bruinell.samples.maui-Signed.apk
adb install -r samples\Bruinell.Samples.Maui.ShellApp\bin\Debug\net10.0-android\com.bruinell.samples.shell-Signed.apk
$env:BRUINELL_APP_PREINSTALLED = "1"
```

## Run

The mobile head, not the Windows one. Start with one class, then widen.

```powershell
dotnet test testsnew\Bruinell.Maui.UITests.Mobile -v:minimal /nr:false --filter "FullyQualifiedName~ButtonTests"
dotnet test testsnew\Bruinell.Maui.UITests.Mobile -v:minimal /nr:false
```

Tests about the Windows bridge, menus, and logical row indexes skip here with their reason
(`WindowsOnlyFact`).

To see where a run waits, start Appium with `--log-timestamp --log-no-colors` and read the gaps
between a response (`<--`) and the next request (`-->`): those are the client's own waits.
