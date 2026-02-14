# functional TestContext
- **id**: FR-400
- **title**: Test Context Management
- **priority**: high
- **status**: draft
- **category**: Execution Context

The framework must provide test context management for controlling test execution lifecycle, configuration, and shared services.

## capabilities

### ContextDefinition
- **id**: FR-400.1
- **title**: Test context definition

A test context provides:
- Configuration access (TimeoutSettings)
- Logger (ITestLogger)
- Screenshot service
- Driver/session management
- Navigation methods

Tests receive a context and use it to create page objects.

**Note:** Context does NOT track current page. Controls receive their page reference via constructor parameter, enabling explicit page scoping without global state.

### ContextCreationModes
- **id**: FR-400.2
- **title**: Context creation modes

The framework must support context creation modes:

| Mode | Description | Isolation |
|------|-------------|-----------|
| PerTest | New context for each test | Full isolation |
| PerFixture | Shared context for test class | Shared state within class |
| PerRun | Single context for entire run | Minimal isolation |

**PerTest (default):**
- Fresh application/browser instance
- Clean state guaranteed
- Highest isolation, slowest execution

**PerFixture:**
- Shared across tests in same class
- Application reset between tests
- Balance of isolation and speed

**PerRun:**
- Single instance for all tests
- Manual state management required
- Fastest execution, lowest isolation

### ContextConfiguration
- **id**: FR-400.3
- **title**: Context configuration access

Context must provide access to configuration:
- Default timeout
- Polling interval
- Screenshot settings
- Log settings
- Platform-specific settings

Configuration can be read but not modified after context creation.

### ContextServices
- **id**: FR-400.4
- **title**: Context-provided services

Context must provide services:

| Service | Description |
|---------|-------------|
| CaptureScreenshot | Capture and save screenshot |
| Logger | Access to logging system |
| Configuration | Access to settings |

### ContextDisposal
- **id**: FR-400.5
- **title**: Context disposal and cleanup

When context is disposed:
1. Close application/browser
2. Release driver resources
3. Flush pending logs
4. Save any pending screenshots
5. Clean temporary files

Disposal must be exception-safe:
- Log disposal errors, don't throw
- Continue cleanup even if step fails
- Ensure all resources released

### ContextLifecycleHooks
- **id**: FR-400.6
- **title**: Test lifecycle hooks

Context must integrate with test framework lifecycle:

| Hook | Timing |
|------|--------|
| Initialize | Before first test using context |
| BeforeTest | Before each test |
| AfterTest | After each test |
| Dispose | After last test using context |

Hooks enable:
- Application launch on initialize
- State reset before test
- Screenshot on failure after test
- Cleanup on dispose

### TestBasePattern
- **id**: FR-400.7
- **title**: Test base class pattern

The framework should provide a test base class that:
- Manages context lifecycle
- Implements async lifecycle interfaces
- Provides access to context
- Captures screenshots on failure
- Handles common setup/teardown

Tests inherit from base class for standard behavior.

---

## relationships

- Context uses [FR-401 Configuration](120_401_Configuration.spx.md)
- Context manages [FR-011 Driver](120_011_DriverAbstraction.spx.md) lifecycle
- Context provides [FR-500 Logging](120_500_Logging.spx.md) services
- Context provides [FR-502 Screenshot](120_502_ScreenshotEvidence.spx.md) services
- Test isolation modes in [FR-700 Test Isolation](120_700_TestIsolation.spx.md)

---

## constraints

- Context must be created before any page/control operations
- Context must be disposed to release resources
- Context state must not leak between tests (in PerTest mode)
- Multiple contexts may exist in parallel (for parallel tests)
