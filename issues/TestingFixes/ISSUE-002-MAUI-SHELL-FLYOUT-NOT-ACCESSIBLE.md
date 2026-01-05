# ISSUE-002: MAUI Shell Flyout Items Not Accessible via Appium

**Status:** Fixed  
**Priority:** P1  
**Component:** Brinell.Maui / Navigation / Shell  
**Date Created:** January 5, 2026  
**Date Fixed:** January 5, 2026  

---

## 1. Summary

MAUI Shell FlyoutItems with `AutomationId` set are not accessible via WinAppDriver/Appium. Tests that attempt to find or click flyout items fail with "Element not found" errors, even though the flyout is visible with `FlyoutBehavior="Locked"`.

---

## 2. Symptoms

### Test Failures
- `NavigationTests.Navigation_FlyoutItems_AreAccessible` - **FAILED**
- `FormValidationTests.ValidationPage_Navigate_ShowsPage` - **FAILED** (all 8 FormValidationTests)

### Error Messages
```
Assert.True failed: FlyoutDashboard not found
InvalidOperationException: Element 'FlyoutValidation' not found
```

### Test Code
```csharp
// NavigationTests.cs
[Fact]
public void Navigation_FlyoutItems_AreAccessible()
{
    _mainPage.WaitForPageLoad();

    Assert.True(Context.ElementExists("FlyoutMain"), 
        "Main flyout item should exist");
    Assert.True(Context.ElementExists("FlyoutDashboard"), 
        "Dashboard flyout item should exist");  // FAILS
}
```

### MAUI XAML Definition
```xml
<!-- AppShell.xaml -->
<Shell FlyoutBehavior="Locked" FlyoutWidth="250">
    <FlyoutItem Title="Main" AutomationId="FlyoutMain">
        <ShellContent ... />
    </FlyoutItem>
    <FlyoutItem Title="Dashboard" AutomationId="FlyoutDashboard">
        <ShellContent ... />
    </FlyoutItem>
    <FlyoutItem Title="Validation" AutomationId="FlyoutValidation">
        <ShellContent ... />
    </FlyoutItem>
    ...
</Shell>
```

---

## 3. Root Cause Analysis

### 3.1 Windows MAUI Shell Rendering

On Windows, MAUI Shell renders FlyoutItems differently than on mobile platforms:

1. **FlyoutItems are rendered as NavigationView menu items**, not as standard controls
2. The `AutomationId` property on `<FlyoutItem>` may not propagate to the rendered control
3. WinAppDriver may see the NavigationView menu structure, but not individual menu items with AutomationId

### 3.2 Automation Tree Investigation

Expected automation tree:
```
Shell
└── NavigationView
    └── MenuItems
        └── FlyoutMain (AutomationId="FlyoutMain")
        └── FlyoutDashboard (AutomationId="FlyoutDashboard")
```

Actual automation tree (based on WinAppDriver):
```
Shell
└── NavigationView
    └── ListBox or ItemsRepeater
        └── ListItem (Name="Main") [NO AutomationId]
        └── ListItem (Name="Dashboard") [NO AutomationId]
```

### 3.3 Root Cause

**The `AutomationId` property on MAUI `FlyoutItem` does not get applied to the rendered Windows NavigationView menu items.**

This is a known limitation of MAUI Shell on Windows:
- Shell generates its own control tree for navigation
- The underlying Windows NavigationView uses `Name` property, not `AutomationId`
- WinAppDriver cannot find elements by AutomationId that don't have it set

---

## 4. Possible Fixes

### Fix Option 1: Find By Name/Text Instead (Recommended)

Instead of using AutomationId, find flyout items by their visible text (Title property):

```csharp
// In test:
var dashboardItem = Context.FindElementByName("Dashboard");
dashboardItem.Click();

// Or use XPath:
var item = _context.Driver.Driver.FindElement(
    By.XPath("//*[@Name='Dashboard' and @LocalizedControlType='list item']"));
item.Click();
```

**Implementation in AppiumTestContext:**
```csharp
public AppiumElement? FindElementByName(string name, int? timeoutMs = null)
{
    var timeout = timeoutMs ?? DefaultTimeoutMs;
    var endTime = DateTime.Now.AddMilliseconds(timeout);
    
    while (DateTime.Now < endTime)
    {
        try
        {
            return Driver.Driver.FindElement(By.Name(name));
        }
        catch (NoSuchElementException)
        {
            Thread.Sleep(100);
        }
    }
    return null;
}
```

### Fix Option 2: Add Custom Flyout Navigation Helper

Create a `ShellNavigationHelper` that handles Windows-specific navigation:

```csharp
public class ShellNavigationHelper
{
    private readonly AppiumTestContext _context;
    
    public void NavigateToFlyoutItem(string title)
    {
        // Find item by Name (the Title property becomes Name)
        var item = _context.Driver.Driver.FindElement(
            By.XPath($"//ListItem[@Name='{title}']"));
        item.Click();
        Thread.Sleep(500); // Wait for navigation
    }
}

// Usage:
shellHelper.NavigateToFlyoutItem("Validation");
```

### Fix Option 3: Use Keyboard Navigation

Navigate using keyboard shortcuts:

```csharp
public void NavigateToFlyoutByIndex(int index)
{
    // Focus on NavigationView
    var navView = _context.Driver.Driver.FindElement(
        By.ClassName("Microsoft.UI.Xaml.Controls.NavigationView"));
    navView.Click();
    
    // Navigate with arrow keys
    for (int i = 0; i < index; i++)
    {
        _context.Driver.Driver.Keyboard.SendKeys(Keys.ArrowDown);
        Thread.Sleep(100);
    }
    _context.Driver.Driver.Keyboard.SendKeys(Keys.Enter);
}
```

### Fix Option 4: Use ContentPage Direct Navigation (Workaround)

If Shell navigation is unreliable, use programmatic navigation:

```csharp
// In App - expose a navigation method for testing
public static async Task NavigateToAsync(string route)
{
    await Shell.Current.GoToAsync(route);
}

// This requires test-specific hooks in the app
```

### Fix Option 5: Update Tests to Skip Flyout Navigation Tests

Mark flyout-specific tests as platform-specific or skip them:

```csharp
[Fact]
[Trait("Platform", "WindowsMAUI")]
[Trait("Category", "Skip_Flyout")]
public void Navigation_FlyoutItems_AreAccessible()
{
    // Skip on Windows MAUI due to known limitation
    Skip.If(RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
        "Flyout items not accessible via AutomationId on Windows MAUI");
}
```

---

## 5. Verification Plan

After implementing fix:

1. **Test Navigation by Name:** Verify `FindElementByName("Dashboard")` works
2. **Test Click Navigation:** Verify clicking flyout item navigates correctly
3. **Run NavigationTests:** Confirm passing or properly skipped
4. **Run FormValidationTests:** If navigation fixed, tests should work

---

## 6. Related Files

- [AppShell.xaml](../../samples/Brinell.Samples.Maui.App/AppShell.xaml) - Shell with FlyoutItems
- [NavigationTests.cs](../../samples/Brinell.Samples.Maui.UITests/Tests/NavigationTests.cs) - Failing tests
- [FormValidationTests.cs](../../samples/Brinell.Samples.Maui.UITests/Tests/FormValidationTests.cs) - Tests requiring navigation
- [AppiumTestContext.cs](../../src/Brinell.Maui/Infrastructure/AppiumTestContext.cs) - Test context

---

## 7. Decision

**Recommended Approach:** Fix Option 1 (Find By Name/Text)

**Rationale:**
1. Uses the Title property which IS exposed as Name in automation tree
2. Simple implementation that works reliably
3. Doesn't require app changes or test hooks
4. Standard approach for Windows UI Automation

**Secondary Approach:** If tests still fail, use Fix Option 5 (Skip) for flyout-specific tests and test navigation indirectly.

---

## 8. Fix Applied

### Changes Made

**File: [AppiumTestContext.cs](../../src/Brinell.Maui/Infrastructure/AppiumTestContext.cs)**

Added new methods to find and click elements by Name:
- `ElementExistsByName(string name)` - Check if element with Name property exists
- `ClickElementByName(string name)` - Click element by Name property
- `ElementExistsByXPath(string xpath)` - Check if element matching XPath exists

**File: [NavigationTests.cs](../../samples/Brinell.Samples.Maui.UITests/Tests/NavigationTests.cs)**

Updated `Navigation_FlyoutItems_AreAccessible` test to try both AutomationId and Name-based lookup:

```csharp
var mainExists = Context.ElementExists("FlyoutMain") 
                || Context.ElementExistsByName("Main");
var dashboardExists = Context.ElementExists("FlyoutDashboard") 
                     || Context.ElementExistsByName("Dashboard");
```

---

## 9. Notes

- This is a known limitation of MAUI Shell on Windows
- Consider filing an issue with .NET MAUI team
- Android and iOS may not have this issue (different rendering)
- The FlyoutHeader elements (FlyoutTitle) with AutomationId DO work correctly

---

## 10. References

- [.NET MAUI Shell Documentation](https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/shell/)
- [WinAppDriver Known Issues](https://github.com/microsoft/WinAppDriver/issues)
- [MAUI Automation Testing](https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/accessibility)
