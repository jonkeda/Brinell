# functional DriverAbstraction
- **id**: FR-011
- **title**: Driver Abstraction Layer
- **priority**: high
- **status**: draft
- **category**: Platform and Technology

The framework must provide an abstraction layer between test code and automation drivers, allowing different drivers to be used interchangeably within the same technology.

## capabilities

### DriverAdapterInterface
- **id**: FR-011.1
- **title**: Driver adapter interface

The framework must define a driver adapter interface with operations:

| Operation | Description |
|-----------|-------------|
| FindElement | Locate a single element by locator |
| FindElements | Locate multiple elements by locator |
| Navigate | Navigate to URL or view |
| CaptureScreenshot | Capture current screen/window |
| Execute | Execute driver-specific command |
| Dispose | Release driver resources |

Properties:
- Technology identifier
- Driver name/version
- Session state

### ElementAdapterInterface
- **id**: FR-011.2
- **title**: Element adapter interface

The framework must define an element adapter interface with operations:

| Operation | Description |
|-----------|-------------|
| GetText | Get element text content |
| GetAttribute | Get element attribute value |
| IsDisplayed | Check if element is visible |
| IsEnabled | Check if element is enabled |
| Click | Perform click action |
| SendKeys | Send text input |
| Clear | Clear element content |
| FindElement | Find child element |
| FindElements | Find child elements |

Properties:
- Element identifier
- Tag/control type name

### TechnologyAdapters
- **id**: FR-011.3
- **title**: Technology-specific adapters

Each supported technology must have at least one driver adapter:

| Technology | Adapters |
|------------|----------|
| Web | Selenium adapter, Playwright adapter |
| MAUI/Mobile | Appium adapter |
| WPF | FlaUI adapter |
| WinForms | FlaUI adapter |
| Stride | Named pipe adapter |

### DriverSelection
- **id**: FR-011.4
- **title**: Driver selection mechanism

Driver selection must be configurable:
- Configuration file setting
- Environment variable
- Programmatic selection at context creation
- Default driver per technology when not specified

Selection happens once at context/session creation, not per operation.

### TechnologyExtensions
- **id**: FR-011.5
- **title**: Technology-specific extensions

Adapters may expose technology-specific operations beyond the common interface:

**Web-specific:**
- Frame/iframe switching
- Script execution
- Cookie management
- Alert handling

**Mobile-specific:**
- Gesture execution
- Orientation control
- App lifecycle control

**Desktop-specific:**
- Window enumeration
- Keyboard simulation

These extensions are accessed through technology-specific interfaces that extend the base adapter.

---

## constraints

- Test code must not reference specific driver types directly
- Driver selection must not require code changes to tests
- Adapters must handle driver-specific exceptions and translate to framework exceptions
- Adapters must be thread-safe for parallel test execution

---

## anti-patterns

The following patterns are prohibited:

1. **Direct driver access in tests** - Tests must use framework abstractions
2. **Driver-conditional logic** - Tests must not branch based on which driver is active
3. **Driver-specific locators** - Locators must work across all drivers for a technology
