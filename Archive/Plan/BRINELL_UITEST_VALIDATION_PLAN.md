# Brinell UI Test Validation Plan & Execution Guide

**Date**: January 2, 2026  
**Status**: Ready for Execution  
**Objective**: Run and validate all Brinell UI test projects with ZERO timeouts using intelligent wait patterns

---

## Executive Summary

This document outlines the complete validation strategy for Brinell's UI test framework across all platforms.

**Core Principle**: Brinell never waits with timeouts. Instead, it intelligently waits FOR SOMETHING specific:
- Wait for control to be visible/enabled
- Wait for element to have text
- Wait for page to be ready
- Wait for async operations to complete

All tests use the **FlaUI automation framework** with **Page Object Model (POM)** pattern and **intelligent wait-for-condition** strategies.

**Auto-Launch Pattern**: All tests automatically launch their sample applications via `UITestBase` initialization, eliminating manual process management.

**Test Execution Model**: Run tests sequentially per technology platform, ensuring all tests for one platform pass before moving to next.

---

## Test Project Inventory & Wait Pattern Implementation

### 1. Brinell.Samples.Wpf.UITests
**Platform**: WPF (Desktop)  
**Framework**: FlaUI + xUnit  
**Auto-Launch**: Yes (via UITestBase)  
**Key Wait Patterns**:
- `WaitVisible()` - Wait for control to become visible
- `WaitEnabled()` - Wait for control to be interactive
- `WaitForText()` - Wait for element to contain specific text
- `WaitPageReady()` - Wait for page state completion (NO timeout - waits indefinitely)

### 2. Brinell.Samples.WinForms.UITests
**Platform**: WinForms (Desktop)  
**Framework**: FlaUI + xUnit  
**Auto-Launch**: Yes (via UITestBase)  
**Key Wait Patterns**:
- TextBox: Wait for text entry to be accepted
- ComboBox: Wait for dropdown population before selection
- CheckBox: Wait for state change completion
- Button: Wait for response after click

### 3. Brinell.Samples.Blazor.UITests
**Platform**: Blazor (Web - Server-side)  
**Framework**: Custom web automation + xUnit  
**Auto-Launch**: Yes (embedded test server)  
**Key Wait Patterns**:
- `WaitComponentRender()` - Wait for Blazor component to render
- `WaitEventProcessed()` - Wait for event handler completion
- `WaitDataBound()` - Wait for data binding completion

### 4. Brinell.Samples.Blazor.PlaywrightTests
**Platform**: Blazor (Web - E2E Browser)  
**Framework**: Playwright + xUnit  
**Auto-Launch**: Yes (browser + server)  
**Key Wait Patterns**:
- `WaitForNavigation()` - Wait for page load
- `WaitForSelector()` - Wait for DOM element
- `WaitForFunction()` - Wait for JS condition

### 5. Brinell.Samples.Maui.UITests
**Platform**: MAUI (Cross-Platform)  
**Framework**: Custom MAUI automation + xUnit  
**Auto-Launch**: Yes (via UITestBase)  
**Key Wait Patterns**:
- `WaitViewLoaded()` - Wait for view lifecycle completion
- `WaitBindingApplied()` - Wait for MVVM binding
- `WaitAnimationComplete()` - Wait for animations to finish

---

## Pre-Execution Requirements

### System Requirements
✅ .NET 10.0 SDK installed  
✅ Windows 10/11 (for desktop platforms)  
✅ 4GB+ RAM available  
✅ No background automation tools interfering  

### Build Prerequisites
```bash
cd e:\repos\Private\Iosk\Oravey\Brinell

# Build sample apps and tests
dotnet build samples/Brinell.Samples.Wpf.App -c Debug
dotnet build samples/Brinell.Samples.Wpf.UITests -c Debug
dotnet build samples/Brinell.Samples.WinForms.App -c Debug
dotnet build samples/Brinell.Samples.WinForms.UITests -c Debug
dotnet build samples/Brinell.Samples.Maui.App -c Debug
dotnet build samples/Brinell.Samples.Maui.UITests -c Debug
dotnet build samples/Brinell.Samples.Blazor.App -c Debug
dotnet build samples/Brinell.Samples.Blazor.UITests -c Debug
```

### Environment Setup
```powershell
# Kill any leftover sample app processes
Get-Process -Name "Brinell.Samples.*" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 1
```

---

## Test Execution Strategy - Sequential Per Platform

**CRITICAL**: Tests run SEQUENTIALLY per platform. All tests for ONE platform must complete successfully BEFORE moving to the next. NO timeouts should occur - if they do, it's a bug in the wait pattern implementation.

### Platform 1: WPF (Desktop)
**Command**:
```powershell
cd e:\repos\Private\Iosk\Oravey\Brinell
dotnet test samples/Brinell.Samples.Wpf.UITests/Brinell.Samples.Wpf.UITests.csproj `
    -v minimal `
    --logger "console;verbosity=normal" `
    --no-build
```

**Expected Outcome**:
- ✅ 14+ tests execute
- ✅ 0 timeouts
- ✅ All pass or clearly indicate reason for skip
- ✅ Sample app auto-launches and auto-closes
- ⏱️ Total time: 10-20 seconds

**Success Criteria**: No "Timeout" in output

---

### Platform 2: WinForms (Desktop)
**Command**:
```powershell
cd e:\repos\Private\Iosk\Oravey\Brinell
dotnet test samples/Brinell.Samples.WinForms.UITests/Brinell.Samples.WinForms.UITests.csproj `
    -v minimal `
    --logger "console;verbosity=normal" `
    --no-build
```

**Expected Outcome**:
- ✅ 20+ tests execute
- ✅ 0 timeouts
- ✅ All control types tested (TextBox, ComboBox, CheckBox, Button, Label, ListBox)
- ⏱️ Total time: 10-20 seconds

**Success Criteria**: No "Timeout" in output

---

### Platform 3: MAUI (Cross-Platform)
**Command**:
```powershell
cd e:\repos\Private\Iosk\Oravey\Brinell
dotnet test samples/Brinell.Samples.Maui.UITests/Brinell.Samples.Maui.UITests.csproj `
    -v minimal `
    --logger "console;verbosity=normal" `
    --no-build
```

**Expected Outcome**:
- ✅ 12+ tests execute
- ✅ 0 timeouts
- ✅ Cross-platform controls validated
- ⏱️ Total time: 10-20 seconds

**Success Criteria**: No "Timeout" in output

---

### Platform 4: Blazor Web (ASP.NET Hosted)
**Command**:
```powershell
cd e:\repos\Private\Iosk\Oravey\Brinell
dotnet test samples/Brinell.Samples.Blazor.UITests/Brinell.Samples.Blazor.UITests.csproj `
    -v minimal `
    --logger "console;verbosity=normal" `
    --no-build
```

**Expected Outcome**:
- ✅ 10+ tests execute
- ✅ 0 timeouts
- ✅ Blazor components wait intelligently for render completion
- ⏱️ Total time: 10-20 seconds

**Success Criteria**: No "Timeout" in output

---

### Platform 5: Blazor Playwright (E2E Browser)
**Command**:
```powershell
cd e:\repos\Private\Iosk\Oravey\Brinell
dotnet test samples/Brinell.Samples.Blazor.PlaywrightTests/Brinell.Samples.Blazor.PlaywrightTests.csproj `
    -v minimal `
    --logger "console;verbosity=normal" `
    --no-build
```

**Expected Outcome**:
- ✅ 10+ tests execute
- ✅ 0 timeouts
- ✅ Browser automation with intelligent navigation waits
- ⏱️ Total time: 15-30 seconds

**Success Criteria**: No "Timeout" in output

---

## Detailed Execution Steps

### Step 1: Clean & Build
```powershell
cd e:\repos\Private\Iosk\Oravey\Brinell

# Clean
dotnet clean Brinell.sln -c Debug

# Restore
dotnet restore Brinell.sln

# Build
dotnet build Brinell.sln -c Debug --no-restore
```

**Expected Output**: All projects build successfully with 0 errors

---

### Step 2: WPF Platform Testing

**2a. Launch WPF Sample App**
```powershell
cd samples
Start-Process powershell -ArgumentList @(
    '-NoExit',
    '-Command',
    'cd e:\repos\Private\Iosk\Oravey\Brinell\samples; dotnet run --project Brinell.Samples.Wpf.App/Brinell.Samples.Wpf.App.csproj'
) -WindowStyle Normal
```

**2b. Run WPF Tests** (in separate terminal after app starts)
```powershell
cd e:\repos\Private\Iosk\Oravey\Brinell
dotnet test samples/Brinell.Samples.Wpf.UITests/Brinell.Samples.Wpf.UITests.csproj `
    -v minimal `
    --logger "console;verbosity=normal" `
    --no-build
```

**Expected Outcomes**:
- All test methods execute
- No timeout exceptions
- Form renders and responds to automation commands
- All control interactions succeed

---

### Step 3: WinForms Platform Testing

**3a. Launch WinForms Sample App**
```powershell
Start-Process powershell -ArgumentList @(
    '-NoExit',
    '-Command',
    'cd e:\repos\Private\Iosk\Oravey\Brinell\samples; dotnet run --project Brinell.Samples.WinForms.App/Brinell.Samples.WinForms.App.csproj'
) -WindowStyle Normal
```

**3b. Run WinForms Tests**
```powershell
cd e:\repos\Private\Iosk\Oravey\Brinell
dotnet test samples/Brinell.Samples.WinForms.UITests/Brinell.Samples.WinForms.UITests.csproj `
    -v minimal `
    --logger "console;verbosity=normal" `
    --no-build
```

**Expected Outcomes**:
- TextBox input/output operations
- ComboBox selection and item enumeration
- CheckBox state management
- Button click event handling
- Label text verification
- ListBox item manipulation

---

### Step 4: Blazor Platform Testing (Web)

**4a. Run Blazor UITests** (self-hosted)
```powershell
cd e:\repos\Private\Iosk\Oravey\Brinell
dotnet test samples/Brinell.Samples.Blazor.UITests/Brinell.Samples.Blazor.UITests.csproj `
    -v minimal `
    --logger "console;verbosity=normal" `
    --no-build
```

**4b. Run Blazor Playwright Tests** (browser automation)
```powershell
cd e:\repos\Private\Iosk\Oravey\Brinell
dotnet test samples/Brinell.Samples.Blazor.PlaywrightTests/Brinell.Samples.Blazor.PlaywrightTests.csproj `
    -v minimal `
    --logger "console;verbosity=normal" `
    --no-build
```

**Expected Outcomes**:
- Server auto-launches on random port
- Chromium browser launches automatically
- Component interaction succeeds
- Navigation works correctly
- Form validation passes
- Playwright assertions succeed

---

### Step 5: Stride Game Engine Testing

**5a. Launch Stride Sample App**
```powershell
Start-Process powershell -ArgumentList @(
    '-NoExit',
    '-Command',
    'cd e:\repos\Private\Iosk\Oravey\Brinell\samples; dotnet run --project Brinell.Samples.Stride.App/Brinell.Samples.Stride.App.csproj -- --automation'
) -WindowStyle Normal
```

**5b. Run Stride Tests** (wait 5 seconds for app startup)
```powershell
Start-Sleep -Seconds 5

cd e:\repos\Private\Iosk\Oravey\Brinell
dotnet test samples/Brinell.Samples.Stride.UITests/Brinell.Samples.Stride.UITests.csproj `
    -v minimal `
    --logger "console;verbosity=normal" `
    --no-build
```

**Expected Outcomes**:
- Game window opens in automation mode
- Named pipe "Brinell.Stride.Automation" created
- IPC communication succeeds
- Game state queries return valid data
- Automation protocol messages processed correctly

---

### Step 6: MAUI Platform Testing

**6a. Launch MAUI Sample App**
```powershell
Start-Process powershell -ArgumentList @(
    '-NoExit',
    '-Command',
    'cd e:\repos\Private\Iosk\Oravey\Brinell\samples; dotnet run --project Brinell.Samples.Maui.App/Brinell.Samples.Maui.App.csproj'
) -WindowStyle Normal
```

**6b. Run MAUI Tests**
```powershell
cd e:\repos\Private\Iosk\Oravey\Brinell
dotnet test samples/Brinell.Samples.Maui.UITests/Brinell.Samples.Maui.UITests.csproj `
    -v minimal `
    --logger "console;verbosity=normal" `
    --no-build
```

**Expected Outcomes**:
- MAUI app launches on Windows
- Cross-platform controls render correctly
- Touch/click events work identically
- Platform-specific features function
- Assertions pass consistently

---

## Validation Metrics

### Success Criteria
✅ All tests execute without timeout  
✅ Zero test failures across all platforms  
✅ All sample apps launch successfully  
✅ All automation connections succeed  
✅ All control interactions work as expected  
✅ Assertions validate correctly  

### Performance Benchmarks
| Platform | Expected Tests | Expected Time | Max Time |
|----------|---|---|---|
| WPF | 15-20 | 8-12s | 15s |
| WinForms | 20-25 | 10-15s | 20s |
| Blazor Web | 10-15 | 5-8s | 10s |
| Blazor Playwright | 10-15 | 8-12s | 15s |
| Stride | 8-12 | 15-25s | 30s |
| MAUI | 12-18 | 10-16s | 20s |
| **TOTAL** | **75-105** | **56-88s** | **110s** |

### Error Handling
| Error | Root Cause | Resolution |
|-------|-----------|-----------|
| Timeout | App not responsive | Increase Wait timeout, check app startup |
| Not Found | Element not visible | Verify AutomationId, check control visibility |
| Connection Failed | App crashed | Check startup logs, rebuild sample app |
| Port Conflict | Port already in use | Kill previous instances, use ephemeral port |
| Pipe Not Found | Game not in automation mode | Verify `--automation` flag passed to Stride |

---

## Cleanup Procedures

### After All Tests Complete
```powershell
# Kill all sample app instances
Get-Process -Name "Brinell.Samples.*" -ErrorAction SilentlyContinue | Stop-Process -Force

# Kill browser processes (Playwright)
Get-Process -Name "chrome" -ErrorAction SilentlyContinue | Stop-Process -Force

# Verify cleanup
Get-Process | Where-Object { $_.Name -like "Brinell*" }  # Should return nothing
```

---

## Expected Results

### Green Path (All Tests Pass)
```
Total Tests: 85-105
Passed: 85-105 ✅
Failed: 0 ✅
Skipped: 0
Total Time: 60-90 seconds ✅
Status: PRODUCTION READY ✅
```

### Failure Analysis (If Needed)
1. **Review error messages** for specific control name mismatches
2. **Check sample app logs** for runtime exceptions
3. **Verify automation IDs** match between app and tests
4. **Increase timeouts** if environment is slow
5. **Rebuild sample apps** if binaries are stale

---

## Troubleshooting Guide

### WPF App Won't Launch
```powershell
# Verify app executable exists
Test-Path "Brinell\samples\Brinell.Samples.Wpf.App\bin\Debug\net10.0-windows\Brinell.Samples.Wpf.App.exe"

# Try launching directly
& "Brinell\samples\Brinell.Samples.Wpf.App\bin\Debug\net10.0-windows\Brinell.Samples.Wpf.App.exe"
```

### Tests Can't Find Controls
```csharp
// In test, add debug output
var app = FlaUIDriver.GetApplication("Brinell.Samples.WinForms.App");
var root = app.GetMainWindow();
var allElements = root.FindAllDescendants();
foreach (var el in allElements)
{
    Console.WriteLine($"AutomationId: {el.Properties.AutomationId.Value}");
}
```

### Playwright Browser Not Launching
```powershell
# Ensure Playwright is installed
dotnet tool install Microsoft.Playwright.CLI

# Install browsers
pwsh bin\Debug\net10.0\playwright.ps1 install
```

### Stride Pipe Communication Fails
```powershell
# Verify pipe was created
Get-Item "\\.\pipe\" | Where-Object { $_.Name -like "*Stride*" }

# Check Stride app logs for errors
Get-Content "Brinell\samples\app_err*.txt"
```

---

## Integration with CI/CD

For automated validation:

```yaml
# Example GitHub Actions workflow
jobs:
  ui-tests:
    runs-on: windows-latest
    strategy:
      matrix:
        platform: [wpf, winforms, blazor, stride, maui]
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
      - run: dotnet build Brinell.sln
      - run: dotnet test samples/Brinell.Samples.${{ matrix.platform }}.UITests
```

---

## Sign-Off Checklist

- [ ] All prerequisites installed and verified
- [ ] Solution builds successfully
- [ ] WPF tests execute and pass
- [ ] WinForms tests execute and pass
- [ ] Blazor web tests execute and pass
- [ ] Blazor Playwright tests execute and pass
- [ ] Stride game tests execute and pass
- [ ] MAUI tests execute and pass
- [ ] All sample apps cleaned up
- [ ] Performance benchmarks within acceptable range
- [ ] No critical errors in logs
- [ ] Framework ready for production deployment

---

## Conclusion

This plan provides comprehensive coverage of all Brinell UI testing platforms with:
- ✅ Detailed execution steps for each platform
- ✅ Success criteria and validation metrics
- ✅ Troubleshooting procedures
- ✅ Performance benchmarks
- ✅ Cleanup and CI/CD integration guidance

**Expected Outcome**: All 85-105 UI tests passing across 5 platforms in under 110 seconds, validating the robustness of the Brinell automation framework.

