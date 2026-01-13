# IDEAS-005: Specification Granularity Problem

**Created:** January 9, 2026  
**Status:** Open  
**Priority:** High  
**Related:** [IDEAS-004](IDEAS-004-Specification-Value-Analysis.md), [IDEAS-003](IDEAS-003-SPX-LLM-Integration-QA.md)

---

## 1. The Problem

Despite improvements in IDEAS-004, we still have significant duplication and over-specification:

1. **Patterns (231_*)** contain concrete code examples that will be duplicated in actual source
2. **Specifications (250_*)** contain detailed interface definitions that are just copied to source
3. **Automated generation/modification** of specs is difficult because they're too large and detailed
4. **Source code becomes the real spec** once implemented — the `.spx.md` files become stale

### The Core Issue

> **Current state:** SPX documents contain implementation details  
> **Desired state:** SPX documents describe WHAT to build; source IS the detail

---

## 2. Evidence of Over-Specification

### 2.1 Patterns (231_*) Problem

Looking at `231_001_ControlObjectPattern.spx.md`:

```csharp
// This code appears in the pattern document
public abstract class ControlBase<TElement, TScope> : IControlObject
    where TScope : IElementScope<TElement>
{
    protected readonly TScope _scope;
    protected readonly Locator _locator;
    
    protected ControlBase(TScope scope, Locator locator)
    {
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _locator = locator ?? throw new ArgumentNullException(nameof(locator));
    }
    // ... 50+ more lines
}
```

**Problem:** This exact code will exist in `src/Brinell.Core/Controls/ControlBase.cs`. Now we have:
- Pattern doc with code
- Source file with code
- XML docs in source
- Potential LLM Summary in spec

**Result:** 4 places to update when something changes.

### 2.2 Specifications (250_*) Problem

Looking at `250_001_IControlObject.spx.md`:

| Section | Lines | Will Be In Source? |
|---------|-------|-------------------|
| LLM Summary | ~60 | Yes (interface) |
| Overview | ~30 | Partially (XML docs) |
| Behavior | ~200 | Yes (implementation + XML) |
| Boundary | ~50 | Yes (tests) |
| Acceptance | ~80 | Yes (tests) |
| Complete Interface | ~50 | Yes (interface file) |
| Generic Base Class | ~100 | Yes (base class file) |

**~570 lines** of spec, most of which will exist in source code.

### 2.3 The Duplication Chain

```
┌─────────────────┐
│  231 Pattern    │ ← Contains concrete implementation code
└────────┬────────┘
         │ duplicates to
         ▼
┌─────────────────┐
│  250 Spec       │ ← Contains interface + behavior + examples
└────────┬────────┘
         │ duplicates to
         ▼
┌─────────────────┐
│  Source Code    │ ← The actual implementation
└────────┬────────┘
         │ duplicates to
         ▼
┌─────────────────┐
│  XML Docs       │ ← Method documentation
└────────┬────────┘
         │ duplicates to
         ▼
┌─────────────────┐
│  Tests          │ ← Boundary/acceptance behavior
└─────────────────┘
```

---

## 3. What SPX Documents SHOULD Contain

### Principle: Specs Describe Intent, Source Contains Detail

| Document Type | Should Contain | Should NOT Contain |
|---------------|----------------|-------------------|
| **Architecture (200_*)** | WHY decisions, HOW things fit | Implementation code |
| **Patterns (231_*)** | Pattern structure, WHEN to use | Complete implementations |
| **Specifications (250_*)** | Interface contract, behavior rules | Full method implementations |
| **Source Code** | Complete implementation | (is the detail) |
| **Tests** | Behavior verification | (is the detail) |

### The Right Level of Detail

**Architecture/Patterns should be:**
- Conceptual diagrams
- Decision rationale
- Pattern structure (abstract)
- When to use / when not to use
- References to specifications

**Specifications should be:**
- Interface signature (method names, parameters, return types)
- Behavior rules (numbered, testable)
- Boundary conditions (table format)
- References to source for implementation

**Source should be:**
- The authoritative implementation
- XML docs with behavior notes
- Links back to specs for rationale

---

## 4. Proposed Solution: Thin Specifications

### 4.1 Thin Pattern Format

```markdown
# 231.001 ControlObject Pattern

## Intent
Wrap platform-specific UI elements with a consistent interface.

## When to Use
- All UI controls in test automation
- When you need state checking, waiting, assertions

## Structure
```
IControlObject
    ├── Locator (identity)
    ├── Scope (context)
    └── Methods: IsExists, IsVisible, WaitExists, AssertExists, GetText
```

## Key Decisions
- Returns null when element doesn't exist (not false)
- Wait methods return bool, never throw
- Assert methods throw on failure

## Implementation
See: `src/Brinell.Core/Controls/ControlBase.cs`

## Related
- [250_001 IControlObject](../250_specifications/250_001_IControlObject.spx.md)
```

**~50 lines instead of ~300 lines. No code blocks.**

### 4.2 Thin Specification Format

```markdown
# 250.001 IControlObject Specification

## Interface
See: `src/Brinell.Core/Interfaces/IControlObject.cs`

## Behavior Rules
1. `IsVisible()` and `IsEnabled()` return `null` when element doesn't exist
2. All `Wait*` methods return `bool`, never throw
3. All `Assert*` methods throw `AssertionException` on failure
4. Nullable Skip Pattern: If `expected` is `null`, skip operation
5. Default timeout from `context.Timeouts` when not specified

## Boundaries
| Scenario | Behavior |
|----------|----------|
| `IsExists()` on missing | Returns `false` |
| `IsVisible()` on missing | Returns `null` |
| `WaitExists(null)` | Returns `true` (skip) |

## Acceptance
See: `tests/Brinell.Core.Tests/IControlObjectTests.cs`

## Implementation
See: `src/Brinell.Core/Controls/ControlBase.cs`
```

**~40 lines instead of ~570 lines. Rules only, no code.**

### 4.3 Source as Specification

The source file becomes the detailed spec:

```csharp
// src/Brinell.Core/Interfaces/IControlObject.cs

namespace Brinell.Core.Interfaces;

/// <summary>
/// Base interface for all controls in the Brinell framework.
/// </summary>
/// <remarks>
/// Spec: specs2/250_specifications/250_001_IControlObject.spx.md
/// Pattern: specs2/200_architecture/231_Patterns/231_001_ControlObjectPattern.spx.md
/// </remarks>
public interface IControlObject
{
    /// <summary>
    /// Check if element is visible.
    /// </summary>
    /// <returns>
    /// True if visible, false if hidden, NULL if element doesn't exist.
    /// </returns>
    /// <remarks>
    /// Rule: Returns null (not false) when element is not in UI tree.
    /// </remarks>
    bool? IsVisible();
    
    // ... rest of interface
}
```

**The source IS the specification. XML docs contain the rules.**

---

## 5. Migration Strategy

### Phase 1: Stop Adding Detail to Specs

When creating new specs:
- Write thin specs (rules + boundaries only)
- Reference source files for implementation
- Reference test files for acceptance

### Phase 2: Slim Existing Specs

For each 250_* spec:
1. Extract code blocks → move to source (if not already there)
2. Keep only: rules, boundaries, references
3. Add "See:" links to source files

For each 231_* pattern:
1. Remove implementation code
2. Keep only: intent, structure (abstract), when to use
3. Add "See:" links to source files

### Phase 3: Source-First Workflow

```
┌─────────────────┐
│  Thin Spec      │ ← Rules, boundaries, references (50-100 lines)
│  (.spx.md)      │
└────────┬────────┘
         │ references
         ▼
┌─────────────────┐
│  Source Code    │ ← Complete implementation + XML docs
│  (.cs)          │    (authoritative detail)
└────────┬────────┘
         │ verified by
         ▼
┌─────────────────┐
│  Tests          │ ← Boundary/acceptance verification
│  (*Tests.cs)    │
└─────────────────┘
```

---

## 6. Impact on LLM Workflows

### Current Problem for LLMs

- Large specs = context overflow / attention dilution
- Code in specs + code in source = confusion about which is authoritative
- Updating specs is expensive (large files, many sections)

### With Thin Specs

**For implementation:**
```
LLM reads:
1. Thin spec (50 lines) — rules and boundaries
2. Source file (authoritative) — actual interface
3. Test file — expected behavior

No confusion. Source is truth.
```

**For spec updates:**
```
LLM reads:
1. Thin spec (50 lines) — easy to parse and update
2. Source changes — extract new rules

Small files = easier automated updates.
```

### SpxLlm.cfg.md Simplification

With thin specs, the LLM config becomes simpler:

```markdown
## Include

| Block Type | Section | Notes |
|------------|---------|-------|
| specification | rules | Numbered rules only |
| specification | boundaries | Edge case table |
| specification | references | Links to source |

## Exclude

| Block Type | Notes |
|------------|-------|
| * | Everything else (code is in source) |
```

---

## 7. Do We Need All 250_* Files?

### Current Plan: One Spec Per Interface

```
250_001_IControlObject.spx.md
250_002_IPageObject.spx.md
250_003_IContainerControlObject.spx.md
250_004_TestContext.spx.md
250_005_InterfaceHierarchy.spx.md
... (15+ files)
```

### Alternative: Consolidated Specs

**Option A: One file per category**
```
250_001_CoreInterfaces.spx.md      # IControlObject, IPageObject, ITestContext
250_002_CapabilityInterfaces.spx.md # IClickable, IText, IToggle, etc.
250_003_ContainerInterfaces.spx.md  # IContainer, ICollection
250_004_PlatformContexts.spx.md     # IMaui, IBlazor, IWpf contexts
```

**Option B: Single foundation spec**
```
250_001_Foundation.spx.md          # All foundation interfaces (rules only)
```

### Recommendation

With thin specs, **consolidation becomes natural**:
- Each interface needs only 20-40 lines (rules + boundaries)
- 10 interfaces = 200-400 lines = manageable single file
- Easier to maintain consistency
- Easier for LLMs to consume

---

## 8. What About Acceptance Criteria?

### Current: Gherkin in Specs

```gherkin
Given a control that exists and is visible
When IsExists() is called
Then it returns true
```

**Problem:** These don't run. They're just documentation.

### Proposed: Tests ARE Acceptance

```csharp
// tests/Brinell.Core.Tests/IControlObjectTests.cs

[Fact]
public void IsExists_WhenControlExists_ReturnsTrue()
{
    // Arrange
    var control = CreateExistingControl();
    
    // Act
    var result = control.IsExists();
    
    // Assert
    Assert.True(result);
}
```

**The test IS the acceptance criterion.** Spec just references it:

```markdown
## Acceptance
See: `tests/Brinell.Core.Tests/IControlObjectTests.cs`
- IsExists_WhenControlExists_ReturnsTrue
- IsExists_WhenControlMissing_ReturnsFalse
- IsVisible_WhenControlMissing_ReturnsNull
```

---

## 9. Comparison: Current vs Proposed

| Aspect | Current | Proposed |
|--------|---------|----------|
| Spec size | 300-600 lines | 50-100 lines |
| Code in specs | Yes (duplicated) | No (reference only) |
| Acceptance | Gherkin (non-executable) | Test references |
| Source of truth | Unclear (spec or source?) | Source (always) |
| LLM consumption | Difficult (large files) | Easy (thin files) |
| Maintenance | Update 4 places | Update source + thin spec |
| Automated updates | Hard | Easier |

---

## 10. Risks and Mitigations

| Risk | Mitigation |
|------|------------|
| Specs become too thin to understand | Keep rules and boundaries; link to source for examples |
| Devs skip reading specs | Specs are for decisions/rationale; source is for implementation |
| Rules drift from implementation | CI check: extract rules from XML docs, compare to spec |
| Loss of design intent | Architecture (200_*) captures WHY; specs capture WHAT rules |

---

## 11. Action Items

### Immediate

1. [ ] **Pilot thin spec** — Rewrite 250_001 as thin spec (~50 lines)
2. [ ] **Pilot thin pattern** — Rewrite 231_001 as thin pattern (~30 lines)
3. [ ] **Compare** — Evaluate readability, maintainability, LLM usability

### If Pilot Succeeds

4. [ ] **Define thin spec template** — Standard sections, max lines
5. [ ] **Migrate 250_*** — Slim all spec files
6. [ ] **Migrate 231_*** — Slim all pattern files
7. [ ] **Update SpxLlm.cfg.md** — Simpler extraction rules
8. [ ] **Update copilot-instructions.md** — Source is authoritative

### Future

9. [ ] **Consolidate specs** — Combine related interfaces into single files
10. [ ] **Automate sync** — CI checks for spec/source drift
11. [ ] **Generate specs from source** — Reverse the flow

---

## 12. The Vision

### Before (Current)

```
Spec (500 lines) → Source (200 lines) → Tests (300 lines)
     ↓                    ↓                   ↓
  Duplicate           Duplicate           Duplicate
```

### After (Proposed)

```
Thin Spec (50 lines)
    │
    │ "Rules: 1, 2, 3"
    │ "See source for implementation"
    │
    ▼
Source (200 lines) ← AUTHORITATIVE
    │
    │ XML docs contain rules
    │
    ▼
Tests (300 lines) ← Verify rules
```

**One source of truth. Specs describe intent. Source is detail.**

---

## 13. Questions to Resolve

1. **How thin is too thin?** What's the minimum viable spec?
2. **What stays in specs?** Rules only? Boundaries? Examples?
3. **How to handle pre-implementation?** Spec-first vs source-first?
4. **Consolidation level?** One file per interface vs category vs all?
5. **Tooling needs?** Spec-to-source generators? Source-to-spec extractors?

---

## Related Documents

- [IDEAS-004: Architecture vs Specification Value](IDEAS-004-Specification-Value-Analysis.md)
- [IDEAS-003: SPX LLM Integration](IDEAS-003-SPX-LLM-Integration-QA.md)
- [Questions-to-Blocks.md](../SPX/Docs/V7/Overview/Questions-to-Blocks.md)
- [SpxLlm.cfg.md](../specs2/SpxLlm.cfg.md)

---

**Version:** 1.0  
**Status:** Open  
**Last Review:** January 9, 2026
