# 12. Standardized Logging - Code Examples

**Parent:** [Standardized Logging](21d12_StandardizedLogging.md)  
**Related:** [21g1 Logging Refactoring](21g1_LoggingRefactoring.md) | [21g3 Console Logging for Agents](21g3_ConsoleLoggingForAgents.md)

---

## 12.1 LogOutput and ConsoleFormat Enums

```csharp
namespace Oravey.UITestFramework.Core.Logging;

/// <summary>
/// Controls where log output is written.
/// </summary>
public enum LogOutput
{
    /// <summary>Write to CSV file only (default for CI/CD).</summary>
    CsvOnly,
    /// <summary>Write to console only (quick debugging).</summary>
    ConsoleOnly,
    /// <summary>Write to both file and console (AI agent runs).</summary>
    Both
}

/// <summary>
/// Controls console output format.
/// </summary>
public enum ConsoleFormat
{
    /// <summary>Human-readable formatted output (default).</summary>
    Formatted,
    /// <summary>Raw CSV format matching file output.</summary>
    Csv
}

/// <summary>
/// Standardized result values for the Result column.
/// </summary>
public enum LogResult
{
    Ok,     // Action succeeded
    Fail,   // Action failed (assertion, timeout)
    Error,  // Exception occurred
    Info,   // Informational message
    Skip    // Skipped action
}
```

---

## 12.2 CsvTestLogger Implementation

```csharp
namespace Oravey.UITestFramework.Core.Logging;

using System.Text;

/// <summary>
/// CSV test logger with configurable output (file, console, or both).
/// Format: Timestamp;TestName;PageName;ControlId;Action;Value;ExpectedValue;Result;Message
/// 
/// Environment variables:
/// - UITEST_LOG_OUTPUT: "csv", "console", or "both" (default: csv)
/// - UITEST_CONSOLE_FORMAT: "formatted" or "csv" (default: formatted)
/// - LOG_OUTPUT_PATH: Directory for log files (default: ./logs)
/// </summary>
public class CsvTestLogger : ITestLogger
{
    private readonly string? _filePath;
    private readonly object _lock = new();
    private readonly StringBuilder _buffer = new();
    private readonly LogOutput _logOutput;
    private readonly ConsoleFormat _consoleFormat;
    private bool _disposed;
    
    private const string Header = "Timestamp;TestName;PageName;ControlId;Action;Value;ExpectedValue;Result;Message";
    
    #region Constructors
    
    public CsvTestLogger(string filePath)
    {
        _logOutput = ParseLogOutput();
        _consoleFormat = ParseConsoleFormat();
        
        if (_logOutput != LogOutput.ConsoleOnly)
        {
            _filePath = filePath;
            
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            
            if (!File.Exists(filePath))
            {
                File.WriteAllText(filePath, Header + Environment.NewLine);
            }
        }
    }
    
    public static CsvTestLogger CreateDefault(string testRunName = "")
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var fileName = string.IsNullOrEmpty(testRunName) 
            ? $"uitest_{timestamp}.csv" 
            : $"uitest_{testRunName}_{timestamp}.csv";
        
        var logDir = Environment.GetEnvironmentVariable("LOG_OUTPUT_PATH") 
            ?? Path.Combine(Directory.GetCurrentDirectory(), "logs");
        
        return new CsvTestLogger(Path.Combine(logDir, fileName));
    }
    
    /// <summary>
    /// Create a logger configured for AI agent runs (both file and formatted console).
    /// </summary>
    public static CsvTestLogger CreateForAgent(string testRunName = "")
    {
        Environment.SetEnvironmentVariable("UITEST_LOG_OUTPUT", "both");
        Environment.SetEnvironmentVariable("UITEST_CONSOLE_FORMAT", "formatted");
        return CreateDefault(testRunName);
    }
    
    private static LogOutput ParseLogOutput()
    {
        return Environment.GetEnvironmentVariable("UITEST_LOG_OUTPUT")?.ToLowerInvariant() switch
        {
            "console" => LogOutput.ConsoleOnly,
            "both" => LogOutput.Both,
            _ => LogOutput.CsvOnly
        };
    }
    
    private static ConsoleFormat ParseConsoleFormat()
    {
        return Environment.GetEnvironmentVariable("UITEST_CONSOLE_FORMAT")?.ToLowerInvariant() switch
        {
            "csv" => ConsoleFormat.Csv,
            _ => ConsoleFormat.Formatted
        };
    }
    
    #endregion
    
    #region ITestLogger Implementation
    
    public void Log(string testName, string pageName, string controlId, string action,
        string? value, string? expectedValue, LogResult result, string? message)
    {
        WriteEntry(testName, pageName, controlId, action, value ?? "", 
            expectedValue ?? "", result.ToString(), message ?? "");
    }
    
    public void LogAction(string testName, string pageName, string controlId,
        string action, string? value = null)
    {
        WriteEntry(testName, pageName, controlId, action, value ?? "", "", "Ok", "");
    }
    
    public void LogAssertPass(string testName, string pageName, string controlId,
        string assertType, string? actualValue, string? expectedValue)
    {
        WriteEntry(testName, pageName, controlId, $"Assert.{assertType}", 
            actualValue ?? "", expectedValue ?? "", "Ok", "");
    }
    
    public void LogAssertFail(string testName, string pageName, string controlId,
        string assertType, string? actualValue, string? expectedValue, string? message = null)
    {
        WriteEntry(testName, pageName, controlId, $"Assert.{assertType}", 
            actualValue ?? "", expectedValue ?? "", "Fail", message ?? "");
    }
    
    public void LogWait(string testName, string pageName, string controlId,
        string waitType, bool success, int elapsedMs)
    {
        var result = success ? LogResult.Ok : LogResult.Fail;
        WriteEntry(testName, pageName, controlId, $"Wait.{waitType}", 
            "", "", result.ToString(), $"elapsed={elapsedMs}ms");
    }
    
    public void LogNavigation(string testName, string sourcePage, string targetPage)
    {
        WriteEntry(testName, sourcePage, "", "Navigate", targetPage, "", "Info", "");
    }
    
    public void LogNavigation(string testName, string pageName, string pageId,
        string action, string? value = null)
    {
        WriteEntry(testName, pageName, pageId, action, value ?? "", "", "Ok", "");
    }
    
    public void LogInfo(string testName, string pageName, string message)
    {
        WriteEntry(testName, pageName, "", "Info", "", "", "Info", message);
    }
    
    public void LogError(string testName, string pageName, string controlId,
        string action, Exception ex)
    {
        var errorMsg = $"{ex.GetType().Name}: {ex.Message}";
        WriteEntry(testName, pageName, controlId, action, "", "", "Error", errorMsg);
    }
    
    #endregion
    
    #region Internal Methods
    
    private void WriteEntry(string testName, string pageName, string controlId,
        string action, string value, string expectedValue, string result, string message)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var csvLine = $"{timestamp};{Escape(testName)};{Escape(pageName)};{Escape(controlId)};" +
                      $"{Escape(action)};{Escape(value)};{Escape(expectedValue)};{result};{Escape(message)}";
        
        // Write to file
        if (_logOutput != LogOutput.ConsoleOnly && _filePath != null)
        {
            lock (_lock)
            {
                _buffer.AppendLine(csvLine);
                if (_buffer.Length > 1000)
                {
                    FlushInternal();
                }
            }
        }
        
        // Write to console
        if (_logOutput != LogOutput.CsvOnly)
        {
            WriteToConsole(timestamp, pageName, controlId, action, value, expectedValue, result, message);
        }
    }
    
    private void WriteToConsole(string timestamp, string pageName, string controlId,
        string action, string value, string expectedValue, string result, string message)
    {
        if (_consoleFormat == ConsoleFormat.Csv)
        {
            Console.WriteLine($"{timestamp};{pageName};{controlId};{action};{value};{expectedValue};{result};{message}");
        }
        else
        {
            // Formatted human-readable output
            var location = string.IsNullOrEmpty(controlId) ? pageName : $"{pageName}.{controlId}";
            var valueDisplay = string.IsNullOrEmpty(value) ? "" : $" {value}";
            var expectedDisplay = string.IsNullOrEmpty(expectedValue) ? "" : $" (expected: {expectedValue})";
            var messageDisplay = string.IsNullOrEmpty(message) ? "" : $"  {message}";
            
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = result switch
            {
                "Fail" or "Error" => ConsoleColor.Red,
                "Ok" => ConsoleColor.Green,
                "Info" => ConsoleColor.Cyan,
                _ => ConsoleColor.Gray
            };
            
            Console.WriteLine($"[{timestamp[11..]}] {location,-28} {action,-18}{valueDisplay,-12} {result,-5}{expectedDisplay}{messageDisplay}");
            Console.ForegroundColor = originalColor;
        }
    }
    
    private static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value.Replace(";", "\\;").Replace("\r", "\\r").Replace("\n", "\\n");
    }
    
    public void Flush()
    {
        lock (_lock) { FlushInternal(); }
    }
    
    private void FlushInternal()
    {
        if (_buffer.Length > 0 && _filePath != null)
        {
            File.AppendAllText(_filePath, _buffer.ToString());
            _buffer.Clear();
        }
    }
    
    public void Dispose()
    {
        if (!_disposed) { Flush(); _disposed = true; }
        GC.SuppressFinalize(this);
    }
    
    #endregion
}
```

---

## 12.3 LoggingExtensions (Log-and-Throw Pattern)

```csharp
namespace Oravey.UITestFramework.Core.Logging;

using Oravey.UITestFramework.Core.Exceptions;

/// <summary>
/// Extension methods for log-and-throw pattern.
/// Ensures exceptions are always logged to CSV before being thrown.
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Log a CheckFailedException and throw it.
    /// </summary>
    public static CheckFailedException ThrowCheckFailed(
        this ITestLogger? logger,
        string testName,
        string pageName,
        string controlId,
        string checkType,
        string message)
    {
        var ex = new CheckFailedException(message, controlId, checkType);
        logger?.LogError(testName, pageName, controlId, $"Check.{checkType}", ex);
        throw ex;
    }
    
    /// <summary>
    /// Log an AssertionException and throw it.
    /// </summary>
    public static AssertionException ThrowAssertionFailed(
        this ITestLogger? logger,
        string testName,
        string pageName,
        string controlId,
        string assertType,
        string? actualValue,
        string? expectedValue,
        string message)
    {
        logger?.LogAssertFail(testName, pageName, controlId, assertType, actualValue, expectedValue, message);
        throw new AssertionException(message);
    }
    
    /// <summary>
    /// Log a PageNotReadyException and throw it.
    /// </summary>
    public static PageNotReadyException ThrowPageNotReady(
        this ITestLogger? logger,
        string testName,
        string pageName,
        string pageId,
        string action,
        string message)
    {
        var ex = new PageNotReadyException(message, pageName);
        logger?.LogError(testName, pageName, pageId, action, ex);
        throw ex;
    }
    
    /// <summary>
    /// Log a PageNotDisplayedException and throw it.
    /// </summary>
    public static PageNotDisplayedException ThrowPageNotDisplayed(
        this ITestLogger? logger,
        string testName,
        string pageName,
        string pageId,
        string action,
        string message)
    {
        var ex = new PageNotDisplayedException(pageName, message);
        logger?.LogError(testName, pageName, pageId, action, ex);
        throw ex;
    }
}

/// <summary>
/// Exception for assertion failures.
/// </summary>
public class AssertionException : Exception
{
    public AssertionException(string message) : base(message) { }
}
```

---

## 12.4 Control Logging Integration

```csharp
namespace Oravey.UITestFramework.Wpf.Controls.Base;

public abstract partial class ControlBase
{
    #region Logging Methods
    
    /// <summary>
    /// Log debug message to console only (not CSV).
    /// </summary>
    protected void LogDebug(string message)
    {
        _context.Log($"[{GetType().Name}] {message}");
    }
    
    /// <summary>
    /// Log action to CSV (after success).
    /// </summary>
    protected void LogAction(string action, string? value = null)
    {
        _context.Logger?.LogAction(_context.TestName, PageName, AutomationId, action, value);
    }
    
    /// <summary>
    /// Log successful assertion to CSV.
    /// </summary>
    protected void LogAssertPass(string assertType, string actual, string expected)
    {
        _context.Logger?.LogAssertPass(_context.TestName, PageName, AutomationId, assertType, actual, expected);
    }
    
    /// <summary>
    /// Log assertion failure and throw.
    /// </summary>
    protected void ThrowAssertionFailed(string assertType, string actual, string expected, string message)
    {
        _context.Logger?.ThrowAssertionFailed(_context.TestName, PageName, AutomationId, 
            assertType, actual, expected, message);
    }
    
    /// <summary>
    /// Log check failure and throw.
    /// </summary>
    protected void ThrowCheckFailed(string checkType, string message)
    {
        _context.Logger?.ThrowCheckFailed(_context.TestName, PageName, AutomationId, checkType, message);
    }
    
    /// <summary>
    /// Log wait result to CSV.
    /// </summary>
    protected void LogWait(string waitType, bool success, int elapsedMs)
    {
        _context.Logger?.LogWait(_context.TestName, PageName, AutomationId, waitType, success, elapsedMs);
    }
    
    #endregion
    
    #region Example: Click with Logging
    
    public virtual void Click()
    {
        var sw = Stopwatch.StartNew();
        if (!WaitForElementVisible())
        {
            LogWait("Visible", false, (int)sw.ElapsedMilliseconds);
            ThrowCheckFailed("Click", $"Element '{AutomationId}' not visible for click.");
        }
        LogWait("Visible", true, (int)sw.ElapsedMilliseconds);
        
        var element = FindElement();
        element?.Click();
        LogAction("Click");  // Log AFTER success
    }
    
    #endregion
    
    #region Example: AssertText with Logging
    
    public virtual void AssertText(string expected, string? message = null)
    {
        CheckVisible(expected: true);
        var actual = GetText();
        if (actual != expected)
        {
            ThrowAssertionFailed("Text", actual, expected,
                message ?? $"Expected text '{expected}' but got '{actual}'.");
        }
        LogAssertPass("Text", actual, expected);
    }
    
    #endregion
}
```

---

## 12.5 Usage Examples

### Running Tests with Console Output

```powershell
# Default: CSV file only
dotnet test --filter "TestLogin"

# AI Agent run: both file and console
$env:UITEST_LOG_OUTPUT = "both"
dotnet test --filter "TestLogin"

# Console output will show:
# [10:15:01.123] LoginPage.txtUsername    Enter              admin        Ok
# [10:15:01.456] LoginPage.txtPassword    Enter              ****         Ok
# [10:15:01.789] LoginPage.btnSubmit      Click                           Ok
# [10:15:02.234] LoginPage                Wait.Ready                      Ok    elapsed=445ms

# Quick debugging: console only, no file
$env:UITEST_LOG_OUTPUT = "console"
dotnet test --filter "TestLogin"

# Raw CSV to console (for piping/parsing)
$env:UITEST_LOG_OUTPUT = "console"
$env:UITEST_CONSOLE_FORMAT = "csv"
dotnet test --filter "TestLogin"
```

### Programmatic Configuration

```csharp
// In test setup for AI agent scenarios
public class LoginTests : WpfUITestBase
{
    protected override void SetupLogging()
    {
        // Use agent-friendly logger with console output
        var logger = CsvTestLogger.CreateForAgent(TestName);
        InitializeContext(context, logger);
    }
}
```

---

## 12.6 PowerShell Log Analysis

```powershell
# analyze-logs.ps1
param(
    [string]$LogFile = "logs/uitest_*.csv",
    [switch]$FailuresOnly,
    [switch]$Summary
)

$logs = Get-ChildItem $LogFile | ForEach-Object {
    Import-Csv -Delimiter ';' $_.FullName
}

if ($FailuresOnly) {
    $logs = $logs | Where-Object { $_.Result -in 'Fail', 'Error' }
}

if ($Summary) {
    Write-Host "`n=== Results by Type ===" -ForegroundColor Cyan
    $logs | Group-Object Result | Select-Object Name, Count | Format-Table
    
    Write-Host "`n=== Failed Actions ===" -ForegroundColor Red
    $logs | Where-Object { $_.Result -in 'Fail', 'Error' } |
        Select-Object Timestamp, PageName, ControlId, Action, Message |
        Format-Table -AutoSize
}
else {
    $logs | Format-Table -AutoSize
}
```

---

*Related: [Application UITest Projects Code Examples](21d13_ApplicationUITestProjects_CodeExamples.md)*
