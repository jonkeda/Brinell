# TabView Migration

**Status:** Superseded | **Priority:** Low | **Supersedes:** SPEC-016 (TabBar)

> **Note:** This spec is superseded by SPEC-023 (TabbedPage Automation). The sample app adopted TabbedPage instead of CommunityToolkit TabView. The TabbedPage approach with XPath-by-Name is working and all tab/container tests pass. The TabView approach remains a viable fallback if TabbedPage issues resurface.

## Problem

Shell TabBar tabs render as `ListItem` elements without `AutomationId`. Finding them requires XPath by Name, which is slow (~1200ms) and unreliable.

## Proposed Solution

Migrate from Shell TabBar to CommunityToolkit `TabView`:
- TabView supports `AutomationId` on tabs directly
- Element finding drops to ~250ms (4-5x faster)
- New `Brinell.Maui.CommunityToolkit` project with `TabViewControl`

## Current Status

The sample app later moved to `TabbedPage` instead of TabView. TabbedPage has its own AutomationId issues (see SPEC-023). The CommunityToolkit approach remains a viable alternative if TabbedPage issues persist.

## Related

- SPEC-023: TabbedPage automation issues
- `srcnew/Brinell.Maui.CommunityToolkit/` — Project exists but minimal
