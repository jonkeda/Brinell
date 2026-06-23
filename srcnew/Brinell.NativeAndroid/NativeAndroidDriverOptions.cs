namespace Brinell.NativeAndroid;

using Brinell.Core.Configuration;

/// <summary>
/// Runtime settings used to create a native Android Appium session.
/// </summary>
public sealed class NativeAndroidDriverOptions
{
    public Uri AppiumServerUri { get; set; } = new("http://127.0.0.1:4723");

    public string DeviceName { get; set; } = "emulator-5554";

    public string AutomationName { get; set; } = "UiAutomator2";

    public string? PlatformVersion { get; set; }

    public string? AppPath { get; set; }

    public string? AppPackage { get; set; }

    public string? AppActivity { get; set; }

    public bool AutoGrantPermissions { get; set; } = true;

    public bool NoReset { get; set; }

    public bool FullReset { get; set; }

    public TimeoutSettings? Timeouts { get; set; }

    public ITestLogger? Logger { get; set; }

    public Dictionary<string, object> AdditionalCapabilities { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Creates Native Android driver options from configuration object.
    /// This is the recommended way to load settings (replaces FromEnvironment).
    /// </summary>
    public static NativeAndroidDriverOptions FromConfiguration(MauiOptions config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var options = new NativeAndroidDriverOptions();

        if (config.ServerUri != null && Uri.TryCreate(config.ServerUri, UriKind.Absolute, out var uri))
        {
            options.AppiumServerUri = uri;
        }

        options.AppPath = config.AppPath;
        options.DeviceName = config.DeviceName ?? "emulator-5554";
        options.PlatformVersion = config.PlatformVersion;
        options.AutoGrantPermissions = true;

        return options;
    }
}
