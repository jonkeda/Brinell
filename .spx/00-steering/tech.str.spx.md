# Technology Stack: Brinell UI Test Framework

## Project Type

**NuGet Library / Test Framework**

Brinell is a multi-package .NET library distributed via NuGet that provides UI test automation capabilities. It's not a standalone application but a framework that test engineers consume in their test projects.

- **Primary**: Class library packages (DLL assemblies)
- **Distribution**: NuGet package registry
- **Consumer Integration**: Referenced in xUnit test projects
- **Runtime Context**: Executes within test runner processes

## Core Technologies

### Primary Language

- **Language**: C# 13 (latest)
- **Runtime**: .NET 8.0, .NET 9.0, .NET 10.0 (multi-targeting)
- **Language Features**: Nullable reference types enabled, implicit usings, latest language version
- **Compilation**: Treat warnings as errors

### Key Dependencies/Libraries

#### Testing Infrastructure
| Package | Version | Purpose |
|---------|---------|---------|
| xunit | 2.9.3 | Test framework (core dependency) |
| xunit.extensibility.core | 2.9.3 | Custom test attributes |
| Microsoft.NET.Test.Sdk | 17.14.0 | Test SDK integration |
| Moq | 4.20.70 | Mocking framework |
| AutoFixture | 4.18.1 | Test data generation |
| Bogus | 35.5.1 | Fake data generation |
| coverlet.collector | 6.0.4 | Code coverage |

#### UI Automation Libraries (Platform-Specific)
| Package | Version | Platform | Purpose |
|---------|---------|----------|---------|
| FlaUI.Core | 5.0.0 | WPF, WinForms | Windows UI Automation |
| FlaUI.UIA3 | 5.0.0 | WPF, WinForms | UIA3 pattern provider |
| Microsoft.Playwright | 1.50.0 | HTML, Blazor | Modern web automation |
| Appium.WebDriver | 8.0.1 | MAUI | Mobile/cross-platform |
| Stride.Engine | 4.3.0.2507 | Stride | 3D game engine integration |

#### API Mocking
| Package | Version | Purpose |
|---------|---------|---------|
| WireMock.Net | 1.6.10 | HTTP API mocking |

#### Infrastructure
| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.EntityFrameworkCore | 10.0.0 | Database fixtures |
| Serilog | 4.1.0 | Structured logging |
| Microsoft.SourceLink.GitHub | 8.0.0 | Source link for debugging |

### Application Architecture

**Four-Layer Architecture with Scope-Based Design**

```
Layer 4: Application Tests (Consumer test projects)
    ↓ depends on
Layer 3: Platform Implementations (Self-contained packages)
    - Brinell.Maui, Brinell.Wpf, Brinell.Html, Brinell.Blazor, etc.
    - Each contains: TestContext, Pages, Controls, Containers
    - Uses native automation library directly
    ↓ depends on
Layer 2: Core (Interfaces only)
    - Brinell.Core
    - Interface contracts with TScope generic (IControlObject<TScope>, IPageObject)
    - IElementScope abstraction for hierarchical element finding
    - Locator types, logging, exceptions, attributes
    ↓ depends on
Layer 1: External Libraries
    - Appium, FlaUI, Playwright
```

**Key Architectural Patterns**:

1. **Scope-Based Element Finding**: IElementScope provides hierarchical search (page → container → child)
2. **Generic Fluent Chaining**: Controls use TScope generic to return containing scope for fluent chains
3. **Page Object Pattern**: Built-in base classes (MauiPageObjectBase<TSelf>) with factory methods
4. **Container Scoping**: MauiContainerBase<TParent, TSelf> for scoped element searches
5. **Is/Wait/Assert Pattern**: Consistent state verification with nullable skip pattern
6. **Self-Contained Platforms**: No cross-platform dependencies

### Data Storage

- **Primary Storage**: None (framework does not persist data)
- **Configuration**: JSON configuration files (`appsettings.json`)
- **Test Artifacts**: 
  - Screenshots saved to configurable output directory
  - Logs written to file system (CSV format)
- **Caching**: In-memory element caching per test context

### External Integrations

#### Automation Protocols
| Protocol | Used By | Purpose |
|----------|---------|---------|
| UIA3 (UI Automation 3) | FlaUI | Windows desktop automation |
| WebDriver | Selenium | Browser automation protocol |
| CDP (Chrome DevTools Protocol) | Playwright | Modern browser automation |
| Appium Protocol | Appium | Mobile/cross-platform automation |
| Named Pipes | Stride | In-process game communication |

#### Cloud Testing Services (Optional)
- BrowserStack integration for cross-browser testing
- Sauce Labs integration for cloud device testing

## Development Environment

### Build & Development Tools

- **Build System**: MSBuild (SDK-style projects)
- **Solution Structure**: `Brinell.sln` with multi-project solution
- **Package Management**: 
  - NuGet with Central Package Management (`Directory.Packages.props`)
  - PackageReference format
- **Central Configuration**:
  - `Directory.Build.props` - Shared project properties
  - `global.json` - SDK version pinning
- **Development Workflow**:
  - `dotnet build` - Build all projects
  - `dotnet test` - Run all tests
  - `dotnet pack` - Create NuGet packages

### Code Quality Tools

- **Static Analysis**: 
  - Nullable reference types enabled
  - Treat warnings as errors
  - .NET analyzers (built-in)
- **Formatting**: 
  - EditorConfig (implied by VS Code workspace)
  - Consistent naming conventions
- **Testing Framework**:
  - xUnit for unit/integration tests
  - xUnit Assert for assertions (never FluentAssertions - see SPEC-017b)
  - Moq/AutoFixture for test isolation
- **Documentation**:
  - XML documentation comments
  - PackageReadmeFile for NuGet

### Version Control & Collaboration

- **VCS**: Git
- **Repository**: GitHub (https://github.com/Iosk/Brinell)
- **Branching Strategy**: GitHub Flow (feature branches + main)
- **CI/CD**: GitHub Actions
- **Source Link**: Enabled for debugging NuGet packages

## Deployment & Distribution

### Target Platforms

| Package | Target Frameworks | Runtime Requirements |
|---------|------------------|---------------------|
| All packages | net8.0, net9.0, net10.0 | .NET 8+ runtime |
| Brinell.Wpf | Windows only | Windows 10+ |
| Brinell.WinForms | Windows only | Windows 10+ |
| Brinell.Html | Cross-platform | Browser + WebDriver |
| Brinell.Html.Playwright | Cross-platform | Chromium/Firefox/WebKit |
| Brinell.Maui | Multi-platform | Appium server |
| Brinell.Stride | Windows | Stride game runtime |

### Distribution Method

- **Primary**: NuGet.org package registry
- **Package IDs**: `Brinell.Core`, `Brinell.Wpf`, `Brinell.Maui`, etc.
- **Versioning**: Semantic versioning (currently 0.1.0 - pre-release)
- **Package Contents**:
  - DLL assemblies
  - XML documentation
  - Symbol packages (snupkg)
  - Source Link enabled

### Installation Requirements

```bash
# Consumer requirements
dotnet add package Brinell.Wpf  # or platform-specific package

# Platform prerequisites
# WPF/WinForms: Windows 10+, .NET 8+ runtime
# HTML: WebDriver executables (managed by WebDriverManager)
# Playwright: Browsers installed via playwright install
# MAUI: Appium server running, device/emulator available
# Stride: Stride game with automation hooks
```

## Technical Requirements & Constraints

### Performance Requirements

- **Element Lookup**: < 100ms for cached elements
- **Default Timeout**: 30 seconds (configurable)
- **Memory**: Minimal overhead per control instance
- **Startup**: Test context creation < 500ms
- **Logging**: Async logging to avoid test slowdown

### Compatibility Requirements

- **Platform Support**:
  - Windows 10/11 (WPF, WinForms, Stride)
  - Windows/macOS/Linux (HTML, Playwright)
  - Android/iOS (MAUI via Appium)
- **Framework Versions**:
  - .NET 8.0 LTS (minimum)
  - .NET 9.0 (current)
  - .NET 10.0 (preview)
- **Test Runners**: xUnit runners, Visual Studio Test Explorer, CLI

### Security & Compliance

- **License**: MIT (permissive open source)
- **Dependencies**: All dependencies are reputable, well-maintained packages
- **Credentials**: No credential storage; test credentials managed by consumers
- **Network**: 
  - WireMock runs on localhost
  - Cloud services require explicit configuration

### Scalability & Reliability

- **Parallel Execution**: Thread-safe design, independent test contexts
- **Resource Cleanup**: IDisposable pattern for driver/element cleanup
- **Error Recovery**: Screenshot capture on failure, detailed exceptions
- **Retry Logic**: Configurable retry for flaky element lookups

## Technical Decisions & Rationale

### Decision Log

#### 1. Self-Contained Platform Packages (v3.0)

**Decision**: Each platform package contains complete implementation without shared base classes.

**Rationale**: 
- Eliminates diamond dependency problems
- Platform-specific optimizations possible
- No adapter overhead
- Easier to debug (no abstraction layers)

**Trade-offs**: Some code duplication across platforms.

#### 2. Native Library Access (No Adapters)

**Decision**: Platform implementations use FlaUI/Appium/Selenium directly, no generic adapters.

**Rationale**:
- Full access to platform capabilities
- Better performance (no abstraction overhead)
- Easier debugging
- Platform-specific features exposed

**Trade-offs**: Test code cannot be "write once, run anywhere" for exact same code.

#### 3. Interface-Based Core

**Decision**: Brinell.Core contains only interfaces and shared types.

**Rationale**:
- Clean dependency graph
- Platform packages can evolve independently
- Clear contract definition
- No runtime dependencies in Core

#### 4. Is/Wait/Assert Pattern (Replaces Is/Wait/Check/Assert)

**Decision**: Standardize on three method types for state verification with fluent chaining.

**Rationale**:
- `Is*` - Immediate check, no waiting, returns `bool` or `bool?`
- `Wait*` - Poll with timeout, returns `bool` indicating success
- `Assert*` - Wait and throw on failure, returns `TScope` for fluent chaining

**Nullable Skip Pattern**: All Wait/Assert methods accept nullable expected values. When null, the operation skips.

**Trade-offs**: Simpler API (3 method types vs 4), fluent chaining enables readable test code.

#### 5. Generic TScope for Fluent Chaining

**Decision**: All control interfaces and classes use `TScope` generic parameter for fluent returns.

**Rationale**:
- Action methods (Click, Enter, Clear) return `TScope` enabling fluent chains
- `TScope` is the containing scope (page or container) not the control itself
- CRTP pattern (`MauiPageObjectBase<TSelf>`) for strongly-typed fluent returns
- Containers return parent scope via `Parent` property for navigation up hierarchy

**Example**:
```csharp
Page.NameEntry.Clear()       // Returns MainPage
    .NameEntry.Enter("Bob")  // Returns MainPage
    .GreetButton.Click()     // Returns MainPage
    .GreetingLabel.AssertText("Hello, Bob!");
```

**Trade-offs**: More complex type signatures, but IDE IntelliSense handles it well.

#### 6. Container Scoping Architecture

**Decision**: Containers (MauiContainerBase<TParent, TSelf>) provide scoped element finding with cached roots.

**Rationale**:
- Child element searches are scoped within container's root element
- Prevents finding wrong elements with same ID in different containers
- Root element caching with stale detection/invalidation
- Parent navigation for fluent chains up the hierarchy

**Trade-offs**: More complex container setup, but enables precise element targeting.

#### 7. xUnit as Test Framework

**Decision**: Build on xUnit, not NUnit or MSTest.

**Rationale**:
- Modern .NET test framework
- Extensive ecosystem
- Good parallel execution
- Trait-based filtering

**Trade-offs**: Tests must use xUnit runner (most common anyway).

## Known Limitations

### Current Limitations

1. **Platform Development Status**
   - MAUI platform is actively developed with full implementation
   - Other platforms (WPF, WinForms, Html, Blazor, Stride) have placeholder controls
   - *Focus*: Complete MAUI implementation as reference, then expand

2. **No Cross-Platform Code Sharing for Tests**
   - Tests are written per platform
   - Page object patterns are similar but not identical
   - *Future*: Consider code generation or shared test patterns

3. **Stride Support Limited to Windows**
   - Named pipe communication requires Windows
   - Stride itself is primarily Windows-focused
   - *Future*: Evaluate cross-platform game testing

4. **Selenium Removed in Favor of Playwright**
   - HTML platform now uses Playwright exclusively
   - Playwright provides better modern web support
   - *Future*: Selenium adapter may be added if needed

5. **Pre-Release Status (0.1.0)**
   - API may change before 1.0
   - Documentation incomplete
   - MAUI platform most mature

### Backward Compatibility Policy

**During pre-release (0.x.x):** Backward compatibility is NOT a constraint. Breaking changes are acceptable to achieve better design. The API is evolving and test writers should expect updates when upgrading versions.

**After 1.0 release:** Semantic versioning will apply - breaking changes only in major versions.

---

**Document Version:** 2.0  
**Created:** January 13, 2026  
**Updated:** January 19, 2026  
**Workflow:** steering_workflow/tech
