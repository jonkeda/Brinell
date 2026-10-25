# Run MAUI Tests

Working directory: Bruinell root.

## Unit Tests

```powershell
dotnet test testsnew\Bruinell.Maui.Tests\Bruinell.Maui.Tests.csproj -v:minimal /nr:false
```

## Windows FlaUI Adapter

```powershell
dotnet build srcnew\Bruinell.Maui.FlaUI\Bruinell.Maui.FlaUI.csproj -f net10.0-windows -v:minimal /nr:false
dotnet test testsnew\Bruinell.Maui.UITests\Bruinell.Maui.UITests.csproj -f net10.0-windows7.0 -v:minimal /nr:false
```

The Windows run drives the app without mouse, keyboard or foreground, so you can keep
working while it runs. It needs no environment variables, and the app under test must
host the gesture bridge - see the [MAUI platform guide](../platform-guides/maui.md).

## In Memory (MUnit Head)

```powershell
$env:BRUINELL_HEAD='munit'; dotnet test testsnew\Bruinell.Maui.UITests\Bruinell.Maui.UITests.csproj -v:minimal /nr:false
```

The same suite renders both sample apps in memory with MUnit, from the apps compiled for plain
`net10.0` (`samples\Bruinell.Samples.Maui.App.Headless`, `...ShellApp.Headless`). It needs no
device, no built app and no window, and the whole suite takes seconds. A test that needs a device
is marked `[DeviceFact("why")]`, or `[WindowsOnlyFact(Device = "why")]`, and a scenario is
tagged `@device`; they are skipped on this head with the reason. A fixture names its in-memory
app in `CreateInMemoryOptions`. The device heads stay the judge at a gate: no layout or pixels
here. See `.my/mUnit/` for the design and the count.

## Appium Setup

Appium drives MAUI on Android and iOS only; Windows always uses FlaUI. Use
[MAUI Android](maui-android.md) for Android setup.
