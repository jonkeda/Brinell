# Running Windows MAUI UI Tests with FlaUI

Windows MAUI tests use **FlaUI** for native UI Automation. No external server required.

## Prerequisites

1. Windows 10/11 with Developer Mode enabled
2. .NET 10.0 SDK

## Build the Sample App

```powershell
cd e:\repos\Clay\ClaiConstructionMobile\Brinell

dotnet build samples/Brinell.Samples.Maui.App/Brinell.Samples.Maui.App.csproj `
    -f net10.0-windows10.0.19041.0 -r win-x64
```

## Run the UI Tests

```powershell
cd e:\repos\Clay\ClaiConstructionMobile\Brinell

dotnet test testsnew/Brinell.Maui.UITests/Brinell.Maui.UITests.csproj
```

## Run Specific Tests

```powershell
# Run a single test
dotnet test testsnew/Brinell.Maui.UITests --filter "FullyQualifiedName~ButtonTests"

# Run with detailed output
dotnet test testsnew/Brinell.Maui.UITests --logger "console;verbosity=detailed"
```

## Enable Developer Mode

If tests fail with automation access errors:

1. Open **Settings** → **Privacy & Security** → **For Developers**
2. Enable **Developer Mode**
