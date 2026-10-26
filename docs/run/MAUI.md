# Run MAUI Tests

Working directory: Bruinet root.

## Unit Tests

```powershell
dotnet test testsnew\Bruinet.Maui.Tests\Bruinet.Maui.Tests.csproj -v:minimal /nr:false
```

## Windows FlaUI Adapter

```powershell
dotnet build srcnew\Bruinet.Maui.FlaUI\Bruinet.Maui.FlaUI.csproj -f net10.0-windows -v:minimal /nr:false
dotnet test testsnew\Bruinet.Maui.UITests\Bruinet.Maui.UITests.csproj -f net10.0-windows7.0 -v:minimal /nr:false
```

The Windows run drives the app without mouse, keyboard or foreground, so you can keep
working while it runs. It needs no environment variables, and the app under test must
host the gesture bridge - see the [MAUI platform guide](../platform-guides/maui.md).

## In Memory (MauiUnit Head)

```powershell
$env:BRUINET_HEAD='mauiunit'; dotnet test testsnew\Bruinet.Maui.UITests\Bruinet.Maui.UITests.csproj -v:minimal /nr:false
```

The same suite renders both sample apps in memory with MauiUnit, from the apps compiled for plain
`net10.0` (`samples\Bruinet.Samples.Maui.App.Headless`, `...ShellApp.Headless`). It needs no
device, no built app and no window, and the whole suite takes seconds. A test that needs a device
is marked `[DeviceFact("why")]`, or `[WindowsOnlyFact(Device = "why")]`, and a scenario is
tagged `@device`; they are skipped on this head with the reason. A fixture names its in-memory
app in `CreateInMemoryOptions`. The device heads stay the judge at a gate: no layout or pixels
here. See `.my/mUnit/` for the design and the count.

## Appium Setup

Appium drives MAUI on Android and iOS only; Windows always uses FlaUI. Use
[MAUI Android](maui-android.md) for Android setup.
