# TabView Migration

**Status:** Draft | **Priority:** High | **Supersedes:** SPEC-016 (TabBar)

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
