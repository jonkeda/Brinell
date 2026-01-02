# 17. Troubleshooting - Code Examples

**Parent:** [Troubleshooting](21d17_Troubleshooting.md)

---

## 17.1 Element Not Found Debugging

```csharp
namespace Oravey.UITestFramework.Core.Diagnostics;

using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

/// <summary>
/// Diagnostic utilities for troubleshooting element issues.
/// </summary>
public static class ElementDiagnostics
{
    /// <summary>
    /// Print the automation tree for debugging.
    /// </summary>
    public static void PrintElementTree(AutomationElement root, int depth = 0)
    {
        if (root == null) return;
        
        var indent = new string(' ', depth * 2);
        var name = root.Properties.Name.ValueOrDefault ?? "(no name)";
        var autoId = root.Properties.AutomationId.ValueOrDefault ?? "(no id)";
        var controlType = root.Properties.ControlType.ValueOrDefault;
        
        Console.WriteLine($"{indent}{controlType}: Name='{name}', AutomationId='{autoId}'");
        
        foreach (var child in root.FindAllChildren())
        {
            PrintElementTree(child, depth + 1);
        }
    }
    
    /// <summary>
    /// Find element by partial AutomationId match.
    /// </summary>
    public static AutomationElement? FindByPartialId(
        AutomationElement root,
        string partialId)
    {
        var descendants = root.FindAllDescendants();
        
        foreach (var element in descendants)
        {
            var autoId = element.Properties.AutomationId.ValueOrDefault;
            
            if (!string.IsNullOrEmpty(autoId) && 
                autoId.Contains(partialId, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"Found match: {autoId}");
                return element;
            }
        }
        
        Console.WriteLine($"No match found for: {partialId}");
        return null;
    }
    
    /// <summary>
    /// List all AutomationIds in the tree.
    /// </summary>
    public static List<string> GetAllAutomationIds(AutomationElement root)
    {
        var ids = new List<string>();
        var descendants = root.FindAllDescendants();
        
        foreach (var element in descendants)
        {
            var autoId = element.Properties.AutomationId.ValueOrDefault;
            if (!string.IsNullOrEmpty(autoId))
            {
                ids.Add(autoId);
            }
        }
        
        return ids.Distinct().OrderBy(x => x).ToList();
    }
    
    /// <summary>
    /// Diagnose why element cannot be found.
    /// </summary>
    public static void DiagnoseElementNotFound(
        AutomationElement root,
        string automationId)
    {
        Console.WriteLine($"=== Diagnosing: {automationId} ===");
        
        // Check exact match
        var exact = root.FindFirstDescendant(cf => cf.ByAutomationId(automationId));
        if (exact != null)
        {
            Console.WriteLine("Element EXISTS but may not be visible/enabled:");
            Console.WriteLine($"  IsVisible: {exact.Properties.IsOffscreen.ValueOrDefault == false}");
            Console.WriteLine($"  IsEnabled: {exact.IsEnabled}");
            return;
        }
        
        // Check partial matches
        Console.WriteLine("No exact match. Checking similar IDs...");
        var allIds = GetAllAutomationIds(root);
        var similar = allIds.Where(id => 
            id.Contains(automationId, StringComparison.OrdinalIgnoreCase) ||
            automationId.Contains(id, StringComparison.OrdinalIgnoreCase))
            .ToList();
        
        if (similar.Any())
        {
            Console.WriteLine("Similar IDs found:");
            foreach (var id in similar)
            {
                Console.WriteLine($"  - {id}");
            }
        }
        else
        {
            Console.WriteLine("No similar IDs found.");
            Console.WriteLine($"Total elements with AutomationId: {allIds.Count}");
        }
    }
}
```

---

## 17.2 Enhanced Wait with Diagnostics

```csharp
namespace Oravey.UITestFramework.Core.Diagnostics;

using Oravey.UITestFramework.Core.Logging;

/// <summary>
/// Wait utilities with diagnostic output.
/// </summary>
public static class DiagnosticWait
{
    /// <summary>
    /// Wait with detailed progress logging.
    /// </summary>
    public static bool WaitWithDiagnostics(
        Func<bool> condition,
        int timeoutMs,
        string description,
        ITestLogger logger,
        string testName,
        int pollIntervalMs = 100)
    {
        var startTime = DateTime.Now;
        var endTime = startTime.AddMilliseconds(timeoutMs);
        var attempts = 0;
        
        logger.LogInfo(testName, "DiagnosticWait", 
            $"Starting wait for: {description} (timeout: {timeoutMs}ms)");
        
        while (DateTime.Now < endTime)
        {
            attempts++;
            
            try
            {
                if (condition())
                {
                    var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
                    logger.LogInfo(testName, "DiagnosticWait",
                        $"SUCCESS after {attempts} attempts ({elapsed:F0}ms)");
                    return true;
                }
            }
            catch (Exception ex)
            {
                logger.LogInfo(testName, "DiagnosticWait",
                    $"Attempt {attempts} threw: {ex.GetType().Name}: {ex.Message}");
            }
            
            Thread.Sleep(pollIntervalMs);
        }
        
        var totalElapsed = (DateTime.Now - startTime).TotalMilliseconds;
        logger.LogInfo(testName, "DiagnosticWait",
            $"TIMEOUT after {attempts} attempts ({totalElapsed:F0}ms)");
        
        return false;
    }
    
    /// <summary>
    /// Wait with state snapshots for debugging.
    /// </summary>
    public static bool WaitWithSnapshots<T>(
        Func<T> getState,
        Func<T, bool> isExpected,
        int timeoutMs,
        ITestLogger logger,
        string testName,
        int pollIntervalMs = 500)
    {
        var startTime = DateTime.Now;
        var endTime = startTime.AddMilliseconds(timeoutMs);
        var snapshots = new List<(DateTime Time, T State)>();
        
        while (DateTime.Now < endTime)
        {
            var state = getState();
            snapshots.Add((DateTime.Now, state));
            
            if (isExpected(state))
            {
                logger.LogInfo(testName, "DiagnosticWait",
                    $"Condition met. State history ({snapshots.Count} snapshots):");
                LogSnapshots(snapshots, logger, testName);
                return true;
            }
            
            Thread.Sleep(pollIntervalMs);
        }
        
        logger.LogInfo(testName, "DiagnosticWait",
            $"TIMEOUT. State history ({snapshots.Count} snapshots):");
        LogSnapshots(snapshots, logger, testName);
        
        return false;
    }
    
    private static void LogSnapshots<T>(
        List<(DateTime Time, T State)> snapshots,
        ITestLogger logger,
        string testName)
    {
        foreach (var (time, state) in snapshots.TakeLast(10))
        {
            logger.LogInfo(testName, "DiagnosticWait",
                $"  {time:HH:mm:ss.fff}: {state}");
        }
    }
}
```

---

## 17.3 Retry Utilities

```csharp
namespace Oravey.UITestFramework.Core.Utilities;

using Oravey.UITestFramework.Core.Logging;

/// <summary>
/// Retry utilities for handling transient failures.
/// </summary>
public static class RetryHelper
{
    /// <summary>
    /// Retry an action with exponential backoff.
    /// </summary>
    public static T RetryWithBackoff<T>(
        Func<T> action,
        int maxAttempts = 3,
        int initialDelayMs = 100,
        double backoffMultiplier = 2.0,
        ITestLogger? logger = null,
        string? testName = null)
    {
        Exception? lastException = null;
        var delay = initialDelayMs;
        
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return action();
            }
            catch (Exception ex)
            {
                lastException = ex;
                
                logger?.LogInfo(testName ?? "Retry", "RetryHelper",
                    $"Attempt {attempt}/{maxAttempts} failed: {ex.Message}");
                
                if (attempt < maxAttempts)
                {
                    Thread.Sleep(delay);
                    delay = (int)(delay * backoffMultiplier);
                }
            }
        }
        
        throw new InvalidOperationException(
            $"All {maxAttempts} attempts failed",
            lastException);
    }
    
    /// <summary>
    /// Retry with specific exception handling.
    /// </summary>
    public static T RetryOn<T, TException>(
        Func<T> action,
        int maxAttempts = 3,
        int delayMs = 500)
        where TException : Exception
    {
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return action();
            }
            catch (TException) when (attempt < maxAttempts)
            {
                Thread.Sleep(delayMs);
            }
        }
        
        // Final attempt - let exception propagate
        return action();
    }
    
    /// <summary>
    /// Retry void action.
    /// </summary>
    public static void Retry(
        Action action,
        int maxAttempts = 3,
        int delayMs = 500)
    {
        RetryWithBackoff(
            () => { action(); return true; },
            maxAttempts,
            delayMs);
    }
}
```

---

## 17.4 Screenshot Capture Utilities

```csharp
namespace Oravey.UITestFramework.Core.Diagnostics;

using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;
using System.Drawing;
using System.Drawing.Imaging;

/// <summary>
/// Screenshot utilities for debugging test failures.
/// </summary>
public static class ScreenshotHelper
{
    private static readonly string DefaultScreenshotDir = 
        Path.Combine("TestResults", "screenshots");
    
    /// <summary>
    /// Capture screenshot of specific element.
    /// </summary>
    public static string CaptureElement(
        AutomationElement element,
        string name,
        string? outputDir = null)
    {
        outputDir ??= DefaultScreenshotDir;
        Directory.CreateDirectory(outputDir);
        
        var fileName = $"{name}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        var filePath = Path.Combine(outputDir, fileName);
        
        using var capture = Capture.Element(element);
        capture.ToFile(filePath);
        
        return filePath;
    }
    
    /// <summary>
    /// Capture full screen.
    /// </summary>
    public static string CaptureScreen(
        string name,
        string? outputDir = null)
    {
        outputDir ??= DefaultScreenshotDir;
        Directory.CreateDirectory(outputDir);
        
        var fileName = $"{name}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        var filePath = Path.Combine(outputDir, fileName);
        
        using var capture = Capture.Screen();
        capture.ToFile(filePath);
        
        return filePath;
    }
    
    /// <summary>
    /// Capture with highlighted element.
    /// </summary>
    public static string CaptureWithHighlight(
        Window window,
        AutomationElement? elementToHighlight,
        string name,
        string? outputDir = null)
    {
        outputDir ??= DefaultScreenshotDir;
        Directory.CreateDirectory(outputDir);
        
        var fileName = $"{name}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        var filePath = Path.Combine(outputDir, fileName);
        
        using var capture = Capture.Element(window);
        var bitmap = capture.Bitmap;
        
        if (elementToHighlight != null)
        {
            var bounds = elementToHighlight.BoundingRectangle;
            var windowBounds = window.BoundingRectangle;
            
            // Adjust to window-relative coordinates
            var relativeRect = new Rectangle(
                bounds.X - windowBounds.X,
                bounds.Y - windowBounds.Y,
                bounds.Width,
                bounds.Height);
            
            using var graphics = Graphics.FromImage(bitmap);
            using var pen = new Pen(Color.Red, 3);
            graphics.DrawRectangle(pen, relativeRect);
        }
        
        bitmap.Save(filePath, ImageFormat.Png);
        
        return filePath;
    }
    
    /// <summary>
    /// Clean up old screenshots.
    /// </summary>
    public static void CleanupOldScreenshots(
        string? outputDir = null,
        int keepDays = 7)
    {
        outputDir ??= DefaultScreenshotDir;
        
        if (!Directory.Exists(outputDir))
            return;
        
        var cutoff = DateTime.Now.AddDays(-keepDays);
        
        foreach (var file in Directory.GetFiles(outputDir, "*.png"))
        {
            if (File.GetCreationTime(file) < cutoff)
            {
                File.Delete(file);
            }
        }
    }
}
```

---

## 17.5 Test Base with Error Recovery

```csharp
namespace Oravey.Tools.Wpf.UITests.Infrastructure;

using Oravey.UITestFramework.Core.Diagnostics;
using Oravey.UITestFramework.Core.Utilities;
using Xunit;
using Xunit.Abstractions;

/// <summary>
/// Test base with enhanced error recovery and diagnostics.
/// </summary>
public abstract class RobustUITestBase : UITestBase
{
    private readonly List<string> _screenshots = new();
    private bool _testFailed;
    
    protected RobustUITestBase(ITestOutputHelper output) : base(output)
    {
    }
    
    #region Error Recovery
    
    /// <summary>
    /// Execute action with automatic recovery on failure.
    /// </summary>
    protected T ExecuteWithRecovery<T>(
        Func<T> action,
        Action? recovery = null,
        int maxAttempts = 2)
    {
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return action();
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                Logger.LogError(TestName, "Recovery",
                    $"Attempt {attempt} failed, recovering...", ex);
                
                // Take diagnostic screenshot
                TakeScreenshot($"recovery_attempt_{attempt}");
                
                // Execute recovery action
                recovery?.Invoke();
                
                // Default recovery: dismiss dialogs, reset state
                TryDismissDialogs();
                Wait(500);
            }
        }
        
        // Final attempt without catch
        return action();
    }
    
    /// <summary>
    /// Try to dismiss any open dialogs.
    /// </summary>
    protected virtual void TryDismissDialogs()
    {
        try
        {
            // Press Escape to close potential dialogs
            FlaUI.Core.Input.Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.ESCAPE);
            Wait(200);
        }
        catch
        {
            // Ignore errors during recovery
        }
    }
    
    #endregion
    
    #region Diagnostics
    
    /// <summary>
    /// Take screenshot with automatic naming.
    /// </summary>
    protected override void TakeScreenshot(string name)
    {
        try
        {
            var path = ScreenshotHelper.CaptureElement(
                MainWindow!,
                $"{TestName}_{name}");
            
            _screenshots.Add(path);
            Logger.LogInfo(TestName, "Screenshot", $"Saved: {path}");
        }
        catch (Exception ex)
        {
            Logger.LogError(TestName, "Screenshot", "Failed to capture", ex);
        }
    }
    
    /// <summary>
    /// Dump element tree for debugging.
    /// </summary>
    protected void DumpElementTree()
    {
        Logger.LogInfo(TestName, "Debug", "Element tree:");
        ElementDiagnostics.PrintElementTree(MainWindow!);
    }
    
    /// <summary>
    /// List all automation IDs in window.
    /// </summary>
    protected void ListAutomationIds()
    {
        var ids = ElementDiagnostics.GetAllAutomationIds(MainWindow!);
        Logger.LogInfo(TestName, "Debug", $"Found {ids.Count} AutomationIds:");
        foreach (var id in ids.Take(50))
        {
            Logger.LogInfo(TestName, "Debug", $"  - {id}");
        }
        if (ids.Count > 50)
        {
            Logger.LogInfo(TestName, "Debug", $"  ... and {ids.Count - 50} more");
        }
    }
    
    #endregion
    
    #region Lifecycle
    
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Take failure screenshot if test failed
            if (_testFailed)
            {
                TakeScreenshot("failure_final");
            }
            
            // Log screenshot summary
            if (_screenshots.Any())
            {
                Logger.LogInfo(TestName, "Cleanup",
                    $"Screenshots taken: {_screenshots.Count}");
            }
        }
        
        base.Dispose(disposing);
    }
    
    /// <summary>
    /// Mark test as failed (call from test on assertion failure).
    /// </summary>
    protected void MarkFailed()
    {
        _testFailed = true;
    }
    
    #endregion
}
```

---

## 17.6 Common Issue Fixes

```csharp
namespace Oravey.UITestFramework.Core.Fixes;

/// <summary>
/// Common issue workarounds.
/// </summary>
public static class CommonFixes
{
    #region Stale Element Fix
    
    /// <summary>
    /// Re-find element if stale.
    /// </summary>
    public static T WithStaleElementRetry<T>(
        Func<T> action,
        Func<T> refindElement,
        int maxRetries = 3)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                return action();
            }
            catch (Exception ex) when (
                ex.Message.Contains("stale", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("no longer valid", StringComparison.OrdinalIgnoreCase))
            {
                if (i == maxRetries - 1) throw;
                
                // Re-find element
                var newElement = refindElement();
                Thread.Sleep(100);
            }
        }
        
        return action();
    }
    
    #endregion
    
    #region Focus Fix
    
    /// <summary>
    /// Ensure window has focus before action.
    /// </summary>
    public static void EnsureFocused(Window window)
    {
        if (!window.Properties.HasKeyboardFocus.ValueOrDefault)
        {
            window.Focus();
            Thread.Sleep(100);
        }
    }
    
    #endregion
    
    #region Scroll Into View
    
    /// <summary>
    /// Scroll element into view if needed.
    /// </summary>
    public static void ScrollIntoView(AutomationElement element)
    {
        if (element.Patterns.ScrollItem.TryGetPattern(out var scrollItem))
        {
            scrollItem.ScrollIntoView();
            Thread.Sleep(200);
        }
    }
    
    #endregion
    
    #region Modal Dialog Handler
    
    /// <summary>
    /// Wait for and handle modal dialog.
    /// </summary>
    public static bool TryHandleModalDialog(
        Window mainWindow,
        string dialogAutomationId,
        Action<Window> handleDialog,
        int timeoutMs = 5000)
    {
        var startTime = DateTime.Now;
        
        while ((DateTime.Now - startTime).TotalMilliseconds < timeoutMs)
        {
            var dialog = mainWindow.FindFirstDescendant(cf =>
                cf.ByAutomationId(dialogAutomationId))?.AsWindow();
            
            if (dialog != null)
            {
                handleDialog(dialog);
                return true;
            }
            
            Thread.Sleep(200);
        }
        
        return false;
    }
    
    #endregion
    
    #region CI Environment Detection
    
    /// <summary>
    /// Check if running in CI environment.
    /// </summary>
    public static bool IsRunningInCI()
    {
        return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")) ||
               !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TF_BUILD")) ||
               !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
    }
    
    /// <summary>
    /// Get appropriate timeout based on environment.
    /// </summary>
    public static int GetAdaptiveTimeout(int baseTimeout)
    {
        if (IsRunningInCI())
        {
            return baseTimeout * 2;  // Double timeout in CI
        }
        return baseTimeout;
    }
    
    #endregion
}
```

---

## 17.7 Troubleshooting Checklist Script

```powershell
# troubleshoot-uitests.ps1
# Diagnostic script for UI test issues

param(
    [string]$LogDir = "TestResults/logs",
    [switch]$CheckEnvironment,
    [switch]$AnalyzeLogs,
    [switch]$All
)

if ($All) {
    $CheckEnvironment = $true
    $AnalyzeLogs = $true
}

Write-Host "=== UI Test Troubleshooting ===" -ForegroundColor Cyan

# Environment Check
if ($CheckEnvironment) {
    Write-Host "`n--- Environment ---" -ForegroundColor Yellow
    
    Write-Host "Platform: $env:PLATFORM"
    Write-Host "App Path: $env:APP_PATH"
    Write-Host "Cloud Provider: $env:CLOUD_PROVIDER"
    Write-Host "CI: $env:CI"
    
    # Check app exists
    if ($env:APP_PATH -and (Test-Path $env:APP_PATH)) {
        Write-Host "App exists: YES" -ForegroundColor Green
    } else {
        Write-Host "App exists: NO" -ForegroundColor Red
    }
    
    # Check .NET
    Write-Host "`n.NET Version:"
    dotnet --version
    
    # Check screen resolution
    Add-Type -AssemblyName System.Windows.Forms
    $screen = [System.Windows.Forms.Screen]::PrimaryScreen
    Write-Host "Screen: $($screen.Bounds.Width)x$($screen.Bounds.Height)"
}

# Log Analysis
if ($AnalyzeLogs) {
    Write-Host "`n--- Log Analysis ---" -ForegroundColor Yellow
    
    if (Test-Path $LogDir) {
        $csvFiles = Get-ChildItem -Path $LogDir -Filter "*.csv" | Sort-Object LastWriteTime -Descending | Select-Object -First 5
        
        foreach ($file in $csvFiles) {
            Write-Host "`nAnalyzing: $($file.Name)"
            
            $logs = Import-Csv -Delimiter ';' $file.FullName
            $failed = $logs | Where-Object { $_.Result -eq 'Failed' }
            
            Write-Host "  Total entries: $($logs.Count)"
            Write-Host "  Failed: $($failed.Count)" -ForegroundColor $(if ($failed.Count -gt 0) { 'Red' } else { 'Green' })
            
            if ($failed.Count -gt 0) {
                Write-Host "  Failed actions:" -ForegroundColor Red
                $failed | Select-Object -First 5 | ForEach-Object {
                    Write-Host "    - $($_.Action) on $($_.ControlId): $($_.Message)"
                }
            }
        }
    } else {
        Write-Host "Log directory not found: $LogDir" -ForegroundColor Yellow
    }
}

Write-Host "`n=== Done ===" -ForegroundColor Cyan
```

---

*This concludes the UI Testing Framework documentation code examples.*
