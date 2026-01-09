# IDEAS-004: Architecture vs Specification Value Analysis

**Created:** January 9, 2026  
**Status:** Open  
**Priority:** High  
**Related:** [IDEAS-002](IDEAS-002-LLM-Documentation-Strategy.md), [IDEAS-003](IDEAS-003-SPX-LLM-Integration-QA.md)

---

## 1. The Question

> What value does **Architecture documentation** (200_*) provide versus **Specification documentation** (250_*)?

This analysis focuses specifically on these two SPX layers:
- **200_architecture/** — Decisions, layers, modules, patterns
- **250_specifications/** — Interface specs, behavior definitions

The implementation is intentionally incomplete — we're evaluating the documentation itself.

---

## 2. SPX Editions Context

SPX v7 has four editions with increasing formality:

| Edition | Icon | Use Case | Formality |
|---------|------|----------|-----------|
| **Lite** | 🟢Ⅰ | Quick specs, small projects | Minimal |
| **Core** | 🟡Ⅱ | Team projects, versioning | Moderate |
| **Connected** | 🟠Ⅲ | Multi-platform, integrations | Substantial |
| **Enterprise** | 🟣Ⅳ | Governance, compliance | Full |

**Current Brinell usage:** Mostly 🟡Ⅱ Core edition.

**Question:** Is Core edition appropriate, or would Lite suffice for some docs?

---

## 3. What Each Layer Contains

### 200_architecture/ (25 files)

```
200_architecture/
├── 200_000_Overview.spx.md        # Layer model, principles
├── 202_Decisions/                  # 5 ADRs
│   ├── 202_001_CleanArchitecture
│   ├── 202_002_InterfaceFirst
│   ├── 202_003_PlatformSeparation
│   ├── 202_004_ControlHierarchy
│   └── 202_005_AsyncSupport
├── 203_Layers/                     # 3 layer specs
│   ├── 203_001_CoreLayer
│   ├── 203_002_PlatformLayer
│   └── 203_003_TechnologyLayer
├── 211_Modules/                    # 4 module specs
│   ├── 211_001_Interfaces
│   ├── 211_002_BaseClasses
│   ├── 211_003_Controls
│   └── 211_004_PageContext
├── 220_External/                   # 4 external deps
│   ├── 220_001_Appium
│   ├── 220_002_Selenium
│   ├── 220_003_Playwright
│   └── 220_004_FlaUI
├── 221_Foundation/                 # 4 cross-cutting
│   ├── 221_001_Logging
│   ├── 221_002_Configuration
│   ├── 221_003_ExceptionHandling
│   └── 221_004_Timeout
└── 231_Patterns/                   # 7+ patterns
    ├── 231_001_ControlObjectPattern
    ├── 231_002_PageObjectPattern
    ├── 231_003_AdapterPattern
    ├── 231_004_ContainerPattern
    └── ... more
```

### 250_specifications/ (15 files)

```
250_specifications/
├── 250_INDEX.md
├── 250_000_Foundation/             # 9 foundation specs
│   ├── 250_001_IControlObject
│   ├── 250_002_IPageObject
│   ├── 250_003_IContainerControlObject
│   ├── 250_004_TestContext
│   ├── 250_005_InterfaceHierarchy
│   ├── 250_006_MauiBaseClasses
│   ├── 250_007_BlazorBaseClasses
│   ├── 250_008_WpfBaseClasses
│   └── 250_009_PlatformContexts
└── 250_100_CoreControls/           # 5 control specs
    ├── 250_101_Button
    ├── 250_102_Label
    ├── 250_103_Entry
    ├── 250_104_CheckBox
    └── 250_105_Container
```

---

## 4. Value Analysis: Architecture (200_*)

### What Architecture Provides

| Document Type | Example | Value Delivered |
|---------------|---------|-----------------|
| **Decisions (ADRs)** | 202_002_InterfaceFirst | HIGH — Explains WHY interfaces, alternatives considered |
| **Layer Model** | 200_000_Overview | HIGH — Visual diagram, clear boundaries |
| **Patterns** | 231_001_ControlObject | HIGH — Reusable design with code examples |
| **Modules** | 211_001_Interfaces | MEDIUM — Namespace organization, overlaps with specs |
| **Foundation** | 221_001_Logging | MEDIUM — Cross-cutting concerns |
| **External** | 220_001_Appium | LOW — Could be README in platform package |

### Architecture Value Summary

| Category | Files | Value | Notes |
|----------|-------|-------|-------|
| Decisions (202_*) | 5 | **HIGH** | Core design rationale |
| Overview (200_000) | 1 | **HIGH** | Essential layer model |
| Patterns (231_*) | 7 | **HIGH** | Reusable blueprints |
| Layers (203_*) | 3 | **MEDIUM** | Somewhat redundant with overview |
| Modules (211_*) | 4 | **MEDIUM** | Overlaps with 250_* specs |
| Foundation (221_*) | 4 | **MEDIUM** | Useful but could be simpler |
| External (220_*) | 4 | **LOW** | Better as code comments |

**Architecture verdict:** ~13 files provide high/medium value. ~12 files could be consolidated or simplified.

---

## 5. Value Analysis: Specifications (250_*)

### What Specifications Provide

| Document Type | Example | Value Delivered |
|---------------|---------|-----------------|
| **Interface Specs** | 250_001_IControlObject | HIGH — Complete interface definition, rules, boundaries |
| **Behavior Sections** | behavior block | HIGH — Method semantics, return values |
| **Boundary Sections** | boundary block | HIGH — Edge cases, error handling |
| **Acceptance Criteria** | acceptance block | MEDIUM — Gherkin scenarios (don't run) |
| **Assumptions** | assumption block | LOW — Context, rarely referenced |
| **Exclusions** | exclusion block | LOW — What's NOT included |
| **Platform Notes** | Implementation Notes | MEDIUM — Platform-specific guidance |

### Specification Value Summary

| Section | Value | Notes |
|---------|-------|-------|
| Interface Definition | **HIGH** | The actual contract |
| Behavior | **HIGH** | Method semantics |
| Boundary | **HIGH** | Edge cases |
| Method Signatures | **HIGH** | Quick reference table |
| Acceptance | **MEDIUM** | Good ideas, but not executable |
| Platform Notes | **MEDIUM** | Useful for implementation |
| Overview/Prose | **LOW** | Often restates interface |
| Assumptions | **LOW** | Boilerplate |
| Exclusions | **LOW** | Rarely referenced |

**Specification verdict:** ~60% of each spec file is high value (interface, behavior, boundary). ~40% is padding.

---

## 6. Overlap Analysis

### Where Architecture and Specifications Duplicate

| Topic | In Architecture | In Specifications | Redundancy |
|-------|-----------------|-------------------|------------|
| Interface hierarchy | 202_004_ControlHierarchy | 250_005_InterfaceHierarchy | **HIGH** |
| IControlObject methods | 211_001_Interfaces | 250_001_IControlObject | **HIGH** |
| Pattern examples | 231_001_ControlObject | 250_001 behavior section | **MEDIUM** |
| Base class structure | 211_002_BaseClasses | 250_006/007/008 BaseClasses | **HIGH** |
| Logging patterns | 221_001_Logging | Referenced in 250_* | **LOW** |

### Redundancy Estimate

- **25-30%** of spec content repeats architecture content
- **Interface definitions** appear in both 211_* and 250_*
- **Pattern code** appears in both 231_* and 250_*
- **Hierarchy diagrams** duplicated between 202_004 and 250_005

---

## 7. What Each Layer SHOULD Provide

### Architecture (200_*) Should Provide

| Purpose | Content | Format |
|---------|---------|--------|
| **WHY decisions** | ADRs explaining rationale | Decision records |
| **HOW it fits together** | Layer diagrams, dependencies | Visual + brief text |
| **PATTERNS to follow** | Reusable designs | Pattern template with code |
| **BOUNDARIES** | What goes where | Clear rules |

**Architecture should NOT contain:** Interface definitions, method signatures, detailed behavior.

### Specifications (250_*) Should Provide

| Purpose | Content | Format |
|---------|---------|--------|
| **WHAT the interface is** | Complete interface code | Code block |
| **HOW methods behave** | Return values, side effects | Rules list |
| **WHEN things go wrong** | Edge cases, errors | Boundary table |
| **HOW to verify** | Acceptance criteria | Test scenarios |

**Specifications should NOT contain:** Architecture rationale, pattern explanations, layer discussions.

---

## 8. The Real Question: What's Missing vs. What's Redundant?

### What's Missing

| Gap | Impact | Location It Should Be |
|-----|--------|----------------------|
| **Executable tests** | Gherkin doesn't run | tests/ folder |
| **Working examples** | Prose, not code | samples/ folder |
| **Quick reference** | Have to read full docs | QUICK-REFERENCE.md |
| **LLM-optimized view** | Verbose for LLMs | .slm.md files or LLM Summary sections |

### What's Redundant

| Redundancy | Files Affected | Resolution |
|------------|----------------|------------|
| Interface in both 211_* and 250_* | 211_001 + 250_001 | **Keep in 250_* only**, reference from 211_* |
| Pattern code in 231_* and 250_* | 231_001 + 250_001 | **Keep in 231_* only**, reference from 250_* |
| Hierarchy in 202_004 and 250_005 | 2 files | **Keep in 250_005**, simplify 202_004 |
| Base classes in 211_002 and 250_006/7/8 | 4 files | **Keep in 250_***, remove from 211_* |

---

## 9. Improvement Proposals

### Proposal A: Clear Separation of Concerns

**Principle:** Architecture = WHY + HOW (structure). Specification = WHAT + WHEN (behavior).

**Actions:**
1. **Remove interface code from 211_*** — Point to 250_* specs
2. **Remove behavior details from 231_*** — Keep pattern structure only
3. **Remove architecture rationale from 250_*** — Keep behavior only
4. **Add cross-references** — Link between layers

**Result:**
- Architecture: Decisions, diagrams, patterns (structure)
- Specifications: Interfaces, behavior, boundaries (contracts)
- No duplication

### Proposal B: Consolidate to Fewer Files

**Actions:**
1. **Merge 202_* decisions** into single `ARCHITECTURE-DECISIONS.md`
2. **Merge 211_* modules** into single `MODULES.md`
3. **Merge 220_* externals** into `EXTERNAL-DEPENDENCIES.md`
4. **Keep 231_* patterns** separate (high value)
5. **Keep 250_* specs** separate (high value)

**Result:**
- 200_architecture/: ~8-10 files instead of 25
- 250_specifications/: Unchanged (already well-structured)

### Proposal C: Use SPX Lite Edition Where Appropriate

**Current:** Most docs use 🟡Ⅱ Core with full metadata.

**Proposed:** Switch to 🟢Ⅰ Lite for:
- Architecture decisions (simple ADR format)
- External dependency docs (brief notes)
- Module descriptions (brief, not comprehensive)

**Keep 🟡Ⅱ Core for:**
- Interface specifications (need versioning, status)
- Control specifications (formal contracts)
- Pattern docs (detailed guidance)

**Benefit:** Less boilerplate, faster to read and write.

### Proposal D: LLM-Aware Sections

**Actions:**
1. Add `## LLM Summary` section to each high-value doc
2. Keep detailed sections for human readers
3. Update copilot-instructions.md to read summaries first

**Result:**
- Same docs serve both audiences
- No separate .slm.md files needed
- Single source of truth

### Proposal E: Reference-Based Architecture

**Actions:**
1. Architecture docs **reference** specs, don't duplicate
2. Specs are the **authoritative source** for interfaces
3. Architecture provides **context** for why specs exist

**Example in 211_001_Interfaces.spx.md:**
```markdown
## Interface Catalog

For complete interface definitions, see:
- [IControlObject](../250_specifications/250_001_IControlObject.spx.md)
- [IPageObject](../250_specifications/250_002_IPageObject.spx.md)
- [IContainerControl](../250_specifications/250_003_IContainerControlObject.spx.md)

This module defines the namespace organization and design principles.
[Principles here, not interface code]
```

---

## 10. Recommended Improvements

### Immediate Actions

| Action | Files Affected | Effort |
|--------|----------------|--------|
| Remove interface code from 211_001 | 1 file | Low |
| Add cross-references between 200/250 | All major docs | Low |
| Add `## LLM Summary` to 250_001 as pilot | 1 file | Low |

### Short-Term Actions

| Action | Files Affected | Effort |
|--------|----------------|--------|
| Consolidate 220_* into single file | 4 → 1 file | Low |
| Remove duplicated hierarchy from 202_004 | 1 file | Low |
| Downgrade low-value docs to 🟢Ⅰ Lite | ~8 files | Medium |

### Validation Actions

| Action | Purpose | Effort |
|--------|---------|--------|
| Compare 211_001 vs 250_001 | Identify exact overlap | Low |
| Test LLM with architecture only | See if it can understand structure | Medium |
| Test LLM with specs only | See if it can implement | Medium |

---

## 11. Conclusions

### Architecture (200_*) Value

**High value:**
- Decisions (ADRs) — Capture WHY, alternatives considered
- Patterns — Reusable blueprints with code examples
- Layer overview — Visual understanding of structure

**Medium value:**
- Foundation docs — Cross-cutting concerns (logging, config)
- Layer details — Expand on overview

**Low value:**
- Module details that duplicate specs
- External dependency docs (better in code)

**Recommendation:** Keep decisions + patterns + overview. Consolidate or simplify the rest. Use references to specs instead of duplicating interface code.

### Specifications (250_*) Value

**High value:**
- Interface definitions — The actual contract
- Behavior rules — Method semantics
- Boundary tables — Edge cases
- Method signature tables — Quick reference

**Medium value:**
- Acceptance criteria — Good but non-executable
- Platform notes — Implementation guidance

**Low value:**
- Verbose prose that restates interfaces
- Assumptions/exclusions boilerplate

**Recommendation:** Keep specs as they are. Add LLM Summary sections. Remove any architecture discussion (move to 200_*).

### Overall Assessment

| Layer | Current Files | Estimated High-Value | Suggested |
|-------|---------------|----------------------|-----------|
| 200_architecture/ | 25 | ~13 | Consolidate to ~12-15 |
| 250_specifications/ | 15 | ~12 | Keep, add summaries |
| **Total** | **40** | **~25** | **~30 focused files** |

### The Key Insight

**Architecture and Specifications serve different purposes but currently overlap significantly.**

- **Architecture answers:** WHY this design? HOW does it fit together?
- **Specifications answer:** WHAT is the interface? HOW does it behave?

**The fix:** Make each layer authoritative for its purpose. Architecture references specs. Specs reference patterns. No duplication.

---

## Related Documents

- [IDEAS-003: SPX LLM Integration](IDEAS-003-SPX-LLM-Integration-QA.md)
- [IDEAS-002: LLM Documentation Strategy](IDEAS-002-LLM-Documentation-Strategy.md)
- [specs2/200_architecture/](../specs2/200_architecture/)
- [specs2/250_specifications/](../specs2/250_specifications/)
- [SPX v7 Editions](../SPX/Docs/V7/blocks2/000_basics/001_Spx.md)

---

**Version:** 2.0  
**Status:** Draft  
**Last Review:** January 9, 2026
