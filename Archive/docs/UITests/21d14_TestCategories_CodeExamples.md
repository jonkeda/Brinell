# 14. Test Categories - Code Examples

**Parent:** [Test Categories](21d14_TestCategories.md)

---

## 14.1 Trait Constants

```csharp
namespace Oravey.UITestFramework.Core.Testing;

/// <summary>
/// Standard trait names and values for consistent categorization.
/// </summary>
public static class TestTraits
{
    #region Category Names
    
    public const string Category = "Category";
    public const string Platform = "Platform";
    public const string Feature = "Feature";
    public const string Priority = "Priority";
    public const string SkipOn = "SkipOn";
    public const string MockApi = "MockApi";
    
    #endregion
    
    #region Category Values
    
    public static class Categories
    {
        public const string UITest = "UITest";
        public const string Smoke = "Smoke";
        public const string Regression = "Regression";
        public const string E2E = "E2E";
        public const string MockedAPITest = "MockedAPITest";
        public const string Integration = "Integration";
    }
    
    #endregion
    
    #region Platform Values
    
    public static class Platforms
    {
        public const string Windows = "Windows";
        public const string WindowsMaui = "WindowsMaui";
        public const string Android = "Android";
        public const string iOS = "iOS";
        public const string Web = "Web";
    }
    
    #endregion
    
    #region Feature Values
    
    public static class Features
    {
        public const string Navigation = "Navigation";
        public const string Settings = "Settings";
        public const string Authentication = "Authentication";
        public const string WorldBrowser = "WorldBrowser";
        public const string Tools = "Tools";
    }
    
    #endregion
    
    #region Priority Values
    
    public static class Priorities
    {
        public const string Critical = "1";
        public const string High = "2";
        public const string Medium = "3";
        public const string Low = "4";
    }
    
    #endregion
}
```

---

## 14.2 Trait Attributes

```csharp
namespace Oravey.UITestFramework.Core.Testing.Attributes;

using Xunit.Sdk;

/// <summary>
/// Marks a test as a UI test.
/// </summary>
[TraitDiscoverer("Xunit.Sdk.TraitDiscoverer", "xunit.core")]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class UITestAttribute : Attribute, ITraitAttribute
{
    public IEnumerable<KeyValuePair<string, string>> GetTraits()
    {
        yield return new KeyValuePair<string, string>(TestTraits.Category, TestTraits.Categories.UITest);
    }
}

/// <summary>
/// Marks a test as a smoke test.
/// </summary>
[TraitDiscoverer("Xunit.Sdk.TraitDiscoverer", "xunit.core")]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class SmokeTestAttribute : Attribute, ITraitAttribute
{
    public IEnumerable<KeyValuePair<string, string>> GetTraits()
    {
        yield return new KeyValuePair<string, string>(TestTraits.Category, TestTraits.Categories.UITest);
        yield return new KeyValuePair<string, string>(TestTraits.Category, TestTraits.Categories.Smoke);
    }
}

/// <summary>
/// Marks a test as an E2E test.
/// </summary>
[TraitDiscoverer("Xunit.Sdk.TraitDiscoverer", "xunit.core")]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class E2ETestAttribute : Attribute, ITraitAttribute
{
    public IEnumerable<KeyValuePair<string, string>> GetTraits()
    {
        yield return new KeyValuePair<string, string>(TestTraits.Category, TestTraits.Categories.UITest);
        yield return new KeyValuePair<string, string>(TestTraits.Category, TestTraits.Categories.E2E);
    }
}

/// <summary>
/// Marks a test for a specific platform.
/// </summary>
[TraitDiscoverer("Xunit.Sdk.TraitDiscoverer", "xunit.core")]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class PlatformAttribute : Attribute, ITraitAttribute
{
    private readonly string _platform;
    
    public PlatformAttribute(string platform)
    {
        _platform = platform;
    }
    
    public IEnumerable<KeyValuePair<string, string>> GetTraits()
    {
        yield return new KeyValuePair<string, string>(TestTraits.Platform, _platform);
    }
}

/// <summary>
/// Marks a test for a specific feature.
/// </summary>
[TraitDiscoverer("Xunit.Sdk.TraitDiscoverer", "xunit.core")]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class FeatureAttribute : Attribute, ITraitAttribute
{
    private readonly string _feature;
    
    public FeatureAttribute(string feature)
    {
        _feature = feature;
    }
    
    public IEnumerable<KeyValuePair<string, string>> GetTraits()
    {
        yield return new KeyValuePair<string, string>(TestTraits.Feature, _feature);
    }
}

/// <summary>
/// Marks test priority level.
/// </summary>
[TraitDiscoverer("Xunit.Sdk.TraitDiscoverer", "xunit.core")]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class PriorityAttribute : Attribute, ITraitAttribute
{
    private readonly string _priority;
    
    public PriorityAttribute(int priority)
    {
        _priority = priority.ToString();
    }
    
    public IEnumerable<KeyValuePair<string, string>> GetTraits()
    {
        yield return new KeyValuePair<string, string>(TestTraits.Priority, _priority);
    }
}
```

---

## 14.3 Platform Skip Attribute

```csharp
namespace Oravey.UITestFramework.Core.Testing.Attributes;

using Oravey.UITestFramework.Core.Configuration;
using Xunit;

/// <summary>
/// Skip test on specific platforms.
/// </summary>
public class PlatformSkipAttribute : FactAttribute
{
    private readonly Platform[] _skippedPlatforms;
    
    public PlatformSkipAttribute(params Platform[] platforms)
    {
        _skippedPlatforms = platforms;
    }
    
    public PlatformSkipAttribute(Platform platform, string reason) : this(platform)
    {
        Skip = reason;
    }
    
    public override string? Skip
    {
        get
        {
            var currentPlatform = Platform.FromEnvironment();
            
            if (_skippedPlatforms.Contains(currentPlatform))
            {
                return base.Skip ?? $"Skipped on {currentPlatform}";
            }
            
            return null;
        }
        set => base.Skip = value;
    }
}

/// <summary>
/// Run test only on specific platforms.
/// </summary>
public class PlatformOnlyAttribute : FactAttribute
{
    private readonly Platform[] _supportedPlatforms;
    
    public PlatformOnlyAttribute(params Platform[] platforms)
    {
        _supportedPlatforms = platforms;
    }
    
    public override string? Skip
    {
        get
        {
            var currentPlatform = Platform.FromEnvironment();
            
            if (!_supportedPlatforms.Contains(currentPlatform))
            {
                return $"Only runs on {string.Join(", ", _supportedPlatforms)}";
            }
            
            return null;
        }
        set => base.Skip = value;
    }
}
```

---

## 14.4 Test Class Examples

```csharp
namespace Oravey.Tools.Wpf.UITests.Tests;

using Oravey.Tools.Wpf.UITests.Infrastructure;
using Oravey.Tools.Wpf.UITests.PageObjects;
using Oravey.UITestFramework.Core.Testing;
using Oravey.UITestFramework.Core.Testing.Attributes;
using Xunit;
using Xunit.Abstractions;

/// <summary>
/// Navigation smoke tests - run on every build.
/// </summary>
[UITest]
[SmokeTest]
[Platform(TestTraits.Platforms.Windows)]
[Feature(TestTraits.Features.Navigation)]
[Priority(1)]
[Collection("UITests")]
public class NavigationSmokeTests : UITestBase
{
    public NavigationSmokeTests(ITestOutputHelper output) : base(output) { }
    
    [Fact]
    public void Shell_Displays_After_Launch()
    {
        var shell = new ShellPage(Context);
        shell.WaitForPageReady();
        shell.IsDisplayed().Should().BeTrue();
    }
    
    [Fact]
    public void Main_Navigation_Buttons_Visible()
    {
        var shell = new ShellPage(Context);
        shell.WaitForPageReady();
        
        shell.SettingsButton.AssertVisible();
        shell.WorldBrowserButton.AssertVisible();
        shell.ToolsButton.AssertVisible();
    }
}

/// <summary>
/// Settings regression tests - full suite.
/// </summary>
[UITest]
[Trait("Category", "Regression")]
[Platform(TestTraits.Platforms.Windows)]
[Feature(TestTraits.Features.Settings)]
[Priority(2)]
[Collection("UITests")]
public class SettingsRegressionTests : UITestBase
{
    public SettingsRegressionTests(ITestOutputHelper output) : base(output) { }
    
    [Fact]
    public void Settings_Fields_Display_Correctly()
    {
        var shell = new ShellPage(Context);
        shell.WaitForShellReady();
        var settings = shell.NavigateToSettings();
        
        settings.UsernameInput.AssertVisible();
        settings.EmailInput.AssertVisible();
        settings.ThemeDropdown.AssertVisible();
    }
    
    [Fact]
    public void Settings_Validation_Shows_Error()
    {
        var shell = new ShellPage(Context);
        shell.WaitForShellReady();
        var settings = shell.NavigateToSettings();
        
        settings.UsernameInput.EnterText("");  // Invalid
        settings.SaveButton.Click();
        
        settings.ValidationError.WaitForVisible();
        settings.ValidationError.AssertText("Username is required");
    }
    
    [Fact]
    [PlatformSkip(Platform.iOS, "Theme switching not supported on iOS")]
    public void Settings_Theme_Changes_Apply()
    {
        var shell = new ShellPage(Context);
        shell.WaitForShellReady();
        var settings = shell.NavigateToSettings();
        
        settings.SetDarkMode(true);
        settings.SaveAndWait();
        
        settings.IsDarkModeEnabled().Should().BeTrue();
    }
}

/// <summary>
/// End-to-end workflow tests.
/// </summary>
[E2ETest]
[Platform(TestTraits.Platforms.Windows)]
[Priority(3)]
[Collection("UITests")]
public class UserWorkflowE2ETests : UITestBase
{
    public UserWorkflowE2ETests(ITestOutputHelper output) : base(output) { }
    
    [Fact]
    public void Complete_Settings_Update_Workflow()
    {
        // Setup
        var shell = new ShellPage(Context);
        shell.WaitForShellReady();
        
        // Navigate to settings
        var settings = shell.NavigateToSettings();
        
        // Update all fields
        var newUsername = TestHelpers.UniqueValue("User");
        settings.SetUsername(newUsername);
        settings.SetEmail($"{newUsername}@example.com");
        settings.SetDarkMode(true);
        
        // Save
        settings.SaveAndWait();
        
        // Verify persistence
        var returnedShell = settings.NavigateToHome();
        var settingsAgain = returnedShell.NavigateToSettings();
        
        settingsAgain.GetUsername().Should().Be(newUsername);
        settingsAgain.IsDarkModeEnabled().Should().BeTrue();
    }
}
```

---

## 14.5 Test Collection Definition

```csharp
namespace Oravey.Tools.Wpf.UITests.Infrastructure;

using Xunit;

/// <summary>
/// Main UI test collection - tests run sequentially.
/// </summary>
[CollectionDefinition("UITests")]
public class UITestCollection : ICollectionFixture<UITestFixture>
{
    // Collection definition class
}

/// <summary>
/// Mocked API test collection - isolated mock server per test.
/// </summary>
[CollectionDefinition("MockedTests", DisableParallelization = true)]
public class MockedTestCollection
{
    // No fixture needed - each test manages its own mock server
}

/// <summary>
/// Read-only tests that can run in parallel.
/// </summary>
[CollectionDefinition("ReadOnlyTests")]
public class ReadOnlyTestCollection
{
    // These tests only read state, no writes
}
```

---

## 14.6 xunit.runner.json Configuration

```json
{
  "$schema": "https://xunit.net/schema/current/xunit.runner.schema.json",
  "diagnosticMessages": false,
  "internalDiagnosticMessages": false,
  "parallelizeAssembly": false,
  "parallelizeTestCollections": false,
  "maxParallelThreads": 1,
  "methodDisplay": "classAndMethod",
  "methodDisplayOptions": "all",
  "preEnumerateTheories": true,
  "shadowCopy": false,
  "stopOnFail": false,
  "longRunningTestSeconds": 60
}
```

---

## 14.7 Filter Examples Script

```powershell
# run-tests.ps1

param(
    [ValidateSet("All", "Smoke", "Regression", "E2E", "Mocked")]
    [string]$Category = "All",
    
    [ValidateSet("All", "Windows", "WindowsMaui", "Android", "iOS", "Web")]
    [string]$Platform = "All",
    
    [string]$Feature,
    
    [int]$Priority = 0,
    
    [switch]$Verbose
)

$filter = @()

# Category filter
switch ($Category) {
    "Smoke"      { $filter += "Category=Smoke" }
    "Regression" { $filter += "Category=Regression" }
    "E2E"        { $filter += "Category=E2E" }
    "Mocked"     { $filter += "Category=MockedAPITest" }
    "All"        { $filter += "Category=UITest" }
}

# Platform filter
if ($Platform -ne "All") {
    $filter += "Platform=$Platform"
}

# Feature filter
if ($Feature) {
    $filter += "Feature=$Feature"
}

# Priority filter
if ($Priority -gt 0) {
    $filter += "Priority<=$Priority"
}

# Build filter string
$filterString = $filter -join "&"

Write-Host "Running tests with filter: $filterString" -ForegroundColor Cyan

# Build command
$cmd = "dotnet test"
$cmd += " --filter `"$filterString`""

if ($Verbose) {
    $cmd += " --logger `"console;verbosity=detailed`""
}

# Execute
Write-Host $cmd -ForegroundColor Yellow
Invoke-Expression $cmd
```

### Usage Examples

```powershell
# Run smoke tests
.\run-tests.ps1 -Category Smoke

# Run Windows regression tests
.\run-tests.ps1 -Category Regression -Platform Windows

# Run navigation feature tests
.\run-tests.ps1 -Feature Navigation

# Run high priority (1-2) tests
.\run-tests.ps1 -Priority 2

# Run smoke tests on Windows with verbose output
.\run-tests.ps1 -Category Smoke -Platform Windows -Verbose
```

---

*Related: [Running Tests Code Examples](21d15_RunningTests_CodeExamples.md)*
