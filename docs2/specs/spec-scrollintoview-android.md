# ScrollIntoView Android Analysis

**Status:** Active analysis

## Problem

`MauiElement.ScrollIntoView()` uses `windows: scroll` command — Windows-only. On Android:
- SliderControlTests: 0/9 pass
- CheckboxControlTests: 1/9 pass
- Any off-screen element interaction fails

## Android Scroll APIs

### 1. `mobile: scrollGesture` (Explicit scroll)

Scroll a specific element or screen area by direction/percentage. Good for explicit "scroll down" operations.

### 2. `UiScrollable` (Scroll-to-find)

Android-specific: scrolls a container until a target element is found. Built into UiAutomator2.

```
// Appium selector for scroll-to-find
new UiScrollable(new UiSelector().scrollable(true))
    .scrollIntoView(new UiSelector().description("ElementId"))
```

## Proposed Solution: Hybrid

1. **Try direct find first** — element might already be visible
2. **UiScrollable scroll-to-find** — scroll containing view until element is found (Android)
3. **`mobile: scrollGesture`** — fallback generic scroll (Android)
4. **`windows: scroll`** — Windows-specific (existing)

Requires `MauiPlatform` enum and platform detection in `IMauiDriver`.
