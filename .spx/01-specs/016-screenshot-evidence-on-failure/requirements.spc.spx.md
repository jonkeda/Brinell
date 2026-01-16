# SPEC-016: Screenshot Evidence on Failure

## Requirements Phase

**Spec ID:** 016  
**Spec Name:** screenshot-evidence-on-failure  
**Created:** 2025-01-20  
**Status:** Requirements  

---

## 1. Problem Statement

### Current State
- Basic screenshot capability exists (`ITestContext.TakeScreenshot()`, `SaveScreenshot(path)`)
- `IPageObject.TakeScreenshot()` provides page-level screenshots
- No automatic capture on test failures (assertions, exceptions, timeouts)
- No integration with test output/artifacts for evidence collection
- No standardized naming or organization for failure screenshots
- Manual screenshot calls require explicit placement in test code

### Pain Points
1. **Debugging Blind Spots** - When tests fail in CI/CD, no visual context of the UI state
2. **Manual Effort** - Test writers must explicitly add screenshot calls around potential failure points
3. **Inconsistent Evidence** - No standard approach to organizing or naming failure artifacts
4. **Lost Context** - Without automatic capture, the exact failure moment is often missed

---

## 2. Requirements

### 2.1 Automatic Screenshot on Failure (FR-502.1)

**MUST** automatically capture a screenshot when any of the following occur:
- Assertion failures (FluentAssertions, xUnit assertions)
- Unhandled exceptions in test methods
- Element not found (`ElementNotFoundException`)
- Timeout exceptions (`TimeoutException`)

**MUST** integrate with xUnit's test lifecycle without requiring test code changes.

### 2.2 Manual Screenshot API (FR-502.2)

**MUST** provide explicit screenshot methods:
- `context.CaptureScreenshot()` - Full screen with auto-generated name
- `context.CaptureScreenshot(string name)` - Full screen with custom name
- `control.CaptureScreenshot()` - Element-specific screenshot (if supported by driver)

**SHOULD** support element-specific screenshots for targeted evidence.

### 2.3 Screenshot Naming Convention (FR-502.3)

**MUST** follow a consistent naming pattern:
```
{TestClass}_{TestMethod}_{Timestamp}_{Description}.{ext}
```

Example: `MainPageTests_ValidateButton_20250120_143022_failure.png`

**MUST** include:
- Test class name
- Test method name  
- Timestamp (sortable format: yyyyMMdd_HHmmss)
- Description (failure, manual, element, etc.)

### 2.4 Storage Configuration (FR-502.4)

**MUST** support configurable output directory via `BrinellTestSettings`:
```csharp
public class BrinellTestSettings
{
    public string ScreenshotDirectory { get; set; } = "./TestResults/Screenshots";
    public ScreenshotFormat Format { get; set; } = ScreenshotFormat.Png;
    public bool CaptureOnFailure { get; set; } = true;
}
```

**SHOULD** support format options (PNG, JPEG with quality).

**SHOULD** create directories automatically if they don't exist.

### 2.5 Test Result Integration (FR-502.5)

**MUST** attach screenshots to test output so they appear in:
- xUnit test results
- CI/CD artifact collection
- Local test explorer output

**SHOULD** log screenshot path to test logger for traceability.

### 2.6 Cross-Platform Support (FR-502.6)

**MUST** work with all supported drivers:
- Windows (Appium WindowsDriver)
- Android (Appium AndroidDriver)  
- iOS (Appium IOSDriver)
- Blazor (Selenium WebDriver - future)

**SHOULD** gracefully handle driver-specific limitations.

---

## 3. Scope

### In Scope
- Automatic failure screenshot capture hook
- Manual screenshot API extensions
- Configuration options for screenshot behavior
- File naming and organization
- Basic test result integration
- MAUI platform implementation

### Out of Scope (Future Considerations)
- Video recording during test execution
- Screenshot comparison/diff tools
- Cloud storage integration
- Blazor platform (separate spec)
- Screenshot compression/optimization

---

## 4. Technical Considerations

### Existing Foundation
- `ITestContext.TakeScreenshot()` → Returns `byte[]`
- `ITestContext.SaveScreenshot(string path)` → Saves to file
- `IPageObject.TakeScreenshot()` → Page-level screenshot
- `MauiTestContext` implements via `_rawDriver.GetScreenshot()`

### Integration Points
- xUnit `ITestOutputHelper` for result attachment
- `ITestLogger` for path logging
- `BrinellTestSettings` for configuration
- Test fixtures for lifecycle hooks

### Architecture Pattern
Following existing patterns:
- Interface in `Brinell.Core.Interfaces`
- Implementation in platform projects (`Brinell.Maui`)
- Configuration in `Brinell.Core.Configuration`

---

## 5. Acceptance Criteria

1. **AC-1:** When an assertion fails, a screenshot is automatically saved without test code changes
2. **AC-2:** Screenshot filename includes test name and timestamp
3. **AC-3:** Output directory is configurable via settings
4. **AC-4:** Manual `CaptureScreenshot()` method is available on context
5. **AC-5:** Screenshots work on Windows MAUI driver (primary target)
6. **AC-6:** Test logger records screenshot paths
7. **AC-7:** Feature can be disabled via configuration

---

## 6. Dependencies

- xUnit 2.9.3 (test framework integration)
- FluentAssertions 8.x (assertion hook consideration)
- Appium.WebDriver 5.x (screenshot capability)
- Existing `ITestContext`, `ITestLogger` interfaces

---

## 7. Open Questions

1. **Q1:** Should element-specific screenshots crop to element bounds, or just highlight the element?
2. **Q2:** How should multiple failures in one test be handled (e.g., parameterized tests)?
3. **Q3:** Should we integrate with xUnit's `ITestOutputHelper` or use custom artifact attachment?

---

**Next Phase:** Design (say "design" to continue)
