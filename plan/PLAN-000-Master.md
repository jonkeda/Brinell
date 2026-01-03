# PLAN-000: Master Implementation Plan

**Created:** January 3, 2026  
**Status:** In Progress

---

## Execution Order

| # | Plan | Platform | Status |
|---|------|----------|--------|
| 1 | PLAN-002 | Brinell.Core | ✅ Complete |
| 2 | PLAN-003 | Brinell.Maui | ✅ Complete |
| 2b | PLAN-003b | MAUI Test Fixes | ✅ Complete (21/21 tests) |
| 3 | PLAN-004 | Brinell.Wpf | ⏳ Next |
| 4 | PLAN-005 | Brinell.WinForms | Not Started |
| 5 | PLAN-006 | Brinell.Html | Not Started |
| 6 | PLAN-007 | Brinell.Html.Playwright | Not Started |
| 7 | PLAN-008 | Brinell.Stride | Not Started |

---

## Workflow Per Platform

```
1. Create PLAN-00X
2. Implement changes
3. Build platform
4. Run tests (see docs/run/{Platform}.md)
5. Fix errors
6. Create/update docs/run/{Platform}.md
7. Mark complete
8. Next platform
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

| Platform | Key Updates |
|----------|-------------|
| Core | ✅ IScrollableControl, PlatformExtensions, Exceptions |
| MAUI | ✅ Container constructors, ScrollViewControl, Windows UIA fixes |
| WPF | Add BusyPageBase, scroll support, container verification |
| WinForms | Rename InputControlBase→TextControlBase, add missing base classes |
| Html | Add BusyPageBase, ItemsControlBase, container/scroll support |
| Html.Playwright | Add BusyPageBase, ItemsControlBase, scroll support |
| Stride | Add StridePageBase, BusyPageBase, ItemsControlBase, scroll |

---

## Current Progress

- [x] PLAN-001: Platform Review Summary
- [x] PLAN-002: Core Update (Complete)
- [x] PLAN-003: MAUI Update (Complete)
- [x] PLAN-003b: MAUI Test Fixes (21/21 passing)
- [ ] PLAN-004: WPF Update
- [ ] PLAN-005: WinForms Update
- [ ] PLAN-006: Html Update
- [ ] PLAN-007: Playwright Update
- [ ] PLAN-008: Stride Update
