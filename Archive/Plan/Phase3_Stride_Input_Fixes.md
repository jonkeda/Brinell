# Phase 3 Sub-Plan: Stride Input & Control Issues

## Problem Summary
The Stride tests are failing primarily due to input/focus issues. Keystrokes are not reaching the game window (user reports Windows search opening), and controls are not responding to automation commands.

## Root Cause Analysis

### Issue 1: Window Focus Problems ⚠️ CRITICAL
**Symptom**: Keystrokes trigger Windows search instead of going to game
**Root Cause**: Game window loses focus or never gains focus during test execution
**Impact**: All text input tests fail, keyboard commands don't work

**Evidence**:
- `NameInput_EnterText_DisplaysInField` - Expected 'Bob', got empty string
- `NameInput_ClearAndEnter_ReplacesText` - Expected 'SecondName', got empty string
- User observation: Windows search window opening during tests

### Issue 2: Input Simulation Not Working
**Symptom**: Text is not being entered into EditText controls
**Root Cause**: Either focus issue OR input simulation method incorrect for Stride

**Affected Tests**:
- All GreetingTests that involve text input
- Any test using `SetText()`, `Enter()`, `ClearAndEnter()`

### Issue 3: Slider Controls Not Responding
**Symptom**: Slider Increment/Decrement operations don't change values
**Root Cause**: Slider control interaction method may be wrong, or sliders disabled/not interactive

**Affected Tests**:
- `LegacySettings_VolumeSlider_Increment_IncreasesValue` - Value stays at 50
- `LegacySettings_VolumeSlider_Decrement_DecreasesValue` - Value stays at 50

### Issue 4: Missing Controls in Legacy UI
**Symptom**: Controls that exist in new UI don't exist in Legacy UI
**Root Cause**: Legacy UI was created before new settings system, missing modern controls

**Missing Controls**:
- MasterVolumeSlider
- MusicVolumeSlider  
- SensitivitySlider
- BrightnessSlider
- FullscreenToggle
- ApplyButton
- MuteAudioToggle
- MoveSpeedSlider (returns 0 instead of expected value)

### Issue 5: Toggle State Persistence
**Symptom**: Toggle doesn't return to initial state after double-toggle
**Root Cause**: Toggle state not persisting or not being read correctly

**Affected Tests**:
- `LegacySettings_DarkModeToggle_ToggleTwice_ReturnsToInitial`

## Detailed Fix Plan

### Priority 1: Window Focus & Input Focus (CRITICAL)

#### Task 1.1: Ensure Game Window Has Focus Before Input
**Location**: `Brinell.Stride.Automation/StrideUIHandler.cs` or `Brinell.Stride/Infrastructure/StrideTestContext.cs`

**Changes Needed**:
1. Add window focus enforcement before any input operation
2. Use Windows API to bring game window to foreground
3. Verify focus succeeded before sending input
4. Add retry logic if focus fails

**Implementation Approach**:
```csharp
// In StrideTestContext or new helper class
[DllImport("user32.dll")]
private static extern bool SetForegroundWindow(IntPtr hWnd);

[DllImport("user32.dll")]
private static extern IntPtr GetForegroundWindow();

public bool EnsureGameHasFocus(int timeoutMs = 5000)
{
    var stopwatch = Stopwatch.StartNew();
    var gameHandle = _processHandle; // Game process window handle
    
    while (stopwatch.ElapsedMilliseconds < timeoutMs)
    {
        SetForegroundWindow(gameHandle);
        Thread.Sleep(50);
        
        if (GetForegroundWindow() == gameHandle)
            return true;
    }
    
    return false;
}
```

**Files to Modify**:
- `Brinell.Stride/Infrastructure/StrideTestContext.cs` - Add focus management
- `Brinell.Stride/Controls/StrideEditTextControl.cs` - Call EnsureGameHasFocus() before input
- `Brinell.Stride.Automation/StrideGameManager.cs` - Store window handle on process start

#### Task 1.2: Store and Track Game Window Handle
**Changes Needed**:
1. Capture window handle when game process starts
2. Store handle in test context
3. Provide method to check if window is still focused

**Files to Modify**:
- `Brinell.Stride.Automation/StrideGameManager.cs`
- `Brinell.Stride/Infrastructure/StrideTestContext.cs`

### Priority 2: Fix Text Input Simulation

#### Task 2.1: Review Current Input Method
**Investigation Needed**:
1. Check how `StrideEditTextControl.SetText()` works
2. Verify if it's using correct Stride UI API
3. Check if EditText control needs focus before input

**Current Implementation to Review**:
- `Brinell.Stride/Controls/StrideEditTextControl.cs`
- `Brinell.Stride.Automation/StrideUIHandler.cs` - HandleAction for "SetText"

#### Task 2.2: Fix SetText Implementation
**Location**: `Brinell.Stride/Controls/StrideEditTextControl.cs`

**Potential Issues**:
1. Not focusing control before sending text
2. Not using correct Stride EditText API
3. Input going through Windows API instead of Stride's input system

**Investigation Questions**:
- Does Stride have a native text input API?
- Should we use `editText.Text = value` directly?
- Or should we simulate keyboard input through Stride's input manager?

**Recommended Approach**:
```csharp
public override void SetText(string text)
{
    // Ensure game has focus FIRST
    Context.EnsureGameHasFocus();
    
    // Focus this specific control
    Focus(); // Already calls CheckVisible
    
    // Clear existing text
    Clear();
    
    // Use Stride's input system or direct property set
    var response = Context.SendCommand(
        AutomationCommand.Action("SetElementText", _automationId, text));
    
    if (!response.Success)
        throw new Exception($"Failed to set text: {response.Error}");
    
    // Verify text was set
    Thread.Sleep(100); // Allow UI to update
    var actualText = GetText();
    if (actualText != text)
        throw new Exception($"Text not set correctly. Expected: '{text}', Got: '{actualText}'");
}
```

#### Task 2.3: Implement Server-Side Text Input
**Location**: `Brinell.Stride.Automation/StrideUIHandler.cs`

**Changes Needed**:
Add new action handler:
```csharp
case "SetElementText":
    return SetElementText(command.Target!, command.Data?.ToString());

private AutomationResponse SetElementText(string automationId, string? text)
{
    var element = FindElement(automationId);
    if (element == null)
        return AutomationResponse.Fail($"Element '{automationId}' not found");
    
    if (element is EditText editText)
    {
        // Direct property set - runs on game's UI thread
        editText.Text = text ?? string.Empty;
        return AutomationResponse.Ok();
    }
    
    return AutomationResponse.Fail($"Element '{automationId}' is not an EditText");
}
```

### Priority 3: Fix Slider Control Interaction

#### Task 3.1: Review Slider SetValue Implementation
**Location**: `Brinell.Stride/Controls/StrideSliderControl.cs`

**Investigation Needed**:
1. Check if `SetValue()` uses correct Stride Slider API
2. Verify slider is interactive (enabled, not read-only)
3. Check if value change triggers properly

#### Task 3.2: Fix Slider Value Setting
**Current Issue**: `Increment()` and `Decrement()` don't change slider value

**Recommended Fix**:
```csharp
public override void SetValue(double value)
{
    CheckEnabled(); // This currently FAILS - slider shows disabled
    
    // Use server-side action to set value directly
    var response = Context.SendCommand(
        AutomationCommand.Action("SetSliderValue", _automationId, value));
    
    if (!response.Success)
        Context.Logger.ThrowCheckFailed(...);
    
    // Wait for value to update
    Thread.Sleep(50);
    
    // Verify
    var actualValue = GetValue();
    if (Math.Abs(actualValue - value) > 0.1)
        throw new Exception($"Slider value not set. Expected: {value}, Got: {actualValue}");
}
```

#### Task 3.3: Investigate Slider Enabled State Issue
**Problem**: Sliders report `IsEnabled = False` but should be enabled

**Investigation Steps**:
1. Check if Legacy UI sliders are properly initialized as enabled
2. Verify parent containers are enabled
3. Check if slider needs to be in "edit mode" to accept input

**Files to Check**:
- `Brinell/samples/Brinell.Samples.Stride.App/SampleStrideGame.cs` - CreateLegacySettingsSection()
- Look at how VolumeSlider, MoveSpeedSlider are created

### Priority 4: Add Missing Controls to Legacy UI

#### Task 4.1: Audit Missing Controls
**Controls to Add**:
1. MasterVolumeSlider (new settings system)
2. MusicVolumeSlider (new settings system)
3. SensitivitySlider (new settings system)
4. BrightnessSlider (new settings system)
5. FullscreenToggle (new settings system)
6. MuteAudioToggle (new settings system)
7. ApplyButton (new settings system)

**Decision Needed**:
- Should we add all new controls to Legacy UI?
- OR should we create "Settings page" tests that work with new UI?
- OR should we update tests to skip controls that don't exist?

#### Task 4.2: Fix Slider Initial Values
**Problem**: Sliders show value=0 instead of expected initial values

**Investigation**:
Check `SampleStrideGame.cs` - slider initialization:
```csharp
// Expected: Initial value set to 50, but reading as 0
CreateSliderRow("VolumeSlider", "Volume", 0, 100, 50)
```

**Potential Issues**:
1. Slider.Value not set during creation
2. Value property not accessible via automation
3. GetRangeValue() implementation bug

### Priority 5: Fix Toggle State Detection

#### Task 5.1: Review Toggle State Reading
**Location**: `Brinell.Stride.Automation/StrideUIHandler.cs`

**Current Implementation**:
```csharp
private bool? GetToggleState(UIElement element)
{
    if (element is ToggleButton toggle)
    {
        return toggle.State == ToggleState.Checked;
    }
    return null;
}
```

**Investigation**:
1. Verify ToggleButton.State updates immediately after click
2. Check if state persists correctly
3. Verify Click() action properly toggles state

#### Task 5.2: Fix Toggle Click Implementation
**Location**: `Brinell.Stride.Automation/StrideUIHandler.cs`

**Potential Issue**: Click might not wait for toggle animation to complete

**Recommended Fix**:
```csharp
private AutomationResponse ClickElement(string automationId)
{
    var element = FindElement(automationId);
    if (element == null)
        return AutomationResponse.Fail($"Element '{automationId}' not found");
    
    if (element is Button button)
    {
        button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        // For toggle buttons, wait for state change
        if (button is ToggleButton)
            Thread.Sleep(50); // Allow toggle animation
        return AutomationResponse.Ok();
    }
    
    return AutomationResponse.Fail($"Element '{automationId}' is not clickable");
}
```

## Implementation Order

### Phase 3A: Critical Input Fixes (Day 1)
1. ✅ Add window focus management (Task 1.1, 1.2)
2. ✅ Fix text input simulation (Task 2.1, 2.2, 2.3)
3. ✅ Test: `NameInput_EnterText_DisplaysInField` should pass

**Success Criteria**: Text input tests pass, no Windows search opening

### Phase 3B: Control Interaction (Day 2)
1. ✅ Fix slider value setting (Task 3.1, 3.2)
2. ✅ Investigate slider enabled state (Task 3.3)
3. ✅ Fix toggle state persistence (Task 5.1, 5.2)

**Success Criteria**: Slider and toggle tests pass

### Phase 3C: Missing Controls (Day 3)
1. ⏰ Decide on strategy for missing controls (Task 4.1)
2. ⏰ Either add to Legacy UI OR update tests to skip
3. ⏰ Fix slider initial values (Task 4.2)

**Success Criteria**: All settings tests pass or properly skipped

## Testing Strategy

### Unit Testing Approach
1. Test window focus before starting full test suite
2. Add logging to track when focus is lost
3. Take screenshots on test failure for debugging

### Validation Steps
After each phase:
1. Run full Stride test suite
2. Monitor for Windows search window opening
3. Check game window focus during test execution
4. Verify no focus lost errors in logs

## Known Constraints

1. **Stride UI Threading**: All UI operations must run on game's main thread
2. **Window Focus**: Windows 10/11 may prevent programmatic focus stealing
3. **Input Timing**: May need delays between focus and input
4. **Game Performance**: Slow game startup can cause timing issues

## Success Metrics

### Current State
- 25/45 Stride tests passing (56%)
- Critical failure: Text input not working
- Critical failure: Keystrokes going to wrong window

### Target State (After Phase 3 Fixes)
- 35+/45 Stride tests passing (78%+)
- ✅ All text input tests passing
- ✅ No focus lost issues
- ✅ Sliders interactive and working
- ⚠️ Some tests may need skipping for missing controls

### Phase 3 Complete Criteria
- ✅ Window focus managed correctly
- ✅ Text input working in all EditText controls
- ✅ Sliders respond to Increment/Decrement/SetValue
- ✅ Toggles maintain state correctly
- ⏰ Decision made on missing controls strategy

## Risk Assessment

### High Risk
- **Window Focus**: OS-level restrictions may prevent reliable focus management
- **Mitigation**: Use elevated process permissions OR run tests in isolated environment

### Medium Risk
- **Stride Input API**: May not support programmatic text input well
- **Mitigation**: Use direct property setting instead of simulated input

### Low Risk
- **Slider/Toggle state**: Should be straightforward fixes
- **Missing controls**: Can skip tests as temporary solution

## Notes & Observations

1. **User Feedback**: Keystrokes opening Windows search = definite focus issue
2. **Architecture**: Current automation runs out-of-process, requires IPC for all actions
3. **Alternative**: Could inject automation into game process for better control
4. **Legacy UI**: Was created as stopgap, may need complete overhaul for production

## Next Steps After This Plan

Once Stride is stable:
1. Review Html/Playwright platforms for similar issues
2. Run full test suite across all platforms
3. Document platform-specific limitations
4. Create Phase 4 plan for advanced features
