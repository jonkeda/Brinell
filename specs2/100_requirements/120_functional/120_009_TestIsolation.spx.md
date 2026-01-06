# functional TestIsolation
- **id**: FR-009
- **title**: Independent test execution
- **priority**: high
- **status**: approved
- **tags**: reliability, isolation

The framework must support independent test execution.

## capabilities

### TestIndependence
- **id**: FR-009.1
- **title**: Order-independent tests

Tests must be executable in any order. Tests must not depend on state from other tests. Tests must be able to run in parallel where appropriate.

### ApplicationLifecycle
- **id**: FR-009.2
- **title**: Per-test application instances

Each test must be able to launch its own application instance. Application instances must be disposed after test completion. The framework may support shared application instances via fixtures.

### SharedApplicationMode
- **id**: FR-009.3
- **title**: Shared application instance mode

The framework must support a shared application mode where a single application instance is used across multiple tests. This is particularly useful for HTML/web testing where browser startup is expensive.

**Configuration:**
A test startup parameter must control application lifecycle mode:
- Per-test mode (default): Each test launches and disposes its own instance
- Shared mode: Single instance shared across tests

**Reset Behavior in Shared Mode:**
Before each test, the framework must attempt to reset the application to a default/known state:
1. Navigate to a known starting point
2. Clear any session state or cached data
3. Verify the application is in expected state

**Recovery on Reset Failure:**
If the reset to default state fails:
1. The application must be stopped/disposed
2. A new application instance must be started
3. The test must proceed with the fresh instance

This ensures test isolation is maintained even with shared instances.

### TestDataIsolation
- **id**: FR-009.4
- **title**: Isolated test data

Tests must be able to create isolated test data. Tests must be able to clean up test data after execution. The framework should support test data fixtures.
