# 11. Cloud Provider Support - Code Examples

**Parent:** [Cloud Provider Support](21d11_CloudProviderSupport.md)

---

## 11.1 CloudProviderConfig Implementation

```csharp
namespace Oravey.UITestFramework.Core.Configuration;

/// <summary>
/// Cloud testing provider selection.
/// </summary>
public enum CloudProvider
{
    /// <summary>
    /// Run locally (default).
    /// </summary>
    None = 0,
    
    /// <summary>
    /// BrowserStack cloud testing.
    /// </summary>
    BrowserStack = 1,
    
    /// <summary>
    /// SauceLabs cloud testing.
    /// </summary>
    SauceLabs = 2
}

/// <summary>
/// Configuration for cloud testing providers.
/// </summary>
public class CloudProviderConfig
{
    // Provider selection
    public CloudProvider Provider { get; set; } = CloudProvider.None;
    
    // Authentication
    public string? Username { get; set; }
    public string? AccessKey { get; set; }
    
    // Session identification
    public string? Project { get; set; }
    public string? Build { get; set; }
    public string? SessionName { get; set; }
    
    // Hub URL (optional, uses default if not set)
    public string? HubUrl { get; set; }
    
    // Platform options
    public string? DeviceName { get; set; }
    public string? PlatformVersion { get; set; }
    public string? BrowserName { get; set; }
    public string? BrowserVersion { get; set; }
    
    // App options
    public string? AppUrl { get; set; }
    
    // Features
    public bool RealDevice { get; set; } = true;
    public bool NetworkLogs { get; set; } = true;
    public bool Video { get; set; } = true;
    
    public bool IsEnabled => Provider != CloudProvider.None;
    
    #region Default Hub URLs
    
    public string GetHubUrl()
    {
        if (!string.IsNullOrEmpty(HubUrl))
        {
            return HubUrl;
        }
        
        return Provider switch
        {
            CloudProvider.BrowserStack => "https://hub-cloud.browserstack.com/wd/hub",
            CloudProvider.SauceLabs => "https://ondemand.us-west-1.saucelabs.com/wd/hub",
            _ => throw new InvalidOperationException("No hub URL for local execution")
        };
    }
    
    #endregion
    
    #region Factory Methods
    
    /// <summary>
    /// Load configuration from environment variables.
    /// </summary>
    public static CloudProviderConfig FromEnvironment()
    {
        var config = new CloudProviderConfig
        {
            Provider = ParseProvider(Environment.GetEnvironmentVariable("CLOUD_PROVIDER")),
            Username = Environment.GetEnvironmentVariable("CLOUD_USERNAME"),
            AccessKey = Environment.GetEnvironmentVariable("CLOUD_ACCESS_KEY"),
            Project = Environment.GetEnvironmentVariable("CLOUD_PROJECT"),
            Build = Environment.GetEnvironmentVariable("CLOUD_BUILD"),
            HubUrl = Environment.GetEnvironmentVariable("CLOUD_HUB_URL"),
            DeviceName = Environment.GetEnvironmentVariable("CLOUD_DEVICE"),
            PlatformVersion = Environment.GetEnvironmentVariable("CLOUD_PLATFORM_VERSION"),
            BrowserName = Environment.GetEnvironmentVariable("CLOUD_BROWSER"),
            BrowserVersion = Environment.GetEnvironmentVariable("CLOUD_BROWSER_VERSION"),
            AppUrl = Environment.GetEnvironmentVariable("CLOUD_APP_URL")
        };
        
        if (bool.TryParse(Environment.GetEnvironmentVariable("CLOUD_REAL_DEVICE"), out var realDevice))
        {
            config.RealDevice = realDevice;
        }
        
        if (bool.TryParse(Environment.GetEnvironmentVariable("CLOUD_VIDEO"), out var video))
        {
            config.Video = video;
        }
        
        return config;
    }
    
    private static CloudProvider ParseProvider(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return CloudProvider.None;
        }
        
        return value.ToLowerInvariant() switch
        {
            "browserstack" => CloudProvider.BrowserStack,
            "saucelabs" or "sauce" => CloudProvider.SauceLabs,
            _ => CloudProvider.None
        };
    }
    
    /// <summary>
    /// Create BrowserStack configuration.
    /// </summary>
    public static CloudProviderConfig BrowserStack(string username, string accessKey)
    {
        return new CloudProviderConfig
        {
            Provider = CloudProvider.BrowserStack,
            Username = username,
            AccessKey = accessKey
        };
    }
    
    /// <summary>
    /// Create SauceLabs configuration.
    /// </summary>
    public static CloudProviderConfig SauceLabs(string username, string accessKey)
    {
        return new CloudProviderConfig
        {
            Provider = CloudProvider.SauceLabs,
            Username = username,
            AccessKey = accessKey
        };
    }
    
    #endregion
    
    #region Validation
    
    public void Validate()
    {
        if (Provider == CloudProvider.None)
        {
            return;  // Local execution doesn't need validation
        }
        
        if (string.IsNullOrEmpty(Username))
        {
            throw new InvalidOperationException(
                $"{Provider} requires Username. Set CLOUD_USERNAME environment variable.");
        }
        
        if (string.IsNullOrEmpty(AccessKey))
        {
            throw new InvalidOperationException(
                $"{Provider} requires AccessKey. Set CLOUD_ACCESS_KEY environment variable.");
        }
    }
    
    #endregion
}
```

---

## 11.2 Capability Builder

```csharp
namespace Oravey.UITestFramework.Cloud;

using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using Oravey.UITestFramework.Core.Configuration;

/// <summary>
/// Builds capabilities for cloud providers.
/// </summary>
public class CloudCapabilityBuilder
{
    private readonly CloudProviderConfig _config;
    private readonly Platform _platform;
    private readonly string _testName;
    
    public CloudCapabilityBuilder(
        CloudProviderConfig config,
        Platform platform,
        string testName)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _platform = platform;
        _testName = testName ?? throw new ArgumentNullException(nameof(testName));
    }
    
    #region BrowserStack
    
    public AppiumOptions BuildBrowserStackAppiumCapabilities()
    {
        var options = new AppiumOptions();
        
        // BrowserStack options
        options.AddAdditionalAppiumOption("bstack:options", new Dictionary<string, object>
        {
            ["userName"] = _config.Username!,
            ["accessKey"] = _config.AccessKey!,
            ["projectName"] = _config.Project ?? "UITests",
            ["buildName"] = _config.Build ?? "local-build",
            ["sessionName"] = _config.SessionName ?? _testName,
            ["deviceName"] = _config.DeviceName ?? GetDefaultDevice(),
            ["osVersion"] = _config.PlatformVersion ?? GetDefaultPlatformVersion(),
            ["realMobile"] = _config.RealDevice ? "true" : "false",
            ["networkLogs"] = _config.NetworkLogs ? "true" : "false",
            ["video"] = _config.Video ? "true" : "false"
        });
        
        // App location
        if (!string.IsNullOrEmpty(_config.AppUrl))
        {
            options.AddAdditionalAppiumOption("appium:app", _config.AppUrl);
        }
        
        // Platform specific
        if (_platform == Platform.Android)
        {
            options.PlatformName = "android";
            options.AutomationName = "UiAutomator2";
        }
        else if (_platform == Platform.iOS)
        {
            options.PlatformName = "ios";
            options.AutomationName = "XCUITest";
        }
        
        return options;
    }
    
    public ICapabilities BuildBrowserStackWebCapabilities()
    {
        var options = new Dictionary<string, object>
        {
            ["bstack:options"] = new Dictionary<string, object>
            {
                ["userName"] = _config.Username!,
                ["accessKey"] = _config.AccessKey!,
                ["projectName"] = _config.Project ?? "UITests",
                ["buildName"] = _config.Build ?? "local-build",
                ["sessionName"] = _config.SessionName ?? _testName,
                ["os"] = "Windows",
                ["osVersion"] = "11",
                ["browserName"] = _config.BrowserName ?? "Chrome",
                ["browserVersion"] = _config.BrowserVersion ?? "latest",
                ["video"] = _config.Video ? "true" : "false",
                ["networkLogs"] = _config.NetworkLogs ? "true" : "false"
            }
        };
        
        return new DriverOptions().ToCapabilities();
    }
    
    #endregion
    
    #region SauceLabs
    
    public AppiumOptions BuildSauceLabsAppiumCapabilities()
    {
        var options = new AppiumOptions();
        
        // SauceLabs options
        options.AddAdditionalAppiumOption("sauce:options", new Dictionary<string, object>
        {
            ["username"] = _config.Username!,
            ["accessKey"] = _config.AccessKey!,
            ["name"] = _config.SessionName ?? _testName,
            ["build"] = _config.Build ?? "local-build",
            ["deviceName"] = _config.DeviceName ?? GetDefaultDevice(),
            ["platformVersion"] = _config.PlatformVersion ?? GetDefaultPlatformVersion(),
            ["realDevice"] = _config.RealDevice
        });
        
        // App location
        if (!string.IsNullOrEmpty(_config.AppUrl))
        {
            options.AddAdditionalAppiumOption("appium:app", _config.AppUrl);
        }
        
        // Platform specific
        if (_platform == Platform.Android)
        {
            options.PlatformName = "Android";
            options.AutomationName = "UiAutomator2";
        }
        else if (_platform == Platform.iOS)
        {
            options.PlatformName = "iOS";
            options.AutomationName = "XCUITest";
        }
        
        return options;
    }
    
    public Dictionary<string, object> BuildSauceLabsWebCapabilities()
    {
        return new Dictionary<string, object>
        {
            ["sauce:options"] = new Dictionary<string, object>
            {
                ["username"] = _config.Username!,
                ["accessKey"] = _config.AccessKey!,
                ["name"] = _config.SessionName ?? _testName,
                ["build"] = _config.Build ?? "local-build"
            },
            ["browserName"] = _config.BrowserName ?? "chrome",
            ["browserVersion"] = _config.BrowserVersion ?? "latest",
            ["platformName"] = "Windows 11"
        };
    }
    
    #endregion
    
    #region Defaults
    
    private string GetDefaultDevice()
    {
        return _platform switch
        {
            Platform.Android => "Google Pixel 6",
            Platform.iOS => "iPhone 14 Pro",
            _ => throw new InvalidOperationException($"No default device for {_platform}")
        };
    }
    
    private string GetDefaultPlatformVersion()
    {
        return _platform switch
        {
            Platform.Android => "13.0",
            Platform.iOS => "16.0",
            _ => throw new InvalidOperationException($"No default version for {_platform}")
        };
    }
    
    #endregion
}
```

---

## 11.3 Cloud Context Factory Extension

```csharp
namespace Oravey.UITestFramework.Cloud;

using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Appium.iOS;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Chrome;
using Oravey.UITestFramework.Core.Abstractions;
using Oravey.UITestFramework.Core.Configuration;
using Oravey.UITestFramework.Core.Logging;
using Oravey.UITestFramework.Appium.Infrastructure;
using Oravey.UITestFramework.Selenium.Infrastructure;

/// <summary>
/// Factory extension for cloud provider test contexts.
/// </summary>
public static class CloudTestContextFactory
{
    /// <summary>
    /// Create test context for cloud execution.
    /// </summary>
    public static ITestContext CreateCloudContext(
        CloudProviderConfig cloudConfig,
        Platform platform,
        string testName,
        ITestLogger? logger = null)
    {
        cloudConfig.Validate();
        logger ??= new CsvTestLogger();
        
        if (!cloudConfig.IsEnabled)
        {
            // Fallback to local execution
            return TestContextFactory.Create(platform, testName, logger);
        }
        
        return (cloudConfig.Provider, platform.Category()) switch
        {
            (CloudProvider.BrowserStack, PlatformCategory.Mobile) =>
                CreateBrowserStackMobileContext(cloudConfig, platform, testName, logger),
            
            (CloudProvider.BrowserStack, PlatformCategory.Web) =>
                CreateBrowserStackWebContext(cloudConfig, testName, logger),
            
            (CloudProvider.SauceLabs, PlatformCategory.Mobile) =>
                CreateSauceLabsMobileContext(cloudConfig, platform, testName, logger),
            
            (CloudProvider.SauceLabs, PlatformCategory.Web) =>
                CreateSauceLabsWebContext(cloudConfig, testName, logger),
            
            _ => throw new NotSupportedException(
                $"Cloud execution not supported for {cloudConfig.Provider} + {platform}")
        };
    }
    
    #region BrowserStack
    
    private static ITestContext CreateBrowserStackMobileContext(
        CloudProviderConfig config,
        Platform platform,
        string testName,
        ITestLogger logger)
    {
        var builder = new CloudCapabilityBuilder(config, platform, testName);
        var capabilities = builder.BuildBrowserStackAppiumCapabilities();
        var hubUrl = new Uri(config.GetHubUrl());
        
        AppiumDriver driver = platform switch
        {
            Platform.Android => new AndroidDriver(hubUrl, capabilities),
            Platform.iOS => new IOSDriver(hubUrl, capabilities),
            _ => throw new NotSupportedException($"Platform {platform} not supported on BrowserStack")
        };
        
        var driverAdapter = new AppiumDriverAdapter(driver, platform);
        
        return new AppiumTestContext(
            driverAdapter,
            platform,
            testName,
            logger,
            CreateCloudConfiguration(config));
    }
    
    private static ITestContext CreateBrowserStackWebContext(
        CloudProviderConfig config,
        string testName,
        ITestLogger logger)
    {
        var hubUrl = new Uri(config.GetHubUrl());
        
        var options = new ChromeOptions();
        options.AddAdditionalOption("bstack:options", new Dictionary<string, object>
        {
            ["userName"] = config.Username!,
            ["accessKey"] = config.AccessKey!,
            ["projectName"] = config.Project ?? "UITests",
            ["buildName"] = config.Build ?? "local-build",
            ["sessionName"] = config.SessionName ?? testName,
            ["os"] = "Windows",
            ["osVersion"] = "11"
        });
        
        var driver = new RemoteWebDriver(hubUrl, options);
        var driverAdapter = new SeleniumDriverAdapter(driver);
        
        return new SeleniumTestContext(
            driverAdapter,
            Platform.Web,
            testName,
            logger,
            CreateCloudConfiguration(config));
    }
    
    #endregion
    
    #region SauceLabs
    
    private static ITestContext CreateSauceLabsMobileContext(
        CloudProviderConfig config,
        Platform platform,
        string testName,
        ITestLogger logger)
    {
        var builder = new CloudCapabilityBuilder(config, platform, testName);
        var capabilities = builder.BuildSauceLabsAppiumCapabilities();
        var hubUrl = new Uri(config.GetHubUrl());
        
        AppiumDriver driver = platform switch
        {
            Platform.Android => new AndroidDriver(hubUrl, capabilities),
            Platform.iOS => new IOSDriver(hubUrl, capabilities),
            _ => throw new NotSupportedException($"Platform {platform} not supported on SauceLabs")
        };
        
        var driverAdapter = new AppiumDriverAdapter(driver, platform);
        
        return new AppiumTestContext(
            driverAdapter,
            platform,
            testName,
            logger,
            CreateCloudConfiguration(config));
    }
    
    private static ITestContext CreateSauceLabsWebContext(
        CloudProviderConfig config,
        string testName,
        ITestLogger logger)
    {
        var hubUrl = new Uri(config.GetHubUrl());
        
        var options = new ChromeOptions();
        options.AddAdditionalOption("sauce:options", new Dictionary<string, object>
        {
            ["username"] = config.Username!,
            ["accessKey"] = config.AccessKey!,
            ["name"] = config.SessionName ?? testName,
            ["build"] = config.Build ?? "local-build"
        });
        options.PlatformName = "Windows 11";
        
        var driver = new RemoteWebDriver(hubUrl, options);
        var driverAdapter = new SeleniumDriverAdapter(driver);
        
        return new SeleniumTestContext(
            driverAdapter,
            Platform.Web,
            testName,
            logger,
            CreateCloudConfiguration(config));
    }
    
    #endregion
    
    #region Helpers
    
    private static TestConfiguration CreateCloudConfiguration(CloudProviderConfig cloudConfig)
    {
        return new TestConfiguration
        {
            DefaultTimeoutMs = 30000,  // Cloud tests need longer timeouts
            ShortTimeoutMs = 10000,
            LongTimeoutMs = 120000,
            PollIntervalMs = 500,
            CloudProvider = cloudConfig
        };
    }
    
    #endregion
}
```

---

## 11.4 Session Status Reporter

```csharp
namespace Oravey.UITestFramework.Cloud;

using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Oravey.UITestFramework.Core.Configuration;

/// <summary>
/// Reports test session results to cloud providers.
/// </summary>
public class CloudSessionReporter
{
    private readonly CloudProviderConfig _config;
    private readonly HttpClient _client;
    
    public CloudSessionReporter(CloudProviderConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _client = new HttpClient();
        ConfigureAuth();
    }
    
    private void ConfigureAuth()
    {
        var credentials = $"{_config.Username}:{_config.AccessKey}";
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", encoded);
    }
    
    #region BrowserStack
    
    /// <summary>
    /// Update BrowserStack session status.
    /// </summary>
    public async Task UpdateBrowserStackSessionAsync(
        string sessionId,
        bool passed,
        string? reason = null)
    {
        var url = $"https://api.browserstack.com/automate/sessions/{sessionId}.json";
        
        var body = new
        {
            status = passed ? "passed" : "failed",
            reason = reason
        };
        
        var content = new StringContent(
            JsonSerializer.Serialize(body),
            Encoding.UTF8,
            "application/json");
        
        await _client.PutAsync(url, content);
    }
    
    #endregion
    
    #region SauceLabs
    
    /// <summary>
    /// Update SauceLabs job status.
    /// </summary>
    public async Task UpdateSauceLabsJobAsync(
        string jobId,
        bool passed,
        Dictionary<string, object>? customData = null)
    {
        var url = $"https://saucelabs.com/rest/v1/{_config.Username}/jobs/{jobId}";
        
        var body = new Dictionary<string, object>
        {
            ["passed"] = passed
        };
        
        if (customData != null)
        {
            body["custom-data"] = customData;
        }
        
        var content = new StringContent(
            JsonSerializer.Serialize(body),
            Encoding.UTF8,
            "application/json");
        
        await _client.PutAsync(url, content);
    }
    
    #endregion
    
    #region Generic Update
    
    /// <summary>
    /// Update session status based on configured provider.
    /// </summary>
    public async Task UpdateSessionAsync(
        string sessionId,
        bool passed,
        string? reason = null)
    {
        if (!_config.IsEnabled)
        {
            return;
        }
        
        switch (_config.Provider)
        {
            case CloudProvider.BrowserStack:
                await UpdateBrowserStackSessionAsync(sessionId, passed, reason);
                break;
                
            case CloudProvider.SauceLabs:
                await UpdateSauceLabsJobAsync(sessionId, passed);
                break;
        }
    }
    
    #endregion
}
```

---

## 11.5 Cloud Test Base

```csharp
namespace Oravey.Tools.UITests.Infrastructure;

using Oravey.UITestFramework.Cloud;
using Oravey.UITestFramework.Core.Abstractions;
using Oravey.UITestFramework.Core.Configuration;
using Oravey.UITestFramework.Core.Logging;
using Xunit;
using Xunit.Abstractions;

/// <summary>
/// Base class for tests that can run on cloud providers.
/// </summary>
public abstract class CloudUITestBase : IDisposable
{
    protected ITestContext Context { get; }
    protected ITestLogger Logger { get; }
    protected string TestName { get; }
    protected CloudProviderConfig CloudConfig { get; }
    
    private readonly CloudSessionReporter? _sessionReporter;
    private string? _sessionId;
    private bool _testPassed;
    
    protected CloudUITestBase(ITestOutputHelper output)
    {
        TestName = GetType().Name;
        Logger = new CsvTestLogger(output);
        CloudConfig = CloudProviderConfig.FromEnvironment();
        
        var platform = Platform.FromEnvironment();
        
        Context = CloudTestContextFactory.CreateCloudContext(
            CloudConfig,
            platform,
            TestName,
            Logger);
        
        if (CloudConfig.IsEnabled)
        {
            _sessionReporter = new CloudSessionReporter(CloudConfig);
            _sessionId = ExtractSessionId();
        }
        
        Logger.LogInfo(TestName, "Setup",
            $"Started on {CloudConfig.Provider} ({platform})");
    }
    
    private string? ExtractSessionId()
    {
        // Extract session ID from driver
        // Implementation depends on driver type
        return Context.Driver.GetSessionId();
    }
    
    /// <summary>
    /// Mark test as passed (call at end of successful test).
    /// </summary>
    protected void MarkPassed()
    {
        _testPassed = true;
    }
    
    public virtual void Dispose()
    {
        try
        {
            // Report session result to cloud provider
            if (_sessionReporter != null && !string.IsNullOrEmpty(_sessionId))
            {
                _sessionReporter.UpdateSessionAsync(
                    _sessionId,
                    _testPassed,
                    _testPassed ? null : "Test failed")
                    .GetAwaiter().GetResult();
            }
            
            Logger.LogInfo(TestName, "Cleanup",
                $"Finished: {(_testPassed ? "PASSED" : "FAILED")}");
        }
        finally
        {
            Context.Dispose();
        }
    }
}
```

---

## 11.6 CI/CD Configuration Examples

### GitHub Actions

```yaml
name: Cloud UI Tests

on:
  push:
    branches: [main]
  workflow_dispatch:
    inputs:
      cloud_provider:
        description: 'Cloud provider'
        required: true
        default: 'BrowserStack'
        type: choice
        options:
          - None
          - BrowserStack
          - SauceLabs

jobs:
  ui-tests:
    runs-on: ubuntu-latest
    strategy:
      matrix:
        platform: [Android, iOS, Web]
    
    env:
      CLOUD_PROVIDER: ${{ github.event.inputs.cloud_provider || 'BrowserStack' }}
      CLOUD_USERNAME: ${{ secrets.CLOUD_USERNAME }}
      CLOUD_ACCESS_KEY: ${{ secrets.CLOUD_ACCESS_KEY }}
      CLOUD_PROJECT: Oravey
      CLOUD_BUILD: ${{ github.run_number }}-${{ github.sha }}
      PLATFORM: ${{ matrix.platform }}
    
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'
      
      - name: Run UI Tests
        run: |
          dotnet test ./src/Oravey.Tools.UITests \
            --filter "Category=UITest" \
            --logger "trx;LogFileName=results.trx"
      
      - name: Upload Results
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: test-results-${{ matrix.platform }}
          path: "**/results.trx"
```

### Azure DevOps

```yaml
trigger:
  branches:
    include:
      - main

pool:
  vmImage: 'windows-latest'

variables:
  - group: CloudProviderSecrets
  - name: CLOUD_PROVIDER
    value: 'BrowserStack'
  - name: CLOUD_PROJECT
    value: 'Oravey'
  - name: CLOUD_BUILD
    value: '$(Build.BuildNumber)'

stages:
  - stage: UITests
    jobs:
      - job: RunTests
        strategy:
          matrix:
            Android:
              PLATFORM: 'Android'
            iOS:
              PLATFORM: 'iOS'
            Web:
              PLATFORM: 'Web'
        steps:
          - task: UseDotNet@2
            inputs:
              version: '9.0.x'
          
          - task: DotNetCoreCLI@2
            displayName: 'Run UI Tests'
            inputs:
              command: 'test'
              projects: '**/Oravey.Tools.UITests.csproj'
              arguments: '--filter "Category=UITest" --logger trx'
            env:
              CLOUD_USERNAME: $(CloudUsername)
              CLOUD_ACCESS_KEY: $(CloudAccessKey)
          
          - task: PublishTestResults@2
            condition: always()
            inputs:
              testResultsFormat: 'VSTest'
              testResultsFiles: '**/*.trx'
```

---

*Related: [Standardized Logging Code Examples](21d12_StandardizedLogging_CodeExamples.md)*
