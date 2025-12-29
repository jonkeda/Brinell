# Brinell UI Test Framework - Migration Plan

## Overview

This document outlines the plan to migrate the `Oravey.UITestFramework` from the Oravey repository to a standalone GitHub repository called **Brinell**.

**Brinell** will be a reusable, cross-platform UI testing framework for .NET applications, following standard .NET open-source project conventions.

---

## Current State (Oravey)

### Projects to Migrate

| Project | Description | Dependencies |
|---------|-------------|--------------|
| `Oravey.UITestFramework.Core` | Platform-agnostic abstractions | xunit.extensibility.core |
| `Oravey.UITestFramework.Wpf` | FlaUI-based WPF implementation | Core, FlaUI.Core, FlaUI.UIA3 |
| `Oravey.UITestFramework.Html` | Selenium-based web implementation | Core, Selenium.WebDriver, Selenium.Support |
| `Oravey.UITestFramework.Maui` | Appium-based mobile implementation | Core, Appium.WebDriver |
| `Oravey.UITestFramework.Mocking` | WireMock API mocking utilities | Core, WireMock.Net |

### Current Location

```
Oravey/Sources/UITestFramework/
├── Oravey.UITestFramework.Core/
├── Oravey.UITestFramework.Wpf/
├── Oravey.UITestFramework.Html/
├── Oravey.UITestFramework.Maui/
└── Oravey.UITestFramework.Mocking/
```

### Consumers

- `Oravey.Tools.Wpf.UITests` - Uses `Oravey.UITestFramework.Core` and `Oravey.UITestFramework.Wpf`

---

## Target State (Brinell)

### Repository Structure

Following standard .NET open-source conventions (similar to FluentAssertions, Serilog, FlaUI):

```
Brinell/
├── .github/
│   ├── workflows/
│   │   ├── build.yml                 # CI build on every PR/push
│   │   ├── release.yml               # NuGet publish on tag
│   │   └── codeql.yml                # Security scanning
│   ├── ISSUE_TEMPLATE/
│   │   ├── bug_report.md
│   │   └── feature_request.md
│   ├── PULL_REQUEST_TEMPLATE.md
│   ├── dependabot.yml
│   └── CODEOWNERS
│
├── docs/
│   ├── index.md                      # Documentation home
│   ├── getting-started.md            # Quick start guide
│   ├── controls/                     # Control reference
│   ├── page-objects/                 # Page object pattern guide
│   └── platform-guides/
│       ├── wpf.md
│       ├── html.md
│       └── maui.md
│
├── samples/
│   ├── Brinell.Samples.Wpf/          # WPF sample app + tests
│   ├── Brinell.Samples.Html/         # Web sample tests
│   └── Brinell.Samples.Maui/         # MAUI sample tests
│
├── src/
│   ├── Brinell.Core/                 # Platform-agnostic abstractions
│   │   ├── Abstractions/
│   │   ├── Attributes/
│   │   ├── Controls/
│   │   ├── Exceptions/
│   │   ├── Logging/
│   │   ├── Screenshots/
│   │   ├── Testing/
│   │   └── Brinell.Core.csproj
│   │
│   ├── Brinell.Wpf/                  # WPF/FlaUI implementation
│   │   ├── Controls/
│   │   ├── Infrastructure/
│   │   ├── Testing/
│   │   ├── VisualValidation/
│   │   └── Brinell.Wpf.csproj
│   │
│   ├── Brinell.Html/                 # Selenium implementation
│   │   ├── Controls/
│   │   ├── Infrastructure/
│   │   ├── Testing/
│   │   └── Brinell.Html.csproj
│   │
│   ├── Brinell.Maui/                 # Appium implementation
│   │   ├── Controls/
│   │   ├── Infrastructure/
│   │   ├── Testing/
│   │   └── Brinell.Maui.csproj
│   │
│   └── Brinell.Mocking/              # API mocking utilities
│       └── Brinell.Mocking.csproj
│
├── tests/
│   ├── Brinell.Core.Tests/           # Unit tests for Core
│   ├── Brinell.Wpf.Tests/            # Unit tests for WPF
│   └── Brinell.Integration.Tests/    # Integration tests
│
├── .editorconfig                     # Code style configuration
├── .gitignore
├── .gitattributes
├── Directory.Build.props             # Common build properties
├── Directory.Build.targets           # Common build targets
├── Directory.Packages.props          # Central package management
├── global.json                       # SDK version pinning
├── nuget.config                      # NuGet configuration
├── Brinell.sln                       # Solution file
├── LICENSE                           # MIT License
├── README.md                         # Project overview
├── CHANGELOG.md                      # Version history
├── CONTRIBUTING.md                   # Contribution guidelines
├── SECURITY.md                       # Security policy
└── CODE_OF_CONDUCT.md                # Community guidelines
```

### Package Naming

| Old Name | New Name | NuGet ID |
|----------|----------|----------|
| `Oravey.UITestFramework.Core` | `Brinell.Core` | `Brinell.Core` |
| `Oravey.UITestFramework.Wpf` | `Brinell.Wpf` | `Brinell.Wpf` |
| `Oravey.UITestFramework.Html` | `Brinell.Html` | `Brinell.Html` |
| `Oravey.UITestFramework.Maui` | `Brinell.Maui` | `Brinell.Maui` |
| `Oravey.UITestFramework.Mocking` | `Brinell.Mocking` | `Brinell.Mocking` |

### Namespace Changes

| Old Namespace | New Namespace |
|---------------|---------------|
| `Oravey.UITestFramework.Core.*` | `Brinell.Core.*` |
| `Oravey.UITestFramework.Wpf.*` | `Brinell.Wpf.*` |
| `Oravey.UITestFramework.Html.*` | `Brinell.Html.*` |
| `Oravey.UITestFramework.Maui.*` | `Brinell.Maui.*` |
| `Oravey.UITestFramework.Mocking` | `Brinell.Mocking` |

---

## Migration Phases

### Phase 1: Repository Setup (Day 1)

1. **Create GitHub Repository**
   - Create `Brinell` repository (public or private as needed)
   - Initialize with README, LICENSE (MIT), and .gitignore

2. **Setup Directory Structure**
   ```bash
   mkdir -p src tests samples docs .github/workflows .github/ISSUE_TEMPLATE
   ```

3. **Create Build Configuration Files**
   - `Directory.Build.props` - Common project properties
   - `Directory.Packages.props` - Central package management
   - `global.json` - SDK version
   - `.editorconfig` - Code style
   - `nuget.config` - Package sources

4. **Create Solution File**
   ```bash
   dotnet new sln -n Brinell
   ```

### Phase 2: Code Migration (Day 1-2)

1. **Copy Source Files**
   - Copy each project folder to `src/`
   - Rename project folders and files

2. **Update Project Files**
   - Rename `.csproj` files
   - Update `<RootNamespace>` to new names
   - Add NuGet package metadata (see template below)
   - Update project references

3. **Update Namespaces**
   - Global find/replace: `Oravey.UITestFramework.Core` → `Brinell.Core`
   - Global find/replace: `Oravey.UITestFramework.Wpf` → `Brinell.Wpf`
   - Global find/replace: `Oravey.UITestFramework.Html` → `Brinell.Html`
   - Global find/replace: `Oravey.UITestFramework.Maui` → `Brinell.Maui`
   - Global find/replace: `Oravey.UITestFramework.Mocking` → `Brinell.Mocking`

4. **Add Projects to Solution**
   ```bash
   dotnet sln add src/Brinell.Core/Brinell.Core.csproj
   dotnet sln add src/Brinell.Wpf/Brinell.Wpf.csproj
   dotnet sln add src/Brinell.Html/Brinell.Html.csproj
   dotnet sln add src/Brinell.Maui/Brinell.Maui.csproj
   dotnet sln add src/Brinell.Mocking/Brinell.Mocking.csproj
   ```

5. **Verify Build**
   ```bash
   dotnet build
   dotnet test
   ```

### Phase 3: CI/CD Setup (Day 2)

1. **GitHub Actions - Build Workflow**
   - Build on Windows (required for WPF)
   - Run tests
   - Code coverage reporting

2. **GitHub Actions - Release Workflow**
   - Trigger on version tag (v*)
   - Build NuGet packages
   - Publish to NuGet.org (or GitHub Packages initially)

3. **Dependabot Configuration**
   - Automatic dependency updates

### Phase 4: Documentation (Day 2-3)

1. **README.md** - Quick overview and badges
2. **Getting Started Guide** - Installation and basic usage
3. **API Documentation** - Control references
4. **Platform Guides** - WPF, HTML, MAUI specifics
5. **Contributing Guide** - How to contribute

### Phase 5: Oravey Integration (Day 3)

1. **Add NuGet Package Reference**
   - Update `Oravey.Tools.Wpf.UITests` to reference Brinell packages

2. **Update Using Statements**
   - Replace old namespaces with new ones

3. **Remove Old Projects from Oravey**
   - Delete `Sources/UITestFramework/` folder
   - Remove projects from `Oravey.slnx`

4. **Verify Tests Still Pass**
   - Run UI tests to confirm no regressions

---

## Project Configuration Templates

### Directory.Build.props

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    
    <!-- Package metadata -->
    <Authors>Your Name</Authors>
    <Company>Your Company</Company>
    <Copyright>Copyright © 2024-$([System.DateTime]::Now.Year) Your Name</Copyright>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageProjectUrl>https://github.com/YourOrg/Brinell</PackageProjectUrl>
    <RepositoryUrl>https://github.com/YourOrg/Brinell</RepositoryUrl>
    <RepositoryType>git</RepositoryType>
    <PackageIcon>icon.png</PackageIcon>
    <PackageReadmeFile>README.md</PackageReadmeFile>
    <PackageTags>ui-testing;automation;wpf;maui;selenium;flaui;testing</PackageTags>
    
    <!-- Source Link -->
    <PublishRepositoryUrl>true</PublishRepositoryUrl>
    <EmbedUntrackedSources>true</EmbedUntrackedSources>
    <IncludeSymbols>true</IncludeSymbols>
    <SymbolPackageFormat>snupkg</SymbolPackageFormat>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="Microsoft.SourceLink.GitHub" Version="8.0.0" PrivateAssets="All" />
  </ItemGroup>
</Project>
```

### Directory.Packages.props

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  
  <ItemGroup>
    <!-- Testing -->
    <PackageVersion Include="xunit" Version="2.9.3" />
    <PackageVersion Include="xunit.extensibility.core" Version="2.9.3" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="3.1.5" />
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageVersion Include="FluentAssertions" Version="8.0.0" />
    <PackageVersion Include="coverlet.collector" Version="6.0.4" />
    
    <!-- FlaUI for WPF -->
    <PackageVersion Include="FlaUI.Core" Version="5.0.0" />
    <PackageVersion Include="FlaUI.UIA3" Version="5.0.0" />
    
    <!-- Selenium for HTML -->
    <PackageVersion Include="Selenium.WebDriver" Version="4.25.0" />
    <PackageVersion Include="Selenium.Support" Version="4.25.0" />
    
    <!-- Appium for MAUI/Mobile -->
    <PackageVersion Include="Appium.WebDriver" Version="8.0.1" />
    
    <!-- WireMock for API mocking -->
    <PackageVersion Include="WireMock.Net" Version="1.6.10" />
    
    <!-- Source Link -->
    <PackageVersion Include="Microsoft.SourceLink.GitHub" Version="8.0.0" />
  </ItemGroup>
</Project>
```

### Sample Package csproj (Brinell.Core)

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFrameworks>net8.0;net9.0</TargetFrameworks>
    <RootNamespace>Brinell.Core</RootNamespace>
    <Description>Platform-agnostic UI testing abstractions for .NET applications. Part of the Brinell UI testing framework.</Description>
    <PackageId>Brinell.Core</PackageId>
    <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="xunit.extensibility.core" />
  </ItemGroup>
  
  <ItemGroup>
    <None Include="..\..\README.md" Pack="true" PackagePath="" />
    <None Include="..\..\icon.png" Pack="true" PackagePath="" />
  </ItemGroup>

</Project>
```

### Sample Package csproj (Brinell.Wpf)

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFrameworks>net8.0-windows;net9.0-windows</TargetFrameworks>
    <RootNamespace>Brinell.Wpf</RootNamespace>
    <Description>WPF UI testing support using FlaUI. Part of the Brinell UI testing framework.</Description>
    <PackageId>Brinell.Wpf</PackageId>
    <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
    <!-- Suppress FlaUI's System.Drawing.Common vulnerability warning -->
    <NoWarn>$(NoWarn);NU1904</NoWarn>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="FlaUI.Core" />
    <PackageReference Include="FlaUI.UIA3" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Brinell.Core\Brinell.Core.csproj" />
  </ItemGroup>

</Project>
```

---

## GitHub Actions Workflows

### .github/workflows/build.yml

```yaml
name: Build

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  build:
    runs-on: windows-latest
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: |
          8.0.x
          9.0.x
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore --configuration Release
    
    - name: Test
      run: dotnet test --no-build --configuration Release --verbosity normal --collect:"XPlat Code Coverage"
    
    - name: Upload coverage
      uses: codecov/codecov-action@v4
      with:
        files: '**/coverage.cobertura.xml'
```

### .github/workflows/release.yml

```yaml
name: Release

on:
  push:
    tags:
      - 'v*'

jobs:
  release:
    runs-on: windows-latest
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: |
          8.0.x
          9.0.x
    
    - name: Get version from tag
      id: version
      run: echo "VERSION=${GITHUB_REF#refs/tags/v}" >> $GITHUB_OUTPUT
      shell: bash
    
    - name: Restore
      run: dotnet restore
    
    - name: Build
      run: dotnet build --configuration Release -p:Version=${{ steps.version.outputs.VERSION }}
    
    - name: Pack
      run: dotnet pack --configuration Release --no-build -p:Version=${{ steps.version.outputs.VERSION }} -o ./nupkg
    
    - name: Push to NuGet
      run: dotnet nuget push "./nupkg/*.nupkg" --api-key ${{ secrets.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json --skip-duplicate
    
    - name: Create GitHub Release
      uses: softprops/action-gh-release@v2
      with:
        files: ./nupkg/*
        generate_release_notes: true
```

---

## Migration Checklist

### Pre-Migration

- [ ] Confirm Brinell repository name is available
- [ ] Decide on license (MIT recommended)
- [ ] Decide on NuGet.org org name or use personal account
- [ ] Create NuGet.org API key

### Phase 1: Repository Setup

- [ ] Create GitHub repository
- [ ] Add LICENSE (MIT)
- [ ] Add .gitignore (Visual Studio template)
- [ ] Add .gitattributes
- [ ] Create directory structure
- [ ] Add Directory.Build.props
- [ ] Add Directory.Packages.props
- [ ] Add global.json
- [ ] Add nuget.config
- [ ] Add .editorconfig
- [ ] Create Brinell.sln

### Phase 2: Code Migration

- [ ] Copy Oravey.UITestFramework.Core → src/Brinell.Core
- [ ] Copy Oravey.UITestFramework.Wpf → src/Brinell.Wpf
- [ ] Copy Oravey.UITestFramework.Html → src/Brinell.Html
- [ ] Copy Oravey.UITestFramework.Maui → src/Brinell.Maui
- [ ] Copy Oravey.UITestFramework.Mocking → src/Brinell.Mocking
- [ ] Update all .csproj files
- [ ] Rename namespaces (global find/replace)
- [ ] Add projects to solution
- [ ] Verify build succeeds
- [ ] Verify no Oravey references remain

### Phase 3: CI/CD

- [ ] Add .github/workflows/build.yml
- [ ] Add .github/workflows/release.yml
- [ ] Add .github/dependabot.yml
- [ ] Add .github/CODEOWNERS
- [ ] Add issue templates
- [ ] Add PR template
- [ ] Configure branch protection rules
- [ ] Verify CI build passes

### Phase 4: Documentation

- [ ] Write README.md with badges
- [ ] Write CHANGELOG.md
- [ ] Write CONTRIBUTING.md
- [ ] Write SECURITY.md
- [ ] Write CODE_OF_CONDUCT.md
- [ ] Create docs/getting-started.md
- [ ] Create platform-specific guides

### Phase 5: Oravey Integration

- [ ] Publish initial Brinell packages (v0.1.0)
- [ ] Add Brinell package references to Oravey
- [ ] Update using statements in Oravey.Tools.Wpf.UITests
- [ ] Verify UI tests pass
- [ ] Remove UITestFramework from Oravey.slnx
- [ ] Delete Sources/UITestFramework folder
- [ ] Commit and push Oravey changes

### Post-Migration

- [ ] Update Oravey documentation references
- [ ] Archive or close any related Oravey issues
- [ ] Announce on relevant channels

---

## Risk Mitigation

| Risk | Mitigation |
|------|------------|
| Breaking changes during rename | Keep parallel references until verified |
| NuGet package availability | Test with local packages first |
| CI/CD failures | Test workflows on feature branch |
| Missing dependencies | Verify all transitive deps are included |
| Version conflicts | Use central package management |

---

## Timeline Estimate

| Phase | Duration | Effort |
|-------|----------|--------|
| Phase 1: Repository Setup | 2-4 hours | Low |
| Phase 2: Code Migration | 4-6 hours | Medium |
| Phase 3: CI/CD Setup | 2-3 hours | Low |
| Phase 4: Documentation | 3-4 hours | Medium |
| Phase 5: Oravey Integration | 2-3 hours | Low |

**Total: 1-2 days**

---

## References

- [.NET Open Source Project Best Practices](https://docs.microsoft.com/en-us/dotnet/standard/library-guidance/)
- [GitHub Actions for .NET](https://docs.github.com/en/actions/automating-builds-and-tests/building-and-testing-net)
- [NuGet Package Guidelines](https://docs.microsoft.com/en-us/nuget/create-packages/package-authoring-best-practices)
- [FlaUI Repository](https://github.com/FlaUI/FlaUI) - Reference for project structure
- [FluentAssertions Repository](https://github.com/fluentassertions/fluentassertions) - Reference for CI/CD
