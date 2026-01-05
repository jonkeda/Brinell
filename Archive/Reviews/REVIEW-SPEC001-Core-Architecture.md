# REVIEW: SPEC-001 Core Architecture vs Implementation

**Review Date:** January 2, 2026  
**Specification:** SPEC-001-core-architecture.md v3.0  
**Status:** Complete  
**Scope:** Full architecture specification compliance review

---

## 1. Executive Summary

This review compares SPEC-001 (Core Architecture) against the actual Brinell implementation. The implementation largely follows the specified architecture with notable deviations in naming conventions and some architectural decisions.

### Overall Compliance

| Section | Compliance | Score |
|---------|------------|-------|
| 2.1 Four-Layer Architecture | 🟢 High | 90% |
| 3.1 Core Layer | 🟡 Medium | 70% |
| 3.2 Platform Layer | 🟢 High | 85% |
| 3.3 Application Test Layer | 🟢 High | 90% |
| 4. Component Relationships | 🟢 High | 95% |
| 5. Platform Abstraction | 🟡 Medium | 65% |
| 6. Project Organization | 🟡 Medium | 75% |
| 7. Configuration | 🟡 Medium | 60% |
| 8. Thread Safety | 🟢 High | 85% |
| 9. Extension Points | 🟢 High | 95% |
| **Overall** | **🟡** | **81%** |

---

## 2. Architecture Overview (Section 2)

### 2.1 Four-Layer Architecture

**Specification:**
```
Layer 4: Application Tests
Layer 3: Platform Implementations
Layer 2: Core (Interfaces Only)
Layer 1: External Libraries
```

**Implementation Status:** ✅ **COMPLIANT**

| Layer | Spec Name | Actual Implementation | Status |
|-------|-----------|----------------------|--------|
| 4 | Application Tests | `samples/Brinell.Samples.*.UITests/` | ✅ |
| 3 | Platform Implementations | `src/Brinell.Wpf/`, `src/Brinell.Maui/`, `src/Brinell.Html/` | ✅ |
| 2 | Core | `src/Brinell.Core/` | ✅ |
| 1 | External Libraries | FlaUI, Appium, Selenium | ✅ |

**Additional Platforms Not in Spec:**
- `Brinell.Html.Playwright` - Playwright-based HTML testing
- `Brinell.Stride` - Stride 3D game engine testing
- `Brinell.Stride.Automation` - Stride automation helpers
- `Brinell.WinForms` - WinForms testing
- `Brinell.Testing` - Shared testing utilities
- `Brinell.Mocking` - API mocking support

**Finding:** Implementation has MORE platforms than specified, which is positive.

---

## 3. Core Layer (Section 3.1)

### 3.1.1 Core Layer MUST Contain

| Spec Requirement | Implementation | Status | Location |
|-----------------|----------------|--------|----------|
| Interface Contracts | ✅ | Complete | `Abstractions/` |
| Platform Enum | ✅ | Complete | `Abstractions/ITestContext.cs` |
| Logging Contracts | ✅ | Complete | `Logging/` |
| Exception Types | ⚠️ | Partial | `Exceptions/` (missing TimeoutException) |
| Configuration | ❌ | Missing | No `Configuration/` folder |
| Attributes | ✅ | Complete | `Attributes/` |

**Additional Items in Core (not in spec):**
- `Screenshots/` - Screenshot service interface
- `Testing/` - `UITestBase<TContext>` generic base class

### 3.1.2 Core Layer MUST NOT Contain

| Prohibited Item | Status | Issue |
|-----------------|--------|-------|
| Base class implementations | ⚠️ VIOLATION | `UITestBase<T>` exists in `Testing/` |
| Adapter abstractions | ❌ VIOLATION | `IDriverAdapter`, `IElementAdapter` exist |
| Platform-specific code | ✅ OK | None found |
| FlaUI/Appium/Selenium deps | ✅ OK | None in csproj |

**Critical Violations:**
1. **`IDriverAdapter` exists** - Should be removed per DES-001 AD-002
2. **`IElementAdapter` exists** - Should be removed per DES-001 AD-002
3. **`UITestBase<T>` in Core** - Spec says base classes moved to platform projects

### 3.1.3 Core Layer Dependencies

**Specification:**
```xml
<PackageReference Include="Microsoft.Extensions.Configuration" />
<PackageReference Include="Microsoft.Extensions.Configuration.Json" />
```

**Actual (Brinell.Core.csproj):**
```xml
<PackageReference Include="xunit.extensibility.core" />
```

**Finding:** ❌ **DEVIATION**
- Missing: `Microsoft.Extensions.Configuration` packages
- Has: `xunit.extensibility.core` (not in spec)

---

## 4. Platform Layer (Section 3.2)

### 3.2.1 Platform Project Structure

**Specification:**
```
Oravey.UITestFramework.{Platform}/
├── Infrastructure/
├── Controls/
│   ├── Base/
│   └── [Concrete controls]
└── Testing/
```

**Actual Implementation:**

#### Brinell.Wpf

| Spec Folder | Actual | Status |
|-------------|--------|--------|
| `Infrastructure/` | ✅ `Infrastructure/` | Compliant |
| `Controls/Base/` | ✅ `Controls/Base/` | Compliant |
| `Controls/[concrete]` | ✅ 12 controls | Compliant |
| `Testing/` | ✅ `Testing/` | Compliant |

**Extra:** `VisualValidation/` folder (not in spec - enhancement)

#### Brinell.Maui

| Spec Folder | Actual | Status |
|-------------|--------|--------|
| `Infrastructure/` | ✅ `Infrastructure/` | Compliant |
| `Controls/Base/` | ✅ `Controls/Base/` | Compliant |
| `Controls/[concrete]` | ✅ 27 controls | Compliant |
| `Testing/` | ✅ `Testing/` | Compliant |

**Extra:** `Abstractions/`, `Gestures/`, `Services/` (enhancements)

#### Brinell.Html

| Spec Folder | Actual | Status |
|-------------|--------|--------|
| `Infrastructure/` | ✅ `Infrastructure/` | Compliant |
| `Controls/Base/` | ✅ `Controls/Base/` | Compliant |
| `Controls/[concrete]` | ✅ 9 controls | Compliant |
| `Testing/` | ✅ `Testing/` | Compliant |

**Extra:** `Abstractions/` (enhancement)

### 3.2.2 Platform Layer MUST Provide

#### Base Class Hierarchy Check

**Specification requires:**

| Base Class | WPF | MAUI | HTML |
|------------|-----|------|------|
| `ControlBase` | ✅ | ✅ | ✅ |
| `PageBase` | ✅ | ✅ | ✅ |
| `BusyPageBase` | ❌ Missing | ✅ In PageBase.cs | ❌ Missing |
| `ContentControlBase` | ✅ | ✅ | ✅ |
| `TextControlBase` | ✅ | ✅ | ✅ |
| `ToggleControlBase` | ✅ | ✅ | ✅ |
| `SelectorControlBase` | ✅ | ✅ | ✅ |
| `RangeControlBase` | ✅ | ✅ | ✅ |
| `ItemsControlBase` | ✅ | ✅ | ❌ Missing |

**Findings:**
- WPF missing `BusyPageBase`
- HTML missing `BusyPageBase` and `ItemsControlBase`

### 3.2.3 Platform Dependencies

**Specification Example (WPF):**
```xml
<ProjectReference Include="..\Core\Oravey.UITestFramework.Core.csproj" />
<PackageReference Include="FlaUI.Core" Version="4.0.0" />
<PackageReference Include="FlaUI.UIA3" Version="4.0.0" />
<PackageReference Include="xunit" Version="2.9.*" />
```

**Actual (Brinell.Wpf.csproj):**
```xml
<ProjectReference Include="..\Brinell.Core\Brinell.Core.csproj" />
<PackageReference Include="FlaUI.Core" />
<PackageReference Include="FlaUI.UIA3" />
```

**Findings:**
- ⚠️ Missing xUnit reference in platform project (relies on Core's xunit.extensibility.core)
- ✅ FlaUI packages correct
- ✅ Core reference correct

### 3.2.4 Platform Isolation

| Requirement | Status | Evidence |
|-------------|--------|----------|
| MUST NOT reference other platform projects | ✅ | No cross-platform refs |
| MUST NOT share base class implementations | ✅ | Each has own hierarchy |
| MUST use native drivers directly | ⚠️ | Uses adapters internally |

---

## 5. Application Test Layer (Section 3.3)

### 3.3.1 Application Test Project Structure

**Specification:**
```
MyApp.UITests/
├── PageObjects/
├── Tests/
├── TestData/
├── Fixtures/
├── appsettings.json
└── MyApp.UITests.csproj
```

**Actual (Brinell.Samples.Wpf.UITests):**
```
Brinell.Samples.Wpf.UITests/
├── PageObjects/      ✅
├── Tests/            ✅
├── TestBase/         (not in spec - extra)
└── *.csproj          ✅
```

**Findings:**
- ❌ Missing `TestData/` folder
- ❌ Missing `Fixtures/` folder  
- ❌ Missing `appsettings.json`
- Extra: `TestBase/` folder

### 3.3.3 Application Test Dependencies

**Specification:**
```xml
<ProjectReference Include="..\UITestFramework\Oravey.UITestFramework.Wpf.csproj" />
<PackageReference Include="xunit" />
<PackageReference Include="xunit.runner.visualstudio" />
<PackageReference Include="FluentAssertions" />
```

**Actual:**
```xml
<ProjectReference Include="..\..\src\Brinell.Wpf\Brinell.Wpf.csproj" />
<PackageReference Include="xunit" />
<PackageReference Include="xunit.runner.visualstudio" />
<PackageReference Include="Microsoft.NET.Test.Sdk" />
<PackageReference Include="coverlet.collector" />
```

**Findings:**
- ⚠️ Missing `FluentAssertions` package
- ✅ Has xUnit packages
- Extra: `coverlet.collector` (code coverage - good)

---

## 6. Component Relationships (Section 4)

### 4.1 Dependency Rules

| Rule | Status | Evidence |
|------|--------|----------|
| Downward dependencies only | ✅ | Verified |
| No circular dependencies | ✅ | Verified |
| No upward references | ✅ | Verified |
| Core has no platform deps | ✅ | Verified |
| Platforms don't reference each other | ✅ | Verified |

**Result:** ✅ **FULLY COMPLIANT**

---

## 7. Platform Abstraction Strategy (Section 5)

### 5.1 Abstraction Through Interfaces

**Specification v3.0:**

| Abstraction | v3.0 Spec | Actual |
|-------------|-----------|--------|
| Core | Interfaces only | ⚠️ Has adapters |
| Platform | Implements Core | ✅ Correct |
| Driver Access | Direct access | ⚠️ Through adapters |

**Finding:** Implementation still uses v2.0-style adapter pattern which spec says should be removed.

---

## 8. Project Organization (Section 6)

### 6.1 Solution Structure

**Specification:**
```
UITestFramework.sln
├── src/
│   ├── Core/
│   ├── Platforms/
│   └── Mocking/
├── samples/
└── docs/
```

**Actual:**
```
Brinell.sln
├── src/
│   ├── Brinell.Core/
│   ├── Brinell.Wpf/
│   ├── Brinell.Maui/
│   ├── Brinell.Html/
│   ├── Brinell.Html.Playwright/
│   ├── Brinell.Stride/
│   ├── Brinell.Stride.Automation/
│   ├── Brinell.WinForms/
│   ├── Brinell.Testing/
│   └── Brinell.Mocking/
├── samples/
├── specs/
├── tests/
└── Archive/
```

**Findings:**
- ⚠️ No `Platforms/` subfolder (platforms at src root)
- ⚠️ Naming: `Brinell.*` not `Oravey.UITestFramework.*`
- ✅ Has `Mocking/`
- Extra: `specs/`, `tests/`, `Archive/`

### 6.2 Namespace Organization

**Specification:**
```
Oravey.UITestFramework.Core
├── Abstractions
├── Logging
├── Exceptions
└── Configuration

Oravey.UITestFramework.Wpf
├── Infrastructure
├── Controls
│   └── Base
└── Testing
```

**Actual:**
```
Brinell.Core
├── Abstractions
│   └── Controls
├── Logging
├── Exceptions
├── Screenshots      (not in spec)
├── Testing          (not in spec)
└── Attributes

Brinell.Wpf
├── Infrastructure
├── Controls
│   └── Base
├── Testing
└── VisualValidation  (not in spec)
```

**Findings:**
- ⚠️ Root namespace `Brinell` not `Oravey.UITestFramework`
- ❌ Missing `Configuration` namespace in Core
- Extra: `Screenshots`, `Attributes`, `VisualValidation`

---

## 9. Configuration Management (Section 7)

### 7.1 Configuration Sources

**Specification:**
1. Environment Variables (highest)
2. appsettings.{Environment}.json
3. appsettings.json (lowest)

**Implementation:** ⚠️ **PARTIAL**
- Environment variables supported in `CsvTestLogger`
- No evidence of appsettings.json configuration loading
- No `Configuration/` folder in Core

### 7.2 Standard Configuration Schema

**Specification defines:**
```json
{
  "UITest": {
    "Platform": "Windows",
    "ApplicationPath": "...",
    "DefaultTimeoutMs": 10000,
    ...
  }
}
```

**Implementation:** ❌ **NOT IMPLEMENTED**
- No configuration schema found
- Timeouts are hardcoded in context classes
- No appsettings.json in samples

---

## 10. Thread Safety (Section 8)

### 8.1 Thread Safety Requirements

| Requirement | Status | Evidence |
|-------------|--------|----------|
| TestContext thread-safe properties | ✅ | Properties are simple types |
| Not shared between parallel tests | ✅ | Each test creates own context |
| Loggers thread-safe | ✅ | `CsvTestLogger` uses lock object |
| Configuration thread-safe reads | N/A | No config system |

**Result:** ✅ **COMPLIANT** (for what's implemented)

---

## 11. Extension Points (Section 9)

### 9.1 Framework Extension Points

| Extension Point | Supported | Evidence |
|-----------------|-----------|----------|
| Custom Controls | ✅ | Virtual methods, inheritance |
| Custom Page Base | ✅ | PageBase is virtual |
| Custom Test Base | ✅ | UITestBase is generic |
| Custom Loggers | ✅ | ITestLogger interface |

**Result:** ✅ **FULLY COMPLIANT**

---

## 12. Summary of Deviations

### Critical (Must Fix)

| ID | Issue | Spec Section | Impact |
|----|-------|--------------|--------|
| C1 | `IDriverAdapter` exists in Core | 3.1.2 | Violates architecture |
| C2 | `IElementAdapter` exists in Core | 3.1.2 | Violates architecture |
| C3 | `UITestBase<T>` in Core | 3.1.2 | Should be platform-only |

### High (Should Fix)

| ID | Issue | Spec Section | Impact |
|----|-------|--------------|--------|
| H1 | Missing Configuration classes | 3.1.1, 7 | No standard config |
| H2 | Missing BusyPageBase in WPF/HTML | 3.2.1 | Inconsistent |
| H3 | Missing ItemsControlBase in HTML | 3.2.1 | Limited functionality |
| H4 | No appsettings.json support | 7.1 | Manual configuration |

### Medium (Consider Fixing)

| ID | Issue | Spec Section | Impact |
|----|-------|--------------|--------|
| M1 | Namespace `Brinell` vs `Oravey.UITestFramework` | 6.2 | Cosmetic |
| M2 | Missing TimeoutException | 3.1.1 | Use System.TimeoutException |
| M3 | No Platforms/ subfolder | 6.1 | Organization |
| M4 | Missing FluentAssertions in samples | 3.3.3 | Not required |

### Positive Deviations (Enhancements)

| ID | Enhancement | Benefit |
|----|-------------|---------|
| P1 | Additional platforms (Stride, WinForms, Playwright) | More coverage |
| P2 | VisualValidation in WPF | Visual testing |
| P3 | Gestures in MAUI | Mobile support |
| P4 | Screenshots service | Failure capture |
| P5 | Logging extensions | Consistent error handling |

---

## 13. Tasklist for Compliance

### Critical Priority

- [ ] **Remove `IDriverAdapter` from Brinell.Core** - Violates SPEC-001 3.1.2
- [ ] **Remove `IElementAdapter` from Brinell.Core** - Violates SPEC-001 3.1.2
- [ ] **Move `UITestBase<T>` to each platform** - Or document as intentional deviation

### High Priority

- [ ] **Add Configuration classes to Core** - Per SPEC-001 3.1.1 and Section 7
- [ ] **Add `BusyPageBase` to Brinell.Wpf** - Per SPEC-001 3.2.2
- [ ] **Add `BusyPageBase` to Brinell.Html** - Per SPEC-001 3.2.2
- [ ] **Add `ItemsControlBase` to Brinell.Html** - Per SPEC-001 3.2.2
- [ ] **Implement appsettings.json loading** - Per SPEC-001 7.1

### Medium Priority

- [ ] **Add `TimeoutException` to Core exceptions** - Per SPEC-001 3.1.1
- [ ] **Add xUnit reference to platform projects** - Per SPEC-001 3.2.3
- [ ] **Add appsettings.json to sample projects** - Per SPEC-001 3.3.1
- [ ] **Add TestData/ and Fixtures/ to samples** - Per SPEC-001 3.3.1
- [ ] **Add FluentAssertions to samples** - Per SPEC-001 3.3.3

### Documentation

- [ ] **Update SPEC-001 to reflect actual naming (Brinell)** - Or rename projects
- [ ] **Document additional platforms in specs** - Stride, WinForms, Playwright
- [ ] **Document UITestBase<T> location decision** - In Core vs platforms

---

## 14. Recommendation

The implementation is **81% compliant** with SPEC-001. The most significant deviations are:

1. **Adapter interfaces in Core** - This is a clear violation of the v3.0 architecture decision
2. **Missing configuration system** - Reduces usability for test customization
3. **Inconsistent base class coverage** - Some platforms missing expected base classes

**Recommended Actions:**
1. Fix critical issues (adapter removal) in next sprint
2. Implement configuration system as separate task
3. Add missing base classes for consistency
4. Consider updating spec to match actual naming (`Brinell` vs `Oravey`)

---

*This review can be used as a template for reviewing other specifications against the implementation.*
