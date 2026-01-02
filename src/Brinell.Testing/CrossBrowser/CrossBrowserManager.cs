using System.Collections.Concurrent;

namespace Brinell.Testing.CrossBrowser;

/// <summary>
/// Cross-browser testing support with capability detection and browser-specific assertions.
/// </summary>
public class CrossBrowserManager
{
    private readonly BrowserType _browserType;
    private readonly BrowserCapabilities _capabilities;
    private readonly ConcurrentDictionary<string, object> _browserFeatures = new();

    public CrossBrowserManager(BrowserType browserType)
    {
        _browserType = browserType;
        _capabilities = GetCapabilitiesForBrowser(browserType);
    }

    /// <summary>
    /// Get the current browser type.
    /// </summary>
    public BrowserType CurrentBrowser => _browserType;

    /// <summary>
    /// Get browser capabilities.
    /// </summary>
    public BrowserCapabilities Capabilities => _capabilities;

    /// <summary>
    /// Check if browser supports specific capability.
    /// </summary>
    public bool Supports(string capabilityName)
    {
        return capabilityName switch
        {
            "webdriver" => _capabilities.SupportsWebDriver,
            "headless" => _capabilities.SupportsHeadlessMode,
            "webgl" => _capabilities.SupportsWebGL,
            "serviceWorker" => _capabilities.SupportsServiceWorker,
            "webp" => _capabilities.SupportsWebP,
            "videoPlayback" => _capabilities.SupportsVideoPlayback,
            "shadowDOM" => _capabilities.SupportsShadowDOM,
            "cssGrid" => _capabilities.SupportsCSSGrid,
            "flexbox" => _capabilities.SupportsFlexbox,
            "customElements" => _capabilities.SupportsCustomElements,
            "intersectionObserver" => _capabilities.SupportsIntersectionObserver,
            _ => false
        };
    }

    /// <summary>
    /// Skip test if browser doesn't support feature.
    /// </summary>
    public void SkipIfNotSupported(string capabilityName)
    {
        if (!Supports(capabilityName))
        {
            throw new SkipTestException($"{_browserType} does not support {capabilityName}");
        }
    }

    /// <summary>
    /// Assert browser-specific behavior.
    /// </summary>
    public void AssertBrowserBehavior(BrowserType expectedBrowser, string assertion)
    {
        if (_browserType != expectedBrowser)
        {
            throw new BrowserSpecificException(
                $"Test assertion '{assertion}' only valid for {expectedBrowser}, current browser is {_browserType}");
        }
    }

    /// <summary>
    /// Assert feature is supported.
    /// </summary>
    public void AssertFeatureSupported(string featureName)
    {
        if (!Supports(featureName))
        {
            throw new BrowserSpecificException(
                $"{_browserType} does not support {featureName}");
        }
    }

    /// <summary>
    /// Register browser-specific behavior or workaround.
    /// </summary>
    public void RegisterFeature(string featureName, object featureData)
    {
        _browserFeatures[featureName] = featureData;
    }

    /// <summary>
    /// Get registered feature.
    /// </summary>
    public T? GetFeature<T>(string featureName) where T : class
    {
        return _browserFeatures.TryGetValue(featureName, out var value)
            ? value as T
            : null;
    }

    /// <summary>
    /// Get browser-specific wait timeout (some browsers are slower).
    /// </summary>
    public TimeSpan GetOptimalTimeout()
    {
        return _browserType switch
        {
            BrowserType.Safari => TimeSpan.FromSeconds(15), // Safari is typically slower
            BrowserType.Firefox => TimeSpan.FromSeconds(12), // Firefox slightly slower
            BrowserType.Edge => TimeSpan.FromSeconds(10),
            BrowserType.Chrome => TimeSpan.FromSeconds(10),
            _ => TimeSpan.FromSeconds(10)
        };
    }

    /// <summary>
    /// Check if running in CI environment.
    /// </summary>
    public bool IsHeadless => _capabilities.SupportsHeadlessMode;

    /// <summary>
    /// Get browser version (if available).
    /// </summary>
    public Version? BrowserVersion => _capabilities.Version;

    /// <summary>
    /// Get operating system.
    /// </summary>
    public OperatingSystem OS => _capabilities.OperatingSystem;

    private BrowserCapabilities GetCapabilitiesForBrowser(BrowserType browserType)
    {
        return browserType switch
        {
            BrowserType.Chrome => new BrowserCapabilities
            {
                BrowserName = "Chrome",
                BrowserType = BrowserType.Chrome,
                SupportsWebDriver = true,
                SupportsHeadlessMode = true,
                SupportsWebGL = true,
                SupportsServiceWorker = true,
                SupportsWebP = true,
                SupportsVideoPlayback = true,
                SupportsShadowDOM = true,
                SupportsCSSGrid = true,
                SupportsFlexbox = true,
                SupportsCustomElements = true,
                SupportsIntersectionObserver = true,
                Version = GetChromeVersion(),
                OperatingSystem = DetectOS()
            },
            BrowserType.Firefox => new BrowserCapabilities
            {
                BrowserName = "Firefox",
                BrowserType = BrowserType.Firefox,
                SupportsWebDriver = true,
                SupportsHeadlessMode = true,
                SupportsWebGL = true,
                SupportsServiceWorker = true,
                SupportsWebP = false,
                SupportsVideoPlayback = true,
                SupportsShadowDOM = true,
                SupportsCSSGrid = true,
                SupportsFlexbox = true,
                SupportsCustomElements = true,
                SupportsIntersectionObserver = true,
                Version = GetFirefoxVersion(),
                OperatingSystem = DetectOS()
            },
            BrowserType.Safari => new BrowserCapabilities
            {
                BrowserName = "Safari",
                BrowserType = BrowserType.Safari,
                SupportsWebDriver = true,
                SupportsHeadlessMode = false, // Safari doesn't support headless
                SupportsWebGL = true,
                SupportsServiceWorker = false,
                SupportsWebP = false,
                SupportsVideoPlayback = true,
                SupportsShadowDOM = true,
                SupportsCSSGrid = true,
                SupportsFlexbox = true,
                SupportsCustomElements = false,
                SupportsIntersectionObserver = true,
                Version = GetSafariVersion(),
                OperatingSystem = OperatingSystem.MacOS
            },
            BrowserType.Edge => new BrowserCapabilities
            {
                BrowserName = "Edge",
                BrowserType = BrowserType.Edge,
                SupportsWebDriver = true,
                SupportsHeadlessMode = true,
                SupportsWebGL = true,
                SupportsServiceWorker = true,
                SupportsWebP = true,
                SupportsVideoPlayback = true,
                SupportsShadowDOM = true,
                SupportsCSSGrid = true,
                SupportsFlexbox = true,
                SupportsCustomElements = true,
                SupportsIntersectionObserver = true,
                Version = GetEdgeVersion(),
                OperatingSystem = DetectOS()
            },
            _ => throw new ArgumentException($"Unknown browser type: {browserType}")
        };
    }

    private Version? GetChromeVersion()
    {
        try
        {
            // In real implementation, would detect from environment
            return new Version(120, 0, 0, 0);
        }
        catch
        {
            return null;
        }
    }

    private Version? GetFirefoxVersion()
    {
        try
        {
            return new Version(121, 0, 0, 0);
        }
        catch
        {
            return null;
        }
    }

    private Version? GetSafariVersion()
    {
        try
        {
            return new Version(17, 1, 0, 0);
        }
        catch
        {
            return null;
        }
    }

    private Version? GetEdgeVersion()
    {
        try
        {
            return new Version(120, 0, 0, 0);
        }
        catch
        {
            return null;
        }
    }

    private OperatingSystem DetectOS()
    {
        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
            System.Runtime.InteropServices.OSPlatform.Windows))
        {
            return OperatingSystem.Windows;
        }
        else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
            System.Runtime.InteropServices.OSPlatform.OSX))
        {
            return OperatingSystem.MacOS;
        }
        else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
            System.Runtime.InteropServices.OSPlatform.Linux))
        {
            return OperatingSystem.Linux;
        }

        return OperatingSystem.Unknown;
    }
}

/// <summary>
/// Browser capabilities and feature support.
/// </summary>
public class BrowserCapabilities
{
    public required string BrowserName { get; set; }
    public required BrowserType BrowserType { get; set; }
    public required bool SupportsWebDriver { get; set; }
    public required bool SupportsHeadlessMode { get; set; }
    public required bool SupportsWebGL { get; set; }
    public required bool SupportsServiceWorker { get; set; }
    public required bool SupportsWebP { get; set; }
    public required bool SupportsVideoPlayback { get; set; }
    public required bool SupportsShadowDOM { get; set; }
    public required bool SupportsCSSGrid { get; set; }
    public required bool SupportsFlexbox { get; set; }
    public required bool SupportsCustomElements { get; set; }
    public required bool SupportsIntersectionObserver { get; set; }
    public Version? Version { get; set; }
    public required OperatingSystem OperatingSystem { get; set; }
}

/// <summary>
/// Supported browser types.
/// </summary>
public enum BrowserType
{
    Chrome,
    Firefox,
    Safari,
    Edge
}

/// <summary>
/// Operating system types.
/// </summary>
public enum OperatingSystem
{
    Windows,
    MacOS,
    Linux,
    Unknown
}

/// <summary>
/// Exception for browser-specific test issues.
/// </summary>
public class BrowserSpecificException : Exception
{
    public BrowserSpecificException(string message) : base(message) { }
}

/// <summary>
/// Exception to skip test (test framework will handle).
/// </summary>
public class SkipTestException : Exception
{
    public SkipTestException(string message) : base(message) { }
}

/// <summary>
/// Builder for cross-browser test configuration.
/// </summary>
public class CrossBrowserBuilder
{
    private readonly List<BrowserType> _browsers = new();
    private readonly ConcurrentDictionary<BrowserType, List<string>> _browserFeatures = new();

    /// <summary>
    /// Add browser to test matrix.
    /// </summary>
    public CrossBrowserBuilder OnBrowser(BrowserType browserType)
    {
        if (!_browsers.Contains(browserType))
        {
            _browsers.Add(browserType);
        }
        return this;
    }

    /// <summary>
    /// Add all major browsers.
    /// </summary>
    public CrossBrowserBuilder OnAllBrowsers()
    {
        _browsers.Clear();
        _browsers.AddRange(new[]
        {
            BrowserType.Chrome,
            BrowserType.Firefox,
            BrowserType.Safari,
            BrowserType.Edge
        });
        return this;
    }

    /// <summary>
    /// Require specific feature on browser.
    /// </summary>
    public CrossBrowserBuilder RequireFeature(BrowserType browser, string feature)
    {
        _browserFeatures.AddOrUpdate(
            browser,
            new List<string> { feature },
            (_, list) =>
            {
                list.Add(feature);
                return list;
            });
        return this;
    }

    /// <summary>
    /// Get configured browsers.
    /// </summary>
    public IReadOnlyList<BrowserType> GetBrowsers() => _browsers.AsReadOnly();

    /// <summary>
    /// Get required features for browser.
    /// </summary>
    public IReadOnlyList<string> GetRequiredFeatures(BrowserType browser)
    {
        return _browserFeatures.TryGetValue(browser, out var features)
            ? features.AsReadOnly()
            : Array.Empty<string>();
    }
}

/// <summary>
/// Extension methods for cross-browser testing.
/// </summary>
public static class CrossBrowserExtensions
{
    /// <summary>
    /// Create cross-browser builder.
    /// </summary>
    public static CrossBrowserBuilder CreateBrowserMatrix() => new();

    /// <summary>
    /// Create manager for browser.
    /// </summary>
    public static CrossBrowserManager CreateBrowserManager(this BrowserType browserType)
        => new(browserType);

    /// <summary>
    /// Check if test should run on browser.
    /// </summary>
    public static bool ShouldRunOn(this BrowserType browser, params BrowserType[] allowedBrowsers)
        => allowedBrowsers.Contains(browser);
}
