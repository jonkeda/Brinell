namespace Bruinet.Stride.Automation.Tests;

public sealed class AutomationServerOptionsTests
{
    [Fact]
    public void FromCommandLine_UsesDefaultPipe_WhenNoPipeArgumentExists()
    {
        var options = AutomationServerOptions.FromCommandLine(["app.exe", "--automation"]);

        Assert.Equal("Bruinet.Stride.Automation", options.PipeName);
    }

    [Fact]
    public void FromCommandLine_ReadsPipeArgument()
    {
        var options = AutomationServerOptions.FromCommandLine(["app.exe", "--automation", "--pipe", "Bruinet.Stride.Test"]);

        Assert.Equal("Bruinet.Stride.Test", options.PipeName);
    }

    [Fact]
    public void FromCommandLine_ReadsPipeEqualsArgument()
    {
        var options = AutomationServerOptions.FromCommandLine(["app.exe", "--pipe=Bruinet.Stride.Equals"]);

        Assert.Equal("Bruinet.Stride.Equals", options.PipeName);
    }
}
