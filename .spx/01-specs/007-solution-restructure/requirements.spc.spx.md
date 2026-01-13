# Requirements Document: Solution Restructure

## Introduction

This specification defines the restructuring of the Brinell UI Test Framework into a clean, organized solution with 9 distinct projects. The new structure will be created in the `srcNew` folder, providing a fresh implementation that follows the established architectural patterns and interface contracts defined in SPEC-006.

### Purpose

- Establish a clean codebase without legacy code
- Implement the four-layer architecture consistently
- Enable independent platform development
- Prepare for NuGet package distribution

### Value

- Reduced technical debt
- Clear project boundaries
- Easier onboarding for contributors
- Consistent implementation patterns across platforms

## Alignment with Product Vision

This restructure directly supports the product goals from the steering documents:

- **Unified API**: Clean separation ensures consistent interfaces
- **Platform-Native Performance**: Self-contained packages with direct library access
- **Multi-Platform Support**: Dedicated projects for each platform
- **NuGet Distribution**: Proper package structure for publishing

## Requirements

### REQ-001: Solution Structure

**User Story:** As a framework maintainer, I want a well-organized solution structure, so that I can easily navigate and maintain the codebase.

#### Acceptance Criteria

1. WHEN the solution is opened THEN VS/Rider SHALL display 9 projects organized by purpose

#### Projects

| Project            | Purpose                           | Layer              |
| ------------------ | --------------------------------- | ------------------ |
| Brinell.Core       | Interface contracts, shared types | Layer 2 (Core)     |
| Brinell.Wpf        | WPF platform implementation       | Layer 3 (Platform) |
| Brinell.WinForms   | WinForms platform implementation  | Layer 3 (Platform) |
| Brinell.Maui       | MAUI platform implementation      | Layer 3 (Platform) |
| Brinell.Blazor     | Blazor platform implementation    | Layer 3 (Platform) |
| Brinell.Html       | Playwright web implementation     | Layer 3 (Platform) |
| Brinell.Stride     | Stride game engine client         | Layer 3 (Platform) |
| Brinell.Automation | Stride in-game automation hooks   | Layer 3 (Platform) |
| Brinell.Mocking    | API mocking utilities             | Layer 3 (Support)  |

---

### REQ-002: Core Project (Brinell.Core)

**User Story:** As a platform implementer, I want a clean set of interface contracts, so that I can implement platform-specific controls consistently.

#### Acceptance Criteria

1. WHEN Brinell.Core is referenced THEN it SHALL provide all control interfaces

#### Folders

- `Abstractions/` - Core interfaces (IControlObject, IPageObject, ITestContext)
- `Abstractions/Controls/` - Capability interfaces (IClickableControl, ITextControl, etc.)
- `Attributes/` - Test attributes ([UITest], [SmokeTest], [Platform])
- `Exceptions/` - Framework exception types
- `Locators/` - Locator types and builders
- `Logging/` - ITestLogger and implementations

#### Dependencies

- None (interfaces only)

---

### REQ-003: WPF Platform (Brinell.Wpf)

**User Story:** As a WPF test developer, I want a complete testing package, so that I can write UI tests for WPF applications.

#### Acceptance Criteria

1. WHEN testing a WPF app THEN the developer SHALL use Brinell.Wpf package only

#### Folders

- `Context/` - WpfTestContext
- `Controls/` - WPF control implementations
- `Pages/` - PageBase for WPF
- `Testing/` - WpfTestBase class

#### Dependencies

- Brinell.Core
- FlaUI.Core
- FlaUI.UIA3

---

### REQ-004: WinForms Platform (Brinell.WinForms)

**User Story:** As a WinForms test developer, I want a complete testing package, so that I can write UI tests for WinForms applications.

#### Acceptance Criteria

1. WHEN testing a WinForms app THEN the developer SHALL use Brinell.WinForms package only

#### Folders

- `Context/` - WinFormsTestContext
- `Controls/` - WinForms control implementations
- `Pages/` - PageBase for WinForms
- `Testing/` - WinFormsTestBase class

#### Dependencies

- Brinell.Core
- FlaUI.Core
- FlaUI.UIA3

---

### REQ-005: MAUI Platform (Brinell.Maui)

**User Story:** As a MAUI test developer, I want a complete testing package, so that I can write UI tests for cross-platform MAUI applications.

#### Acceptance Criteria

1. WHEN testing a MAUI app THEN the developer SHALL use Brinell.Maui package only

#### Folders

- `Context/` - MauiTestContext
- `Controls/` - MAUI control implementations
- `Pages/` - PageBase for MAUI
- `Gestures/` - Mobile gesture support
- `Testing/` - MauiTestBase class

#### Dependencies

- Brinell.Core
- Appium.WebDriver

---

### REQ-006: Blazor Platform (Brinell.Blazor)

**User Story:** As a Blazor test developer, I want a complete testing package, so that I can write UI tests for Blazor web applications.

#### Acceptance Criteria

1. WHEN testing a Blazor app THEN the developer SHALL use Brinell.Blazor package only

#### Folders

- `Context/` - BlazorTestContext
- `Controls/` - Blazor control implementations
- `Pages/` - PageBase for Blazor
- `Testing/` - BlazorTestBase class

#### Dependencies

- Brinell.Core
- Microsoft.Playwright

---

### REQ-007: HTML/Web Platform (Brinell.Html)

**User Story:** As a web test developer, I want a Playwright-based testing package, so that I can write UI tests for web applications.

#### Acceptance Criteria

1. WHEN testing a web app THEN the developer SHALL use Brinell.Html package

#### Folders

- `Context/` - HtmlTestContext
- `Controls/` - HTML control implementations
- `Pages/` - PageBase for HTML
- `Testing/` - HtmlTestBase class

#### Dependencies

- Brinell.Core
- Microsoft.Playwright

---

### REQ-008: Stride Game Engine (Brinell.Stride)

**User Story:** As a Stride game developer, I want to write UI tests for my game, so that I can automate game UI testing.

#### Acceptance Criteria

1. WHEN testing a Stride game THEN the developer SHALL use Brinell.Stride package

#### Folders

- `Context/` - StrideTestContext
- `Controls/` - Stride UI control implementations
- `Communication/` - Named pipe client
- `Testing/` - StrideTestBase class

#### Dependencies

- Brinell.Core

---

### REQ-009: Stride Automation Hooks (Brinell.Automation)

**User Story:** As a Stride game developer, I want to add automation hooks to my game, so that Brinell.Stride can interact with my UI.

#### Acceptance Criteria

1. WHEN added to a Stride game THEN automation endpoints SHALL be available

#### Folders

- Root level files (small project)

#### Files

- `AutomationServer.cs` - Named pipe server
- `AutomationGameSystem.cs` - Stride game system integration
- `StrideUIHandler.cs` - UI query/action handler

#### Dependencies

- Stride.Engine
- Stride.UI

---

### REQ-010: API Mocking (Brinell.Mocking)

**User Story:** As a UI test developer, I want to mock backend APIs, so that I can test UI behavior in isolation.

#### Acceptance Criteria

1. WHEN setting up a mock API THEN WireMock.Net SHALL be used

#### Folders

- Root level files (small project)

#### Files

- `MockApiServer.cs` - Server wrapper
- `ApiStubBuilder.cs` - Fluent stub configuration

#### Dependencies

- Brinell.Core
- WireMock.Net

---

## Non-Functional Requirements

### NFR-001: Code Architecture and Modularity

- **Single Responsibility**: Each project has one clear purpose
- **Self-Contained Platforms**: No cross-platform dependencies
- **Clean Interfaces**: All public APIs documented with XML comments
- **Consistent Patterns**: All controls follow Is/Wait/Check/Assert

### NFR-002: Build and Compilation

- **Multi-Targeting**: All projects target net8.0, net9.0, net10.0
- **Central Package Management**: Use Directory.Packages.props
- **Warnings as Errors**: TreatWarningsAsErrors=true
- **Nullable References**: Nullable=enable

### NFR-003: NuGet Packaging

- **Package IDs**: Match project names (Brinell.Core, Brinell.Maui, etc.)
- **Source Link**: Enable for debugging
- **Symbol Packages**: Generate .snupkg files
- **README**: Include in packages

### NFR-004: Performance

- **Element Lookup**: < 100ms for cached elements
- **Startup**: TestContext creation < 500ms
- **Memory**: Minimal per-control overhead

### NFR-005: Compatibility

- **.NET Versions**: 8.0 (LTS), 9.0 (current), 10.0 (preview)
- **Platform Requirements**:
  - WPF/WinForms: Windows 10+
  - MAUI: Windows/macOS/Linux/Android/iOS
  - Blazor/Html: Cross-platform with browser support
  - Stride: Windows (primary)

---

## Out of Scope

- Test projects (will be created in separate spec)
- Sample applications
- Documentation website
- CI/CD pipeline configuration

---

**Document Version:** 1.0
**Created:** January 13, 2026
**Workflow:** spec_workflow/requirements
