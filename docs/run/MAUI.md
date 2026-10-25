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

## Appium Setup

Appium drives MAUI on Android and iOS only; Windows always uses FlaUI. Use
[MAUI Android](maui-android.md) for Android setup.
