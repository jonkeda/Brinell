# IDEAS-002: LLM Documentation Strategy

**Created:** January 9, 2026  
**Status:** Open  
**Priority:** High  
**Related:** [IDEAS-001-Documentation-Context-Overflow](IDEAS-001-Documentation-Context-Overflow.md)

---

## 1. Problem Analysis

### 1.1 What is the Actual Problem?

Before proposing solutions, let's clearly define the **real problems** we face:

#### Problem A: Context Window is NOT the Limiting Factor Anymore

The original IDEAS-001 assumed 8K context windows. Current reality (January 2026):

| Model | Context Window | Notes |
|-------|----------------|-------|
| **Claude Opus 4.5** | 200K tokens | ~150K words, ~500 pages |
| **Claude Sonnet 4** | 200K tokens | Same as Opus |
| **GPT-4.1** | 1M tokens | ~750K words |
| **GPT-5/5.1/5.2** | 1M+ tokens | Latest OpenAI models |
| **Gemini 2.5 Flash/Pro** | 1M tokens | Can handle 50K lines of code |
| **Gemini 3 Pro/Flash** | 1M+ tokens | Latest Google models |

**Reality Check:** Our entire specs2 folder is probably <50K tokens. Context overflow is **not** the real problem.

#### Problem B: The Real Problems

1. **Attention Dilution** — Large context doesn't mean perfect recall. Models lose focus when given too much information. Research shows accuracy drops for "needle-in-haystack" retrieval when there are multiple "needles."

2. **Cost** — More tokens = more money. Reading 50K tokens for a simple change is wasteful.

3. **Latency** — Larger prompts take longer to process.

4. **Relevance Filtering** — The LLM doesn't know which 5% of 100 files is relevant to the current task.

5. **Maintenance Burden** — Duplicate "LLM-optimized" docs drift from "human docs."

6. **Tool Limitations** — GitHub Copilot, Azure DevOps, and IDE integrations have their own context limits that are often smaller than raw API limits.

#### Problem C: Tool-Specific Constraints

| Tool | Effective Context | Notes |
|------|-------------------|-------|
| **GitHub Copilot Chat** | ~32K-64K | Uses RAG, not full context |
| **GitHub Copilot Agent Mode** | Varies | Reads files on demand |
| **Azure DevOps Copilot** | Limited | Focused on work items |
| **VS Code Inline Suggestions** | ~8K | Very limited window |
| **Claude API Direct** | 200K | Full context available |
| **Gemini API Direct** | 1M | Full context available |

**Key Insight:** The bottleneck is often the **tooling integration**, not the model itself.

---

## 2. Requirements for Solution

Based on your constraints:

1. **Single source of truth** — One document serves both LLM and human readers
2. **GitHub/Azure DevOps compatible** — Works with existing tooling
3. **GitHub Copilot optimized** — Works well with Copilot's context gathering
4. **No duplicate maintenance** — Changes in one place propagate everywhere

---

## 3. Proposed Solutions

### Solution 1: Structured Document Sections (Recommended)

**Concept:** Use document structure to let tools extract what they need.

```markdown
# 250.001 IControlObject Specification

<!-- LLM-CONTEXT-START -->
## Quick Reference

```csharp
public interface IControlObject
{
    Locator Locator { get; }
    IElementScope Scope { get; }
    IPageObject? Page { get; }
    bool IsExists();
    bool? IsVisible();
    bool? IsEnabled();
    // ... full interface
}
```

**Key Rules:**
- IsVisible/IsEnabled return null when element doesn't exist
- All Wait methods return bool (success/failure)
- All Assert methods throw AssertionException on failure
- Nullable expected parameter = skip operation
<!-- LLM-CONTEXT-END -->

## Detailed Documentation

[Full human-readable documentation below...]
```

**How it works:**
- LLM tools can be instructed to read only between markers
- Humans read the full document
- Single file, single maintenance point
- GitHub Copilot's `.github/copilot-instructions.md` can instruct: "When reading spec files, prioritize content between `LLM-CONTEXT-START` and `LLM-CONTEXT-END` markers"

**Pros:**
- Single source of truth
- No tooling required
- Works with all current tools
- Progressive disclosure (brief → detailed)

**Cons:**
- Requires discipline to maintain markers
- Some duplication within document

---

### Solution 2: GitHub Copilot Custom Instructions + File Organization

**Concept:** Use Copilot's instruction system to guide context gathering.

#### 2a. Enhanced `.github/copilot-instructions.md`

```markdown
# Copilot Instructions for Brinell Framework

## Documentation Priority

When implementing features, read files in this order:
1. `specs2/250_specifications/250_INDEX.md` - Overview of all specs
2. Relevant `250_xxx_*.spx.md` file - Just the "Complete Interface Definition" section
3. `src/` existing implementations - For patterns

## Reading Specifications

In spec files (*.spx.md), focus on:
- Section 7: "Complete Interface Definition" - The actual code
- Section 3: "Boundary" - Edge cases and error handling
- Section 4: "Acceptance Criteria" - What tests should verify

Skip unless needed:
- Section 1: Overview (prose)
- Section 2: Behavior (verbose explanations)
- Section 5-6: Assumptions/Exclusions (context)
```

#### 2b. Index Files as Navigation

Create lightweight index files that Copilot reads first:

```markdown
# specs2/COPILOT-INDEX.md

## Foundation Interfaces (Level 0)

| Interface | File | Key Section |
|-----------|------|-------------|
| IControlObject | 250_001_IControlObject.spx.md#7-complete-interface-definition | Core control interface |
| IPageObject | 250_002_IPageObject.spx.md#7-complete-interface-definition | Page abstraction |
| IContainerControl | 250_003_IContainerControl.spx.md#5-complete-interface | Scoped element finding |

## When to Read Full Spec

- Implementing new control → Read full 250_001
- Fixing assertion bug → Read 250_001 Section 2.5 (Assert Methods)
- Adding wait logic → Read 250_001 Section 2.4 (Wait Methods)
```

**Pros:**
- Uses native Copilot features
- No document restructuring needed
- Instructions guide intelligent navigation

**Cons:**
- Copilot doesn't always follow instructions perfectly
- Index requires maintenance

---

### Solution 3: Collapsible/Foldable Documentation

**Concept:** Use HTML details/summary for progressive disclosure in Markdown.

```markdown
# IControlObject Specification

## Interface Definition

```csharp
public interface IControlObject { /* ... */ }
```

## Rules

1. IsVisible returns null if element doesn't exist
2. Wait methods never throw, Assert methods do

<details>
<summary>📖 Detailed Behavior Documentation</summary>

### State Methods

The interface provides state query methods that return immediately...

[500 lines of detailed explanation]

</details>

<details>
<summary>📋 Full Acceptance Criteria</summary>

### ACC-001: State Methods Return Correct Values
[Gherkin scenarios...]

</details>
```

**How it works:**
- Collapsed by default in GitHub/VS Code rendering
- LLM reads everything but the structure hints at importance
- Humans can expand sections as needed

**Pros:**
- Single document
- Works in GitHub, VS Code, Azure DevOps
- Natural progressive disclosure
- No custom markers needed

**Cons:**
- LLMs still read collapsed content
- May not work in all Markdown renderers

---

### Solution 4: Specification Layering with Includes

**Concept:** Use a build process to generate combined docs.

```
specs2/
├── src/                          # Source specs (human-readable)
│   ├── 250_001_IControlObject.md
│   └── ...
├── interfaces/                   # Extracted interfaces only
│   ├── IControlObject.cs         # Generated from specs
│   └── ...
├── generated/                    # Combined for LLM consumption
│   └── FULL-SPEC.md              # All interfaces + rules only
└── build-specs.ps1               # Extraction script
```

**Build script extracts:**
- All code blocks from spec files
- All "Rules" sections
- All "Errors" tables

**Pros:**
- True single source (in `src/`)
- Generated artifacts optimized for purpose
- Can integrate with CI/CD

**Cons:**
- Requires tooling
- Generated files can drift if build forgotten
- More complex workflow

---

### Solution 5: Code-First Specification

**Concept:** The code IS the specification. Documentation is commentary.

```csharp
// File: src/Brinell.Core/Interfaces/IControlObject.cs

namespace Brinell.Core.Interfaces;

/// <summary>
/// Base interface for all controls in the Brinell framework.
/// Every control implements this interface regardless of platform.
/// </summary>
/// <remarks>
/// ## Key Rules
/// - IsVisible/IsEnabled return null when element doesn't exist
/// - All Wait methods return bool (success/failure), never throw
/// - All Assert methods throw AssertionException on failure
/// - Nullable expected parameter = skip operation (return immediately)
/// 
/// ## Acceptance Criteria
/// - ACC-001: State methods return correct values for existing/missing elements
/// - ACC-002: Wait methods respect timeout parameter
/// - ACC-003: Assert methods throw with descriptive messages
/// </remarks>
public interface IControlObject
{
    /// <summary>
    /// The locator used to find this control in the UI tree.
    /// Set at construction and never changes.
    /// </summary>
    Locator Locator { get; }
    
    /// <summary>
    /// Check if the element exists in the UI tree.
    /// </summary>
    /// <returns>True if element exists, false otherwise. Never throws.</returns>
    bool IsExists();
    
    /// <summary>
    /// Check if the element is visible.
    /// </summary>
    /// <returns>
    /// True if visible, false if not visible, 
    /// NULL if element doesn't exist (not found in tree).
    /// </returns>
    bool? IsVisible();
    
    // ... rest of interface with XML docs
}
```

**Pros:**
- Code is always up-to-date
- LLMs read code naturally
- XML docs serve both IntelliSense and documentation
- Single source of truth (the code)
- Works perfectly with GitHub Copilot

**Cons:**
- Less room for detailed explanations
- Examples must live elsewhere
- Narrative documentation lost

---

### Solution 6: GitHub/Azure DevOps Wiki Integration

**Concept:** Use Wiki features for structured, tool-aware documentation.

**Azure DevOps Wiki:**
- Supports `.order` files to control navigation
- Supports Mermaid diagrams
- Has REST API for programmatic access
- Can be referenced from work items

**GitHub Wiki:**
- Separate git repo (can be automated)
- Supports custom sidebars
- Can link to code files

**Structure:**
```
wiki/
├── Home.md                       # Landing page
├── _Sidebar.md                   # Navigation
├── Quick-Reference/
│   ├── IControlObject.md         # Concise (for LLM/quick lookup)
│   └── ...
└── Detailed-Specs/
│   ├── IControlObject-Full.md    # Comprehensive (for humans)
│   └── ...
```

**Pros:**
- Native integration with Azure DevOps/GitHub
- Clear separation by audience
- Sidebar navigation

**Cons:**
- Two locations = potential drift
- Wiki separate from code repo

---

## 4. Recommended Approach: Hybrid 1 + 2 + 5

Combine the best of multiple solutions:

### Step 1: Code-First for Interfaces (Solution 5)

Make interface files in `src/` the authoritative spec:
- Rich XML documentation
- Rules in `<remarks>` blocks
- Acceptance criteria references

### Step 2: Structured Spec Files with Markers (Solution 1)

For complex behaviors that need prose:
```markdown
<!-- LLM-CONTEXT-START -->
## Quick Reference
[Interface + Rules + Key Behaviors]
<!-- LLM-CONTEXT-END -->

## Full Documentation
[Detailed explanations for humans]
```

### Step 3: Copilot Instructions (Solution 2)

Update `.github/copilot-instructions.md`:
```markdown
## Reading Brinell Specifications

### For Implementation Tasks
1. Read the interface file in `src/Brinell.Core/Interfaces/`
2. If behavior unclear, check corresponding `specs2/250_xxx_*.spx.md`
3. Focus on `LLM-CONTEXT` sections and "Complete Interface Definition"

### For Understanding Tasks
Read the full spec file including detailed documentation.
```

---

## 5. Implementation Checklist

### Phase 1: Enhance Copilot Instructions
- [ ] Update `.github/copilot-instructions.md` with documentation reading guidance
- [ ] Add section on spec file navigation
- [ ] Document marker conventions

### Phase 2: Add Markers to Existing Specs
- [ ] Add `LLM-CONTEXT-START/END` markers to 250_001 (pilot)
- [ ] Test with Copilot Chat
- [ ] Roll out to remaining specs

### Phase 3: Enrich Interface Code
- [ ] Add comprehensive XML docs to `IControlObject`
- [ ] Include rules and acceptance criteria references
- [ ] Validate IntelliSense shows useful info

### Phase 4: Create Navigation Index
- [ ] Create `specs2/COPILOT-INDEX.md`
- [ ] Link to key sections in each spec
- [ ] Test navigation with Copilot

---

## 6. Context Window Reference (January 2026)

For future reference, current model capabilities:

### Anthropic Claude
| Model | Context | Output | Notes |
|-------|---------|--------|-------|
| Claude Opus 4.5 | 200K | 32K | Best for complex reasoning |
| Claude Sonnet 4 | 200K | 64K | Balanced performance |
| Claude Haiku | 200K | 8K | Fast, cost-effective |

### OpenAI
| Model | Context | Notes |
|-------|---------|-------|
| GPT-5.2 | 1M+ | Latest flagship |
| GPT-5 | 1M | Previous flagship |
| GPT-4.1 | 1M | Smartest non-reasoning |
| GPT-4o | 128K | Fast, multimodal |

### Google Gemini
| Model | Context | Notes |
|-------|---------|-------|
| Gemini 3 Pro | 1M+ | Best multimodal |
| Gemini 3 Flash | 1M+ | Balanced |
| Gemini 2.5 Pro | 1M | Thinking model |
| Gemini 2.5 Flash | 1M | Price-performance |

### What 1M Tokens Means
- ~50,000 lines of code
- ~8 average novels
- ~200 podcast transcripts
- ~750,000 words

**Our entire specs2 folder is likely <20K tokens** — context overflow is not the real issue.

---

## 7. Key Insights

1. **Context size isn't the problem** — Modern models handle our entire spec collection easily.

2. **Attention quality is the problem** — Models perform worse with many "needles" in large haystacks.

3. **Tooling is the bottleneck** — GitHub Copilot and IDE integrations have smaller effective contexts.

4. **Structure helps everyone** — Clear document structure benefits humans AND LLMs.

5. **Code is the best spec** — For interfaces, well-documented code IS the specification.

6. **Instructions matter** — Copilot instructions can guide intelligent navigation without restructuring.

---

## 8. Questions to Resolve

1. **Should we measure actual token counts?**
   - Run tokenizer on specs2 folder
   - Establish baseline

2. **Which tool is the primary bottleneck?**
   - Test with Copilot Chat, Agent Mode, and API directly
   - Identify where context limits actually bite

3. **How much do Copilot Instructions help?**
   - A/B test implementation tasks with/without enhanced instructions

4. **Is code-first viable for all specs?**
   - Some specs describe behavior, not interfaces
   - Need separate strategy for those

---

## Related Documents

- [IDEAS-001: Documentation Context Overflow](IDEAS-001-Documentation-Context-Overflow.md) (Original problem statement)
- [.github/copilot-instructions.md](../.github/copilot-instructions.md) (Current Copilot config)
- [specs2/250_INDEX.md](../specs2/250_specifications/250_INDEX.md) (Specification index)
- [SPX v7 Documentation](../SPX/Docs/V7/) (Documentation format reference)

---

**Version:** 1.0  
**Status:** Draft  
**Last Review:** January 9, 2026
