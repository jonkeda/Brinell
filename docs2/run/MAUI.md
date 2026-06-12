# Running MAUI UI Tests

## Prerequisites

1. **Node.js** installed (for Appium)
2. **Appium** with Windows driver
3. **Windows App SDK** installed
4. Built MAUI app

## Quick Start

```powershell
# 1. Start Appium server (in separate terminal)
Start-Process cmd.exe -ArgumentList "/c","appium --address 127.0.0.1 --port 4723 --relaxed-security"

# 2. Wait for server to start, then verify
Start-Sleep -Seconds 3
(Invoke-WebRequest -Uri "http://127.0.0.1:4723/status" -UseBasicParsing).Content

# 3. Build and run tests
cd samples/Brinell.Samples.Maui.App
dotnet build -f net10.0-windows10.0.19041.0
cd ../Brinell.Samples.Maui.UITests
dotnet test
```

## Detailed Setup

### 1. Install Appium

```powershell
# Install Appium globally
npm install -g appium

# Install Windows driver
appium driver install windows

# Verify installation
appium driver list --installed
```

### 2. Start Appium Server

**Important:** Start Appium in a **separate terminal window** so it keeps running while tests execute.

```powershell
# Start in a new window (recommended)
Start-Process cmd.exe -ArgumentList "/c","appium --address 127.0.0.1 --port 4723 --relaxed-security"

# Or start in current terminal (will block)
appium --address 127.0.0.1 --port 4723 --relaxed-security
```

**Required flags:**
- `--address 127.0.0.1` - Bind to localhost
- `--port 4723` - Default Appium port (matches test configuration)
- `--relaxed-security` - Required for Windows automation capabilities

**Verify server is running:**
```powershell
(Invoke-WebRequest -Uri "http://127.0.0.1:4723/status" -UseBasicParsing).Content
# Should return: {"value":{"ready":true,...}}
```

### 3. Build the MAUI App

```powershell
cd samples/Brinell.Samples.Maui.App
dotnet build -f net10.0-windows10.0.19041.0
```

The app executable will be at:
```
samples/Brinell.Samples.Maui.App/bin/Debug/net10.0-windows10.0.19041.0/win-x64/Brinell.Samples.Maui.App.exe
```

### 4. Build the Test Project

```powershell
cd samples/Brinell.Samples.Maui.UITests
dotnet build
```

## Running Tests

### Run All Tests

```powershell
cd samples/Brinell.Samples.Maui.UITests
dotnet test
```

### Run Specific Test Class

```powershell
dotnet test --filter "FullyQualifiedName~CounterTests"
```

### Run Single Test

```powershell
dotnet test --filter "FullyQualifiedName~Counter_InitialValue_IsZero"
```

### Run with Verbose Output

```powershell
dotnet test --logger:"console;verbosity=detailed"
```

### Run with No Build (faster, if already built)

```powershell
dotnet test --no-build
```

## Troubleshooting

### "Connection refused" Error
```
No connection could be made because the target machine actively refused it. (127.0.0.1:4723)
```
**Solution:** Appium server is not running. Start it in a separate terminal:
```powershell
Start-Process cmd.exe -ArgumentList "/c","appium --address 127.0.0.1 --port 4723 --relaxed-security"
```

### "Could not find app" Error
- Verify app path in `MauiTestBase.cs` matches your build output
- Ensure app was built for Windows: `dotnet build -f net10.0-windows10.0.19041.0`

### Tests Timeout
- Increase `DefaultTimeoutMs` in `AppiumTestOptions.Windows()` call
- Ensure app launches successfully manually first
- Check if antivirus is blocking automation

### Toggle/Switch State Not Detected
The framework checks multiple attribute names for Windows compatibility:
- `Toggle.ToggleState` (Windows UIA)
- `checked` (standard)
- `IsToggled` / `IsChecked` (MAUI)

### Slider Value Not Changing
The slider control uses click-at-position to set values. If this doesn't work:
- Ensure the slider is fully visible (not partially scrolled off-screen)
- Try scrolling to the slider first: `scrollView.ScrollToElement("SliderId")`

### App Doesn't Close Between Tests
Each test creates a new app session. If the app doesn't close:
- Check `Dispose()` is being called in test base
- Manually kill any lingering app processes

## Platform Notes

### Windows (current)
- Uses Appium with Windows driver (wraps WinAppDriver)
- Requires Windows 10/11 with Developer Mode enabled
- Uses UI Automation (UIA) for element discovery

### Android (future)
- Update `AppiumTestOptions` to use `Android()` factory
- Requires Android emulator or device
- Uses `appium driver install uiautomator2`

### iOS (future)
- Update `AppiumTestOptions` to use `iOS()` factory
- Requires macOS with Xcode
- Uses `appium driver install xcuitest`

## Test Results

Screenshots on assertion failures are saved to:
```
samples/Brinell.Samples.Maui.UITests/bin/Debug/net8.0/TestResults/Screenshots/
```
