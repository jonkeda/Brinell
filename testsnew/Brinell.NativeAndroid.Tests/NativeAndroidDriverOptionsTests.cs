namespace Brinell.NativeAndroid.Tests;

public sealed class NativeAndroidDriverOptionsTests
{
    [Fact]
    public void FromEnvironment_Reads_Appium_Settings()
    {
        using var env = new EnvironmentVariables(
            ("APPIUM_SERVER_URI", "http://localhost:4725"),
            ("APPIUM_APP_PATH", @"C:\apps\bouw7.apk"),
            ("APPIUM_APP_PACKAGE", "nl.bouw7"),
            ("APPIUM_APP_ACTIVITY", ".activity.LoginScreenActivity"),
            ("APPIUM_DEVICE_NAME", "Pixel_8"),
            ("APPIUM_PLATFORM_VERSION", "15"),
            ("APPIUM_AUTOMATION_NAME", "UiAutomator2"),
            ("APPIUM_AUTO_GRANT_PERMISSIONS", "false"),
            ("APPIUM_NO_RESET", "true"),
            ("APPIUM_FULL_RESET", "yes"));

        var options = NativeAndroidDriverOptions.FromEnvironment();

        Assert.Equal(new Uri("http://localhost:4725"), options.AppiumServerUri);
        Assert.Equal(@"C:\apps\bouw7.apk", options.AppPath);
        Assert.Equal("nl.bouw7", options.AppPackage);
        Assert.Equal(".activity.LoginScreenActivity", options.AppActivity);
        Assert.Equal("Pixel_8", options.DeviceName);
        Assert.Equal("15", options.PlatformVersion);
        Assert.Equal("UiAutomator2", options.AutomationName);
        Assert.False(options.AutoGrantPermissions);
        Assert.True(options.NoReset);
        Assert.True(options.FullReset);
    }

    [Fact]
    public void AdditionalCapabilities_Is_Case_Insensitive()
    {
        var options = new NativeAndroidDriverOptions();

        options.AdditionalCapabilities["appWaitActivity"] = ".MainActivity";
        options.AdditionalCapabilities["APPWAITACTIVITY"] = ".LoginActivity";

        Assert.Single(options.AdditionalCapabilities);
        Assert.Equal(".LoginActivity", options.AdditionalCapabilities["appWaitActivity"]);
    }

    private sealed class EnvironmentVariables : IDisposable
    {
        private readonly Dictionary<string, string?> previousValues = new(StringComparer.Ordinal);

        public EnvironmentVariables(params (string Name, string? Value)[] values)
        {
            foreach (var (name, value) in values)
            {
                previousValues[name] = Environment.GetEnvironmentVariable(name);
                Environment.SetEnvironmentVariable(name, value);
            }
        }

        public void Dispose()
        {
            foreach (var (name, value) in previousValues)
            {
                Environment.SetEnvironmentVariable(name, value);
            }
        }
    }
}
