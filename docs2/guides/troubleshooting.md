# Troubleshooting

Common issues and solutions for UI testing.

---

## Element Not Found

### Symptoms

```
ElementNotFoundException: Element 'SaveButton' not found
NoSuchElementException: Unable to locate element
Element not found by AutomationId: txtUsername
```

### Common Causes and Solutions

| Cause | How to Identify | Solution |
|-------|----------------|----------|
| **Wrong AutomationId** | Check XAML/HTML | Use Inspect.exe (Windows) or browser DevTools |
| **Element not loaded** | Timing issue | Add `WaitForVisible()` or `WaitForPageReady()` |
| **Element in different scope** | Hierarchy issue | Verify parent element is correct |
| **Page not ready** | Race condition | Use `page.WaitForPageReady()` |
| **Dynamic content** | Generated elements | Wait for container, then find element |

### Debugging Steps

1. **Verify AutomationId exists**
   ```csharp
   // Use Windows SDK Inspect.exe to verify:
   // - Element exists
   // - AutomationId is correct
   // - Element is in expected location
   ```

2. **Add diagnostic logging**
   ```csharp
   Logger.LogInfo(TestName, "Debug", $"Looking for element: {automationId}");
   var exists = Context.ElementExists(automationId);
   Logger.LogInfo(TestName, "Debug", $"Element exists: {exists}");
   ```

3. **Increase timeout**
   ```csharp
   // Element may need more time to appear
   element.WaitVisible(true, timeoutMs: 30000);
   ```

4. **Take screenshot**
   ```csharp
   Context.TakeScreenshot("before_element_search");
   ```

---

## Timing and Synchronization

### Symptoms

- Tests pass locally but fail in CI
- Intermittent failures
- `TimeoutException`
- Race conditions

### Solutions by Symptom

#### Intermittent Failures

```csharp
// ❌ Bad - doesn't wait for page ready
home.NavigateToSettings();
var settings = new SettingsPage(Context);
settings.SaveButton.Click();  // May fail

// ✅ Good - waits for page ready
home.NavigateToSettings();
var settings = new SettingsPage(Context);
settings.WaitForPageReady();  // Waits for IsBusy = false
settings.SaveButton.Click();
```

#### Element Not Interactable

```csharp
// ❌ Bad - element may be covered by loading overlay
button.Click();

// ✅ Good - wait for page not busy
page.WaitForNotBusy();
button.Click();
```

#### Stale Element Reference

```csharp
// ❌ Bad - element reference becomes stale after page refresh
var button = page.SaveButton;
page.RefreshData();  // Page updates
button.Click();  // Stale element error!

// ✅ Good - access element after update
page.RefreshData();
page.WaitForNotBusy();
page.SaveButton.Click();  // Fresh element reference
```

---

## Window Focus Issues

### Symptoms

- Click goes to wrong window
- Keyboard input doesn't work
- Actions fail with "element not interactable"
- Actions fail with `WindowsInteractionPolicyException`

### Solutions

```csharp
// ✅ Handle modal dialogs blocking main window
if (Context.HasModalWindow())
{
    var dialog = new MessageBoxDialog(Context);
    dialog.Close();
}

// ✅ Switch between multiple windows
var windows = App.GetAllTopLevelWindows();
var settingsWindow = windows.First(w => w.Name == "Settings");
settingsWindow.Focus();
```

For MAUI/FlaUI on Windows, the default is semantic mode. Semantic mode intentionally does not bring the app to the foreground or use the real mouse, keyboard, or clipboard.

Prefer semantic operations:

```csharp
page.NameEntry.SetText("Alice");   // ValuePattern when available
page.SaveButton.Click();           // Invoke/SelectionItem/LegacyIAccessible first
```

If an action reports that pointer, keyboard, clipboard, or foreground activation is disabled, either expose a semantic UI Automation surface in the app or run that specific suite in interactive mode:

```powershell
$env:BRINELL_WINDOWS_INTERACTION_MODE = "interactive"
```

Granular overrides are also available:

```powershell
$env:BRINELL_ALLOW_FOREGROUND_ACTIVATION = "true"
$env:BRINELL_ALLOW_POINTER_INPUT = "true"
$env:BRINELL_ALLOW_GLOBAL_KEYBOARD_INPUT = "true"
$env:BRINELL_ALLOW_CLIPBOARD_INPUT = "true"
```

Do not use raw Win32 `SendMessage` as the default workaround for MAUI/WinUI click issues. Many MAUI/WinUI controls are not individual HWND-backed controls, so UI Automation patterns or improved app accessibility surfaces are more reliable.

---

## Application Launch Issues

### Symptoms

```
Application not found at: C:\path\to\app.exe
Main window not found after 30 seconds
Application hangs on startup
```

### Solutions

#### Wrong Path

```csharp
// ✅ Verify path exists
var appPath = Configuration["ApplicationPath"];
if (!File.Exists(appPath))
{
    throw new FileNotFoundException($"Application not found: {appPath}");
}
Logger.LogInfo(TestName, "Launch", $"Starting: {appPath}");
```

#### Missing Dependencies

```csharp
// Check for required dependencies before launch
if (!File.Exists("config.json"))
{
    throw new FileNotFoundException("config.json missing");
}

// Check for required services
if (!IsServiceRunning("MyAppService"))
{
    throw new InvalidOperationException("Required service not running");
}
```

#### Timeout Waiting for Window

```csharp
// Increase timeout for slow startup
Context.MainWindow = App.GetMainWindow(
    Automation, 
    timeout: TimeSpan.FromSeconds(60)
);

if (Context.MainWindow == null)
{
    throw new InvalidOperationException(
        "Main window not found after 60 seconds"
    );
}
```

---

## CI/CD Specific Issues

### Tests Pass Locally, Fail in CI

| Issue | Detection | Solution |
|-------|-----------|----------|
| **Different screen resolution** | Check CI machine specs | Use relative positioning |
| **No display/headless** | CI logs show display errors | Use virtual display or headless mode |
| **Different DPI** | Font rendering differences | Set consistent DPI in CI |
| **Missing environment variables** | Configuration errors | Verify env vars in CI config |
| **File path differences** | Path not found errors | Use path helpers, avoid hardcoded paths |

### CI Configuration Example

```yaml
# GitHub Actions
jobs:
  ui-tests:
    runs-on: windows-latest
    env:
      UITEST_PLATFORM: Windows
      UITEST_TIMEOUT: 30000
      UITEST_LOG_OUTPUT: both
      APP_PATH: ${{ github.workspace }}/app/MyApp.exe
      
    steps:
      - name: Run UI Tests
        run: dotnet test --verbosity normal
        
      - name: Upload Screenshots on Failure
        if: failure()
        uses: actions/upload-artifact@v3
        with:
          name: test-screenshots
          path: '**/screenshots/*.png'
```

---

## Control-Specific Issues

### DataGrid/Table Issues

```csharp
// ❌ Bad - assuming immediate population
var count = dataGrid.GetRowCount();

// ✅ Good - wait for data loaded
page.WaitForNotBusy();
Context.WaitFor(() => dataGrid.GetRowCount() > 0, 10000);
var count = dataGrid.GetRowCount();
```

### ComboBox/Dropdown Issues

```csharp
// ❌ Bad - trying to select from closed dropdown
comboBox.SelectItem("Option 1");

// ✅ Good - expand first (if needed)
if (!comboBox.IsExpanded())
{
    comboBox.Expand();
}
comboBox.WaitForItemsLoaded();
comboBox.SelectItem("Option 1");
```

### CheckBox Issues

```csharp
// ❌ Bad - clicking may not toggle
checkbox.Click();

// ✅ Good - use toggle pattern
checkbox.Toggle();

// ✅ Better - set to specific state
checkbox.SetChecked(true);
```

---

## Debugging Tools

### Windows Tools

| Tool | Purpose | Download |
|------|---------|----------|
| **Inspect.exe** | View UI Automation tree | Windows SDK |
| **Accessibility Insights** | Accessibility testing | Microsoft |
| **UIA Verify** | Test automation patterns | Microsoft |
| **Spy++** | Window message viewer | Visual Studio |
| **FlaUI Inspect** | FlaUI-specific inspector | FlaUI GitHub |

### Using Inspect.exe

1. Start Inspect.exe
2. Launch your application
3. Use Ctrl+Shift to inspect elements
4. Verify:
   - AutomationId is correct
   - Element is in expected hierarchy
   - Control patterns are available

### Debugging in Code

```csharp
// Add temporary diagnostic output
Logger.LogInfo(TestName, "Debug", $"Element visible: {element.IsVisible()}");
Logger.LogInfo(TestName, "Debug", $"Element enabled: {element.IsEnabled()}");
Logger.LogInfo(TestName, "Debug", $"Page busy: {page.IsBusy()}");

// Take screenshots at key points
Context.TakeScreenshot("before_action");
button.Click();
Context.TakeScreenshot("after_action");

// Dump element properties
var element = Context.FindElement("SaveButton");
Logger.LogInfo(TestName, "Debug", $"Name: {element.Name}");
Logger.LogInfo(TestName, "Debug", $"ClassName: {element.ClassName}");
Logger.LogInfo(TestName, "Debug", $"IsOffscreen: {element.IsOffscreen}");
```

---

## Running Tests with Diagnostics

### Verbose Logging

```bash
# Console verbosity
dotnet test --logger "console;verbosity=detailed"

# Enable framework debug logging
$env:UITEST_LOG_OUTPUT = "both"
$env:UITEST_CONSOLE_FORMAT = "formatted"
dotnet test
```

### Blame Mode (Crash Detection)

```bash
# Detect hanging or crashing tests
dotnet test --blame --blame-hang --blame-hang-timeout 5m

# Generate crash dump on failure
dotnet test --blame --blame-crash
```

### Run Single Test

```bash
# Run specific test with full output
dotnet test --filter "FullyQualifiedName~MyTest" --verbosity detailed

# Run test class
dotnet test --filter "FullyQualifiedName~NavigationTests"
```

---

## Common Error Messages

### FlaUI Errors

| Error | Meaning | Solution |
|-------|---------|----------|
| `ElementNotAvailableException` | Element removed from tree | Re-find element |
| `InvalidOperationException: Pattern not supported` | Control doesn't support pattern | Use correct control type |
| `TimeoutException` | Wait exceeded | Increase timeout or check element exists |
| `COMException` | UI Automation failure | Restart application |

### Appium Errors

| Error | Meaning | Solution |
|-------|---------|----------|
| `NoSuchElementException` | Element not found | Verify locator/accessibility ID |
| `StaleElementReferenceException` | Element reference invalid | Re-find element |
| `SessionNotCreatedException` | Driver session failed | Check Appium server, app path |
| `InvalidSelectorException` | Bad locator strategy | Use correct locator type |
| `UnknownMethodException` | Driver doesn't support API | See platform-specific limitations below |

### Windows Driver Limitations

The Windows Application Driver doesn't implement all W3C WebDriver APIs:

| API | Status | Workaround |
|-----|--------|------------|
| `GET /timeouts` | ❌ Not supported | Store timeout values locally instead of reading `Timeouts.ImplicitWait` |
| `SET /timeouts` | ✅ Supported | Setting timeouts works normally |

**Common symptom:** Element finding silently fails because exception is caught:

```csharp
// This throws UnknownMethodException on Windows:
var timeout = driver.Manage().Timeouts().ImplicitWait;  // ❌ Fails!

// Use stored value instead:
var timeout = _storedTimeoutValue;  // ✅ Works
```

**Debugging tip:** Run Appium with `--log-level debug` to see actual HTTP requests. If no `/element` requests appear, an exception is being silently swallowed.

### Selenium Errors

| Error | Meaning | Solution |
|-------|---------|----------|
| `NoSuchElementException` | Element not in DOM | Verify selector, wait for element |
| `ElementNotInteractableException` | Element not visible/enabled | Wait for element ready |
| `WebDriverException` | Browser communication failed | Restart browser/driver |
| `StaleElementReferenceException` | DOM updated | Re-find element |

---

## Performance Issues

### Slow Test Execution

| Cause | Detection | Solution |
|-------|-----------|----------|
| Too many waits | Tests take >> expected time | Use smarter waits, reduce timeout |
| Large page objects | High memory usage | Lazy-initialize controls |
| Repeated app launch | Each test starts app | Use shared fixture |
| Screenshot overhead | Slow test completion | Only screenshot on failure |
| Network delays | API tests slow | Use mocked APIs |

### Example: Shared Fixture

```csharp
// Define collection fixture
[CollectionDefinition("App Collection")]
public class AppCollection : ICollectionFixture<AppFixture>
{
}

// Fixture class (app launched once)
public class AppFixture : IDisposable
{
    public FlaUITestContext Context { get; }
    
    public AppFixture()
    {
        Context = CreateContext();
        // App launched once for all tests
    }
    
    public void Dispose()
    {
        Context?.Dispose();
    }
}

// Use in tests
[Collection("App Collection")]
public class FastTests
{
    private readonly AppFixture _fixture;
    
    public FastTests(AppFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public void Test_Uses_Shared_App()
    {
        // App already running from fixture
        var page = new MainPage(_fixture.Context);
        page.WaitForPageReady();
    }
}
```

---

## Memory Issues

### Memory Leaks

```csharp
// ✅ Always dispose context
public override void Dispose()
{
    try
    {
        CleanupTestData();
    }
    finally
    {
        Context?.Dispose();  // Critical!
        base.Dispose();
    }
}

// ✅ Clean up screenshots
[OneTimeTearDown]
public void GlobalCleanup()
{
    // Delete screenshots older than 7 days
    var screenshotDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "screenshots");
    var oldFiles = Directory.GetFiles(screenshotDir)
        .Where(f => File.GetCreationTime(f) < DateTime.Now.AddDays(-7));
    
    foreach (var file in oldFiles)
    {
        File.Delete(file);
    }
}
```

---

## Quick Diagnostic Checklist

When a test fails, check:

1. **Is the AutomationId correct?**
   - Use Inspect.exe to verify

2. **Did you wait for page ready?**
   - Add `page.WaitForPageReady()` after navigation

3. **Is there a busy indicator?**
   - Check `page.IsBusy()` before interaction

4. **Is the element visible and enabled?**
   - Use `element.AssertClickable()`

5. **Are you using the right timeout?**
   - Increase timeout for slow operations

6. **Is this a timing issue?**
   - Add waits instead of `Thread.Sleep()`

7. **Did the application start correctly?**
   - Check logs for startup errors

8. **Are there modal dialogs?**
   - Check for and handle unexpected dialogs

9. **Is the window focused?**
   - Call `window.Focus()` before interaction

10. **Is this environment-specific?**
    - Compare local vs. CI environment settings

---

## Getting Help

### Information to Provide

When reporting issues, include:

1. **Error message** (full stack trace)
2. **Test code** (minimal reproducible example)
3. **Page object code**
4. **Application XAML/HTML** (for the element)
5. **Screenshot** (if available)
6. **Log output** (CSV log entries)
7. **Environment** (OS, framework versions, CI/local)

### Example Bug Report

```
**Issue**: Element not found after navigation

**Error**:
```
ElementNotFoundException: Element 'SaveButton' not found by AutomationId
  at ControlBase.GetElement()
  at ButtonControl.Click()
```

**Test Code**:
```csharp
[Fact]
public void Test_Save()
{
    var home = LaunchApp<HomePage>();
    home.NavigateToEditor();
    var editor = new EditorPage(Context);
    editor.SaveButton.Click();  // Fails here
}
```

**Environment**:
- Windows 11
- .NET 9.0
- FlaUI 4.0.0
- Running in: GitHub Actions

**Already Tried**:
- Verified AutomationId with Inspect.exe ✓
- Added WaitForPageReady() ✓
- Increased timeout to 30s ✗ (still fails)

**Screenshots**: [attached]
**Logs**: [attached]
```

---

*See also: [Best Practices](12-best-practices.md) | [Test Writing Guide](15-test-writing-guide.md)*
