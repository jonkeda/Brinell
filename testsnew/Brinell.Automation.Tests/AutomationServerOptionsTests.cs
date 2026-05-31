namespace Brinell.Automation.Tests;

public sealed class AutomationServerOptionsTests
{
    [Fact]
    public void FromCommandLine_UsesDefaultPipe_WhenNoPipeArgumentExists()
    {
        var options = AutomationServerOptions.FromCommandLine(["app.exe", "--automation"]);

        Assert.Equal("Brinell.Stride.Automation", options.PipeName);
    }

    [Fact]
    public void FromCommandLine_ReadsPipeArgument()
    {
        var options = AutomationServerOptions.FromCommandLine(["app.exe", "--automation", "--pipe", "Brinell.Stride.Test"]);

        Assert.Equal("Brinell.Stride.Test", options.PipeName);
    }

    [Fact]
    public void FromCommandLine_ReadsPipeEqualsArgument()
    {
        var options = AutomationServerOptions.FromCommandLine(["app.exe", "--pipe=Brinell.Stride.Equals"]);

        Assert.Equal("Brinell.Stride.Equals", options.PipeName);
    }
}
