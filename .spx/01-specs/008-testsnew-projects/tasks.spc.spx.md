# Tasks Document: TestsNew Projects

## Task Format

- `[ ]` = Pending, `[-]` = In-progress, `[x]` = Completed
- Include File path, Purpose, _Leverage, _Requirements, and _Prompt fields

---

## Phase 1: Infrastructure Setup

- [ ] 1. Create testsnew folder and Directory.Build.props
  - File: `testsnew/Directory.Build.props`
  - Purpose: Establish test folder with shared build properties for all test projects
  - _Leverage: `srcnew/Directory.Build.props`, `tests/Brinell.Core.Tests.ControlObject6/`_
  - _Requirements: REQ-001, REQ-004_
  - _Prompt: Role: .NET Build Engineer | Task: Create testsnew folder with Directory.Build.props that sets IsPackable=false, IsTestProject=true, inherits from root props | Restrictions: Do not duplicate settings from root Directory.Build.props | Success: Folder exists, props file valid, inherits correctly_

---

## Phase 2: Unit Test Projects - Core

- [ ] 2. Create Brinell.Core.Tests project
  - File: `testsnew/Brinell.Core.Tests/Brinell.Core.Tests.csproj`
  - Purpose: Unit tests for Core interfaces, locators, utilities
  - _Leverage: `tests/Brinell.Core.Tests.ControlObject6/Brinell.Core.Tests.ControlObject6.csproj`_
  - _Requirements: REQ-002, REQ-004_
  - _Prompt: Role: .NET Developer | Task: Create unit test project for Brinell.Core with reference to srcnew/Brinell.Core | Restrictions: Use central package management, no version numbers in PackageReference | Success: Project builds, references Core correctly_

  - [ ] 2.1 Create GlobalUsings.cs for Core.Tests
    - File: `testsnew/Brinell.Core.Tests/GlobalUsings.cs`
    - Purpose: Common using statements for Core tests
    - _Leverage: `tests/Brinell.Core.Tests.ControlObject6/GlobalUsings.cs`_
    - _Requirements: REQ-007_
    - _Prompt: Role: .NET Developer | Task: Create GlobalUsings with xunit, FluentAssertions, Moq, and Brinell.Core namespaces | Restrictions: Only include commonly used namespaces | Success: File compiles, usings available globally_

  - [ ] 2.2 Create initial test structure folders
    - Folders: `testsnew/Brinell.Core.Tests/Interfaces/`, `testsnew/Brinell.Core.Tests/Locators/`
    - Purpose: Mirror source project structure
    - _Requirements: REQ-006_
    - _Prompt: Role: .NET Developer | Task: Create folder structure matching Brinell.Core source folders | Restrictions: Only create folders for areas with testable code | Success: Folders exist matching source structure_

---

## Phase 3: Unit Test Projects - Platform Libraries

- [ ] 3. Create Brinell.Wpf.Tests project
  - File: `testsnew/Brinell.Wpf.Tests/Brinell.Wpf.Tests.csproj`
  - Purpose: Unit tests for WPF platform implementation
  - _Leverage: `tests/Brinell.Maui.Tests.ControlObject6/`_
  - _Requirements: REQ-002, REQ-004_
  - _Prompt: Role: .NET Developer | Task: Create unit test project for Brinell.Wpf with references to srcnew/Brinell.Wpf and srcnew/Brinell.Core | Restrictions: No FlaUI dependency needed for unit tests (mock it) | Success: Project builds, references correct_

  - [ ] 3.1 Create GlobalUsings.cs and folder structure
    - Files: `testsnew/Brinell.Wpf.Tests/GlobalUsings.cs`
    - Folders: `Context/`, `Controls/`, `Mocks/`, `Fixtures/`
    - Purpose: Standard test project structure
    - _Requirements: REQ-006, REQ-007_
    - _Prompt: Role: .NET Developer | Task: Create GlobalUsings and folders mirroring Brinell.Wpf structure | Success: Structure matches source project_

- [ ] 4. Create Brinell.WinForms.Tests project
  - File: `testsnew/Brinell.WinForms.Tests/Brinell.WinForms.Tests.csproj`
  - Purpose: Unit tests for WinForms platform implementation
  - _Requirements: REQ-002, REQ-004_
  - _Prompt: Role: .NET Developer | Task: Create unit test project for Brinell.WinForms | Restrictions: Same pattern as Wpf.Tests | Success: Project builds correctly_

  - [ ] 4.1 Create GlobalUsings.cs and folder structure
    - Files/Folders: Same pattern as Wpf.Tests
    - _Requirements: REQ-006, REQ-007_
    - _Prompt: Role: .NET Developer | Task: Create standard test project structure | Success: Structure complete_

- [ ] 5. Create Brinell.Maui.Tests project
  - File: `testsnew/Brinell.Maui.Tests/Brinell.Maui.Tests.csproj`
  - Purpose: Unit tests for MAUI platform implementation
  - _Leverage: `tests/Brinell.Maui.Tests.ControlObject6/`_
  - _Requirements: REQ-002, REQ-004_
  - _Prompt: Role: .NET Developer | Task: Create unit test project for Brinell.Maui including Gestures folder | Restrictions: Mock Appium driver | Success: Project builds, includes gesture tests structure_

  - [ ] 5.1 Create GlobalUsings.cs and folder structure
    - Folders: `Context/`, `Controls/`, `Gestures/`, `Mocks/`, `Fixtures/`
    - _Requirements: REQ-006, REQ-007_
    - _Prompt: Role: .NET Developer | Task: Create MAUI test structure including Gestures folder | Success: All folders created_

- [ ] 6. Create Brinell.Blazor.Tests project
  - File: `testsnew/Brinell.Blazor.Tests/Brinell.Blazor.Tests.csproj`
  - Purpose: Unit tests for Blazor platform implementation
  - _Requirements: REQ-002, REQ-004_
  - _Prompt: Role: .NET Developer | Task: Create unit test project for Brinell.Blazor | Restrictions: Mock Playwright | Success: Project builds correctly_

  - [ ] 6.1 Create GlobalUsings.cs and folder structure
    - _Requirements: REQ-006, REQ-007_
    - _Prompt: Role: .NET Developer | Task: Create standard test project structure | Success: Structure complete_

- [ ] 7. Create Brinell.Html.Tests project
  - File: `testsnew/Brinell.Html.Tests/Brinell.Html.Tests.csproj`
  - Purpose: Unit tests for HTML/Playwright platform implementation
  - _Requirements: REQ-002, REQ-004_
  - _Prompt: Role: .NET Developer | Task: Create unit test project for Brinell.Html | Restrictions: Mock Playwright | Success: Project builds correctly_

  - [ ] 7.1 Create GlobalUsings.cs and folder structure
    - _Requirements: REQ-006, REQ-007_
    - _Prompt: Role: .NET Developer | Task: Create standard test project structure | Success: Structure complete_

- [ ] 8. Create Brinell.Stride.Tests project
  - File: `testsnew/Brinell.Stride.Tests/Brinell.Stride.Tests.csproj`
  - Purpose: Unit tests for Stride client implementation
  - _Requirements: REQ-002, REQ-004_
  - _Prompt: Role: .NET Developer | Task: Create unit test project for Brinell.Stride including Communication folder | Restrictions: Mock named pipe communication | Success: Project builds, includes Communication tests structure_

  - [ ] 8.1 Create GlobalUsings.cs and folder structure
    - Folders: `Context/`, `Controls/`, `Communication/`, `Mocks/`, `Fixtures/`
    - _Requirements: REQ-006, REQ-007_
    - _Prompt: Role: .NET Developer | Task: Create Stride test structure including Communication folder | Success: All folders created_

---

## Phase 4: Unit Test Projects - Support Libraries

- [ ] 9. Create Brinell.Automation.Tests project
  - File: `testsnew/Brinell.Automation.Tests/Brinell.Automation.Tests.csproj`
  - Purpose: Unit tests for Stride automation hooks
  - _Requirements: REQ-002, REQ-004_
  - _Prompt: Role: .NET Developer | Task: Create unit test project for Brinell.Automation (server, handler, game system) | Restrictions: Mock Stride dependencies | Success: Project builds, tests server logic_

  - [ ] 9.1 Create GlobalUsings.cs and folder structure
    - _Requirements: REQ-006, REQ-007_
    - _Prompt: Role: .NET Developer | Task: Create minimal test structure for small project | Success: Structure complete_

- [ ] 10. Create Brinell.Mocking.Tests project
  - File: `testsnew/Brinell.Mocking.Tests/Brinell.Mocking.Tests.csproj`
  - Purpose: Unit tests for API mocking utilities
  - _Requirements: REQ-002, REQ-004_
  - _Prompt: Role: .NET Developer | Task: Create unit test project for Brinell.Mocking (MockApiServer, ApiStubBuilder) | Restrictions: Test builder patterns, not actual HTTP | Success: Project builds, tests fluent API_

  - [ ] 10.1 Create GlobalUsings.cs and folder structure
    - _Requirements: REQ-006, REQ-007_
    - _Prompt: Role: .NET Developer | Task: Create minimal test structure for small project | Success: Structure complete_

---

## Phase 5: UI Test Projects

- [ ] 11. Create Brinell.Wpf.UITests project
  - File: `testsnew/Brinell.Wpf.UITests/Brinell.Wpf.UITests.csproj`
  - Purpose: UI/Integration tests for WPF applications
  - _Requirements: REQ-003, REQ-005_
  - _Prompt: Role: .NET Developer | Task: Create UI test project referencing Brinell.Wpf, include FlaUI via platform | Restrictions: Requires sample WPF app (separate spec) | Success: Project builds, ready for sample app_

  - [ ] 11.1 Create GlobalUsings.cs and folder structure
    - Folders: `Pages/`, `Controls/`, `Fixtures/`
    - _Requirements: REQ-006, REQ-007_
    - _Prompt: Role: .NET Developer | Task: Create UI test structure with Pages and Fixtures | Success: Structure complete_

- [ ] 12. Create Brinell.WinForms.UITests project
  - File: `testsnew/Brinell.WinForms.UITests/Brinell.WinForms.UITests.csproj`
  - Purpose: UI/Integration tests for WinForms applications
  - _Requirements: REQ-003, REQ-005_
  - _Prompt: Role: .NET Developer | Task: Create UI test project referencing Brinell.WinForms | Restrictions: Requires sample WinForms app | Success: Project builds correctly_

  - [ ] 12.1 Create GlobalUsings.cs and folder structure
    - _Requirements: REQ-006, REQ-007_
    - _Prompt: Role: .NET Developer | Task: Create standard UI test structure | Success: Structure complete_

- [ ] 13. Create Brinell.Maui.UITests project
  - File: `testsnew/Brinell.Maui.UITests/Brinell.Maui.UITests.csproj`
  - Purpose: UI/Integration tests for MAUI applications
  - _Leverage: `tests/Brinell.Maui.UITests.ControlObject6/`_
  - _Requirements: REQ-003, REQ-005_
  - _Prompt: Role: .NET Developer | Task: Create UI test project referencing Brinell.Maui, Appium via platform | Restrictions: Requires sample MAUI app + Appium server | Success: Project builds correctly_

  - [ ] 13.1 Create GlobalUsings.cs and folder structure
    - _Requirements: REQ-006, REQ-007_
    - _Prompt: Role: .NET Developer | Task: Create standard UI test structure | Success: Structure complete_

- [ ] 14. Create Brinell.Blazor.UITests project
  - File: `testsnew/Brinell.Blazor.UITests/Brinell.Blazor.UITests.csproj`
  - Purpose: UI/Integration tests for Blazor applications
  - _Leverage: `tests/Brinell.Blazor.UITests.ControlObject6/`_
  - _Requirements: REQ-003, REQ-005_
  - _Prompt: Role: .NET Developer | Task: Create UI test project referencing Brinell.Blazor, Playwright via platform | Restrictions: Requires sample Blazor app | Success: Project builds correctly_

  - [ ] 14.1 Create GlobalUsings.cs and folder structure
    - _Requirements: REQ-006, REQ-007_
    - _Prompt: Role: .NET Developer | Task: Create standard UI test structure | Success: Structure complete_

- [ ] 15. Create Brinell.Html.UITests project
  - File: `testsnew/Brinell.Html.UITests/Brinell.Html.UITests.csproj`
  - Purpose: UI/Integration tests for web applications
  - _Requirements: REQ-003, REQ-005_
  - _Prompt: Role: .NET Developer | Task: Create UI test project referencing Brinell.Html, Playwright via platform | Restrictions: Requires sample web app | Success: Project builds correctly_

  - [ ] 15.1 Create GlobalUsings.cs and folder structure
    - _Requirements: REQ-006, REQ-007_
    - _Prompt: Role: .NET Developer | Task: Create standard UI test structure | Success: Structure complete_

- [ ] 16. Create Brinell.Stride.UITests project
  - File: `testsnew/Brinell.Stride.UITests/Brinell.Stride.UITests.csproj`
  - Purpose: UI/Integration tests for Stride games
  - _Requirements: REQ-003, REQ-005_
  - _Prompt: Role: .NET Developer | Task: Create UI test project referencing Brinell.Stride and Brinell.Automation | Restrictions: Requires sample Stride game | Success: Project builds correctly_

  - [ ] 16.1 Create GlobalUsings.cs and folder structure
    - _Requirements: REQ-006, REQ-007_
    - _Prompt: Role: .NET Developer | Task: Create standard UI test structure | Success: Structure complete_

---

## Phase 6: Solution Integration

- [ ] 17. Update srcnew/Brinell.sln with test projects
  - File: `srcnew/Brinell.sln`
  - Purpose: Add all test projects to solution with proper folder organization
  - _Leverage: Existing solution structure_
  - _Requirements: REQ-008_
  - _Prompt: Role: .NET Developer | Task: Add all 15 test projects to solution, organize in Tests/Unit and Tests/UI solution folders | Restrictions: Maintain existing project GUIDs, use proper solution folder structure | Success: All projects visible in solution, organized in folders_

- [ ] 18. Verify build and test execution
  - Command: `dotnet build srcnew/Brinell.sln` and `dotnet test srcnew/Brinell.sln`
  - Purpose: Ensure all projects build and test infrastructure works
  - _Requirements: REQ-008_
  - _Prompt: Role: .NET Developer | Task: Build solution and run test discovery to verify setup | Restrictions: No actual tests needed yet, verify infrastructure | Success: All projects build, test runner discovers test projects_

---

## Summary

| Phase | Tasks | Projects Created |
|-------|-------|-----------------|
| 1 | 1 | Infrastructure (Directory.Build.props) |
| 2 | 2-2.2 | Brinell.Core.Tests |
| 3 | 3-8.1 | 6 platform unit test projects |
| 4 | 9-10.1 | 2 support unit test projects |
| 5 | 11-16.1 | 6 UI test projects |
| 6 | 17-18 | Solution integration |

**Total Tasks:** 18 main tasks + 14 sub-tasks = 32 tasks
**Total Projects:** 15 test projects

---

**Document Version:** 1.0
**Created:** January 13, 2026
**Workflow:** spec_workflow/tasks
