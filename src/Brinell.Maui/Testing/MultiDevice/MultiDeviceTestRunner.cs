using System.Collections.Concurrent;
using Brinell.Maui.Infrastructure;

namespace Brinell.Maui.Testing.MultiDevice;

/// <summary>
/// Runner for executing tests across multiple devices.
/// </summary>
public class MultiDeviceTestRunner
{
    private readonly List<DeviceConfiguration> _devices;
    private readonly int _maxParallelism;

    public MultiDeviceTestRunner(IEnumerable<DeviceConfiguration> devices, int maxParallelism = 4)
    {
        _devices = devices?.ToList() ?? throw new ArgumentNullException(nameof(devices));
        _maxParallelism = maxParallelism;
    }

    /// <summary>
    /// Run a test action on all configured devices.
    /// </summary>
    /// <param name="testName">Name of the test.</param>
    /// <param name="testAction">Test action to execute on each device context.</param>
    /// <param name="parallel">Whether to run in parallel.</param>
    public async Task<MultiDeviceTestResults> RunAsync(
        string testName,
        Func<AppiumTestContext, Task> testAction,
        bool parallel = true)
    {
        var results = new MultiDeviceTestResults
        {
            TestName = testName,
            StartedAt = DateTime.UtcNow
        };

        var deviceResults = new ConcurrentBag<DeviceTestResult>();

        if (parallel)
        {
            var parallelDevices = _devices.Where(d => d.RunInParallel).ToList();
            var sequentialDevices = _devices.Where(d => !d.RunInParallel).ToList();

            // Run parallel devices
            await Parallel.ForEachAsync(
                parallelDevices,
                new ParallelOptions { MaxDegreeOfParallelism = _maxParallelism },
                async (device, ct) =>
                {
                    var result = await RunOnDeviceAsync(device, testAction);
                    deviceResults.Add(result);
                });

            // Run sequential devices
            foreach (var device in sequentialDevices)
            {
                var result = await RunOnDeviceAsync(device, testAction);
                deviceResults.Add(result);
            }
        }
        else
        {
            foreach (var device in _devices)
            {
                var result = await RunOnDeviceAsync(device, testAction);
                deviceResults.Add(result);
            }
        }

        results.DeviceResults = deviceResults.OrderBy(r => r.Device.Id).ToList();
        results.CompletedAt = DateTime.UtcNow;

        return results;
    }

    /// <summary>
    /// Run a synchronous test action on all configured devices.
    /// </summary>
    public async Task<MultiDeviceTestResults> RunAsync(
        string testName,
        Action<AppiumTestContext> testAction,
        bool parallel = true)
    {
        return await RunAsync(testName, ctx =>
        {
            testAction(ctx);
            return Task.CompletedTask;
        }, parallel);
    }

    private async Task<DeviceTestResult> RunOnDeviceAsync(
        DeviceConfiguration device,
        Func<AppiumTestContext, Task> testAction)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            using var context = CreateContext(device);
            await testAction(context);

            return DeviceTestResult.Success(device, DateTime.UtcNow - startTime);
        }
        catch (Exception ex)
        {
            return DeviceTestResult.Failure(device, ex, DateTime.UtcNow - startTime);
        }
    }

    private static AppiumTestContext CreateContext(DeviceConfiguration device)
    {
        var options = new AppiumTestOptions
        {
            ServerUrl = device.AppiumServerUrl,
            PlatformName = device.Platform,
            DeviceName = device.DeviceName,
            PlatformVersion = device.PlatformVersion,
            AppPath = device.AppPath,
            AppId = device.AppId
        };

        return AppiumTestContext.Create(options);
    }

    /// <summary>
    /// Filter devices by tag.
    /// </summary>
    public MultiDeviceTestRunner WithTag(string tag)
    {
        var filtered = _devices.Where(d => d.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase));
        return new MultiDeviceTestRunner(filtered, _maxParallelism);
    }

    /// <summary>
    /// Filter devices by platform.
    /// </summary>
    public MultiDeviceTestRunner ForPlatform(string platform)
    {
        var filtered = _devices.Where(d => d.Platform.Equals(platform, StringComparison.OrdinalIgnoreCase));
        return new MultiDeviceTestRunner(filtered, _maxParallelism);
    }

    /// <summary>
    /// Filter to mobile devices only.
    /// </summary>
    public MultiDeviceTestRunner MobileOnly()
    {
        var filtered = _devices.Where(d =>
            d.Platform.Equals("Android", StringComparison.OrdinalIgnoreCase) ||
            d.Platform.Equals("iOS", StringComparison.OrdinalIgnoreCase));
        return new MultiDeviceTestRunner(filtered, _maxParallelism);
    }

    /// <summary>
    /// Filter to desktop platforms only.
    /// </summary>
    public MultiDeviceTestRunner DesktopOnly()
    {
        var filtered = _devices.Where(d =>
            d.Platform.Equals("Windows", StringComparison.OrdinalIgnoreCase) ||
            d.Platform.Equals("Mac", StringComparison.OrdinalIgnoreCase));
        return new MultiDeviceTestRunner(filtered, _maxParallelism);
    }

    /// <summary>
    /// Create a runner from a configuration file.
    /// </summary>
    public static MultiDeviceTestRunner FromConfigFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("Device configuration file not found.", path);

        var json = File.ReadAllText(path);
        var devices = System.Text.Json.JsonSerializer.Deserialize<List<DeviceConfiguration>>(json);

        return new MultiDeviceTestRunner(devices ?? new List<DeviceConfiguration>());
    }

    /// <summary>
    /// Create a default runner with common device configurations.
    /// </summary>
    public static MultiDeviceTestRunner CreateDefault()
    {
        return new MultiDeviceTestRunner(new[]
        {
            DeviceConfiguration.Android("Pixel 7", "Pixel_7_API_34"),
            DeviceConfiguration.IOS("iPhone 15", "iPhone 15"),
            DeviceConfiguration.Windows()
        });
    }
}
