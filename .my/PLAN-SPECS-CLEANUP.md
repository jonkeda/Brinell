# Plan: Specs & Specs2 Folder Cleanup

**Created:** February 14, 2026  
**Status:** EXECUTED  
**Goal:** Consolidate `specs/` and `specs2/` into a single `.specs/` folder with clear structure, eliminating duplication and dead references.

## Results

| Metric | Before | After |
|--------|--------|-------|
| Spec folders | 3 (`specs/` 53 files, `specs2/` 136 files, `SPX/` empty) | 1 (`.specs/` 33 files) |
| srcnew/.spx | 14 files (3 per spec + issue + fix) | 6 files (1 per spec + issue + fix) |
| Total spec files | 189+ | 39 |
| Source code bloat | ~65% of content was C# listings | Removed — specs reference srcnew/ as source of truth |
| Superseded docs | 6 in specs/ | 0 |

### What was done:
1. Created `.specs/` with condensed, code-free specs organized by category
2. Condensed `srcnew/.spx` from 3-file-per-spec (design+requirements+tasks, ~700-1600 lines each) to single `spec.md` per spec (~40-80 lines)
3. Removed 12 old bloated `.spc.spx.md` files from `srcnew/.spx`

### Remaining cleanup (manual):
- Delete `specs/` folder (or rename to `specs.old/`)
- Delete `specs2/` folder (or rename to `specs2.old/`)
- Delete empty `SPX/` folder
- Update root `README.md` to reference `.specs/`

---

## 1. Current State Analysis

### 1.1 The Problem

The project has **two parallel spec folders** that partially overlap:

| Folder | Files | Format | Status |
|--------|-------|--------|--------|
| `specs/` | ~51 files | Plain markdown | Mixed: some Final, some Draft, some Superseded |
| `specs2/` | ~100+ files | SPX V7 (.spx.md) | All Draft, more structured but aspirational |

**Issues:**
- ~30-40% content overlap between the two folders
- `specs/README.md` references ~15 files that don't exist (SPEC-002–005, SPEC-007, DES-002–006, REQ-003)
- `specs/` has 4 superseded documents still present alongside their replacements
- `specs2/` is better organized but not yet tied to the actual `srcnew/` codebase
- Neither folder has a clear "source of truth" ownership model
- `SPX/` folder exists but is empty

### 1.2 Active Codebase

Only `srcnew/` and `testsnew/` are active code. The only fully implemented platform is **MAUI** (with Appium + FlaUI drivers). Blazor/Html/WPF/WinForms/Stride are scaffolded with `Placeholder.cs` files.

The specs that directly drive active code are the **SPEC-006 series** (interfaces, class definitions, hierarchies) — these map to `srcnew/Brinell.Core/Interfaces/` and `srcnew/Brinell.Maui/Controls/`.

---

## 2. Decision: What to Keep

### 2.1 Superseded/Obsolete Documents (DELETE)

These files are explicitly superseded or obsolete:

| File | Reason |
|------|--------|
| `specs/SPEC-016-TabBar-Navigation-Redesign.md` | Superseded by SPEC-017-TabView-Migration |
| `specs/SPEC-017-CONTAINER-TESTING.md` | Superseded by SPEC-017b-CONTAINER-TESTING |
| `specs/SPEC-022-FlyoutItemControl.md` | Implemented & obsolete (navigation moved to TabView) |
| `specs/SPEC-006-003-HIERARCHY-INDEX.md` | Superseded by SPEC-006-003b-INDEX (more comprehensive) |
| `specs/SPEC-006-003-HIERARCHY-MAUI.md` | Superseded by SPEC-006-003b series |
| `specs/SPEC-006-003-HIERARCHY-BLAZOR.md` | Superseded by SPEC-006-003b series |

### 2.2 From specs/ → Keep (Authoritative Implementation Specs)

These are Final or actively used by the implementation:

| File | Category | Rename/Move To |
|------|----------|----------------|
| `SPEC-001-core-architecture.md` | Architecture | `architecture/ARCH-001-core-architecture.md` |
| `DES-001-architectural-decisions.md` | Architecture | `architecture/ARCH-002-decisions.md` |
| `REQ-001-functional-requirements.md` | Requirements | `requirements/REQ-001-functional.md` |
| `REQ-002-non-functional-requirements.md` | Requirements | `requirements/REQ-002-non-functional.md` |
| `REQ-CHANGES-SPEC-006.md` | Requirements | `requirements/REQ-003-changes-from-spec006.md` |
| `SPEC-006-INDEX.md` | Controls | `controls/INDEX.md` |
| `SPEC-006-001-INTERFACES.md` | Controls | `controls/001-INTERFACES.md` |
| `SPEC-006-002-CLASSES-FOUNDATION.md` | Controls | `controls/classes/FOUNDATION.md` |
| `SPEC-006-002-CLASSES-INPUT.md` | Controls | `controls/classes/INPUT.md` |
| `SPEC-006-002-CLASSES-TOGGLE.md` | Controls | `controls/classes/TOGGLE.md` |
| `SPEC-006-002-CLASSES-SELECTION.md` | Controls | `controls/classes/SELECTION.md` |
| `SPEC-006-002-CLASSES-RANGE.md` | Controls | `controls/classes/RANGE.md` |
| `SPEC-006-002-CLASSES-DATETIME.md` | Controls | `controls/classes/DATETIME.md` |
| `SPEC-006-002-CLASSES-COLLECTION.md` | Controls | `controls/classes/COLLECTION.md` |
| `SPEC-006-002-CLASSES-CONTAINER.md` | Controls | `controls/classes/CONTAINER.md` |
| `SPEC-006-002-CLASSES-DISPLAY.md` | Controls | `controls/classes/DISPLAY.md` |
| `SPEC-006-002-CLASSES-MEDIA.md` | Controls | `controls/classes/MEDIA.md` |
| `SPEC-006-002-CLASSES-NAVIGATION.md` | Controls | `controls/classes/NAVIGATION.md` |
| `SPEC-006-002-CLASSES-CONTEXT.md` | Controls | `controls/classes/CONTEXT.md` |
| `SPEC-006-002-CLASSES-EXCEPTIONS.md` | Controls | `controls/classes/EXCEPTIONS.md` |
| `SPEC-006-002-CLASSES-LOCATOR.md` | Controls | `controls/classes/LOCATOR.md` |
| `SPEC-006-002-CLASSES-STRING-LOCATOR.md` | Controls | `controls/classes/STRING-LOCATOR.md` |
| `SPEC-006-003b-INDEX.md` | Controls | `controls/hierarchy/INDEX.md` |
| `SPEC-006-003b-FOUNDATION.md` | Controls | `controls/hierarchy/FOUNDATION.md` |
| `SPEC-006-003b-TOGGLE.md` | Controls | `controls/hierarchy/TOGGLE.md` |
| `SPEC-006-003b-SELECTION.md` | Controls | `controls/hierarchy/SELECTION.md` |
| `SPEC-006-003b-RANGE.md` | Controls | `controls/hierarchy/RANGE.md` |
| `SPEC-006-003b-DATETIME.md` | Controls | `controls/hierarchy/DATETIME.md` |
| `SPEC-006-003b-COLLECTION.md` | Controls | `controls/hierarchy/COLLECTION.md` |
| `SPEC-006-003b-CONTAINER.md` | Controls | `controls/hierarchy/CONTAINER.md` |
| `SPEC-006-003b-DISPLAY.md` | Controls | `controls/hierarchy/DISPLAY.md` |
| `SPEC-006-003b-NAVIGATION.md` | Controls | `controls/hierarchy/NAVIGATION.md` |
| `SPEC-006-003b-MEDIA.md` | Controls | `controls/hierarchy/MEDIA.md` |
| `SPEC-006-003b-PAGE.md` | Controls | `controls/hierarchy/PAGE.md` |
| `SPEC-006-004-TESTING-GUIDE.md` | Controls | `controls/TESTING-GUIDE.md` |
| `DES-001c-MAUI-SAMPLE-APP-DESIGN.md` | Design | `design/MAUI-SAMPLE-APP.md` |
| `DES-002c-BLAZOR-SAMPLE-APP-DESIGN.md` | Design | `design/BLAZOR-SAMPLE-APP.md` |

### 2.3 From specs/ → Keep (Active Work-in-Progress)

These document ongoing work or known issues:

| File | Category | Rename/Move To |
|------|----------|----------------|
| `SPEC-015-Element-Lookup-Optimization.md` | Optimization | `active/SPEC-015-element-lookup-optimization.md` |
| `SPEC-015b-Element-Lookup-Optimization-Phase2.md` | Optimization | `active/SPEC-015b-element-lookup-phase2.md` |
| `SPEC-017-TabView-Migration.md` | Migration | `active/SPEC-017-tabview-migration.md` |
| `SPEC-017b-CONTAINER-TESTING.md` | Testing | `active/SPEC-017b-container-testing.md` |
| `SPEC-023-TabbedPage-Automation-Testing.md` | Blocked | `active/SPEC-023-tabbedpage-automation.md` |
| `SPEC-026-UI-Test-Control-Interaction-Fixes.md` | Fixes | `active/SPEC-026-ui-test-fixes.md` |
| `DES-026-UI-Test-Control-Interaction-Fixes.md` | Design | `active/DES-026-ui-test-fixes-design.md` |
| `PLAN-ANDROID-TESTING.md` | Planning | `active/PLAN-android-testing.md` |
| `SPEC-SCROLLINTOVIEW-ANDROID-ANALYSIS.md` | Analysis | `active/SPEC-scrollintoview-android.md` |

### 2.4 From specs2/ → Keep (Unique Value-Add Content)

These provide content not available in specs/:

| specs2/ File/Folder | Value | Move To |
|---------------------|-------|---------|
| `000_basics/002_Idea.spx.md` | Project vision/rationale | `architecture/VISION.md` |
| `100_requirements/110_goal/` (8 files) | Goal documents | `requirements/goals/` |
| `200_architecture/211_Modules/` | Module organization | `architecture/modules/` |
| `200_architecture/220_External/` | External dependency docs | `architecture/external/` |
| `200_architecture/221_Foundation/` | Cross-cutting concerns | `architecture/foundation/` |
| `200_architecture/231_Patterns/` | Design patterns docs | `architecture/patterns/` |
| `250_specifications/250_200_MauiMinimal/` | MAUI minimal scope | `controls/maui-minimal/` |
| `Plan/PLAN-002-Specification-Levels.md` | Level progression system | `planning/specification-levels.md` |
| `Plan/PLAN-003-Implementation-Roadmap.md` | Implementation roadmap | `planning/implementation-roadmap.md` |

### 2.5 From specs2/ → Skip (Duplicated by specs/ or Low Value)

| specs2/ File/Folder | Reason to Skip |
|---------------------|----------------|
| `000_basics/001_Spx.spx.md` | SPX metadata only, no project value |
| `SpxLlm.cfg.md` | LLM extraction config; not needed with consolidated specs |
| `100_requirements/120_functional/` (27 files) | Granular but covered better by REQ-001 + SPEC-006 combined |
| `100_requirements/130_quality/` through `133_usability/` | Covered by REQ-002 |
| `200_architecture/200_000_Overview.spx.md` | Overlaps SPEC-001 + DES-001 |
| `200_architecture/202_Decisions/` | Overlaps DES-001 which has the same ADRs |
| `200_architecture/203_Layers/` | Overlaps SPEC-001 §3 |
| `250_specifications/250_000_Foundation/` | Overlaps SPEC-006-001 + 003b series |
| `250_specifications/250_100_CoreControls/` | Overlaps SPEC-006-002 class files |
| `Plan/PLAN-001-Architecture-Creation.md` | Process doc, completed |
| `Prompts/` | LLM prompt history, not spec content |

---

## 3. Target Structure: `.specs/`

```
.specs/
├── README.md                              # New master index
│
├── requirements/
│   ├── REQ-001-functional.md              # From specs/
│   ├── REQ-002-non-functional.md          # From specs/
│   ├── REQ-003-changes-from-spec006.md    # From specs/
│   └── goals/                             # From specs2/ 110_goal/ (8 files, stripped of SPX format)
│       ├── GOAL-001-unified-test-api.md
│       ├── GOAL-002-reliable-test-execution.md
│       ├── GOAL-003-fast-test-development.md
│       ├── GOAL-004-easy-onboarding.md
│       ├── GOAL-005-debug-friendly.md
│       ├── GOAL-006-open-source-friendly.md
│       ├── GOAL-007-extensible-framework.md
│       └── GOAL-008-native-performance.md
│
├── architecture/
│   ├── ARCH-001-core-architecture.md      # From specs/ SPEC-001
│   ├── ARCH-002-decisions.md              # From specs/ DES-001
│   ├── VISION.md                          # From specs2/ 002_Idea
│   ├── modules/                           # From specs2/ 211_Modules/
│   ├── external/                          # From specs2/ 220_External/
│   ├── foundation/                        # From specs2/ 221_Foundation/
│   └── patterns/                          # From specs2/ 231_Patterns/
│
├── controls/
│   ├── INDEX.md                           # From specs/ SPEC-006-INDEX
│   ├── 001-INTERFACES.md                  # From specs/ SPEC-006-001
│   ├── TESTING-GUIDE.md                   # From specs/ SPEC-006-004
│   ├── classes/                           # From specs/ SPEC-006-002-CLASSES-*
│   │   ├── FOUNDATION.md
│   │   ├── INPUT.md
│   │   ├── TOGGLE.md
│   │   ├── SELECTION.md
│   │   ├── RANGE.md
│   │   ├── DATETIME.md
│   │   ├── COLLECTION.md
│   │   ├── CONTAINER.md
│   │   ├── DISPLAY.md
│   │   ├── MEDIA.md
│   │   ├── NAVIGATION.md
│   │   ├── CONTEXT.md
│   │   ├── EXCEPTIONS.md
│   │   ├── LOCATOR.md
│   │   └── STRING-LOCATOR.md
│   ├── hierarchy/                         # From specs/ SPEC-006-003b-*
│   │   ├── INDEX.md
│   │   ├── FOUNDATION.md
│   │   ├── TOGGLE.md
│   │   ├── SELECTION.md
│   │   ├── RANGE.md
│   │   ├── DATETIME.md
│   │   ├── COLLECTION.md
│   │   ├── CONTAINER.md
│   │   ├── DISPLAY.md
│   │   ├── NAVIGATION.md
│   │   ├── MEDIA.md
│   │   └── PAGE.md
│   └── maui-minimal/                     # From specs2/ 250_200_MauiMinimal/
│
├── design/
│   ├── MAUI-SAMPLE-APP.md                 # From specs/ DES-001c
│   └── BLAZOR-SAMPLE-APP.md              # From specs/ DES-002c
│
├── active/                                # Work-in-progress specs
│   ├── SPEC-015-element-lookup-optimization.md
│   ├── SPEC-015b-element-lookup-phase2.md
│   ├── SPEC-017-tabview-migration.md
│   ├── SPEC-017b-container-testing.md
│   ├── SPEC-023-tabbedpage-automation.md
│   ├── SPEC-026-ui-test-fixes.md
│   ├── DES-026-ui-test-fixes-design.md
│   ├── PLAN-android-testing.md
│   └── SPEC-scrollintoview-android.md
│
└── planning/
    ├── specification-levels.md            # From specs2/ Plan/PLAN-002
    └── implementation-roadmap.md          # From specs2/ Plan/PLAN-003
```

**Total: ~65 files** (down from ~150+ across both folders)

---

## 4. Execution Steps

### Phase 1: Create `.specs/` Structure

1. Create all subdirectories in `.specs/`
2. Copy files from `specs/` → `.specs/` with new names (content unchanged)
3. Copy unique files from `specs2/` → `.specs/` (strip SPX V7 format to plain markdown)
4. Create new `README.md` index with table of contents

### Phase 2: Validate & Cross-Reference

5. Update internal cross-references in moved documents
6. Verify no broken links between specs
7. Ensure SPEC-006 interface names match `srcnew/Brinell.Core/Interfaces/` actual code

### Phase 3: Clean Up Old Folders

8. Delete `specs/` folder (or rename to `specs.old/` for safety)
9. Delete `specs2/` folder (or rename to `specs2.old/`)  
10. Delete empty `SPX/` folder
11. Update root `README.md` to point to `.specs/` instead of `specs/`

### Phase 4: Update Copilot Instructions

12. Update `.github/copilot-instructions.md` references from `specs/` to `.specs/`

---

## 5. Risk Mitigation

| Risk | Mitigation |
|------|-----------|
| Losing content during migration | Keep `specs.old/` and `specs2.old/` until verified |
| Breaking cross-references | Phase 2 explicitly validates links |
| Team confusion | Announce change; README.md update provides clear pointer |
| Git history loss | Use `git mv` where possible to preserve history |

---

## 6. What to Do with `src/` and `tests/` (Bonus)

While not part of this plan, these legacy folders should also be addressed:

| Folder | Status | Recommendation |
|--------|--------|---------------|
| `src/` | Deprecated — not in any active .sln | Archive to `src.old/` or delete after confirming `srcnew/` has full coverage |
| `tests/` | Deprecated — ControlObject6 era | Archive to `tests.old/` or delete |
| `SPX/` | Empty | Delete |

---

## 7. Summary

| Metric | Before | After |
|--------|--------|-------|
| Spec folders | 3 (`specs/`, `specs2/`, `SPX/`) | 1 (`.specs/`) |
| Total files | ~150+ | ~65 |
| Superseded docs | 6 | 0 |
| Broken references | 15+ | 0 |
| Structure clarity | Low (two conflicting systems) | High (single organized tree) |
