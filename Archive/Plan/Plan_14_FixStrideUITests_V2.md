# Plan 14: Fix Stride UI Tests V2

## Status: In Progress

## Date: December 31, 2025

## Overview

This plan addresses the failing Stride UI tests after the NET 10 upgrade and CommunityToolkit integration. The previous plan (Plan 13) fixed the foundation, but several critical issues remain.

---

## Root Cause Analysis

### Issue 0: Tests Hang (NEW - December 31, 2025)
**Symptom**: Tests hang indefinitely, never complete

**Root Cause**: Pipe transmission mode mismatch:
- Server uses `PipeTransmissionMode.Message` (Windows-only, message-boundary-based)
- Client uses default `PipeTransmissionMode.Byte` (streaming)
- Server expects message boundaries, client sends continuous stream
- `ReadLineAsync()` blocks forever waiting for line delimiter that never arrives properly

**Fix**: Change server to use `PipeTransmissionMode.Byte` for consistent line-based protocol

### Issue 1: DefaultFont Asset Not Found
**Symptom**: `[ContentManager]: Error: The asset 'DefaultFont' could not be found`

**Root Cause**: The sample app tries to load `Content.Load<SpriteFont>("DefaultFont")` but:
1. This is a code-only sample with no Stride editor/assets
2. No `.sdpkg` or `.sdtex` asset files exist
3. The font asset doesn't exist - it's just swallowed and UI proceeds without fonts

**Impact**: 
- TextBlocks render with no visible text
- Font is `null`, text size calculations may fail
- UI elements may have incorrect bounds

**Fix**: Removed font dependency in SampleStrideGame.cs (DONE)

### Issue 2: Clicks Go to Wrong Window
**Symptom**: Clicks don't interact with the game window correctly

**Root Cause**: `StrideUIHandler.GetElementBounds()` returns **UI-local coordinates** but `StrideInputSimulator.Click()` expects **screen coordinates**

**Fix**: Added GetWindowInfo query and coordinate transformation (DONE)

### Issue 3: App Stays Open Between Tests
**Symptom**: Game window doesn't close, multiple instances may pile up

**Root Cause**: Named pipe connection may not be properly cleaned up first

**Fix**: Added stale process cleanup in StrideUITestBase (DONE)

### Issue 4: Elements Have No Bounds (Zero Size)
**Symptom**: `Cannot click element 'VolumeSlider' - not found or has no bounds`

**Root Cause**: Layout hasn't computed sizes yet

**Fix**: Added fallback bounds calculation in StrideUIHandler (DONE)

---

## Solution Architecture

### Solution 1: Embedded Font or No-Font UI
**Options**:
- **Option A**: Create UI without fonts (shapes only) - simpler for testing
- **Option B**: Embed a font as a resource and load programmatically
- **Option C**: Use CommunityToolkit's built-in font if available

**Decision**: Start with Option A - remove font dependency for test sample

### Solution 2: Convert UI Coordinates to Screen Coordinates
The bounds returned must account for:
1. Game window position on screen
2. UI resolution vs window resolution  
3. UI viewport offset

**Approach**: Add window info query to automation protocol, transform coordinates on test side

### Solution 3: Proper Process Lifecycle
1. Use process exit event instead of relying on pipe
2. Track window handle for proper cleanup
3. Add process kill timeout

### Solution 4: Ensure UI Layout Runs
1. Force a layout pass after creating UI
2. Wait for at least one frame before querying bounds
3. Attach UI properly to scene graph

---

## Implementation Tasks

### Task 1: Remove Font Dependency (Test Sample)
Remove all font references from sample app UI. Use default styling.

**Files**:
- `samples/Brinell.Samples.Stride.App/SampleStrideGame.cs`

**Changes**:
- Remove `TryLoadFont()` and `_defaultFont` field
- Remove all `Font = _defaultFont` assignments (let Stride use defaults)
- Keep text elements but without explicit fonts

### Task 2: Add Window Position Query
Add automation command to return game window position on screen.

**Files**:
- `src/Brinell.Stride.Automation/AutomationGameSystem.cs` - Add window info handler
- `src/Brinell.Stride.Automation/StrideUIHandler.cs` - Handle GetWindowInfo query

**Protocol**:
```json
{"type":"Query","method":"GetWindowInfo"} 
→ {"windowX": 100, "windowY": 100, "windowWidth": 1280, "windowHeight": 720, "uiResolution": {"x": 1280, "y": 720}}
```

### Task 3: Transform Coordinates in Test Side
Calculate screen coordinates from UI-local bounds + window position.

**Files**:
- `src/Brinell.Stride/Infrastructure/StrideTestContext.cs` - Transform bounds

**Changes**:
- Cache window info on connect
- Transform `CenterX`/`CenterY` to screen space before clicking

### Task 4: Force Layout Update
Ensure UI layout is computed before returning bounds.

**Files**:
- `samples/Brinell.Samples.Stride.App/SampleStrideGame.cs` - Trigger layout
- `src/Brinell.Stride.Automation/StrideUIHandler.cs` - Verify layout

### Task 5: Improve Process Cleanup
Better lifecycle management in test base.

**Files**:
- `samples/Brinell.Samples.Stride.UITests/StrideUITestBase.cs`

**Changes**:
- Wait for window to appear before connecting
- Track window handle
- Ensure clean shutdown

### Task 6: Add Debug Logging
Add verbose logging to trace issues.

**Files**:
- `src/Brinell.Stride.Automation/AutomationGameSystem.cs`

### Task 7: Run and Verify Tests

---

## Test Cases

All 16 existing tests should pass:
- 5 Counter tests
- 5 Greeting tests  
- 6 Settings tests

---

## Dependencies

- NET 10 SDK
- Stride 4.3.0.2507
- Stride.CommunityToolkit.Bepu 1.0.0-preview.62
- Stride.CommunityToolkit.Windows 1.0.0-preview.62

---

## Rollback Plan

If fixes don't work:
1. Revert to Plan 13 state
2. Consider alternative testing approach (inject mock input handler)

---

## Implementation Order

1. **Task 1**: Remove font dependency (simplest, may fix crashes)
2. **Task 4**: Force layout update (may fix zero bounds)
3. **Task 2 + 3**: Coordinate transformation (fixes wrong window clicks)
4. **Task 5**: Process cleanup (fixes app staying open)
5. **Task 6**: Debug logging (for troubleshooting)
6. **Task 7**: Run tests

---

## Success Criteria

- [ ] No DefaultFont errors in console
- [ ] Game window opens and stays stable during test
- [ ] Clicks interact with correct elements
- [ ] Game window closes after each test
- [ ] All 16 tests pass
