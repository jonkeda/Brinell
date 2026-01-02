# 17. Troubleshooting

**Parent:** [Documentation Index](21d0_UITestFramework_Index.md)  
**Code Examples:** [21d17_Troubleshooting_CodeExamples.md](21d17_Troubleshooting_CodeExamples.md)  
**Previous:** [Best Practices](21d16_BestPractices.md)  
**Version:** 3.0 (Updated December 2025)

---

## 17.1 Overview

This document covers common issues encountered in UI testing and their solutions.

---

## 17.2 Element Not Found Issues

### 17.2.1 Symptoms

- `ElementNotFoundException`
- `NoSuchElementException`
- `Element not found by AutomationId: xyz`

### 17.2.2 Causes & Solutions

| Cause | Solution |
|-------|----------|
| Wrong AutomationId | Verify with Inspect.exe or UIA tools |
| Element not loaded | Add `WaitForVisible()` before action |
| Element in different scope | Check element hierarchy |
| Page not ready | Use `WaitForPageReady()` after navigation |
| Element inside template | Use tree traversal |

### 17.2.3 Debugging Steps

1. **Use Inspect.exe** - Windows SDK tool to verify AutomationIds
2. **Add logging** - Log element search attempts
3. **Increase timeout** - May need more time to load
4. **Check element tree** - Verify element is in expected location

---

## 17.3 Timing and Synchronization Issues

### 17.3.1 Symptoms

- Tests pass locally, fail in CI
- Intermittent failures
- `TimeoutException`
- Race conditions

### 17.3.2 Solutions

| Issue | Solution |
|-------|----------|
| Element not ready | Use `CheckVisible()` before action |
| Page still loading | Implement `IsBusy` pattern |
| Animation in progress | Wait for animation to complete |
| Too short timeout | Increase timeout values |
| Too many parallel tests | Run UI tests sequentially |

### 17.3.3 IsBusy Pattern Fix

```csharp
// Before action that depends on loaded data
page.WaitForNotBusy();
page.DataGrid.Click();
```

---

## 17.4 Window Focus Issues

### 17.4.1 Symptoms

- Actions fail with window in background
- Click goes to wrong window
- Keyboard input goes elsewhere

### 17.4.2 Solutions

| Issue | Solution |
|-------|----------|
| Window not focused | Call `window.Focus()` before action |
| Dialog blocking | Handle dialog first |
| Multiple windows | Ensure correct window reference |
| Modal dialog | Wait for and handle modal |

### 17.4.3 Focus Fix

```csharp
// Ensure window has focus before interaction
MainWindow.Focus();
element.Click();
```

---

## 17.5 Application Launch Issues

### 17.5.1 Symptoms

- `Application not found`
- `Main window not found`
- Application hangs on startup

### 17.5.2 Solutions

| Issue | Solution |
|-------|----------|
| Wrong path | Use absolute path, verify exists |
| Missing dependencies | Check app.config, dependencies |
| Window timeout | Increase window wait timeout |
| App not starting | Check prerequisites (DB, services) |

### 17.5.3 Launch Debugging

```csharp
var appPath = GetApplicationPath();

if (!File.Exists(appPath))
{
    throw new FileNotFoundException($"App not found at: {appPath}");
}

Logger.LogInfo(TestName, "Launch", $"Starting: {appPath}");
App = Application.Launch(appPath);

// Extended wait for main window
MainWindow = App.GetMainWindow(Automation, TimeSpan.FromSeconds(60));

if (MainWindow == null)
{
    throw new InvalidOperationException("Main window not found after 60s");
}
```

---

## 17.6 CI/CD Specific Issues

### 17.6.1 Symptoms

- Works locally, fails in CI
- Screen resolution issues
- Missing fonts/DPI problems

### 17.6.2 Solutions

| Issue | Solution |
|-------|----------|
| Different screen size | Use relative coordinates |
| No display | Use virtual display or headless |
| Different DPI | Set consistent DPI in test setup |
| Font rendering | Accept small visual differences |
| Environment variables | Verify all env vars set in CI |

### 17.6.3 CI Configuration

```yaml
# Ensure consistent environment
env:
  PLATFORM: Windows
  APP_PATH: ${{ github.workspace }}/app/MyApp.exe
  LOG_OUTPUT_PATH: ${{ github.workspace }}/logs
```

---

## 17.7 Mock Server Issues

### 17.7.1 Symptoms

- Mock server not starting
- Stubs not matching requests
- Port conflicts

### 17.7.2 Solutions

| Issue | Solution |
|-------|----------|
| Port in use | Use dynamic port allocation |
| Stub not matching | Verify exact path/method |
| Server not started | Add null check and start logic |
| Timeout on cloud | Increase network timeouts |

### 17.7.3 Mock Server Debug

```csharp
// Debug stub registration
MockServer.Stub(stub =>
{
    Logger.LogInfo(TestName, "Mock", $"Registering: {path}");
    stub.WithPath(path).WithMethod("GET").ReturnsStatus(200);
});

// Verify stub was hit
MockServer.VerifyCallMade("/api/users");
```

---

## 17.8 Control-Specific Issues

### 17.8.1 DataGrid Issues

| Issue | Solution |
|-------|----------|
| Can't find cells | Wait for grid populated |
| Wrong row index | Account for header row |
| Scroll needed | Scroll to row before action |

### 17.8.2 ComboBox Issues

| Issue | Solution |
|-------|----------|
| Items not visible | Expand dropdown first |
| Can't select item | Use value pattern |
| List not populated | Wait for items loaded |

### 17.8.3 CheckBox Issues

| Issue | Solution |
|-------|----------|
| Can't toggle | Use Toggle pattern, not click |
| State not changing | Verify enabled state |

---

## 17.9 Debugging Tools

### 17.9.1 Windows Tools

| Tool | Purpose |
|------|---------|
| **Inspect.exe** | UI Automation tree viewer |
| **AccessibilityInsights** | Accessibility testing |
| **UIA Verify** | Automation pattern testing |
| **Spy++** | Window message viewer |

### 17.9.2 Debugging Commands

```powershell
# Run with verbose logging
dotnet test --logger "console;verbosity=diagnostic"

# Run with blame (crash detection)
dotnet test --blame --blame-hang --blame-hang-timeout 5m

# Run single test with output
dotnet test --filter "FullyQualifiedName~MyTest" --verbosity detailed
```

### 17.9.3 Code Debugging

```csharp
// Add temporary debug output
Logger.LogInfo(TestName, "Debug", $"Element visible: {element.IsVisible}");
Logger.LogInfo(TestName, "Debug", $"Page busy: {page.IsBusy()}");

// Take screenshot at specific point
TakeScreenshot("before_click");
button.Click();
TakeScreenshot("after_click");
```

---

## 17.10 Common Error Messages

### 17.10.1 FlaUI Errors

| Error | Meaning | Fix |
|-------|---------|-----|
| `ElementNotAvailableException` | Element removed from tree | Re-find element |
| `InvalidOperationException: Pattern not supported` | Control doesn't support pattern | Use correct control type |
| `TimeoutException` | Wait exceeded | Increase timeout or fix timing |

### 17.10.2 Appium Errors

| Error | Meaning | Fix |
|-------|---------|-----|
| `NoSuchElementException` | Element not found | Verify locator |
| `StaleElementReferenceException` | Element reference stale | Re-find element |
| `SessionNotCreatedException` | Driver session failed | Check app path, capabilities |

### 17.10.3 Selenium Errors

| Error | Meaning | Fix |
|-------|---------|-----|
| `NoSuchElementException` | Element not in DOM | Verify selector |
| `ElementNotInteractableException` | Element not visible/enabled | Wait for interactable |
| `WebDriverException` | Driver communication failed | Restart browser |

---

## 17.11 Performance Issues

### 17.11.1 Slow Tests

| Cause | Solution |
|-------|----------|
| Too many waits | Use smarter waits |
| Large page objects | Lazy initialization |
| Repeated app launch | Use shared fixture |
| Screenshot overhead | Only on failure |

### 17.11.2 Memory Issues

| Cause | Solution |
|-------|----------|
| Not disposing context | Implement proper Dispose |
| Screenshot accumulation | Clean up old screenshots |
| Large log files | Rotate logs |

---

*This concludes the UI Testing Framework documentation.*
