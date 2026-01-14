# Requirements Document: Test Logging and Diagnostics

## Introduction

The Test Logging and Diagnostics feature provides a unified interface for recording test operations, assertions, wait results, and errors throughout the Brinell UI test automation framework. This foundational capability enables test engineers to track test execution, diagnose failures, and generate reports for analysis.

All logging outputs to CSV format for easy analysis, integration with reporting tools, and post-execution debugging. The logging system follows an entry/exit pattern that captures operation timing, parameters, and results.

## Alignment with Product Vision

This feature directly supports Brinell's core value proposition:

| Product Goal | How Logging Supports It |
|--------------|------------------------|
| **Debuggability** | Comprehensive logging with timestamps, operation details, and error messages |
| **Reliability** | Track wait operations, retry attempts, and state verification results |
| **CI/CD integration** | CSV format enables parsing by CI tools and test reporters |
| **Consistent patterns** | Unified logging API used by all platform implementations |

## Requirements

### REQ-LOG-001: Core Logging Interface

**User Story:** As a test framework developer, I want a unified logging interface, so that all platform implementations log consistently.

#### Acceptance Criteria

1. WHEN a logging operation is called THEN the system SHALL write to the configured output synchronously
2. IF the logger is disposed THEN the system SHALL flush all pending writes before releasing resources
3. WHEN multiple threads log simultaneously THEN the system SHALL serialize writes thread-safely using locks
4. IF no logger is configured THEN the control operations SHALL execute without errors (null-safe)

### REQ-LOG-002: CSV Output Format

**User Story:** As a QA engineer, I want log output in CSV format, so that I can analyze test results in Excel or reporting tools.

#### Acceptance Criteria

1. WHEN the logger writes the first entry THEN the system SHALL write a header row with column names
2. WHEN a log entry is written THEN the system SHALL include: Timestamp, Direction, TestName, PageName, ControlId, Action, Value, Expected, Result, DurationMs, Message
3. WHEN a value contains semicolons, quotes, or newlines THEN the system SHALL escape the value according to CSV standards
4. IF the output directory does not exist THEN the system SHALL create it before writing

### REQ-LOG-003: Entry/Exit Logging Pattern

**User Story:** As a test engineer debugging a failure, I want to see operation entry and exit points, so that I can trace execution flow and identify where failures occur.

#### Acceptance Criteria

1. WHEN an operation starts THEN the system SHALL log an entry (→) with action name and input value
2. WHEN an operation completes successfully THEN the system SHALL log an exit (←) with Success result and duration
3. WHEN an operation throws an exception THEN the system SHALL log an exit (←) with Error result and exception message
4. WHEN an assertion fails THEN the system SHALL log an exit (←) with Fail result, actual value, and expected value

### REQ-LOG-004: Operation Wrapping (Run Pattern)

**User Story:** As a control implementer, I want helper methods to wrap operations with logging, so that I don't have to write logging boilerplate in every method.

#### Acceptance Criteria

1. WHEN `Run(action, operation)` is called THEN the system SHALL log entry before and exit after the operation
2. WHEN `Run<T>(action, value, operation)` is called THEN the system SHALL include the typed value in the entry log
3. WHEN `Run<TResult>(action, operation)` is called THEN the system SHALL return the operation result after logging exit
4. IF the operation throws an exception THEN the system SHALL log the error and re-throw the exception

### REQ-LOG-005: Assertion Wrapping (RunAssert Pattern)

**User Story:** As a control implementer, I want helper methods to wrap assertions with logging, so that assertion results are consistently logged with expected and actual values.

#### Acceptance Criteria

1. WHEN `RunAssert<T>(assertType, expected, getActual)` is called THEN the system SHALL log entry with expected value
2. WHEN the assertion passes THEN the system SHALL log exit with Success, actual value, and expected value
3. WHEN the assertion fails THEN the system SHALL log exit with Fail, actual value, expected value, and throw AssertionException
4. WHEN a custom comparison function is provided THEN the system SHALL use it instead of default equality

### REQ-LOG-006: Log Result Types

**User Story:** As a test analyst, I want log entries categorized by result type, so that I can quickly filter and identify issues.

#### Acceptance Criteria

1. WHEN an operation completes normally THEN the result SHALL be `Success`
2. WHEN an assertion or wait condition is not met THEN the result SHALL be `Fail`
3. WHEN an exception occurs THEN the result SHALL be `Error`
4. WHEN logging informational messages (navigation, info) THEN the result SHALL be `Info`
5. WHEN a potential issue is detected THEN the result SHALL be `Warning`

### REQ-LOG-007: Specialized Logging Methods

**User Story:** As a test framework user, I want convenience methods for common logging scenarios, so that I can log navigation, info, and errors easily.

#### Acceptance Criteria

1. WHEN `LogNavigation(testName, sourcePage, targetPage)` is called THEN the system SHALL log page transition with Info result
2. WHEN `LogInfo(testName, pageName, message)` is called THEN the system SHALL log informational message with Info result
3. WHEN `LogError(testName, pageName, controlId, action, exception)` is called THEN the system SHALL log with Error result and exception message
4. WHEN `LogWait(testName, pageName, controlId, waitType, success, elapsedMs)` is called THEN the system SHALL log wait result with elapsed time

### REQ-LOG-008: Test Base Integration

**User Story:** As a test writer, I want logging automatically configured in my test base class, so that all tests have logging without manual setup.

#### Acceptance Criteria

1. WHEN a test class inherits from UITestBase THEN the system SHALL create a logger with test-specific file path
2. WHEN the test name is provided THEN the system SHALL include it in the log file name with timestamp
3. WHEN the test disposes THEN the system SHALL flush and dispose the logger
4. IF log output path is configured THEN the system SHALL use that path as the logs directory

## Non-Functional Requirements

### Code Architecture and Modularity

- **Single Responsibility**: `ITestLogger` defines the contract; `CsvTestLogger` implements CSV output
- **Modular Design**: Logging is injectable into controls via constructor; controls work with or without logger
- **Dependency Management**: Logger depends only on standard .NET I/O; no external logging frameworks required
- **Clear Interfaces**: `ITestLogger` interface in Brinell.Core; implementations in respective packages

### Performance

| Requirement | Target | Rationale |
|-------------|--------|-----------|
| Log write latency | < 1ms per entry | Should not impact test execution time |
| Lock contention | Minimal | Only lock during actual file write |
| Memory usage | No buffering beyond StreamWriter | Immediate writes, no memory buildup |
| Flush frequency | On-demand + on dispose | Balance between durability and performance |

### Reliability

| Requirement | Implementation |
|-------------|----------------|
| Thread safety | Lock on all write operations |
| Exception safety | Log errors caught and logged, don't break test execution |
| Resource cleanup | Implement IDisposable, flush on dispose |
| File system errors | Create directory if not exists, handle write errors gracefully |

### Usability

| Requirement | Implementation |
|-------------|----------------|
| Zero configuration | Default log path if not specified |
| IntelliSense support | Full XML documentation on all interface methods |
| Discoverable API | Consistent naming: Log*, Run, RunAssert |
| Easy analysis | CSV format opens in Excel, parseable by scripts |

### Maintainability

| Requirement | Implementation |
|-------------|----------------|
| Extensibility | Interface-based design allows custom logger implementations |
| Testability | Logger can be mocked for unit testing controls |
| Documentation | XML docs + usage examples in specification |

## Scope

### In Scope

- `ITestLogger` interface definition with all logging methods
- `CsvTestLogger` implementation with thread-safe CSV output
- `LogResult` enumeration for result categorization
- `Run` and `RunAssert` helper methods in control base classes
- Entry/exit logging pattern with direction indicators
- Duration tracking for all operations
- Integration points for test base classes

### Out of Scope

- Structured logging frameworks (Serilog, NLog, etc.) - CSV is the primary format
- Remote logging / log aggregation services
- Log rotation and archival
- Real-time log streaming
- Log visualization UI
- Performance metrics aggregation beyond timing

## Dependencies

| Dependency | Type | Description |
|------------|------|-------------|
| System.IO | Framework | File writing and path management |
| Brinell.Core.Exceptions | Internal | AssertionException for failed assertions |
| Control base classes | Internal | Run/RunAssert methods in ControlBase |

## Validation Checklist

- [ ] ITestLogger interface defines LogEntry and LogExit methods
- [ ] ITestLogger interface defines LogAssertExit for assertion results
- [ ] CsvTestLogger implements thread-safe CSV writing
- [ ] Run() method handles try/catch and logs entry/exit automatically
- [ ] RunAssert() includes expected value (nullable) in logging
- [ ] CSV output includes Direction column (→ or ←)
- [ ] Duration is captured and logged on exit
- [ ] Timestamps are ISO 8601 format
- [ ] Semicolon delimiter used (CSV standard for European locales)
- [ ] Flush and Dispose properly implemented
