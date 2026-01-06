# functional StateVerification
- **id**: FR-004
- **title**: Consistent state verification patterns
- **priority**: high
- **status**: approved
- **tags**: core, verification, assertions

The framework must provide consistent patterns for verifying element and page state.

## capabilities

### ImmediateStateChecks
- **id**: FR-004.1
- **title**: Is* methods for immediate checks

The framework must provide methods to immediately check current state. Immediate checks must return boolean values. Immediate checks must not wait or retry. Immediate checks must not perform logging.

### PollingWaits
- **id**: FR-004.2
- **title**: Wait* methods with polling

The framework must provide methods that poll for expected state. Poll methods must accept timeout parameters. Poll methods must return boolean indicating success/failure. Poll methods must use configurable polling intervals.

### PreconditionChecks
- **id**: FR-004.3
- **title**: Check* methods for preconditions

The framework must provide methods that verify preconditions before actions. Precondition methods must wait for condition with timeout. Precondition methods must throw exceptions on failure. Precondition methods must be called automatically by action methods.

### TestAssertions
- **id**: FR-004.4
- **title**: Assert* methods for verification

The framework must provide assertion methods for test verification. Assertion methods must log all assertion attempts. Assertion methods must throw exceptions on assertion failure. Assertion methods must include expected and actual values in error messages.

### AssertPrerequisites
- **id**: FR-004.4.1
- **title**: Assert methods call Check first

Assert methods must perform prerequisite checks before evaluating the assertion condition. Assert methods must call the corresponding Check method (with waiting/polling) before evaluating. Assert methods must not use Is* methods directly.

### PreferControlAssertions
- **id**: FR-004.5
- **title**: Prefer control object assertions
- **priority**: medium

Test code should prefer using control object assertion methods over external assertion libraries for automatic logging, screenshot capture, and better error messages.

### FailFastOnTimeout
- **id**: FR-004.6
- **title**: Fail fast when timeout expires

When a timeout expires during a Check* or Action method, the method must throw an exception immediately with element ID, timeout value, and current state. Methods must not return false and continue or silently swallow timeouts.
