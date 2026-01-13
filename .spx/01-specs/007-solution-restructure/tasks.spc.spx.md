# Tasks Document: Solution Restructure

## Task Format

- `[ ]` = Pending, `[-]` = In-progress, `[x]` = Completed
- Include File path, Purpose, _Leverage, _Requirements, and _Prompt fields

---

## Phase 1: Infrastructure

- [x] 1. Create Directory.Build.props
  - File: srcNew/Directory.Build.props
  - Purpose: Shared MSBuild properties for all projects
  - _Requirements: REQ-001, NFR-002_
  - _Prompt: Role: .NET Build Engineer | Task: Create Directory.Build.props with LangVersion=latest, Nullable=enable, TreatWarningsAsErrors=true, package metadata | Restrictions: Multi-target net8.0;net9.0;net10.0 | Success: File compiles, properties inherited by all projects_

- [x] 2. Create Directory.Packages.props
  - File: srcNew/Directory.Packages.props
  - Purpose: Central package version management
  - _Leverage: existing Directory.Packages.props in root_
  - _Requirements: REQ-001, NFR-002_
  - _Prompt: Role: .NET Build Engineer | Task: Create Directory.Packages.props with all package versions (FlaUI, Appium, Playwright, WireMock, Stride, xunit) | Restrictions: Use ManagePackageVersionsCentrally | Success: All package versions centralized_

---

## Phase 2: Core Project

- [x] 3. Create Brinell.Core project
  - File: srcNew/Brinell.Core/Brinell.Core.csproj
  - Purpose: Core interfaces project
  - _Requirements: REQ-002_
  - _Prompt: Role: .NET Developer | Task: Create class library project with multi-targeting, no external dependencies | Restrictions: No PackageReferences except xunit.extensibility.core | Success: Project builds, added to solution_

- [x] 4. Create Brinell.Core folder structure
  - Files: srcNew/Brinell.Core/Abstractions/, Attributes/, Exceptions/, Locators/, Logging/
  - Purpose: Establish folder structure per requirements
  - _Requirements: REQ-002_
  - _Prompt: Role: .NET Developer | Task: Create folder structure with placeholder files | Restrictions: Empty folders need .gitkeep or placeholder class | Success: All folders exist with namespace files_

---

## Phase 3: Platform Projects

- [x] 5. Create Brinell.Wpf project
  - File: srcNew/Brinell.Wpf/Brinell.Wpf.csproj
  - Folders: Context/, Controls/, Pages/, Testing/
  - Purpose: WPF platform implementation
  - _Requirements: REQ-003_
  - _Prompt: Role: .NET Developer | Task: Create class library with reference to Brinell.Core, FlaUI.Core, FlaUI.UIA3 | Restrictions: Windows-only TFMs | Success: Project builds, references Core_

- [x] 6. Create Brinell.WinForms project
  - File: srcNew/Brinell.WinForms/Brinell.WinForms.csproj
  - Folders: Context/, Controls/, Pages/, Testing/
  - Purpose: WinForms platform implementation
  - _Requirements: REQ-004_
  - _Prompt: Role: .NET Developer | Task: Create class library with reference to Brinell.Core, FlaUI.Core, FlaUI.UIA3 | Restrictions: Windows-only TFMs | Success: Project builds, references Core_

- [x] 7. Create Brinell.Maui project
  - File: srcNew/Brinell.Maui/Brinell.Maui.csproj
  - Folders: Context/, Controls/, Pages/, Gestures/, Testing/
  - Purpose: MAUI platform implementation
  - _Requirements: REQ-005_
  - _Prompt: Role: .NET Developer | Task: Create class library with reference to Brinell.Core, Appium.WebDriver | Restrictions: Cross-platform TFMs | Success: Project builds, references Core_

- [x] 8. Create Brinell.Blazor project
  - File: srcNew/Brinell.Blazor/Brinell.Blazor.csproj
  - Folders: Context/, Controls/, Pages/, Testing/
  - Purpose: Blazor platform implementation
  - _Requirements: REQ-006_
  - _Prompt: Role: .NET Developer | Task: Create class library with reference to Brinell.Core, Microsoft.Playwright | Restrictions: Cross-platform TFMs | Success: Project builds, references Core_

- [x] 9. Create Brinell.Html project
  - File: srcNew/Brinell.Html/Brinell.Html.csproj
  - Folders: Context/, Controls/, Pages/, Testing/
  - Purpose: HTML/Web platform implementation
  - _Requirements: REQ-007_
  - _Prompt: Role: .NET Developer | Task: Create class library with reference to Brinell.Core, Microsoft.Playwright | Restrictions: Cross-platform TFMs | Success: Project builds, references Core_

- [x] 10. Create Brinell.Stride project
  - File: srcNew/Brinell.Stride/Brinell.Stride.csproj
  - Folders: Context/, Controls/, Communication/, Testing/
  - Purpose: Stride game engine test client
  - _Requirements: REQ-008_
  - _Prompt: Role: .NET Developer | Task: Create class library with reference to Brinell.Core only | Restrictions: No Stride dependencies (client only) | Success: Project builds, references Core_

- [x] 11. Create Brinell.Automation project
  - File: srcNew/Brinell.Automation/Brinell.Automation.csproj
  - Files: AutomationServer.cs, AutomationGameSystem.cs, StrideUIHandler.cs
  - Purpose: Stride in-game automation hooks
  - _Requirements: REQ-009_
  - _Prompt: Role: .NET Developer | Task: Create class library with Stride.Engine, Stride.UI references | Restrictions: No Brinell.Core reference, net10.0 only | Success: Project builds with Stride packages_

- [x] 12. Create Brinell.Mocking project
  - File: srcNew/Brinell.Mocking/Brinell.Mocking.csproj
  - Files: MockApiServer.cs, ApiStubBuilder.cs
  - Purpose: API mocking utilities
  - _Requirements: REQ-010_
  - _Prompt: Role: .NET Developer | Task: Create class library with reference to Brinell.Core, WireMock.Net | Restrictions: Cross-platform | Success: Project builds, references Core_

---

## Phase 4: Solution Integration

- [x] 13. Add all projects to solution
  - File: srcNew/Brinell.sln
  - Purpose: Register all projects in solution file
  - _Requirements: REQ-001_
  - _Prompt: Role: .NET Developer | Task: Run dotnet sln add for all 9 projects | Restrictions: Verify build order | Success: Solution opens in VS/Rider with all projects_

- [x] 14. Verify solution builds
  - Purpose: Ensure all projects compile without errors
  - _Requirements: REQ-001, NFR-002_
  - _Prompt: Role: .NET Developer | Task: Run dotnet build and fix any errors | Restrictions: No warnings allowed | Success: Clean build with no errors or warnings_

---

## Implementation Notes

- **Brinell.Automation**: Targets net10.0 only (Stride packages require it)
- **Brinell.Mocking**: Suppresses NU1903 warning for WireMock transitive dependency vulnerability
- **Legacy code removal**: Pre-existing Selenium-based Blazor and incompatible Maui code was removed in favor of clean placeholder structure

---

**Document Version:** 1.1  
**Created:** January 13, 2026  
**Completed:** January 13, 2026  
**Workflow:** spec_workflow/tasks
