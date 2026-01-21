# SPEC: ScrollIntoView Android Analysis

**Status:** Analysis  
**Date:** January 20, 2026  
**Author:** Generated from codebase analysis  
**Related Issues:** SliderControlTests 0/9 pass, CheckboxControlTests elements not found

---

## 1. Executive Summary

The `ScrollIntoView` functionality is not working correctly on Android, causing tests to fail when elements are below the visible viewport. Elements like `VolumeSlider`, `SubscribeCheckBox`, and various RadioButton controls are not found because:

1. **Current implementation is Windows-focused** - uses `windows: scroll` and JavaScript `scrollIntoView`
2. **Android requires different approach** - needs `mobile: scrollGesture` or `UiScrollable` commands
3. **No platform detection** - same code path used for all platforms

---

## 2. Problem Description

### 2.1 Symptoms

| Test Class | Pass Rate | Error |
|------------|-----------|-------|
| SliderControlTests | 0/9 (0%) | `ElementNotFoundException: Element not found with locator: AutomationId:VolumeSlider after 1000ms` |
| CheckboxControlTests | 1/9 (11%) | `SubscribeCheckBox` not found |
| RadioButtonControlTests | Low | RadioButton elements not found |

### 2.2 Root Cause

The `VolumeSlider` element exists in [UserFormPage.xaml](../samples/Brinell.Samples.Maui.App/Pages/UserFormPage.xaml#L187) inside a `ScrollView`:

```xaml
<ScrollView AutomationId="FormScrollView">
    <VerticalStackLayout>
        <!-- ... many controls above ... -->
        
        <!-- Range Section - approximately 80% down the page -->
        <Slider Minimum="0" Maximum="100" AutomationId="VolumeSlider" />
        
        <!-- ... more controls ... -->
    </VerticalStackLayout>
</ScrollView>
```

The slider is positioned **below the initial viewport** on mobile devices. Without scrolling, Appium cannot find the element.

---

## 3. Current Implementation Analysis

### 3.1 MauiElement.ScrollIntoView (Windows-Focused)

**File:** [srcnew/Brinell.Maui/Wrappers/MauiElement.cs](../srcnew/Brinell.Maui/Wrappers/MauiElement.cs#L63-L114)

```csharp
public void ScrollIntoView(IMauiDriver driver)
{
    // Check if already displayed
    if (_element.Displayed) return;
    
    try
    {
        var unwrappedDriver = driver.UnwrapDriver();
        
        // Attempt 1: Windows-specific scroll pattern
        try
        {
            unwrappedDriver.ExecuteScript("windows: scroll", new Dictionary<string, object>
            {
                { "elementId", _element.Id },
                { "direction", "down" },
                { "percent", 0.5 }
            });
            return;
        }
        catch
        {
            // windows: scroll not available
        }
        
        // Attempt 2: JavaScript scrollIntoView (web views only)
        if (unwrappedDriver is IJavaScriptExecutor jsExecutor)
        {
            jsExecutor.ExecuteScript(
                "arguments[0].scrollIntoView({behavior: 'auto', block: 'center'});", 
                _element);
        }
    }
    catch
    {
        // Swallow - element may still be interactable
    }
}
```

**Issues:**
- ❌ `windows: scroll` does not work on Android
- ❌ `IJavaScriptExecutor.scrollIntoView` is for web contexts only, not native Android
- ❌ No Android-specific scroll logic
- ❌ No error logging when scroll fails

### 3.2 MauiScrollableControlBase (Uses Actions API)

**File:** [srcnew/Brinell.Maui/Controls/MauiScrollableControlBase.cs](../srcnew/Brinell.Maui/Controls/MauiScrollableControlBase.cs)

This class uses Selenium's `Actions` API for scrolling:

```csharp
protected virtual void SwipeUpCore(IMauiElement element)
{
    var rect = unwrappedElement.Rect;
    var centerX = rect.X + rect.Width / 2;
    var startY = rect.Y + (int)(rect.Height * 0.8);
    var endY = rect.Y + (int)(rect.Height * 0.2);
    
    var actions = new OpenQA.Selenium.Interactions.Actions(unwrappedDriver);
    actions.MoveToLocation(centerX, startY)
           .ClickAndHold()
           .MoveToLocation(centerX, endY)
           .Release()
           .Perform();
}
```

**Issues:**
- ⚠️ `MoveToLocation` may not work reliably on Android
- ⚠️ Coordinate-based scrolling is fragile across device sizes
- ❌ Not connected to `ScrollIntoView` for individual controls

---

## 4. Android-Specific Scroll APIs

### 4.1 mobile: scrollGesture (Recommended)

The UiAutomator2 driver provides `mobile: scrollGesture` for native scrolling:

```csharp
// C# Example
driver.ExecuteScript("mobile: scrollGesture", new Dictionary<string, object>
{
    { "left", 100 },
    { "top", 100 },
    { "width", 200 },
    { "height", 600 },
    { "direction", "down" },
    { "percent", 1.0 }
});
```

**Parameters:**
- `elementId` - Scroll within element bounds (optional)
- `left`, `top`, `width`, `height` - Scroll bounding area
- `direction` - `up`, `down`, `left`, `right`
- `percent` - Scroll distance as % of area (1.0 = 100%)
- `speed` - Pixels per second (default: 5000 * displayDensity)

**Returns:** `boolean` - `true` if can still scroll in that direction

### 4.2 UiScrollable (For Finding Elements)

Android's `UiScrollable` can scroll AND find elements in one operation:

```csharp
// Scroll to find element by text
driver.FindElement(MobileBy.AndroidUIAutomator(
    "new UiScrollable(new UiSelector().scrollable(true))" +
    ".scrollIntoView(new UiSelector().description(\"VolumeSlider\"))"
));

// Scroll to find by resource ID
driver.FindElement(MobileBy.AndroidUIAutomator(
    "new UiScrollable(new UiSelector().scrollable(true))" +
    ".scrollIntoView(new UiSelector().resourceIdMatches(\".*VolumeSlider\"))"
));
```

**Benefits:**
- ✅ Scrolls until element found
- ✅ Works with nested scrollable containers
- ✅ Handles vertical and horizontal scrolling
- ✅ Native Android API - very reliable

### 4.3 mobile: scroll (Legacy)

Older command still available:

```csharp
driver.ExecuteScript("mobile: scroll", new Dictionary<string, object>
{
    { "direction", "down" }
});
```

---

## 5. Proposed Solutions

### 5.1 Option A: Platform-Specific ScrollIntoView (Recommended)

Modify `MauiElement.ScrollIntoView` to detect platform and use appropriate method:

```csharp
public void ScrollIntoView(IMauiDriver driver)
{
    if (_element.Displayed) return;
    
    var platform = driver.GetPlatform(); // Add platform detection
    
    switch (platform)
    {
        case MauiPlatform.Android:
            ScrollIntoViewAndroid(driver);
            break;
        case MauiPlatform.iOS:
            ScrollIntoViewiOS(driver);
            break;
        case MauiPlatform.Windows:
            ScrollIntoViewWindows(driver);
            break;
    }
}

private void ScrollIntoViewAndroid(IMauiDriver driver)
{
    var unwrappedDriver = driver.UnwrapDriver();
    
    // Get element bounds to determine scroll direction
    var windowSize = unwrappedDriver.Manage().Window.Size;
    var location = _element.Location;
    
    // Scroll down if element is below viewport
    if (location.Y > windowSize.Height)
    {
        var scrollParams = new Dictionary<string, object>
        {
            { "left", 0 },
            { "top", 100 },
            { "width", windowSize.Width },
            { "height", windowSize.Height - 200 },
            { "direction", "down" },
            { "percent", 0.8 }
        };
        
        var maxAttempts = 10;
        for (int i = 0; i < maxAttempts; i++)
        {
            var canScrollMore = (bool)unwrappedDriver.ExecuteScript(
                "mobile: scrollGesture", scrollParams);
            
            if (_element.Displayed) return;
            if (!canScrollMore) break;
        }
    }
    // Similar logic for scroll up
}
```

**Pros:**
- ✅ Clean separation of platform logic
- ✅ Uses native Android APIs
- ✅ Maintainable

**Cons:**
- ⚠️ Requires platform detection mechanism
- ⚠️ More code to maintain

### 5.2 Option B: UiScrollable for Element Finding

Use `UiScrollable` in the element finding logic instead of ScrollIntoView:

```csharp
// In MauiScope.TryFindElement or similar
protected override IMauiElement? TryFindElementAndroid(Locator locator)
{
    var unwrappedDriver = Context.Driver.UnwrapDriver();
    
    // First try direct find
    var element = base.TryFindElement(locator);
    if (element != null) return element;
    
    // Use UiScrollable to find with scroll
    try
    {
        var automationId = locator.GetAutomationId();
        var uiAutomatorQuery = 
            $"new UiScrollable(new UiSelector().scrollable(true))" +
            $".scrollIntoView(new UiSelector().resourceIdMatches(\".*{automationId}\"))";
        
        var appiumElement = unwrappedDriver.FindElement(
            MobileBy.AndroidUIAutomator(uiAutomatorQuery));
        
        return new MauiElement(appiumElement);
    }
    catch
    {
        return null;
    }
}
```

**Pros:**
- ✅ Scroll + find in one atomic operation
- ✅ Most reliable for Android
- ✅ Handles nested scroll views

**Cons:**
- ⚠️ Only works for Android
- ⚠️ Changes element finding logic
- ⚠️ May have performance implications

### 5.3 Option C: Hybrid Approach (Best)

Combine both approaches:

1. **Element finding** - Use direct find first
2. **If not found** - Use `UiScrollable` on Android
3. **For explicit scroll operations** - Use `mobile: scrollGesture`

```csharp
// IMauiElement extension or driver method
public static IMauiElement? FindElementWithScroll(
    this IMauiDriver driver, 
    Locator locator,
    int timeoutMs = 5000)
{
    var platform = driver.GetPlatform();
    
    // Try direct find first
    var element = driver.TryFindElement(locator);
    if (element != null && element.Displayed) return element;
    
    // Platform-specific scroll-find
    if (platform == MauiPlatform.Android)
    {
        return FindWithUiScrollable(driver, locator);
    }
    else if (platform == MauiPlatform.iOS)
    {
        return FindWithIOSScroll(driver, locator, timeoutMs);
    }
    
    // Windows doesn't typically need scroll for element finding
    return element;
}
```

---

## 6. Implementation Plan

### Phase 1: Platform Detection (Required First)

**Add platform detection to driver:**

```csharp
public enum MauiPlatform { Windows, Android, iOS, MacCatalyst, Unknown }

public interface IMauiDriver
{
    // ... existing methods ...
    MauiPlatform Platform { get; }
}
```

### Phase 2: Android ScrollIntoView

**Files to modify:**
- `srcnew/Brinell.Maui/Wrappers/MauiElement.cs` - Add Android scroll logic
- `srcnew/Brinell.Maui/Wrappers/MauiDriver.cs` - Add Platform property

### Phase 3: Element Finding Enhancement

**Files to modify:**
- `srcnew/Brinell.Maui/Scopes/MauiScopeBase.cs` - Add scroll-find for Android

### Phase 4: Scrollable Control Base

**Files to modify:**
- `srcnew/Brinell.Maui/Controls/MauiScrollableControlBase.cs` - Use `mobile: scrollGesture`

---

## 7. Test Cases to Verify Fix

After implementation, these tests should pass:

| Test | Expected Result |
|------|-----------------|
| `Slider_IsExists_ReturnsTrue` | ✅ VolumeSlider found after scrolling |
| `Slider_GetValue_ReturnsCurrentValue` | ✅ Can read slider value |
| `CheckBox_IsExists_ReturnsTrue` | ✅ SubscribeCheckBox found |
| `RadioButton_IsExists_ReturnsTrue` | ✅ RadioButton elements found |
| `ScrollView_ScrollToEnd` | ✅ Uses mobile: scrollGesture |

---

## 8. References

- [Appium UiAutomator2 Mobile Gestures](https://github.com/appium/appium-uiautomator2-driver/blob/master/docs/android-mobile-gestures.md)
- [Appium Execute Methods Guide](https://appium.io/docs/en/latest/guides/execute-methods/)
- [UiScrollable Android Docs](https://developer.android.com/reference/androidx/test/uiautomator/UiScrollable)
- Related spec: [SPEC-026-UI-Test-Control-Interaction-Fixes.md](SPEC-026-UI-Test-Control-Interaction-Fixes.md)

---

## 9. Appendix: Android Attribute Support

For reference, Android UiAutomator2 supports these element attributes:

**Supported for finding/scrolling:**
- `resource-id` (AutomationId)
- `content-desc` (AccessibilityLabel)
- `text`
- `className`
- `scrollable`

**NOT supported:**
- `ToggleState` (use `checked` instead)
- `Placeholder` (use `hint` for Android)
- `Scroll.VerticalScrollPercent` (Windows only)

---

## 10. Decision Required

Choose implementation approach:

- [ ] **Option A:** Platform-specific `ScrollIntoView` only
- [ ] **Option B:** `UiScrollable` for element finding only
- [x] **Option C:** Hybrid (recommended) - both approaches

**Next Step:** Implement platform detection, then proceed with hybrid approach.
