# PLAN: Run Brinell MAUI UI Tests on Android

**Created:** January 20, 2026  
**Status:** Draft  
**Priority:** High

## 1. Executive Summary

This plan outlines the steps required to run the existing Brinell MAUI UI tests on an Android device/emulator. The current test infrastructure is configured for Windows and needs modifications to support Android.

## 2. Current State

### 2.1 What Exists
- **MAUI Sample App** (`samples/Brinell.Samples.Maui.App/`): Currently targets only `net10.0-windows10.0.19041.0`
- **UI Tests** (`testsnew/Brinell.Maui.UITests/`): ~224 tests running against Windows
- **Test Infrastructure**: `MauiTestFixtureBase` already has Android configuration stubs
- **Appium Support**: Framework supports Windows, Android, iOS via environment variables

### 2.2 Current Test Pass Rate (Windows)
- 68% (153/224 tests passing)
- Key blockers: off-screen elements, scroll issues, platform-specific control rendering

### 2.3 Environment Variables Already Defined
```bash
APPIUM_PLATFORM      # "windows", "android", or "ios" (default: windows)
APPIUM_SERVER_URI    # Appium server URL (default: http://127.0.0.1:4723)
APPIUM_APP_PATH      # Path to APK or app
APPIUM_DEVICE_NAME   # Device/emulator name (default: emulator-5554)
```

## 3. Tasks

### Phase 1: Prerequisites & Setup

#### Task 1.1: Add Android Target Framework to MAUI App ⬜
**File:** `samples/Brinell.Samples.Maui.App/Brinell.Samples.Maui.App.csproj`

Change:
```xml
<TargetFrameworks>net10.0-windows10.0.19041.0</TargetFrameworks>
```

To:
```xml
<TargetFrameworks>net10.0-windows10.0.19041.0;net10.0-android</TargetFrameworks>
```

Add Android-specific properties:
```xml
<!-- Android -->
<SupportedOSPlatformVersion Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'android'">21.0</SupportedOSPlatformVersion>
```

#### Task 1.2: Install Android Workload ⬜
```powershell
dotnet workload install android
```

#### Task 1.3: Install Appium UiAutomator2 Driver ⬜
```powershell
appium driver install uiautomator2
```

Verify installed drivers:
```powershell
appium driver list --installed
```

#### Task 1.4: Set Up Android Emulator or Connect Device ⬜

**Option A: Emulator (Recommended for CI)**
```powershell
# List available AVDs
emulator -list-avds

# Start emulator (example: Pixel_6_API_34)
emulator -avd Pixel_6_API_34 -no-snapshot-load
```

**Option B: Physical Device**
```powershell
# Enable USB debugging on device
# Connect via USB
adb devices  # Should show device
```

#### Task 1.5: Verify ADB Connectivity ⬜
```powershell
adb devices
# Expected output: 
# emulator-5554   device
```

### Phase 2: Build & Deploy Android App

#### Task 2.1: Build Android APK ⬜
```powershell
cd samples/Brinell.Samples.Maui.App
dotnet build -f net10.0-android -c Debug
```

Output will be in: `bin/Debug/net10.0-android/com.brinell.samples.maui-Signed.apk`

#### Task 2.2: Verify APK Package Name ⬜
```powershell
# Extract package info from APK
aapt dump badging bin/Debug/net10.0-android/com.brinell.samples.maui-Signed.apk | Select-String "package:"
```

Expected: `package: name='com.brinell.samples.maui'`

#### Task 2.3: Find Main Activity Class Name ⬜
```powershell
# The activity name is hashed by MAUI - need to extract it
aapt dump badging <apk-path> | Select-String "launchable-activity:"
```

**Note:** MAUI uses a hashed activity name like `crc64<hash>.MainActivity`. Update `AppiumFixture.cs` if different.

### Phase 3: Start Services

#### Task 3.1: Start Appium Server ⬜
```powershell
appium --base-path /
# Or use the existing script:
.\start-appium.ps1
```

#### Task 3.2: Verify Appium Status ⬜
```powershell
curl http://127.0.0.1:4723/status
# Should return: {"value":{"ready":true,"message":"..."}}
```

### Phase 4: Run Tests

#### Task 4.1: Set Environment Variables for Android ⬜
```powershell
$env:APPIUM_PLATFORM = "android"
$env:APPIUM_DEVICE_NAME = "emulator-5554"  # Or your device ID from 'adb devices'
$env:APPIUM_APP_PATH = "$PWD\samples\Brinell.Samples.Maui.App\bin\Debug\net10.0-android\com.brinell.samples.maui-Signed.apk"
```

#### Task 4.2: Run a Single Test First ⬜
```powershell
dotnet test testsnew/Brinell.Maui.UITests/Brinell.Maui.UITests.csproj `
    --filter "FullyQualifiedName~Button_IsExists" `
    -- xunit.diagnosticMessages=true
```

#### Task 4.3: Run All Tests ⬜
```powershell
dotnet test testsnew/Brinell.Maui.UITests/Brinell.Maui.UITests.csproj `
    --logger "console;verbosity=detailed"
```

### Phase 5: Platform-Specific Fixes (Expected)

#### Task 5.1: Investigate Android Control Differences ⬜
Android MAUI controls may have different:
- AutomationId attribute names
- Element hierarchy (more nesting)
- State attribute formats

Use diagnostic tests to dump page source:
```csharp
var pageSource = driver.PageSource;
File.WriteAllText("android-page-source.xml", pageSource);
```

#### Task 5.2: Add Android-Specific Attribute Handling ⬜
If needed, modify `MauiToggleControlBase.IsCheckedCore()` to handle Android:
```csharp
// Android may use different attribute names
string[] androidAttributes = { "checked", "selected", "focused" };
```

#### Task 5.3: Fix Locator Strategies ⬜
Android uses different locator strategies:
- `AccessibilityId` → `content-desc` attribute
- May need XPath fallbacks for complex hierarchies

## 4. Expected Issues & Mitigations

| Issue | Likelihood | Mitigation |
|-------|------------|------------|
| Activity name mismatch | High | Extract actual activity from APK with `aapt` |
| Different element attributes | High | Add Android-specific attribute checks |
| Off-screen elements | Medium | Android has better scroll support than Windows |
| Element not found | Medium | Android hierarchy may differ - use XPath |
| Touch actions fail | Low | UiAutomator2 supports touch well |

## 5. Test Script

Create a convenience script `run-android-tests.ps1`:

```powershell
#!/usr/bin/env pwsh
# run-android-tests.ps1 - Run Brinell UI tests on Android

param(
    [string]$Filter = "",
    [string]$DeviceName = "emulator-5554",
    [switch]$Build
)

$ErrorActionPreference = "Stop"

# Build APK if requested
if ($Build) {
    Write-Host "Building Android APK..." -ForegroundColor Cyan
    dotnet build samples/Brinell.Samples.Maui.App/Brinell.Samples.Maui.App.csproj `
        -f net10.0-android -c Debug
}

# Set environment
$env:APPIUM_PLATFORM = "android"
$env:APPIUM_DEVICE_NAME = $DeviceName
$env:APPIUM_APP_PATH = "$PSScriptRoot\samples\Brinell.Samples.Maui.App\bin\Debug\net10.0-android\com.brinell.samples.maui-Signed.apk"

# Verify APK exists
if (-not (Test-Path $env:APPIUM_APP_PATH)) {
    Write-Error "APK not found at: $($env:APPIUM_APP_PATH)"
    Write-Host "Run with -Build to build the APK first" -ForegroundColor Yellow
    exit 1
}

# Check Appium running
try {
    $status = Invoke-RestMethod "http://127.0.0.1:4723/status"
    Write-Host "Appium server ready: $($status.value.message)" -ForegroundColor Green
} catch {
    Write-Error "Appium server not running. Start with: appium --base-path /"
    exit 1
}

# Check ADB device
$devices = adb devices | Select-String $DeviceName
if (-not $devices) {
    Write-Error "Device '$DeviceName' not found. Run 'adb devices' to list available devices."
    exit 1
}
Write-Host "Device found: $DeviceName" -ForegroundColor Green

# Run tests
$filterArg = if ($Filter) { "--filter `"$Filter`"" } else { "" }
Write-Host "`nRunning tests..." -ForegroundColor Cyan
Invoke-Expression "dotnet test testsnew/Brinell.Maui.UITests/Brinell.Maui.UITests.csproj $filterArg --logger `"console;verbosity=normal`""
```

## 6. CI/CD Integration

For GitHub Actions, add workflow:

```yaml
# .github/workflows/android-tests.yml
name: Android UI Tests

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  android-test:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '10.0.x'
    
    - name: Setup Java
      uses: actions/setup-java@v4
      with:
        distribution: 'temurin'
        java-version: '17'
    
    - name: Setup Android SDK
      uses: android-actions/setup-android@v3
    
    - name: Start Android Emulator
      uses: reactivecircus/android-emulator-runner@v2
      with:
        api-level: 34
        target: google_apis
        arch: x86_64
        script: |
          # Install workload
          dotnet workload install android
          
          # Build APK
          dotnet build samples/Brinell.Samples.Maui.App -f net10.0-android -c Debug
          
          # Start Appium
          npm install -g appium
          appium driver install uiautomator2
          appium --base-path / &
          sleep 10
          
          # Run tests
          export APPIUM_PLATFORM=android
          export APPIUM_DEVICE_NAME=emulator-5554
          export APPIUM_APP_PATH=./samples/Brinell.Samples.Maui.App/bin/Debug/net10.0-android/*.apk
          dotnet test testsnew/Brinell.Maui.UITests
```

## 7. Verification Checklist

- [ ] Android workload installed (`dotnet workload list`)
- [ ] UiAutomator2 driver installed (`appium driver list --installed`)
- [ ] Emulator/device connected (`adb devices`)
- [ ] APK built successfully
- [ ] Appium server running (`curl http://127.0.0.1:4723/status`)
- [ ] Environment variables set correctly
- [ ] Single test passes
- [ ] Full test suite runs (note pass rate)

## 8. Timeline Estimate

| Phase | Duration | Notes |
|-------|----------|-------|
| Phase 1: Setup | 1-2 hours | One-time setup |
| Phase 2: Build | 15-30 min | Includes first-time restore |
| Phase 3: Services | 5 min | Start emulator + Appium |
| Phase 4: Initial Run | 30 min | First test run, debug issues |
| Phase 5: Fixes | 2-8 hours | Depends on platform differences |

**Total Estimated Time:** 4-12 hours (first time)

## 9. References

- [Appium UiAutomator2 Driver Docs](https://appium.io/docs/en/drivers/android-uiautomator2/)
- [MAUI Android Deployment](https://learn.microsoft.com/en-us/dotnet/maui/android/deployment/)
- [Android Emulator Setup](https://developer.android.com/studio/run/emulator)
