using Brinell.Maui.Configuration;
using Brinell.Maui.Enums;

namespace Brinell.Maui.Tests;

/// <summary>
/// How <see cref="MauiDriverOptions.LaunchSettings"/> reach an Android app: as launch-intent extras.
/// </summary>
public class LaunchSettingsTests
{
    [Fact]
    public void LaunchSettings_OnAndroid_BecomeStringExtras()
    {
        var options = new MauiDriverOptions { Platform = MauiPlatform.Android };
        options.LaunchSettings["TODO_API_BASEURL"] = "http://10.0.2.2:51234";
        options.LaunchSettings["TODO_NOW"] = "2026-03-10T09:00:00.0000000+00:00";

        var arguments = MauiDriverFactory.LaunchIntentArguments(options);

        Assert.Equal(
            "--es TODO_API_BASEURL http://10.0.2.2:51234 --es TODO_NOW 2026-03-10T09:00:00.0000000+00:00",
            arguments);
    }

    [Fact]
    public void LaunchSettings_WhenEmpty_AddNothing()
        => Assert.Null(MauiDriverFactory.LaunchIntentArguments(new MauiDriverOptions { Platform = MauiPlatform.Android }));

    [Theory]
    [InlineData(@"C:\Program Files\app.db")]
    [InlineData("two words")]
    [InlineData("it's")]
    [InlineData("")]
    public void LaunchSettings_WithAValueTheCommandLineWouldBreak_ThrowNamingIt(string value)
    {
        var options = new MauiDriverOptions { Platform = MauiPlatform.Android };
        options.LaunchSettings["TODO_DB_PATH"] = value;

        var failure = Assert.Throws<ArgumentException>(() => MauiDriverFactory.LaunchIntentArguments(options));

        Assert.Contains("TODO_DB_PATH", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LaunchSettings_OnIos_ThrowRatherThanBeingDropped()
    {
        var options = new MauiDriverOptions { Platform = MauiPlatform.iOS };
        options.LaunchSettings["TODO_NOW"] = "2026-03-10";

        Assert.Throws<NotSupportedException>(() => MauiDriverFactory.LaunchIntentArguments(options));
    }
}
