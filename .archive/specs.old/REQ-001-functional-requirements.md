# REQ-001: Functional Requirements

**Version:** 3.1  
**Status:** Active  
**Last Updated:** January 2026

---

## 1. Purpose

This document specifies the functional requirements for the UI Test Framework. It defines what the framework MUST, SHOULD, and MAY provide to enable automated testing of user interfaces across multiple platforms.

---

## 2. Scope

The framework SHALL support automated testing of:
- Windows desktop applications (WPF) - via FlaUI/UIA3
- Windows Forms applications (WinForms) - via FlaUI/UIA3
- Cross-platform desktop and mobile applications (MAUI) - via Appium
- Web applications (HTML) - via Selenium WebDriver
- Web applications (Blazor) - via Playwright
- 3D game applications (Stride Engine) - via named pipes

Supporting projects:
- Test utilities (Brinell.Testing) - Database fixtures, test helpers
- Mocking support (Brinell.Mocking) - API mocking integration

---

## 3. Core Functional Requirements

### FR-001: Multi-Platform Support

**Priority:** MUST  
**Category:** Core Functionality

The framework MUST support automated testing across multiple platforms using a unified API.

#### FR-001.1: Platform Identification
- The framework MUST provide type-safe platform identification
- The framework MUST distinguish between: Windows (WPF), Windows MAUI, Android, iOS, and Web

#### FR-001.2: Platform Detection
- The framework MUST support runtime platform detection
- The framework MUST provide platform capability queries (IsMobile, IsDesktop, IsWeb)

#### FR-001.3: Platform-Specific Implementations
- Each platform MUST have its own complete implementation
- Platform implementations MUST NOT depend on other platform implementations
- Platform implementations MUST use native automation libraries directly

**Rationale:** See [DES-002: Interface-Based Design](DES-002-interface-based-design.md)

---

### FR-002: Control Object Pattern

**Priority:** MUST  
**Category:** Element Interaction

The framework MUST provide abstraction for UI control interactions.

#### FR-002.1: Control Identification
- Controls MUST be identifiable by platform-specific identifiers:
  - WPF: AutomationProperties.AutomationId
  - MAUI: AutomationId property
  - Web: data-automation-id or id attribute

#### FR-002.2: Control State Verification
- Controls MUST support existence checking
- Controls MUST support visibility checking
- Controls MUST support enabled/disabled state checking
- Controls MUST support clickability checking (visible AND enabled)

#### FR-002.3: Control Actions
- Controls MUST verify preconditions before performing actions
- Controls MUST fail fast with clear error messages when preconditions not met
- Controls MUST log all actions performed

#### FR-002.4: Control Capabilities
- The framework MUST support text input controls
- The framework MUST support clickable controls (buttons, links)
- The framework MUST support toggle controls (checkboxes, switches)
- The framework MUST support selection controls (dropdowns, lists)
- The framework MUST support range controls (sliders, progress bars)
- The framework MUST support collection controls (lists, grids)

Each platform MUST support the controls listed in the platform-specific appendix.

#### FR-002.5: Unified Control Interface Hierarchy

**Priority:** MUST

The framework MUST define a single, unified interface hierarchy for control objects in Core:

```
IControlObject (base)
├── IClickableControl
│   └── IContentControl
├── ITextControl
├── IToggleControl
├── ISelectorControl
├── IRangeControl
├── IItemsControl
└── IContainerControl
```

All platform implementations MUST implement these interfaces.
Platform-specific interfaces MAY extend the core interfaces.

#### FR-002.6: Container-Scoped Control Objects

**Priority:** MUST

Platform control base classes MUST support container-scoped element searching:

1. All control base classes MUST accept an optional container parameter
2. When container is specified, element search MUST be scoped to descendants of that container
3. When container is null, element search MUST search from the application/window root

**Use Cases:**
- Controls inside list item templates
- Controls within data-bound repeaters
- Performance optimization for large UI trees

#### FR-002.7: Scroll-to-Element Support

**Priority:** SHOULD

Scrollable container controls SHOULD support scrolling to make elements visible:

| Method | Description |
|--------|-------------|
| `ScrollToElement(automationId)` | Scroll until element with ID is visible |
| `ScrollToTop()` | Scroll to top of content |
| `ScrollToBottom()` | Scroll to bottom of content |
| `ScrollUp(distance)` | Scroll up by distance |
| `ScrollDown(distance)` | Scroll down by distance |

**Specification:** See [SPEC-003: Control Objects](SPEC-003-control-objects.md)

---

### FR-003: Page Object Pattern

**Priority:** MUST  
**Category:** Test Organization

The framework MUST support page object pattern for organizing test code.

#### FR-003.1: Page Representation
- Each view/page MUST be representable as a page object class
- Page objects MUST encapsulate the structure of a view
- Page objects MUST provide access to controls on the page

#### FR-003.2: Page State
- Page objects MUST support checking if page is displayed
- Page objects MUST support waiting for page to be displayed
- Page objects MUST support checking page readiness (not busy)

#### FR-003.3: Page Navigation
- Page objects MAY provide navigation methods to other pages
- Navigation methods MUST NOT create or return target page objects
- Navigation methods MUST only perform the navigation action

#### FR-003.4: Page Lifecycle
- Tests MUST explicitly create page object instances
- Tests MUST explicitly wait for page readiness after navigation
- Page objects MUST NOT manage application lifecycle

**Specification:** See [SPEC-004: Page Objects](SPEC-004-page-objects.md)  
**Rationale:** See [DES-004: Navigation Pattern](DES-004-navigation-pattern.md)

---

### FR-004: State Verification Pattern

**Priority:** MUST  
**Category:** Test Assertions

The framework MUST provide consistent patterns for verifying element and page state.

#### FR-004.1: Immediate State Checks
- The framework MUST provide methods to immediately check current state
- Immediate checks MUST return boolean values
- Immediate checks MUST NOT wait or retry
- Immediate checks MUST NOT perform logging

#### FR-004.2: Polling Waits
- The framework MUST provide methods that poll for expected state
- Poll methods MUST accept timeout parameters
- Poll methods MUST return boolean indicating success/failure
- Poll methods MUST use configurable polling intervals

#### FR-004.3: Precondition Checks
- The framework MUST provide methods that verify preconditions before actions
- Precondition methods MUST wait for condition with timeout
- Precondition methods MUST throw exceptions on failure
- Precondition methods MUST be called automatically by action methods

#### FR-004.4: Test Assertions
- The framework MUST provide assertion methods for test verification
- Assertion methods MUST log all assertion attempts
- Assertion methods MUST throw exceptions on assertion failure
- Assertion methods MUST include expected and actual values in error messages

#### FR-004.4.1: Assert Method Prerequisites

**Priority:** MUST

Assert methods MUST perform prerequisite checks before evaluating the assertion condition:

- Assert methods MUST call the corresponding Check method (with waiting/polling) before evaluating
- This ensures the UI state has stabilized before the assertion is evaluated
- Assert methods MUST NOT use Is* methods directly (which don't wait)

**Pattern:**
```csharp
// ✅ CORRECT - Assert calls Check first
public void AssertVisible(string? message = null)
{
    CheckVisible(expected: true);  // Waits for element to be visible
    LogAssertPass("Visible", "true", "true");
}

// ❌ INCORRECT - Assert using Is directly (no waiting)
public void AssertVisible(string? message = null)
{
    if (!IsVisible())  // No waiting, may fail prematurely
        throw new AssertionException("...");
}
```

**Rationale:**
- UI state may not be immediately stable after actions
- Check methods provide built-in waiting/polling behavior
- Eliminates flaky tests caused by timing issues
- Consistent behavior across all Assert methods

#### FR-004.5: Prefer Control Object Assertions

**Priority:** SHOULD

Test code SHOULD prefer using control object assertion methods over external assertion libraries:

```csharp
// ✅ PREFERRED - Uses control's Assert method
loginButton.AssertEnabled();
usernameField.AssertTextEquals("admin");

// ❌ DISCOURAGED - External library
Assert.True(loginButton.IsEnabled());  // xUnit
```

**Rationale:**
- Control assertions include automatic logging
- Control assertions capture screenshots on failure
- Control assertions provide better error messages with context
- Removes dependency on external assertion libraries

#### FR-004.6: Fail-Fast on Timeout

**Priority:** MUST

When a timeout expires during a Check* or Action method:
- The method MUST throw an exception immediately
- The method MUST NOT return false and continue
- The method MUST NOT silently swallow the timeout
- The exception MUST include: element ID, timeout value, and current state

**Method Distinction:**
- `Is*` methods return bool immediately (no waiting)
- `Wait*` methods return bool after polling (caller decides on failure)
- `Check*` methods throw on failure (preconditions)
- `Assert*` methods throw on failure (test assertions)
- Action methods throw on failure (Click, Enter, etc.)

**Specification:** See [SPEC-005: State Verification](SPEC-005-state-verification.md)

---

### FR-005: Waiting and Synchronization

**Priority:** MUST  
**Category:** Timing Control

The framework MUST handle asynchronous UI updates and state changes.

#### FR-005.1: Automatic Waiting
- Control actions MUST automatically wait for element readiness
- The framework MUST NOT require manual waits before actions

#### FR-005.2: Configurable Timeouts
- The framework MUST support configurable default timeouts
- The framework MUST support per-operation timeout overrides
- Timeouts MUST be configurable via:
  - Configuration files
  - Environment variables
  - Method parameters

#### FR-005.3: Custom Conditions
- The framework MUST support waiting for custom conditions
- Custom condition waits MUST accept lambda expressions
- Custom condition waits MUST support timeout and polling intervals

#### FR-005.4: Busy State Tracking
- The framework MUST support page-level busy state tracking
- Page objects MUST be able to indicate when asynchronous operations are in progress
- The framework MUST provide methods to wait for busy state to clear

#### FR-005.4.1: BusyPageBase Pattern

**Priority:** SHOULD

Platform implementations SHOULD provide a `BusyPageBase` class:

| Method | Description |
|--------|-------------|
| `IsBusy()` | Returns true if page is showing loading/busy state |
| `IsNotBusy()` | Returns true if page is not busy |
| `WaitForNotBusy(timeout)` | Waits for busy state to clear |
| `IsReady()` override | Returns `IsDisplayed() && !IsBusy()` |

**Implementation Options:**
1. Override `BusyIndicatorId` property (element-based)
2. Override `IsBusy()` method (custom logic)

#### FR-005.5: Synchronous Operation Model

**Priority:** MUST

Control and page object operations MUST be synchronous:
- Action methods (`Click`, `Enter`, `Select`) - Synchronous
- Wait methods (`WaitVisible`, `WaitEnabled`) - Synchronous with internal polling
- Is methods (`IsVisible`, `IsEnabled`) - Synchronous immediate check
- Get/Set methods (`GetText`, `SetText`) - Synchronous

Test base classes SHOULD implement `IAsyncLifetime` for async test setup/teardown.

**Rationale:** Native automation drivers are synchronous. Async wrappers add overhead without benefit. See [DES-001: AD-009](DES-001-architectural-decisions.md#ad-009).

**Specification:** See [SPEC-005: State Verification](SPEC-005-state-verification.md)  
**Design:** See [DES-006: IsBusy Tracking](DES-006-isbusy-tracking.md)

---

### FR-006: Logging and Diagnostics

**Priority:** MUST  
**Category:** Observability

The framework MUST provide comprehensive logging for debugging and analysis.

#### FR-006.1: Structured Logging
- The framework MUST log all test actions in structured format
- Log entries MUST include: timestamp, test name, page name, control ID, action, values, and result
- The framework MUST support CSV log format for machine parsing

#### FR-006.2: Action Logging
- The framework MUST log all control actions (click, type, select, etc.)
- The framework MUST log navigation events
- The framework MUST log assertion results (pass/fail)

#### FR-006.3: Error Logging
- The framework MUST log all errors with full context
- Error logs MUST include control state at time of failure
- Error logs MUST include expected vs. actual values

#### FR-006.4: Screenshot Capture
- The framework MUST support screenshot capture
- The framework SHOULD automatically capture screenshots on test failure
- Screenshots MUST be saved with meaningful names including test name and timestamp

**Specification:** See [SPEC-006: Logging](SPEC-006-logging.md)

---

### FR-007: Platform-Specific Automation

**Priority:** MUST  
**Category:** Platform Integration

The framework MUST integrate with native automation libraries for each platform.

#### FR-007.1: WPF Platform
- The framework MUST use FlaUI for WPF automation
- The framework MUST access UI Automation 3 (UIA3) directly
- The framework MUST support all standard WPF controls

#### FR-007.2: MAUI Platform
- The framework MUST use Appium WebDriver for MAUI automation
- The framework MUST support Windows, Android, and iOS targets
- The framework MUST support platform-specific gestures (mobile)

##### FR-007.2.1: Mobile Gesture Support

**Priority:** MUST (for mobile platforms)

Mobile platform implementations (MAUI for Android/iOS) MUST support:

| Gesture | Method | Description |
|---------|--------|-------------|
| Tap | `Tap()` | Single tap/touch |
| Double Tap | `DoubleTap()` | Two taps in quick succession |
| Long Press | `LongPress(durationMs)` | Extended press with configurable duration |
| Swipe | `Swipe(direction, distance)` | Directional swipe gesture |

**Platform Mapping:**
- `Click()` SHOULD alias to `Tap()` for mobile
- Desktop platforms MAY implement gestures as no-ops or throw NotSupportedException

**SwipeDirection Enum:** `Left`, `Right`, `Up`, `Down`

#### FR-007.3: Web Platform
- The framework MUST support Selenium WebDriver for traditional web automation
- The framework MUST support Playwright for modern web applications and Blazor
- The framework MUST support Chrome, Firefox, Edge, and Safari browsers
- The framework MUST support standard HTML elements and custom components
- Each web driver MUST have its own platform implementation project

#### FR-007.4: WinForms Platform
- The framework MUST use FlaUI for WinForms automation
- The framework MUST access UI Automation 3 (UIA3) directly
- The framework MUST support all standard WinForms controls

#### FR-007.5: Stride Platform
- The framework MUST support Stride Engine 3D game UI testing
- The framework MUST use named pipes for test-to-game communication
- The framework MUST support Stride UIElement hierarchy

#### FR-007.6: Direct Driver Access
- Platform implementations MUST access automation drivers directly
- The framework MUST NOT introduce adapter abstraction layers
- Platform implementations MUST expose native driver capabilities

**Specification:** See [SPEC-007: Platform Implementations](SPEC-007-platform-implementations.md)  
**Rationale:** See [DES-003: Native Driver Access](DES-003-native-driver-access.md)

---

### FR-008: Extensibility

**Priority:** SHOULD  
**Category:** Framework Extension

The framework SHOULD support extension and customization by users.

#### FR-008.1: Virtual Methods
- All base class methods SHOULD be virtual
- Virtual methods MUST allow override in derived classes
- Overrides MUST be able to call base implementation

#### FR-008.2: Custom Controls
- Users MUST be able to create custom control types
- Custom controls MUST be able to inherit from framework base classes
- Custom controls MUST be able to add platform-specific functionality

#### FR-008.3: Custom Pages
- Users MUST be able to create custom page object base classes
- Custom page objects MUST be able to override default behaviors
- Custom page objects MUST maintain framework patterns

#### FR-008.4: Third-Party Control Library Support

**Priority:** SHOULD

For third-party control libraries (e.g., Telerik, DevExpress, Syncfusion), separate NuGet packages SHOULD be created:

```
Brinell.Wpf.Telerik       - Telerik WPF controls
Brinell.Wpf.DevExpress    - DevExpress WPF controls
Brinell.Maui.Syncfusion   - Syncfusion MAUI controls
```

These packages:
- MUST reference the base platform package
- MUST follow the same interface patterns
- SHOULD be maintained separately from core framework
- MAY be community-contributed

**Rationale:** See [DES-005: Virtual Methods](DES-005-virtual-methods.md)

---

### FR-009: Test Isolation

**Priority:** MUST  
**Category:** Test Reliability

The framework MUST support independent test execution.

#### FR-009.1: Test Independence
- Tests MUST be executable in any order
- Tests MUST NOT depend on state from other tests
- Tests MUST be able to run in parallel (where appropriate)

#### FR-009.2: Application Lifecycle
- Each test MUST be able to launch its own application instance
- Application instances MUST be disposed after test completion
- The framework MAY support shared application instances via fixtures

#### FR-009.3: Test Data Isolation
- Tests MUST be able to create isolated test data
- Tests MUST be able to clean up test data after execution
- The framework SHOULD support test data fixtures

---

### FR-010: Error Handling

**Priority:** MUST  
**Category:** Reliability

The framework MUST provide clear, actionable error messages.

#### FR-010.1: Error Messages
- Error messages MUST include element identification (AutomationId)
- Error messages MUST include expected and actual states
- Error messages MUST include timeout values
- Error messages MUST include page context

#### FR-010.2: Exception Types
- The framework MUST provide specific exception types for different failure modes:
  - ElementNotFoundException
  - TimeoutException
  - AssertionException
  - InvalidOperationException

#### FR-010.3: Error Recovery
- The framework SHOULD support retry logic for transient failures
- The framework MUST fail fast for non-recoverable errors
- The framework MUST NOT silently ignore errors

---

---

### FR-011: Dependency Licensing

**Priority:** SHOULD  
**Category:** Compliance

#### FR-011.1: License Requirements
Framework dependencies SHOULD use permissive open source licenses:
- Allow commercial use without fees
- Do not require per-developer or per-seat licensing
- Include at minimum: MIT, Apache 2.0, BSD, LGPL

Commercial/paid dependencies MUST be documented and approved.

#### FR-011.2: Prohibited Dependencies
The framework MUST NOT depend on FluentAssertions library.

**Rationale:** FluentAssertions adopted commercial licensing (post v6.x) requiring paid licenses for commercial use.

**Alternatives:**
- Built-in Assert methods on control objects (preferred)
- Shouldly (MIT license)
- xUnit assertions
- Custom assertion helpers

---

## 4. Optional Functional Requirements

### FR-OPT-001: API Mocking
**Priority:** MAY  
The framework MAY provide integration with API mocking tools (e.g., WireMock).

### FR-OPT-002: Cloud Testing
**Priority:** MAY  
The framework MAY support cloud-based testing services (BrowserStack, Sauce Labs, etc.).

### FR-OPT-003: Visual Testing
**Priority:** MAY  
The framework MAY support visual regression testing.

Platforms MAY provide visual validation capabilities:

| Feature | Description |
|---------|-------------|
| Screenshot capture | Capture element or page screenshots |
| Baseline comparison | Compare against baseline images |
| Difference highlighting | Highlight visual differences |
| Threshold tolerance | Configurable pixel difference tolerance |
| Report generation | Generate HTML/Markdown reports |

### FR-OPT-004: Accessibility Testing
**Priority:** MAY  
The framework MAY integrate with accessibility testing tools.

---

## 5. Requirements Traceability

| Requirement | Specification | Design Document |
|-------------|---------------|-----------------|
| FR-001 | SPEC-001, SPEC-007 | DES-002 |
| FR-002 | SPEC-003 | DES-001 |
| FR-003 | SPEC-004 | DES-004 |
| FR-004 | SPEC-005 | DES-001 |
| FR-005 | SPEC-005 | DES-006 |
| FR-006 | SPEC-006 | - |
| FR-007 | SPEC-007 | DES-003 |
| FR-008 | SPEC-003, SPEC-004 | DES-005 |
| FR-009 | SPEC-001 | - |
| FR-010 | SPEC-002 | - |
| FR-011 | - | - |

---

## 6. Change History

| Version | Date | Changes |
|---------|------|---------|
| 3.2 | Jan 2026 | Added FR-004.4.1 (Assert method prerequisites) |
| 3.1 | Jan 2026 | Added FR-002.5-7 (interfaces, containers, scroll), FR-004.5-6 (assertions, fail-fast), FR-005.4.1-5.5 (BusyPageBase, sync model), FR-007.2.1-7.7 (gestures, platforms), FR-008.4 (third-party controls), FR-011 (licensing), expanded scope |
| 3.0 | Dec 2025 | Added FR-001.3 (platform-specific implementations), FR-007.4 (direct driver access) |
| 2.0 | Dec 2025 | Added FR-006 (logging), FR-010 (error handling) |
| 1.0 | Nov 2025 | Initial requirements |

---

*Next: [REQ-002: Non-Functional Requirements](REQ-002-non-functional-requirements.md)*
