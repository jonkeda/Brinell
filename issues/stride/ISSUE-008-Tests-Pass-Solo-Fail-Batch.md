# ISSUE-008: Tests Pass Solo But Fail in Batch

**Created:** January 3, 2026  
**Status:** In Progress  
**Severity:** High  
**Category:** Test Execution / Focus Management  

---

## Summary

The same test passes when run individually but fails when run as part of a batch with other tests.

---

## Symptoms

- `Player_MoveNorth_PositionIncreases` passes when run alone
- Same test fails when run with other tests
- All 10 keyboard-dependent tests fail in batch
- Focus appears to be the common factor

---

## Root Cause

Even with sequential execution (`parallelizeTestCollections: false`):

1. **Process cleanup timing** - Previous game process may still be closing when new test starts
2. **Focus state unpredictability** - New game window doesn't immediately get focus
3. **Focus stealing by IDE** - VS Code or test runner reclaims focus between tests
4. **Window handle conflicts** - Old window handles may still be cached

---

## Test Execution Flow Problem

```
Test 1 Ends
    ↓
Game Process Shutting Down (async)
    ↓
Test 2 Starts
    ↓
New Game Process Starting
    ↓
Test 2 tries to use keyboard
    ↓
FAIL: Focus on wrong window (VS Code, old game, etc.)
```

---

## Solution

### 1. Always Click Before Keyboard Input

Never assume focus persists. Click the game window before every keyboard operation:

```csharp
public void PressKey(VirtualKey key)
{
    EnsureGameHasKeyboardFocus();  // Click window
    _inputSimulator.PressKey(key);
}

public void HoldKey(VirtualKey key, int durationMs)
{
    EnsureGameHasKeyboardFocus();  // Click window
    _inputSimulator.HoldKey(key, durationMs);
}
```

### 2. Wait for Previous Game to Close

```csharp
private async Task StartGameAsync()
{
    // Wait for any existing game process to fully exit
    await WaitForNoGameProcessAsync(timeout: 5000);
    
    // Start new game
    _gameProcess = Process.Start(...);
    
    // Wait for window to be ready
    await WaitForWindowAsync(timeout: 10000);
}
```

### 3. Verify Focus After Establishing

```csharp
private void EnsureGameHasKeyboardFocus()
{
    ClickGameWindow();
    Thread.Sleep(200);
    
    // Verify focus was actually obtained
    if (GetForegroundWindow() != _gameWindowHandle)
    {
        // Retry click
        ClickGameWindow();
        Thread.Sleep(200);
    }
}
```

---

## Verification

Run the following command to test batch execution:

```powershell
dotnet test --filter "Player_Move|PressEscape|OpenAndClose|Settings" --no-build
```

**Expected:** All 10 tests pass.

---

## Related Issues

- [ISSUE-004-Keyboard-Input-Focus](ISSUE-004-Keyboard-Input-Focus.md)
- [ISSUE-005-GetForegroundWindow-Unreliable](ISSUE-005-GetForegroundWindow-Unreliable.md)
- [ISSUE-006-SetForegroundWindow-Restrictions](ISSUE-006-SetForegroundWindow-Restrictions.md)

---

## Related Files

- `src/Brinell.Stride/Infrastructure/StrideTestContext.cs`
- `src/Brinell.Stride/Infrastructure/StrideUITestBase.cs`
