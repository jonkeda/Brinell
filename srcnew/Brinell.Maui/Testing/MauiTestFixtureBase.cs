using Brinell.Core.Services;
using Brinell.Core.Testing;
using Brinell.Core.Artifacts;
using Brinell.Maui.Context;
using Brinell.Maui.Configuration;

namespace Brinell.Maui.Testing;

/// <summary>
/// Base fixture for MAUI UI tests that manages the Appium driver and test context lifecycle.
/// Inherit from this class in your test project and implement <see cref="GetDefaultAppPath"/>.
/// </summary>
/// <remarks>
/// Configuration is loaded from brinell.maui.config.json. 
/// For per-test overrides, use SetupWith() before running your test.
/// Environment variables are still supported for backward compatibility but are deprecated.
/// </remarks>
public abstract class MauiTestFixtureBase : IDisposable
{
    private static int _instanceCount;
    private readonly int _instanceId;
    private readonly MauiTestContextOptions _options;
    private MauiTestContext _context;
    private IScreenshotService _screenshotService;
    private bool _disposed;
    
    /// <summary>
    /// Current MAUI configuration loaded from brinell.maui.config.json
    /// </summary>
    protected BrinellMauiConfiguration Configuration { get; private set; }

    /// <summary>
    /// Initializes the fixture by loading configuration and creating the test context and screenshot service.
    /// </summary>
    protected MauiTestFixtureBase()
    {
        _instanceId = Interlocked.Increment(ref _instanceCount);
        Console.WriteLine($"[FIXTURE] {GetType().Name} #{_instanceId} CREATING at {DateTime.Now:HH:mm:ss.fff}");
        
        Configuration = BrinellMauiConfiguration.Load();
        
        _options = CreateTestContextOptions();

        _context = new MauiTestContext(_options);
        
        Console.WriteLine($"[FIXTURE] {GetType().Name} #{_instanceId} CREATED - Driver session started");
        
        _screenshotService = CreateScreenshotService(_context);
    }

    /// <summary>
    /// Closes the app and launches it again, with the options it was first launched with.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For a test whose subject is a restart ("the data is still there"), and for measuring what a
    /// fresh app per test would cost. What the first launch arranged outside the app - files,
    /// servers, environment - is left as it is: <see cref="CreateTestContextOptions"/> is not
    /// called again, so a fixture that arranges state there keeps that state.
    /// </para>
    /// <para>
    /// <see cref="Context"/> and <see cref="ScreenshotService"/> are new objects afterwards. Page
    /// and control objects built on the old context are stale: build new ones.
    /// </para>
    /// </remarks>
    public void RestartApp()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        Console.WriteLine($"[FIXTURE] {GetType().Name} #{_instanceId} RESTARTING at {DateTime.Now:HH:mm:ss.fff}");

        _context.Dispose();
        _context = new MauiTestContext(_options);
        _screenshotService = CreateScreenshotService(_context);

        Console.WriteLine($"[FIXTURE] {GetType().Name} #{_instanceId} RESTARTED - Driver session started");
    }

    private IScreenshotService CreateScreenshotService(MauiTestContext context)
    {
        var screenshotSettings = new ScreenshotSettings
        {
            OutputDirectory = GetScreenshotDirectory(),
            CaptureOnFailure = true,
            IncludeTimestamp = true,
            Format = ScreenshotFormat.Png
        };
        var service = new ScreenshotService(context, context.Logger, screenshotSettings);
        ScreenshotTestAttribute.SetService(service);
        return service;
    }

    /// <summary>
    /// Gets the MAUI test context.
    /// </summary>
    public MauiTestContext Context => _context;
    
    /// <summary>
    /// Gets the screenshot service.
    /// </summary>
    public IScreenshotService ScreenshotService => _screenshotService;

    /// <summary>
    /// Gets the current platform from configuration.
    /// </summary>
    protected MauiPlatform Platform => Configuration?.Maui?.Platform ?? MauiPlatform.Windows;

    #region Abstract Methods

    /// <summary>
    /// Gets the default app path based on platform. Must be implemented by derived classes.
    /// </summary>
    /// <param name="platform">The platform: "windows", "android", or "ios".</param>
    /// <returns>The path to the app executable or package.</returns>
    protected abstract string GetDefaultAppPath(MauiPlatform platform);

    #endregion

    #region Configuration Setup

    /// <summary>
    /// Allows per-test configuration overrides.
    /// Call this before your test logic to customize the configuration for a specific test.
    /// </summary>
    /// <example>
    /// <code>
    /// var fixture = new MyTestFixture();
    /// fixture.SetupWith(config => {
    ///     config.Maui.Platform = "android";
    ///     config.Maui.DeviceName = "emulator-5556";
    /// });
    /// // Now run your test with the customized configuration
    /// </code>
    /// </example>
    protected void SetupWith(Action<BrinellMauiConfiguration> configureAction)
    {
        ArgumentNullException.ThrowIfNull(configureAction);
        configureAction(Configuration);
    }

    #endregion

    #region Virtual Configuration Methods

    /// <summary>
    /// Creates test context options with platform-specific capabilities.
    /// Uses configuration loaded from brinell.maui.config.json.
    /// Override to further customize driver configuration.
    /// </summary>
    protected virtual MauiTestContextOptions CreateTestContextOptions()
    {
        ArgumentNullException.ThrowIfNull(Configuration, nameof(Configuration));
        ArgumentNullException.ThrowIfNull(Configuration.Maui, nameof(Configuration.Maui));

        var driverOptions = MauiDriverOptions.FromConfiguration(Configuration.Maui);
        
        driverOptions.Timeouts = new TimeoutSettings
        {
            DefaultWait = 5000,
            PageLoad = 10000,
            ElementFind = 3000,
            ElementState = 3000,
            Animation = 300,
            PollingInterval = 100
        };
        driverOptions.AppPath = GetDefaultAppPath(Configuration.Maui.Platform);
        
        switch (Configuration.Maui.Platform)
        {
            case MauiPlatform.Android:
                ConfigureAndroidOptions(driverOptions);
                break;
            case MauiPlatform.iOS:
                ConfigureiOSOptions(driverOptions);
                break;
            // Windows uses FlaUI - no additional options needed
        }

        return new MauiTestContextOptions
        {
            
            DriverOptions = driverOptions,
            Timeouts = driverOptions.Timeouts
        };
    }
    
    /// <summary>
    /// Configures driver options for Android MAUI app testing.
    /// Override to customize Android capabilities.
    /// </summary>
    protected virtual void ConfigureAndroidOptions(MauiDriverOptions options)
    {
        var serverUri = Configuration?.Maui?.ServerUri ?? "http://127.0.0.1:4723";
        var deviceName = Configuration?.Maui?.DeviceName ?? "emulator-5554";
        
        options.AppiumServerUri = new Uri(serverUri);
        options.DeviceName = deviceName;
    }
    
    /// <summary>
    /// Configures driver options for iOS MAUI app testing.
    /// Override to customize iOS capabilities.
    /// </summary>
    protected virtual void ConfigureiOSOptions(MauiDriverOptions options)
    {
        var serverUri = Configuration?.Maui?.ServerUri ?? "http://127.0.0.1:4723";
        var deviceName = Configuration?.Maui?.DeviceName ?? "iPhone 15";
        var platformVersion = Configuration?.Maui?.PlatformVersion ?? "17.0";
        
        options.AppiumServerUri = new Uri(serverUri);
        options.DeviceName = deviceName;
        options.PlatformVersion = platformVersion;
    }

    #endregion

    #region Utility Methods

    protected virtual string GetScreenshotDirectory()
    {
        var path = GetArtifactPathProvider().ScreenshotsDirectory;
        Directory.CreateDirectory(path);
        return path;
    }

    protected virtual ITestArtifactPathProvider GetArtifactPathProvider()
    {
        return DefaultTestArtifactPathProvider.Create(Configuration.Artifacts, GetType().Assembly.GetName().Name);
    }
    
    /// <summary>
    /// Finds the solution root directory by searching for *.sln files.
    /// </summary>
    protected static string FindSolutionDirectory()
    {
        var dir = Directory.GetCurrentDirectory();
        while (dir != null)
        {
            if (Directory.GetFiles(dir, "*.sln").Length > 0)
                return dir;
            dir = Directory.GetParent(dir)?.FullName;
        }
        return Directory.GetCurrentDirectory();
    }

    #endregion

    #region IDisposable

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes managed and unmanaged resources.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        Console.WriteLine($"[FIXTURE] {GetType().Name} #{_instanceId} DISPOSING at {DateTime.Now:HH:mm:ss.fff}");

        if (disposing)
        {
            _context?.Dispose();
        }

        Console.WriteLine($"[FIXTURE] {GetType().Name} #{_instanceId} DISPOSED");
        _disposed = true;
    }

    #endregion
}
