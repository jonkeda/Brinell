using Brinell.Maui.Appium;

namespace Brinell.Maui.Tests;

/// <summary>How a server on the test machine is addressed from inside the Android emulator.</summary>
public class AndroidHostTests
{
    [Theory]
    [InlineData("http://localhost:51234", "http://10.0.2.2:51234")]
    [InlineData("http://127.0.0.1:5080", "http://10.0.2.2:5080")]
    [InlineData("http://[::1]:5080", "http://10.0.2.2:5080")]
    [InlineData("http://localhost:51234/", "http://10.0.2.2:51234/")]
    [InlineData("http://localhost:51234/api/todos?x=1", "http://10.0.2.2:51234/api/todos?x=1")]
    public void EmulatorUrl_MapsTheHostsLoopbackToTheEmulatorAlias(string hostUrl, string expected)
        => Assert.Equal(expected, AndroidHost.EmulatorUrl(hostUrl));

    [Fact]
    public void EmulatorUrl_LeavesARealHostAlone()
        => Assert.Equal("https://todo.example.test/api", AndroidHost.EmulatorUrl("https://todo.example.test/api"));
}
