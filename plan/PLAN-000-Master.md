# PLAN-000: Master Implementation Plan

**Created:** January 3, 2026
**Status:** In Progress

---

## Execution Order

| #  | Plan      | Platform                | Status                    |
| -- | --------- | ----------------------- | ------------------------- |
| 1  | PLAN-002  | Brinell.Core            | ✅ Complete               |
| 2  | PLAN-003  | Brinell.Maui            | ✅ Complete               |
| 2b | PLAN-003b | MAUI Test Fixes         | ✅ Complete (21/21 tests) |
| 3  | PLAN-004  | Brinell.Wpf             | ✅ Complete (14/14 tests) |
| 4  | PLAN-005  | Brinell.WinForms        | ✅ Complete (71 passing)  |
| 5  | PLAN-006  | Brinell.Html            | ✅ Complete (33 tests)    |
| 6  | PLAN-007  | Brinell.Html.Playwright | ✅ Complete (32 tests)    |
| 7  | PLAN-008  | Brinell.Stride          | Not Started               |

---

## Workflow Per Platform

```
1. Create PLAN-00X
2. Implement changes
3. Build platform
4. Create/update docs/run/{Platform}.md
5. Run tests (see docs/run/{Platform}.md)
6. document errors in PLAN-XXXb-{Platform}-Test-Fixes.md
7. Fix errors
8. Create/update docs/run/{Platform}.md
9. Mark complete
10. Next platform
```

---

## Platform Priority

1. **Core** - Interfaces needed by all platforms
2. **MAUI** - Reference implementation (100% complete)
3. **WPF** - Production, needs BusyPageBase
4. **WinForms** - Needs base class alignment
5. **Html** - Web platform (Selenium)
6. **Html.Playwright** - Web platform (async)
7. **Stride** - Experimental, lowest priority

---

## Key Updates Per Platform

| Platform        | Key Updates                                                        |
| --------------- | ------------------------------------------------------------------ |
| Core            | ✅ IScrollableControl, PlatformExtensions, Exceptions              |
| MAUI            | ✅ Container constructors, ScrollViewControl, Windows UIA fixes    |
| WPF             | ✅ ScrollViewControl, container constructors for all base classes  |
| WinForms        | Rename InputControlBase→TextControlBase, add missing base classes |
| Html            | Add BusyPageBase, ItemsControlBase, container/scroll support       |
| Html.Playwright | Add BusyPageBase, ItemsControlBase, scroll support                 |
| Stride          | Add StridePageBase, BusyPageBase, ItemsControlBase, scroll         |

---

## Current Progress

- [X] PLAN-001: Platform Review Summary
- [X] PLAN-002: Core Update (Complete)
- [X] PLAN-003: MAUI Update (Complete)
- [X] PLAN-003b: MAUI Test Fixes (21/21 passing)
- [X] PLAN-004: WPF Update (14/14 passing)
- [X] PLAN-005: WinForms Update (71 passing)
- [X] PLAN-006: Html Update (33 passing)
- [X] PLAN-007: Playwright Update (32 passing)
- [ ] PLAN-008: Stride Update
