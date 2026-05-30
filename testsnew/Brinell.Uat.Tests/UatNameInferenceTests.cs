using Xunit;

namespace Brinell.Uat.Tests;

public sealed class UatNameInferenceTests
{
    [Theory]
    [InlineData("SettingsPage", "Settings")]
    [InlineData("DisplayNameEntry", "Display Name")]
    [InlineData("EmailNotificationsSwitch", "Email Notifications")]
    [InlineData("SaveButton", "Save")]
    [InlineData("ApiKeyField", "Api Key")]
    public void FromIdentifier_RemovesKnownSuffixesAndSplitsNames(string identifier, string expected)
    {
        var result = UatNameInference.FromIdentifier(identifier);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("SaveButton", true)]
    [InlineData("DisplayNameEntry", true)]
    [InlineData("SettingsPanel", false)]
    public void HasKnownSuffix_ReturnsWhetherIdentifierUsesSupportedControlSuffix(string identifier, bool expected)
    {
        var result = UatNameInference.HasKnownSuffix(identifier);

        Assert.Equal(expected, result);
    }
}
