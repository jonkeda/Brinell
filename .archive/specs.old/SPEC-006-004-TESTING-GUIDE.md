````markdown
# SPEC-006-004: Testing & Mockability Guide

**Version:** 1.0  
**Status:** Final  
**Date:** January 2026  
**Source:** POC Implementation Lessons Learned

---

## 1. Overview

This document captures testing patterns and mockability considerations discovered during ControlObject6 POC implementation. It provides guidance for unit testing framework components across MAUI and Blazor platforms.

---

## 2. Platform Mockability Matrix

| Platform | Driver/API | Mockable | Pattern Required |
|----------|-----------|----------|------------------|
| **Blazor** | Playwright (IPage, ILocator) | ✅ Direct | Mock interfaces directly |
| **MAUI** | Appium (AppiumDriver, AppiumElement) | ❌ Non-virtual members | Wrapper pattern |
| **WinForms** | Microsoft.UIAutomation | ⚠️ Partial | TBD |
| **WPF** | FlaUI / MS.UIAutomation | ⚠️ Partial | TBD |

---

## 3. Blazor: Direct Interface Mocking

### Why It Works

Playwright uses interfaces (`IPage`, `ILocator`, `IBrowser`, `IElementHandle`) that Moq can mock directly:

```csharp
// ✅ Works - IPage and ILocator are interfaces
var mockPage = new Mock<IPage>();
var mockLocator = new Mock<ILocator>();

mockPage.Setup(p => p.GetByTestId("submitBtn")).Returns(mockLocator.Object);
mockLocator.Setup(l => l.ClickAsync(It.IsAny<LocatorClickOptions>()))
    .Returns(Task.CompletedTask);
```

### Mock Factory Pattern

```csharp
public static class MockPlaywrightFactory
{
    public static Mock<IPage> CreateMockPage()
    {
        var mockPage = new Mock<IPage>();
        // Common setup...
        return mockPage;
    }

    public static Mock<ILocator> CreateMockLocator(
        string? text = null,
        bool visible = true,
        bool enabled = true,
        int count = 1)
    {
        var mock = new Mock<ILocator>();
        
        if (text != null)
            mock.Setup(l => l.TextContentAsync(null)).ReturnsAsync(text);
        
        mock.Setup(l => l.IsVisibleAsync(null)).ReturnsAsync(visible);
        mock.Setup(l => l.IsEnabledAsync(null)).ReturnsAsync(enabled);
        mock.Setup(l => l.CountAsync()).ReturnsAsync(count);
        
        return mock;
    }
}
```

### Playwright API Notes

| Method | Signature | Notes |
|--------|-----------|-------|
| `CountAsync()` | Takes no arguments | Don't pass options |
| `GetByTestId()` | Single string argument | No options overload |
| `IsVisibleAsync()` | Optional `null` argument | Pass `null` for defaults |
| `ClickAsync()` | Optional `LocatorClickOptions` | Use `It.IsAny<>()` in mocks |

---

## 4. MAUI: Testable Wrapper Pattern

### Why Direct Mocking Fails

AppiumDriver and AppiumElement have **non-virtual members** that Moq cannot mock:

```csharp
// ❌ FAILS - AppiumDriver.Url is not virtual
var mockDriver = new Mock<AppiumDriver>();
mockDriver.Setup(d => d.Url).Returns("http://localhost");
// System.NotSupportedException: Unsupported expression: d => d.Url
// Non-overridable members may not be used in setup / verification expressions.
```

**Root Cause:** AppiumDriver inherits from Selenium's WebDriver, which uses non-virtual properties and methods for core functionality like `Url`, `FindElement`, `Navigate`, etc.

### Solution: Wrapper Interfaces

Create testable wrapper interfaces that abstract the driver:

```csharp
public interface IAppiumDriverWrapper
{
    string Url { get; }
    IAppiumElementWrapper? FindElement(By by);
    IReadOnlyList<IAppiumElementWrapper> FindElements(By by);
    void Navigate(string url);
    byte[] GetScreenshot();
}

public interface IAppiumElementWrapper
{
    string Text { get; }
    bool Displayed { get; }
    bool Enabled { get; }
    void Click();
    void Clear();
    void SendKeys(string text);
    string? GetAttribute(string name);
    IAppiumElementWrapper? FindElement(By by);
}
```

### Production Implementation

```csharp
public class AppiumDriverWrapper : IAppiumDriverWrapper
{
    private readonly AppiumDriver _driver;

    public AppiumDriverWrapper(AppiumDriver driver)
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
    }

    public string Url => _driver.Url;

    public IAppiumElementWrapper? FindElement(By by)
    {
        try
        {
            var element = _driver.FindElement(by);
            return new AppiumElementWrapper(element);
        }
        catch (NoSuchElementException)
        {
            return null;
        }
    }

    // ... other members
}

public class AppiumElementWrapper : IAppiumElementWrapper
{
    private readonly AppiumElement _element;

    public AppiumElementWrapper(AppiumElement element)
    {
        _element = element ?? throw new ArgumentNullException(nameof(element));
    }

    public string Text => _element.Text;
    public bool Displayed => _element.Displayed;
    public bool Enabled => _element.Enabled;

    public void Click() => _element.Click();
    public void Clear() => _element.Clear();
    public void SendKeys(string text) => _element.SendKeys(text);
    
    // ... other members
}
```

### Test Context for Mocking

```csharp
public class TestableMauiTestContext : ITestContext
{
    private readonly IAppiumDriverWrapper _driverWrapper;

    public TestableMauiTestContext(IAppiumDriverWrapper driverWrapper)
    {
        _driverWrapper = driverWrapper 
            ?? throw new ArgumentNullException(nameof(driverWrapper));
    }

    public IAppiumDriverWrapper DriverWrapper => _driverWrapper;
    public int DefaultTimeoutMs { get; set; } = 30000;
    public int DefaultPollingIntervalMs { get; set; } = 100;
    
    // ... ITestContext implementation
}
```

### Testable Control Classes

```csharp
public class TestableControlBase
{
    private readonly TestableMauiTestContext _context;
    private readonly ControlLocator _locator;

    protected IAppiumElementWrapper? FindElement()
    {
        // Use wrapper interface instead of AppiumDriver directly
        return _context.DriverWrapper.FindElement(
            MobileBy.AccessibilityId(_locator.Value));
    }
}
```

### Mock Factory for MAUI

```csharp
public static class MockAppiumFactory
{
    public static Mock<IAppiumDriverWrapper> CreateMockDriverWrapper()
    {
        var mock = new Mock<IAppiumDriverWrapper>();
        mock.Setup(d => d.Url).Returns("http://localhost:4723");
        return mock;
    }

    public static Mock<IAppiumElementWrapper> CreateMockElement(
        string? text = null,
        bool displayed = true,
        bool enabled = true)
    {
        var mock = new Mock<IAppiumElementWrapper>();
        
        if (text != null)
            mock.Setup(e => e.Text).Returns(text);
        
        mock.Setup(e => e.Displayed).Returns(displayed);
        mock.Setup(e => e.Enabled).Returns(enabled);
        
        return mock;
    }

    public static void SetupFindElement(
        Mock<IAppiumDriverWrapper> driver,
        Mock<IAppiumElementWrapper> element)
    {
        driver.Setup(d => d.FindElement(It.IsAny<By>()))
            .Returns(element.Object);
    }

    public static void SetupElementNotFound(Mock<IAppiumDriverWrapper> driver)
    {
        driver.Setup(d => d.FindElement(It.IsAny<By>()))
            .Returns((IAppiumElementWrapper?)null);
    }
}
```

---

## 5. Test Organization

### Recommended Structure

```
tests/
├── Brinell.Core.Tests/
│   ├── Locators/           # Locator strategy tests
│   └── Interfaces/         # Interface contract tests
│
├── Brinell.Maui.Tests/
│   ├── Mocks/
│   │   ├── MockAppiumFactory.cs      # IAppiumDriverWrapper mocks
│   │   ├── TestableMauiTestContext.cs
│   │   └── TestableControls.cs
│   ├── Context/            # MauiTestContext tests
│   └── Controls/           # Control-specific tests
│
└── Brinell.Blazor.Tests/
    ├── Mocks/
    │   └── MockPlaywrightFactory.cs  # IPage/ILocator mocks
    ├── Context/            # BlazorTestContext tests
    └── Controls/           # Async control tests
```

### Test Naming Convention

```
[Component]-[Scenario]-[ExpectedBehavior]

Examples:
- MTC-001: MauiTestContext_Constructor_SetsDefaultTimeouts
- BC-005: ButtonControl_Click_CallsElementClick
- IC-012: InputControl_EnterAsync_SetsTextOnElement
```

---

## 6. Design Decisions

### Decision 1: Wrapper Pattern vs Interface Segregation

**Decision:** Use wrapper interfaces for MAUI, direct mocking for Blazor.

**Rationale:**
- Playwright was designed with testability in mind (interfaces)
- Appium/Selenium was not (non-virtual members)
- Wrapper pattern adds minimal overhead but enables comprehensive testing
- Production code uses real implementations; tests use mocks

### Decision 2: Testable Variants vs Production Classes

**Decision:** Create separate testable control classes for MAUI tests.

**Rationale:**
- Keeps production code clean (no test dependencies)
- Testable classes inherit same behavior patterns
- Allows testing of protected methods
- Clear separation of concerns

### Decision 3: Mock Factory Pattern

**Decision:** Use static factory classes for mock creation.

**Rationale:**
- Consistent mock setup across tests
- Reduces boilerplate in individual tests
- Easy to extend for new mock scenarios
- Centralizes mock configuration

---

## 7. Coverage Targets

| Component | Line Coverage | Branch Coverage | Method Coverage |
|-----------|--------------|-----------------|-----------------|
| Core | 90% | 85% | 95% |
| MAUI | 85% | 80% | 90% |
| Blazor | 85% | 80% | 90% |

### Priority Levels

| Priority | Description | Target |
|----------|-------------|--------|
| P0 | Critical path, must not fail | 100% coverage |
| P1 | Important functionality | 90% coverage |
| P2 | Edge cases, nice-to-have | 80% coverage |

---

## 8. Common Pitfalls

### Pitfall 1: Mocking Non-Virtual Members

```csharp
// ❌ Will throw at runtime
var mock = new Mock<AppiumDriver>();
mock.Setup(d => d.Url).Returns("...");
```

**Solution:** Always use wrapper interfaces for Appium components.

### Pitfall 2: Async Method Verification

```csharp
// ❌ May miss async issues
mockLocator.Verify(l => l.ClickAsync(null), Times.Once);

// ✅ Proper async verification
await mockLocator.Object.ClickAsync(null);
mockLocator.Verify(l => l.ClickAsync(It.IsAny<LocatorClickOptions>()), Times.Once);
```

### Pitfall 3: Playwright API Assumptions

```csharp
// ❌ CountAsync takes no arguments
mockLocator.Setup(l => l.CountAsync(null)).ReturnsAsync(1);

// ✅ Correct signature
mockLocator.Setup(l => l.CountAsync()).ReturnsAsync(1);
```

### Pitfall 4: Missing Interface Members

When creating testable controls, ensure ALL interface members are implemented:
- `IClickableControlObject`: Click, DoubleClick, RightClick, Hover, LongPress
- `IFocusableControlObject`: IsFocused, Focus, Blur
- `ITextControlObject`: Enter, Clear, ClearAndEnter, Append, IsReadOnly, GetTextLength

---

## 9. Integration with CI/CD

### Recommended Test Commands

```powershell
# Run all tests
dotnet test --configuration Release

# Run with coverage
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage

# Run specific project
dotnet test tests/Brinell.Core.Tests/

# Run with filter
dotnet test --filter "Category=Unit"
```

### CI Pipeline Considerations

1. **No real Appium/Playwright servers needed** - All tests use mocks
2. **Fast execution** - Unit tests complete in seconds
3. **Parallel execution** - Tests are isolated and can run in parallel
4. **Coverage reports** - Use coverlet for coverage collection

---

## 10. Future Considerations

### WinForms/WPF Testing

The same wrapper pattern may be needed for:
- Microsoft.UIAutomation
- FlaUI
- White framework

Evaluate non-virtual member usage before deciding on pattern.

### Integration Testing

For end-to-end testing with real automation servers:
- Separate integration test projects
- Use real Appium/Playwright connections
- Mark with `[Trait("Category", "Integration")]`
- Run in dedicated CI stages

---

## References

- [SPEC-006-INDEX](SPEC-006-INDEX.md) - ControlObject Framework Overview
- [SPEC-006-003-HIERARCHY-MAUI](SPEC-006-003-HIERARCHY-MAUI.md) - MAUI Control Hierarchy
- [SPEC-006-003-HIERARCHY-BLAZOR](SPEC-006-003-HIERARCHY-BLAZOR.md) - Blazor Async Hierarchy
- [REQ-002](REQ-002-non-functional-requirements.md) - NFR-MAINT-002.2: Testability

---

**Version History:**

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | January 2026 | POC Team | Initial version from POC learnings |

````
