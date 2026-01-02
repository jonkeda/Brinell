# 21d31b: UITestFramework Failure Screenshots Proposal

## Problem Statement

When UI tests fail, developers currently have no visual context of the application state at the moment of failure. They must:
1. Re-run the test manually
2. Try to reproduce the exact failure conditions
3. Guess what the UI looked like based on log messages

This is particularly problematic for:
- **CI/CD pipelines** where the test environment may differ from local development
- **Intermittent failures** that are hard to reproduce
- **MessageBox/Dialog issues** like the current CopyModelDialog cleanup failures

## Design Decisions

| Question | Decision | Rationale |
|----------|----------|-----------|
| Capture window or desktop? | **Window only** | Focused, smaller file size, no sensitive data leak |
| Capture for passing tests? | **No** | Only for failing tests to minimize storage |
| CI/CD retention period? | **5 days** | Enough time to investigate, not excessive storage |
| Visual regression testing? | **No** | Out of scope, adds complexity |
| Capture on WaitFor timeout? | **No** | Timeout is normal flow, not always a failure |

## Architecture Overview

### Multi-Technology Support

```
┌─────────────────────────────────────────────────────────────────┐
│                 Oravey.UITestFramework.Core                     │
├─────────────────────────────────────────────────────────────────┤
│  IScreenshotService                                             │
│  ├── CaptureWindow() : byte[]                                   │
│  ├── SaveScreenshot(testName, suffix) : string                  │
│  └── ScreenshotDirectory : string                               │
├─────────────────────────────────────────────────────────────────┤
│  ITestContext                                                   │
│  └── CaptureFailureScreenshot(suffix) : string                  │
└─────────────────────────────────────────────────────────────────┘
                              │
        ┌─────────────────────┼─────────────────────┐
        ▼                     ▼                     ▼
┌───────────────┐   ┌───────────────┐   ┌───────────────┐
│  WPF (FlaUI)  │   │ Stride (Game) │   │  Web (Future) │
├───────────────┤   ├───────────────┤   ├───────────────┤
│ FlaUIScreen-  │   │ StrideScreen- │   │ WebScreen-    │
│ shotService   │   │ shotService   │   │ shotService   │
└───────────────┘   └───────────────┘   └───────────────┘
```

### Storage Structure

```
TestResults/
└── Screenshots/
    └── {YYYY-MM-DD}/
        ├── CopyModelDialogTests_CopyModel_CopiedModelCanBeEdited_180617_failure.png
        └── ModelManagementTests_DeleteModel_180618_exception.png
```

## Implementation Details

### 1. Core Interface (Oravey.UITestFramework.Core)

```csharp
namespace Oravey.UITestFramework.Core.Screenshots;

/// <summary>
/// Technology-agnostic screenshot capture service.
/// </summary>
public interface IScreenshotService
{
    /// <summary>
    /// Capture a screenshot of the test window.
    /// </summary>
    /// <returns>PNG image data, or empty array if capture fails.</returns>
    byte[] CaptureWindow();
    
    /// <summary>
    /// Save screenshot to the test results folder.
    /// </summary>
    /// <param name="imageData">PNG image data.</param>
    /// <param name="testName">Full test name (class.method).</param>
    /// <param name="suffix">Descriptive suffix (e.g., "failure", "exception").</param>
    /// <returns>The saved file path, or empty string if save fails.</returns>
    string SaveScreenshot(byte[] imageData, string testName, string suffix);
    
    /// <summary>
    /// Get the configured screenshot output directory.
    /// </summary>
    string ScreenshotDirectory { get; }
}
```

### 2. Base Implementation (Oravey.UITestFramework.Core)

```csharp
namespace Oravey.UITestFramework.Core.Screenshots;

/// <summary>
/// Base implementation with common file operations.
/// Technology-specific services inherit from this.
/// </summary>
public abstract class ScreenshotServiceBase : IScreenshotService
{
    private readonly string _outputDirectory;
    
    protected ScreenshotServiceBase(string? outputDirectory = null)
    {
        _outputDirectory = outputDirectory 
            ?? Environment.GetEnvironmentVariable("UITEST_SCREENSHOT_DIR")
            ?? Path.Combine(Environment.CurrentDirectory, "TestResults", "Screenshots", 
                DateTime.Now.ToString("yyyy-MM-dd"));
    }
    
    public string ScreenshotDirectory => _outputDirectory;
    
    public abstract byte[] CaptureWindow();
    
    public string SaveScreenshot(byte[] imageData, string testName, string suffix)
    {
        if (imageData.Length == 0)
            return string.Empty;
            
        try
        {
            Directory.CreateDirectory(_outputDirectory);
            
            var sanitizedName = SanitizeFileName(testName);
            var timestamp = DateTime.Now.ToString("HHmmss");
            var fileName = $"{sanitizedName}_{timestamp}_{suffix}.png";
            var filePath = Path.Combine(_outputDirectory, fileName);
            
            File.WriteAllBytes(filePath, imageData);
            return filePath;
        }
        catch
        {
            return string.Empty;
        }
    }
    
    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
    }
}
```

### 3. WPF Implementation (Oravey.UITestFramework.Wpf)

```csharp
namespace Oravey.UITestFramework.Wpf.Infrastructure;

using System.Drawing;
using System.Drawing.Imaging;
using FlaUI.Core.AutomationElements;

/// <summary>
/// WPF/FlaUI-specific screenshot capture using GDI+.
/// </summary>
public class FlaUIScreenshotService : ScreenshotServiceBase
{
    private readonly Func<AutomationElement?> _windowProvider;
    
    public FlaUIScreenshotService(Func<AutomationElement?> windowProvider, string? outputDirectory = null)
        : base(outputDirectory)
    {
        _windowProvider = windowProvider;
    }
    
    public override byte[] CaptureWindow()
    {
        try
        {
            var window = _windowProvider();
            if (window == null)
                return [];
                
            var bounds = window.BoundingRectangle;
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return [];
            
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
        catch
        {
            return [];
        }
    }
}
```

### 4. Stride Implementation (Oravey.UITestFramework.Stride - Future)

```csharp
namespace Oravey.UITestFramework.Stride.Infrastructure;

using Stride.Graphics;

/// <summary>
/// Stride game engine screenshot capture.
/// </summary>
public class StrideScreenshotService : ScreenshotServiceBase
{
    private readonly Func<GraphicsDevice?> _graphicsProvider;
    
    public StrideScreenshotService(Func<GraphicsDevice?> graphicsProvider, string? outputDirectory = null)
        : base(outputDirectory)
    {
        _graphicsProvider = graphicsProvider;
    }
    
    public override byte[] CaptureWindow()
    {
        try
        {
            var graphics = _graphicsProvider();
            if (graphics == null)
                return [];
            
            // Use Stride's built-in screenshot capability
            var backBuffer = graphics.Presenter.BackBuffer;
            // ... Stride-specific capture logic
            
            return []; // TODO: Implement
        }
        catch
        {
            return [];
        }
    }
}
```

### 5. FlaUITestContext Integration

```csharp
public class FlaUITestContext : IDisposable
{
    private readonly IScreenshotService _screenshotService;
    private readonly ITestLogger _logger;
    
    public FlaUITestContext(/* ... */)
    {
        // ...
        _screenshotService = new FlaUIScreenshotService(() => MainWindow);
    }
    
    /// <summary>
    /// Capture a failure screenshot. Call this before throwing exceptions.
    /// </summary>
    /// <param name="suffix">Descriptive suffix for the screenshot file.</param>
    /// <returns>Path to saved screenshot, or empty string if capture failed.</returns>
    public string CaptureFailureScreenshot(string suffix = "failure")
    {
        var imageData = _screenshotService.CaptureWindow();
        if (imageData.Length == 0)
        {
            _logger.LogWarning("Failed to capture screenshot");
            return string.Empty;
        }
        
        var path = _screenshotService.SaveScreenshot(imageData, TestName, suffix);
        if (!string.IsNullOrEmpty(path))
        {
            _logger.LogInformation("Screenshot saved: {Path}", path);
        }
        return path;
    }
}
```

### 6. Exception Capture Points

Capture screenshot before throwing in `LoggingExtensions.cs`:

```csharp
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
        flaUILogger.Context.CaptureFailureScreenshot($"page-not-displayed-{SanitizeSuffix(pageId)}");
    }
    
    logger.LogError("[{TestName}] Page '{PageName}' ({PageId}) not displayed during {Action}", 
        testName, pageName, pageId, action);
    throw new PageNotDisplayedException($"{pageName} is not displayed.{message}");
}

public static void ThrowElementNotFound(
    this ITestLogger logger,
    string testName,
    string elementId,
    string action)
{
    // Capture screenshot before throwing
    if (logger is FlaUITestLogger flaUILogger)
    {
        flaUILogger.Context.CaptureFailureScreenshot($"element-not-found-{SanitizeSuffix(elementId)}");
    }
    
    logger.LogError("[{TestName}] Element '{ElementId}' not found during {Action}", 
        testName, elementId, action);
    throw new ElementNotFoundException($"Element '{elementId}' not found.");
}

private static string SanitizeSuffix(string suffix)
{
    return suffix.Replace(" ", "-").Replace("/", "-").Replace("\\", "-");
}
```

### 7. UITestBase Disposal Capture

```csharp
public abstract class UITestBase : IDisposable
{
    private bool _disposed;
    
    public virtual void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        try
        {
            // Capture if test failed but no earlier screenshot was taken
            // xUnit sets TestContext.Current.TestState after test completion
            if (TestContext.Current?.TestState == TestState.Failed)
            {
                Context.CaptureFailureScreenshot("test-failure");
            }
        }
        catch
        {
            // Don't fail disposal due to screenshot issues
        }
        finally
        {
            CleanupApplication();
        }
    }
}
```

## CI/CD Configuration

### Azure DevOps

```yaml
- task: DotNetCoreCLI@2
  displayName: 'Run UI Tests'
  inputs:
    command: 'test'
    projects: '**/Oravey.Tools.Wpf.UITests.csproj'
  env:
    UITEST_SCREENSHOT_DIR: '$(Build.ArtifactStagingDirectory)/Screenshots'

- task: PublishBuildArtifacts@1
  displayName: 'Publish Screenshots'
  condition: always()
  inputs:
    pathtoPublish: '$(Build.ArtifactStagingDirectory)/Screenshots'
    artifactName: 'UITestScreenshots'
    
# Retention policy: 5 days
- task: DeleteFiles@1
  displayName: 'Cleanup old screenshots'
  inputs:
    SourceFolder: '$(Build.ArtifactStagingDirectory)/Screenshots'
    Contents: '**'
    RemoveOlderThan: 5
```

### GitHub Actions

```yaml
- name: Run UI Tests
  run: dotnet test **/Oravey.Tools.Wpf.UITests.csproj
  env:
    UITEST_SCREENSHOT_DIR: ./screenshots

- name: Upload Screenshots
  if: always()
  uses: actions/upload-artifact@v4
  with:
    name: ui-test-screenshots
    path: ./screenshots
    retention-days: 5
```

## Implementation Plan

### Phase 1: Core Infrastructure
1. Add `IScreenshotService` interface to `Oravey.UITestFramework.Core`
2. Add `ScreenshotServiceBase` abstract class
3. Implement `FlaUIScreenshotService` in `Oravey.UITestFramework.Wpf`
4. Add `CaptureFailureScreenshot()` to `FlaUITestContext`

### Phase 2: Exception Integration
1. Update `ThrowPageNotDisplayed` to capture before throwing
2. Update `ThrowElementNotFound` to capture before throwing
3. Update other exception-throwing methods in `LoggingExtensions`
4. Add disposal capture in `UITestBase`

### Phase 3: Future Technology Support
1. Add `StrideScreenshotService` when game UI tests are implemented
2. Add web screenshot service if web UI testing is added

## Acceptance Criteria

1. ✅ Screenshot captured before `PageNotDisplayedException` is thrown
2. ✅ Screenshot captured before `ElementNotFoundException` is thrown
3. ✅ Screenshot captured on test disposal if test failed
4. ✅ Screenshot path logged to test output
5. ✅ No screenshots for passing tests
6. ✅ No screenshots on WaitFor timeout (normal flow)
7. ✅ Graceful degradation - if capture fails, test continues with warning
8. ✅ Screenshots stored with date-based folders
9. ✅ CI/CD configured for 5-day retention

## Files to Create/Modify

### New Files
- `Sources/UITestFramework/Oravey.UITestFramework.Core/Screenshots/IScreenshotService.cs`
- `Sources/UITestFramework/Oravey.UITestFramework.Core/Screenshots/ScreenshotServiceBase.cs`
- `Sources/UITestFramework/Oravey.UITestFramework.Wpf/Infrastructure/FlaUIScreenshotService.cs`

### Modified Files
- `Sources/UITestFramework/Oravey.UITestFramework.Wpf/Infrastructure/FlaUITestContext.cs`
- `Sources/UITestFramework/Oravey.UITestFramework.Core/Logging/LoggingExtensions.cs`
- `Sources/UITestFramework/Oravey.UITestFramework.Wpf/Infrastructure/UITestBase.cs`

## Dependencies

- `System.Drawing.Common` - Already referenced in WPF projects
- No new external dependencies required
