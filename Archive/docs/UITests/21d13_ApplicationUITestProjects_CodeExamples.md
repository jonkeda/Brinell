# 13. Application UITest Projects - Code Examples

**Parent:** [Application UITest Projects](21d13_ApplicationUITestProjects.md)

---

## 13.1 Project File

```xml
<!-- Oravey.Tools.Wpf.UITests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
    <UseWPF>true</UseWPF>
    <RootNamespace>Oravey.Tools.Wpf.UITests</RootNamespace>
  </PropertyGroup>

  <!-- Test Framework -->
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="FluentAssertions" Version="7.0.0" />
  </ItemGroup>

  <!-- UI Test Framework -->
  <ItemGroup>
    <ProjectReference Include="..\Oravey.UITestFramework.Core\Oravey.UITestFramework.Core.csproj" />
    <ProjectReference Include="..\Oravey.UITestFramework.Wpf\Oravey.UITestFramework.Wpf.csproj" />
    <ProjectReference Include="..\Oravey.UITestFramework.Mocking\Oravey.UITestFramework.Mocking.csproj" />
  </ItemGroup>

  <!-- Test Data -->
  <ItemGroup>
    <None Update="TestData\**\*.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>

  <!-- FlaUI -->
  <ItemGroup>
    <PackageReference Include="FlaUI.UIA3" Version="4.0.0" />
  </ItemGroup>

</Project>
```

---

## 13.2 UITestBase Implementation

```csharp
namespace Oravey.Tools.Wpf.UITests.Infrastructure;

using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using Oravey.UITestFramework.Core.Abstractions;
using Oravey.UITestFramework.Core.Configuration;
using Oravey.UITestFramework.Core.Logging;
using Oravey.UITestFramework.Wpf.Infrastructure;
using Xunit;
using Xunit.Abstractions;

/// <summary>
/// Base class for WPF UI tests.
/// </summary>
public abstract class UITestBase : IDisposable
{
    protected ITestContext Context { get; private set; } = null!;
    protected ITestLogger Logger { get; }
    protected string TestName { get; }
    
    // FlaUI components
    protected Application? App { get; private set; }
    protected UIA3Automation Automation { get; }
    protected Window? MainWindow { get; private set; }
    
    private readonly ITestOutputHelper _output;
    private bool _disposed;
    
    protected UITestBase(ITestOutputHelper output)
    {
        _output = output;
        TestName = GetType().Name;
        
        // Initialize automation
        Automation = new UIA3Automation();
        
        // Initialize logger
        Logger = new CsvTestLogger(
            new LoggerConfiguration
            {
                OutputPath = "logs",
                FilePrefix = "UITests",
                LogToConsole = true,
                LogToXUnit = true
            },
            output);
        
        // Setup test
        SetupTest();
    }
    
    #region Setup/Teardown
    
    protected virtual void SetupTest()
    {
        Logger.LogInfo(TestName, "Setup", "Starting test");
        
        // Launch application
        LaunchApplication();
        
        // Create test context
        Context = CreateTestContext();
        
        Logger.LogInfo(TestName, "Setup", "Test context created");
    }
    
    protected virtual void LaunchApplication()
    {
        var appPath = GetApplicationPath();
        
        if (!File.Exists(appPath))
        {
            throw new FileNotFoundException($"Application not found: {appPath}");
        }
        
        Logger.LogInfo(TestName, "Setup", $"Launching: {appPath}");
        
        App = Application.Launch(appPath);
        
        // Wait for main window
        MainWindow = App.GetMainWindow(Automation, TimeSpan.FromSeconds(30));
        
        if (MainWindow == null)
        {
            throw new InvalidOperationException("Main window not found");
        }
        
        Logger.LogInfo(TestName, "Setup", "Application launched");
    }
    
    protected virtual string GetApplicationPath()
    {
        // Check environment variable first
        var envPath = Environment.GetEnvironmentVariable("APP_PATH");
        if (!string.IsNullOrEmpty(envPath))
        {
            return envPath;
        }
        
        // Default to relative path from test output
        return Path.GetFullPath(
            Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                @"..\..\..\..\..\Oravey.Tools.Wpf\bin\Debug\net9.0-windows\Oravey.Tools.Wpf.exe"));
    }
    
    protected virtual ITestContext CreateTestContext()
    {
        var config = TestConfiguration.FromEnvironment();
        
        var driverAdapter = new FlaUIDriverAdapter(Automation, MainWindow!);
        
        return new FlaUITestContext(
            driverAdapter,
            Platform.Windows,
            TestName,
            Logger,
            config);
    }
    
    #endregion
    
    #region Utilities
    
    /// <summary>
    /// Take screenshot of current window.
    /// </summary>
    protected void TakeScreenshot(string name)
    {
        try
        {
            var screenshotDir = Path.Combine("logs", "screenshots");
            Directory.CreateDirectory(screenshotDir);
            
            var fileName = $"{TestName}_{name}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            var filePath = Path.Combine(screenshotDir, fileName);
            
            using var capture = MainWindow?.Capture();
            capture?.ToFile(filePath);
            
            Logger.LogInfo(TestName, "Screenshot", $"Saved: {filePath}");
        }
        catch (Exception ex)
        {
            Logger.LogError(TestName, "Screenshot", "Failed to capture", ex);
        }
    }
    
    /// <summary>
    /// Wait for specified duration.
    /// </summary>
    protected void Wait(int milliseconds)
    {
        Thread.Sleep(milliseconds);
    }
    
    #endregion
    
    #region IDisposable
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            try
            {
                Logger.LogInfo(TestName, "Cleanup", "Cleaning up");
                
                Context?.Dispose();
                
                if (App != null && !App.HasExited)
                {
                    App.Close();
                    
                    if (!App.WaitWhileBusy(TimeSpan.FromSeconds(5)))
                    {
                        App.Kill();
                    }
                }
                
                Automation?.Dispose();
                
                Logger.LogInfo(TestName, "Cleanup", "Complete");
            }
            catch (Exception ex)
            {
                Logger.LogError(TestName, "Cleanup", "Error during cleanup", ex);
            }
            
            _disposed = true;
        }
    }
    
    #endregion
}
```

---

## 13.3 Test Fixture

```csharp
namespace Oravey.Tools.Wpf.UITests.Infrastructure;

using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using Oravey.UITestFramework.Core.Logging;

/// <summary>
/// Shared fixture for UI test collection.
/// Manages application lifecycle across all tests in collection.
/// </summary>
public class UITestFixture : IDisposable
{
    public Application? App { get; private set; }
    public UIA3Automation Automation { get; }
    public Window? MainWindow { get; private set; }
    public bool IsInitialized => App != null && !App.HasExited;
    
    private readonly ITestLogger _logger;
    
    public UITestFixture()
    {
        _logger = new CsvTestLogger();
        Automation = new UIA3Automation();
    }
    
    /// <summary>
    /// Initialize application (called once per collection).
    /// </summary>
    public void Initialize(string appPath)
    {
        if (IsInitialized)
        {
            return;
        }
        
        _logger.LogInfo("Fixture", "Initialize", $"Launching: {appPath}");
        
        App = Application.Launch(appPath);
        MainWindow = App.GetMainWindow(Automation, TimeSpan.FromSeconds(30));
        
        _logger.LogInfo("Fixture", "Initialize", "Application ready");
    }
    
    /// <summary>
    /// Reset application to initial state between tests.
    /// </summary>
    public void Reset()
    {
        _logger.LogInfo("Fixture", "Reset", "Resetting application state");
        
        // Navigate to home, close dialogs, etc.
        // Implementation depends on application
    }
    
    public void Dispose()
    {
        _logger.LogInfo("Fixture", "Dispose", "Shutting down");
        
        if (App != null && !App.HasExited)
        {
            App.Close();
            
            if (!App.WaitWhileBusy(TimeSpan.FromSeconds(5)))
            {
                App.Kill();
            }
        }
        
        Automation.Dispose();
    }
}

/// <summary>
/// Collection definition for shared fixture.
/// </summary>
[CollectionDefinition("UITests")]
public class UITestCollection : ICollectionFixture<UITestFixture>
{
    // This class has no code, and is never created.
    // Its purpose is to associate the fixture with the collection.
}
```

---

## 13.4 Test with Fixture

```csharp
namespace Oravey.Tools.Wpf.UITests.Tests;

using FluentAssertions;
using Oravey.Tools.Wpf.UITests.Infrastructure;
using Oravey.Tools.Wpf.UITests.PageObjects;
using Xunit;
using Xunit.Abstractions;

/// <summary>
/// Tests using shared fixture (faster, app stays open).
/// </summary>
[Trait("Category", "UITest")]
[Collection("UITests")]
public class NavigationTests : UITestBase
{
    public NavigationTests(ITestOutputHelper output) : base(output) { }
    
    [Fact]
    public void Shell_Displays_After_Launch()
    {
        // Arrange
        var shell = new ShellPage(Context);
        
        // Assert
        shell.WaitForPageReady();
        shell.IsDisplayed().Should().BeTrue();
    }
    
    [Fact]
    public void Settings_Navigation_Works()
    {
        // Arrange
        var shell = new ShellPage(Context);
        shell.WaitForPageReady();
        
        // Act
        var settings = shell.NavigateToSettings();
        
        // Assert
        settings.IsDisplayed().Should().BeTrue();
        settings.PageTitle.GetText().Should().Be("Settings");
    }
    
    [Fact]
    public void Navigate_Back_Returns_To_Shell()
    {
        // Arrange
        var shell = new ShellPage(Context);
        shell.WaitForPageReady();
        var settings = shell.NavigateToSettings();
        
        // Act
        var returnedShell = settings.NavigateToHome();
        
        // Assert
        returnedShell.IsDisplayed().Should().BeTrue();
    }
}
```

---

## 13.5 Test Data Management

```csharp
namespace Oravey.Tools.Wpf.UITests.TestData;

using System.Text.Json;

/// <summary>
/// Test data management utilities.
/// </summary>
public static class TestDataLoader
{
    private static readonly string TestDataPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "TestData");
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    
    /// <summary>
    /// Load test data from JSON file.
    /// </summary>
    public static T Load<T>(string fileName)
    {
        var filePath = Path.Combine(TestDataPath, fileName);
        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<T>(json, JsonOptions)
            ?? throw new InvalidOperationException($"Failed to deserialize {fileName}");
    }
    
    /// <summary>
    /// Load specific property from JSON file.
    /// </summary>
    public static T Load<T>(string fileName, string propertyName)
    {
        var filePath = Path.Combine(TestDataPath, fileName);
        var json = File.ReadAllText(filePath);
        var doc = JsonDocument.Parse(json);
        
        if (!doc.RootElement.TryGetProperty(propertyName, out var property))
        {
            throw new KeyNotFoundException($"Property '{propertyName}' not found in {fileName}");
        }
        
        return JsonSerializer.Deserialize<T>(property.GetRawText(), JsonOptions)
            ?? throw new InvalidOperationException($"Failed to deserialize {propertyName}");
    }
}

/// <summary>
/// Pre-defined test data accessors.
/// </summary>
public static class TestUsers
{
    public static TestUser Valid => TestDataLoader.Load<TestUser>("TestUsers.json", "validUser");
    public static TestUser Admin => TestDataLoader.Load<TestUser>("TestUsers.json", "adminUser");
    public static TestUser Invalid => TestDataLoader.Load<TestUser>("TestUsers.json", "invalidUser");
}

/// <summary>
/// Test user record.
/// </summary>
public record TestUser(
    string Username,
    string Password,
    string? DisplayName = null,
    string? Email = null);
```

### TestData/TestUsers.json

```json
{
  "validUser": {
    "username": "testuser",
    "password": "Test123!",
    "displayName": "Test User",
    "email": "test@example.com"
  },
  "adminUser": {
    "username": "admin",
    "password": "Admin123!",
    "displayName": "Administrator",
    "email": "admin@example.com"
  },
  "invalidUser": {
    "username": "invalid",
    "password": "wrongpassword"
  }
}
```

---

## 13.6 Test Helpers

```csharp
namespace Oravey.Tools.Wpf.UITests.Infrastructure;

using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;

/// <summary>
/// Common test helper methods.
/// </summary>
public static class TestHelpers
{
    /// <summary>
    /// Press Escape key.
    /// </summary>
    public static void PressEscape()
    {
        Keyboard.Press(VirtualKeyShort.ESCAPE);
    }
    
    /// <summary>
    /// Press Enter key.
    /// </summary>
    public static void PressEnter()
    {
        Keyboard.Press(VirtualKeyShort.ENTER);
    }
    
    /// <summary>
    /// Press Tab key.
    /// </summary>
    public static void PressTab()
    {
        Keyboard.Press(VirtualKeyShort.TAB);
    }
    
    /// <summary>
    /// Type text using keyboard.
    /// </summary>
    public static void Type(string text)
    {
        Keyboard.Type(text);
    }
    
    /// <summary>
    /// Perform keyboard shortcut.
    /// </summary>
    public static void Shortcut(VirtualKeyShort modifier, VirtualKeyShort key)
    {
        Keyboard.Pressing(modifier).Press(key);
    }
    
    /// <summary>
    /// Ctrl+S shortcut.
    /// </summary>
    public static void Save()
    {
        Shortcut(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_S);
    }
    
    /// <summary>
    /// Ctrl+Z shortcut.
    /// </summary>
    public static void Undo()
    {
        Shortcut(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_Z);
    }
    
    /// <summary>
    /// Generate unique test value.
    /// </summary>
    public static string UniqueValue(string prefix = "Test")
    {
        return $"{prefix}_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid():N}".Substring(0, 50);
    }
    
    /// <summary>
    /// Retry action with exponential backoff.
    /// </summary>
    public static T Retry<T>(Func<T> action, int maxAttempts = 3, int baseDelayMs = 100)
    {
        Exception? lastException = null;
        
        for (int i = 0; i < maxAttempts; i++)
        {
            try
            {
                return action();
            }
            catch (Exception ex)
            {
                lastException = ex;
                var delay = baseDelayMs * (int)Math.Pow(2, i);
                Thread.Sleep(delay);
            }
        }
        
        throw new InvalidOperationException(
            $"Action failed after {maxAttempts} attempts",
            lastException);
    }
}
```

---

## 13.7 Complete Test Example

```csharp
namespace Oravey.Tools.Wpf.UITests.Tests;

using FluentAssertions;
using Oravey.Tools.Wpf.UITests.Infrastructure;
using Oravey.Tools.Wpf.UITests.PageObjects;
using Oravey.Tools.Wpf.UITests.TestData;
using Xunit;
using Xunit.Abstractions;

[Trait("Category", "UITest")]
[Trait("Feature", "Settings")]
[Collection("UITests")]
public class SettingsTests : UITestBase
{
    public SettingsTests(ITestOutputHelper output) : base(output) { }
    
    [Fact]
    public void Settings_Can_Change_Username()
    {
        // Arrange
        var shell = new ShellPage(Context);
        shell.WaitForShellReady();
        var settings = shell.NavigateToSettings();
        
        var newUsername = TestHelpers.UniqueValue("User");
        
        // Act
        settings.SetUsername(newUsername);
        settings.SaveAndWait();
        
        // Assert
        settings.GetUsername().Should().Be(newUsername);
    }
    
    [Fact]
    public void Settings_Cancel_Discards_Changes()
    {
        // Arrange
        var shell = new ShellPage(Context);
        shell.WaitForShellReady();
        var settings = shell.NavigateToSettings();
        
        var originalUsername = settings.GetUsername();
        
        // Act - Make change but cancel
        settings.SetUsername("Changed");
        var returnedShell = settings.NavigateToHome();  // Cancel
        
        // Navigate back
        var settingsAgain = returnedShell.NavigateToSettings();
        
        // Assert - Original value should remain
        settingsAgain.GetUsername().Should().Be(originalUsername);
    }
    
    [Fact]
    public void Settings_Theme_Change_Updates_UI()
    {
        // Arrange
        var shell = new ShellPage(Context);
        shell.WaitForShellReady();
        var settings = shell.NavigateToSettings();
        
        // Act
        settings.SetDarkMode(true);
        settings.SaveAndWait();
        
        // Assert
        settings.IsDarkModeEnabled().Should().BeTrue();
        
        // Cleanup - Reset theme
        settings.SetDarkMode(false);
        settings.SaveAndWait();
    }
}
```

---

*Related: [Test Categories Code Examples](21d14_TestCategories_CodeExamples.md)*
