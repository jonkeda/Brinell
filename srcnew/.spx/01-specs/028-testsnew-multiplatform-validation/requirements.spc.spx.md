# SPEC-028: Testsnew Multi-Platform Validation - Requirements

**Spec ID:** 028  
**Feature:** testsnew-multiplatform-validation  
**Status:** Draft  
**Created:** January 21, 2026  
**Depends On:** SPEC-027 (Unified Driver Abstraction)

---

## Introduction

This specification validates the implementation status of SPEC-027 (Unified Driver Abstraction) and defines the remaining work needed to run `testsnew/Brinell.Maui.UITests` on both **Android** (via Appium) and **Windows** (via FlaUI).

### Purpose

1. **Validate** what has been implemented from SPEC-027
2. **Identify gaps** preventing multi-platform test execution
3. **Define requirements** for completing the unified driver abstraction
4. **Enable** the same test code to run on Android and Windows platforms

### Current State Analysis

Based on code review of `srcnew/`:

| Component | Status | Location | Notes |
|-----------|--------|----------|-------|
| `IElement<TSelf>` | ✅ Complete | `Brinell.Core/Interfaces/IElement.cs` | All gestures included |
| `IDriver<TElement>` | ✅ Complete | `Brinell.Core/Interfaces/IDriver.cs` | Generic driver interface |
| `IDiagnosticDriver` | ✅ Complete | `Brinell.Core/Interfaces/IDiagnosticDriver.cs` | Optional diagnostics |
| `TextInputMethod` | ✅ Complete | `Brinell.Core/TextInputMethod.cs` | Keys, Paste, SetValue |
| `IMauiElement` | ✅ Complete | `Brinell.Maui/Interfaces/IMauiElement.cs` | Extends IElement |
| `IMauiDriver` | ✅ Complete | `Brinell.Maui/Interfaces/IMauiDriver.cs` | Extends IDriver + IDiagnosticDriver |
| `AppiumMauiDriver` | ✅ Complete | `Brinell.Maui.Appium/AppiumMauiDriver.cs` | Implements IMauiDriver |
| `AppiumMauiElement` | ✅ Complete | `Brinell.Maui.Appium/AppiumMauiElement.cs` | Implements IMauiElement |
| `FlaUIMauiDriver` | ✅ Complete | `Brinell.Maui.FlaUI/FlaUIMauiDriver.cs` | Implements IMauiDriver |
| `FlaUIMauiElement` | ✅ Complete | `Brinell.Maui.FlaUI/FlaUIMauiElement.cs` | Implements IMauiElement |
| `MauiTestContext` | ⚠️ Partial | `Brinell.Maui/Context/MauiTestContext.cs` | Still uses Appium directly |
| `MauiTestFixtureBase` | ⚠️ Partial | `Brinell.Maui/Testing/MauiTestFixtureBase.cs` | No FlaUI support |
| `MauiDriverFactory` | ❌ Missing | N/A | Platform-based driver selection |
| `MauiDriverOptions` | ❌ Missing | N/A | Unified driver configuration |

### Problem Statement

The current `testsnew/Brinell.Maui.UITests`:

1. **Works on Android** - Uses Appium via `MauiTestContext` and `AppiumFixture`
2. **Does NOT work on Windows with FlaUI** - No integration path exists

The test fixture (`MauiTestFixtureBase`) directly creates Appium drivers without using the new `IMauiDriver` abstraction:

```csharp
// Current: Directly creates Appium driver
(_rawDriver, _platform) = platformName switch
{
    "android" => ((AppiumDriver)new AndroidDriver(...), MauiPlatform.Android),
    "windows" => ((AppiumDriver)new WindowsDriver(...), MauiPlatform.Windows),
    ...
};
_driver = new MauiDriver(_rawDriver, _platform);
```

To use FlaUI on Windows, we need:
```csharp
// Needed: Factory-based driver selection
_driver = MauiDriverFactory.Create(options);  // Returns FlaUIMauiDriver on Windows
```

---

## Alignment with Product Vision

This feature supports Brinell's core goals:

- **Cross-Platform Testing** - Same test code runs on Windows and Android
- **Framework Independence** - Tests use `IMauiDriver`/`IMauiElement` interfaces
- **Performance** - FlaUI on Windows is faster than Appium+WinAppDriver
- **Maintainability** - One test suite, multiple platform drivers

---

## Requirements

### REQ-028.1: Driver Factory for Platform Selection

**User Story:** As a test framework user, I want the system to automatically select the best driver for my platform so that I don't need to configure platform-specific drivers manually.

#### Acceptance Criteria

1. WHEN `MauiDriverFactory.Create()` is called with `MauiPlatform.Windows` THEN the system SHALL return a `FlaUIMauiDriver` instance (always - no Appium option)
2. WHEN `MauiDriverFactory.Create()` is called with `MauiPlatform.Android` THEN the system SHALL return an `AppiumMauiDriver` instance
3. WHEN `MauiDriverFactory.Create()` is called with `MauiPlatform.iOS` THEN the system SHALL return an `AppiumMauiDriver` instance
4. WHEN driver creation fails THEN the system SHALL throw a descriptive exception with troubleshooting guidance
5. WHEN the FlaUI assembly is not available on non-Windows platforms THEN the system SHALL NOT throw at compile time (runtime resolution)
6. Windows SHALL NOT support Appium driver - FlaUI is the only option

---

### REQ-028.2: Unified Driver Options

**User Story:** As a test developer, I want a single options class that works for all platforms so that I can configure tests consistently.

#### Acceptance Criteria

1. WHEN `MauiDriverOptions` is created THEN it SHALL accept platform-agnostic configuration (AppPath, ProcessName, Timeouts)
2. WHEN `MauiDriverOptions` is used with Android THEN it SHALL accept Appium-specific settings (AppiumServerUri, DeviceName, Capabilities)
3. WHEN `MauiDriverOptions` is used with Windows THEN it SHALL accept FlaUI-specific settings (WindowHandle, AttachToProcess)
4. WHEN both AppPath and ProcessName are set THEN AppPath SHALL take precedence (launch app)
5. WHEN neither AppPath nor ProcessName is set THEN the system SHALL throw `ArgumentException`

---

### REQ-028.3: Test Context Integration

**User Story:** As a test developer, I want `MauiTestContext` to use the driver factory so that my tests work on any platform without code changes.

#### Acceptance Criteria

1. WHEN `MauiTestContext` is created with `MauiTestContextOptions` THEN it SHALL use `MauiDriverFactory` for driver creation
2. WHEN `MauiTestContext.Driver` is accessed THEN it SHALL return the `IMauiDriver` interface (not concrete type)
3. WHEN the context is disposed THEN it SHALL properly dispose the underlying driver
4. WHEN existing test code uses `MauiTestContext` THEN it SHALL continue to work without modification (backward compatible)

---

### REQ-028.4: Test Fixture Platform Support

**User Story:** As a test runner, I want to run the same tests on different platforms by changing an environment variable.

#### Acceptance Criteria

1. WHEN `APPIUM_PLATFORM=windows` THEN `MauiTestFixtureBase` SHALL use FlaUI driver (always)
2. WHEN `APPIUM_PLATFORM=android` THEN `MauiTestFixtureBase` SHALL use Appium driver
3. WHEN `APPIUM_PLATFORM=ios` THEN `MauiTestFixtureBase` SHALL use Appium driver
4. WHEN the driver type changes THEN all tests in `testsnew/Brinell.Maui.UITests` SHALL pass without modification

---

### REQ-028.5: Existing Test Compatibility

**User Story:** As a test developer, I want my existing tests to work without changes when switching drivers.

#### Acceptance Criteria

1. WHEN tests use `Page.Button.Click()` THEN it SHALL work on both Appium and FlaUI
2. WHEN tests use `control.AssertText("expected")` THEN it SHALL work on both drivers
3. WHEN tests use `element.ScrollIntoView()` THEN it SHALL work on both drivers (platform-appropriate scrolling)
4. WHEN tests use `WaitExists()` or `WaitClickable()` THEN polling SHALL work on both drivers
5. WHEN tests use locators (`Locator.AutomationId("...")`) THEN they SHALL work on both drivers

#### Test Categories to Validate

All tests in `testsnew/Brinell.Maui.UITests/Tests/` must pass on both platforms:

| Category | Test File | Controls Tested |
|----------|-----------|-----------------|
| Button | `ButtonControlTests.cs` | Click, state checks, fluent chaining |
| Entry | `EntryControlTests.cs` | Text entry, clear, assertions |
| Container | `ContainerScopingTests.cs` | Scoped element finding |
| Navigation | `TabbedPageTests.cs` | Tab navigation |
| Display | `Display/*.cs` | Labels, images |
| Selection | `Selection/*.cs` | Pickers, lists |
| Toggle | `Toggle/*.cs` | Switches, checkboxes |
| Range | `Range/*.cs` | Sliders, progress bars |

---

### REQ-028.6: Locator Strategy Compatibility

**User Story:** As a test developer, I want my locators to work on both platforms without modification.

#### Acceptance Criteria

1. WHEN `LocatorStrategy.AutomationId` is used THEN it SHALL map to:
   - **Android**: `By.Id()` (resource-id contains value)
   - **Windows FlaUI**: `ConditionFactory.ByAutomationId()`
   - **Windows Appium**: `MobileBy.AccessibilityId()`

2. WHEN `LocatorStrategy.Name` is used THEN it SHALL map to:
   - **Android**: `MobileBy.AccessibilityId()` (content-desc)
   - **Windows FlaUI**: `ConditionFactory.ByName()`
   
3. WHEN `LocatorStrategy.XPath` is used with FlaUI THEN the system SHALL throw `LocatorNotSupportedException` with message suggesting AutomationId

4. WHEN locator translation fails THEN the error message SHALL include the locator value and supported strategies

---

### REQ-028.7: FlaUI Assembly Isolation

**User Story:** As a framework user on non-Windows platforms, I want the framework to compile without FlaUI dependencies.

#### Acceptance Criteria

1. WHEN building `Brinell.Maui` on non-Windows THEN it SHALL NOT require FlaUI packages
2. WHEN `MauiDriverFactory` is used on non-Windows THEN it SHALL NOT load FlaUI assemblies
3. WHEN FlaUI is needed but not available THEN the system SHALL throw descriptive `PlatformNotSupportedException`
4. FlaUI references SHALL be isolated to `Brinell.Maui.FlaUI` project only

---

## Non-Functional Requirements

### Code Architecture and Modularity

- **Factory Pattern**: `MauiDriverFactory` encapsulates driver creation logic
- **Interface Segregation**: Tests depend only on `IMauiDriver`/`IMauiElement`
- **Lazy Loading**: FlaUI assembly loaded only when Windows platform selected
- **Backward Compatibility**: Existing tests work without modification

### Performance

- **Driver Initialization**: FlaUI driver SHALL initialize within 5 seconds
- **Element Finding**: FlaUI `FindElement` SHALL be at least as fast as Appium
- **No Polling Overhead**: Poll intervals SHALL not change between drivers

### Reliability

- **Clean Shutdown**: Drivers SHALL release all resources on Dispose
- **Process Cleanup**: Application processes SHALL terminate on test completion
- **Error Recovery**: Driver errors SHALL not leave orphaned processes

### Testing

- **Unit Tests**: Factory and options classes SHALL have 80%+ coverage
- **Integration Tests**: Both drivers SHALL pass the full UI test suite
- **CI Validation**: Tests SHALL run on Windows (FlaUI) and Android emulator (Appium)

---

## Scope

### In Scope

1. `MauiDriverFactory` - Platform-based driver selection
2. `MauiDriverOptions` - Unified configuration class
3. `MauiTestContext` refactoring - Use factory instead of direct driver creation
4. `MauiTestFixtureBase` updates - Support FlaUI via environment variable
5. Validation of all `testsnew/Brinell.Maui.UITests` on both platforms
6. Documentation updates

### Out of Scope

1. iOS platform validation (no macOS available)
2. macOS driver implementation
3. New control implementations (covered by SPEC-024)
4. Performance benchmarking (separate effort)
5. CI/CD pipeline changes (separate effort)

---

## Dependencies

| Dependency | Type | Status |
|------------|------|--------|
| SPEC-027 Interfaces | Required | ✅ Complete |
| SPEC-027 AppiumMauiDriver | Required | ✅ Complete |
| SPEC-027 FlaUIMauiDriver | Required | ✅ Complete |
| SPEC-024 Controls | Required | ✅ Complete |
| Sample MAUI App | Required | ✅ Exists |
| Appium Server | Runtime | Available |
| Android Emulator | Runtime | Available |

---

## Validation Checklist

### Phase 1: Factory Implementation
- [ ] Create `MauiDriverOptions` class
- [ ] Create `MauiDriverFactory` with platform selection
- [ ] Add unit tests for factory

### Phase 2: Context Integration  
- [ ] Refactor `MauiTestContext` to use factory
- [ ] Update `MauiTestFixtureBase` for FlaUI support
- [ ] Maintain backward compatibility

### Phase 3: Test Validation - Android
- [ ] Run all `testsnew/Brinell.Maui.UITests` on Android emulator
- [ ] Verify all tests pass with Appium driver
- [ ] Document any failures

### Phase 4: Test Validation - Windows (FlaUI)
- [ ] Set `USE_FLAUI=true` environment variable
- [ ] Run all `testsnew/Brinell.Maui.UITests` on Windows
- [ ] Verify all tests pass with FlaUI driver
- [ ] Document any failures

### Phase 5: Documentation
- [ ] Update README with multi-platform instructions
- [ ] Document environment variables
- [ ] Add troubleshooting guide

---

## References

- [SPEC-027: Unified Driver Abstraction](./027-unified-driver-abstraction/requirements.spc.spx.md)
- [FlaUI Documentation](https://github.com/FlaUI/FlaUI)
- [Appium Documentation](https://appium.io/docs/)
