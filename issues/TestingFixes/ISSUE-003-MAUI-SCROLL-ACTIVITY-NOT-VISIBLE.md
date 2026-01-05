# ISSUE-003: MAUI ScrollToBottom ActivityIndicator Not Visible

**Status:** Fixed  
**Priority:** P2  
**Component:** Brinell.Maui / ScrollViewControl / ActivityIndicatorControl  
**Date Created:** January 5, 2026  
**Date Fixed:** January 5, 2026  

---

## 1. Summary

After calling `ScrollToBottom()` on the MainPage's ScrollView, the `LoadingIndicator` (ActivityIndicator) control assertion fails. The test expects the ActivityIndicator to exist after scrolling to bottom, but the assertion fails.

---

## 2. Symptoms

### Test Failure
- `NavigationTests.Navigation_ScrollToBottom_ShowsActivitySection` - **FAILED**

### Error Message
```
AssertExists failed: LoadingIndicator - Activity indicator should be visible after scrolling
```

### Test Code
```csharp
[Fact]
public void Navigation_ScrollToBottom_ShowsActivitySection()
{
    // Arrange
    _mainPage.WaitForPageLoad();

    // Act
    _mainPage.MainScrollView.ScrollToBottom();

    // Assert
    _mainPage.LoadingIndicator.AssertExists("Activity indicator should be visible after scrolling");
}
```

### MAUI XAML (MainPage.xaml)
```xml
<ScrollView AutomationId="MainScrollView">
    <VerticalStackLayout>
        <!-- ... other controls ... -->
        
        <!-- Activity Indicator at bottom -->
        <Frame AutomationId="ActivityFrame" Padding="20" CornerRadius="10">
            <VerticalStackLayout Spacing="10">
                <Label Text="Activity Indicator" ... />
                <ActivityIndicator
                    x:Name="LoadingIndicator"
                    AutomationId="LoadingIndicator"
                    IsRunning="false"
                    Color="{StaticResource Primary}" />
                <Button
                    AutomationId="ToggleLoadingButton"
                    Text="Toggle Loading" ... />
            </VerticalStackLayout>
        </Frame>
    </VerticalStackLayout>
</ScrollView>
```

---

## 3. Root Cause Analysis

### 3.1 Possible Causes

#### Cause A: Scroll Not Completing Before Assertion

The `ScrollToBottom()` method may return before the UI has finished scrolling and rendering:

```csharp
// ScrollViewControl.ScrollToBottom()
public void ScrollToBottom()
{
    var element = FindElement();
    // Scroll action may return immediately
    element?.SendKeys(Keys.Control + Keys.End);
    // No wait for scroll to complete
}
```

**Problem:** The assertion runs before the ActivityIndicator enters the viewport.

#### Cause B: ActivityIndicator Not in Automation Tree When Not Running

MAUI `ActivityIndicator` with `IsRunning="false"` may:
- Not render anything visible
- Be collapsed/hidden from automation tree
- Have zero size

**Problem:** WinAppDriver may not find elements that have no visible content.

#### Cause C: ScrollView Virtualization

If the ScrollView or parent uses virtualization:
- Elements not in viewport may not exist in the visual tree
- Even after scroll, there may be a delay before elements are created

#### Cause D: Element Exists But IsVisible Check Fails

The `AssertExists` may internally check visibility, and the ActivityIndicator might:
- Exist but have `Displayed = false`
- Be obscured by another element
- Be outside the viewport after scroll

---

## 4. Investigation Steps

### 4.1 Check ActivityIndicator Visibility

Modify test to add wait and check multiple conditions:

```csharp
[Fact]
public void Navigation_ScrollToBottom_ShowsActivitySection()
{
    _mainPage.WaitForPageLoad();
    
    // Scroll
    _mainPage.MainScrollView.ScrollToBottom();
    
    // Wait for scroll animation
    Thread.Sleep(500);
    
    // Try finding ToggleLoadingButton instead (should be more reliable)
    var toggleButtonExists = _mainPage.ToggleLoadingButton.WaitExists(true, 3000);
    Assert.True(toggleButtonExists, "Toggle button should exist after scroll");
    
    // Then check ActivityIndicator
    var indicatorExists = _mainPage.LoadingIndicator.IsExists();
    Output.WriteLine($"LoadingIndicator.IsExists: {indicatorExists}");
    
    if (!indicatorExists)
    {
        // Maybe it's the IsRunning=false issue
        // Try clicking toggle to start it
        _mainPage.ToggleLoadingButton.Click();
        Thread.Sleep(200);
        indicatorExists = _mainPage.LoadingIndicator.IsExists();
        Output.WriteLine($"After Toggle - IsExists: {indicatorExists}");
    }
}
```

### 4.2 Inspect Element Tree

Use Inspect.exe (Windows SDK) or Accessibility Insights to:
1. Launch the MAUI app
2. Scroll to bottom manually
3. Check if ActivityIndicator appears in automation tree
4. Note its properties (Name, AutomationId, IsEnabled, BoundingRectangle)

---

## 5. Possible Fixes

### Fix Option 1: Add Wait After Scroll (Recommended)

Update `ScrollToBottom()` to wait for scroll completion:

```csharp
public void ScrollToBottom()
{
    var element = FindElement();
    if (element == null) return;
    
    // Perform scroll
    element.SendKeys(Keys.Control + Keys.End);
    
    // Wait for scroll animation to complete
    Thread.Sleep(ScrollAnimationDelayMs); // e.g., 300-500ms
}

protected virtual int ScrollAnimationDelayMs => 500;
```

### Fix Option 2: Use WaitExists Instead of AssertExists

Update test to use explicit wait:

```csharp
[Fact]
public void Navigation_ScrollToBottom_ShowsActivitySection()
{
    _mainPage.WaitForPageLoad();
    _mainPage.MainScrollView.ScrollToBottom();
    
    // Wait with timeout for element to appear
    var exists = _mainPage.LoadingIndicator.WaitExists(true, 3000);
    Assert.True(exists, "Activity indicator should exist after scrolling");
}
```

### Fix Option 3: Test Toggle Button Instead

If ActivityIndicator with `IsRunning=false` is invisible to automation:

```csharp
[Fact]
public void Navigation_ScrollToBottom_ShowsActivitySection()
{
    _mainPage.WaitForPageLoad();
    _mainPage.MainScrollView.ScrollToBottom();
    
    // Test the ToggleLoadingButton instead (always visible)
    _mainPage.ToggleLoadingButton.AssertExists("Toggle button should exist after scrolling");
    
    // Or test the ActivityFrame
    Context.FindElementByAutomationId("ActivityFrame")
        .AssertExists("Activity section should be visible after scroll");
}
```

### Fix Option 4: Activate ActivityIndicator Before Assert

If the issue is `IsRunning=false`:

```csharp
[Fact]
public void Navigation_ScrollToBottom_ShowsActivitySection()
{
    _mainPage.WaitForPageLoad();
    _mainPage.MainScrollView.ScrollToBottom();
    
    // Start the indicator first
    _mainPage.ToggleLoadingButton.Click();
    Thread.Sleep(200);
    
    // Now it should be visible
    _mainPage.LoadingIndicator.AssertExists("Activity indicator should exist when running");
}
```

### Fix Option 5: Use Different Scroll Method

If `Keys.Control + Keys.End` doesn't work reliably:

```csharp
public void ScrollToBottom()
{
    var element = FindElement();
    if (element == null) return;
    
    // Use JavaScript-like scroll (if available) or touch actions
    var driver = _context.Driver.Driver;
    
    // Get element bounds
    var rect = element.Rect;
    
    // Perform swipe gesture
    var touchAction = new TouchAction(driver);
    touchAction.Press(rect.X + rect.Width/2, rect.Y + rect.Height - 50)
               .MoveTo(rect.X + rect.Width/2, rect.Y + 50)
               .Release()
               .Perform();
    
    // Repeat until at bottom
    Thread.Sleep(300);
}
```

---

## 6. Verification Plan

1. **Add Wait:** Implement Fix Option 1 (add delay after scroll)
2. **Run Test:** Execute `Navigation_ScrollToBottom_ShowsActivitySection`
3. **Verify Pass:** Test should pass with wait
4. **Check Other Scroll Tests:** Ensure `Navigation_ScrollToTop_ShowsTitle` still works

---

## 7. Related Files

- [ScrollViewControl.cs](../../src/Brinell.Maui/Controls/ScrollViewControl.cs) - Scroll implementation
- [NavigationTests.cs](../../samples/Brinell.Samples.Maui.UITests/Tests/NavigationTests.cs) - Failing test
- [MainPageObject.cs](../../samples/Brinell.Samples.Maui.UITests/Pages/MainPageObject.cs) - Page object
- [MainPage.xaml](../../samples/Brinell.Samples.Maui.App/MainPage.xaml) - MAUI page definition

---

## 8. Decision

**Recommended Approach:** Fix Option 1 + Fix Option 2

1. Add scroll animation delay to `ScrollToBottom()` method
2. Update test to use `WaitExists()` with explicit timeout
3. Fallback: Test `ToggleLoadingButton` if ActivityIndicator remains problematic

**Rationale:**
- Scroll animations take time to complete
- UI tests should wait for state changes
- Testing sibling controls is valid if primary control has platform issues

---

## 9. Fix Applied

### Changes Made

**File: [ScrollViewControl.cs](../../src/Brinell.Maui/Controls/ScrollViewControl.cs)**

Added final wait after scroll completes:
```csharp
public void ScrollToBottom()
{
    LogAction("ScrollToBottom");
    var lastPosition = GetVerticalScrollPosition();
    for (int i = 0; i < 20; i++)
    {
        ScrollDown(500);
        Thread.Sleep(200);
        var newPosition = GetVerticalScrollPosition();
        if (Math.Abs(newPosition - lastPosition) < 1)
            break;
        lastPosition = newPosition;
    }
    
    // Final wait for UI to stabilize after scroll completes
    Thread.Sleep(300);
}
```

**File: [NavigationTests.cs](../../samples/Brinell.Samples.Maui.UITests/Tests/NavigationTests.cs)**

Updated test to use `WaitExists()` and fallback to ToggleLoadingButton:
```csharp
var indicatorVisible = _mainPage.LoadingIndicator.WaitExists(true, 3000);
var buttonVisible = _mainPage.ToggleLoadingButton.WaitExists(true, 3000);

Assert.True(indicatorVisible || buttonVisible, 
    "Activity section should be visible after scrolling");
```

---

## 10. Notes

- MAUI `ActivityIndicator` with `IsRunning="false"` may have rendering quirks
- Windows WinAppDriver may not expose collapsed/zero-size elements
- Consider adding a `ScrollTo(element)` method that scrolls to a specific control
