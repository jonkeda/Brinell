# ISSUE-004: Keyboard Input Requires Window Focus

**Created:** January 3, 2026  
**Status:** In Progress  
**Severity:** High  
**Category:** Input Simulation  

---

## Summary

Keyboard input via `SendInput` requires the game window to have focus. The `HoldKey` method doesn't ensure focus before sending keys, causing keyboard input to go to the wrong window.

---

## Symptoms

- Player movement tests fail (WASD keys)
- ESC key doesn't open settings
- Tests pass individually but fail in batch
- Keyboard input goes to VS Code or other windows

---

## Affected Tests (10)

### Category A: Keyboard Input Issues (4 tests)
1. `Player_MoveNorth_PositionIncreases` - Uses WASD/HoldKey
2. `Player_MoveEast_PositionChanges` - Uses WASD/HoldKey
3. `Game_PressEscape_OpensSettings` - Uses ESC key
4. `Game_OpenAndCloseSettings_ReturnsToGame` - Uses ESC key

### Category B: Settings Page Tests (6 tests)
Depend on ESC key working to open settings.
5. `SettingsPage_Opens_ShowsAllSections`
6. `AudioSettings_MuteToggle_CanBeToggled`
7. `Settings_ApplyButton_IsClickable`
8. `Settings_ApplyButton_ClickDoesNotCloseSettings`
9. `Settings_CloseButton_ClosesSettings`
10. `Settings_ResetButton_ResetsAllValues`

---

## Root Cause

The `HoldKey` method in `StrideTestContext` doesn't call `EnsureGameHasFocus()` before sending key input:

```csharp
public void HoldKey(VirtualKey key, int durationMs)
{
    // Missing: EnsureGameHasFocus()
    _inputSimulator.HoldKey(key, durationMs);
}
```

---

## Solution

### Fix: Add EnsureGameHasFocus to HoldKey

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

---

## Verification Checklist

- [ ] Fix HoldKey focus issue
- [ ] Run `Player_MoveNorth_PositionIncreases`
- [ ] Run `Game_PressEscape_OpensSettings`
- [ ] Run `SettingsPage_Opens_ShowsAllSections`
- [ ] Run all 10 tests together

---

## Related Issues

- [ISSUE-005-GetForegroundWindow-Unreliable](ISSUE-005-GetForegroundWindow-Unreliable.md)
- [ISSUE-006-SetForegroundWindow-Restrictions](ISSUE-006-SetForegroundWindow-Restrictions.md)
- [ISSUE-008-Tests-Pass-Solo-Fail-Batch](ISSUE-008-Tests-Pass-Solo-Fail-Batch.md)

---

## Related Files

- `src/Brinell.Stride/Infrastructure/StrideTestContext.cs`
- `src/Brinell.Stride/Input/StrideInputSimulator.cs`
