using Brinell.Core.Configuration;

namespace Brinell.Wpf.Tests;

/// <summary>
/// Unit tests for WPF context options and configuration.
/// </summary>
public class WpfTestContextOptionsTests
{
    [Fact]
    public void Options_CanBeCreatedWithExecutablePath()
    {
        var options = new WpfTestContextOptions
        {
            ExecutablePath = @"C:\test\app.exe"
        };

        Assert.Equal(@"C:\test\app.exe", options.ExecutablePath);
        Assert.Null(options.ProcessId);
        Assert.Null(options.WindowHandle);
    }

    [Fact]
    public void Options_CanBeCreatedWithProcessId()
    {
        var options = new WpfTestContextOptions
        {
            ProcessId = 12345
        };

        Assert.Equal(12345, options.ProcessId);
        Assert.Null(options.ExecutablePath);
    }

    [Fact]
    public void Options_CanBeCreatedWithWindowHandle()
    {
        var handle = new IntPtr(0x12345);
        var options = new WpfTestContextOptions
        {
            WindowHandle = handle
        };

        Assert.Equal(handle, options.WindowHandle!.Value);
    }

    [Fact]
    public void Options_DefaultTimeouts_AreNull()
    {
        var options = new WpfTestContextOptions();
        Assert.Null(options.Timeouts);
    }

    [Fact]
    public void Options_CanSetCustomTimeouts()
    {
        var timeouts = new TimeoutSettings
        {
            DefaultWait = 10000,
            PageLoad = 20000,
            PollingInterval = 200
        };

        var options = new WpfTestContextOptions
        {
            Timeouts = timeouts
        };

        Assert.Equal(10000, options.Timeouts?.DefaultWait);
        Assert.Equal(20000, options.Timeouts?.PageLoad);
        Assert.Equal(200, options.Timeouts?.PollingInterval);
    }
}

/// <summary>
/// Unit tests verifying WPF type structure compiles correctly.
/// </summary>
public class WpfTypeStructureTests
{
    [Fact]
    public void IWpfTestContext_InheritsTestContext()
    {
        Assert.True(typeof(ITestContext<IWpfElement>).IsAssignableFrom(typeof(IWpfTestContext)));
    }

    [Fact]
    public void IWpfTestContext_InheritsElementScope()
    {
        Assert.True(typeof(IWpfElementScope).IsAssignableFrom(typeof(IWpfTestContext)));
    }

    [Fact]
    public void IWpfDriver_InheritsDriver()
    {
        Assert.True(typeof(IDriver<IWpfElement>).IsAssignableFrom(typeof(IWpfDriver)));
    }

    [Fact]
    public void Locator_ByAutomationId_CreatesCorrectLocator()
    {
        var locator = Locator.ByAutomationId("MyControl");
        Assert.Equal(LocatorStrategy.AutomationId, locator.Strategy);
        Assert.Equal("MyControl", locator.Value);
    }

    [Fact]
    public void Locator_ByName_CreatesCorrectLocator()
    {
        var locator = Locator.ByName("MyControl");
        Assert.Equal(LocatorStrategy.Name, locator.Strategy);
        Assert.Equal("MyControl", locator.Value);
    }
}
