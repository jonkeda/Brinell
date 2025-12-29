using System.Text;

namespace Brinell.Core.Logging;

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
/// CSV test logger implementation.
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
    
    /// <summary>
    /// Create a CSV logger that writes to the specified file.
    /// </summary>
    public CsvTestLogger(string filePath)
    {
        _logOutput = ParseLogOutput();
        _consoleFormat = ParseConsoleFormat();
        
        // Only set up file if we're writing to it
        if (_logOutput != LogOutput.ConsoleOnly)
        {
            _filePath = filePath;
            
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            
            // Write header if new file
            if (!File.Exists(filePath))
            {
                File.WriteAllText(filePath, Header + Environment.NewLine);
            }
        }
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
    
    /// <summary>
    /// Create a CSV logger with default file path.
    /// </summary>
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
    
    #region Core Log Method
    
    public void Log(
        string testName,
        string pageName,
        string controlId,
        string action,
        string? value,
        string? expectedValue,
        LogResult result,
        string? message)
    {
        WriteEntry(testName, pageName, controlId, action, value ?? "", 
            expectedValue ?? "", result.ToString(), message ?? "");
    }
    
    #endregion
    
    #region Action Logging
    
    public void LogAction(
        string testName,
        string pageName,
        string controlId,
        string action,
        string? value = null)
    {
        WriteEntry(testName, pageName, controlId, action, value ?? "", "", "Ok", "");
    }
    
    #endregion
    
    #region Assertion Logging
    
    public void LogAssertPass(
        string testName,
        string pageName,
        string controlId,
        string assertType,
        string? actualValue,
        string? expectedValue)
    {
        WriteEntry(testName, pageName, controlId, $"Assert.{assertType}", 
            actualValue ?? "", expectedValue ?? "", "Ok", "");
    }
    
    public void LogAssertFail(
        string testName,
        string pageName,
        string controlId,
        string assertType,
        string? actualValue,
        string? expectedValue,
        string? message = null)
    {
        WriteEntry(testName, pageName, controlId, $"Assert.{assertType}", 
            actualValue ?? "", expectedValue ?? "", "Fail", message ?? "");
    }
    
    #endregion
    
    #region Wait Logging
    
    public void LogWait(
        string testName,
        string pageName,
        string controlId,
        string waitType,
        bool success,
        int elapsedMs)
    {
        var result = success ? LogResult.Ok : LogResult.Fail;
        WriteEntry(testName, pageName, controlId, $"Wait.{waitType}", 
            "", "", result.ToString(), $"elapsed={elapsedMs}ms");
    }
    
    #endregion
    
    #region Navigation Logging
    
    public void LogNavigation(
        string testName,
        string sourcePage,
        string targetPage)
    {
        WriteEntry(testName, sourcePage, "", "Navigate", targetPage, "", "Info", "");
    }
    
    public void LogNavigation(
        string testName,
        string pageName,
        string pageId,
        string action,
        string? value = null)
    {
        WriteEntry(testName, pageName, pageId, action, value ?? "", "", "Ok", "");
    }
    
    #endregion
    
    #region Info and Error Logging
    
    public void LogInfo(
        string testName,
        string pageName,
        string message)
    {
        WriteEntry(testName, pageName, "", "Info", "", "", "Info", message);
    }
    
    public void LogError(
        string testName,
        string pageName,
        string controlId,
        string action,
        Exception ex)
    {
        var errorMsg = $"{ex.GetType().Name}: {ex.Message}";
        WriteEntry(testName, pageName, controlId, action, "", "", "Error", errorMsg);
    }
    
    #endregion
    
    #region Internal Methods
    
    private void WriteEntry(
        string testName,
        string pageName,
        string controlId,
        string action,
        string value,
        string expectedValue,
        string result,
        string message)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var csvLine = $"{timestamp};{Escape(testName)};{Escape(pageName)};{Escape(controlId)};{Escape(action)};{Escape(value)};{Escape(expectedValue)};{result};{Escape(message)}";
        
        // Write to file
        if (_logOutput != LogOutput.ConsoleOnly && _filePath != null)
        {
            lock (_lock)
            {
                _buffer.AppendLine(csvLine);
                
                // Auto-flush every 10 entries
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
    
    private void WriteToConsole(
        string timestamp,
        string pageName,
        string controlId,
        string action,
        string value,
        string expectedValue,
        string result,
        string message)
    {
        if (_consoleFormat == ConsoleFormat.Csv)
        {
            // Raw CSV format
            Console.WriteLine($"{timestamp};{pageName};{controlId};{action};{value};{expectedValue};{result};{message}");
        }
        else
        {
            // Formatted human-readable output
            var location = string.IsNullOrEmpty(controlId) 
                ? pageName 
                : $"{pageName}.{controlId}";
            
            var valueDisplay = string.IsNullOrEmpty(value) ? "" : $" {value}";
            var expectedDisplay = string.IsNullOrEmpty(expectedValue) ? "" : $" (expected: {expectedValue})";
            var messageDisplay = string.IsNullOrEmpty(message) ? "" : $"  {message}";
            
            // Color based on result
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = result switch
            {
                "Fail" or "Error" => ConsoleColor.Red,
                "Ok" => ConsoleColor.Green,
                "Info" => ConsoleColor.Cyan,
                _ => ConsoleColor.Gray
            };
            
            // Format: [HH:mm:ss] Location          Action           Value      Result  Message
            Console.WriteLine($"[{timestamp[11..]}] {location,-28} {action,-18}{valueDisplay,-12} {result,-5}{expectedDisplay}{messageDisplay}");
            Console.ForegroundColor = originalColor;
        }
    }
    
    private static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        
        // Escape semicolons and newlines
        return value
            .Replace(";", "\\;")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n");
    }
    
    public void Flush()
    {
        lock (_lock)
        {
            FlushInternal();
        }
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
        if (!_disposed)
        {
            Flush();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
    
    #endregion
}
