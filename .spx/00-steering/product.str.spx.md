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

A single interface hierarchy that all platforms implement, with generic `TScope` parameter for fluent method chaining:

```
IControlObject<TScope> (base - all controls)
├── IClickableControlObject<TScope> (buttons, links)
├── ITextControlObject<TScope> (text display)
│   └── IEditableTextControlObject<TScope> (text input)
├── IToggleControlObject<TScope> (checkboxes, switches)
├── ISelectorControlObject<TScope> (dropdowns, lists)
├── IRangeControlObject<TScope> (sliders, progress bars)
├── IScrollableControlObject<TScope> (scrollable content)
└── IContainerControl<TElement> (panels, groups - scoped search)

IElementScope<TElement> (element finding abstraction)
├── IPageObject<TElement> (page-level scope)
└── IContainerControl<TElement> (container-level scope)
```

### 2. Page Object Pattern Support

Built-in base classes and patterns for organizing test code:
- PageBase classes for each platform
- Control discovery and caching
- Page lifecycle management (displayed, ready states)
- Container-scoped element searching

### 3. Is/Wait/Assert Pattern with Fluent Chaining

Consistent state verification across all controls with fluent method chaining:

| Method Type | Example | Behavior | Returns |
|-------------|---------|----------|---------|
| **Is** | `IsExists()` | Immediate check, returns bool | `bool` or `bool?` |
| **Wait** | `WaitExists(true, 5000)` | Polls until condition or timeout | `bool` |
| **Assert** | `AssertExists(true, "msg")` | Waits, throws on failure | `TScope` (fluent) |

**Nullable Skip Pattern**: All Wait/Assert methods accept nullable expected values. When `expected` is null, the operation is skipped (returns true for Wait, returns scope for Assert). This enables conditional assertions in test code.

**Fluent Chaining**: Action and assertion methods return `TScope` (the containing scope), enabling fluent chains:
```csharp
Page.NameEntry.Clear()
    .NameEntry.Enter("Bob")
    .NameEntry.AssertText("Bob")
    .GreetButton.Click()
    .GreetingLabel.AssertText("Hello, Bob!");
```

### 4. Multi-Platform Support

| Platform | Automation Library | Package | Status |
|----------|-------------------|---------|--------|
| **MAUI** | Appium 8.x | Brinell.Maui | Active development |
| **WPF** | FlaUI/UIA3 | Brinell.Wpf | Placeholder |
| **WinForms** | FlaUI/UIA3 | Brinell.WinForms | Placeholder |
| **HTML** | Playwright | Brinell.Html | Placeholder |
| **Blazor** | Playwright | Brinell.Blazor | Placeholder |
| **Stride** | Named Pipes | Brinell.Stride | Placeholder |

### 5. Container Scoping

Containers provide hierarchical element scoping with fluent navigation:

```csharp
// Define a container that scopes child searches
public class ContactContainer : MauiContainerBase<ContainerDemoPage, ContactContainer>
{
    public ContactContainer(IMauiScope<ContainerDemoPage> parentScope, int index)
        : base(parentScope, new Locator(LocatorStrategy.AutomationId, $"Contact_{index}"))
    {
    }
    
    public MauiControlBase<ContactContainer> NameLabel => new(this, "ContactName");
    public MauiButtonControl<ContactContainer> CallButton => Button("ContactCallButton");
}

// Usage in tests - child searches are scoped to container
Page.GetContact(0).NameLabel.AssertText("Alice")
    .CallButton.Click()
    .Parent  // Navigate back to page
    .GreetingLabel.AssertText("Calling Alice...");
```

### 6. API Mocking Integration

Built-in WireMock support for isolated UI testing:
- Mock backend APIs during UI tests
- Configure endpoint stubs programmatically
- Verify API calls made during tests

### 7. Test Utilities

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

**Document Version:** 2.0  
**Created:** January 13, 2026  
**Updated:** January 19, 2026  
**Workflow:** steering_workflow/product
