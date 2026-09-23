namespace Brinell.NativeAndroid.Tests;

public sealed class NativeAndroidDriverOptionsTests
{
    [Fact]
    public void AdditionalCapabilities_Is_Case_Insensitive()
    {
        var options = new NativeAndroidDriverOptions();

        options.AdditionalCapabilities["appWaitActivity"] = ".MainActivity";
        options.AdditionalCapabilities["APPWAITACTIVITY"] = ".LoginActivity";

        Assert.Single(options.AdditionalCapabilities);
        Assert.Equal(".LoginActivity", options.AdditionalCapabilities["appWaitActivity"]);
    }
}
