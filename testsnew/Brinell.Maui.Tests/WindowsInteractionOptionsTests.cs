namespace Brinell.Maui.Tests;

public class WindowsInteractionOptionsTests
{
    [Fact]
    public void FromEnvironment_DefaultsToSemanticMode()
    {
        var options = WindowsInteractionOptions.FromEnvironment(_ => null);

        Assert.False(options.AllowForegroundActivation);
        Assert.False(options.AllowPointerInput);
        Assert.False(options.AllowGlobalKeyboardInput);
        Assert.False(options.AllowClipboardInput);
    }

    [Fact]
    public void FromEnvironment_InteractiveModeEnablesPhysicalInput()
    {
        var values = new Dictionary<string, string?>
        {
            [WindowsInteractionOptions.InteractionModeEnvironmentVariable] = "interactive"
        };

        var options = WindowsInteractionOptions.FromEnvironment(name => values.GetValueOrDefault(name));

        Assert.True(options.AllowForegroundActivation);
        Assert.True(options.AllowPointerInput);
        Assert.True(options.AllowGlobalKeyboardInput);
        Assert.True(options.AllowClipboardInput);
    }

    [Fact]
    public void FromEnvironment_GranularOverridesWinOverModeDefaults()
    {
        var values = new Dictionary<string, string?>
        {
            [WindowsInteractionOptions.InteractionModeEnvironmentVariable] = "interactive",
            [WindowsInteractionOptions.AllowPointerInputEnvironmentVariable] = "false",
            [WindowsInteractionOptions.AllowClipboardInputEnvironmentVariable] = "0"
        };

        var options = WindowsInteractionOptions.FromEnvironment(name => values.GetValueOrDefault(name));

        Assert.True(options.AllowForegroundActivation);
        Assert.False(options.AllowPointerInput);
        Assert.True(options.AllowGlobalKeyboardInput);
        Assert.False(options.AllowClipboardInput);
    }

    [Fact]
    public void FromEnvironment_InvalidModeThrowsActionableError()
    {
        var values = new Dictionary<string, string?>
        {
            [WindowsInteractionOptions.InteractionModeEnvironmentVariable] = "desktop"
        };

        var ex = Assert.Throws<InvalidOperationException>(
            () => WindowsInteractionOptions.FromEnvironment(name => values.GetValueOrDefault(name)));

        Assert.Contains(WindowsInteractionOptions.InteractionModeEnvironmentVariable, ex.Message);
    }

    [Fact]
    public void FromEnvironment_InvalidBooleanThrowsActionableError()
    {
        var values = new Dictionary<string, string?>
        {
            [WindowsInteractionOptions.AllowPointerInputEnvironmentVariable] = "maybe"
        };

        var ex = Assert.Throws<InvalidOperationException>(
            () => WindowsInteractionOptions.FromEnvironment(name => values.GetValueOrDefault(name)));

        Assert.Contains(WindowsInteractionOptions.AllowPointerInputEnvironmentVariable, ex.Message);
    }
}
