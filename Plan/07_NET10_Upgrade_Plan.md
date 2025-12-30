# Plan 07: .NET 10 and NuGet Package Upgrade

## Overview
Upgrade Brinell framework from .NET 8/9 to .NET 10 and update all NuGet packages to their latest versions.

## Current State
- **SDK**: .NET 9.0.100 (global.json)
- **Target Frameworks**: net8.0, net9.0, net9.0-windows
- **Central Package Management**: Directory.Packages.props

## Goals
1. Update to .NET 10 SDK
2. Add net10.0 target framework across all projects
3. Update all NuGet packages to latest stable versions
4. Maintain backward compatibility with net8.0 and net9.0
5. Fix any breaking changes from package updates

---

## Phase 1: SDK and Global Configuration (0.5 days)

### 1.1 Update global.json
```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature"
  }
}
```

### 1.2 Update Directory.Build.props
- Add net10.0 to multi-targeting where applicable
- Update any SDK-specific properties for .NET 10

### 1.3 Verify .NET 10 SDK Installation
```powershell
dotnet --list-sdks
# Should show 10.0.100 or higher
```

---

## Phase 2: NuGet Package Updates (1 day)

### 2.1 Current vs Target Versions

| Package | Current | Target | Notes |
|---------|---------|--------|-------|
| **Testing** |
| xunit | 2.9.3 | 2.10.x | Check for breaking changes |
| xunit.extensibility.core | 2.9.3 | 2.10.x | Must match xunit version |
| xunit.runner.visualstudio | 3.1.5 | 3.2.x | Latest runner |
| Microsoft.NET.Test.Sdk | 17.12.0 | 17.13.x | .NET 10 support |
| FluentAssertions | 8.0.0 | 8.1.x | Check Xceed license |
| coverlet.collector | 6.0.4 | 6.1.x | Coverage support |
| **FlaUI (WPF)** |
| FlaUI.Core | 5.0.0 | 5.1.x | .NET 10 support |
| FlaUI.UIA3 | 5.0.0 | 5.1.x | Must match Core |
| **Selenium (HTML)** |
| Selenium.WebDriver | 4.27.0 | 4.28.x | Latest Selenium |
| Selenium.Support | 4.27.0 | 4.28.x | Must match WebDriver |
| WebDriverManager | 2.17.4 | 2.18.x | Driver management |
| **Appium (MAUI)** |
| Appium.WebDriver | 8.0.1 | 8.1.x | Based on Selenium |
| **Mocking** |
| WireMock.Net | 1.6.10 | 1.7.x | API mocking |
| **Build** |
| Microsoft.SourceLink.GitHub | 8.0.0 | 9.0.x | .NET 10 compatible |

### 2.2 Update Directory.Packages.props
```xml
<?xml version="1.0" encoding="utf-8"?>
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  
  <ItemGroup>
    <!-- Testing -->
    <PackageVersion Include="xunit" Version="2.10.0" />
    <PackageVersion Include="xunit.extensibility.core" Version="2.10.0" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="3.2.0" />
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.13.0" />
    <PackageVersion Include="FluentAssertions" Version="8.1.0" />
    <PackageVersion Include="coverlet.collector" Version="6.1.0" />
    
    <!-- FlaUI for WPF -->
    <PackageVersion Include="FlaUI.Core" Version="5.1.0" />
    <PackageVersion Include="FlaUI.UIA3" Version="5.1.0" />
    
    <!-- Selenium for HTML/Web -->
    <PackageVersion Include="Selenium.WebDriver" Version="4.28.0" />
    <PackageVersion Include="Selenium.Support" Version="4.28.0" />
    <PackageVersion Include="WebDriverManager" Version="2.18.0" />
    
    <!-- Appium for MAUI/Mobile -->
    <PackageVersion Include="Appium.WebDriver" Version="8.1.0" />
    
    <!-- WireMock for API mocking -->
    <PackageVersion Include="WireMock.Net" Version="1.7.0" />
    
    <!-- Source Link -->
    <PackageVersion Include="Microsoft.SourceLink.GitHub" Version="9.0.0" />
  </ItemGroup>
</Project>
```

---

## Phase 3: Project File Updates (0.5 days)

### 3.1 Library Projects (src/)
Update target frameworks for multi-targeting:

**Brinell.Core.csproj**
```xml
<TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>
```

**Brinell.Html.csproj**
```xml
<TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>
```

**Brinell.Maui.csproj**
```xml
<TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>
```

**Brinell.Wpf.csproj**
```xml
<TargetFrameworks>net8.0-windows;net9.0-windows;net10.0-windows</TargetFrameworks>
```

**Brinell.Mocking.csproj**
```xml
<TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>
```

### 3.2 Sample Projects (samples/)
Update to net10.0:

**Brinell.Samples.Shared.csproj**
```xml
<TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>
```

**Brinell.Samples.Wpf.App.csproj**
```xml
<TargetFramework>net10.0-windows</TargetFramework>
```

**Brinell.Samples.Blazor.App.csproj**
```xml
<TargetFramework>net10.0</TargetFramework>
```

### 3.3 Test Projects
Update to net10.0:

**All UITest projects**
```xml
<TargetFramework>net10.0</TargetFramework>
<!-- or net10.0-windows for WPF tests -->
```

---

## Phase 4: Breaking Changes and Fixes (1 day)

### 4.1 Known .NET 10 Changes
- [ ] Review .NET 10 breaking changes documentation
- [ ] Update obsolete API usage
- [ ] Fix any nullable reference type warnings
- [ ] Update C# language version if new features available

### 4.2 Package-Specific Breaking Changes

**xUnit 2.10.x**
- Check for test discovery changes
- Verify collection fixtures still work
- Update any deprecated attributes

**Selenium 4.28.x**
- Migrate from deprecated `GetAttribute()` to `GetDomAttribute()` / `GetDomProperty()`
- Update WebDriver initialization if needed
- Check for BiDi API changes

**FluentAssertions 8.x**
- Review Xceed licensing requirements
- Update any deprecated assertion methods

### 4.3 Compilation Fixes
```powershell
# Build all targets to find issues
dotnet build Brinell.sln -c Debug

# Fix any errors per-project
```

---

## Phase 5: Testing and Validation (0.5 days)

### 5.1 Build Verification
```powershell
# Clean build
dotnet clean Brinell.sln
dotnet restore Brinell.sln
dotnet build Brinell.sln -c Debug
dotnet build Brinell.sln -c Release
```

### 5.2 Run All Tests
```powershell
# Unit tests
dotnet test tests/ --no-build

# WPF UI tests (requires app running)
dotnet test samples/Brinell.Samples.Wpf.UITests/ --no-build

# Blazor UI tests (requires app running)
dotnet test samples/Brinell.Samples.Blazor.UITests/ --no-build
```

### 5.3 NuGet Package Generation
```powershell
# Verify packages can be created
dotnet pack src/Brinell.Core/ -c Release
dotnet pack src/Brinell.Wpf/ -c Release
dotnet pack src/Brinell.Html/ -c Release
dotnet pack src/Brinell.Maui/ -c Release
```

---

## Phase 6: Documentation Updates (0.25 days)

### 6.1 Update README.md
- Update .NET version requirements
- Update package version badges
- Add .NET 10 compatibility notes

### 6.2 Update CHANGELOG.md
- Document .NET 10 support
- List updated package versions
- Note any breaking changes

### 6.3 Update Instructions
- Update any version-specific guidance in .github/instructions/

---

## Rollback Plan
If critical issues are found:
1. Revert global.json to .NET 9.0.100
2. Revert Directory.Packages.props to previous versions
3. Revert project file TargetFrameworks
4. Create issue to track resolution

---

## Estimated Timeline
| Phase | Duration | Dependencies |
|-------|----------|--------------|
| Phase 1: SDK Configuration | 0.5 days | .NET 10 SDK installed |
| Phase 2: NuGet Updates | 1 day | Phase 1 |
| Phase 3: Project Files | 0.5 days | Phase 2 |
| Phase 4: Breaking Changes | 1 day | Phase 3 |
| Phase 5: Testing | 0.5 days | Phase 4 |
| Phase 6: Documentation | 0.25 days | Phase 5 |
| **Total** | **~4 days** | |

---

## Success Criteria
- [ ] All projects build successfully on net10.0
- [ ] All existing tests pass
- [ ] NuGet packages can be generated
- [ ] No regression in functionality
- [ ] Backward compatibility maintained for net8.0/net9.0
