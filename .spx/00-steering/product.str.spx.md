# Product Overview: Brinell UI Test Framework

## Product Purpose

Brinell is a cross-platform UI testing framework for .NET applications that provides a unified API for automating WPF, WinForms, MAUI, HTML/Web, Blazor, and Stride 3D game engine applications.

### Problem Statement

UI test automation across multiple platforms traditionally requires:
- Learning different automation libraries for each platform (FlaUI, Selenium, Appium, Playwright)
- Writing platform-specific test code with inconsistent APIs
- Maintaining separate test infrastructures and patterns
- Lack of reusable abstractions between desktop, mobile, and web testing

Brinell solves these problems by providing:
- **Single unified API** across all supported platforms
- **Consistent patterns** (Page Object, Control Object) regardless of target platform
- **Platform-native performance** by using native automation libraries directly
- **Built-in best practices** including waiting, assertions, and logging

## Target Users

### Primary Users

1. **QA Engineers / Test Automation Engineers**
   - Writing automated UI tests for .NET applications
   - Need reliable, maintainable test code
   - Want consistent patterns across projects
   - Require good debugging and reporting capabilities

2. **.NET Developers**
   - Adding UI tests to their applications
   - Want familiar .NET patterns and tooling
   - Need tests that integrate with existing CI/CD pipelines
   - Prefer strongly-typed, IntelliSense-friendly APIs

3. **Test Team Leads / Architects**
   - Establishing testing standards across teams
   - Need framework that supports multiple platforms
   - Want maintainable, scalable test architectures
   - Require good documentation and onboarding experience

### User Needs

| User Need | How Brinell Addresses It |
|-----------|-------------------------|
| Consistent API | Unified interface hierarchy (IControlObject, IPageObject) |
| Maintainability | Page Object pattern, control abstractions |
| Reliability | Built-in waiting, retry mechanisms, state verification |
| Debuggability | Comprehensive logging, screenshot capture |
| Platform support | WPF, WinForms, MAUI, HTML, Blazor, Stride |
| CI/CD integration | Standard xUnit tests, configurable timeouts |

## Key Features

### 1. Unified Control Interface Hierarchy

A single interface hierarchy that all platforms implement:

```
IControlObject (base - all controls)
├── IClickableControl (buttons, links)
│   └── IContentControl (elements with content)
├── ITextControl (text display)
│   └── IEditableTextControl (text input)
├── IToggleControl (checkboxes, switches)
├── ISelectorControl (dropdowns, lists)
├── IRangeControl (sliders, progress bars)
├── IItemsControl (lists, collections)
└── IContainerControl (panels, groups)
```

### 2. Page Object Pattern Support

Built-in base classes and patterns for organizing test code:
- PageBase classes for each platform
- Control discovery and caching
- Page lifecycle management (displayed, ready states)
- Container-scoped element searching

### 3. Is/Wait/Check/Assert Pattern

Consistent state verification across all controls:

| Method Type | Example | Behavior |
|-------------|---------|----------|
| **Is** | `IsExists()` | Immediate check, returns bool |
| **Wait** | `WaitExists(true, 5000)` | Polls until condition or timeout |
| **Check** | `CheckExists()` | Waits and throws if not met |
| **Assert** | `AssertExists("message")` | Immediate test assertion |

### 4. Multi-Platform Support

| Platform | Automation Library | Use Case |
|----------|-------------------|----------|
| **WPF** | FlaUI/UIA3 | Windows desktop apps |
| **WinForms** | FlaUI/UIA3 | Legacy Windows apps |
| **MAUI** | Appium | Cross-platform desktop/mobile |
| **HTML** | Selenium | Traditional web apps |
| **Blazor** | Playwright | Modern web/SPA apps |
| **Stride** | Named Pipes | 3D game engine UI |

### 5. API Mocking Integration

Built-in WireMock support for isolated UI testing:
- Mock backend APIs during UI tests
- Configure endpoint stubs programmatically
- Verify API calls made during tests

### 6. Test Utilities

Supporting infrastructure for test development:
- Database fixtures for integration tests
- Performance benchmarking utilities
- Visual regression testing support
- Cloud testing integration (BrowserStack, Sauce Labs)

## Business Objectives

### Primary Objectives

1. **Reduce Test Development Time**
   - Provide ready-to-use control abstractions
   - Eliminate boilerplate code with base classes
   - Enable code reuse across platforms

2. **Improve Test Reliability**
   - Built-in waiting and synchronization
   - Clear error messages with context
   - Screenshot capture on failures

3. **Enable Cross-Platform Testing**
   - Single framework for all .NET UI platforms
   - Consistent patterns reduce learning curve
   - Share page object patterns across platforms

4. **Support Enterprise CI/CD**
   - Standard xUnit integration
   - Configurable for different environments
   - Support for parallel test execution

### Secondary Objectives

- **Open Source Community Building**: MIT license, contribution-friendly
- **Documentation Excellence**: Comprehensive guides and examples
- **NuGet Distribution**: Easy package installation and updates

## Success Metrics

### Adoption Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| NuGet Downloads | 1,000+ monthly | NuGet.org statistics |
| GitHub Stars | 100+ | GitHub repository |
| Active Contributors | 5+ | GitHub contributors |

### Quality Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| Test Coverage | >80% | Coverlet reports |
| Build Success Rate | >95% | CI/CD pipeline |
| Issue Resolution Time | <7 days avg | GitHub issues |
| Documentation Coverage | 100% public APIs | XML docs |

### User Experience Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| Time to First Test | <30 minutes | User feedback |
| Onboarding Completion | >90% | Tutorial completion |
| Stack Overflow Questions | Answered <24h | Community engagement |

## Product Principles

### 1. Platform-Native Performance

**Principle:** Use native automation libraries directly, never abstract away platform capabilities.

**Explanation:** Each platform implementation uses FlaUI, Appium, Selenium, or Playwright directly. No generic adapters that limit functionality or add overhead. Test code has full access to native capabilities when needed.

### 2. Consistent Over Identical

**Principle:** Provide consistent patterns across platforms, not identical APIs that hide platform differences.

**Explanation:** All platforms support `IControlObject.IsExists()` but platform-specific capabilities (like gesture support on mobile) are exposed through platform-specific extensions. Don't force artificial limitations for consistency.

### 3. Fail Fast with Context

**Principle:** When tests fail, provide maximum context for debugging.

**Explanation:** Every failure includes the control AutomationId, expected vs actual state, timeout used, and screenshot. Never leave test engineers guessing why a test failed.

### 4. Convention Over Configuration

**Principle:** Sensible defaults that work out of the box, with full configurability when needed.

**Explanation:** Default timeouts, logging, and screenshots work without configuration. Override via test context, environment variables, or configuration files as needed.

### 5. Test Writer First

**Principle:** Optimize for the test writer's experience, not framework elegance.

**Explanation:** APIs should be discoverable via IntelliSense, errors should suggest fixes, and common patterns should require minimal code. Framework complexity should not leak into test code.

## Future Vision

### Near-Term Enhancements

1. **Enhanced Blazor Support**
   - Component-level testing
   - JavaScript interop testing
   - WebAssembly performance testing

2. **AI-Assisted Test Generation**
   - Record user actions to generate page objects
   - Suggest assertions based on UI state
   - Auto-heal broken locators

3. **Advanced Reporting**
   - Test execution dashboards
   - Trend analysis over time
   - Screenshot/video galleries

### Long-Term Vision

1. **Visual Testing Platform**
   - Baseline image management
   - Visual diff detection
   - Cross-browser visual comparison

2. **Test Intelligence**
   - Flaky test detection
   - Test impact analysis
   - Optimal test ordering

3. **Extended Platform Support**
   - Avalonia UI (cross-platform XAML)
   - Uno Platform
   - React Native .NET

---

**Document Version:** 1.0  
**Created:** January 13, 2026  
**Workflow:** steering_workflow/product
