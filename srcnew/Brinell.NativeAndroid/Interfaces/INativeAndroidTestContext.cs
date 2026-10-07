using Brinell.Core.Calls;
using Brinell.Core.Interfaces;

namespace Brinell.NativeAndroid.Interfaces;

// R9: implements Core's non-generic ITestContext and NativeAndroid's own scope, not Core's
// generic ITestContext<T>. See AD-010.

/// <summary>
/// One app under test on one device: the driver, the timeouts and log every call uses, and the
/// screen as the outermost scope.
/// </summary>
public interface INativeAndroidTestContext : ITestContext, INativeAndroidElementScope
{
    /// <summary>The Appium session.</summary>
    INativeAndroidDriver Driver { get; }

    /// <summary>A name for the context in logs and messages, when a test has several apps.</summary>
    string? Name { get; }

    /// <summary>When a call that passed is logged as a near-miss.</summary>
    NearMissSettings NearMiss { get; }
}
