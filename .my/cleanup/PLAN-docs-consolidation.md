# Documentation Cleanup Plan

## Problem

Brinell has documentation scattered across **7+ locations**, with significant overlap and stale content. This creates confusion about what's authoritative and makes it hard to find anything.

## Current State — Inventory

### 1. `docs/` — User-facing documentation (13 files)
| Content | Status |
|---------|--------|
| Quick start, framework overview, best practices | Useful — keep |
| Test writing guide, interface usage guide | Useful — keep |
| Platform guides (maui, playwright, stride, winforms) | Useful — keep |
| Run guides (Html, MAUI, WPF, WinForms, Playwright, Android) | Useful — keep |
| Phase 1 Task 5 completion summary, migration guide | Stale — historical |
| Plans (MAUI-UITests investigation/fix) | Stale — belongs in .my |

### 2. `.specs/` — Formal specifications (28 files)
| Content | Status |
|---------|--------|
| `architecture/` — ARCH-001 core, ARCH-002 decisions, ARCH-003 structure | Useful — canonical architecture |
| `controls/` — 001-INTERFACES, classes/, INDEX, TESTING-GUIDE | Useful — control specs |
| `requirements/` — REQ-001 functional, REQ-002 non-functional, REQ-003 changes | Useful — keep |
| `design/` — BLAZOR-SAMPLE-APP, MAUI-SAMPLE-APP | Useful — keep |
| `active/` — SPEC-015..029, DES-026, PLAN-android | Mixed — some done, some active |

### 3. `.spx/` — SPX project management system (8+ spec folders + steering)
| Content | Status |
|---------|--------|
| `00-steering/` — product, structure, tech strategy | Meta/process — review |
| `01-specs/` — 003, 004, 005, 006, 023, 025, 026, 029 | **Duplicates** `.specs/active/` specs |
| `02-features/`, `03-implementation-logs/`, `04-archive/` | Empty (`.gitkeep` only) |
| `.setup/`, `config.json` | SPX tooling config |

### 4. `.github/` — Copilot/GSD agent system (80+ files)
| Content | Status |
|---------|--------|
| `copilot-instructions.md` | Active — Copilot context |
| `instructions/` (11 files) | Active — agent instructions |
| `agents/` (20 files) | Active — GSD agent definitions |
| `skills/` (16 folders) | Active — GSD skill definitions |
| `prompts/` (47 files) | Active — GSD prompt templates |
| `workflows/` (4 yml) | Active — CI/CD |
| `PIPELINE.md` | Active |

### 5. `.archive/` — Old specs (50+ files)
| Content | Status |
|---------|--------|
| `specs.old/` — Full old spec set (SPEC-001..026, DES, REQ, PLAN, etc.) | **Dead** — superseded by `.specs/` |
| `specs2.old/` — Another old generation (basics, requirements, architecture, specifications) | **Dead** — superseded by `.specs/` |

### 6. `.planning/codebase/` — Codebase documentation (7 files)
| Content | Status |
|---------|--------|
| ARCHITECTURE, CONVENTIONS, CONCERNS, INTEGRATIONS, STACK, STRUCTURE, TESTING | **Overlaps** with `.specs/architecture/` and `docs/` |

### 7. `.my/` — Personal plans and issues (11 files)
| Content | Status |
|---------|--------|
| PLAN-* and ISSUE-* files | Working notes — keep as-is (not public docs) |

### 8. Root-level markdown (6 files)
| Content | Status |
|---------|--------|
| README.md, CHANGELOG.md, CONTRIBUTING.md, LICENSE | Standard — keep |
| BREAKING-CHANGES-POLICY.md, VERSIONING.md | Standard — keep |
| review.md | Stale? — review |

### 9. `srcnew/explanation/` (1 file)
| Content | Status |
|---------|--------|
| DEVIATIONS-Phase1.md | Historical — move or delete |

---

## Recommendation: Merge everything into `docs/`

**`docs/`** is the single canonical documentation location. Everything else gets merged in or deleted.

### Target Structure

```
docs/
├── README.md                          (index / table of contents)
├── getting-started/
│   ├── quick-start.md                 ← from docs/01-quick-start.md
│   └── framework-overview.md          ← from docs/02-framework-overview.md
├── architecture/
│   ├── core-architecture.md           ← from .specs/architecture/ARCH-001
│   ├── decisions.md                   ← from .specs/architecture/ARCH-002
│   └── project-structure.md           ← from .specs/architecture/ARCH-003
├── controls/
│   ├── interfaces.md                  ← from .specs/controls/001-INTERFACES
│   ├── classes/                       ← from .specs/controls/classes/
│   ├── index.md                       ← from .specs/controls/INDEX
│   └── testing-guide.md              ← from .specs/controls/TESTING-GUIDE
├── platform-guides/
│   ├── maui.md                        ← keep
│   ├── playwright.md                  ← keep
│   ├── stride.md                      ← keep
│   └── winforms.md                    ← keep
├── run/
│   ├── (all existing run guides)      ← keep
├── requirements/
│   ├── functional.md                  ← from .specs/requirements/REQ-001
│   ├── non-functional.md              ← from .specs/requirements/REQ-002
│   └── changes.md                     ← from .specs/requirements/REQ-003
├── design/
│   ├── blazor-sample-app.md           ← from .specs/design/
│   └── maui-sample-app.md             ← from .specs/design/
├── guides/
│   ├── best-practices.md              ← from docs/12-best-practices.md
│   ├── test-writing-guide.md          ← from docs/15-test-writing-guide.md
│   ├── interface-usage-guide.md       ← from docs/16-interface-usage-guide.md
│   ├── troubleshooting.md             ← from docs/13-troubleshooting.md
│   └── migration-guide.md             ← from docs/18-test-writer-migration-guide.md
└── specs/
    ├── (active specs from .specs/active/)
    └── (only specs still relevant)
```

---

## What to DELETE

| Path | Reason |
|------|--------|
| `.archive/specs.old/` | Dead — fully superseded by `.specs/` |
| `.archive/specs2.old/` | Dead — fully superseded by `.specs/` |
| `.archive/` (entire folder if empty after above) | No remaining content |
| `.spx/02-features/` | Empty |
| `.spx/03-implementation-logs/` | Empty |
| `.spx/04-archive/` | Empty |
| `.planning/codebase/` | Redundant with `.specs/architecture/` → merged into `docs/architecture/` |
| `.planning/` (entire folder if empty after above) | No remaining content |
| `docs/19-phase-1-task-5-completion-summary.md` | Historical — no longer relevant |
| `docs/README-PHASE1-TASK5.md` | Historical — no longer relevant |
| `docs/10-plans/` | Stale investigation notes — move to `.my/` or delete |
| `srcnew/explanation/DEVIATIONS-Phase1.md` | Historical — no longer relevant |
| `review.md` (root) | Review if stale, likely delete |

## What to MERGE then DELETE source

| Source | → Target | Then Delete Source |
|--------|----------|-------------------|
| `.specs/architecture/*` | → `docs/architecture/` | `.specs/architecture/` |
| `.specs/controls/*` | → `docs/controls/` | `.specs/controls/` |
| `.specs/requirements/*` | → `docs/requirements/` | `.specs/requirements/` |
| `.specs/design/*` | → `docs/design/` | `.specs/design/` |
| `.specs/active/*` (still relevant) | → `docs/specs/` | `.specs/active/` |
| `.specs/README.md` | → drop (index not needed) | `.specs/README.md` |
| `.specs/` (entire folder after merge) | — | Delete `.specs/` |
| `.planning/codebase/*` | → `docs/architecture/` (merge content) | `.planning/` |

## What to KEEP as-is

| Path | Reason |
|------|--------|
| `.github/` | Copilot agents/instructions/skills/workflows — separate concern (tooling, not docs) |
| `.spx/00-steering/` | Project strategy — review if still used, but not documentation |
| `.spx/01-specs/` | If SPX tooling actively uses this, keep; otherwise merge into `docs/specs/` and delete |
| `.my/` | Personal working notes — not public docs |
| Root markdown (README, CHANGELOG, etc.) | Standard project files |

---

## Execution Order

1. **Create target folders** in `docs/` (architecture, controls, requirements, design, guides, specs)
2. **Copy files** from `.specs/` → `docs/` (rename to clean names, drop number prefixes)
3. **Merge** `.planning/codebase/` content into `docs/architecture/` (deduplicate)
4. **Move** stale `docs/` files to `.my/` or delete
5. **Update** `docs/README.md` as table of contents for new structure
6. **Delete** `.specs/`, `.archive/`, `.planning/`
7. **Review** `.spx/` — if SPX tooling is no longer used, delete; if used, keep `00-steering/` + `01-specs/` only
8. **Verify** no broken cross-references in remaining docs
