# Tasks Document: Test Logging and Diagnostics

## Task Format

- `[ ]` = Pending, `[-]` = In-progress, `[x]` = Completed
- Each task includes File, Purpose, _Leverage, _Requirements, and _Prompt fields

---

## Phase 1: Core Types and Interfaces

### [ ] 1. Create LogResult enumeration
- **File**: `srcnew/Brinell.Core/Logging/LogResult.cs`
- **Purpose**: Define result categories for log entries
- _Leverage: None (new file)_
- _Requirements: REQ-LOG-006_
- _Prompt: Role: C# developer | Task: Create LogResult enum with Success, Fail, Error, Info, Warning values per design section "Component 1" | Restrictions: Simple enum, no attributes | Success: Enum compiles, values match spec_

### [ ] 2. Replace ITestLogger interface
- **File**: `srcnew/Brinell.Core/Logging/ITestLogger.cs`
- **Purpose**: Replace existing interface with spec-compliant contract including IDisposable
- _Leverage: Design document section "Component 2: ITestLogger Interface (Replacement)"_
- _Requirements: REQ-LOG-001, REQ-LOG-003, REQ-LOG-007_
- _Prompt: Role: C# interface designer | Task: Replace entire ITestLogger with spec design - core Log method, LogEntry, LogExit, LogAssertExit, convenience methods, Flush, IDisposable | Restrictions: Complete replacement, not extension | Success: Interface matches spec exactly, compiles_

---

## Phase 2: Logger Implementations

### [ ] 3. Rewrite NullTestLogger
- **File**: `srcnew/Brinell.Core/Logging/NullTestLogger.cs`
- **Purpose**: Null object pattern implementation of new interface
- _Leverage: New ITestLogger interface_
- _Requirements: REQ-LOG-001_
- _Prompt: Role: C# developer | Task: Rewrite NullTestLogger to implement new ITestLogger with empty method bodies, static Instance property | Restrictions: All methods no-op, Dispose does nothing | Success: Implements all interface methods, compiles_

### [ ] 4. Rewrite ConsoleTestLogger
- **File**: `srcnew/Brinell.Core/Logging/ConsoleTestLogger.cs`
- **Purpose**: Console output implementation for debugging
- _Leverage: New ITestLogger interface_
- _Requirements: REQ-LOG-001_
- _Prompt: Role: C# developer | Task: Rewrite ConsoleTestLogger to implement new ITestLogger with Console.WriteLine for each method, formatted output with direction indicators | Restrictions: Simple console output, no file I/O | Success: All methods output to console with readable format_

### [ ] 5. Create CsvTestLogger implementation
- **File**: `srcnew/Brinell.Core/Logging/CsvTestLogger.cs`
- **Purpose**: Thread-safe CSV file logger per spec
- _Leverage: Design document section "Component 3: CsvTestLogger Implementation", 221_001_Logging spec_
- _Requirements: REQ-LOG-002, REQ-LOG-003_
- _Prompt: Role: C# developer with file I/O experience | Task: Create CsvTestLogger with StreamWriter, lock object, header writing on first entry, semicolon delimiter, ISO 8601 timestamps, direction indicators (→/←), CSV escaping | Restrictions: Thread-safe with lock, create directory if not exists | Success: Produces valid CSV, thread-safe, matches spec output format_

---

## Phase 3: Control Base Integration

### [ ] 6. Add Run methods to MauiControlBase
- **File**: `srcnew/Brinell.Maui/Controls/MauiControlBase.cs`
- **Purpose**: Operation wrapper with automatic entry/exit logging
- _Leverage: Design document "Component 4: Run/RunAssert Helper Methods", IMauiTestContext.Logger_
- _Requirements: REQ-LOG-004_
- _Prompt: Role: C# developer | Task: Add protected Run method overloads - Run(action, operation), Run<T>(action, value, operation), Run<TResult>(action, func), Run<TValue,TResult>(action, value, func) - with Stopwatch timing, LogEntry before, LogExit after, exception handling | Restrictions: Log only if logger available via Context, re-throw exceptions after logging | Success: All 4 Run overloads implemented, timing accurate_

### [ ] 7. Add RunAssert methods to MauiControlBase
- **File**: `srcnew/Brinell.Maui/Controls/MauiControlBase.cs`
- **Purpose**: Assertion wrapper with expected/actual logging
- _Leverage: Design document "Component 4: Run/RunAssert Helper Methods"_
- _Requirements: REQ-LOG-005_
- _Prompt: Role: C# developer | Task: Add protected RunAssert<T> method overloads - one with default equality, one with custom Func<T?,T?,bool> compare - with LogEntry, execute getActual, compare, LogAssertExit with pass/fail, throw AssertionException on fail | Restrictions: Generic constraint where T : IComparable?, nullable support | Success: Both RunAssert overloads work, logs expected/actual correctly_

---

## Phase 4: Update Control Methods

### [ ] 8. Update MauiControlBase Is/Wait/Assert methods to use Run
- **File**: `srcnew/Brinell.Maui/Controls/MauiControlBase.cs`
- **Purpose**: Wrap existing methods with logging
- _Leverage: Run and RunAssert methods from tasks 6-7_
- _Requirements: REQ-LOG-004, REQ-LOG-005_
- _Prompt: Role: C# developer | Task: Modify AssertExists, AssertVisible, AssertEnabled, AssertText, AssertTextContains to use RunAssert pattern | Restrictions: Preserve existing behavior, only add logging wrapper | Success: All Assert methods log entry/exit with expected/actual_

### [ ] 9. Update MauiButtonControl to use Run
- **File**: `srcnew/Brinell.Maui/Controls/MauiButtonControl.cs`
- **Purpose**: Add logging to Click, DoubleClick, RightClick
- _Leverage: Run methods in base class_
- _Requirements: REQ-LOG-004_
- _Prompt: Role: C# developer | Task: Wrap Click, DoubleClick, RightClick operations with Run("Click", ...) etc | Restrictions: Preserve existing behavior | Success: Click operations logged with timing_

### [ ] 10. Update MauiEntryControl to use Run
- **File**: `srcnew/Brinell.Maui/Controls/MauiEntryControl.cs`
- **Purpose**: Add logging to Enter, Clear, text assertions
- _Leverage: Run and RunAssert methods in base class_
- _Requirements: REQ-LOG-004, REQ-LOG-005_
- _Prompt: Role: C# developer | Task: Wrap Enter with Run<string>("Enter", text, ...), Clear with Run, text assertions with RunAssert | Restrictions: Preserve existing behavior | Success: Text operations logged with values_

---

## Phase 5: Test Context Integration

### [ ] 11. Add Logger property to IMauiTestContext
- **File**: `srcnew/Brinell.Maui/Interfaces/IMauiTestContext.cs`
- **Purpose**: Expose logger through test context
- _Leverage: ITestLogger interface_
- _Requirements: REQ-LOG-008_
- _Prompt: Role: C# developer | Task: Add ITestLogger Logger property to IMauiTestContext interface | Restrictions: Property only, implementation in concrete class | Success: Interface has Logger property_

### [ ] 12. Implement Logger in MauiTestContext
- **File**: `srcnew/Brinell.Maui/Context/MauiTestContext.cs`
- **Purpose**: Provide logger instance to controls
- _Leverage: NullTestLogger as default_
- _Requirements: REQ-LOG-008_
- _Prompt: Role: C# developer | Task: Add Logger property with NullTestLogger.Instance as default, add SetLogger method or constructor parameter | Restrictions: Never null, use NullTestLogger if not configured | Success: Logger always available, configurable_

---

## Phase 6: Unit Tests

### [ ] 13. Create CsvTestLogger unit tests
- **File**: `testsnew/Brinell.Core.Tests/Logging/CsvTestLoggerTests.cs`
- **Purpose**: Verify CSV output format and thread safety
- _Leverage: xUnit, temp file handling_
- _Requirements: REQ-LOG-002, REQ-LOG-003_
- _Prompt: Role: C# test developer | Task: Test header written once, CSV escaping, direction indicators, thread safety with parallel writes, flush/dispose | Restrictions: Use temp files, clean up after tests | Success: All CSV format requirements verified_

### [ ] 14. Create Run/RunAssert unit tests
- **File**: `testsnew/Brinell.Maui.Tests/Controls/RunMethodTests.cs`
- **Purpose**: Verify logging wrapper behavior
- _Leverage: Mock ITestLogger, xUnit_
- _Requirements: REQ-LOG-004, REQ-LOG-005_
- _Prompt: Role: C# test developer | Task: Test Run logs entry/exit, duration captured, exceptions logged then rethrown, RunAssert logs expected/actual, throws on fail | Restrictions: Use mock logger to verify calls | Success: All Run/RunAssert behaviors verified_

---

## Phase 7: Build Verification

### [ ] 15. Verify solution builds
- **Command**: `dotnet build srcnew/Brinell.sln`
- **Purpose**: Ensure all changes compile across all targets
- _Requirements: All_
- _Prompt: Role: Build engineer | Task: Build entire solution, fix any compilation errors | Restrictions: All targets must pass (net8.0, net9.0, net10.0) | Success: Build succeeded with 0 errors_

### [ ] 16. Run unit tests
- **Command**: `dotnet test testsnew/Brinell.Core.Tests` and `dotnet test testsnew/Brinell.Maui.Tests`
- **Purpose**: Verify all tests pass
- _Requirements: All_
- _Prompt: Role: QA engineer | Task: Run all unit tests, fix any failures | Restrictions: All tests must pass | Success: All tests pass_
