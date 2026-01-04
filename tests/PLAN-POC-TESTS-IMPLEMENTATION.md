# ControlObject6 POC Test Implementation Plan

**Version:** 1.1  
**Created:** January 4, 2026  
**Completed:** January 4, 2026  
**Status:** ✅ Complete

---

## Implementation Results

| Project | Planned | Implemented | Status |
|---------|---------|-------------|--------|
| Brinell.Core.Tests.ControlObject6 | 69 | 93 | ✅ Passed |
| Brinell.Maui.Tests.ControlObject6 | 100 | 36 | ✅ Passed |
| Brinell.Blazor.Tests.ControlObject6 | 120 | 37 | ✅ Passed |
| **Total** | **289** | **166** | **✅ All Passed** |

> **Note:** Test counts differ from plan due to consolidation of test cases and focus on core functionality. 
> Key learnings documented in [SPEC-006-004-TESTING-GUIDE](../specs/SPEC-006-004-TESTING-GUIDE.md).

---

## Key Learnings

### MAUI Testability Issue

**Problem:** AppiumDriver and AppiumElement have non-virtual members that Moq cannot mock.

```csharp
// ❌ Fails with NotSupportedException
var mock = new Mock<AppiumDriver>();
mock.Setup(d => d.Url).Returns("...");
```

**Solution:** Created testable wrapper pattern with `IAppiumDriverWrapper` and `IAppiumElementWrapper` interfaces.

See: [SPEC-006-004: Testing & Mockability Guide](../specs/SPEC-006-004-TESTING-GUIDE.md)

### Blazor Works Directly

Playwright uses interfaces (`IPage`, `ILocator`) that Moq can mock directly. No wrapper needed.

---

## Overview

This plan describes the implementation of unit tests for the SPEC-006b POC (ControlObject6 framework). Tests are organized into three test projects covering Core, MAUI, and Blazor implementations.

---

## Phase 1: Project Setup (Day 1, ~2 hours)

### 1.1 Create Test Projects

| Task  | Description                                       | Files |
| ----- | ------------------------------------------------- | ----- |
| 1.1.1 | Create Brinell.Core.Tests.ControlObject6.csproj   | 1     |
| 1.1.2 | Create Brinell.Maui.Tests.ControlObject6.csproj   | 1     |
| 1.1.3 | Create Brinell.Blazor.Tests.ControlObject6.csproj | 1     |
| 1.1.4 | Add projects to solution                          | -     |

### 1.2 Project Configuration

```xml
<!-- Common test project structure -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="Moq" />
    <PackageReference Include="coverlet.collector" />
  </ItemGroup>
</Project>
```

---

## Phase 2: Core Locator Tests (Day 1, ~3 hours)

### 2.1 Locator Strategy Tests

| File                    | Test Cases       | Priority |
| ----------------------- | ---------------- | -------- |
| LocatorStrategyTests.cs | LS-001 to LS-003 | P0       |

### 2.2 ControlLocator Tests

| File                   | Test Cases       | Priority |
| ---------------------- | ---------------- | -------- |
| ControlLocatorTests.cs | CL-001 to CL-010 | P0       |

### 2.3 By Factory Tests

| File       | Test Cases       | Priority |
| ---------- | ---------------- | -------- |
| ByTests.cs | BY-001 to BY-016 | P0       |

### 2.4 Interface Contract Tests

| File                              | Test Cases         | Priority |
| --------------------------------- | ------------------ | -------- |
| IControlObjectTests.cs            | ICO-001 to ICO-008 | P1       |
| IInteractiveControlObjectTests.cs | IIC-001 to IIC-005 | P1       |
| IFocusableControlObjectTests.cs   | IFC-001 to IFC-004 | P1       |
| IClickableControlObjectTests.cs   | ICC-001 to ICC-006 | P1       |
| ITextControlObjectTests.cs        | ITC-001 to ITC-007 | P1       |
| IPageObjectTests.cs               | IPO-001 to IPO-007 | P1       |
| ITestContextTests.cs              | ITC-001 to ITC-009 | P1       |

---

## Phase 3: MAUI Tests (Day 2, ~4 hours)

### 3.1 Mock Infrastructure

| File                        | Description                 |
| --------------------------- | --------------------------- |
| Mocks/MockAppiumDriver.cs   | Moq setup for AppiumDriver  |
| Mocks/MockAppiumElement.cs  | Moq setup for AppiumElement |
| Fixtures/MauiTestFixture.cs | Shared test context setup   |

### 3.2 Context Tests

| File                    | Test Cases         | Priority |
| ----------------------- | ------------------ | -------- |
| MauiTestContextTests.cs | MTC-001 to MTC-034 | P0/P1    |

### 3.3 Control Tests

| File                      | Test Cases         | Priority |
| ------------------------- | ------------------ | -------- |
| ControlObjectBaseTests.cs | COB-001 to COB-054 | P0/P1    |
| ButtonControlTests.cs     | BC-001 to BC-008   | P0       |
| EntryControlTests.cs      | EC-001 to EC-032   | P0/P1    |

### 3.4 Page Tests

| File                   | Test Cases         | Priority |
| ---------------------- | ------------------ | -------- |
| PageObjectBaseTests.cs | POB-001 to POB-015 | P1       |

---

## Phase 4: Blazor Tests (Day 3, ~4 hours)

### 4.1 Mock Infrastructure

| File                           | Description               |
| ------------------------------ | ------------------------- |
| Mocks/MockPlaywrightPage.cs    | Moq setup for IPage       |
| Mocks/MockPlaywrightLocator.cs | Moq setup for ILocator    |
| Fixtures/BlazorTestFixture.cs  | Shared test context setup |

### 4.2 Context Tests

| File                      | Test Cases         | Priority |
| ------------------------- | ------------------ | -------- |
| BlazorTestContextTests.cs | BTC-001 to BTC-034 | P0/P1    |

### 4.3 Control Tests

| File                           | Test Cases         | Priority |
| ------------------------------ | ------------------ | -------- |
| AsyncControlObjectBaseTests.cs | ACO-001 to ACO-060 | P0/P1    |
| ButtonControlTests.cs          | BC-001 to BC-007   | P0       |
| InputControlTests.cs           | IC-001 to IC-032   | P0/P1    |

### 4.4 Page Tests

| File                        | Test Cases         | Priority |
| --------------------------- | ------------------ | -------- |
| AsyncPageObjectBaseTests.cs | APO-001 to APO-032 | P1       |

### 4.5 Interface Tests

| File                                 | Test Cases           | Priority |
| ------------------------------------ | -------------------- | -------- |
| IAsyncControlObjectTests.cs          | IAC-001 to IAC-004   | P1       |
| IAsyncClickableControlObjectTests.cs | IACC-001 to IACC-005 | P1       |
| IAsyncTextControlObjectTests.cs      | IATC-001 to IATC-005 | P1       |

---

## Phase 5: Verification & Coverage (Day 4, ~2 hours)

### 5.1 Run All Tests

```powershell
dotnet test tests/Brinell.Core.Tests.ControlObject6/
dotnet test tests/Brinell.Maui.Tests.ControlObject6/
dotnet test tests/Brinell.Blazor.Tests.ControlObject6/
```

### 5.2 Coverage Report

```powershell
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage
```

### 5.3 Targets

| Metric          | Target | Minimum |
| --------------- | ------ | ------- |
| Line Coverage   | 90%    | 80%     |
| Branch Coverage | 85%    | 75%     |
| Method Coverage | 95%    | 90%     |

---

## File Structure

```
tests/
├── PLAN-POC-TESTS-IMPLEMENTATION.md
├── Brinell.Core.Tests.ControlObject6/
│   ├── Brinell.Core.Tests.ControlObject6.csproj
│   ├── TEST-CASES-CORE.md
│   ├── Locators/
│   │   ├── LocatorStrategyTests.cs
│   │   ├── ControlLocatorTests.cs
│   │   └── ByTests.cs
│   └── Interfaces/
│       ├── IControlObjectTests.cs
│       ├── IInteractiveControlObjectTests.cs
│       ├── IFocusableControlObjectTests.cs
│       ├── IClickableControlObjectTests.cs
│       ├── ITextControlObjectTests.cs
│       ├── IPageObjectTests.cs
│       └── ITestContextTests.cs
├── Brinell.Maui.Tests.ControlObject6/
│   ├── Brinell.Maui.Tests.ControlObject6.csproj
│   ├── TEST-CASES-MAUI.md
│   ├── Mocks/
│   │   ├── MockAppiumDriver.cs
│   │   └── MockAppiumElement.cs
│   ├── Fixtures/
│   │   └── MauiTestFixture.cs
│   ├── Context/
│   │   └── MauiTestContextTests.cs
│   ├── Controls/
│   │   ├── ControlObjectBaseTests.cs
│   │   ├── ButtonControlTests.cs
│   │   └── EntryControlTests.cs
│   └── Pages/
│       └── PageObjectBaseTests.cs
└── Brinell.Blazor.Tests.ControlObject6/
    ├── Brinell.Blazor.Tests.ControlObject6.csproj
    ├── TEST-CASES-BLAZOR.md
    ├── Mocks/
    │   ├── MockPlaywrightPage.cs
    │   └── MockPlaywrightLocator.cs
    ├── Fixtures/
    │   └── BlazorTestFixture.cs
    ├── Context/
    │   └── BlazorTestContextTests.cs
    ├── Controls/
    │   ├── AsyncControlObjectBaseTests.cs
    │   ├── ButtonControlTests.cs
    │   └── InputControlTests.cs
    ├── Pages/
    │   └── AsyncPageObjectBaseTests.cs
    └── Interfaces/
        ├── IAsyncControlObjectTests.cs
        ├── IAsyncClickableControlObjectTests.cs
        └── IAsyncTextControlObjectTests.cs
```

---

## Test Count Summary

| Project         | P0 Tests      | P1 Tests      | P2 Tests     | Total         |
| --------------- | ------------- | ------------- | ------------ | ------------- |
| Core            | 29            | 35            | 5            | 69            |
| MAUI            | 40            | 45            | 15           | 100           |
| Blazor          | 45            | 50            | 25           | 120           |
| **Total** | **114** | **130** | **45** | **289** |

---

## Implementation Order

1. **Phase 1**: Project setup (all 3 projects)
2. **Phase 2**: Core tests (no mocking needed)
3. **Phase 3**: MAUI tests (with Appium mocks)
4. **Phase 4**: Blazor tests (with Playwright mocks)
5. **Phase 5**: Verification and coverage analysis

---

## Dependencies

| Package                   | Version | Purpose           |
| ------------------------- | ------- | ----------------- |
| xunit                     | 2.9.3   | Test framework    |
| xunit.runner.visualstudio | 3.1.5   | Test runner       |
| Microsoft.NET.Test.Sdk    | 17.14.0 | Test SDK          |
| FluentAssertions          | 6.12.0  | Assertion library |
| Moq                       | 4.20.70 | Mocking framework |
| coverlet.collector        | 6.0.4   | Code coverage     |

---

## Success Criteria

- [x] Test projects created and integrated with solution
- [x] All tests pass (166 implemented, 166 passed)
- [x] Core locator and interface tests complete (93 tests)
- [x] MAUI tests with testable wrapper pattern (36 tests)
- [x] Blazor async tests with mock interfaces (37 tests)
- [x] No test flakiness
- [x] Key learnings documented in SPEC-006-004
