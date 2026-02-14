# Prompt: Phase 1 Core Infrastructure Implementation

**Purpose:** Use this prompt to implement Phase 1 of the Brinell UI Test Automation Framework following specifications exactly.

---

## The Prompt

```
I need you to implement Phase 1 (Core Infrastructure) of the Brinell UI Test Automation Framework.

## Critical Instructions

1. **READ SPECIFICATIONS FIRST** - Before writing ANY code, you MUST read and understand:
   - specs2/250_specifications/250_INDEX.md (overview)
   - specs2/250_specifications/250_000_Foundation/250_001_IControlObject.spx.md
   - specs2/250_specifications/250_000_Foundation/250_002_IPageObject.spx.md
   - specs2/250_specifications/250_000_Foundation/250_003_IContainerControlObject.spx.md
   - specs2/250_specifications/250_000_Foundation/250_004_TestContext.spx.md
   - specs2/250_specifications/250_000_Foundation/250_005_InterfaceHierarchy.spx.md
   - specs2/250_specifications/250_000_Foundation/250_006_MauiBaseClasses.spx.md
   - specs2/250_specifications/250_000_Foundation/250_007_BlazorBaseClasses.spx.md
   - specs2/250_specifications/250_000_Foundation/250_009_PlatformContexts.spx.md

2. **FOLLOW SPECIFICATIONS EXACTLY** - The specifications define:
   - Exact interface signatures (method names, parameters, return types)
   - Nullable patterns (bool? for IsVisible, IsEnabled when element not found)
   - The IElementScope pattern for container scoping
   - Locator class usage (not string AutomationId)
   - The Scope property on IControlObject

3. **DO NOT SIMPLIFY** - Do not:
   - Replace `Locator` with `string automationId`
   - Change `bool?` returns to `bool`
   - Skip the `IElementScope` interface hierarchy
   - Omit text methods from IControlObject
   - Create stub implementations with NotImplementedException

## Output Location

All source code goes in: `srcnew/`

## Project Structure Required

```
srcnew/
├── Brinell.sln
├── Brinell.Core/
│   ├── Brinell.Core.csproj
│   ├── Interfaces/
│   │   ├── IControlObject.cs           # Per 250_001
│   │   ├── IPageObject.cs              # Per 250_002
│   │   ├── IElementScope.cs            # Per 250_004 section 2.3
│   │   ├── ITestContext.cs             # Per 250_004
│   │   ├── IClickableControlObject.cs  # Per 250_005
│   │   ├── ITextControlObject.cs       # Per 250_005
│   │   ├── IEditableTextControlObject.cs
│   │   ├── IToggleControlObject.cs
│   │   ├── IContainerControlObject.cs  # Per 250_003
│   │   └── [other capability interfaces per 250_005]
│   ├── Locators/
│   │   ├── Locator.cs
│   │   ├── LocatorStrategy.cs
│   │   └── By.cs (optional factory)
│   ├── Exceptions/
│   │   ├── BrinellException.cs
│   │   ├── ElementNotFoundException.cs
│   │   ├── AssertionException.cs
│   │   └── WaitTimeoutException.cs
│   ├── Configuration/
│   │   └── TimeoutSettings.cs          # Per 250_004 section 2.4
│   ├── Logging/
│   │   └── ITestLogger.cs              # Per 250_004 section 2.5
│   └── Utilities/
│       └── WaitHelper.cs               # Polling utility
├── Brinell.Maui/
│   ├── Brinell.Maui.csproj
│   ├── Context/
│   │   └── IMauiTestContext.cs         # Per 250_009
│   └── Base/
│       ├── MauiControlBase.cs          # Per 250_006
│       ├── MauiClickableControlBase.cs
│       ├── MauiTextControlBase.cs
│       └── [other base classes per 250_006]
├── Brinell.Blazor/
│   ├── Brinell.Blazor.csproj
│   ├── Context/
│   │   └── IBlazorTestContext.cs       # Per 250_009
│   └── Base/
│       └── BlazorControlBase.cs        # Per 250_007
├── Brinell.Wpf/
│   └── [similar structure]
└── Brinell.Core.Tests/
    └── [unit tests]
```

## Key Patterns to Implement

### 1. IElementScope Pattern (CRITICAL)

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

```csharp
public interface IControlObject
{
    Locator Locator { get; }
    IElementScope Scope { get; }
    IPageObject? Page { get; }
    
    bool IsExists();
    bool? IsVisible();   // NULL if element doesn't exist
    bool? IsEnabled();   // NULL if element doesn't exist
    
    // Text methods ON BASE INTERFACE
    string? GetText(int? timeoutMs = null);
    bool WaitText(string? expected, int? timeoutMs = null);
    void AssertText(string? expected, string? message = null, int? timeoutMs = null);
    
    // ... wait and assert methods per spec
}
```

### 3. Generic Base Class Pattern

```csharp
public abstract class ControlBase<TElement, TScope> : IControlObject
    where TScope : IElementScope<TElement>
{
    protected readonly TScope _scope;
    protected readonly Locator _locator;
    
    protected TElement? TryFindElement() => _scope.TryFindElement(_locator);
}
```

## Verification Checklist

Before marking complete, verify:

- [ ] IElementScope and IElementScope<TElement> exist
- [ ] IControlObject uses Locator (not string)
- [ ] IControlObject has Scope property
- [ ] IsVisible() and IsEnabled() return bool? (nullable)
- [ ] IControlObject has GetText, WaitText, AssertText methods
- [ ] TimeoutSettings has PageLoad, ElementFind, Animation properties
- [ ] TimeoutSettings has Default, Fast, Slow static presets
- [ ] ITestLogger matches spec signature (with testName, pageName parameters)
- [ ] WaitHelper utility exists
- [ ] Platform base classes are IMPLEMENTED (not stubs)
- [ ] All builds without errors
- [ ] Unit tests pass

## Central Package Management

This repo uses Central Package Management. Do NOT put Version= in .csproj files.
Package versions are in: Directory.Packages.props (at repo root)

## What NOT To Do

- ❌ Do not use `string AutomationId` instead of `Locator`
- ❌ Do not make IsVisible/IsEnabled return bool (must be bool?)
- ❌ Do not skip IElementScope interfaces
- ❌ Do not put text methods only on ITextControlObject (they belong on IControlObject too)
- ❌ Do not create stub implementations with NotImplementedException
- ❌ Do not simplify signatures to "save time"
- ❌ Do not skip reading the specification files

## Proceed

1. First, read all specification files listed above
2. Create a todo list of components to implement
3. Implement each component following specs EXACTLY
4. Verify against checklist
5. Run build and tests
```

---

## Usage Notes

1. Copy the entire prompt above (between the ``` markers)
2. Paste into a new conversation
3. The AI should read specs first, then implement

## Why This Prompt Works

- **Explicit spec file list** - Forces reading before coding
- **Key patterns shown** - Prevents common mistakes
- **Verification checklist** - Clear success criteria
- **"What NOT to do"** - Prevents known simplification mistakes
- **No ambiguity** - Exact folder structure and interfaces defined

## Alternative: Incremental Approach

If the full prompt is too much, use these in sequence:

### Step 1: Foundation Interfaces
```
Read specs2/250_specifications/250_000_Foundation/250_001_IControlObject.spx.md and 250_004_TestContext.spx.md.

Implement ONLY:
- IElementScope and IElementScope<TElement>
- IControlObject (exactly as specified)
- ITestContext and ITestContext<TElement>

Follow the specifications exactly. Pay attention to:
- Locator property (not string)
- Scope property
- Nullable bool? returns for IsVisible/IsEnabled
- Text methods on IControlObject
```

### Step 2: Capability Interfaces
```
Read specs2/250_specifications/250_000_Foundation/250_005_InterfaceHierarchy.spx.md.

Implement all capability interfaces:
- IClickableControlObject
- ITextControlObject  
- IEditableTextControlObject
- IToggleControlObject
- ISelectorControlObject
- IRangeControlObject
- IContainerControlObject
- etc.

Follow the exact method signatures in the specification.
```

### Step 3: Platform Base Classes
```
Read specs2/250_specifications/250_000_Foundation/250_006_MauiBaseClasses.spx.md.

Implement the MAUI base class hierarchy with REAL implementations (not stubs):
- MauiControlBase
- MauiClickableControlBase
- MauiTextControlBase
- etc.

All methods must have working implementations using Appium.
```
