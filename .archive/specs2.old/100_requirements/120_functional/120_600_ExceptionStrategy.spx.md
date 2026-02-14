# functional ExceptionStrategy
- **id**: FR-600
- **title**: Exception Strategy
- **priority**: high
- **status**: draft
- **category**: Error Handling

The framework must provide a unified exception strategy with rich context and actionable error messages.

## capabilities

### ExceptionHierarchy
- **id**: FR-600.1
- **title**: Exception type hierarchy

The framework must define an exception hierarchy:

```
BrinellException (base for all framework exceptions)
├── ControlException (control-related failures)
│   ├── ControlNotFoundException
│   ├── ControlNotVisibleException
│   ├── ControlNotEnabledException
│   ├── ControlTimeoutException
│   ├── ControlAssertionException
│   └── ControlReadOnlyException
├── PageException (page-related failures)
│   ├── PageNotReadyException
│   └── PageNavigationException
├── DriverException (driver/automation failures)
│   ├── DriverCommunicationException
│   └── DriverSessionException
└── ConfigurationException (configuration failures)
```

### RichExceptionContext
- **id**: FR-600.2
- **title**: Rich exception context

All framework exceptions must include:

| Property | Description |
|----------|-------------|
| Locator | Element locator (if applicable) |
| PageName | Page where error occurred |
| TimeoutMs | Timeout value used |
| Timestamp | When error occurred |
| ScreenshotPath | Path to failure screenshot |
| Data | Additional diagnostic data |

### ActionableMessages
- **id**: FR-600.3
- **title**: Actionable error messages

Exception messages must be actionable:
- What went wrong
- What was expected
- What was actual
- Suggestions for resolution

Example:
```
Element not found: Button with AutomationId 'submitBtn'
  Page: LoginPage
  Waited: 30000ms
  Suggestion: Verify the AutomationId is correct and the element 
              is rendered. Check if the element is inside a 
              container that requires scrolling.
```

### ExceptionChaining
- **id**: FR-600.4
- **title**: Exception chaining

Framework exceptions must preserve original exceptions:
- Driver exceptions as InnerException
- Full stack trace preserved
- Original exception type identifiable

### LogBeforeThrow
- **id**: FR-600.5
- **title**: Log exceptions before throwing

All exceptions must be logged before being thrown:
- Log at Error level
- Include all context
- Ensure visibility even if exception swallowed

### ScreenshotOnException
- **id**: FR-600.6
- **title**: Screenshot on exception

Framework exceptions should trigger screenshots:
- Capture before exception propagates
- Attach path to exception
- Configurable (default: enabled)

### SpecificExceptionTypes
- **id**: FR-600.7
- **title**: Specific exception type usage

Exception types for specific scenarios:

| Exception | When |
|-----------|------|
| ControlNotFoundException | Element not found in UI tree |
| ControlNotVisibleException | Element exists but not visible |
| ControlNotEnabledException | Element visible but disabled |
| ControlTimeoutException | Operation timed out |
| ControlAssertionException | Assertion failed |
| ControlReadOnlyException | Cannot modify read-only element |
| PageNotReadyException | Page did not become ready |
| PageNavigationException | Navigation failed |
| DriverCommunicationException | Lost connection to driver |
| DriverSessionException | Invalid or expired session |
| ConfigurationException | Invalid configuration |

---

## relationships

- Used by [FR-302 Assertions](120_302_Assertions.spx.md)
- Used by [FR-301 Waiting](120_301_WaitingSynchronization.spx.md)
- Logged by [FR-500 Logging](120_500_Logging.spx.md)
- Screenshots by [FR-502 Screenshot Evidence](120_502_ScreenshotEvidence.spx.md)

---

## constraints

- All framework exceptions must inherit from base exception
- Exception messages must not include sensitive data
- Exception construction must not fail
- InnerException must always be preserved when wrapping
