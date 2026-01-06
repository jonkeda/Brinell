# functional Logging
- **id**: FR-500
- **title**: Logging System
- **priority**: high
- **status**: draft
- **category**: Logging and Evidence

The framework must provide comprehensive logging for debugging and analysis of test execution.

## capabilities

### LoggerInterface
- **id**: FR-500.1
- **title**: Logger interface

The framework must define a logger interface with:

| Method | Description |
|--------|-------------|
| Run | Execute operation with automatic logging |
| RunAsync | Execute async operation with automatic logging |

Logger wraps operations to provide:
- Entry logging (method start)
- Exit logging (method complete or failed)
- Duration tracking
- Call stack depth tracking
- Exception capture

### OperationWrapping
- **id**: FR-500.2
- **title**: Operation wrapping pattern

All public methods must use logger operation wrapping:

```
// Pseudocode
logger.Run("Click", parameters, () => {
    // Actual click implementation
})
```

The logger manages:
- Try/catch/finally for consistent logging
- Success logged on normal completion
- Failure logged on exception (then re-thrown)
- Duration measurement
- Call stack depth

### CallStackTracking
- **id**: FR-500.3
- **title**: Call stack tracking

Logger must track nested operation calls:
- Maintain call stack depth per logger instance
- Log depth with each entry
- Stack unwinds on exception (failure at each level)
- Enables understanding of operation hierarchy

Example output:
```
→ Click [Button: submit] depth=1
  → WaitClickable [Button: submit] depth=2
  ← WaitClickable [Success] 50ms depth=2
← Click [Success] 60ms depth=1
```

### LogFormat
- **id**: FR-500.4
- **title**: Structured log format

Logs must be structured for machine parsing:

| Field | Description |
|-------|-------------|
| Timestamp | When operation occurred |
| Direction | → (entry) or ← (exit) |
| Method | Operation name |
| Control | Control identifier |
| ControlType | Type of control |
| Parameters | Operation parameters |
| Status | Success, Failure, or entry |
| DurationMs | Operation duration (exit only) |
| Error | Error message (failure only) |
| Depth | Call stack depth |

Format: CSV or similar delimited format for easy parsing.

### LogLevels
- **id**: FR-500.5
- **title**: Log levels

Support standard log levels:

| Level | Use For |
|-------|---------|
| Trace | Detailed diagnostic information |
| Debug | Internal framework information |
| Information | Normal operation flow |
| Warning | Unexpected but recoverable situations |
| Error | Failures and exceptions |

Default level: Information.
Level configurable via configuration.

### ParameterSanitization
- **id**: FR-500.6
- **title**: Parameter sanitization

Sensitive parameters must be sanitized before logging:
- Password fields → "********"
- Long strings → truncated with length indicator
- Null values → "null"
- Binary data → "[binary data]"

Sanitization automatic based on parameter name patterns.

### ControlContext
- **id**: FR-500.7
- **title**: Control context in logs

Logs must include control context:
- Control identifier (locator or name)
- Control type
- Page context (when available)

This enables filtering logs by control or page.

### PerformanceRequirements
- **id**: FR-500.8
- **title**: Logging performance

Logging overhead must be minimal:
- Less than 1ms per logged operation
- Minimal memory allocation
- Lazy parameter serialization
- Async file writing (where applicable)

---

## relationships

- Log files managed by [FR-501 Log File Management](120_501_LogFileManagement.spx.md)
- Used by [FR-302 Assertions](120_302_Assertions.spx.md)
- Error logging with [FR-600 Exceptions](120_600_ExceptionStrategy.spx.md)
- Configuration in [FR-401 Configuration](120_401_Configuration.spx.md)

---

## constraints

- Logging must not throw exceptions (log errors internally)
- Logging must not significantly impact test performance
- Log format must be consistent across all operations
- Sensitive data must never appear in logs
