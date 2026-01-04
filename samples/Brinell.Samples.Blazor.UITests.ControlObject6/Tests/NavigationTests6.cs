using Brinell.Samples.Blazor.UITests.ControlObject6.PageObjects;
using Brinell.Samples.Blazor.UITests.ControlObject6.TestBase;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Brinell.Samples.Blazor.UITests.ControlObject6.Tests;

/// <summary>
/// Navigation tests using ControlObject6 async API.
/// </summary>
public class NavigationTests6 : BlazorTestBase6
{
    public NavigationTests6(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    [Trait("Category", "Navigation")]
    [Trait("Priority", "P0")]
    public async Task Navigation_ToCounterPage_LoadsSuccessfully()
    {
        // Arrange & Act
        await NavigateToAsync("counter");
        var counterPage = new CounterPage6(Context);

        // Assert
        var isLoaded = await counterPage.IsLoadedAsync();
        isLoaded.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Navigation")]
    [Trait("Priority", "P0")]
    public async Task Navigation_ToLoginPage_LoadsSuccessfully()
    {
        // Arrange & Act
        await NavigateToAsync("login");
        var loginPage = new LoginPage6(Context);

        // Assert
        await loginPage.WaitLoadedAsync(true);
    }

    [Fact]
    [Trait("Category", "Navigation")]
    [Trait("Priority", "P0")]
    public async Task Navigation_ToHomePage_LoadsSuccessfully()
    {
        // Arrange & Act
        await NavigateToAsync("/");
        var homePage = new HomePage6(Context);

        // Assert
        await homePage.WaitLoadedAsync(true);
    }

    [Fact]
    [Trait("Category", "Navigation")]
    [Trait("Priority", "P1")]
    public async Task Navigation_ViaContext_NavigatesToPage()
    {
        // Arrange & Act
        await Context.NavigateToAsync($"{BaseUrl}/counter");
        var counterPage = new CounterPage6(Context);

        // Assert
        await counterPage.WaitLoadedAsync(true);
    }

    [Fact]
    [Trait("Category", "Navigation")]
    [Trait("Priority", "P1")]
    public async Task PageObject_AssertLoaded_PassesWhenLoaded()
    {
        // Arrange
        await NavigateToAsync("counter");
        var counterPage = new CounterPage6(Context);

        // Act & Assert - should not throw
        await counterPage.AssertLoadedAsync(true);
    }

    [Fact]
    [Trait("Category", "Navigation")]
    [Trait("Priority", "P1")]
    public async Task PageObject_WaitLoaded_ReturnsTrue()
    {
        // Arrange
        await NavigateToAsync("login");
        var loginPage = new LoginPage6(Context);

        // Act
        var result = await loginPage.WaitLoadedAsync(true, 5000);

        // Assert
        result.Should().BeTrue();
    }
}
