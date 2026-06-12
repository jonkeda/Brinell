# TabbedPage Automation Testing

**Status:** ✅ Complete | **Priority:** High

## Problem

TabbedPage on Windows: AutomationId is not exposed on `NavigationViewItem` elements in the UI Automation tree. Root cause is timing — the `AutomationPeer` is created before the MAUI renderer sets the AutomationId.

MAUI issue: dotnet/maui#3996

**Impact:** 24+ container tests blocked; tab navigation unreliable.

## Solution: XPath by Name (Implemented)

### Key Discovery

During implementation, Windows UI Automation tree analysis revealed that tabs render as `TabItem` elements, not `NavigationViewItem` as originally expected:

```csharp
// Working locator pattern:
Locator.ByXPath("//TabItem[@Name='TabTitle']")
```

### Implementation

- `MauiTabControl` and `MauiFlyoutItemControl` use Name-based XPath for tab finding
- `AppShellPage` provides typed tab accessors for all 8 sample app tabs
- Container scoping works correctly within tabbed content

## Results

| Test Category | Result |
|---------------|--------|
| TabbedPage navigation tests | 6/6 pass ✅ |
| Container scoping tests | 14/14 pass ✅ |

### Task Completion

All implementation tasks completed:
1. ✅ Windows UI Automation tree analysis
2. ✅ XPath fallback implementation with `TabItem` discovery
3. ✅ MauiTabControl and MauiFlyoutItemControl updates
4. ✅ AppShellPage with tab accessors
5. ✅ TabbedPage tests written and passing
6. ✅ Container scoping tests validated

## Original Proposed Fix (Not Needed)

The original proposal to fix `AutomationPeer` timing via `DispatcherQueue` deferral was superseded by the XPath-by-Name approach, which proved sufficient.

## Related

- SPEC-017: TabView migration (superseded — TabbedPage approach adopted instead)
- `testsnew/Brinell.Maui.UITests/Tests/TabbedPageTests.cs`
