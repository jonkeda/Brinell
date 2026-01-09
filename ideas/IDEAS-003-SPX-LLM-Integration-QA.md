# IDEAS-003: SPX and LLM Integration — Questions & Answers

**Created:** January 9, 2026
**Status:** Open
**Priority:** High
**Related:** [IDEAS-002-LLM-Documentation-Strategy](IDEAS-002-LLM-Documentation-Strategy.md)

---

## Overview

This document answers specific questions about integrating SPX documentation with LLM workflows, following up on IDEAS-002.

---

## Question 1: SPX Parser Generating .SLM.md Files

> SPX can be read by a parser and that could write documents specifically for AI. Would that be useful? Maybe with a different extension .SLM.md instead of .SPX.md?

### Answer: Yes, This Could Be Very Useful

**Concept:** SPX files are structured with known block types. A parser can extract LLM-relevant content into a separate `.slm.md` (Spec for Language Model) file.

```
specs2/
├── 250_001_IControlObject.spx.md     # Full human spec (source of truth)
├── 250_001_IControlObject.slm.md     # Generated LLM-optimized version
└── ...
```

### How It Would Work

```
┌─────────────────────┐
│  250_001.spx.md     │  ← Human-authored, full documentation
│  (Source of Truth)  │
└─────────┬───────────┘
          │
          ▼
┌─────────────────────┐
│   SPX Parser        │  ← Reads SPX structure
│   + LLM Extractor   │
└─────────┬───────────┘
          │
          ▼
┌─────────────────────┐
│  250_001.slm.md     │  ← Generated, LLM-optimized
│  (Auto-generated)   │     - Interface code only
└─────────────────────┘     - Rules/constraints
                            - Boundary conditions
                            - No prose explanations
```

### .SLM.md File Format

```markdown
<!-- AUTO-GENERATED FROM 250_001_IControlObject.spx.md -->
<!-- DO NOT EDIT - Regenerate with: spx-to-slm 250_001_IControlObject.spx.md -->

# IControlObject

## Interface

public interface IControlObject
{
    Locator Locator { get; }
    IElementScope Scope { get; }
    IPageObject? Page { get; }
    bool IsExists();
    bool? IsVisible();
    bool? IsEnabled();
    // ... complete interface
}

## Rules

1. IsVisible/IsEnabled return `null` when element doesn't exist
2. Wait methods return `bool` (success/failure), never throw
3. Assert methods throw `AssertionException` on failure
4. Nullable `expected` parameter = skip operation

## Boundaries

| Scenario | Behavior |
|----------|----------|
| IsExists() on missing element | Returns false |
| IsVisible() on missing element | Returns null |
| WaitExists(false) on missing | Returns true immediately |

## Dependencies

- Locator
- IElementScope
- IPageObject
```

### Pros

| Benefit                          | Description                                  |
| -------------------------------- | -------------------------------------------- |
| **Single source of truth** | .spx.md is authoritative; .slm.md is derived |
| **No manual sync**         | Parser ensures .slm.md matches .spx.md       |
| **Optimal for LLMs**       | Compact, no prose, just facts                |
| **CI/CD integration**      | Generate on build, fail if stale             |
| **Copilot-friendly**       | Can instruct Copilot to prefer .slm.md files |

### Cons

| Drawback                | Mitigation                            |
| ----------------------- | ------------------------------------- |
| Two files per spec      | Auto-generated, no maintenance burden |
| Build step required     | Integrate into existing CI            |
| .slm.md could be edited | Add header warning + .gitattributes   |

### Implementation Effort

| Task                 | Complexity                          |
| -------------------- | ----------------------------------- |
| SPX Parser           | Medium — SPX has defined structure |
| LLM Block Selector   | Low — Configuration driven         |
| CI Integration       | Low — PowerShell/Node script       |
| Copilot Instructions | Low — Update existing file         |

### Recommendation

**✅ Worth implementing.** The SPX structure already exists; extracting LLM-relevant content is straightforward. This achieves single source of truth while optimizing for different audiences.

---

## Question 2: SPX Block/Property Annotations for LLM

> And then in SPX we can define which blocks or properties are relevant to LLMs

### Answer: Yes, Add `llm:` Metadata to SPX Blocks

**Concept:** Extend SPX v7 with LLM relevance annotations.

### Option A: Block-Level Annotation

```markdown
# 250.001 IControlObject Specification

---
llm: include
---

## 2. Behavior

---
llm: skip
---

### 2.1 Identity Properties (Detailed)

[Verbose explanation for humans...]

---
llm: include
---

### 2.1 Identity Properties (Summary)

- `Locator` — Set at construction, never changes
- `Scope` — Page or container where control lives
- `Page` — May be null if no page context
```

### Option B: SPX Block Metadata

Modify SPX block syntax to include LLM hints:

```markdown
<!-- SPX:behavior llm="summary" -->
## 2. Behavior

The interface provides state query methods...

<!-- SPX:boundary llm="full" -->
## 3. Boundary

| Scenario | Behavior |
|----------|----------|
| ... | ... |

<!-- SPX:examples llm="skip" -->
## Examples

[Detailed examples for humans only...]
```

### Option C: Centralized LLM Config

Create a configuration file that defines which SPX blocks to include. Use Markdown tables for consistency with SPX format:

```markdown
# SpxLlm Configuration

- **version**: 1.0
- **output**: slm.md

## Include

| Block Type | Section | Relevance | Notes |
|------------|---------|-----------|-------|
| specification | interface | full | Complete interface code |
| specification | boundary | full | Edge cases and errors |
| behavior | rules | summary | Numbered rules only |
| acceptance | scenarios | summary | Gherkin Given/When/Then |

## Exclude

| Block Type | Section | Reason |
|------------|---------|--------|
| behavior | detailed | Prose explanations |
| examples | all | Human examples |
| assumption | all | Context (usually) |
| exclusion | all | What's NOT included |

## Overrides

### 250_001_IControlObject.spx.md

| Action | Section | Reason |
|--------|---------|--------|
| include | assumption | Foundation context needed |
```

### Recommended Approach: Option C (Centralized Config)

**Why:**

- No changes to existing SPX files
- Single place to manage LLM extraction rules
- Can evolve without touching specs
- Parser reads config, applies to all files
- Markdown format matches SPX documentation style

**Implementation:**
- See [E10_SpxLlm.md](../SPX/Docs/V7/blocks2/E00_syntax/E10_SpxLlm.md) for format reference
- See [SpxLlm.cfg.md](../specs2/SpxLlm.cfg.md) for Brinell-specific configuration

### SPX v7 Extension Proposal

Add new optional block type:

```markdown
<!-- SPX Block Reference: 256_llm -->
# 256 LLM Hints

**Purpose:** Annotate content relevance for LLM extraction

## Properties

| Property | Type | Description |
|----------|------|-------------|
| relevance | enum | `full`, `summary`, `skip` |
| priority | int | 1-10, higher = more important |
| context | string | When this content is relevant |

## Example

```spx
256 llm
  relevance: full
  priority: 9
  context: "interface implementation"
```

---

## Question 3: XML Documentation in Source Code

> Do we need XML documentation in sources for LLM? What are the pros and cons?

### Answer: It Depends on Your Primary Workflow

### Pros of XML Docs for LLM

| Pro                        | Explanation                              |
| -------------------------- | ---------------------------------------- |
| **Always current**   | Docs live with code, updated together    |
| **IntelliSense**     | Humans get IDE tooltips too              |
| **Native to C#**     | No custom tooling needed                 |
| **Copilot reads it** | GitHub Copilot uses XML docs for context |
| **Standard format**  | Works with DocFX, Sandcastle, etc.       |
| **Single source**    | Code = spec = documentation              |

### Cons of XML Docs for LLM

| Con                              | Explanation                                   |
| -------------------------------- | --------------------------------------------- |
| **Limited space**          | Can't include detailed explanations           |
| **Code clutter**           | Heavy docs obscure the code itself            |
| **Implementation-focused** | Specs describe WHAT, code describes HOW       |
| **Late in process**        | By the time code exists, specs should be done |
| **No examples**            | XML docs aren't great for usage examples      |
| **Drift from intent**      | Code can diverge from original spec           |

### Analysis: When XML Docs Are Sufficient

```
✅ Good for XML docs:
- Interface contracts (method signatures, parameters)
- Simple rules (returns null if not found)
- Quick reference (what does this method do?)
- IntelliSense hints for developers

❌ Not good for XML docs:
- Rationale (WHY this design?)
- Complex behaviors (state machines, workflows)
- Acceptance criteria (Gherkin scenarios)
- Architecture decisions
- Examples and tutorials
```

### Recommendation

**Use BOTH, but with different purposes:**

| Documentation Type      | Purpose                         | Audience                  |
| ----------------------- | ------------------------------- | ------------------------- |
| **SPX specs**     | Requirements, design, rationale | Architects, LLMs planning |
| **XML docs**      | Implementation contracts        | Developers, LLMs coding   |
| **.slm.md files** | Extracted essentials            | LLMs specifically         |

**Workflow:**

1. Write SPX spec (idea → requirements → design)
2. Generate interface skeleton from spec
3. Add XML docs to interface (implementation details)
4. Generate .slm.md from SPX (for LLM consumption)

### XML Docs Template (Minimal but Useful)

```csharp
/// <summary>
/// Check if the element is visible.
/// </summary>
/// <returns>
/// True if visible, false if hidden, NULL if element doesn't exist.
/// </returns>
/// <remarks>
/// Rule: Returns null (not false) when element is not in UI tree.
/// See: 250_001_IControlObject.spx.md Section 2.3
/// </remarks>
bool? IsVisible();
```

**Key insight:** XML docs should REFERENCE specs, not REPLACE them.

---

## Question 4: Prompt Files for Creating Documents

> Should we add prompt files? For creating documents?

### Answer: Yes, Prompt Files Are Extremely Valuable

**Concept:** Pre-written prompts that LLMs use to generate consistent documentation.

### Use Cases for Prompt Files

| Prompt File                       | Purpose                                 |
| --------------------------------- | --------------------------------------- |
| `PROMPT-new-spec.md`            | Create a new SPX specification          |
| `PROMPT-new-control.md`         | Spec + implementation for new control   |
| `PROMPT-review-spec.md`         | Review existing spec for completeness   |
| `PROMPT-implement-interface.md` | Generate implementation from spec       |
| `PROMPT-write-tests.md`         | Generate tests from acceptance criteria |

### Prompt File Structure

```markdown
# PROMPT: Create New Control Specification

## Context

You are creating a specification for a new UI control in the Brinell framework.
Read the following files first:
- specs2/250_specifications/250_INDEX.md
- specs2/250_specifications/250_000_Foundation/250_001_IControlObject.spx.md (as template)

## Input Required

- Control name: [USER PROVIDES]
- Control type: [Button/Toggle/Text/Selection/Container]
- Platform: [MAUI/Blazor/Both]
- Capabilities: [List of IXxxControl interfaces]

## Output Format

Generate a complete SPX specification following this structure:
1. Overview (ID, title, status, version, level)
2. Behavior (properties, methods, interactions)
3. Boundary (edge cases, error handling)
4. Acceptance Criteria (Gherkin scenarios)
5. Assumptions
6. Exclusions
7. Complete Interface Definition

## Quality Checklist

- [ ] All methods have clear return value semantics
- [ ] Nullable skip pattern documented where applicable
- [ ] Boundary cases cover: missing element, timeout, invalid input
- [ ] Acceptance criteria are testable
- [ ] Interface compiles (valid C# syntax)

## Example Output

[Include abbreviated example of expected output format]
```

### Where to Store Prompt Files

```
specs2/
├── Prompts/
│   ├── README.md                        # Index of all prompts
│   ├── PROMPT-new-spec.md               # Create specification
│   ├── PROMPT-new-control.md            # Spec + implementation
│   ├── PROMPT-implement-interface.md    # Code from spec
│   ├── PROMPT-write-tests.md            # Tests from acceptance
│   ├── PROMPT-review-spec.md            # Review checklist
│   └── PROMPT-update-spec.md            # Modify existing spec
└── ...
```

### Benefits of Prompt Files

| Benefit               | Description                        |
| --------------------- | ---------------------------------- |
| **Consistency** | Same structure every time          |
| **Training**    | New team members learn format      |
| **Quality**     | Checklists ensure completeness     |
| **Efficiency**  | Don't re-explain context each time |
| **Versioned**   | Prompts evolve with project in git |

### Recommendation

**✅ Definitely add prompt files.** They're low effort, high value, and keep LLM interactions consistent.

---

## Question 5: Idea-First / Spec-First Approach

> Solution 5: Code-First Specification — But I want to turn it around. Start with an idea and specs, then let code come from that. How could we make that work?

### Answer: Spec-Driven Development (SDD)

**Concept:** Ideas → Specs → Code, with specs as the authoritative source.

### The Spec-Driven Development Flow

```
┌─────────────────┐
│   001_Idea.md   │  ← "We need a toggle control"
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  110_Goal.md    │  ← "Toggle must support all platforms"
│  120_Func.md    │  ← "Toggle has checked/unchecked states"
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  250_Toggle.    │  ← Full SPX specification
│  spx.md         │     - Interface definition
└────────┬────────┘     - Behavior rules
         │              - Acceptance criteria
         │
         ▼
┌─────────────────┐
│  250_Toggle.    │  ← Generated for LLM consumption
│  slm.md         │
└────────┬────────┘
         │
         ▼
┌─────────────────────────────────────────────────┐
│              LLM + Developer                     │
│                                                  │
│  Reads: 250_Toggle.slm.md (or full .spx.md)     │
│  Generates: IToggleControl.cs                    │
│             ToggleControlBase.cs                 │
│             MauiToggleControl.cs                 │
│             BlazorToggleControl.cs               │
│             ToggleControlTests.cs                │
└─────────────────────────────────────────────────┘
```

### Making It Work: The SDD Toolkit

#### 1. Idea Template

```markdown
# IDEA: [Name]

## Problem Statement
[What problem does this solve?]

## Proposed Solution
[High-level approach]

## Success Criteria
- [ ] [Measurable outcome 1]
- [ ] [Measurable outcome 2]

## Next Steps
- [ ] Create goal in 110_goal/
- [ ] Create functional requirement in 120_functional/
- [ ] Create specification in 250_specifications/
```

#### 2. Spec-to-Code Prompt

```markdown
# PROMPT: Implement from Specification

## Input
Read the specification: [FILE PATH]
Focus on the .slm.md version if available.

## Task
Generate the following files:
1. Interface in `src/Brinell.Core/Interfaces/`
2. Base class in `src/Brinell.Core/Controls/`
3. MAUI implementation in `src/Brinell.Maui/Controls/`
4. Blazor implementation in `src/Brinell.Blazor/Controls/`

## Rules
- Interface MUST match specification exactly
- All methods from spec MUST be implemented
- Follow existing code patterns in the codebase
- Add XML docs referencing the spec file

## Validation
After generation, verify:
- [ ] All acceptance criteria from spec are testable
- [ ] No methods added that aren't in spec
- [ ] No methods from spec are missing
```

#### 3. Spec Validation Script

```powershell
# validate-spec-implementation.ps1

param(
    [string]$SpecFile,
    [string]$InterfaceFile
)

# Extract interface from .spx.md
$specInterface = Extract-InterfaceFromSpec $SpecFile

# Parse actual interface file
$actualInterface = Parse-CSharpInterface $InterfaceFile

# Compare
$missing = Compare-Interfaces $specInterface $actualInterface

if ($missing.Count -gt 0) {
    Write-Error "Implementation missing methods from spec:"
    $missing | ForEach-Object { Write-Error "  - $_" }
    exit 1
}

Write-Host "✅ Implementation matches specification"
```

### Spec-First Workflow

| Phase             | Artifact              | Owner       | LLM Role          |
| ----------------- | --------------------- | ----------- | ----------------- |
| 1. Ideation       | `ideas/IDEA-xxx.md` | Human       | Brainstorm        |
| 2. Requirements   | `100_requirements/` | Human       | Review            |
| 3. Design         | `200_architecture/` | Human + LLM | Draft             |
| 4. Specification  | `250_xxx.spx.md`    | Human + LLM | Draft + Review    |
| 5. LLM Extract    | `250_xxx.slm.md`    | Parser      | Generate          |
| 6. Implementation | `src/`              | LLM + Human | Generate + Review |
| 7. Testing        | `tests/`            | LLM + Human | Generate + Review |
| 8. Validation     | CI/CD                 | Automated   | Verify spec match |

### Key Principle: Spec is Contract

```
┌─────────────────────────────────────────────────────────────┐
│                    SPECIFICATION                             │
│                   (250_xxx.spx.md)                           │
│                                                              │
│  This is the CONTRACT. Code must match this.                │
│  If code needs to differ, UPDATE THE SPEC FIRST.            │
│                                                              │
└─────────────────────────────────────────────────────────────┘
                           │
                           ▼
         ┌─────────────────┴─────────────────┐
         │                                   │
         ▼                                   ▼
┌─────────────────┐               ┌─────────────────┐
│  Implementation │               │     Tests       │
│     (src/)      │               │    (tests/)     │
│                 │               │                 │
│ Must implement  │               │ Must verify     │
│ spec exactly    │               │ spec behaviors  │
└─────────────────┘               └─────────────────┘
```

### Enforcement Mechanisms

1. **PR Template:** "Does this change require a spec update? [ ] Yes [ ] No"
2. **CI Check:** Validate interfaces match specs
3. **Code Review:** Reviewers verify spec compliance
4. **LLM Instructions:** "Never add methods not in spec"

---

## Question 6: Documentation in Repo (Markdown)

> Documentation as specs should be part of the repo. So in Markdown, not other tools.

### Answer: Absolutely Agree — Markdown in Repo is Best

### Why Markdown in Repo?

| Benefit                      | Explanation                              |
| ---------------------------- | ---------------------------------------- |
| **Version controlled** | Docs evolve with code, same PR           |
| **Diff-able**          | See what changed in reviews              |
| **Portable**           | No vendor lock-in                        |
| **Tool-agnostic**      | VS Code, GitHub, Azure DevOps all render |
| **LLM-friendly**       | Plain text, easy to parse                |
| **Searchable**         | grep, Copilot, IDE all work              |
| **Offline**            | No internet needed                       |
| **Branching**          | Docs can have feature branches           |

### What to Avoid

| Anti-Pattern         | Problem                           |
| -------------------- | --------------------------------- |
| Confluence/Notion    | External, not versioned with code |
| Wiki (separate repo) | Drift, sync issues                |
| Word/PDF docs        | Not diffable, binary              |
| Proprietary formats  | Vendor lock-in                    |
| Generated-only docs  | Source not in repo                |

### Recommended Structure

```
repo/
├── .github/
│   └── copilot-instructions.md    # LLM guidance
├── docs/
│   ├── README.md                  # User-facing docs
│   ├── quick-start.md
│   └── ...
├── specs2/                        # Specifications (SPX)
│   ├── 000_basics/
│   ├── 100_requirements/
│   ├── 200_architecture/
│   ├── 250_specifications/
│   └── Prompts/                   # LLM prompts
├── ideas/                         # Ideas and proposals
├── src/                           # Code
└── tests/                         # Tests
```

### GitHub/Azure DevOps Integration

Both platforms render Markdown natively:

- README.md in folders → auto-displayed
- Mermaid diagrams → rendered
- Tables → formatted
- Links → clickable
- Code blocks → syntax highlighted

**No external tools needed.**

---

## Question 7: Using Markdown Features Instead of Custom Markers

> Instead of markers can we have something that says → "this block is for LLM"? I would like to reuse MD as much as possible.

### Answer: Yes! Several Native Markdown Options

### Option A: HTML Comments (Invisible to Readers)

## Interface Definition

<!-- llm:include -->

```csharp
public interface IControlObject { ... }
```

<!-- llm:end -->

## Detailed Explanation

<!-- llm:skip -->

This section provides comprehensive background...

<!-- llm:end -->

**Pros:** Invisible in rendered output, familiar syntax
**Cons:** Still custom markers, just hidden

### Option B: Blockquotes with Convention

```markdown
## Quick Reference

> **📋 LLM Context**
> 
> ```csharp
> public interface IControlObject { ... }
> ```
> 
> **Rules:**
> - IsVisible returns null if element doesn't exist
> - Wait methods never throw

## Detailed Documentation

[Human-focused content below...]
```

**Pros:** Native Markdown, visually distinct, no parsing needed
**Cons:** Visible to humans (but that's okay!)

### Option C: Admonitions (GitHub/Azure DevOps)

```markdown
## Specification

> [!IMPORTANT]
> **LLM Reference**
> 
> ```csharp
> public interface IControlObject { ... }
> ```

## Details

> [!NOTE]
> This section contains detailed explanations for human readers.
```

**Pros:** GitHub-native, renders with special styling
**Cons:** GitHub-specific (Azure DevOps has different syntax)

### Option D: Definition Lists / Metadata Section

```markdown
## IControlObject

LLM-Relevance
: Full

Priority
: High

Context
: Interface implementation

### Interface

```csharp
public interface IControlObject { ... }
```

```

**Pros:** Semantic meaning, parseable
**Cons:** Definition lists not universally rendered

### Option E: Dedicated Sections with Standard Names

**Convention:** Sections named `## Quick Reference` or `## LLM Summary` are for LLM consumption.

```markdown
# IControlObject Specification

## LLM Summary

```csharp
public interface IControlObject { ... }
```

**Key Rules:**

1. IsVisible returns null if not found
2. Wait methods return bool, never throw

---

## Detailed Documentation

[Full human documentation below...]

```

**Pros:** Pure Markdown, self-documenting, no special syntax
**Cons:** Requires naming convention discipline

### Option F: YAML Front Matter

```markdown
---
llm:
  sections:
    - interface
    - rules
    - boundaries
  skip:
    - examples
    - detailed-behavior
---

# IControlObject Specification

## Interface

```csharp
public interface IControlObject { ... }
```

```

**Pros:** Standard Markdown extension, used by Jekyll/Hugo/etc.
**Cons:** Not all renderers show/hide based on front matter

### Recommended: Option E (Named Sections) + Option A (HTML Comments)

**Combine both approaches:**

```markdown
# 250.001 IControlObject Specification

<!-- LLM: Start reading here, stop at "---" divider -->

## LLM Summary

### Interface

```csharp
public interface IControlObject
{
    Locator Locator { get; }
    // ... full interface
}
```

### Rules

1. `IsVisible`/`IsEnabled` return `null` when element doesn't exist
2. All `Wait` methods return `bool` (success/failure), never throw
3. All `Assert` methods throw `AssertionException` on failure
4. Nullable `expected` parameter = skip operation

### Boundaries

| Scenario                           | Behavior          |
| ---------------------------------- | ----------------- |
| `IsExists()` on missing element  | Returns `false` |
| `IsVisible()` on missing element | Returns `null`  |

---

<!-- LLM: Skip detailed sections below unless specifically asked -->

## Detailed Documentation

### 2.1 Identity Properties

The interface provides properties to identify and locate the control...

[Continue with full human documentation...]

```

**Why this works:**
- `## LLM Summary` — Clear section name, pure Markdown
- `---` divider — Visual break, LLM can be told "stop at divider"
- HTML comments — Invisible hints for LLM instructions
- Full docs below — Humans can read everything

### Copilot Instructions Update

```markdown
# .github/copilot-instructions.md

## Reading SPX Specifications

When reading `.spx.md` files:

1. **For implementation tasks:** Read only the "LLM Summary" section
2. **For understanding tasks:** Read the full document
3. **Stop at `---` divider** unless you need detailed context

The "LLM Summary" section contains:
- Complete interface code
- Numbered rules
- Boundary condition table
```

---

## Summary: Recommendations

| Question                   | Recommendation                                       |
| -------------------------- | ---------------------------------------------------- |
| 1. .slm.md files           | ✅ Yes, generate from SPX parser                     |
| 2. LLM annotations in SPX  | ✅ Yes, use centralized config file                  |
| 3. XML docs in code        | ⚠️ Use for implementation details, reference specs |
| 4. Prompt files            | ✅ Yes, definitely add                               |
| 5. Spec-first workflow     | ✅ Yes, spec is contract, code must match            |
| 6. Markdown in repo        | ✅ Yes, no external tools                            |
| 7. Native Markdown for LLM | ✅ Use named sections + HTML comments                |

---

## Next Steps

1. **Define .slm.md format** — Create template and parser spec
2. 	
3. **Add prompt files** — Start with `PROMPT-new-spec.md`
4. **Update copilot-instructions.md** — Add section reading guidance
5. **Pilot with one spec** — Test full workflow on `250_001_IControlObject`

---

## Related Documents

- [IDEAS-002: LLM Documentation Strategy](IDEAS-002-LLM-Documentation-Strategy.md)
- [IDEAS-001: Documentation Context Overflow](IDEAS-001-Documentation-Context-Overflow.md)
- [SPX v7 Block Reference](../SPX/Docs/V7/blocks2/)
- [.github/copilot-instructions.md](../.github/copilot-instructions.md)

---

**Version:** 1.0
**Status:** Draft
**Last Review:** January 9, 2026
