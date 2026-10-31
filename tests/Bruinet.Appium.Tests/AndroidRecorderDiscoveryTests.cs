using Bruinet.Recording;

namespace Bruinet.Appium.Tests;

/// <summary>The Android recorder is offered from its own package, by the name the registry knows.</summary>
public class AndroidRecorderDiscoveryTests
{
    [Fact]
    public void Discover_OffersTheAndroidRecorder()
        => Assert.Contains(ScreenRecorders.Discover().Recorders, recorder => recorder is AndroidRecorder);
}
