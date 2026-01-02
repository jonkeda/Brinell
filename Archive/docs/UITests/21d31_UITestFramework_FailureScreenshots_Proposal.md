# 21d31: UITestFramework Failure Screenshots Proposal

## Problem Statement

When UI tests fail, developers currently have no visual context of the application state at the moment of failure. They must:
1. Re-run the test manually
2. Try to reproduce the exact failure conditions
3. Guess what the UI looked like based on log messages

This is particularly problematic for:
- **CI/CD pipelines** where the test environment may differ from local development
- **Intermittent failures** that are hard to reproduce
- **MessageBox/Dialog issues** like the current CopyModelDialog cleanup failures where we can't see if a dialog appeared or not

## Proposed Solution

Automatically capture a screenshot when:
1. An assertion fails
2. An unhandled exception occurs
3. A test times out waiting for a condition

### Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                        Test Execution                           │
├─────────────────────────────────────────────────────────────────┤
│  UITestBase                                                     │
│  ├── ITestOutputHelper (xUnit output)                          │
│  ├── ScreenshotService                                          │
│  └── Dispose() → Capture on failure                            │
├─────────────────────────────────────────────────────────────────┤
│  ScreenshotService                                              │
│  ├── CaptureWindow(AutomationElement window)                   │
│  ├── CaptureDesktop()                                          │
│  ├── SaveToFile(string testName, string suffix)                │
│  └── AttachToTestOutput(ITestOutputHelper output)              │
├─────────────────────────────────────────────────────────────────┤
│  Storage                                                        │
│  └── TestResults/{TestClass}/{TestMethod}_{timestamp}.png      │
└─────────────────────────────────────────────────────────────────┘
```

## Implementation Details

### 1. ScreenshotService

New service in `Oravey.UITestFramework.Wpf.Infrastructure`:

```csharp
public interface IScreenshotService
{
    /// <summary>
    /// Capture a screenshot of the specified window.
    /// </summary>
    byte[] CaptureWindow(AutomationElement window);
    
    /// <summary>
    /// Capture a screenshot of the entire desktop.
    /// </summary>
    byte[] CaptureDesktop();
    
    /// <summary>
    /// Save screenshot to the test results folder.
    /// Returns the file path.
    /// </summary>
    string SaveScreenshot(byte[] imageData, string testName, string suffix = "failure");
    
    /// <summary>
    /// Get the configured screenshot output directory.
    /// </summary>
    string ScreenshotDirectory { get; }
}

public class ScreenshotService : IScreenshotService
{
    private readonly string _outputDirectory;
    
    public ScreenshotService(string? outputDirectory = null)
    {
        _outputDirectory = outputDirectory 
            ?? Path.Combine(Environment.CurrentDirectory, "TestResults", "Screenshots");
    }
    
    public byte[] CaptureWindow(AutomationElement window)
    {
        var bounds = window.BoundingRectangle;
        using var bitmap = new Bitmap((int)bounds.Width, (int)bounds.Height);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(
            (int)bounds.X, (int)bounds.Y, 
            0, 0, 
            new Size((int)bounds.Width, (int)bounds.Height));
        
        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }
    
    public byte[] CaptureDesktop()
    {
        var bounds = Screen.PrimaryScreen.Bounds;
        using var bitmap = new Bitmap(bounds.Width, bounds.Height);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
        
        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }
    
    public string SaveScreenshot(byte[] imageData, string testName, string suffix = "failure")
    {
        Directory.CreateDirectory(_outputDirectory);
        
        var sanitizedName = SanitizeFileName(testName);
        var timestamp = DateTime.Now.ToString("HHmmss");
        var fileName = $"{sanitizedName}_{timestamp}_{suffix}.png";
        var filePath = Path.Combine(_outputDirectory, fileName);
        
        File.WriteAllBytes(filePath, imageData);
        return filePath;
    }
}
```

### 2. FlaUITestContext Enhancement

Add screenshot support to the test context:

```csharp
public class FlaUITestContext : IDisposable
{
    private readonly IScreenshotService _screenshotService;
    private bool _testFailed;
    private string? _failureReason;
    
    public IScreenshotService Screenshots => _screenshotService;
    
    /// <summary>
    /// Mark the test as failed and capture screenshot.
    /// Called automatically by assertion helpers.
    /// </summary>
    public void MarkTestFailed(string reason)
    {
        _testFailed = true;
        _failureReason = reason;
        CaptureFailureScreenshot(reason);
    }
    
    /// <summary>
    /// Capture a screenshot immediately.
    /// </summary>
    public string CaptureFailureScreenshot(string suffix = "failure")
    {
        try
        {
            var imageData = MainWindow != null 
                ? _screenshotService.CaptureWindow(MainWindow)
                : _screenshotService.CaptureDesktop();
            
            var path = _screenshotService.SaveScreenshot(imageData, TestName, suffix);
            _logger.LogInformation("Screenshot saved: {Path}", path);
            return path;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to capture screenshot");
            return string.Empty;
        }
    }
}
```

### 3. UITestBase Enhancement

Modify the base test class to capture on failure:

```csharp
public abstract class UITestBase : IDisposable
{
    private Exception? _capturedException;
    
    protected void CaptureScreenshotOnFailure()
    {
        if (_capturedException != null || TestContext.Current?.TestState == TestState.Failed)
        {
            Context.CaptureFailureScreenshot("test-failure");
        }
    }
    
    public virtual void Dispose()
    {
        try
        {
            CaptureScreenshotOnFailure();
        }
        finally
        {
            // Existing cleanup...
        }
    }
}
```

### 4. Integration with Assertions

Enhance PageBase and logging to capture on failure:

```csharp
// In LoggingExtensions.cs
public static void ThrowPageNotDisplayed(
    this ITestLogger logger, 
    string testName, 
    string pageName, 
    string pageId, 
    string action, 
    string message)
{
    // Capture screenshot before throwing
    if (logger is FlaUITestLogger flaUILogger)
    {
        flaUILogger.Context.CaptureFailureScreenshot($"page-not-displayed-{pageId}");
    }
    
    logger.LogError("[{TestName}] Page '{PageName}' ({PageId}) not displayed during {Action}", 
        testName, pageName, pageId, action);
    throw new PageNotDisplayedException($"{pageName} is not displayed.{message}");
}
```

### 5. WaitFor Enhancement

Capture screenshot on timeout:

```csharp
public bool WaitFor(Func<bool> condition, string description, int timeoutMs = 10000)
{
    var sw = Stopwatch.StartNew();
    while (sw.ElapsedMilliseconds < timeoutMs)
    {
        if (condition())
        {
            Log($"Condition met: {description} (elapsed: {sw.ElapsedMilliseconds}ms)");
            return true;
        }
        Thread.Sleep(100);
    }
    
    // Capture screenshot on timeout
    var screenshotPath = Context.CaptureFailureScreenshot($"timeout-{SanitizeName(description)}");
    Log($"Timeout waiting for: {description} (elapsed: {sw.ElapsedMilliseconds}ms)");
    Log($"Screenshot saved: {screenshotPath}");
    
    return false;
}
```

## Screenshot Storage Strategy

### Directory Structure
```
TestResults/
└── Screenshots/
    └── 2025-12-26/
        ├── CopyModelDialogTests_CopyModel_CopiedModelCanBeEdited_180617_timeout-page-MessageBox.png
        ├── CopyModelDialogTests_CopyModel_CopiedModelCanBeEdited_180618_test-failure.png
        └── ModelManagementTests_DeleteModel_Success_181542_failure.png
```

### CI/CD Integration

Configure test runner to collect screenshots as artifacts:

```yaml
# Azure DevOps
- task: PublishTestResults@2
  inputs:
    testResultsFormat: 'VSTest'
    testResultsFiles: '**/*.trx'
    
- task: PublishBuildArtifacts@1
  condition: always()
  inputs:
    pathtoPublish: 'TestResults/Screenshots'
    artifactName: 'UITestScreenshots'
```

## Alternative Approaches Considered

### 1. Video Recording
- **Pros**: Complete visual history of test execution
- **Cons**: Large file sizes, complex implementation, slower
- **Verdict**: Future enhancement, not needed for initial implementation

### 2. xUnit ITestOutputHelper Attachment
- **Pros**: Direct integration with test results
- **Cons**: xUnit doesn't support binary attachments well
- **Verdict**: Use file system + console output with path

### 3. Selenium-style WebDriver Screenshots
- **Pros**: Established pattern
- **Cons**: Not applicable to WPF/FlaUI architecture
- **Verdict**: Adapt pattern for FlaUI

## Implementation Plan

### Phase 1: Core Screenshot Service (T1)
1. Create `IScreenshotService` interface
2. Implement `ScreenshotService` with window/desktop capture
3. Add to `FlaUITestContext`
4. Add `CaptureFailureScreenshot()` method

### Phase 2: Automatic Capture Points (T2)
1. Capture on `WaitFor` timeout
2. Capture on `ThrowPageNotDisplayed`
3. Capture on test disposal if failed
4. Capture on assertion failure

### Phase 3: CI/CD Integration (T3)
1. Configure screenshot directory via environment variable
2. Add artifact collection to pipeline
3. Create screenshot viewer/gallery tool

## Acceptance Criteria

1. **Screenshot captured on timeout** - When `WaitFor` times out, a screenshot is saved
2. **Screenshot captured on exception** - When `PageNotDisplayedException` is thrown, a screenshot is saved first
3. **Screenshot path logged** - The path to the screenshot is output to the test log
4. **No impact on passing tests** - Screenshots only captured on failure
5. **Graceful degradation** - If screenshot capture fails, test continues with warning

## Dependencies

- `System.Drawing.Common` (already referenced for WPF tests)
- No new external dependencies required

## Testing Strategy

1. Create test that intentionally fails, verify screenshot exists
2. Create test that times out, verify screenshot captures waiting state
3. Create test that passes, verify no screenshot created
4. Verify screenshots work in CI/CD environment (may need virtual display)

## Open Questions

1. Should we capture both window and desktop on failure?
2. Should we keep screenshots from passing tests for a "visual test baseline"?
3. How long should we retain screenshots in CI/CD?
4. Should we add screenshot comparison for visual regression testing?
