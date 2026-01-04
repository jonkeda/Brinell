# PLAN: SPEC-006b Proof-of-Concept Implementation

**Version:** 1.0  
**Status:** Draft  
**Date:** January 4, 2026  
**Type:** Vertical Slice / Proof of Concept  
**Parent Plan:** [PLAN-SPEC-006-IMPLEMENTATION](PLAN-SPEC-006-IMPLEMENTATION.md)

---

## 1. Overview

This plan implements a **minimal vertical slice** of SPEC-006 by adding to **existing projects** with new folders suffixed with "6". We implement only **2 controls** (Button + TextInput) to validate the architecture.

### Key Differences from PLAN-SPEC-006-POC

| Aspect | Original POC | This POC (006b) |
|--------|--------------|-----------------|
| Projects | New projects in `src/v6/` | Existing projects with `*6/` folders |
| Exceptions | New exception types | Reuse existing `Brinell.Core.Exceptions` |
| Namespace | `Brinell.ControlObject.*` | `Brinell.*.ControlObject6` |

### Selected Controls

| Control | Interface | Rationale |
|---------|-----------|-----------|
| **Button** | `IClickableControlObject` | Most basic interactive control |
| **TextInput** | `ITextControlObject` | Most common input control |

### Existing Exceptions to Reuse

| SPEC-006 Exception | Existing Exception | Location |
|--------------------|--------------------|----------|
| `ControlNotFoundException` | `ElementNotFoundException` | `Brinell.Core.Exceptions` |
| `ControlTimeoutException` | `UITestTimeoutException` | `Brinell.Core.Exceptions` |
| `ControlAssertionException` | `AssertionException` | `Brinell.Core.Exceptions` |

---

## 2. Folder Structure (Added to Existing Projects)

### Project Considerations

| Platform | Existing Project | POC Approach |
|----------|------------------|--------------|
| MAUI | `Brinell.Maui` | Add `ControlObject6/` folder |
| Blazor | **None** (new) | Create `Brinell.Blazor` project |

**Why Blazor needs its own project:**
- Blazor has a specific component model (not just HTML)
- Blazor uses SignalR for server-side, WebAssembly for client-side
- Blazor components have different automation patterns than raw HTML
- `Brinell.Html.Playwright` is for generic HTML/Playwright testing
- Separation allows Blazor-specific optimizations

```
src/
├── Brinell.Core/
│   ├── ... (existing)
│   └── ControlObject6/           ← NEW
│       ├── Locators/
│       │   ├── By.cs
│       │   ├── ControlLocator.cs
│       │   └── LocatorStrategy.cs
│       └── Interfaces/
│           ├── IControlObject.cs
│           ├── IInteractiveControlObject.cs
│           ├── IClickableControlObject.cs
│           ├── IFocusableControlObject.cs
│           ├── ITextControlObject.cs
│           ├── IPageObject.cs
│           └── ITestContext.cs
├── Brinell.Maui/
│   ├── ... (existing)
│   └── ControlObject6/           ← NEW
│       ├── Context/
│       │   └── MauiTestContext.cs
│       ├── Controls/
│       │   ├── ControlObjectBase.cs
│       │   ├── ButtonControl.cs
│       │   └── EntryControl.cs
│       └── Pages/
│           └── PageObjectBase.cs
└── Brinell.Blazor/               ← NEW PROJECT
    ├── Brinell.Blazor.csproj
    └── ControlObject6/
        ├── Context/
        │   └── BlazorTestContext.cs
        ├── Controls/
        │   ├── AsyncControlObjectBase.cs
        │   ├── ButtonControl.cs
        │   └── InputControl.cs
        └── Pages/
            └── AsyncPageObjectBase.cs
```

### Namespace Convention

```csharp
// Core interfaces and locators
namespace Brinell.Core.ControlObject6;
namespace Brinell.Core.ControlObject6.Locators;
namespace Brinell.Core.ControlObject6.Interfaces;

// MAUI implementation
namespace Brinell.Maui.ControlObject6;
namespace Brinell.Maui.ControlObject6.Controls;
namespace Brinell.Maui.ControlObject6.Pages;

// Blazor implementation (NEW PROJECT)
namespace Brinell.Blazor.ControlObject6;
namespace Brinell.Blazor.ControlObject6.Controls;
namespace Brinell.Blazor.ControlObject6.Pages;

// Reuse existing exceptions
using Brinell.Core.Exceptions;  // ElementNotFoundException, UITestTimeoutException, AssertionException
```

---

## 3. Implementation Phases

### Phase 1: Locator System

**Goal:** Add locator classes to Brinell.Core

**Project:** `Brinell.Core`  
**Folder:** `ControlObject6/Locators/`

**Deliverables:**

| File | Reference |
|------|-----------|
| `LocatorStrategy.cs` | [SPEC-006-001 §1](../specs/SPEC-006-001-INTERFACES.md) |
| `ControlLocator.cs` | [SPEC-006-001 §1](../specs/SPEC-006-001-INTERFACES.md) |
| `By.cs` | [SPEC-006-001 §1](../specs/SPEC-006-001-INTERFACES.md) |

**Validation:**
- `By.AutomationId("test")` creates correct locator
- `By.Css(".btn")` creates CSS locator
- Implicit string conversion: `"myId"` → `By.AutomationId("myId")`
- Chained: `By.AutomationId("form").Then(By.Name("submit"))`

---

### Phase 2: Foundation Interfaces

**Goal:** Add base interface hierarchy to Brinell.Core

**Project:** `Brinell.Core`  
**Folder:** `ControlObject6/Interfaces/`

**Deliverables:**

| File | Reference |
|------|-----------|
| `IControlObject.cs` | [SPEC-006-001 §2](../specs/SPEC-006-001-INTERFACES.md) |
| `IInteractiveControlObject.cs` | [SPEC-006-001 §2](../specs/SPEC-006-001-INTERFACES.md) |
| `IFocusableControlObject.cs` | [SPEC-006-001 §2](../specs/SPEC-006-001-INTERFACES.md) |

**Key Interface Properties:**
- `ControlLocator Locator { get; }` (not `string AutomationId`)
- All Wait/Check/Assert methods have nullable expected: `bool?`, `string?`, etc.
- `int? timeoutMs = null` always last parameter

---

### Phase 3: Target Control Interfaces

**Goal:** Add Button and TextInput interfaces

**Project:** `Brinell.Core`  
**Folder:** `ControlObject6/Interfaces/`

**Deliverables:**

| File | Reference |
|------|-----------|
| `IClickableControlObject.cs` | [SPEC-006-001 §3](../specs/SPEC-006-001-INTERFACES.md) |
| `ITextControlObject.cs` | [SPEC-006-001 §3](../specs/SPEC-006-001-INTERFACES.md) |

**Interface Hierarchy:**
```
IControlObject
├── IInteractiveControlObject
│   ├── IClickableControlObject  ← Button
│   └── IFocusableControlObject
│       └── ITextControlObject   ← TextInput
```

---

### Phase 4: Page & Context Interfaces

**Goal:** Add page and context interfaces

**Project:** `Brinell.Core`  
**Folder:** `ControlObject6/Interfaces/`

**Deliverables:**

| File | Reference |
|------|-----------|
| `IPageObject.cs` | [SPEC-006-001 §14](../specs/SPEC-006-001-INTERFACES.md) |
| `ITestContext.cs` | [SPEC-006-001 §15](../specs/SPEC-006-001-INTERFACES.md) |

**Key Methods:**
```csharp
// IPageObject
T GetControl<T>(ControlLocator locator, int? timeoutMs = null) where T : IControlObject;

// ITestContext
int DefaultTimeoutMs { get; set; }
T CreateControl<T>(ControlLocator locator) where T : IControlObject;
```

---

### Phase 5: MAUI Test Context

**Goal:** Add MAUI context implementation

**Project:** `Brinell.Maui`  
**Folder:** `ControlObject6/Context/`

**Deliverables:**

| File | Reference |
|------|-----------|
| `MauiTestContext.cs` | [SPEC-006-002-CLASSES-CONTEXT](../specs/SPEC-006-002-CLASSES-CONTEXT.md) |

**Key Implementation:**
- Holds `AppiumDriver` reference
- Implements `ITestContext`
- Creates controls via factory pattern

---

### Phase 6: MAUI Base Control

**Goal:** Implement base control with Is/Wait/Check/Assert pattern

**Project:** `Brinell.Maui`  
**Folder:** `ControlObject6/Controls/`

**Deliverables:**

| File | Reference |
|------|-----------|
| `ControlObjectBase.cs` | [SPEC-006-002-CLASSES-FOUNDATION](../specs/SPEC-006-002-CLASSES-FOUNDATION.md) |

**Key Implementation:**
- `FindElement()` - Translates `ControlLocator` to Appium locator
- Nullable expected handling: if `expected == null`, skip operation
- Uses existing `UITestTimeoutException` for timeouts
- Uses existing `ElementNotFoundException` for missing elements

---

### Phase 7: MAUI Button Control

**Goal:** Implement Button control

**Project:** `Brinell.Maui`  
**Folder:** `ControlObject6/Controls/`

**Deliverables:**

| File | Reference |
|------|-----------|
| `ButtonControl.cs` | [SPEC-006-002-CLASSES-INPUT](../specs/SPEC-006-002-CLASSES-INPUT.md) |

**Key Methods:**
```csharp
void Click(int? timeoutMs = null);
void DoubleClick(int? timeoutMs = null);
bool IsEnabled();
bool WaitEnabled(bool? expected, int? timeoutMs = null);
void AssertEnabled(bool? expected, string? message = null, int? timeoutMs = null);
```

---

### Phase 8: MAUI Entry Control

**Goal:** Implement Entry (text input) control

**Project:** `Brinell.Maui`  
**Folder:** `ControlObject6/Controls/`

**Deliverables:**

| File | Reference |
|------|-----------|
| `EntryControl.cs` | [SPEC-006-002-CLASSES-INPUT](../specs/SPEC-006-002-CLASSES-INPUT.md) |

**Key Methods:**
```csharp
void Enter(string? text, int? timeoutMs = null);
void Clear(int? timeoutMs = null);
void ClearAndEnter(string? text, int? timeoutMs = null);
bool IsReadOnly();
void AssertReadOnly(bool? expected, string? message = null, int? timeoutMs = null);
```

---

### Phase 9: MAUI Page Base

**Goal:** Implement base page object

**Project:** `Brinell.Maui`  
**Folder:** `ControlObject6/Pages/`

**Deliverables:**

| File | Reference |
|------|-----------|
| `PageObjectBase.cs` | [SPEC-006-002-CLASSES-FOUNDATION](../specs/SPEC-006-002-CLASSES-FOUNDATION.md) |

**Key Implementation:**
- `GetControl<T>(ControlLocator locator)` creates scoped controls
- Uses context for element finding
- Screenshot support via existing infrastructure

---

### Phase 10: Blazor Project Setup

**Goal:** Create new Brinell.Blazor project with Playwright dependency

**Project:** `Brinell.Blazor` (NEW)  
**Dependencies:** `Brinell.Core`, `Microsoft.Playwright`

**Deliverables:**

| File | Description |
|------|-------------|
| `Brinell.Blazor.csproj` | New project file |
| `BlazorTestContext.cs` | [SPEC-006-002-CLASSES-CONTEXT](../specs/SPEC-006-002-CLASSES-CONTEXT.md) |

**Project File:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Brinell.Core\Brinell.Core.csproj" />
    <PackageReference Include="Microsoft.Playwright" Version="*" />
  </ItemGroup>
</Project>
```

**Validation:**
- Project builds
- References Brinell.Core and Playwright

---

### Phase 11: Blazor Base Control (Async)

**Goal:** Implement async base control for Playwright

**Project:** `Brinell.Blazor`  
**Folder:** `ControlObject6/Controls/`

**Deliverables:**

| File | Reference |
|------|-----------|
| `AsyncControlObjectBase.cs` | [SPEC-006-002-CLASSES-FOUNDATION](../specs/SPEC-006-002-CLASSES-FOUNDATION.md) |

**Key Implementation:**
- All methods are `async Task` or `async Task<T>`
- Uses Playwright's `ILocator` 
- Translates `ControlLocator` to Playwright selector strings

---

### Phase 12: Blazor Button Control

**Goal:** Implement async Button control

**Project:** `Brinell.Blazor`  
**Folder:** `ControlObject6/Controls/`

**Deliverables:**

| File | Reference |
|------|-----------|
| `ButtonControl.cs` | [SPEC-006-002-CLASSES-INPUT](../specs/SPEC-006-002-CLASSES-INPUT.md) |

---

### Phase 13: Blazor Input Control

**Goal:** Implement async Input control

**Project:** `Brinell.Blazor`  
**Folder:** `ControlObject6/Controls/`

**Deliverables:**

| File | Reference |
|------|-----------|
| `InputControl.cs` | [SPEC-006-002-CLASSES-INPUT](../specs/SPEC-006-002-CLASSES-INPUT.md) |

---

### Phase 14: Blazor Page Base

**Goal:** Implement async base page object

**Project:** `Brinell.Blazor`  
**Folder:** `ControlObject6/Pages/`

**Deliverables:**

| File | Reference |
|------|-----------|
| `AsyncPageObjectBase.cs` | [SPEC-006-002-CLASSES-FOUNDATION](../specs/SPEC-006-002-CLASSES-FOUNDATION.md) |

---

### Phase 15: MAUI Integration Test

**Goal:** Test MAUI controls against sample app

**Project:** `Brinell.Samples.Maui.UITests`  
**Folder:** `ControlObject6Tests/`

**Deliverables:**

| File | Description |
|------|-------------|
| `MainPageTests.cs` | Test Button + Entry |

**Test Cases:**
```
1. Button_Click_Works
2. Button_AssertEnabled_Passes
3. Button_AssertEnabled_ThrowsWhenDisabled
4. Entry_Enter_SetsText
5. Entry_Clear_ClearsText
6. Entry_AssertText_Passes
7. Locator_ByAutomationId_Finds
8. NullableExpected_Skips
```

---

### Phase 16: Blazor Integration Test

**Goal:** Test Blazor controls against sample app

**Project:** `Brinell.Samples.Blazor.UITests`  
**Folder:** `ControlObject6Tests/`

**Deliverables:**

| File | Description |
|------|-------------|
| `HomePageTests.cs` | Test Button + Input |

**Test Cases:**
```
1. Button_ClickAsync_Works
2. Button_AssertEnabledAsync_Passes
3. Input_EnterAsync_SetsValue
4. Input_ClearAsync_Works
5. Locator_ByCss_Finds
6. Locator_ByDataAutomationId_Finds
```

---

## 4. Files Summary

### Brinell.Core (10 files)

```
ControlObject6/
├── Locators/
│   ├── By.cs
│   ├── ControlLocator.cs
│   └── LocatorStrategy.cs
└── Interfaces/
    ├── IControlObject.cs
    ├── IInteractiveControlObject.cs
    ├── IFocusableControlObject.cs
    ├── IClickableControlObject.cs
    ├── ITextControlObject.cs
    ├── IPageObject.cs
    └── ITestContext.cs
```

### Brinell.Maui (4 files)

```
ControlObject6/
├── Context/
│   └── MauiTestContext.cs
├── Controls/
│   ├── ControlObjectBase.cs
│   ├── ButtonControl.cs
│   └── EntryControl.cs
└── Pages/
    └── PageObjectBase.cs
```

### Brinell.Blazor (5 files) - NEW PROJECT

```
Brinell.Blazor.csproj
ControlObject6/
├── Context/
│   └── BlazorTestContext.cs
├── Controls/
│   ├── AsyncControlObjectBase.cs
│   ├── ButtonControl.cs
│   └── InputControl.cs
└── Pages/
    └── AsyncPageObjectBase.cs
```

### Test Projects (2 files)

```
Brinell.Samples.Maui.UITests/ControlObject6Tests/MainPageTests.cs
Brinell.Samples.Blazor.UITests/ControlObject6Tests/HomePageTests.cs
```

**Total: 21 files** (including new project file)

---

## 5. Exception Mapping

No new exceptions needed. Map SPEC-006 exceptions to existing:

| SPEC-006 Pattern | Existing Exception | Usage |
|------------------|--------------------|-------|
| Element not found | `ElementNotFoundException` | When locator finds nothing |
| Timeout waiting | `UITestTimeoutException` | When Wait/Check times out |
| Assertion failed | `AssertionException` | When Assert* fails |
| Check failed | `CheckFailedException` | When Check* fails |

**Import in each file:**
```csharp
using Brinell.Core.Exceptions;
```

---

## 6. Dependency Graph

```
Phase 1-4 (Core Interfaces)
    ↓
    ├── Phase 5-9 (MAUI Implementation)
    │       ↓
    │   Phase 15 (MAUI Tests)
    │
    └── Phase 10-14 (Blazor Implementation)
            ↓
        Phase 16 (Blazor Tests)
```

---

## 7. Estimated Timeline

| Phase | Effort | Cumulative |
|-------|--------|------------|
| 1-4 (Core) | 0.5 day | 0.5 day |
| 5-9 (MAUI) | 1.5 days | 2 days |
| 10-14 (Blazor) | 1.5 days | 3.5 days |
| 15-16 (Tests) | 0.5 day | 4 days |
| **Total** | **4 days** | |

---

## 8. Go/No-Go Decision

After POC completion, evaluate:

| Question | Go Criteria |
|----------|-------------|
| Locator system works? | Both platforms find elements |
| Interface hierarchy correct? | No inheritance issues |
| MAUI/Appium works? | Tests pass |
| Blazor/Playwright works? | Tests pass |
| Existing exceptions work? | Error messages are clear |
| Pattern is consistent? | Same patterns both platforms |
| Coexists with v1? | No conflicts with existing code |

**If GO:** Proceed with full [PLAN-SPEC-006-IMPLEMENTATION](PLAN-SPEC-006-IMPLEMENTATION.md) (adjust to use same folder pattern)  
**If NO-GO:** Document issues, revise approach

---

## 9. Cleanup Options

After full implementation, options:
1. **Keep "6" suffix** - Clear separation from v1
2. **Remove "6" suffix** - Rename to `ControlObject/` if v1 deprecated
3. **Move to separate project** - Extract to `Brinell.ControlObject.*` if needed

---

**End of POC Plan**
