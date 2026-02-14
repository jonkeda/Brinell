# 221_002 Configuration

## foundation Configuration

- **title**: Test Configuration and Settings Management
- **package**: Brinell.Core.Configuration
- **purpose**: Centralized configuration for timeouts, paths, and platform settings

---

## Description

The Configuration foundation provides centralized management of test settings including timeouts, output paths, and platform-specific configurations. Configuration is designed to be loaded from files, environment variables, or set programmatically.

> **Note:** Code snippets in this document are illustrative examples showing architectural patterns. Actual implementation may vary. See source code for current implementation details.

---

## 1. Core Configuration Class

### 1.1 UITestConfiguration

```csharp
public class UITestConfiguration
{
    /// <summary>
    /// Platform-specific configurations keyed by platform name.
    /// </summary>
    public Dictionary<string, PlatformConfiguration> Platforms { get; set; } = new();
    
    /// <summary>
    /// Default timeout in milliseconds for Wait operations.
    /// </summary>
    public int DefaultTimeoutMs { get; set; } = 10000;
    
    /// <summary>
    /// Short timeout in milliseconds for quick checks.
    /// </summary>
    public int ShortTimeoutMs { get; set; } = 3000;
    
    /// <summary>
    /// Polling interval in milliseconds for Wait operations.
    /// </summary>
    public int PollingIntervalMs { get; set; } = 250;
    
    /// <summary>
    /// Path for log output files.
    /// </summary>
    public string LogOutputPath { get; set; } = "logs";
    
    /// <summary>
    /// Path for screenshot output files.
    /// </summary>
    public string ScreenshotPath { get; set; } = "screenshots";
    
    /// <summary>
    /// Maximum test execution time in milliseconds.
    /// </summary>
    public int TestTimeoutMs { get; set; } = 120000; // 2 minutes
    
    /// <summary>
    /// Maximum setup time in milliseconds.
    /// </summary>
    public int SetupTimeoutMs { get; set; } = 60000; // 1 minute
    
    /// <summary>
    /// Maximum teardown time in milliseconds.
    /// </summary>
    public int TeardownTimeoutMs { get; set; } = 30000; // 30 seconds
    
    /// <summary>
    /// Get configuration for a specific platform.
    /// </summary>
    public PlatformConfiguration GetPlatform(string platformName)
    {
        return Platforms.TryGetValue(platformName, out var config) 
            ? config 
            : new PlatformConfiguration();
    }
}
```

### 1.2 PlatformConfiguration

```csharp
public class PlatformConfiguration
{
    /// <summary>
    /// Path to the application executable.
    /// </summary>
    public string? ApplicationPath { get; set; }
    
    /// <summary>
    /// Base URL for web applications.
    /// </summary>
    public string? BaseUrl { get; set; }
    
    /// <summary>
    /// Browser type for web testing (Chrome, Firefox, Edge, Safari).
    /// </summary>
    public string? BrowserType { get; set; }
    
    /// <summary>
    /// Command line arguments for the application.
    /// </summary>
    public string? Arguments { get; set; }
    
    /// <summary>
    /// Whether to run in headless mode (for browsers).
    /// </summary>
    public bool Headless { get; set; } = false;
    
    /// <summary>
    /// Platform-specific timeout override in milliseconds.
    /// </summary>
    public int? DefaultTimeoutMs { get; set; }
    
    /// <summary>
    /// Additional platform-specific settings.
    /// </summary>
    public Dictionary<string, string> Settings { get; set; } = new();
    
    /// <summary>
    /// Get a platform-specific setting.
    /// </summary>
    public string GetSetting(string key, string defaultValue = "")
    {
        return Settings.TryGetValue(key, out var value) ? value : defaultValue;
    }
}
```

---

## 2. Timeout Configuration

### 2.1 Timeout Categories

| Category | Setting | Default | Purpose |
|----------|---------|---------|---------|
| **Element** | DefaultTimeoutMs | 10000ms | Wait for element visibility/existence |
| **Quick** | ShortTimeoutMs | 3000ms | Quick state checks |
| **Page** | PageLoadTimeoutMs | 30000ms | Wait for page to fully load |
| **Test** | TestTimeoutMs | 120000ms | Maximum test execution time |
| **Setup** | SetupTimeoutMs | 60000ms | Test fixture setup |
| **Teardown** | TeardownTimeoutMs | 30000ms | Test fixture cleanup |

### 2.2 Polling Configuration

| Setting | Default | Purpose |
|---------|---------|---------|
| PollingIntervalMs | 250ms | Interval between condition checks |
| AnimationDelayMs | 500ms | Wait for animations to complete |

---

## 3. Configuration Sources

### 3.1 JSON Configuration File

```json
{
  "DefaultTimeoutMs": 10000,
  "ShortTimeoutMs": 3000,
  "PollingIntervalMs": 250,
  "LogOutputPath": "logs",
  "ScreenshotPath": "screenshots",
  "TestTimeoutMs": 120000,
  "Platforms": {
    "Windows": {
      "ApplicationPath": "C:\\Apps\\MyApp.exe",
      "DefaultTimeoutMs": 15000
    },
    "Android": {
      "ApplicationPath": "com.company.myapp",
      "DefaultTimeoutMs": 20000,
      "Settings": {
        "DeviceName": "Pixel 6",
        "PlatformVersion": "13"
      }
    },
    "Blazor": {
      "BaseUrl": "https://localhost:5001",
      "BrowserType": "Chrome",
      "Headless": true
    }
  }
}
```

### 3.2 Environment Variables

Environment variables override file settings:

| Variable | Maps To |
|----------|---------|
| BRINELL_DEFAULT_TIMEOUT | DefaultTimeoutMs |
| BRINELL_LOG_PATH | LogOutputPath |
| BRINELL_SCREENSHOT_PATH | ScreenshotPath |
| BRINELL_TEST_TIMEOUT | TestTimeoutMs |

### 3.3 Programmatic Configuration

```csharp
var config = new UITestConfiguration
{
    DefaultTimeoutMs = 15000,
    ShortTimeoutMs = 5000,
    LogOutputPath = "test-output/logs",
    ScreenshotPath = "test-output/screenshots"
};

config.Platforms["Windows"] = new PlatformConfiguration
{
    ApplicationPath = @"C:\Apps\MyApp.exe",
    DefaultTimeoutMs = 20000
};
```

---

## 4. Configuration Loading

### 4.1 Configuration Loader

```csharp
public static class ConfigurationLoader
{
    public static UITestConfiguration Load(string? filePath = null)
    {
        var config = new UITestConfiguration();
        
        // 1. Load from file if exists
        var path = filePath ?? FindConfigFile();
        if (path != null && File.Exists(path))
        {
            var json = File.ReadAllText(path);
            config = JsonSerializer.Deserialize<UITestConfiguration>(json) 
                ?? new UITestConfiguration();
        }
        
        // 2. Override from environment variables
        ApplyEnvironmentOverrides(config);
        
        return config;
    }
    
    private static string? FindConfigFile()
    {
        var candidates = new[]
        {
            "brinell.json",
            "uitest.config.json",
            "testsettings.json"
        };
        
        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
                return candidate;
        }
        
        return null;
    }
    
    private static void ApplyEnvironmentOverrides(UITestConfiguration config)
    {
        if (int.TryParse(Environment.GetEnvironmentVariable("BRINELL_DEFAULT_TIMEOUT"), out var timeout))
            config.DefaultTimeoutMs = timeout;
            
        var logPath = Environment.GetEnvironmentVariable("BRINELL_LOG_PATH");
        if (!string.IsNullOrEmpty(logPath))
            config.LogOutputPath = logPath;
            
        var screenshotPath = Environment.GetEnvironmentVariable("BRINELL_SCREENSHOT_PATH");
        if (!string.IsNullOrEmpty(screenshotPath))
            config.ScreenshotPath = screenshotPath;
            
        if (int.TryParse(Environment.GetEnvironmentVariable("BRINELL_TEST_TIMEOUT"), out var testTimeout))
            config.TestTimeoutMs = testTimeout;
    }
}
```

---

## 5. Platform-Specific Configuration

### 5.1 MAUI/Appium Configuration

```csharp
var mauiConfig = new PlatformConfiguration
{
    ApplicationPath = "com.company.myapp",
    Settings = new Dictionary<string, string>
    {
        ["appium:automationName"] = "UiAutomator2",
        ["appium:deviceName"] = "Android Emulator",
        ["appium:platformVersion"] = "13",
        ["appium:noReset"] = "true"
    }
};
```

### 5.2 Blazor/Selenium Configuration

```csharp
var blazorConfig = new PlatformConfiguration
{
    BaseUrl = "https://localhost:5001",
    BrowserType = "Chrome",
    Headless = true,
    Settings = new Dictionary<string, string>
    {
        ["windowSize"] = "1920x1080",
        ["implicitWait"] = "0"  // Brinell handles waits
    }
};
```

### 5.3 WPF/FlaUI Configuration

```csharp
var wpfConfig = new PlatformConfiguration
{
    ApplicationPath = @"C:\Apps\MyWpfApp.exe",
    Arguments = "--test-mode",
    Settings = new Dictionary<string, string>
    {
        ["uiaVersion"] = "UIA3",
        ["highlightOnFind"] = "false"
    }
};
```

---

## 6. Configuration in Test Context

### 6.1 Context Configuration Integration

```csharp
public class MauiTestContext : IMauiTestContext
{
    public UITestConfiguration Configuration { get; }
    public int DefaultTimeoutMs => Configuration.DefaultTimeoutMs;
    
    public MauiTestContext(AppiumOptions options, UITestConfiguration? config = null)
    {
        Configuration = config ?? ConfigurationLoader.Load();
        // Apply platform-specific overrides
        var platform = Configuration.GetPlatform("Android");
        if (platform.DefaultTimeoutMs.HasValue)
        {
            // Use platform-specific timeout
        }
    }
}
```

### 6.2 Timeout Resolution

Timeouts resolve in priority order:

1. **Method parameter** - Explicit timeout passed to method
2. **Platform configuration** - Platform-specific override
3. **Global configuration** - UITestConfiguration default
4. **Framework default** - Hard-coded fallback

```csharp
public int ResolveTimeout(int? methodTimeout, string platform)
{
    // 1. Method parameter takes precedence
    if (methodTimeout.HasValue)
        return methodTimeout.Value;
    
    // 2. Platform-specific override
    var platformConfig = Configuration.GetPlatform(platform);
    if (platformConfig.DefaultTimeoutMs.HasValue)
        return platformConfig.DefaultTimeoutMs.Value;
    
    // 3. Global configuration
    return Configuration.DefaultTimeoutMs;
}
```

---

## 7. CI/CD Configuration

### 7.1 Environment-Based Configuration

```yaml
# Azure DevOps pipeline
variables:
  BRINELL_DEFAULT_TIMEOUT: 20000
  BRINELL_LOG_PATH: $(Build.ArtifactStagingDirectory)/logs
  BRINELL_SCREENSHOT_PATH: $(Build.ArtifactStagingDirectory)/screenshots
  BRINELL_TEST_TIMEOUT: 180000

# GitHub Actions
env:
  BRINELL_DEFAULT_TIMEOUT: 20000
  BRINELL_LOG_PATH: ${{ github.workspace }}/logs
```

### 7.2 Configuration Profiles

Different configurations for different environments:

```
config/
├── brinell.dev.json      # Development (shorter timeouts)
├── brinell.ci.json       # CI/CD (longer timeouts, headless)
├── brinell.prod.json     # Production testing
```

```csharp
// Load profile based on environment
var profile = Environment.GetEnvironmentVariable("BRINELL_PROFILE") ?? "dev";
var config = ConfigurationLoader.Load($"config/brinell.{profile}.json");
```

---

## 8. Validation Rules

The Configuration foundation is valid when:

- [ ] UITestConfiguration has sensible defaults
- [ ] Platform-specific settings are isolated in PlatformConfiguration
- [ ] Environment variables can override file settings
- [ ] Configuration loading is fail-safe (returns defaults on error)
- [ ] Timeout resolution follows priority order
- [ ] Path settings create directories if needed
- [ ] Sensitive settings (credentials) are not in config files

---

## Related Documents

- [221_001 Logging](221_001_Logging.spx.md)
- [221_004 Timeout](221_004_Timeout.spx.md)
- [211_004 PageContext](../211_Modules/211_004_PageContext.spx.md)
- [220 External Dependencies](../220_External/220_INDEX.md)
