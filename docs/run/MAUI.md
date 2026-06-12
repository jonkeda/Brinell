# Run MAUI Tests

Working directory: Brinell root.

## Unit Tests

```powershell
dotnet test testsnew\Brinell.Maui.Tests\Brinell.Maui.Tests.csproj -v:minimal /nr:false
```

## Windows FlaUI Adapter

```powershell
dotnet build srcnew\Brinell.Maui.FlaUI\Brinell.Maui.FlaUI.csproj -f net10.0-windows -v:minimal /nr:false
dotnet test testsnew\Brinell.Maui.UITests\Brinell.Maui.UITests.csproj -f net10.0-windows7.0 -v:minimal /nr:false
```

## Appium Setup

Set Appium values before Appium-backed tests:

```powershell
$env:APPIUM_SERVER_URI = "http://127.0.0.1:4723"
$env:APPIUM_PLATFORM = "windows"
$env:APPIUM_APP_PATH = "path\to\app.exe"
```

Use [MAUI Android](maui-android.md) for Android-specific setup.
