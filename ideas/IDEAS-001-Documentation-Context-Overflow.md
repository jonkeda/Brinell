# ISSUE-001: Documentation Context Overflow

**Created:** January 9, 2026  
**Status:** Open  
**Priority:** High  
**Impact:** Blocks effective LLM-assisted development

---

## Problem Statement

The specs2 documentation has grown unwieldy:
- **Too long** — Individual documents exceed practical reading limits
- **Too duplicated** — Same examples repeated across multiple files
- **Too many examples** — Verbosity intended for humans overwhelms LLM context
- **Context overflow** — Reading all specs exhausts LLM context before code generation begins

### Symptoms

1. Phase 1 implementation required reading 10+ spec files
2. Each spec file is 200-600 lines
3. Total context consumed: ~5000-8000 lines just for specs
4. Leaves insufficient context for actual implementation
5. LLM makes assumptions rather than reading everything

---

## Root Cause Analysis

### Current Flow (Broken)
```
Idea → Goals → Requirements → Architecture → Specifications → Design → Code
        ↓           ↓              ↓               ↓
     [500 lines] [1000 lines] [2000 lines]   [3000 lines]
                                                  ↓
                                        CONTEXT OVERFLOW
```

### Why SPX v7 Structure Causes This

SPX v7 is designed for **human documentation**, not **LLM consumption**:
- Rich explanations (good for humans, wasteful for LLMs)
- Redundant examples (humans need repetition, LLMs don't)
- Cross-references (humans navigate; LLMs must load everything)
- Comprehensive coverage (humans skim; LLMs read all)

---

## Proposed Solutions

### Solution A: Layered Documentation (Recommended)

Create **two documentation tiers**:

#### Tier 1: LLM-Optimized Specs (New)
- **Location:** `specs2/LLM/` or `specs2/compact/`
- **Format:** Minimal, no prose, just contracts
- **Size:** Each file < 100 lines
- **Content:** Interface signatures, critical rules only
- **No examples** — LLM can generate from signatures

```markdown
# IControlObject (Compact)

## Interface
```csharp
public interface IControlObject
{
    Locator Locator { get; }
    IElementScope Scope { get; }
    IPageObject? Page { get; }
    bool IsExists();
    bool? IsVisible();  // null if not found
    bool? IsEnabled();  // null if not found
    string? GetText(int? timeoutMs = null);
    // ... rest of methods
}
```

## Rules
- IsVisible/IsEnabled return null when element doesn't exist
- All Wait methods return bool (success/failure)
- All Assert methods throw AssertionException on failure
```

#### Tier 2: Human Documentation (Existing)
- Keep existing SPX v7 docs for human reference
- Full explanations, examples, rationale
- Used for onboarding, reviews, understanding

### Solution B: Progressive Prompt Chain

Instead of one mega-prompt, use **staged prompts**:

```
Stage 1: Architecture Summary (500 lines max)
    ↓ Generates: Interface skeletons
Stage 2: Core Interfaces Spec (300 lines)
    ↓ Generates: Core interfaces
Stage 3: Platform Spec (300 lines per platform)
    ↓ Generates: Platform implementations
Stage 4: Validation Checklist (100 lines)
    ↓ Validates: Against requirements
```

**Prompt Files:**
```
specs2/Prompts/
├── PROMPT-Stage1-Architecture.md      (read 200_* summaries)
├── PROMPT-Stage2-CoreInterfaces.md    (read 250_001-005 compact)
├── PROMPT-Stage3-MauiPlatform.md      (read 250_006 compact)
├── PROMPT-Stage4-BlazorPlatform.md    (read 250_007 compact)
└── PROMPT-Stage5-Validation.md        (checklist only)
```

### Solution C: Specification Compression

Transform existing specs using these rules:

| Remove | Keep |
|--------|------|
| Prose explanations | Interface signatures |
| Multiple examples | One minimal example |
| Rationale sections | Critical rules only |
| Cross-references | Inline dependencies |
| History/versioning | Current version only |

**Compression Target:** 80% reduction (600 lines → 120 lines)

### Solution D: Code-as-Spec Pattern

For implementation phases, provide **reference implementations** instead of specs:

```
specs2/Reference/
├── IControlObject.cs          (the interface IS the spec)
├── IPageObject.cs
├── MauiControlBase.cs         (example implementation)
└── README.md                  (just the rules, 50 lines)
```

LLM reads working code, not documentation about code.

---

## Recommended Approach: Hybrid A + B

### Phase 1: Create Compact Specs
1. Create `specs2/compact/` folder
2. Compress each 250_* spec to <100 lines
3. Remove all examples except interface signatures
4. Keep only "Rules" and "Errors" sections

### Phase 2: Create Staged Prompts
1. Each prompt reads only what's needed for that stage
2. Each stage produces artifacts the next stage can use
3. Total context per stage: <2000 lines

### Phase 3: Validate with Fresh LLM Session
1. Test each stage independently
2. Verify context stays within limits
3. Adjust compression as needed

---

## Implementation Plan

### Immediate (This Session)
- [x] Document the problem (this file)
- [ ] Create `specs2/compact/` folder structure
- [ ] Compress 250_001 (IControlObject) as pilot

### Short Term (Next Session)
- [ ] Compress remaining foundation specs (250_002-009)
- [ ] Create staged prompt files
- [ ] Test with Phase 2 implementation

### Medium Term
- [ ] Establish compression guidelines
- [ ] Update SPX v7 to include "compact variant" guidance
- [ ] Create tooling to auto-generate compact from full specs

---

## Compact Spec Template

```markdown
# [Interface Name]

## Signature
\`\`\`csharp
[Full interface code - no comments needed]
\`\`\`

## Dependencies
- [Interface]: [One-line purpose]

## Rules
1. [Critical rule 1]
2. [Critical rule 2]

## Errors
| Condition | Exception |
|-----------|-----------|
| [When] | [Throws] |
```

**Target:** 50-100 lines per interface

---

## Questions to Resolve

1. **Should compact specs be auto-generated from full specs?**
   - Pro: Single source of truth
   - Con: Adds tooling complexity

2. **Where do examples live?**
   - Option A: Separate `specs2/examples/` folder
   - Option B: In test projects only
   - Option C: In human docs only

3. **How to handle cross-cutting concerns?**
   - Logging, timeouts, exceptions span all interfaces
   - Need compact summary for LLM consumption

4. **Version alignment?**
   - When full spec changes, compact must update
   - Manual sync vs automated extraction

---

## Success Criteria

- [ ] Any single implementation stage fits in 8K context
- [ ] LLM can implement Phase 1 without "running out of context"
- [ ] Human docs remain comprehensive for onboarding
- [ ] No duplicate maintenance burden

---

## Related

- [PROMPT-Phase1-Implementation-v2.md](../specs2/Prompts/PROMPT-Phase1-Implementation-v2.md)
- SPX v7 specification format
- specs2/200_architecture/
- specs2/250_specifications/
