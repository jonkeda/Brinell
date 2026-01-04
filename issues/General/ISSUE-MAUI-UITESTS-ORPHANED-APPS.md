# ISSUE: MAUI UI Tests Leave Orphaned App Processes

**Date:** January 4, 2026  
**Status:** FIXED  
**Priority:** High  
**Category:** Resource Leak / Test Infrastructure

---

## Symptom

After running MAUI UI tests, multiple app processes remain running (not closed).

**Evidence (before fix):**
```
Name                        Id StartTime
----                        -- ---------
Brinell.Samples.Maui.App 29288 04-Jan-26 20:07:46
Brinell.Samples.Maui.App 55120 04-Jan-26 20:07:22
Brinell.Samples.Maui.App 61080 04-Jan-26 20:07:44
```

---

## Root Cause

`driver.Quit()` disconnects from WinAppDriver session but doesn't always close the app process.

The `ms:forcequit` capability was not set, so WinAppDriver did not forcefully terminate the app.

---

## Solution Applied

Added `ms:forcequit` capability to force close the app when the session ends:

```csharp
// MauiTestBase6.cs - CreateDriver()
options.AddAdditionalAppiumOption("ms:forcequit", true);
```

From [Appium Windows Driver docs](https://github.com/appium/appium-windows-driver):
> **ms:forcequit** - Defines if the WinAppDriver should be started with the /forcequit 
> command line argument which will forcefully kill the application process during 
> session termination. Default false.

---

## Verification

After fix, ran 6 tests (including 2 failures):
- ✅ No orphaned processes after test completion
- ✅ Failed tests also clean up properly

```powershell
> Get-Process -Name "*Brinell*" -ErrorAction SilentlyContinue
# (no output - all processes closed)
```

---

## Files Modified

- `samples/Brinell.Samples.Maui.UITests.ControlObject6/MauiTestBase6.cs`

