namespace Bruinell.Stride.Automation.Tests;

public sealed class AutomationServerOptionsTests
{
    [Fact]
    public void FromCommandLine_UsesDefaultPipe_WhenNoPipeArgumentExists()
    {
        var options = AutomationServerOptions.FromCommandLine(["app.exe", "--automation"]);

        Assert.Equal("Bruinell.Stride.Automation", options.PipeName);
    }

    [Fact]
    public void FromCommandLine_ReadsPipeArgument()
    {
        var options = AutomationServerOptions.FromCommandLine(["app.exe", "--automation", "--pipe", "Bruinell.Stride.Test"]);

        Assert.Equal("Bruinell.Stride.Test", options.PipeName);
    }

    [Fact]
    public void FromCommandLine_ReadsPipeEqualsArgument()
    {
        var options = AutomationServerOptions.FromCommandLine(["app.exe", "--pipe=Bruinell.Stride.Equals"]);

        Assert.Equal("Bruinell.Stride.Equals", options.PipeName);
    }
}
