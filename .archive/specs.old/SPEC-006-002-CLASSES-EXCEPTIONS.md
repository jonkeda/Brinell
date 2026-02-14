# SPEC-006-002n: Exception Classes

**Version:** 1.0  
**Status:** Final  
**Date:** January 2026

---

## 1. BrinellException (Base)

```csharp
namespace Brinell.Core;

/// <summary>
/// Base exception for all Brinell framework exceptions.
/// </summary>
public class BrinellException : Exception
{
    public string? TestName { get; }
    public string? PageName { get; }
    public string? ControlId { get; }
    public byte[]? Screenshot { get; }

    public BrinellException(string message)
        : base(message)
    {
    }

    public BrinellException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public BrinellException(
        string message,
        string? testName,
        string? pageName,
        string? controlId,
        byte[]? screenshot = null)
        : base(message)
    {
        TestName = testName;
        PageName = pageName;
        ControlId = controlId;
        Screenshot = screenshot;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine(Message);
        
        if (!string.IsNullOrEmpty(TestName))
            sb.AppendLine($"Test: {TestName}");
        if (!string.IsNullOrEmpty(PageName))
            sb.AppendLine($"Page: {PageName}");
        if (!string.IsNullOrEmpty(ControlId))
            sb.AppendLine($"Control: {ControlId}");
        if (Screenshot != null)
            sb.AppendLine($"Screenshot: {Screenshot.Length} bytes captured");
        
        if (InnerException != null)
        {
            sb.AppendLine();
            sb.AppendLine("Inner Exception:");
            sb.AppendLine(InnerException.ToString());
        }
        
        return sb.ToString();
    }
}
```

---

## 2. AssertionException

```csharp
namespace Brinell.Core;

/// <summary>
/// Thrown when an assertion fails.
/// </summary>
public class AssertionException : BrinellException
{
    public string AssertType { get; }
    public string? ActualValue { get; }
    public string? ExpectedValue { get; }

    public AssertionException(
        string message,
        string assertType,
        string? actual,
        string? expected)
        : base(message)
    {
        AssertType = assertType;
        ActualValue = actual;
        ExpectedValue = expected;
    }

    public AssertionException(
        string message,
        string assertType,
        string? actual,
        string? expected,
        string? testName,
        string? pageName,
        string? controlId,
        byte[]? screenshot = null)
        : base(message, testName, pageName, controlId, screenshot)
    {
        AssertType = assertType;
        ActualValue = actual;
        ExpectedValue = expected;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Assertion Failed: {AssertType}");
        sb.AppendLine($"Expected: {ExpectedValue ?? "(null)"}");
        sb.AppendLine($"Actual: {ActualValue ?? "(null)"}");
        sb.AppendLine();
        sb.AppendLine(base.ToString());
        return sb.ToString();
    }
}
```

---

## 3. CheckException

```csharp
namespace Brinell.Core;

/// <summary>
/// Thrown when a check/precondition fails.
/// </summary>
public class CheckException : BrinellException
{
    public string CheckType { get; }

    public CheckException(string message, string checkType)
        : base(message)
    {
        CheckType = checkType;
    }

    public CheckException(
        string message,
        string checkType,
        string? testName,
        string? pageName,
        string? controlId,
        byte[]? screenshot = null)
        : base(message, testName, pageName, controlId, screenshot)
    {
        CheckType = checkType;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Check Failed: {CheckType}");
        sb.AppendLine();
        sb.AppendLine(base.ToString());
        return sb.ToString();
    }
}
```

---

## 4. TimeoutException

```csharp
namespace Brinell.Core;

/// <summary>
/// Thrown when a wait operation times out.
/// </summary>
public class WaitTimeoutException : BrinellException
{
    public string WaitType { get; }
    public int TimeoutMs { get; }

    public WaitTimeoutException(string message, string waitType, int timeoutMs)
        : base(message)
    {
        WaitType = waitType;
        TimeoutMs = timeoutMs;
    }

    public WaitTimeoutException(
        string message,
        string waitType,
        int timeoutMs,
        string? testName,
        string? pageName,
        string? controlId,
        byte[]? screenshot = null)
        : base(message, testName, pageName, controlId, screenshot)
    {
        WaitType = waitType;
        TimeoutMs = timeoutMs;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Wait Timeout: {WaitType}");
        sb.AppendLine($"Timeout: {TimeoutMs}ms");
        sb.AppendLine();
        sb.AppendLine(base.ToString());
        return sb.ToString();
    }
}
```

---

## 5. ElementNotFoundException

```csharp
namespace Brinell.Core;

/// <summary>
/// Thrown when an element cannot be found.
/// </summary>
public class ElementNotFoundException : BrinellException
{
    public ControlLocator Locator { get; }

    public ElementNotFoundException(string message, ControlLocator locator)
        : base(message)
    {
        Locator = locator;
    }

    public ElementNotFoundException(
        string message,
        ControlLocator locator,
        string? testName,
        string? pageName,
        byte[]? screenshot = null)
        : base(message, testName, pageName, locator.ToString(), screenshot)
    {
        Locator = locator;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Element Not Found: {Locator}");
        sb.AppendLine();
        sb.AppendLine(base.ToString());
        return sb.ToString();
    }
}
```

---

## 6. ControlNotSupportedException

```csharp
namespace Brinell.Core;

/// <summary>
/// Thrown when a control type is not supported on the platform.
/// </summary>
public class ControlNotSupportedException : BrinellException
{
    public Type ControlType { get; }
    public string Platform { get; }

    public ControlNotSupportedException(Type controlType, string platform)
        : base($"Control type '{controlType.Name}' is not supported on platform '{platform}'.")
    {
        ControlType = controlType;
        Platform = platform;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Control Not Supported: {ControlType.Name}");
        sb.AppendLine($"Platform: {Platform}");
        sb.AppendLine();
        sb.AppendLine(base.ToString());
        return sb.ToString();
    }
}
```

---

## 7. ITestLogger Interface

```csharp
namespace Brinell.Core;

/// <summary>
/// Interface for test logging and exception handling.
/// </summary>
public interface ITestLogger
{
    // Basic logging
    void Log(string? testName, string? pageName, string? controlId, string message);
    
    // Action logging
    void LogAction(string? testName, string? pageName, string? controlId, 
        string action, string? parameter = null);
    
    // Wait logging
    void LogWait(string? testName, string? description, bool success, int elapsedMs);
    
    // Assertion logging
    void LogAssertPass(string? testName, string? pageName, string? controlId,
        string assertType, string? actual, string? expected);
    
    // Test lifecycle
    void StartTest(string testName);
    void EndTest(string testName, bool success);
    
    // Exception throwing with screenshot capture
    void ThrowAssertionFailed(
        string? testName, string? pageName, string? controlId,
        string assertType, string? actual, string? expected,
        string message, ITestContext context);
    
    void ThrowCheckFailed(
        string? testName, string? pageName, string? controlId,
        string checkType, string message, ITestContext context);
    
    void ThrowTimeout(
        string? testName, string? pageName, string? controlId,
        string waitType, int timeoutMs, string message, ITestContext context);
}
```

---

## 8. CsvTestLogger Implementation

```csharp
namespace Brinell.Core;

/// <summary>
/// CSV file-based test logger implementation.
/// </summary>
public class CsvTestLogger : ITestLogger, IDisposable
{
    private readonly string _logPath;
    private readonly StreamWriter _writer;
    private readonly object _lock = new();
    private bool _disposed;

    public CsvTestLogger(string logPath)
    {
        _logPath = logPath;
        var directory = Path.GetDirectoryName(logPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        
        _writer = new StreamWriter(logPath, append: false, Encoding.UTF8);
        WriteHeader();
    }

    private void WriteHeader()
    {
        _writer.WriteLine("Timestamp,TestName,PageName,ControlId,Type,Action,Parameter,Result,Message");
    }

    // Full implementation for Log
    public void Log(string? testName, string? pageName, string? controlId, string message)
    {
        lock (_lock)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            _writer.WriteLine($"{timestamp},{Escape(testName)},{Escape(pageName)},{Escape(controlId)},LOG,,,INFO,{Escape(message)}");
            _writer.Flush();
        }
    }

    // Full implementation for LogAction
    public void LogAction(string? testName, string? pageName, string? controlId, string action, string? parameter = null)
    {
        lock (_lock)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            _writer.WriteLine($"{timestamp},{Escape(testName)},{Escape(pageName)},{Escape(controlId)},ACTION,{Escape(action)},{Escape(parameter)},OK,");
            _writer.Flush();
        }
    }

    // Method signatures only
    public void LogWait(string? testName, string? description, bool success, int elapsedMs);
    public void LogAssertPass(string? testName, string? pageName, string? controlId, string assertType, string? actual, string? expected);
    public void StartTest(string testName);
    public void EndTest(string testName, bool success);

    // Full implementation for ThrowAssertionFailed
    public void ThrowAssertionFailed(
        string? testName, string? pageName, string? controlId,
        string assertType, string? actual, string? expected,
        string message, ITestContext context)
    {
        // Log the failure
        lock (_lock)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            _writer.WriteLine($"{timestamp},{Escape(testName)},{Escape(pageName)},{Escape(controlId)},ASSERT,{Escape(assertType)},,FAIL,{Escape(message)}");
            _writer.Flush();
        }
        
        // Capture screenshot
        byte[]? screenshot = null;
        try
        {
            screenshot = context.CaptureScreenshot();
        }
        catch { /* Ignore screenshot errors */ }
        
        // Throw exception
        throw new AssertionException(message, assertType, actual, expected, testName, pageName, controlId, screenshot);
    }

    public void ThrowCheckFailed(
        string? testName, string? pageName, string? controlId,
        string checkType, string message, ITestContext context);

    public void ThrowTimeout(
        string? testName, string? pageName, string? controlId,
        string waitType, int timeoutMs, string message, ITestContext context);

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _writer.Dispose();
    }
}
```

---

## 9. ConsoleTestLogger Implementation

```csharp
namespace Brinell.Core;

/// <summary>
/// Console-based test logger for development and debugging.
/// </summary>
public class ConsoleTestLogger : ITestLogger
{
    private readonly LogLevel _minLevel;

    public ConsoleTestLogger(LogLevel minLevel = LogLevel.Info)
    {
        _minLevel = minLevel;
    }

    // Full implementation for Log
    public void Log(string? testName, string? pageName, string? controlId, string message)
    {
        if (_minLevel > LogLevel.Debug) return;
        
        var prefix = BuildPrefix(testName, pageName, controlId);
        Console.WriteLine($"[DEBUG] {prefix}{message}");
    }

    // Full implementation for LogAction
    public void LogAction(string? testName, string? pageName, string? controlId, string action, string? parameter = null)
    {
        if (_minLevel > LogLevel.Info) return;
        
        var prefix = BuildPrefix(testName, pageName, controlId);
        var paramStr = parameter != null ? $"({parameter})" : "()";
        Console.WriteLine($"[ACTION] {prefix}{action}{paramStr}");
    }

    // Method signatures only
    public void LogWait(string? testName, string? description, bool success, int elapsedMs);
    public void LogAssertPass(string? testName, string? pageName, string? controlId, string assertType, string? actual, string? expected);
    public void StartTest(string testName);
    public void EndTest(string testName, bool success);
    public void ThrowAssertionFailed(string? testName, string? pageName, string? controlId, string assertType, string? actual, string? expected, string message, ITestContext context);
    public void ThrowCheckFailed(string? testName, string? pageName, string? controlId, string checkType, string message, ITestContext context);
    public void ThrowTimeout(string? testName, string? pageName, string? controlId, string waitType, int timeoutMs, string message, ITestContext context);

    private static string BuildPrefix(string? testName, string? pageName, string? controlId)
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(testName)) parts.Add(testName);
        if (!string.IsNullOrEmpty(pageName)) parts.Add(pageName);
        if (!string.IsNullOrEmpty(controlId)) parts.Add(controlId);
        
        return parts.Count > 0 ? $"[{string.Join(" > ", parts)}] " : "";
    }
}
```

---

## 10. NullTestLogger Implementation

```csharp
namespace Brinell.Core;

/// <summary>
/// No-op logger for when logging is disabled.
/// </summary>
public class NullTestLogger : ITestLogger
{
    public static NullTestLogger Instance { get; } = new();

    private NullTestLogger() { }

    public void Log(string? testName, string? pageName, string? controlId, string message) { }
    public void LogAction(string? testName, string? pageName, string? controlId, string action, string? parameter = null) { }
    public void LogWait(string? testName, string? description, bool success, int elapsedMs) { }
    public void LogAssertPass(string? testName, string? pageName, string? controlId, string assertType, string? actual, string? expected) { }
    public void StartTest(string testName) { }
    public void EndTest(string testName, bool success) { }

    public void ThrowAssertionFailed(
        string? testName, string? pageName, string? controlId,
        string assertType, string? actual, string? expected,
        string message, ITestContext context)
    {
        throw new AssertionException(message, assertType, actual, expected, testName, pageName, controlId);
    }

    public void ThrowCheckFailed(
        string? testName, string? pageName, string? controlId,
        string checkType, string message, ITestContext context)
    {
        throw new CheckException(message, checkType, testName, pageName, controlId);
    }

    public void ThrowTimeout(
        string? testName, string? pageName, string? controlId,
        string waitType, int timeoutMs, string message, ITestContext context)
    {
        throw new WaitTimeoutException(message, waitType, timeoutMs, testName, pageName, controlId);
    }
}
```

---

**End of SPEC-006-002 Class Documents**

## Summary

| Document | Categories |
|----------|-----------|
| [SPEC-006-002a](SPEC-006-002-CLASSES-LOCATOR.md) | LocatorStrategy, ControlLocator, By Factory |
| [SPEC-006-002b](SPEC-006-002-CLASSES-FOUNDATION.md) | ControlBase, InteractiveControlBase, FocusableControlBase |
| [SPEC-006-002c](SPEC-006-002-CLASSES-INPUT.md) | ClickableControlBase, TextControlBase, SearchControlBase |
| [SPEC-006-002d](SPEC-006-002-CLASSES-TOGGLE.md) | ToggleControlBase, CheckBoxControlBase, SwitchControlBase, RadioButtonControlBase |
| [SPEC-006-002e](SPEC-006-002-CLASSES-SELECTION.md) | SelectorControlBase, PickerControlBase, MultiSelectorControlBase |
| [SPEC-006-002f](SPEC-006-002-CLASSES-RANGE.md) | RangeControlBase, SliderControlBase, StepperControlBase |
| [SPEC-006-002g](SPEC-006-002-CLASSES-DATETIME.md) | DateControlBase, TimeControlBase |
| [SPEC-006-002h](SPEC-006-002-CLASSES-COLLECTION.md) | ItemsControlBase, SelectableItemsControlBase, MultiSelectItemsControlBase |
| [SPEC-006-002i](SPEC-006-002-CLASSES-CONTAINER.md) | ContainerControlBase, ScrollableControlBase, ExpanderControlBase, GroupControlBase |
| [SPEC-006-002j](SPEC-006-002-CLASSES-DISPLAY.md) | LabelControlBase, ImageControlBase, ProgressControlBase, ActivityIndicatorControlBase |
| [SPEC-006-002k](SPEC-006-002-CLASSES-MEDIA.md) | MediaControlBase, WebViewControlBase |
| [SPEC-006-002l](SPEC-006-002-CLASSES-NAVIGATION.md) | TabControlBase, MenuControlBase, FlyoutControlBase, ToolbarControlBase |
| [SPEC-006-002m](SPEC-006-002-CLASSES-CONTEXT.md) | PageBase, TestContextBase, TestSettings, Control Factories |
| [SPEC-006-002n](SPEC-006-002-CLASSES-EXCEPTIONS.md) | BrinellException, AssertionException, CheckException, ITestLogger |
