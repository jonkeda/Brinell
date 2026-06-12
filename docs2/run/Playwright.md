# Running Playwright UI Tests

## Prerequisites

1. .NET 8.0+ SDK
2. Playwright browsers installed

## Install Playwright Browsers

```powershell
# Install Playwright browsers (one-time setup)
pwsh bin/Debug/net8.0/playwright.ps1 install
```

## Start the Blazor App

```powershell
# From the Brinell root directory
cd samples/Brinell.Samples.Blazor.App
dotnet run --urls "http://localhost:5180"
```

Or start as background process:
```powershell
Start-Process dotnet -ArgumentList "run", "--urls", "http://localhost:5180" -WorkingDirectory "samples\Brinell.Samples.Blazor.App" -WindowStyle Hidden
```

## Run Tests

### Headless Mode (default)

```powershell
cd E:\repos\Private\Iosk\Oravey\Brinell
$env:HEADLESS = "true"
$env:BLAZOR_APP_URL = "http://localhost:5180"
dotnet test samples/Brinell.Samples.Blazor.PlaywrightTests --logger "console;verbosity=normal"
```

### Visible Mode (for debugging)

```powershell
cd E:\repos\Private\Iosk\Oravey\Brinell
$env:HEADLESS = "false"
$env:BLAZOR_APP_URL = "http://localhost:5180"
dotnet test samples/Brinell.Samples.Blazor.PlaywrightTests --logger "console;verbosity=normal"
```

### Run Single Test

```powershell
dotnet test samples/Brinell.Samples.Blazor.PlaywrightTests --filter "FullyQualifiedName~Counter_ClickIncrement" --logger "console;verbosity=normal"
```

## Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| HEADLESS | Run browser in headless mode | true |
| BLAZOR_APP_URL | URL of the Blazor app | http://localhost:5180 |
| SLOW_MO | Slow down operations (ms) | 0 |

## Troubleshooting

### Browser not installed
```powershell
# Navigate to test output directory and run
pwsh playwright.ps1 install
```

### Connection refused
Make sure the Blazor app is running on the correct port before running tests.

### Timeout errors
Increase timeout in test base or use explicit waits:
```csharp
await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
```

## Test Structure

```
samples/Brinell.Samples.Blazor.PlaywrightTests/
├── PageObjects/           # Page object classes
│   ├── CounterPage.cs
│   └── FormControlsPage.cs
├── TestBase/              # Test base classes
│   └── BlazorPlaywrightTestBase.cs
└── Tests/                 # Test classes
    ├── CheckBoxTests.cs
    ├── CounterTests.cs
    ├── LinkTests.cs
    └── SelectTests.cs
```
