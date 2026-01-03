# ISSUE-001: UI Mode Configuration Causes Test Failures

**Created:** January 3, 2026  
**Status:** Identified  
**Severity:** High  
**Category:** Sample App Configuration  

---

## Summary

The sample app (`SampleStrideGame.cs`) has a flag that controls which UI is created, but tests expect both UI modes to be available simultaneously.

---

## Symptoms

- 36 out of 55 tests failing
- `PageNotDisplayedException: Page 'Game Page' is not displayed`
- `AssertionException: Control '{ControlId}' should exist but does not`

---

## Root Cause

Line 255 in `SampleStrideGame.cs`:

```csharp
var useLegacyUI = true;
```

This flag controls which UI is created:

| `useLegacyUI` | UI Created | Page Object | Tests That Work |
|---------------|------------|-------------|-----------------|
| `true` (current) | Legacy UI with MainPanel, Counter, Greeting, Volume | `MainPage` | Counter, Greeting, Legacy Settings |
| `false` | New HUD with GamePage, SettingsOverlay | `GamePage`, `SettingsPage` | Gameplay, New Settings |

---

## Affected Tests

### When `useLegacyUI = true` (16 tests fail):
- All GameplayTests (12 tests)
- Debug_CheckGameplayElements
- Debug_TakeGameplayScreenshot
- Debug_TakeSettingsScreenshot  
- Debug_CheckSettingsElements

### When `useLegacyUI = false` (would break 17 tests):
- CounterTests (5 tests)
- GreetingTests (4 tests)
- Legacy SettingsTests (8 tests)

---

## Missing Controls by Mode

| Control | Legacy Mode | New Mode |
|---------|-------------|----------|
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

## Solution Options

### Option A: Switch to New UI
Change `useLegacyUI = false` and accept that legacy tests will fail.

### Option B: Merge Both UIs (Recommended)
Modify the sample app to include BOTH:
1. Keep the legacy MainPanel with Counter/Greeting/Volume
2. Add the HUD overlay 
3. Add the Settings overlay (ESC key)

### Option C: Skip Incompatible Tests
Mark the 36 failing tests as Skip until sample app is updated.

---

## Resolution

**Recommended:** Option B - Merge Both UIs to support all tests.

---

## Related Files

- `samples/Brinell.Samples.Stride.App/SampleStrideGame.cs` (line 255)
- `samples/Brinell.Samples.Stride.UITests/Pages/GamePage.cs`
- `samples/Brinell.Samples.Stride.UITests/Pages/SettingsPage.cs`
- `samples/Brinell.Samples.Stride.UITests/Pages/MainPage.cs`
