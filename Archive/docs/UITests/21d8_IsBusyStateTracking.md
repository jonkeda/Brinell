# 8. IsBusy-Based State Tracking

**Parent:** [Documentation Index](21d0_UITestFramework_Index.md)  
**Code Examples:** [21d8_IsBusyStateTracking_CodeExamples.md](21d8_IsBusyStateTracking_CodeExamples.md)  
**Previous:** [Wait/Check/Is/Assert Pattern](21d7_WaitCheckIsAssertPattern.md)  
**Version:** 3.0 (Updated December 2025)

---

## 8.1 Overview

IsBusy-based state tracking ensures tests wait for page readiness before interacting with controls. Pages indicate busy state via a standardized indicator control.

**Architecture (v3):** Each platform implements its own `BusyPageBase` (or `LoadingPageBase`) using native driver access. No shared base class.

---

## 8.2 The Problem

### 8.2.1 Without IsBusy Tracking

```csharp
// PROBLEM: Page may still be loading
var settings = shell.NavigateToSettings();
settings.SaveButton.Click();  // May fail - button not ready yet
```

### 8.2.2 Common Failure Modes

| Failure | Cause |
|---------|-------|
| Element not found | DOM not yet updated |
| Element not interactable | Control still loading |
| Stale element reference | Page re-rendered |
| Wrong value | Old value from previous state |

---

## 8.3 The Solution

### 8.3.1 Standardized IsBusy Indicator

Every page includes a busy indicator control:
- WPF: `BusyIndicator` or `ProgressBar`
- MAUI: `ActivityIndicator`
- HTML: CSS spinner or overlay

### 8.3.2 WaitForPageReady Pattern

```csharp
var settings = shell.NavigateToSettings();
settings.WaitForPageReady();  // Waits for IsBusy = false
settings.SaveButton.Click();   // Now safe to interact
```

---

## 8.4 Page Object Implementation

### 8.4.1 BusyPageBase Class (Platform-Specific)

Each platform implements its own busy page base. Example for WPF:

```csharp
// In WPF platform project
public abstract class BusyPageBase : PageBase
{
    protected abstract string BusyIndicatorId { get; }
    
    private readonly Lazy<AutomationElement?> _busyIndicator;
    
    protected BusyPageBase(FlaUITestContext context, string pageName)
        : base(context, pageName)
    {
        _busyIndicator = new Lazy<AutomationElement?>(() => 
            context.FindElement(BusyIndicatorId));
    }
    
    public virtual bool IsBusy()
    {
        var indicator = _busyIndicator.Value;
        return indicator != null && indicator.IsVisible();
    }
    
    public virtual bool WaitForNotBusy(TimeSpan? timeout = null)
    {
        return Context.WaitFor(
            () => !IsBusy(),
            timeout,
            $"'{PageName}' not busy");
    }
    
    public override void WaitForPageReady(TimeSpan? timeout = null)
    {
        // Wait for page displayed
        WaitForDisplayed(timeout);
        
        // Wait for not busy
        WaitForNotBusy(timeout);
    }
}
```

### 8.4.2 Derived Page Example

```csharp
public class SettingsPage : BusyPageBase
{
    protected override string BusyIndicatorId => "SettingsPageBusyIndicator";
    
    public SettingsPage(FlaUITestContext context) : base(context, "Settings")
    {
        // Initialize controls...
    }
}
```

---

## 8.5 Busy Indicator Requirements

### 8.5.1 XAML Requirements

**WPF:**
```xml
<Grid>
    <BusyIndicator AutomationProperties.AutomationId="PageBusyIndicator"
                   IsBusy="{Binding IsBusy}" />
    <!-- Page content -->
</Grid>
```

**MAUI:**
```xml
<Grid>
    <ActivityIndicator AutomationId="PageBusyIndicator"
                       IsRunning="{Binding IsBusy}"
                       IsVisible="{Binding IsBusy}" />
    <!-- Page content -->
</Grid>
```

**HTML:**
```html
<div data-automation-id="PageBusyIndicator" 
     class="spinner" 
     style="display: none;">
</div>
```

### 8.5.2 Visibility Pattern

| State | Indicator Visible | Indicator Active |
|-------|------------------|------------------|
| Loading | ✅ | ✅ |
| Ready | ❌ | ❌ |

---

## 8.6 Navigation with IsBusy

### 8.6.1 Recommended Pattern (v3)

Navigation methods return void. Tests create and wait for target page:

```csharp
// In test
[Test]
public void Test_Navigate_And_Wait()
{
    var shell = new ShellPage(Context);
    shell.WaitForPageReady();
    
    // Navigation returns void
    shell.NavigateToSettings();
    
    // Test creates and waits for target page
    var settings = new SettingsPage(Context);
    settings.WaitForPageReady();  // Includes IsBusy wait
    
    // Now safe to interact
    settings.SaveButton.Click();
}

// In page object
public void NavigateToSettings()
{
    Log("Navigating to Settings");
    SettingsButton.Click();
}
```

### 8.6.2 Flow Diagram

```
Test: shell.NavigateToSettings()
    │
    └── SettingsButton.Click()

Test: var settings = new SettingsPage(Context)
Test: settings.WaitForPageReady()
            │
            ├── WaitForDisplayed()
            │       └── Wait for key control visible
            │
            └── WaitForNotBusy()
                    └── Wait for BusyIndicator not visible
```

---

## 8.7 IsBusy States

### 8.7.1 Page Lifecycle

```
Page Navigation Triggered
    │
    ├── [IsBusy = true] - Loading started
    │
    ├── Data fetching...
    │
    ├── UI rendering...
    │
    └── [IsBusy = false] - Ready for interaction
```

### 8.7.2 ViewModel Support

```csharp
// ViewModel implements IsBusy
public class SettingsViewModel : ViewModelBase
{
    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }
    
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            await LoadSettingsAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }
}
```

---

## 8.8 Advanced Scenarios

### 8.8.1 Multiple Busy Indicators

Some pages have multiple loading regions:

```csharp
public class DashboardPage : BusyPageBase
{
    protected override string BusyIndicatorId => "DashboardBusyIndicator";
    
    public IndicatorControl ChartLoadingIndicator { get; }
    public IndicatorControl TableLoadingIndicator { get; }
    
    public void WaitForAllContentReady()
    {
        WaitForPageReady();
        ChartLoadingIndicator.WaitForActive(false);
        TableLoadingIndicator.WaitForActive(false);
    }
}
```

### 8.8.2 Long-Running Operations

```csharp
// Handle operations that take longer than default timeout
settings.WaitForPageReady(timeoutMs: 30000);
```

### 8.8.3 No Busy Indicator Fallback

If page doesn't have busy indicator, extend `PageBase` directly:

```csharp
public class SimplePage : PageBase
{
    public SimplePage(FlaUITestContext context) : base(context, "Simple")
    {
        TitleLabel = new LabelControl(context, this, "TitleLabel");
        ContentPanel = new ControlBase(context, this, "ContentPanel");
    }
    
    public LabelControl TitleLabel { get; }
    public ControlBase ContentPanel { get; }
    
    public override void WaitForPageReady(TimeSpan? timeout = null)
    {
        WaitForDisplayed(timeout);
        
        // No busy indicator - wait for key controls
        TitleLabel.WaitVisible(true, timeout);
        ContentPanel.WaitVisible(true, timeout);
    }
}
```

---

## 8.9 Testing IsBusy

### 8.9.1 Test That Page Waits Correctly

```csharp
[Fact]
public void Page_Waits_For_Loading_Complete()
{
    var shell = new ShellPage(Context);
    shell.WaitForPageReady();
    
    // Navigate triggers loading
    shell.NavigateToSettings();
    
    var settings = new SettingsPage(Context);
    // WaitForPageReady should handle busy state
    settings.WaitForPageReady();
    
    // If this fails, page wasn't ready
    settings.SaveButton.AssertEnabled(false);  // Not enabled until data loaded
}
```

### 8.9.2 Test With Mock API Delays

```csharp
[Fact]
public void Page_Handles_Slow_API()
{
    // Configure mock to delay response
    MockApi.Stub("/api/settings")
        .WithDelay(TimeSpan.FromSeconds(5))
        .ReturnsJson(new { theme = "dark" });
    
    var shell = new ShellPage(Context);
    shell.WaitForPageReady();
    shell.NavigateToSettings();
    
    var settings = new SettingsPage(Context);
    // Should wait for slow API
    settings.WaitForPageReady(timeout: TimeSpan.FromSeconds(10));
    
    settings.ThemeDropdown.AssertText("dark");
}
```

---

*Next: [Page Object Pattern](21d9_PageObjectPattern.md)*
