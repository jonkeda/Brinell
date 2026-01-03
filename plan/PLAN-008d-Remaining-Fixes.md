# PLAN-008d: Remaining Stride Test Fixes

**Created:** January 3, 2026
**Status:** In Progress

---

## Summary

- **Total Tests:** 55
- **Passed:** 45
- **Failed:** 10

---

## Failing Tests (10)

### Category A: Keyboard Input Issues (4 tests)
Tests that require ESC key or WASD movement aren't working.

1. `Player_MoveNorth_PositionIncreases` - Uses WASD/HoldKey
2. `Player_MoveEast_PositionChanges` - Uses WASD/HoldKey
3. `Game_PressEscape_OpensSettings` - Uses ESC key
4. `Game_OpenAndCloseSettings_ReturnsToGame` - Uses ESC key

**Root Cause:** Keyboard input via `SendInput` requires window focus. The `HoldKey` method doesn't call `EnsureGameHasFocus()` before sending keys.

### Category B: Settings Page Tests (6 tests)
All require Settings overlay to be visible (need ESC to work first).

5. `SettingsPage_Opens_ShowsAllSections`
6. `AudioSettings_MuteToggle_CanBeToggled`
7. `Settings_ApplyButton_IsClickable`
8. `Settings_ApplyButton_ClickDoesNotCloseSettings`
9. `Settings_CloseButton_ClosesSettings`
10. `Settings_ResetButton_ResetsAllValues`

**Root Cause:** Depends on Category A - ESC key must work to open settings.

---

## Fix Plan

### Fix 1: Add EnsureGameHasFocus to HoldKey

The `HoldKey` method in `StrideTestContext` doesn't ensure focus before sending key input.

**File:** `src/Brinell.Stride/Infrastructure/StrideTestContext.cs`

```csharp
public void HoldKey(VirtualKey key, int durationMs)
{
    if (!EnsureGameHasFocus())
    {
        Log("Warning: Game may not have focus, key hold might go to wrong window");
    }
    _inputSimulator.HoldKey(key, durationMs);
}
```

### Fix 2: Verify Shift Key Release

The shift key is still getting stuck. Need to ensure `ReleaseAllModifiers()` is called properly and that WASD keys (which are used in movement tests) aren't causing modifier issues.

---

## Test Order

1. Fix HoldKey focus issue
2. Run `Player_MoveNorth_PositionIncreases`
3. Run `Game_PressEscape_OpensSettings`
4. If ESC works, run `SettingsPage_Opens_ShowsAllSections`
5. Continue with remaining settings tests

---

## Progress

- [ ] Fix 1: Add EnsureGameHasFocus to HoldKey
- [ ] Fix 2: Verify shift key release
- [ ] Test Player_MoveNorth_PositionIncreases
- [ ] Test Game_PressEscape_OpensSettings
- [ ] Test SettingsPage_Opens_ShowsAllSections
- [ ] Run all tests
