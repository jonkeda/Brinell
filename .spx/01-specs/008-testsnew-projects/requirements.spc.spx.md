# Requirements Document: TestsNew Projects

## Introduction

This specification defines the creation of a `testsnew` folder containing unit tests and integration/UI tests for the projects in the `srcnew` folder. The test structure will mirror the srcnew projects and follow the established patterns from the existing tests folder.

### Purpose

- Establish comprehensive test coverage for the new solution structure
- Create unit test projects for testing framework code in isolation
- Create UI/integration test projects for end-to-end testing
- Maintain consistency with existing test conventions

### Value

- Validates framework implementations work correctly
- Catches regressions during development
- Documents expected behavior through test cases
- Enables CI/CD pipeline validation

## Alignment with Product Vision

This test structure supports the product goals from the steering documents:

- **Quality Assurance**: Comprehensive tests ensure framework reliability
- **Self-Contained Platforms**: Each platform has isolated test projects
- **NuGet Distribution**: Tests validate package behavior before publishing
- **Multi-Platform Support**: Platform-specific tests verify implementations

## Requirements

### REQ-001: Folder Structure

**User Story:** As a framework developer, I want organized test folders, so that I can easily find and run tests for specific projects.

#### Acceptance Criteria

1. WHEN the solution is opened THEN VS/Rider SHALL display test projects organized under `testsnew/`
2. WHEN running tests THEN unit tests and UI tests SHALL be executable independently

#### Folder Structure

```
testsnew/
├── Brinell.Core.Tests/           # Unit tests for Core
├── Brinell.Wpf.Tests/            # Unit tests for WPF
├── Brinell.Wpf.UITests/          # UI/Integration tests for WPF
├── Brinell.WinForms.Tests/       # Unit tests for WinForms
├── Brinell.WinForms.UITests/     # UI/Integration tests for WinForms
├── Brinell.Maui.Tests/           # Unit tests for MAUI
├── Brinell.Maui.UITests/         # UI/Integration tests for MAUI
├── Brinell.Blazor.Tests/         # Unit tests for Blazor
├── Brinell.Blazor.UITests/       # UI/Integration tests for Blazor
├── Brinell.Html.Tests/           # Unit tests for HTML/Playwright
├── Brinell.Html.UITests/         # UI/Integration tests for HTML
├── Brinell.Stride.Tests/         # Unit tests for Stride client
├── Brinell.Stride.UITests/       # UI/Integration tests for Stride
├── Brinell.Automation.Tests/     # Unit tests for Automation hooks
└── Brinell.Mocking.Tests/        # Unit tests for Mocking utilities
```

---

### REQ-002: Unit Test Projects (*.Tests)

**User Story:** As a framework developer, I want unit test projects, so that I can test framework code in isolation without running actual applications.

#### Acceptance Criteria

1. WHEN unit tests run THEN they SHALL NOT require external applications
2. WHEN unit tests run THEN they SHALL use mocking for external dependencies
3. WHEN unit tests run THEN they SHALL complete within seconds

#### Projects Requiring Unit Tests

| Project | Unit Test Project | Scope |
|---------|------------------|-------|
| Brinell.Core | Brinell.Core.Tests | Interfaces, locators, utilities |
| Brinell.Wpf | Brinell.Wpf.Tests | Context, control base classes |
| Brinell.WinForms | Brinell.WinForms.Tests | Context, control base classes |
| Brinell.Maui | Brinell.Maui.Tests | Context, control base classes, gestures |
| Brinell.Blazor | Brinell.Blazor.Tests | Context, control base classes |
| Brinell.Html | Brinell.Html.Tests | Context, control base classes |
| Brinell.Stride | Brinell.Stride.Tests | Communication, context |
| Brinell.Automation | Brinell.Automation.Tests | Server, handler logic |
| Brinell.Mocking | Brinell.Mocking.Tests | MockApiServer, ApiStubBuilder |

---

### REQ-003: UI/Integration Test Projects (*.UITests)

**User Story:** As a framework developer, I want UI test projects, so that I can validate controls work correctly against real applications.

#### Acceptance Criteria

1. WHEN UI tests run THEN they SHALL launch actual test applications
2. WHEN UI tests run THEN they SHALL interact with real UI elements
3. WHEN UI tests run THEN they SHALL validate real automation behavior

#### Projects Requiring UI Tests

| Project | UI Test Project | Requires |
|---------|----------------|----------|
| Brinell.Wpf | Brinell.Wpf.UITests | Sample WPF app |
| Brinell.WinForms | Brinell.WinForms.UITests | Sample WinForms app |
| Brinell.Maui | Brinell.Maui.UITests | Sample MAUI app + Appium |
| Brinell.Blazor | Brinell.Blazor.UITests | Sample Blazor app + Playwright |
| Brinell.Html | Brinell.Html.UITests | Sample web app + Playwright |
| Brinell.Stride | Brinell.Stride.UITests | Sample Stride game |

#### Projects NOT Requiring UI Tests

| Project | Reason |
|---------|--------|
| Brinell.Core | Interfaces only, no executable code |
| Brinell.Automation | Tested via Brinell.Stride.UITests |
| Brinell.Mocking | No UI, HTTP mocking only |

---

### REQ-004: Unit Test Project Configuration

**User Story:** As a framework developer, I want consistent test project configuration, so that all tests run reliably.

#### Acceptance Criteria

1. WHEN a unit test project builds THEN it SHALL target net10.0
2. WHEN a unit test project builds THEN it SHALL use central package management

#### Standard .csproj Configuration

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
    <RootNamespace>Brinell.[Project].Tests</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="Moq" />
    <PackageReference Include="coverlet.collector" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\srcnew\Brinell.[Project]\Brinell.[Project].csproj" />
  </ItemGroup>
</Project>
```

---

### REQ-005: UI Test Project Configuration

**User Story:** As a framework developer, I want UI test projects configured correctly, so that end-to-end tests execute properly.

#### Acceptance Criteria

1. WHEN a UI test project builds THEN it SHALL reference the platform package
2. WHEN a UI test project builds THEN it SHALL include platform-specific test utilities

#### Platform-Specific Dependencies

| UI Test Project | Additional Dependencies |
|-----------------|------------------------|
| Brinell.Wpf.UITests | FlaUI.Core, FlaUI.UIA3 (via platform) |
| Brinell.WinForms.UITests | FlaUI.Core, FlaUI.UIA3 (via platform) |
| Brinell.Maui.UITests | Appium.WebDriver (via platform) |
| Brinell.Blazor.UITests | Microsoft.Playwright (via platform) |
| Brinell.Html.UITests | Microsoft.Playwright (via platform) |
| Brinell.Stride.UITests | Brinell.Automation (for test harness) |

---

### REQ-006: Test File Organization

**User Story:** As a test author, I want consistent folder organization, so that I can easily navigate test projects.

#### Acceptance Criteria

1. WHEN viewing a test project THEN folders SHALL mirror the source project structure
2. WHEN viewing a test project THEN test files SHALL follow naming conventions

#### Unit Test Folder Structure

```
Brinell.[Platform].Tests/
├── Context/
│   └── [Platform]TestContextTests.cs
├── Controls/
│   ├── ButtonControlTests.cs
│   ├── TextBoxControlTests.cs
│   └── ...
├── Fixtures/
│   └── [Platform]TestFixture.cs
├── Mocks/
│   └── Mock[Element].cs
├── GlobalUsings.cs
└── Brinell.[Platform].Tests.csproj
```

#### UI Test Folder Structure

```
Brinell.[Platform].UITests/
├── Pages/
│   └── TestAppMainPage.cs
├── Controls/
│   ├── ButtonControlUITests.cs
│   └── ...
├── Fixtures/
│   ├── AppFixture.cs
│   └── TestAppStartup.cs
├── GlobalUsings.cs
└── Brinell.[Platform].UITests.csproj
```

---

### REQ-007: GlobalUsings.cs

**User Story:** As a test author, I want common usings pre-imported, so that test files are concise.

#### Acceptance Criteria

1. WHEN writing test code THEN common namespaces SHALL be implicitly available

#### Standard GlobalUsings.cs

```csharp
global using Xunit;
global using FluentAssertions;
global using Moq;
global using Brinell.Core.Abstractions;
global using Brinell.Core.Interfaces;
global using Brinell.Core.Locators;
```

---

### REQ-008: Solution Integration

**User Story:** As a developer, I want test projects in the solution, so that I can build and run everything together.

#### Acceptance Criteria

1. WHEN opening Brinell.sln in srcnew THEN all test projects SHALL be visible
2. WHEN building the solution THEN test projects SHALL compile
3. WHEN running `dotnet test` THEN all tests SHALL execute

#### Solution Organization

Test projects SHALL be organized in Solution Folders:
- `Tests/Unit` - All unit test projects
- `Tests/UI` - All UI test projects

---

## Non-Functional Requirements

### NFR-001: Code Architecture and Modularity

- **Single Responsibility**: Each test class tests one control or component
- **Test Isolation**: Tests do not depend on each other
- **Reusable Fixtures**: Shared setup code in fixture classes
- **Clear Naming**: Test methods follow `MethodName_Scenario_ExpectedResult` pattern

### NFR-002: Performance

- **Unit Test Speed**: All unit tests complete in < 30 seconds total
- **UI Test Speed**: Individual UI tests complete in < 30 seconds each
- **Parallel Execution**: Unit tests support parallel execution

### NFR-003: Reliability

- **Deterministic Results**: Tests produce same results on repeated runs
- **Error Recovery**: UI tests capture screenshots on failure
- **Cleanup**: Tests clean up resources after completion

### NFR-004: Code Coverage

- **Core Coverage**: > 80% coverage for Brinell.Core
- **Platform Coverage**: > 70% coverage for platform implementations
- **Critical Paths**: 100% coverage for Is/Wait/Check/Assert patterns

---

## Out of Scope

- Sample test applications (covered in separate spec)
- CI/CD pipeline configuration
- Code coverage reporting tools
- Performance benchmarking tests

---

## Dependencies

- **SPEC-007**: Solution Restructure (srcnew projects must exist)
- **Sample Apps**: UI tests require sample applications (separate spec)

---

**Document Version:** 1.0
**Created:** January 13, 2026
**Workflow:** spec_workflow/requirements
