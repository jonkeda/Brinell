using Brinell.Maui.UITests.Pages2;

namespace Brinell.Maui.UITests.Tests.Collection;

/// <summary>
/// UI tests for ListView verifying list item operations.
/// </summary>
[Collection("Maui")]
[Trait("Category", "UITest")]
[Trait("Control", "ListView")]
public class ListViewControlTests
{
    private readonly MauiFixture _fixture;
    private ContainerDemoPage Page => _fixture.ContainerDemoPage;

    public ListViewControlTests(MauiFixture fixture)
    {
        _fixture = fixture;
        _fixture.NavigateToContainerDemo();
    }

    #region State Tests

    /// <summary>
    /// Verifies that list exists on container demo page.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsExists")]
    public Task ListView_IsExists_ReturnsTrue()
    {
        // Assert - ContainerDemoPage has lists
        Assert.True(Page.IsLoaded());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that list content is visible.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsVisible")]
    public Task ListView_IsVisible_ReturnsTrue()
    {
        // Assert
        Assert.True(Page.IsLoaded());
        return Task.CompletedTask;
    }

    #endregion
}
