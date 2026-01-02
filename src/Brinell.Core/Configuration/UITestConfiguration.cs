namespace Brinell.Core.Configuration;

/// <summary>
/// Configuration for UI tests.
/// Implements SPEC-001 Section 7: Configuration Management.
/// </summary>
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
    /// See NFR-REL-003.
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
    /// <param name="platformName">The platform name (e.g., "Windows", "Android").</param>
    /// <returns>The platform configuration, or a new empty configuration if not found.</returns>
    public PlatformConfiguration GetPlatform(string platformName)
    {
        return Platforms.TryGetValue(platformName, out var config) 
            ? config 
            : new PlatformConfiguration();
    }
}

/// <summary>
/// Platform-specific configuration.
/// </summary>
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
    /// <param name="key">The setting key.</param>
    /// <param name="defaultValue">Default value if not found.</param>
    /// <returns>The setting value or default.</returns>
    public string GetSetting(string key, string defaultValue = "")
    {
        return Settings.TryGetValue(key, out var value) ? value : defaultValue;
    }
}
