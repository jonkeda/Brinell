# PROMPT: Minimize 203_Layers Documents

**Purpose:** Create minimized versions of the 203_Layers architecture documents  
**Output:** New files named `203_001b_CoreLayer.spx.md`, `203_002b_PlatformLayer.spx.md`, etc.

---

## Context

Read all documents in `specs2/200_architecture/203_Layers/`:
- 203_001_CoreLayer.spx.md
- 203_002_PlatformLayer.spx.md
- 203_003_TechnologyLayer.spx.md (if exists)

---

## Requirements

### 1. No Code or Class Names

❌ **Remove:**
```csharp
public interface ITextControlObject : IControlObject
{
    string? GetText();
    bool WaitTextEquals(string? expected, int? timeoutMs = null);
}
```

❌ **Remove:**
```csharp
public abstract class ControlBase : IControlObject { ... }
public class EntryControl : EditableTextControlBase { ... }
```

✅ **Keep:** Conceptual descriptions without code

### 2. Folder Structures — Folders Only with Comments

❌ **Remove file listings:**
```
Brinell.Core/
├── Interfaces/
│   ├── IControlObject.cs
│   ├── IClickableControlObject.cs
│   ├── ITextControlObject.cs
│   └── ...
├── Exceptions/
│   ├── ControlNotFoundException.cs
│   └── ...
```

✅ **Replace with folder + comment:**
```
Brinell.Core/
├── Interfaces/      # Control and page interface definitions
├── Exceptions/      # Framework exception types
├── Configuration/   # Configuration contracts
├── Logging/         # Logging abstractions and default implementation
├── Timeout/         # Timeout settings and wait utilities
└── Assertions/      # Assertion helper utilities
```

### 3. Remove These Sections

- **Interface Design Principles** — Belongs in 211_Modules or specifications
- **Control Implementation Pattern** — Belongs in 231_Patterns
- **Any code examples** — Belongs in source or specifications

### 4. Add Test Layers

Add new layer descriptions for:

**Unit Test Layer:**
- Package: `*.Tests` (e.g., `Brinell.Core.Tests`)
- Purpose: Test framework components in isolation with mocks
- Dependencies: Core, platform packages, test framework (xUnit/NUnit)

**Integration Test Layer:**
- Package: `*.IntegrationTests`
- Purpose: Test framework components with real drivers (but no UI)
- Dependencies: Platform packages, test framework, driver packages

**UI Test Layer:**
- Package: `*.UITests` (e.g., `Brinell.Samples.Maui.UITests`)
- Purpose: Test applications using Brinell framework
- Dependencies: Platform packages, test framework, sample applications

### 5. Keep These Sections (Simplified)

- **Overview** — What is this layer?
- **Purpose** — Why does it exist? (2-3 sentences max)
- **Contents** — Folder structure with comments only
- **Dependencies** — What it depends on
- **Dependents** — What depends on it
- **Design Rules** — Keep but remove code examples
- **Validation Rules** — Keep as checklist

---

## Output Format

Each minimized document should follow this structure:

```markdown
# 203.00X [Layer Name]

**Block Type:** LYR (Layer)  
**Edition:** 🟢Ⅰ Lite

---

## Overview

[2-3 sentences describing the layer]

## Purpose

[Why this layer exists - 2-3 bullet points]

## Contents

```
Package.Name/
├── Folder1/    # Description of what goes here
├── Folder2/    # Description of what goes here
└── Folder3/    # Description of what goes here
```

## Dependencies

- [Package it depends on]
- [Package it depends on]

## Dependents

- [What depends on this layer]

## Design Rules

1. [Rule without code example]
2. [Rule without code example]

## Validation

- [ ] Validation check 1
- [ ] Validation check 2

---

## Related Documents

- [Links to related docs]
```

---

## Layer Order

Create/update documents for all layers in dependency order:

1. **203_001b_CoreLayer** — Abstractions, no dependencies
2. **203_002b_PlatformLayer** — Platform implementations (MAUI, Blazor, WPF)
3. **203_003b_TechnologyLayer** — Technology adapters (if applicable)
4. **203_004b_UnitTestLayer** — Unit tests with mocks
5. **203_005b_IntegrationTestLayer** — Integration tests with drivers
6. **203_006b_UITestLayer** — UI tests against applications

---

## Target Size

Each document should be **under 80 lines** (excluding blank lines and Related Documents).

---

## Example: Minimized Core Layer

```markdown
# 203.001 Core Layer

**Block Type:** LYR (Layer)  
**Edition:** 🟢Ⅰ Lite

---

## Overview

The Core layer contains platform-agnostic abstractions and cross-cutting concerns. It has zero external dependencies.

## Purpose

- Define contracts (interfaces) for all platform implementations
- Provide cross-cutting utilities (logging, timeout, retry)
- Ensure compile-time safety through interface-based design

## Contents

```
Brinell.Core/
├── Interfaces/      # Control and page interface definitions
├── Exceptions/      # Framework exception types
├── Configuration/   # Configuration contracts
├── Logging/         # Logging abstractions and default implementation
├── Timeout/         # Timeout settings and wait utilities
├── Retry/           # Retry policies and execution
└── Assertions/      # Assertion helper utilities
```

## Dependencies

- None (pure .NET)

## Dependents

- All platform packages (Brinell.Maui, Brinell.Blazor, Brinell.Wpf)
- All test projects

## Design Rules

1. No references to automation libraries (Appium, Selenium, etc.)
2. No platform-specific types
3. Target .NET Standard 2.0 for maximum compatibility
4. Interfaces are stable — breaking changes require major version bump

## Validation

- [ ] No external package dependencies
- [ ] Compiles against .NET Standard 2.0
- [ ] No platform-specific code

---

## Related Documents

- [ADR-001 Clean Architecture](../202_Decisions/202_001_CleanArchitecture.spx.md)
- [Platform Layer](203_002b_PlatformLayer.spx.md)
```

---

## Execution

1. Read each existing 203_* document
2. Apply the rules above to create minimized version
3. Save as 203_00Xb_[Name].spx.md
4. Verify each is under 80 lines
5. Add the new test layer documents (004b, 005b, 006b)
