# Prompt: Phase 1 Core Infrastructure Implementation (v2)

**Purpose:** Use this prompt to implement Phase 1 of the Brinell UI Test Automation Framework.

**Version:** 2.0**Changes from v1:**

- Added architecture documents (200_*) as base reference
- Added guidance that specs are directional, not prescriptive
- Added requirement to document deviations in `explanation/` folder

---

## The Prompt

```
I need you to implement Phase 1 (Core Infrastructure) of the Brinell UI Test Automation Framework.

## Critical Instructions

1. **READ DOCUMENTATION FIRST** - Before writing ANY code, you MUST read and understand:

   ### Architecture Documents (Foundation - READ FIRST)
   - specs2/200_architecture/200_INDEX.md (architecture overview)
   - specs2/200_architecture/200_000_Overview.spx.md (main architecture)
   - specs2/200_architecture/202_Decisions/ (Architecture Decision Records - ADRs)
   - specs2/200_architecture/203_Layers/ (layer definitions)
   - specs2/200_architecture/231_Patterns/ (design patterns)
   
   ### Specifications (Implementation Details)
   - specs2/250_specifications/250_INDEX.md (spec overview)
   - specs2/250_specifications/250_000_Foundation/250_001_IControlObject.spx.md
   - specs2/250_specifications/250_000_Foundation/250_002_IPageObject.spx.md
   - specs2/250_specifications/250_000_Foundation/250_003_IContainerControlObject.spx.md
   - specs2/250_specifications/250_000_Foundation/250_004_TestContext.spx.md
   - specs2/250_specifications/250_000_Foundation/250_005_InterfaceHierarchy.spx.md
   - specs2/250_specifications/250_000_Foundation/250_006_MauiBaseClasses.spx.md
   - specs2/250_specifications/250_000_Foundation/250_007_BlazorBaseClasses.spx.md
   - specs2/250_specifications/250_000_Foundation/250_009_PlatformContexts.spx.md

2. **FOLLOW THE INTENT, NOT THE LETTER**
   
   The specifications are a GUIDE, not perfect documentation. They represent the intended design direction but may contain:
   - Inconsistencies between documents
   - Method signatures that don't quite work in practice
   - Missing details that require judgment calls
   - Overly complex patterns that can be simplified
   
   **Your job is to:**
   - Understand the INTENT behind each specification
   - Implement something that WORKS and follows the spirit of the design
   - Make practical decisions when specs are unclear or impractical
   - Document any significant deviations (see step 3)

3. **DOCUMENT DEVIATIONS**
   
   Create one or more markdown files in: `srcnew/explanation/`
   
   Document:
   - Where you deviated from specifications and WHY
   - Where you deviated from architecture and WHY
   - Design decisions you made that weren't covered by specs
   - Simplifications made for practical reasons
   - Any inconsistencies found in the specs
   
   Example file: `srcnew/explanation/DEVIATIONS-Phase1.md`

## Key Architectural Principles (from 200_architecture)

These are the NON-NEGOTIABLE principles from the architecture:

1. **Interface-First Design** - Define interfaces before implementations
2. **Platform Separation** - Core is platform-agnostic, platform packages handle specifics
3. **Clean Architecture** - Dependencies flow inward (implementations depend on abstractions)
4. **Control Interface Hierarchy** - Capability-based interface inheritance

## Key Design Patterns to Follow

1. **IElementScope Pattern** - Pages, Containers, and TestContext are element scopes
2. **Locator Pattern** - Use Locator class (not raw strings) for element identification
3. **Nullable State Pattern** - Return null when element state can't be determined
4. **Timeout Configuration** - Centralized timeout management with presets

## Output Location

All source code goes in: `srcnew/`

## Project Structure Required

```

srcnew/
├── Brinell.sln
├── explanation/                    # NEW: Deviation documentation
│   └── DEVIATIONS-Phase1.md        # Document your deviations here
├── Brinell.Core/
│   ├── Brinell.Core.csproj
│   ├── Interfaces/
│   │   ├── IControlObject.cs
│   │   ├── IPageObject.cs
│   │   ├── IElementScope.cs
│   │   ├── ITestContext.cs
│   │   ├── IClickableControlObject.cs
│   │   ├── ITextControlObject.cs
│   │   ├── IEditableTextControlObject.cs
│   │   ├── IToggleControlObject.cs
│   │   ├── IContainerControlObject.cs
│   │   └── [other capability interfaces]
│   ├── Locators/
│   │   ├── Locator.cs
│   │   └── LocatorStrategy.cs
│   ├── Exceptions/
│   │   ├── BrinellException.cs
│   │   ├── ElementNotFoundException.cs
│   │   ├── AssertionException.cs
│   │   └── WaitTimeoutException.cs
│   ├── Configuration/
│   │   └── TimeoutSettings.cs
│   ├── Logging/
│   │   └── ITestLogger.cs
│   └── Utilities/
│       └── WaitHelper.cs
├── Brinell.Maui/
│   ├── Brinell.Maui.csproj
│   ├── Interfaces/
│   │   └── IMauiTestContext.cs
│   ├── Controls/
│   │   ├── MauiControlBase.cs
│   │   ├── MauiClickableControl.cs
│   │   ├── MauiTextControl.cs
│   │   └── [other control classes]
│   └── Pages/
│       └── MauiPageBase.cs
├── Brinell.Blazor/
│   ├── Brinell.Blazor.csproj
│   ├── Interfaces/
│   │   └── IBlazorTestContext.cs
│   ├── Controls/
│   │   └── BlazorControlBase.cs
│   └── Pages/
│       └── BlazorPageBase.cs
└── Directory.Packages.props            # Central package management

```

## Essential Patterns to Implement

### 1. IElementScope Pattern

This is the foundation for container scoping. Pages, Containers, and TestContext all implement this.

```csharp
public interface IElementScope
{
    LocatorStrategy DefaultLocatorStrategy { get; }
}

public interface IElementScope<TElement> : IElementScope
{
    TElement? TryFindElement(Locator locator);
    TElement FindElement(Locator locator);
    IReadOnlyList<TElement> FindElements(Locator locator);
}
```

### 2. IControlObject with Locator and Scope

Every control has a Locator and knows its Scope. This enables container scoping.

```csharp
public interface IControlObject
{
    Locator Locator { get; }
    IElementScope Scope { get; }
    IPageObject? Page { get; }
  
    bool IsExists();
    bool? IsVisible();   // NULL if element doesn't exist
    bool? IsEnabled();   // NULL if element doesn't exist
  
    string? GetText();
    // ... other methods
}
```

### 3. Constructor Pattern

Controls take a scope and locator:

```csharp
public MauiControlBase(IElementScope<AppiumElement> scope, Locator locator)
{
    _scope = scope;
    _locator = locator;
}
```

## Practical Guidelines

### When specs are unclear:

- Choose the simpler approach
- Make it work first, optimize later
- Document your decision in `explanation/`

### When specs conflict:

- Use common sense for method signatures
- Document the conflict and your resolution

## Verification Checklist

Before marking complete, verify:

- [ ] IElementScope and IElementScope `<TElement>` exist
- [ ] IControlObject uses Locator (not string)
- [ ] IControlObject has Scope property
- [ ] IsVisible() and IsEnabled() return bool? (nullable)
- [ ] TimeoutSettings exists with presets
- [ ] ITestLogger interface exists
- [ ] WaitHelper utility exists
- [ ] Platform base classes are IMPLEMENTED (not stubs)
- [ ] All builds without errors
- [ ] Deviation documentation exists in srcnew/explanation/

## Central Package Management

This repo uses Central Package Management. Do NOT put Version= in .csproj files.
Package versions go in: Directory.Packages.props

## What To Avoid

- ❌ Do not use `string AutomationId` instead of `Locator` class
- ❌ Do not make IsVisible/IsEnabled return non-nullable bool
- ❌ Do not skip IElementScope interfaces
- ❌ Do not create stub implementations with NotImplementedException
- ❌ Do not blindly copy spec code that doesn't compile
- ❌ Do not skip the deviation documentation

## Proceed

1. First, read architecture documents (200_*)
2. Then read specification files (250_*)
3. Create a todo list of components to implement
4. Implement each component following the INTENT of specs
5. Create srcnew/explanation/DEVIATIONS-Phase1.md documenting:
   - Deviations from specs
   - Deviations from architecture
   - Design decisions made
   - Spec inconsistencies found
6. Verify against checklist
7. Run build and tests

```

---

## Usage Notes

1. Copy the entire prompt above (between the ``` markers)
2. Paste into a new conversation
3. The AI should read architecture + specs first, then implement

## Key Differences from v1

| Aspect | v1 | v2 |
|--------|----|----|
| **Documentation** | Specs only | Architecture + Specs |
| **Compliance** | Follow specs exactly | Follow intent, be practical |
| **Deviations** | Not allowed | Required to document |
| **Flexibility** | Rigid | Pragmatic |

## Why This Version Works Better

1. **Architecture context** - Understanding the WHY helps make better decisions
2. **Practical flexibility** - Real specs have gaps; this acknowledges that
3. **Documented decisions** - Creates institutional knowledge for future work
4. **Reduced frustration** - No need to implement impossible spec signatures

## Deviation Documentation Template

Create `srcnew/explanation/DEVIATIONS-Phase1.md` with this structure:

```markdown
# Phase 1 Implementation Deviations

**Date:** [Date]
**Implementer:** [AI/Human]

## Summary

Brief overview of implementation and key decisions made.

## Deviations from Specifications

### 1. [Topic]
- **Spec said:** [What the spec specified]
- **We did:** [What was actually implemented]
- **Reason:** [Why this deviation was necessary]

### 2. [Topic]
...

## Deviations from Architecture

### 1. [Topic]
- **Architecture said:** [What the architecture specified]
- **We did:** [What was actually implemented]
- **Reason:** [Why this deviation was necessary]

## Design Decisions (Not Covered by Specs)

### 1. [Decision]
- **Context:** [What situation required a decision]
- **Decision:** [What was decided]
- **Rationale:** [Why this decision was made]

## Spec Inconsistencies Found

### 1. [Inconsistency]
- **Documents:** [Which specs conflict]
- **Issue:** [What the inconsistency is]
- **Resolution:** [How it was resolved]

## Recommendations for Spec Updates

1. [Recommendation]
2. [Recommendation]
```

---

## Alternative: Incremental Approach

If the full prompt is overwhelming, use these phases:

### Phase 1a: Read & Plan

```
Read the architecture documents in specs2/200_architecture/ and the foundation 
specifications in specs2/250_specifications/250_000_Foundation/.

Create a plan document outlining:
1. Key interfaces to implement
2. Key patterns to follow
3. Anticipated challenges or spec gaps
4. Questions about unclear requirements

Do not write code yet - just create the plan.
```

### Phase 1b: Core Interfaces

```
Implement Brinell.Core interfaces:
- IElementScope, IControlObject, IPageObject, ITestContext
- Capability interfaces (IClickable, IText, etc.)
- Locator, TimeoutSettings, Exceptions

Follow spec intent. Document any deviations.
```

### Phase 1c: Platform Implementations

```
Implement Brinell.Maui and Brinell.Blazor:
- Base classes with real implementations
- Platform-specific test contexts
- Page object base classes

Document deviations in srcnew/explanation/
```
