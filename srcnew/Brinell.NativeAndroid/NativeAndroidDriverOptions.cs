namespace Brinell.NativeAndroid;

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

    public static NativeAndroidDriverOptions FromEnvironment()
    {
        var options = new NativeAndroidDriverOptions();

        var serverUri = Environment.GetEnvironmentVariable("APPIUM_SERVER_URI");
        if (Uri.TryCreate(serverUri, UriKind.Absolute, out var uri))
        {
            options.AppiumServerUri = uri;
        }

        options.AppPath = GetEnvOrNull("APPIUM_APP_PATH");
        options.AppPackage = GetEnvOrNull("APPIUM_APP_PACKAGE");
        options.AppActivity = GetEnvOrNull("APPIUM_APP_ACTIVITY");
        options.PlatformVersion = GetEnvOrNull("APPIUM_PLATFORM_VERSION");
        options.DeviceName = GetEnvOrNull("APPIUM_DEVICE_NAME") ?? options.DeviceName;
        options.AutomationName = GetEnvOrNull("APPIUM_AUTOMATION_NAME") ?? options.AutomationName;
        options.AutoGrantPermissions = GetEnvBool("APPIUM_AUTO_GRANT_PERMISSIONS", options.AutoGrantPermissions);
        options.NoReset = GetEnvBool("APPIUM_NO_RESET", options.NoReset);
        options.FullReset = GetEnvBool("APPIUM_FULL_RESET", options.FullReset);

        return options;
    }

    private static string? GetEnvOrNull(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static bool GetEnvBool(string name, bool fallback)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        return value.Equals("1", StringComparison.OrdinalIgnoreCase)
            || value.Equals("true", StringComparison.OrdinalIgnoreCase)
            || value.Equals("yes", StringComparison.OrdinalIgnoreCase);
    }
}
