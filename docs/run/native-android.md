# Run The Native Android UI Tests

`testsnew/Brinell.NativeAndroid.UITests` drives the gallery app
(`samples/Brinell.Samples.NativeAndroid.App`) on an Android emulator through Appium. It holds the
UI tests and the platform's UAT suite. CI does not run it: it needs a device.

## Once

- The .NET `android` workload (`dotnet workload install android`) and the Android SDK, with
  `ANDROID_HOME` set.
- An emulator image (an AVD), API 34 or later.
- Appium 2 and its UiAutomator2 driver: `npm i -g appium`, then `appium driver install uiautomator2`.

## Each time

From the Brinell root:

```powershell
# The emulator. Software rendering: with the host GPU, screenshots and recordings came out black.
& "$env:ANDROID_HOME\emulator\emulator.exe" -avd <your AVD> -no-snapshot-save -no-boot-anim -gpu swiftshader_indirect

# Appium, in a second terminal.
appium --address 127.0.0.1 --port 4723

# The gallery APK. Rebuild it whenever anything under samples/ changed: the fixture installs it.
dotnet build samples\Brinell.Samples.NativeAndroid.App -v:minimal

# The tests: one class, the UAT suite, or everything.
dotnet test testsnew\Brinell.NativeAndroid.UITests --filter "FullyQualifiedName~ControlsTests"
dotnet test testsnew\Brinell.NativeAndroid.UITests --filter "Category=UAT"
dotnet test testsnew\Brinell.NativeAndroid.UITests
```

The fixture opens one Appium session for the whole run and installs the APK it finds under the
sample's `bin\Debug\net10.0-android`; `APPIUM_APP_PATH` overrides it, and the other `APPIUM_*`
variables ([the platform guide](../platform-guides/native-android.md)) override the device and the
server. One UI test process at a time, and no build while one runs.

The full suite is 30 tests and takes about three and a half minutes on a freshly started emulator.
An emulator that has run for hours gets slower (the same suite took 11-16 minutes after two hours):
restart it when runs slow down.

## Recording

```powershell
$env:BRINELL_RECORD = "1"; $env:BRINELL_RECORD_DIR = "TestResults\recordings"
dotnet test testsnew\Brinell.NativeAndroid.UITests --filter "Category=UAT"
```

Each scenario is recorded with `adb screenrecord`, with its taps and typing in the timeline.

## When a run fails

- **"The app under test is no longer available"**: the Appium session ended. Is Appium still running?
- **The app exits at once after install**: a Debug build left its assemblies out of the APK; the
  sample sets `EmbedAssembliesIntoApk`, an app of your own must too.
- **"not ready ... a system dialog covers the app"**: an Android dialog (a permission prompt, "app
  not responding") is in front. Answer it, or restart the emulator.
- **Runs get slower and slower, or one aborts**: the emulator has run for too long. Restart it
  (and Appium).
- **A control below the fold is "missing"**: it is scrolled in only on a page; in a row or a dialog,
  scroll the list or the dialog first.
