# functional LogFileManagement
- **id**: FR-501
- **title**: Log File Management
- **priority**: high
- **status**: draft
- **category**: Logging and Evidence

The framework must provide log file management for persisting test logs to files.

## capabilities

### LogFileModes
- **id**: FR-501.1
- **title**: Log file modes

The framework must support log file modes:

| Mode | Description |
|------|-------------|
| None | Console/output only, no file |
| PerTest | One log file per test |
| PerRun | Single log file for entire test run |

Mode selected via configuration.

### LogFileConfiguration
- **id**: FR-501.2
- **title**: Log file configuration

Configurable log file settings:

| Setting | Description |
|---------|-------------|
| OutputDirectory | Directory for log files |
| MinimumLevel | Minimum level to write |
| IncludeTimestamp | Include timestamp in filename |
| MaxFileSizeMB | Maximum file size before rotation |
| RetentionDays | Days to keep old logs |

### PerTestLogFiles
- **id**: FR-501.3
- **title**: Per-test log file naming

In PerTest mode:
- One file per test
- Filename pattern: `{TestClass}_{TestMethod}_{Timestamp}.log`
- Clear separation of test output
- Easy to attach to test results

Advantages:
- Parallel test safe (separate files)
- Easy to find specific test output
- Natural attachment to test results

### PerRunLogFiles
- **id**: FR-501.4
- **title**: Per-run log file format

In PerRun mode:
- Single file for all tests
- Filename pattern: `TestRun_{Timestamp}.log`
- Test markers separate output
- Complete run history in one file

Test markers:
```
=== TEST START: MyTestClass.MyTestMethod ===
[log entries]
=== TEST END: MyTestClass.MyTestMethod [PASSED/FAILED] ===
```

### ThreadSafeLogging
- **id**: FR-501.5
- **title**: Thread-safe logging

Logging must be thread-safe:
- PerTest: Naturally safe (separate files)
- PerRun: Synchronized or buffered writes
- No interleaved log entries
- No file corruption

### LogFileRotation
- **id**: FR-501.6
- **title**: Log file rotation

Long-running scenarios require rotation:
- Size-based rotation when limit reached
- Automatic creation of new file
- Retention policy for old files
- Cleanup of expired files

### LogFileAccess
- **id**: FR-501.7
- **title**: Log file path access

Tests must be able to access log file path:
```
// Pseudocode
path = context.GetLogFilePath()
```

Enables:
- Attaching to test results
- Custom post-processing
- Test result reporting

### FlushBehavior
- **id**: FR-501.8
- **title**: Log flush behavior

Log flushing requirements:
- Flush on test completion
- Flush on error
- Flush on context disposal
- Configurable auto-flush interval

Ensures logs available even on crash.

---

## relationships

- Implements file storage for [FR-500 Logging](120_500_Logging.spx.md)
- Configuration via [FR-401 Configuration](120_401_Configuration.spx.md)
- Lifecycle managed by [FR-400 Test Context](120_400_TestContext.spx.md)

---

## constraints

- Log files must not be locked during test execution
- Log files must survive test process crash
- Log files must be readable while test is running
- File I/O must not block test execution
