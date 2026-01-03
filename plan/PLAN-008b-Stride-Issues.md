# PLAN-008b: Stride Test Issues

**Created:** January 3, 2026
**Status:** In Progress

---

## Test Results Summary

- **Total Tests:** 55
- **Passed:** 19
- **Failed:** 36

---

## ROOT CAUSE IDENTIFIED

The sample app (`SampleStrideGame.cs`) has a flag on **line 255**:

```csharp
var useLegacyUI = true;
```

This flag controls which UI is created:

| `useLegacyUI` | UI Created | Page Object | Tests That Work |
|---------------|------------|-------------|-----------------|
| `true` (current) | Legacy UI with MainPanel, Counter, Greeting, Volume | `MainPage` | Counter, Greeting, Legacy Settings |
| `false` | New HUD with GamePage, SettingsOverlay | `GamePage`, `SettingsPage` | Gameplay, New Settings |

**The tests expect BOTH UI modes to be available, but only ONE is active.**

---

## Issue Categories

### Category 1: Page Not Displayed - GamePage (16 tests)

**Pattern:** `PageNotDisplayedException: Page 'Game Page' is not displayed`

**Root Cause:** GamePage looks for element `"HUD"` which only exists when `useLegacyUI = false`. Currently `useLegacyUI = true`, so there's no "HUD" element.

**Affected:**
- All GameplayTests (12 tests)
- Debug_CheckGameplayElements
- Debug_TakeGameplayScreenshot
- Debug_TakeSettingsScreenshot  
- Debug_CheckSettingsElements

**Fix:** Change `useLegacyUI = false` in `SampleStrideGame.cs` line 255, OR update tests to use the current UI structure.

---

### Category 2: Settings Controls Not Found (20 tests)

**Pattern:** `AssertionException: Control '{ControlId}' should exist but does not`

**Root Cause:** SettingsPage expects controls from the new Settings overlay (MasterVolumeSlider, FullscreenToggle, etc.) which only exist when `useLegacyUI = false`.

**Missing Controls (in legacy mode):**
| Control | Exists in Legacy | Exists in New |
|---------|------------------|---------------|
| MasterVolumeSlider | ❌ | ✅ |
| MusicVolumeSlider | ❌ | ✅ |
| SFXVolumeSlider | ❌ | ✅ |
| MuteAudioToggle | ❌ | ✅ |
| FullscreenToggle | ❌ | ✅ |
| VSyncToggle | ❌ | ✅ |
| BrightnessSlider | ❌ | ✅ |
| PlayerNameInput | ❌ | ✅ |
| MoveSpeedSlider | ❌ | ✅ |
| SensitivitySlider | ❌ | ✅ |
| InvertYToggle | ❌ | ✅ |
| ShowFpsToggle | ❌ | ✅ |
| ApplyButton | ❌ | ✅ |
| CloseButton | ❌ | ✅ |
| VolumeSlider | ✅ | ❌ |
| DarkModeToggle | ✅ | ❌ |

---

## Working Tests (19 passing)

These tests use `MainPage` which maps to `"MainPanel"` (exists in legacy UI):

### SimpleAppTest (2 tests)
- App_StartsAndStops_Successfully ✅
- App_GetElementState_ReturnsValidResponse ✅

### CounterTests (5 tests) 
- Counter_InitialValue_IsZero ✅
- IncrementButton_Click_IncreasesCounter ✅
- DecrementButton_Click_DecreasesCounter ✅
- ResetButton_Click_ResetsToZero ✅
- Counter_MultipleIncrements_Accumulates ✅

### GreetingTests (4 tests)
- Uses NameInput, GreetButton, GreetingDisplay (all in legacy UI)

### Legacy SettingsTests (8 tests)
- LegacySettings_DarkModeToggle_* tests
- LegacySettings_VolumeSlider_* tests

---

## Solution Options

### Option A: Switch to New UI (Recommended)

1. Change `useLegacyUI = false` in `SampleStrideGame.cs` line 255
2. Re-run tests
3. Legacy tests will fail, new tests will pass

**Impact:** 
- GameplayTests (12) → should pass
- New SettingsTests (20) → should pass
- Legacy SettingsTests (8) → will fail
- CounterTests (5) → will fail (no counter in new UI)
- GreetingTests (4) → will fail (no greeting in new UI)

### Option B: Merge Both UIs

Modify the sample app to include BOTH:
1. Keep the legacy MainPanel with Counter/Greeting/Volume
2. Add the HUD overlay 
3. Add the Settings overlay (ESC key)

This would support ALL tests.

### Option C: Mark GamePage/SettingsPage Tests as Skip

Keep current state, mark the 36 failing tests as Skip until sample app is updated.

---

## Recommended Action

**Option B: Merge Both UIs** - This is the cleanest solution that:
1. Preserves working Counter/Greeting tests
2. Enables GamePage tests (HUD controls)
3. Enables SettingsPage tests (Settings overlay)
4. Demonstrates the full Brinell.Stride framework capabilities

**Implementation:**
1. Change `useLegacyUI = false`
2. Modify `CreateUI()` to include legacy Counter/Greeting in the HUD
3. Re-run tests to verify

---

## Framework Status

The Brinell.Stride **framework is working correctly**:

| Component | Status | Evidence |
|-----------|--------|----------|
| Named pipe communication | ✅ | App starts and connects |
| Element state queries | ✅ | GetElementState works |
| Click operations | ✅ | Counter buttons work |
| Toggle operations | ✅ | DarkModeToggle works |
| Slider operations | ✅ | VolumeSlider works |
| Text input | ✅ | NameInput works |
| Page lifecycle | ✅ | MainPage.CheckActive() works |
| Is/Wait/Check/Assert | ✅ | All patterns work |

**The 36 failures are SAMPLE APP configuration issues, not framework bugs.**
