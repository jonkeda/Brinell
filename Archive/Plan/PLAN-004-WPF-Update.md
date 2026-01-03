# PLAN-004: WPF Update

**Created:** January 3, 2026  
**Completed:** January 3, 2026  
**Status:** ✅ Complete  
**Depends On:** PLAN-002 (Core Update)

## Summary

Updated Brinell.Wpf to match spec requirements and align with MAUI reference implementation.

**Result:** 14/14 tests passing ✅

## Changes Made

### 1. Added ScrollViewControl (FR-002.7)

Created `ScrollViewControl.cs` implementing `IScrollableControl` for WPF ScrollViewer controls.

**Methods Implemented:**
- `ScrollToElement(string automationId)` - Scroll until element visible
- `ScrollToTop()` - Set vertical scroll to 0%
- `ScrollToBottom()` - Set vertical scroll to 100%
- `ScrollUp(int distance)` - Scroll up by percentage
- `ScrollDown(int distance)` - Scroll down by percentage
- `ScrollLeft(int distance)` - Scroll left by percentage
- `ScrollRight(int distance)` - Scroll right by percentage

**Additional Properties:**
- `GetVerticalScrollPercent()`
- `GetHorizontalScrollPercent()`
- `IsVerticallyScrollable()`
- `IsHorizontallyScrollable()`

### 2. Added Container Constructors (FR-002.6)

Added container constructor to base classes that were missing it:
- `RangeControlBase`
- `SelectorControlBase`
- `ItemsControlBase`

### 3. Created docs/run/WPF.md

Comprehensive documentation for running WPF tests including:
- Prerequisites and setup
- Build and run instructions
- Architecture overview
- Troubleshooting guide

## Files Modified

| File | Action |
|------|--------|
| `src/Brinell.Wpf/Controls/ScrollViewControl.cs` | Created |
| `src/Brinell.Wpf/Controls/Base/RangeControlBase.cs` | Added container constructor |
| `src/Brinell.Wpf/Controls/Base/SelectorControlBase.cs` | Added container constructor |
| `src/Brinell.Wpf/Controls/Base/ItemsControlBase.cs` | Added container constructor |
| `docs/run/WPF.md` | Created |

## Existing Features Confirmed

| Component | Status |
|-----------|--------|
| BusyPageBase | ✅ Already in PageBase.cs |
| ControlBase container support | ✅ Already present |
| ContentControlBase container | ✅ Already present |
| TextControlBase container | ✅ Already present |
| ToggleControlBase container | ✅ Already present |

## Verification

```
Test summary: total: 14, failed: 0, succeeded: 14, skipped: 0, duration: 21.8s
```
