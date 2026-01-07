# functional StateVerification
- **id**: FR-300
- **title**: State Verification Methods
- **priority**: high
- **status**: draft
- **category**: State and Verification

The framework must provide consistent method patterns for verifying element and page state.

## capabilities

### MethodNamingConventions
- **id**: FR-300.1
- **title**: Method naming conventions

All state verification methods must follow consistent naming:

| Prefix | Purpose | Behavior |
|--------|---------|----------|
| Is* | Immediate state query | Returns value or null, no waiting |
| Get* | Immediate value retrieval | Returns value or null, no waiting |
| Wait* | Poll for expected state | Returns boolean, polls with timeout |
| Check* | Precondition verification | Throws on failure, waits with timeout |
| Assert* | Test assertion | Throws on failure, logs result |

### IsMethods
- **id**: FR-300.2
- **title**: Is* method behavior

Is* methods perform immediate state checks:

**Behavior:**
- Single immediate check, no waiting
- No polling or retries
- No exceptions thrown
- No logging

**Return semantics:**
- Non-null value = element exists, state determined
- Null = element does not exist

**Examples:**
- IsExists → true/false/null
- IsVisible → true/false/null
- IsEnabled → true/false/null
- IsChecked → true/false/null

### GetMethods
- **id**: FR-300.3
- **title**: Get* method behavior

Get* methods retrieve values immediately:

**Behavior:**
- Single immediate retrieval, no waiting
- No polling or retries
- No exceptions thrown
- No logging

**Return semantics:**
- Value = element exists, value retrieved
- Null = element does not exist OR value is empty/not applicable

**Examples:**
- GetText → text content or null
- GetValue → current value or null
- GetAttribute(name) → attribute value or null
- GetSelectedItem → selected item or null

### WaitMethods
- **id**: FR-300.4
- **title**: Wait* method behavior

Wait* methods poll for expected state:

**Behavior:**
- Accept nullable expected parameter for skip-on-null
- Poll repeatedly until condition met or timeout
- Configurable polling interval
- Return boolean result
- No exceptions thrown on timeout

**Signature pattern:**
```
bool WaitExists(bool? expected, int? timeoutMs = null)
bool WaitVisible(bool? expected, int? timeoutMs = null)
bool WaitEnabled(bool? expected, int? timeoutMs = null)
```

**Return semantics:**
- true = condition met within timeout (or expected was null)
- false = timeout expired, condition not met

**Null handling:**
- When expected is null, return true immediately (skip operation)
- Enables conditional waiting without explicit null checks

### CheckMethods
- **id**: FR-300.5
- **title**: Check* method behavior

Check* methods verify preconditions:

**Behavior:**
- Wait for condition with timeout
- Throw exception if condition not met
- Used internally before actions
- May be called explicitly

**Return semantics:**
- Returns normally = condition met
- Throws = condition not met after timeout

**Examples:**
- CheckExists(timeout) → void or throws
- CheckVisible(timeout) → void or throws
- CheckEnabled(timeout) → void or throws
- CheckClickable(timeout) → void or throws

### AssertMethods
- **id**: FR-300.6
- **title**: Assert* method behavior

Assert* methods verify test expectations:

**Behavior:**
- Accept nullable expected parameter for skip-on-null
- Log the assertion attempt
- Compare expected vs actual
- Throw on mismatch with detailed message
- Include expected and actual values in exception

**Signature pattern:**
```
void AssertExists(bool? expected, string? message = null, int? timeoutMs = null)
void AssertVisible(bool? expected, string? message = null, int? timeoutMs = null)
void AssertText(string? expected, string? message = null, int? timeoutMs = null)
```

**Pattern:**
1. If expected is null, return immediately (skip)
2. Call corresponding Check* method first
3. Retrieve actual value
4. Compare with expected
5. Log result
6. Throw AssertionException if mismatch

**Null handling:**
- When expected is null, do nothing (skip operation)
- Enables conditional assertions without explicit null checks

### AssertCallsCheck
- **id**: FR-300.7
- **title**: Assert methods call Check first

Assert methods must call the corresponding Check method:
- AssertVisible calls CheckVisible internally
- Ensures element is in testable state before comparison
- Separates "can we test this?" from "is the value correct?"

### PreferControlAssertions
- **id**: FR-300.8
- **title**: Prefer control object assertions

Tests should prefer control object assertions over external libraries:
- Framework assertions automatically log
- Framework assertions provide consistent error messages
- Framework assertions include control context
- Framework assertions capture screenshots on failure

---

## relationships

- Methods implemented by [FR-100 Controls](120_100_ControlObject.spx.md)
- Waiting behavior defined in [FR-301 Waiting](120_301_WaitingSynchronization.spx.md)
- Assertion exceptions defined in [FR-600 Exceptions](120_600_ExceptionStrategy.spx.md)
- Logging behavior defined in [FR-500 Logging](120_500_Logging.spx.md)

---

## constraints

- Method naming must be consistent across all control types
- Is*/Get* methods must never throw exceptions for missing elements
- Assert* methods must always log before throwing
- Check* methods must not log (internal use)
