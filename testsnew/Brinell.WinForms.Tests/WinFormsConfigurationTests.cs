using Brinell.Core.Configuration;

namespace Brinell.WinForms.Tests;

/// <summary>
/// Unit tests for WinForms context options and configuration.
/// </summary>
public class WinFormsTestContextOptionsTests
{
    [Fact]
    public void Options_CanBeCreatedWithExecutablePath()
    {
        var options = new WinFormsTestContextOptions
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
        var options = new WinFormsTestContextOptions
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
        var options = new WinFormsTestContextOptions
        {
            WindowHandle = handle
        };

        Assert.Equal(handle, options.WindowHandle!.Value);
    }

    [Fact]
    public void Options_DefaultTimeouts_AreNull()
    {
        var options = new WinFormsTestContextOptions();
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

        var options = new WinFormsTestContextOptions
        {
            Timeouts = timeouts
        };

        Assert.Equal(10000, options.Timeouts?.DefaultWait);
        Assert.Equal(20000, options.Timeouts?.PageLoad);
        Assert.Equal(200, options.Timeouts?.PollingInterval);
    }
}

/// <summary>
/// Unit tests verifying WinForms type structure compiles correctly.
/// </summary>
public class WinFormsTypeStructureTests
{
    [Fact]
    public void IWinFormsTestContext_InheritsTestContext()
    {
        Assert.True(typeof(ITestContext<IWinFormsElement>).IsAssignableFrom(typeof(IWinFormsTestContext)));
    }

    [Fact]
    public void IWinFormsTestContext_InheritsElementScope()
    {
        Assert.True(typeof(IWinFormsElementScope).IsAssignableFrom(typeof(IWinFormsTestContext)));
    }

    [Fact]
    public void IWinFormsDriver_InheritsDriver()
    {
        Assert.True(typeof(IDriver<IWinFormsElement>).IsAssignableFrom(typeof(IWinFormsDriver)));
    }

    [Fact]
    public void Locator_ByAutomationId_CreatesCorrectLocator()
    {
        var locator = Locator.ByAutomationId("MyControl");
        Assert.Equal(LocatorStrategy.AutomationId, locator.Strategy);
        Assert.Equal("MyControl", locator.Value);
    }
}
