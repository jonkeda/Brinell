# Plan: Run & Test the Stride UI Tests

**Created:** February 22, 2026
**Status:** EXECUTED — 8 passed, 21 failed (see Results section)
**Prereq:** The refactored Stride framework in `srcnew/` builds clean (all 5 projects: Automation, Stride, UITests, Tests, Sample App).

---

## Problem

The 29 UI tests in `testsnew/Brinell.Stride.UITests/` all fail because they require the Stride sample game to be running with its automation pipe server active. The `StrideAppFixture` also has a lifecycle bug — it doesn't implement `IAsyncLifetime`, so xUnit never calls `InitializeAsync()`.

---

## Architecture Overview

```
Test Process                          Game Process
┌──────────────────┐                  ┌──────────────────────────┐
│ StrideAppFixture  │                  │ Brinell.Samples.Stride.App│
│  └ StrideGameDriver│──starts──►     │  └ SampleStrideGame         │
│  └ NamedPipeChannel│◄──pipe──►      │  └ AutomationGameSystem     │
│  └ StrideTestContext│                │  └ AutomationServer         │
│                    │                 │  └ StrideUIHandler          │
│ CounterTests       │                 └──────────────────────────┘
│ GreetingTests      │
│ GameplayTests      │
│ SettingsTests      │
└──────────────────┘
```

- **Pipe name:** `Brinell.Stride.Automation`
- **Protocol:** JSON-over-named-pipe, one command per line, one response per line
- **Game startup flag:** `--automation` (enables `#if AUTOMATION_ENABLED` code in game)
- **Game TFM:** `net10.0-windows` (Stride requires Windows)
- **Test TFM:** `net10.0`

---

## Prerequisites

1. **Build the sample app** (must produce the `.exe` the tests launch):
   ```powershell
   dotnet build samples/Brinell.Samples.Stride.App/Brinell.Samples.Stride.App.csproj -c Debug
   ```

2. **Build the test project:**
   ```powershell
   dotnet build testsnew/Brinell.Stride.UITests/Brinell.Stride.UITests.csproj
   ```

3. **Verify the app runs standalone** (manual smoke test):
   ```powershell
   $exe = "samples\Brinell.Samples.Stride.App\bin\Debug\net10.0-windows\Brinell.Samples.Stride.App.exe"
   Start-Process $exe -ArgumentList "--automation"
   # → Window appears, pipe "Brinell.Stride.Automation" should be created
   # → Close the window manually
   ```

4. **Verify pipe is available** while app runs:
   ```powershell
   Test-Path "\\.\pipe\Brinell.Stride.Automation"
   ```

---

## Issues to Fix Before Tests Can Pass

### Issue 1: StrideAppFixture Missing IAsyncLifetime

**File:** `testsnew/Brinell.Stride.UITests/StrideUITestBase.cs`

`StrideAppFixture` extends `StrideTestFixtureBase` which only implements `IDisposable`. For xUnit's `IClassFixture<T>` to call `InitializeAsync()` / `DisposeAsync()`, the fixture must implement `IAsyncLifetime`.

**Fix:** Add `IAsyncLifetime` to `StrideAppFixture`:

```csharp
public class StrideAppFixture : StrideTestFixtureBase, IAsyncLifetime
{
    protected override string GetDefaultAppPath() { ... }

    public new async Task InitializeAsync() => await base.InitializeAsync();
    public new async Task DisposeAsync() => await base.DisposeAsync();
}
```

**Why in fixture, not base:** `StrideTestFixtureBase` lives in `srcnew/Brinell.Stride/` which doesn't reference xUnit. The test project does.

### Issue 2: App Exe Path May Be Wrong

The fixture computes the path via relative traversal from `AppContext.BaseDirectory`:
```
{assemblyDir}/../../../../../samples/Brinell.Samples.Stride.App/bin/Debug/net10.0-windows/Brinell.Samples.Stride.App.exe
```

This depends on the exact `bin/Debug/net10.0/` depth. Could break.

**Safer approach:** Also support `STRIDE_APP_PATH` env variable (already done in `StrideTestFixtureBase.GetAppPath()`). Can set in test run:
```powershell
$env:STRIDE_APP_PATH = "E:\repos\Private\Iosk\Oravey\Brinell\samples\Brinell.Samples.Stride.App\bin\Debug\net10.0-windows\Brinell.Samples.Stride.App.exe"
dotnet test testsnew/Brinell.Stride.UITests/
```

### Issue 3: Startup Timing

The `StrideGameDriver.StartAsync()` waits for `MainWindowHandle != IntPtr.Zero`, then `StrideTestFixtureBase.InitializeAsync()` calls `WaitForGameReady()`. Need to ensure the game's `BeginRun()` → `UseAutomation()` → pipe server start happens within the `StartupTimeoutMs` (15 seconds).

Stride games can take several seconds to load assets. If the 15s timeout isn't enough, increase `StartupTimeoutMs` in `StrideAppFixture.CreateOptions()`.

### Issue 4: Game Uses `#if AUTOMATION_ENABLED` Compile-Time Check

The app only registers automation when built with `AUTOMATION_ENABLED` defined, which is set for `Debug` configuration only:
```xml
<PropertyGroup Condition="'$(Configuration)' == 'Debug'">
    <DefineConstants>$(DefineConstants);AUTOMATION_ENABLED</DefineConstants>
</PropertyGroup>
```

**Must build in Debug** (the default). If someone builds Release, automation won't be available and tests will timeout on pipe connect.

---

## Steps to Run the Tests

### Step 1: Fix StrideAppFixture (add IAsyncLifetime)
```
File: testsnew/Brinell.Stride.UITests/StrideUITestBase.cs
```

### Step 2: Build Everything
```powershell
# Build the game app (Debug, so AUTOMATION_ENABLED is defined)
dotnet build samples/Brinell.Samples.Stride.App/Brinell.Samples.Stride.App.csproj -c Debug

# Build the test project
dotnet build testsnew/Brinell.Stride.UITests/Brinell.Stride.UITests.csproj
```

### Step 3: Quick Smoke Test (Manual)
Verify the pipe works independently before running xUnit:
```powershell
# Use the existing TestRunner.ps1 as reference, or run:
$exe = "samples\Brinell.Samples.Stride.App\bin\Debug\net10.0-windows\Brinell.Samples.Stride.App.exe"
$proc = Start-Process $exe -ArgumentList "--automation" -PassThru
Start-Sleep -Seconds 3

# Check pipe
Test-Path "\\.\pipe\Brinell.Stride.Automation"  # Should be True

# Cleanup
$proc.Kill()
```

### Step 4: Run Tests
```powershell
# Option A: Let tests launch the game via StrideGameDriver
dotnet test testsnew/Brinell.Stride.UITests/Brinell.Stride.UITests.csproj -v normal

# Option B: Use env var for explicit app path
$env:STRIDE_APP_PATH = (Resolve-Path "samples\Brinell.Samples.Stride.App\bin\Debug\net10.0-windows\Brinell.Samples.Stride.App.exe").Path
dotnet test testsnew/Brinell.Stride.UITests/Brinell.Stride.UITests.csproj -v normal
```

### Step 5: Troubleshoot if Needed

| Symptom | Likely Cause | Fix |
|---------|-------------|-----|
| `Context not initialized` | `IAsyncLifetime` missing on fixture | Issue 1 fix |
| `Game executable not found` | Wrong path in `GetDefaultAppPath()` | Set `STRIDE_APP_PATH` env var |
| `Timeout connecting to pipe` | App crashed or AUTOMATION_ENABLED missing | Build in Debug; check app runs manually |
| `Game process exited unexpectedly` | Missing Stride NuGet packages or GPU issue | Build app standalone first; check error output |
| `Pipe exists but empty response` | JSON protocol mismatch | Check `AutomationCommand` serialization matches between Automation and Stride projects |
| Tests pass individually but fail in batch | Shared fixture state pollution | `IClassFixture` shares one game instance; tests must not depend on order |

---

## Test Inventory (29 tests)

| Class | Count | Depends On |
|-------|-------|-----------|
| `CounterTests` | 5 | MainPage: IncrementButton, DecrementButton, ResetButton, CounterDisplay |
| `GreetingTests` | 4 | MainPage: NameInput, GreetButton, GreetingDisplay |
| `GameplayTests` | 7 | GamePage: GameTitle, PositionDisplay, EscHint, MovementHint + keyboard input |
| `SettingsTests` | 11+2 | SettingsPage: Sliders, toggles, inputs, buttons (opened via ESC from GamePage) |

**Note:** `CounterTests` and `GreetingTests` use `MainPage` (AutomationId="MainPanel"), while `GameplayTests` and `SettingsTests` use `GamePage` (AutomationId="HUD") and `SettingsPage` (AutomationId="SettingsPanel"). Need to verify the sample app actually renders all three panels, or tests may need to navigate between them.

---

## Optional: Run Script

Could create a `run-stride-tests.ps1` script that:
1. Builds the app
2. Builds the tests
3. Optionally runs the smoke test
4. Runs `dotnet test` with proper env vars
5. Collects test results and screenshots

---

## Definition of Done

- [x] Issue 1 fixed (`IAsyncLifetime` on `StrideAppFixture`)
- [x] Sample app builds and launches successfully with `--automation`
- [x] Pipe `Brinell.Stride.Automation` is created and responds to commands
- [x] `dotnet test` runs all 29 tests with the game auto-launching
- [ ] At least the counter and greeting tests pass (MainPage panel exists)
- [x] Document any tests that need sample app changes (missing UI elements)

---

## Execution Results (February 22, 2026)

### Summary: 8 Passed, 21 Failed

The game launches successfully, the pipe connects, element state is queried correctly, and physical clicks are landing (partial counter increments observed). The 21 failures fall into 4 categories:

### Failure Category 1: Settings Page Never Loads (13 tests)
**Tests:** All 11 SettingsTests + Game_PressEscape_OpensSettings, Game_OpenAndCloseSettings_ReturnsToGame
**Error:** `PageLoadException: Expected page 'Settings Page' to be loaded but loaded state is False`
**Root Cause:** `GamePage.OpenSettings()` calls `PressEscape()` which uses `StrideInputSimulator` to send ESC keypress. The game may not have focus, or the ESC handler in the game doesn't toggle the settings overlay. The `SettingsPanel` AutomationId must exist in the UI tree only when settings are visible.
**Fix needed:** Ensure game window is focused before key presses; or use a server-side command to open settings.

### Failure Category 2: Counter Not Fully Updating (2 tests)
**Tests:** IncrementButton_Click_IncreasesCounter (expected 1, got 0), Counter_MultipleIncrements_Accumulates (expected 5, got 2)
**Error:** `Assert.Equal() Failure: Values differ`
**Root Cause:** Physical mouse clicks partially land. 2 out of 5 increments registered, suggesting the click coordinates are correct but the game processes some clicks while others arrive during frame transitions or UI hover state changes. The `WaitFor` condition in `IncrementCounter()` may timeout before the value updates.
**Fix needed:** Increase polling timeout in `WaitFor`, or add explicit post-click delay. Better: use server-side click command instead of physical mouse.

### Failure Category 3: Greeting Display Empty (2 tests)
**Tests:** GreetButton_WithName_DisplaysGreeting, GreetButton_WithEmptyName_DisplaysDefaultGreeting
**Error:** `AssertionException: Control 'GreetingDisplay' text mismatch. Expected: 'Hello, Alice!', Actual: ''`
**Root Cause:** `SetText` on the EditText may not be entering text correctly (physical keyboard simulation), so the greet button fires with empty input but the GreetingDisplay doesn't update.
**Fix needed:** Verify EditText.SetText works via server-side SetElementText command; check greeting logic in sample app.

### Failure Category 4: Movement Not Detected (2 tests)  
**Tests:** Player_MoveNorth_PositionChanges, Player_MoveSouth_PositionChanges
**Error:** `Assert.NotEqual() - Position stays "Position: (0.0, 0.0)"`
**Root Cause:** Key hold simulation (W/S keys for 300ms) doesn't move the player. Likely because the game window doesn't have keyboard focus, or the game's `Update()` loop doesn't see the simulated key input through `Input.IsKeyDown()`.
**Fix needed:** Key simulation must work with Stride's input system. Physical key events may not be captured by Stride's `Input` class. May need a server-side "SimulateInput" command.

### Tests That Passed (8)
These are read-only state assertions that don't depend on input simulation:
- Counter_InitialValue_IsZero ✅
- ResetButton_Click_ResetsToZero ✅  
- DecrementButton_Click_DecreasesCounter ✅
- Game_Initializes_ShowsHUD ✅
- Player_InitialPosition_IsAtOrigin ✅  
- HUD_EscHintSaysPress ✅
- NameInput_EnterText_DisplaysInField ✅
- NameInput_ClearAndEnter_ReplacesText ✅

### Key Insight
The pipe communication and element state querying work perfectly. The failures are all in **physical input simulation** — clicks are unreliable and key presses don't reach Stride's input system. The fix path is to add **server-side interaction commands** in the automation handler (click element by ID, press key by name, input text) rather than relying on OS-level mouse/keyboard simulation.
