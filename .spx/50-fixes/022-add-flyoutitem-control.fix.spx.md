# FIX-022: Add FlyoutItemControl to MAUI Framework

**Status:** DOCUMENTED  
**Created:** 2026-01-03  
**Priority:** High  
**Blocks:** Container tests (SPEC-017b)

---

## Summary

Add a `MauiFlyoutItemControl` class to the MAUI framework that provides reliable interaction with Shell FlyoutItem elements, enabling navigation in test automation.

---

## Problem Description

### Current Behavior

The `NavigateToContainerDemo()` method in `AppiumFixture.cs` attempts to click on a FlyoutItem using multiple locator strategies:
1. `MobileBy.AccessibilityId("FlyoutContainerDemo")`
2. `By.Name("Container Demo")`
3. XPath with `contains(@Name, 'Container Demo')`

All approaches fail to locate or click the FlyoutItem element, causing all container tests to fail with navigation errors.

### Expected Behavior

Clicking a Shell FlyoutItem by its AutomationId or Title should navigate to the corresponding page.

### Root Cause Analysis

MAUI Shell renders FlyoutItem elements through a complex visual tree. On Windows:
- The `AutomationId` property on `FlyoutItem` in XAML does not directly propagate to the Windows UI Automation tree
- The element hierarchy differs from standard controls
- FlyoutItem may need to be located via its Title text content or a structural XPath

### Evidence

From `AppShell.xaml`:
```xml
<FlyoutItem Title="Container Demo" 
            AutomationId="FlyoutContainerDemo">
    <ShellContent Title="Container Demo"
                  ContentTemplate="{DataTemplate pages:ContainerDemoPage}"
                  Route="containerdemo"
                  AutomationId="ShellContainerDemo" />
</FlyoutItem>
```

The FlyoutItem has `AutomationId="FlyoutContainerDemo"` set, but Appium cannot find it using `MobileBy.AccessibilityId`.

---

## Proposed Solution

### 1. Create `MauiFlyoutItemControl` Class

Create a new control class that:
- Extends `MauiControlBase<TScope>`
- Implements `IClickableControlObject<TScope>`
- Uses platform-specific locator strategies for FlyoutItem elements
- Provides fallback mechanisms for different MAUI versions

### 2. Control Interface

```csharp
public class MauiFlyoutItemControl<TScope> : MauiControlBase<TScope>, IClickableControlObject<TScope>
    where TScope : IMauiPage<TScope>
{
    private readonly string _title;
    
    public MauiFlyoutItemControl(TScope scope, string automationId, string? title = null)
        : base(scope, automationId)
    {
        _title = title ?? automationId.Replace("Flyout", "").Replace("Demo", " Demo").Trim();
    }
    
    // IClickableControlObject implementation
    public TScope Click();
    public TScope DoubleClick();
    public TScope RightClick();
    public bool IsClickable();
    public bool WaitClickable(bool? expected = true, int? timeoutMs = null);
    public TScope AssertClickable(bool? expected = true, string? message = null);
    
    // FlyoutItem-specific
    protected override AppiumElement FindElement()
    {
        // Try multiple strategies:
        // 1. AccessibilityId
        // 2. Title text
        // 3. XPath for flyout item structure
    }
}
```

### 3. Locator Strategy Sequence

```csharp
protected override AppiumElement FindElement()
{
    var strategies = new Func<IReadOnlyCollection<AppiumElement>>[]
    {
        // 1. Try by AutomationId
        () => Driver.FindElements(MobileBy.AccessibilityId(AutomationId)),
        
        // 2. Try by Title as Name
        () => Driver.FindElements(By.Name(_title)),
        
        // 3. Try XPath for FlyoutItem text content
        () => Driver.FindElements(By.XPath($"//ListItem[.//Text[@Name='{_title}']]")),
        
        // 4. Try XPath for NavigationViewItem (Windows Shell flyout)
        () => Driver.FindElements(By.XPath($"//NavigationViewItem[@Name='{_title}']")),
        
        // 5. Fallback: Find any element containing the title text
        () => Driver.FindElements(By.XPath($"//*[contains(@Name, '{_title}')]"))
    };
    
    foreach (var strategy in strategies)
    {
        var elements = strategy();
        if (elements.Count > 0)
        {
            return (AppiumElement)elements.First();
        }
    }
    
    throw new NoSuchElementException($"FlyoutItem not found: {AutomationId}");
}
```

---

## Affected Files

### New Files
| File | Description |
|------|-------------|
| `srcnew/Brinell.Maui/Controls/MauiFlyoutItemControl.cs` | FlyoutItem control implementation |

### Modified Files
| File | Change |
|------|--------|
| `testsnew/Brinell.Maui.UITests/AppiumFixture.cs` | Update NavigateToContainerDemo to use new control |
| `testsnew/Brinell.Maui.UITests/Pages/AppShellPage.cs` | Add page object for Shell navigation (optional) |

---

## Implementation Steps

1. **Create `MauiFlyoutItemControl.cs`**
   - Extend `MauiControlBase<TScope>`
   - Implement `IClickableControlObject<TScope>`
   - Override `FindElement()` with multi-strategy locator
   - Add constructor accepting both automationId and title

2. **Test the control**
   - Debug to identify which locator strategy works on Windows
   - Verify Click() navigates correctly
   - Test with multiple FlyoutItems

3. **Update `AppiumFixture.cs`**
   - Replace manual element finding with `MauiFlyoutItemControl`
   - Use fluent pattern for navigation

4. **Run container tests**
   - Verify all 26 container tests pass

---

## Acceptance Criteria

- [ ] `MauiFlyoutItemControl` class exists in `srcnew/Brinell.Maui/Controls/`
- [ ] Implements `IClickableControlObject<TScope>` interface
- [ ] Uses multi-strategy locator for finding FlyoutItem elements
- [ ] `NavigateToContainerDemo()` successfully navigates to ContainerDemoPage
- [ ] All 26 container tests pass (SPEC-017b)
- [ ] No hardcoded Thread.Sleep in final implementation (use proper waits)

---

## Testing Notes

To test manually:
1. Start Appium: `appium --relaxed-security`
2. Build and run the MAUI sample app
3. Use Appium Inspector to examine the FlyoutItem element tree
4. Identify the correct locator strategy
5. Update control implementation accordingly

---

## References

- **SPEC-017b**: Container Control Testing
- **IClickableControlObject**: `srcnew/Brinell.Core/Interfaces/IClickableControlObject.cs`
- **MauiButtonControl pattern**: `srcnew/Brinell.Maui/Controls/MauiButtonControl.cs`
- **AppShell.xaml**: `samples/Brinell.Samples.Maui.App/AppShell.xaml`
