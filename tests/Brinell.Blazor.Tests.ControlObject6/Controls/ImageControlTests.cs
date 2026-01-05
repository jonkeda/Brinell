using Brinell.Blazor.ControlObject6.Context;
using Brinell.Blazor.ControlObject6.Controls;
using Brinell.Blazor.Tests.ControlObject6.Mocks;
using Brinell.Core.ControlObject6.Locators;
using Microsoft.Playwright;

namespace Brinell.Blazor.Tests.ControlObject6.Controls;

/// <summary>
/// Tests for ImageControl (BIM-001 to BIM-008).
/// </summary>
[Trait("Category", "Media")]
[Trait("Platform", "Blazor")]
public class ImageControlTests
{
    #region Constructor Tests (BIM-001 to BIM-002)

    [Fact]
    [Trait("Priority", "P0")]
    public void BIM001_Constructor_WithTestId_SetsLocator()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var context = new BlazorTestContext(mockPage.Object);

        // Act
        var image = new ImageControl(context, "profileImage", null);

        // Assert
        image.Locator.Should().NotBeNull();
        image.Locator.Value.Should().Be("profileImage");
        image.Locator.Strategy.Should().Be(LocatorStrategy.TestId);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void BIM002_Constructor_WithLocator_SetsLocator()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var context = new BlazorTestContext(mockPage.Object);
        var locator = By.Id("myImage");

        // Act
        var image = new ImageControl(context, locator, null);

        // Assert
        image.Locator.Should().Be(locator);
    }

    #endregion

    #region State Tests (BIM-003 to BIM-004)

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BIM003_GetSourceAsync_ReturnsSource()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.GetAttributeAsync("src", It.IsAny<LocatorGetAttributeOptions?>()))
            .ReturnsAsync("https://example.com/image.png");
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var image = new ImageControl(context, "profileImage", null);

        // Act
        var source = await image.GetSourceAsync();

        // Assert
        source.Should().Be("https://example.com/image.png");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BIM004_GetAltTextAsync_ReturnsAltText()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.GetAttributeAsync("alt", It.IsAny<LocatorGetAttributeOptions?>()))
            .ReturnsAsync("User profile picture");
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var image = new ImageControl(context, "profileImage", null);

        // Act
        var altText = await image.GetAltTextAsync();

        // Assert
        altText.Should().Be("User profile picture");
    }

    #endregion

    #region Common State Tests (BIM-005 to BIM-006)

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BIM005_IsExistsAsync_WhenExists_ReturnsTrue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 1);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var image = new ImageControl(context, "profileImage", null);

        // Act
        var exists = await image.IsExistsAsync();

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BIM006_IsVisibleAsync_WhenVisible_ReturnsTrue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(visible: true);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var image = new ImageControl(context, "profileImage", null);

        // Act
        var visible = await image.IsVisibleAsync();

        // Assert
        visible.Should().BeTrue();
    }

    #endregion

    #region Assertion Tests (BIM-007)

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BIM007_AssertSourceAsync_WhenMatches_Passes()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.GetAttributeAsync("src", It.IsAny<LocatorGetAttributeOptions?>()))
            .ReturnsAsync("https://example.com/image.png");
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var image = new ImageControl(context, "profileImage", null);

        // Act & Assert - should not throw
        await image.Invoking(i => i.AssertSourceAsync("https://example.com/image.png")).Should().NotThrowAsync();
    }

    #endregion

    #region Action Tests (BIM-008)

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BIM008_ClickAsync_WhenClickable_PerformsClick()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var image = new ImageControl(context, "profileImage", null);

        // Act
        await image.ClickAsync();

        // Assert
        mockLocator.Verify(l => l.ClickAsync(It.IsAny<LocatorClickOptions?>()), Times.Once);
    }

    #endregion
}
