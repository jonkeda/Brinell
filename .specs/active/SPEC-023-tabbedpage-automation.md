# TabbedPage Automation Testing

**Status:** Active (blocking issue) | **Priority:** High

## Problem

TabbedPage on Windows: AutomationId is not exposed on `NavigationViewItem` elements in the UI Automation tree. Root cause is timing — the `AutomationPeer` is created before the MAUI renderer sets the AutomationId.

MAUI issue: dotnet/maui#3996

**Impact:** 24+ container tests blocked; tab navigation unreliable.

## Workaround: XPath by Name

```csharp
// Instead of AutomationId:
Locator.ByXPath("//NavigationViewItem[@Name='TabTitle']")
```

## Proposed Fix: Enhanced TabbedPageAutomationMapper

Defer AutomationId mapping until `NavigationView.Loaded` event using `DispatcherQueue`:
1. Subscribe to `NavigationView.Loaded`
2. After load, iterate child `NavigationViewItem` elements
3. Set `AutomationProperties.AutomationId` from tab page metadata

## Implementation Phases

1. **Phase 1 (now):** XPath fallback with `Locator.ByName()` to unblock tests
2. **Phase 2:** Fix mapper timing in sample app
3. **Phase 3:** Add `Locator.ByName()` convenience to framework
