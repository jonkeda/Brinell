using Brinell.Blazor.ControlObject6.Context;
using Brinell.Blazor.ControlObject6.Controls;
using Brinell.Blazor.Tests.ControlObject6.Mocks;
using Brinell.Core.ControlObject6.Locators;
using Microsoft.Playwright;

namespace Brinell.Blazor.Tests.ControlObject6.Controls;

/// <summary>
/// Tests for LinkControl (LK-001 to LK-010).
/// </summary>
[Trait("Category", "Navigation")]
[Trait("Platform", "Blazor")]
public class LinkControlTests
{
    #region Constructor Tests (LK-001 to LK-002)

    [Fact]
    [Trait("Priority", "P0")]
    public void LK001_Constructor_WithTestId_SetsLocator()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var context = new BlazorTestContext(mockPage.Object);

        // Act
        var link = new LinkControl(context, "homeLink", null);

        // Assert
        link.Locator.Should().NotBeNull();
        link.Locator.Value.Should().Be("homeLink");
        link.Locator.Strategy.Should().Be(LocatorStrategy.TestId);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void LK002_Constructor_WithLocator_SetsLocator()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var context = new BlazorTestContext(mockPage.Object);
        var locator = By.Id("myLink");

        // Act
        var link = new LinkControl(context, locator, null);

        // Assert
        link.Locator.Should().Be(locator);
    }

    #endregion

    #region Action Tests (LK-003)

    [Fact]
    [Trait("Priority", "P0")]
    public async Task LK003_ClickAsync_NavigatesToHref()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var link = new LinkControl(context, "homeLink", null);

        // Act
        await link.ClickAsync();

        // Assert
        mockLocator.Verify(l => l.ClickAsync(It.IsAny<LocatorClickOptions?>()), Times.Once);
    }

    #endregion

    #region State Tests (LK-004 to LK-008)

    [Fact]
    [Trait("Priority", "P0")]
    public async Task LK004_GetTextAsync_ReturnsLinkText()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(text: "Go Home");
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var link = new LinkControl(context, "homeLink", null);

        // Act
        var text = await link.GetTextAsync();

        // Assert
        text.Should().Be("Go Home");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task LK005_GetHrefAsync_ReturnsHref()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.GetAttributeAsync("href", It.IsAny<LocatorGetAttributeOptions?>()))
            .ReturnsAsync("https://example.com");
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var link = new LinkControl(context, "homeLink", null);

        // Act
        var href = await link.GetHrefAsync();

        // Assert
        href.Should().Be("https://example.com");
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task LK006_IsExistsAsync_WhenExists_ReturnsTrue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 1);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var link = new LinkControl(context, "homeLink", null);

        // Act
        var exists = await link.IsExistsAsync();

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task LK007_IsVisibleAsync_WhenVisible_ReturnsTrue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(visible: true);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var link = new LinkControl(context, "homeLink", null);

        // Act
        var visible = await link.IsVisibleAsync();

        // Assert
        visible.Should().BeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task LK008_GetTargetAsync_ReturnsTarget()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.GetAttributeAsync("target", It.IsAny<LocatorGetAttributeOptions?>()))
            .ReturnsAsync("_blank");
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var link = new LinkControl(context, "homeLink", null);

        // Act
        var target = await link.GetTargetAsync();

        // Assert
        target.Should().Be("_blank");
    }

    #endregion

    #region Assertion Tests (LK-009 to LK-010)

    [Fact]
    [Trait("Priority", "P1")]
    public async Task LK009_AssertHrefAsync_WhenMatches_Passes()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.GetAttributeAsync("href", It.IsAny<LocatorGetAttributeOptions?>()))
            .ReturnsAsync("https://example.com");
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var link = new LinkControl(context, "homeLink", null);

        // Act & Assert - should not throw
        await link.Invoking(l => l.AssertHrefAsync("https://example.com")).Should().NotThrowAsync();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task LK010_IsEnabledAsync_WhenEnabled_ReturnsTrue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(enabled: true);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var link = new LinkControl(context, "homeLink", null);

        // Act
        var enabled = await link.IsEnabledAsync();

        // Assert
        enabled.Should().BeTrue();
    }

    #endregion
}
