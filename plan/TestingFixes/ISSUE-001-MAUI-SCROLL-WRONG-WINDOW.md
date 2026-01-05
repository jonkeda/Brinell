# ISSUE-001: MAUI Scroll Actions Sent to Wrong Window

**Date:** January 5, 2026  
**Status:** ✅ RESOLVED  
**Severity:** High  
**Affected Tests:** 11 MAUI UITests (TextInput, Toggle, Slider, ActivityIndicator)  
**Resolution Date:** January 5, 2026

---

## 1. Problem Description

When running MAUI UI tests that require scrolling to find elements below the visible area, the scroll/swipe actions are being sent to the wrong application window (VS Code) instead of the MAUI app under test.

### Observed Behavior

1. **Window Resize:** The MAUI app window height is reduced (lower edge moves up)
2. **Misrouted Actions:** Scroll/swipe gestures are sent to VS Code (the window behind the MAUI app)
3. **Test Failure:** Elements never become visible, causing 18-second timeout failures

### User Report

> "What I see happening with the tests is that first the window is made smaller (less high), as the lower side is moved up, and then the scroll actions are sent to the window below it (in this case Visual Studio Code). So it seems the mouse move or keystrokes are not being sent to the right app."

---

## 2. Root Cause Analysis

### 2.1 Scroll Implementation

The scroll is implemented in `ScrollViewControl.ScrollDown()` which calls:
```csharp
public void ScrollDown(int distance = 300)
{
    LogAction("ScrollDown", distance.ToString());
    SwipeUp(distance);  // Swipe up to scroll content down
}
```

This calls `ControlBase.Swipe()`:
```csharp
public virtual void Swipe(SwipeDirection direction, int distance = 200)
{
    var element = WaitForElementVisible();
    if (element == null)
        ThrowCheckFailed("Swipe", $"Element '{AutomationId}' not visible for swipe.");
    _context.Driver.PerformSwipe(element!, direction, distance);
    LogAction("Swipe", $"{direction}, {distance}px");
}
```

### 2.2 Windows-Specific Scroll Implementation

For Windows, `AppiumDriverAdapter.PerformSwipe()` uses `PerformWindowsScroll()`:

```csharp
private void PerformWindowsScroll(AppiumElement element, SwipeDirection direction, int distance)
{
    var pen = new PointerInputDevice(PointerKind.Pen, "pen");
    var actions = new ActionSequence(pen);
    
    var location = element.Location;
    var size = element.Size;
    var startX = location.X + size.Width / 2;
    var startY = location.Y + size.Height / 2;
    
    var (endX, endY) = direction switch
    {
        SwipeDirection.Up => (startX, startY - distance),
        // ...
    };
    
    // Move to element center, click, drag, release
    actions.AddAction(pen.CreatePointerMove(CoordinateOrigin.Viewport, startX, startY, TimeSpan.Zero));
    actions.AddAction(pen.CreatePointerDown(MouseButton.Left));
    actions.AddAction(pen.CreatePointerMove(CoordinateOrigin.Viewport, endX, endY, TimeSpan.FromMilliseconds(300)));
    actions.AddAction(pen.CreatePointerUp(MouseButton.Left));
    
    _driver.PerformActions(new List<ActionSequence> { actions });
}
```

### 2.3 Suspected Issues

1. **CoordinateOrigin.Viewport Issue:**  
   The coordinates use `CoordinateOrigin.Viewport` which may be calculating coordinates relative to the wrong viewport (screen vs. app window).

2. **Window Focus Loss:**  
   The pointer actions may be causing the MAUI app to lose focus, and subsequent actions go to the window that gains focus.

3. **PointerKind.Pen on Windows:**  
   Using `PointerKind.Pen` instead of `PointerKind.Mouse` may have unexpected behavior on Windows desktop apps.

4. **Window Resize Trigger:**  
   The pointer move to coordinates outside the app window may be triggering a window resize operation instead of scroll.

5. **Appium WinAppDriver Issue:**  
   The Windows Application Driver may have bugs with pointer actions and viewport coordinates.

---

## 3. Affected Code Locations

| File | Location | Description |
|------|----------|-------------|
| `AppiumDriverAdapter.cs` | Lines 438-462 | `PerformWindowsScroll()` implementation |
| `ControlBase.cs` | Lines 605-615 | `Swipe()` method |
| `ScrollViewControl.cs` | Lines 56-62 | `ScrollDown()` method |
| `ScrollViewControl.cs` | Lines 119-140 | `ScrollToElement()` method |

---

## 4. Affected Tests

All tests using `ScrollToElement()` or scrolling to find off-screen elements:

### TextInputTests.cs
- `TextInput_GreetButton_RespondsToClick` (SKIPPED)
- `TextInput_MessageEditor_CanEnterMultilineText` (SKIPPED)
- `TextInput_MessageEditor_PersistsText` (SKIPPED)

### ToggleControlTests.cs
- All 5 tests (SKIPPED)

### SliderTests.cs
- All 5 tests (SKIPPED)

---

## 5. Proposed Solutions

### 5.1 Solution A: Use Keyboard Scroll Instead of Pointer Actions

Replace pointer-based scrolling with keyboard-based scrolling:
```csharp
private void PerformWindowsScroll(AppiumElement element, SwipeDirection direction, int distance)
{
    // Focus the element first
    element.Click();
    
    // Use keyboard for scrolling
    var keyboard = _driver.Keyboard;
    int scrollSteps = distance / 50; // Approximate
    
    var key = direction switch
    {
        SwipeDirection.Up => Keys.PageUp,
        SwipeDirection.Down => Keys.PageDown,
        _ => Keys.Down
    };
    
    for (int i = 0; i < scrollSteps; i++)
    {
        keyboard.SendKeys(key);
        Thread.Sleep(50);
    }
}
```

### 5.2 Solution B: Use Mouse Wheel Instead of Drag

Use mouse wheel scrolling instead of drag:
```csharp
private void PerformWindowsScroll(AppiumElement element, SwipeDirection direction, int distance)
{
    // Focus the ScrollView
    element.Click();
    Thread.Sleep(100);
    
    // Use Actions API with scroll wheel
    var actions = new Actions(_driver);
    
    int scrollAmount = direction == SwipeDirection.Up ? distance : -distance;
    actions.ScrollByAmount(0, scrollAmount);
    actions.Perform();
}
```

### 5.3 Solution C: Use Element-Relative Coordinates

Ensure coordinates are relative to the app window, not the screen:
```csharp
private void PerformWindowsScroll(AppiumElement element, SwipeDirection direction, int distance)
{
    // Get app window location to offset coordinates
    var appWindow = _driver.FindElement(MobileBy.ClassName("ApplicationFrameWindow"));
    var appLocation = appWindow.Location;
    
    var location = element.Location;
    var size = element.Size;
    
    // Calculate relative to app window
    var startX = location.X - appLocation.X + size.Width / 2;
    var startY = location.Y - appLocation.Y + size.Height / 2;
    
    // ... rest of implementation
}
```

### 5.4 Solution D: Bring App Window to Front Before Actions

Ensure the app window has focus before performing actions:
```csharp
private void PerformWindowsScroll(AppiumElement element, SwipeDirection direction, int distance)
{
    // Bring app to front and ensure focus
    _driver.SwitchTo().Window(_driver.CurrentWindowHandle);
    
    // Click somewhere in the app to ensure it has focus
    var appElement = _driver.FindElement(MobileBy.ClassName("Window"));
    appElement.Click();
    Thread.Sleep(100);
    
    // Now perform the scroll
    // ...
}
```

---

## 6. Investigation Steps

1. **Add Logging:** Add detailed logging to `PerformWindowsScroll()` to capture:
   - Element location and size
   - Calculated start/end coordinates
   - Current window handle
   - App window position

2. **Capture Screenshots:** Capture screenshots before and after scroll attempts

3. **Test Focus Management:** 
   - Add explicit window focus before scroll
   - Test with different PointerKind values

4. **Test Coordinate Systems:**
   - Try `CoordinateOrigin.Element` instead of `CoordinateOrigin.Viewport`
   - Calculate absolute screen coordinates manually

5. **Test Alternative Scroll Methods:**
   - Try `SendKeys(Keys.PageDown)` approach
   - Try Windows UI Automation scroll patterns directly

---

## 7. Workaround (Current)

Tests that require scrolling are currently skipped with:
```csharp
[Fact(Skip = "ISSUE-001: Scroll actions sent to wrong window - requires framework fix")]
```

This allows the test suite to continue running while the issue is investigated.

---

## 8. References

- Appium Windows Driver: https://github.com/appium/appium-windows-driver
- W3C WebDriver Actions: https://www.w3.org/TR/webdriver/#actions
- Selenium Actions API: https://www.selenium.dev/documentation/webdriver/actions_api/

---

## 9. Resolution Tracking

| Date | Action | Result |
|------|--------|--------|
| 2026-01-05 | Issue documented | - |
| 2026-01-05 | Tests skipped as workaround | 11 tests skipped |
| 2026-01-05 | Attempted Selenium Actions API `ScrollByAmount()` | Failed - "Only pen and touch pointer types supported" |
| 2026-01-05 | **Implemented Solution A: Keyboard-based scrolling** | ✅ SUCCESS |
| 2026-01-05 | Re-enabled all skipped tests | ✅ 21/21 tests passing |

---

## 10. Implemented Solution

**Solution A: Keyboard-based scrolling** was implemented in `AppiumDriverAdapter.PerformWindowsScroll()`:

```csharp
private void PerformWindowsScroll(AppiumElement element, SwipeDirection direction, int distance)
{
    // Click element first to ensure the app window has focus
    try { element.Click(); Thread.Sleep(100); } catch { }
    
    // Use keyboard-based scrolling
    int scrollSteps = Math.Max(1, distance / 200);
    string key = direction switch
    {
        SwipeDirection.Up => Keys.PageDown,    // Swipe up = scroll content down
        SwipeDirection.Down => Keys.PageUp,    // Swipe down = scroll content up
        _ => Keys.PageDown
    };
    
    for (int i = 0; i < scrollSteps; i++)
    {
        element.SendKeys(key);
        Thread.Sleep(50);
    }
    Thread.Sleep(150);
}
```

### Why This Works

1. **Focus Management:** Clicking the element first ensures the MAUI app window has focus
2. **Keyboard Input:** SendKeys goes directly to the focused element within the app
3. **No Coordinate Issues:** No viewport coordinate calculations that could misroute to wrong window
4. **Page Up/Down:** Native scroll behavior that works reliably on Windows

### Test Results After Fix

```
Test Run Successful.
Total tests: 21
     Passed: 21
 Total time: 1.6396 Minutes
```

