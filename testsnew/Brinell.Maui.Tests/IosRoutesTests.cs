using Brinell.Maui.Appium;

namespace Brinell.Maui.Tests;

/// <summary>
/// The decisions inside the iOS routes, apart from the tree they read. Whether the locators find
/// that tree is for a simulator (<c>.my/ios/plan.md</c>).
/// </summary>
public class IosRoutesTests
{
    [Theory]
    [InlineData("Back")]
    [InlineData("Controls")]
    [InlineData("BackButton")]
    [InlineData(null)]
    public void ALeadingButton_IsTheBackButton_WhateverItsName(string? name)
        => Assert.True(AppiumMauiDriver.IsIosBackButton(name, offsetInBar: 8, barWidth: 430));

    [Theory]
    [InlineData("OK")]
    [InlineData("Menu")]
    [InlineData("FlyoutIcon")]
    public void TheFlyoutOpener_IsNotTheBackButton(string name)
        => Assert.False(AppiumMauiDriver.IsIosBackButton(name, offsetInBar: 8, barWidth: 430));

    [Fact]
    public void ATrailingToolbarItem_IsNotTheBackButton()
        => Assert.False(AppiumMauiDriver.IsIosBackButton("Save", offsetInBar: 370, barWidth: 430));
}
