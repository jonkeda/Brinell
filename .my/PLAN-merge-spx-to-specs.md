# Plan: Merge .spx Content into .specs

**Created:** February 14, 2026
**Status:** ✅ Complete
**Goal:** Consolidate information from `.spx/` into `.specs/`, making `.specs` the single source of truth for all project documentation and specifications.

---

## 1. Current State

### .specs — Reference documentation
Organized by topic (architecture, requirements, controls, design, active specs). Freeform markdown. Strong on API reference, control documentation, and architecture. Weak on task tracking and newer specs (025, 029).

### .spx — Work-tracking specs
Structured per-work-item (design + requirements + tasks). Good task tracking with completion percentages. Contains steering docs, newer specs, and implementation details not present in `.specs`. Weak on control API reference (defers to code).

### Key Differences

| Aspect | .spx | .specs |
|--------|------|--------|
| Format | 3 files per spec (design/requirements/tasks) | One file per topic |
| Task tracking | Checkbox lists with % complete | Status in prose only |
| Steering docs | Product, structure, tech | None |
| Control API reference | None | Comprehensive (25 interfaces, 14 class files) |
| Newer specs (025, 029) | Present | Missing |

---

## 2. Merge Strategy

**Principle:** `.specs` becomes the canonical location. Merge valuable `.spx` content into the `.specs` structure. After merge, `.spx` can be archived or removed.

---

## 3. Merge Tasks

### Phase 1: Steering Documents → .specs

These have no equivalent in `.specs` and provide important project-level context.

| # | Action | Source (.spx) | Target (.specs) |
|---|--------|---------------|-----------------|
| 1.1 | Create product overview | `00-steering/product.str.spx.md` | `README.md` — Enrich with product definition, target users, success metrics from SPX. The current README is a lightweight index; the SPX product doc adds business context. |
| 1.2 | Merge tech stack & decisions | `00-steering/tech.str.spx.md` | `architecture/ARCH-001-core-architecture.md` — Add technology stack details (C# 13, .NET 8/9/10, dependency list). `architecture/ARCH-002-decisions.md` — Add any decisions from SPX tech doc not already captured (TScope pattern, CSV logging, nullable skip). |
| 1.3 | Create structure conventions doc | `00-steering/structure.str.spx.md` | `architecture/ARCH-003-project-structure.md` (new) — Directory organization, naming conventions, code structure patterns, module boundaries, dependency rules, code size guidelines. None of this exists in `.specs`. |

### Phase 2: Overlapping Specs — Reconcile Duplicates

Specs that exist in both folders with different content splits.

| # | Action | Source (.spx) | Target (.specs) |
|---|--------|---------------|-----------------|
| 2.1 | Merge SPEC-023 TabbedPage | `01-specs/023-tabbedpage-automation-testing/design.spc.spx.md` + `tasks.spc.spx.md` | `active/SPEC-023-tabbedpage-automation.md` — Add: Windows UI Automation tree analysis details from SPX design doc. Add task completion status (all tasks complete, 6 TabbedPage tests pass, 14 container tests pass). Mark spec as **completed**. |
| 2.2 | Merge SPEC-026 UI Test Fixes | `01-specs/026-ui-test-control-interaction-fixes/design.spc.spx.md` + `requirements.spc.spx.md` + `tasks.spc.spx.md` | `active/SPEC-026-ui-test-fixes.md` + `active/DES-026-ui-test-fixes-design.md` — Enrich design doc with SPX specifics (IRangePatternElement for slider, poll-verify for toggle, button-click for stepper). Add task list from SPX tasks file (all pending). Add requirements (90%+ pass rate target, specific fix criteria). |
| 2.3 | Reconcile interface hierarchy | `01-specs/003-interface-hierarchy-consolidation/design.spc.spx.md` + `requirements.spc.spx.md` + `tasks.spc.spx.md` | `controls/001-INTERFACES.md` — Already comprehensive. Add: consolidation rationale from SPX (why 29→25 interfaces). Add completion note (16/18 tasks done). Consider adding new interfaces from SPX design (IExpandable, IFocusable, IProgress, IDate, ITime, ISwipeable, IRefreshable) if not already in 001-INTERFACES. |
| 2.4 | Reconcile base class hierarchy | `01-specs/004-maui-base-control-hierarchy/design.spc.spx.md` + `requirements.spc.spx.md` + `tasks.spc.spx.md` | `controls/classes/FOUNDATION.md` — Add: intermediate base class details (MauiClickableControlBase, MauiToggleControlBase, MauiRangeControlBase). Add task status (13/15 complete). Enrich shared-logic description (Run/RunWithElement/Poll consolidation). |
| 2.5 | Reconcile sample app design | `01-specs/005-maui-sample-app-tabs/design.spc.spx.md` + `requirements.spc.spx.md` + `tasks.spc.spx.md` | `design/MAUI-SAMPLE-APP.md` — Add: 8 tab category structure from SPX. Add demo views (ListView, TableView, Expander, TreeView-like, Popup). Update status (19/21 tasks complete). |
| 2.6 | Recognize SPEC-006 as completed | `01-specs/006-maui-uitests-update/design.spc.spx.md` + `requirements.spc.spx.md` + `tasks.spc.spx.md` | No target needed — SPX shows 6/6 tasks complete. Add a one-line note in `active/` or the README marking this work as done. Archive reference only. |

### Phase 3: SPX-Only Specs → .specs (New Content)

Specs that exist only in `.spx` and need to be brought into `.specs`.

| # | Action | Source (.spx) | Target (.specs) |
|---|--------|---------------|-----------------|
| 3.1 | Add SPEC-025 MAUI Control UI Tests | `01-specs/025-maui-control-uitests/design.spc.spx.md` + `requirements.spc.spx.md` + `tasks.spc.spx.md` | `active/SPEC-025-maui-control-uitests.md` (new) — Comprehensive UI test plan covering 24 controls across 10 categories. Include design (test plan, per-control test lists), requirements (one test class per control, Is/Wait/Assert coverage, no Thread.Sleep), and task status (33 tasks, all pending). |
| 3.2 | Add SPEC-029 FlaUI Windows Fixes | `01-specs/029-flaui-windows-driver-fixes/design.spc.spx.md` + `requirements.spc.spx.md` + `tasks.spc.spx.md` | `active/SPEC-029-flaui-windows-fixes.md` (new) — FlaUI-specific fixes for slider (RangeValue pattern), picker (ComboBox expansion), SearchBar text, Editor clear. Include extension interfaces, capability detection, baseline/target metrics (65.5%→85%+), and task status (15/22 done). |

### Phase 4: Specs-Only Docs — Review & Retain

Documents in `.specs` with no counterpart in `.spx`. No merge needed, just acknowledge they stay.

| # | File | Action |
|---|------|--------|
| 4.1 | `active/SPEC-015-element-lookup-optimization.md` | Keep as-is. Implemented spec. Consider moving to an `archive/` or `completed/` folder. |
| 4.2 | `active/SPEC-017-tabview-migration.md` | Keep as-is. Partially superseded by SPEC-023. Add note that this was superseded. |
| 4.3 | `active/SPEC-017b-container-testing.md` | Keep as-is. Container testing patterns. Still relevant. |
| 4.4 | `active/PLAN-android-testing.md` | Keep as-is. Android testing plan draft. Still relevant. |
| 4.5 | `active/SPEC-scrollintoview-android.md` | Keep as-is. Android ScrollIntoView analysis. Cross-reference from SPEC-026 design. |
| 4.6 | `requirements/REQ-003-changes.md` | Keep as-is. Requirement changes unique to `.specs`. |
| 4.7 | `controls/TESTING-GUIDE.md` | Keep as-is. Test mockability guide. |
| 4.8 | `controls/classes/*.md` (14 files) | Keep as-is. Core API reference with no SPX equivalent. |
| 4.9 | `design/BLAZOR-SAMPLE-APP.md` | Keep as-is. Blazor sample app design. |

### Phase 5: Post-Merge Cleanup

| # | Action | Details |
|---|--------|---------|
| 5.1 | Add completion status section to .specs README | Update `README.md` with a status section listing which specs are completed, in-progress, or pending. Source this from SPX task files. |
| 5.2 | Archive or delete .spx folder | Once all content is merged, the `.spx` folder is redundant. Options: (a) move to `.spx-archive/`, (b) delete, (c) keep but add a README noting `.specs` is canonical. |
| 5.3 | Update .specs/README.md structure | Add references to new files (ARCH-003, SPEC-025, SPEC-029). Update the folder structure description. |
| 5.4 | Cross-reference audit | Verify all internal links between `.specs` documents still work. Add cross-references where SPX content referenced other specs. |

---

## 4. Execution Order & Dependencies

```
Phase 1 (Steering)     ─── no dependencies, can start immediately
Phase 2 (Reconcile)    ─── no dependencies, can run in parallel with Phase 1
Phase 3 (New specs)    ─── no dependencies, can run in parallel
Phase 4 (Review)       ─── no changes needed, just verification
Phase 5 (Cleanup)      ─── depends on Phases 1-3 being complete
```

Recommended order: **1 → 2 → 3 → 4 → 5** (sequential for review clarity, but Phases 1-3 are independent).

---

## 5. Risk & Considerations

| Risk | Mitigation |
|------|------------|
| Information loss during merge | Keep `.spx` as archive until merge is validated |
| Conflicting information between sources | SPX design docs take precedence for implementation details; `.specs` takes precedence for API reference |
| Task status becomes stale | After merge, task tracking lives in `.specs/active/` files with status sections |
| Large diffs make review hard | Do one phase at a time with a review after each |

---

## 6. Definition of Done

- [x] All unique information from `.spx` is in `.specs`
- [x] No contradictions between merged documents
- [x] `.specs/README.md` updated with full structure
- [x] All new files (ARCH-003, SPEC-025, SPEC-029) created
- [x] Cross-references valid
- [ ] `.spx` archived or marked as superseded (user decision)
