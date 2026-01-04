# ISSUE: MAUI UI Tests - Delay After App Launch

**Date:** January 4, 2026  
**Status:** FIXED  
**Priority:** High  
**Category:** Performance / Configuration

---

## Symptom

After the MAUI app window appears, there is a long pause (~10 seconds) before the test actually starts interacting with the UI.

**Observation:** App starts → Nothing happens for ~10 seconds → Test starts clicking

---

## Root Cause

`ms:waitForAppLaunch` was set to `"10"` = **10-second mandatory wait** after app launch, before WinAppDriver attaches.

---

## Solution Applied

Changed from fixed wait to **poll-wait pattern** (like all other waits in the framework):

### Before (Fixed 10-second wait):
```csharp
options.AddAdditionalAppiumOption("ms:waitForAppLaunch", "10");  // Always waits 10s!
```

### After (Poll-wait, returns immediately when ready):
```csharp
// Minimal fixed wait (required by WinAppDriver)
options.AddAdditionalAppiumOption("ms:waitForAppLaunch", "1");

// Then poll-wait ourselves - returns as soon as app is ready
WaitForAppReady(_driver, timeoutMs: 10000, pollingMs: 100);
```

### `WaitForAppReady` Implementation:
```csharp
private void WaitForAppReady(AppiumDriver driver, int timeoutMs, int pollingMs)
{
    var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
    
    while (DateTime.UtcNow < deadline)
    {
        try
        {
            var handles = driver.WindowHandles;
            if (handles.Count > 0)
                return;  // App is ready!
        }
        catch (WebDriverException)
        {
            // Not ready yet, keep polling
        }
        Thread.Sleep(pollingMs);
    }
    
    throw new TimeoutException($"App did not become ready within {timeoutMs}ms");
}
```

---

## Expected Improvement

| Metric | Before | After |
|--------|--------|-------|
| Wait after app launch | 10s (always) | ~1-2s (until ready) |
| Time saved per test | - | ~8s |
| 44 tests improvement | - | ~6 minutes faster |

---

## Files Modified

- `samples/Brinell.Samples.Maui.UITests.ControlObject6/MauiTestBase6.cs`

---

## Hypotheses Checked

| # | Hypothesis | Status | Finding |
|---|------------|--------|---------|
| 1 | `ms:waitForAppLaunch` capability set to 10 seconds | ROOT CAUSE | Fixed - now uses poll-wait |
| 2 | Implicit wait configured on the driver | Clear | Not set in actual code |
| 3 | WaitLoaded() in page object has hardcoded delay | Clear | Polls at 100ms, returns immediately on success |
| 4 | Thread.Sleep or Task.Delay somewhere in startup | Clear | None in test startup path |
| 5 | Appium session creation takes time after app launch | Clear | Part of ms:waitForAppLaunch |
| 6 | Element finding with long timeout on first lookup | Clear | No implicit wait set |
| 7 | Logger initialization blocking | Clear | Not blocking |
| 8 | FindElement retry loop with delays | Clear | Only polls if element not found |

---

## Related Issues

- `ISSUE-MAUI-UITESTS-SLOW-EXECUTION.md` - Documents app-per-test overhead (separate issue)

---

## References

- [Appium Windows Driver - ms:waitForAppLaunch documentation](https://github.com/appium/appium-windows-driver#usage)
