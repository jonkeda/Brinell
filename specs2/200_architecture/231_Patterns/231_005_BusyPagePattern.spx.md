# 231_005 Busy Page Pattern

## pattern BusyPage

- **title**: Busy Page Pattern
- **type**: Behavioral
- **purpose**: Track and wait for page loading/busy states during async operations

---

## Description

The Busy Page pattern extends the Page Object pattern to handle asynchronous loading states. Pages implementing this pattern can detect when they are busy (showing loading indicators) and provide methods to wait for operations to complete before proceeding with tests.

> **Note:** Code snippets in this document are illustrative examples showing architectural patterns. Actual implementation may vary. See source code for current implementation details.

---

## 1. Intent

**Problem:** Asynchronous operations cause:
- Tests that proceed before data loads
- Flaky assertions on incomplete UI states
- Race conditions between test actions and async updates
- No clear way to know when operations complete

**Solution:** Create busy-aware page objects that:
- Detect loading indicators automatically
- Wait for busy state to clear before proceeding
- Integrate busy checks into page readiness
- Provide assertions for busy state

---

## 2. Structure

### 2.1 Participants

| Participant | Role |
|-------------|------|
| IBusyPageObject | Interface for busy state tracking |
| BusyPageBase | Abstract base implementing busy behavior |
| DataPage | Concrete page with loading states |
| BusyIndicator | Control that indicates loading (spinner, overlay) |

### 2.2 Class Hierarchy

```
IPageObject
└── IBusyPageObject
    └── BusyPageBase (abstract)
        └── DataLoadingPage (concrete)
```

---

## 3. Implementation

### 3.1 IBusyPageObject Interface

```csharp
/// <summary>
/// Interface for page objects that track busy/loading state.
/// Use for pages that display loading indicators during async operations.
/// </summary>
public interface IBusyPageObject : IPageObject
{
    /// <summary>
    /// Check if the page is currently busy (showing loading indicator).
    /// </summary>
    /// <returns>True if the page is busy.</returns>
    bool IsBusy();
    
    /// <summary>
    /// Wait for the page to not be busy.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>True if page became not busy within timeout.</returns>
    bool WaitForNotBusy(int? timeoutMs = null);
    
    /// <summary>
    /// Assert the page is not busy. Captures screenshot on failure.
    /// </summary>
    /// <param name="message">Optional custom assertion message.</param>
    void AssertNotBusy(string? message = null);
}
```

### 3.2 BusyPageBase Abstract Class

```csharp
/// <summary>
/// Page base class that includes busy indicator tracking.
/// Implements IBusyPageObject for cross-platform busy state tracking.
/// </summary>
public abstract class BusyPageBase : PageBase, IBusyPageObject
{
    /// <summary>
    /// The busy indicator control.
    /// Override in derived class to specify the indicator element.
    /// Return null if page doesn't have a busy indicator.
    /// </summary>
    protected virtual IControlObject? BusyIndicator => null;
    
    protected BusyPageBase(ITestContext context) : base(context)
    {
    }
    
    /// <summary>
    /// Check if the page is currently busy (showing loading indicator).
    /// Returns false if no BusyIndicator is defined.
    /// </summary>
    public virtual bool IsBusy()
    {
        if (BusyIndicator == null)
            return false;
        
        return BusyIndicator.IsVisible() == true;
    }
    
    /// <summary>
    /// Wait for the page to not be busy.
    /// Returns true immediately if no BusyIndicator is defined.
    /// </summary>
    public virtual bool WaitForNotBusy(int? timeoutMs = null)
    {
        if (BusyIndicator == null)
            return true;
        
        var timeout = timeoutMs ?? _context.DefaultTimeoutMs;
        Log($"WaitForNotBusy (timeout: {timeout}ms)");
        return BusyIndicator.WaitVisible(false, timeout);
    }
    
    /// <summary>
    /// Assert the page is not busy.
    /// Captures screenshot on failure.
    /// </summary>
    public virtual void AssertNotBusy(string? message = null)
    {
        if (IsBusy())
        {
            ThrowPageNotReady("AssertNotBusy", 
                message ?? $"Expected page '{Name}' to not be busy but it is currently busy.");
        }
    }
    
    /// <summary>
    /// Page is ready when displayed AND not busy.
    /// Override to add additional ready conditions.
    /// </summary>
    public override bool IsReady()
    {
        return base.IsReady() && !IsBusy();
    }
}
```

### 3.3 Platform-Specific Implementations

**MAUI (Appium):**

```csharp
public abstract class MauiBusyPageBase : MauiPageBase, IBusyPageObject
{
    protected virtual IControlObject? BusyIndicator => null;
    
    protected MauiBusyPageBase(AppiumTestContext context) : base(context) { }
    
    public virtual bool IsBusy()
    {
        if (BusyIndicator == null)
            return false;
        
        return BusyIndicator.IsVisible() == true;
    }
    
    public virtual bool WaitForNotBusy(int? timeoutMs = null)
    {
        if (BusyIndicator == null)
            return true;
        
        var timeout = timeoutMs ?? _context.DefaultTimeoutMs;
        Log($"WaitForNotBusy (timeout: {timeout}ms)");
        return BusyIndicator.WaitVisible(false, timeout);
    }
    
    public virtual void AssertNotBusy(string? message = null)
    {
        if (IsBusy())
        {
            ThrowPageNotReady("AssertNotBusy", 
                message ?? $"Expected page '{Name}' to not be busy.");
        }
    }
    
    public override bool IsReady() => base.IsReady() && !IsBusy();
}
```

**Blazor (Playwright):**

```csharp
public abstract class BlazorBusyPageBase : BlazorPageBase, IBusyPageObject
{
    protected virtual IControlObject? BusyIndicator => null;
    
    protected BlazorBusyPageBase(PlaywrightTestContext context) : base(context) { }
    
    public virtual bool IsBusy()
    {
        if (BusyIndicator == null)
            return false;
        
        return BusyIndicator.IsVisible() == true;
    }
    
    public virtual bool WaitForNotBusy(int? timeoutMs = null)
    {
        if (BusyIndicator == null)
            return true;
        
        var timeout = timeoutMs ?? _context.DefaultTimeoutMs;
        Log($"WaitForNotBusy (timeout: {timeout}ms)");
        return BusyIndicator.WaitVisible(false, timeout);
    }
    
    public virtual void AssertNotBusy(string? message = null)
    {
        if (IsBusy())
        {
            ThrowPageNotReady("AssertNotBusy", 
                message ?? $"Expected page '{Name}' to not be busy.");
        }
    }
    
    public override bool IsReady() => base.IsReady() && !IsBusy();
}
```

---

## 4. Usage

### 4.1 Define Busy Page

```csharp
public class DataDashboardPage : BusyPageBase
{
    public DataDashboardPage(ITestContext context) : base(context) { }
    
    // Specify the busy indicator as a control
    protected override IControlObject? BusyIndicator => LoadingSpinner;
    
    // Page controls
    public ActivityIndicatorControl LoadingSpinner => new(_context, "LoadingSpinner", this);
    public LabelControl DataCount => new(_context, "DataCount", this);
    public ButtonControl RefreshButton => new(_context, "RefreshButton", this);
    public ContainerControl DataGrid => new(_context, "DataGrid", this);
}
```

### 4.2 Wait for Data Load

```csharp
[Fact]
public void Dashboard_OnLoad_DisplaysData()
{
    var dashboard = new DataDashboardPage(_context);
    
    // Wait for initial data load
    dashboard.WaitForNotBusy();
    
    // Now safe to check data
    dashboard.DataCount.AssertTextContains("10 items");
}
```

### 4.3 Wait After Action

```csharp
[Fact]
public void Dashboard_Refresh_UpdatesData()
{
    var dashboard = new DataDashboardPage(_context);
    dashboard.WaitForNotBusy();
    
    // Trigger async operation
    dashboard.RefreshButton.Click();
    
    // Wait for operation to complete
    dashboard.WaitForNotBusy();
    
    // Verify updated state
    dashboard.DataCount.AssertTextContains("Updated");
}
```

### 4.4 Use IsReady for Navigation

```csharp
[Fact]
public void Navigate_ToDataPage_WaitsForLoad()
{
    var homePage = new HomePage(_context);
    homePage.DataLink.Click();
    
    var dataPage = new DataDashboardPage(_context);
    
    // WaitForPage checks IsReady which includes !IsBusy()
    dataPage.WaitForPage();
    
    // Page is loaded AND not busy
    dataPage.DataGrid.AssertExists();
}
```

### 4.5 Assert Not Busy

```csharp
[Fact]
public void LongOperation_Completes_WithinTimeout()
{
    var page = new DataDashboardPage(_context);
    
    page.StartLongOperation.Click();
    
    // Will throw with screenshot if still busy after timeout
    var completed = page.WaitForNotBusy(timeoutMs: 30000);
    
    Assert.True(completed, "Operation did not complete within 30 seconds");
    
    // Or assert immediately
    page.AssertNotBusy("Expected operation to be complete");
}
```

---

## 5. Advanced Patterns

### 5.1 Multiple Busy Indicators

For pages with multiple loading regions:

```csharp
public class ComplexDashboardPage : PageBase, IBusyPageObject
{
    public ComplexDashboardPage(ITestContext context) : base(context) { }
    
    // Multiple indicators
    private bool IsHeaderLoading => _context.ElementIsVisible("HeaderSpinner");
    private bool IsGridLoading => _context.ElementIsVisible("GridSpinner");
    private bool IsChartLoading => _context.ElementIsVisible("ChartSpinner");
    
    public bool IsBusy() => IsHeaderLoading || IsGridLoading || IsChartLoading;
    
    public bool WaitForNotBusy(int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? _context.DefaultTimeoutMs;
        return _context.WaitFor(() => !IsBusy(), timeout, "page not busy");
    }
    
    public void AssertNotBusy(string? message = null)
    {
        if (IsBusy())
            ThrowPageNotReady("AssertNotBusy", message ?? "Page is still loading.");
    }
    
    // Wait for specific region
    public bool WaitForGridReady(int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? _context.DefaultTimeoutMs;
        return _context.WaitFor(() => !IsGridLoading, timeout, "grid loaded");
    }
}
```

### 5.2 Busy Indicator as Control

```csharp
public class DataPage : PageBase
{
    public DataPage(ITestContext context) : base(context) { }
    
    // Busy indicator as a control
    public ActivityIndicatorControl LoadingSpinner => new(_context, "LoadingSpinner", this);
    
    public bool IsBusy() => LoadingSpinner.IsVisible() == true;
    
    public bool WaitForNotBusy(int? timeoutMs = null)
    {
        return LoadingSpinner.WaitVisible(false, timeoutMs);
    }
}
```

### 5.3 Custom Busy Detection

For complex busy states (network requests, animations):

```csharp
public class AjaxPage : BusyPageBase
{
    protected override string? BusyIndicatorId => null;  // No visual indicator
    
    public AjaxPage(ITestContext context) : base(context) { }
    
    /// <summary>
    /// Custom busy detection - check for pending network requests.
    /// </summary>
    public override bool IsBusy()
    {
        // Execute JavaScript to check pending requests
        var pendingRequests = _context.ExecuteScript<int>(
            "return window.pendingAjaxRequests || 0;");
        return pendingRequests > 0;
    }
}
```

---

## 6. When to Use

### 6.1 Use IBusyPageObject When

| Scenario | Example |
|----------|---------|
| Page loads data asynchronously | Dashboard with API data |
| Actions trigger background work | Save button, refresh |
| Page has loading overlay | Modal spinner during submission |
| Need to wait for animations | Transition completes |

### 6.2 Use Regular PageBase When

| Scenario | Example |
|----------|---------|
| Static content pages | About, Help pages |
| Synchronous operations only | Form with client validation |
| Busy state not observable | No visual indicator |
| Busy detection too complex | Multiple unrelated operations |

---

## 7. Integration with Page Lifecycle

### 7.1 IsReady Flow

```
IsReady()
├── base.IsReady()
│   ├── IsDisplayed()           // Page element visible
│   └── WaitForPage control     // Key control exists
└── !IsBusy()                   // Not showing loader
```

### 7.2 WaitForPage Behavior

```csharp
// BusyPageBase.WaitForPage() waits until:
// 1. Page is displayed
// 2. Key controls exist  
// 3. Busy indicator is hidden

page.WaitForPage();  // All conditions met
```

---

## 8. Anti-Patterns

### 8.1 Don't Use Fixed Delays

```csharp
// ❌ BAD: Arbitrary sleep
page.RefreshButton.Click();
Thread.Sleep(3000);
page.DataLabel.AssertTextEquals("Updated");

// ✅ GOOD: Wait for busy state
page.RefreshButton.Click();
page.WaitForNotBusy();
page.DataLabel.AssertTextEquals("Updated");
```

### 8.2 Don't Ignore Busy State

```csharp
// ❌ BAD: Proceed without waiting
var page = new DataPage(_context);
page.DataGrid.AssertExists();  // Might fail if still loading

// ✅ GOOD: Wait for page ready
var page = new DataPage(_context);
page.WaitForPage();  // Includes !IsBusy() check
page.DataGrid.AssertExists();
```

### 8.3 Don't Bypass Control Pattern

```csharp
// ❌ BAD: Using raw string IDs
if (_context.ElementIsVisible("LoadingSpinner")) { }
_context.WaitFor(() => !_context.ElementIsVisible("LoadingSpinner"));

// ✅ GOOD: Use control object
protected override IControlObject? BusyIndicator => LoadingSpinner;
public ActivityIndicatorControl LoadingSpinner => new(_context, "LoadingSpinner", this);
page.WaitForNotBusy();
```

---

## 9. Validation Rules

The Busy Page pattern is valid when:

- [ ] Pages with async operations implement IBusyPageObject
- [ ] BusyIndicator returns a control object (not string ID)
- [ ] IsReady() includes !IsBusy() check
- [ ] Tests use WaitForNotBusy() instead of fixed delays
- [ ] AssertNotBusy captures screenshots on failure
- [ ] BusyIndicator uses control's WaitVisible() for waiting

---

## Related Documents

- [231_002 Page Object Pattern](231_002_PageObjectPattern.spx.md)
- [231_001 Control Object Pattern](231_001_ControlObjectPattern.spx.md)
- [221_004 Timeout](../221_Foundation/221_004_Timeout.spx.md)
- [FR-101 Page Object](../../100_requirements/120_functional/120_101_PageObject.spx.md)
