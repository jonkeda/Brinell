using Brinell.Blazor.ControlObject6.Context;
using Brinell.Blazor.ControlObject6.Controls;
using Brinell.Blazor.Tests.ControlObject6.Mocks;
using Brinell.Core.ControlObject6.Locators;
using Microsoft.Playwright;

namespace Brinell.Blazor.Tests.ControlObject6.Controls;

/// <summary>
/// Tests for ButtonControl (BC-001 to BC-008).
/// </summary>
public class ButtonControlTests
{
    [Fact]
    public void Constructor_WithTestId_SetsLocator()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var context = new BlazorTestContext(mockPage.Object);

        // Act
        var button = new ButtonControl(context, "submitBtn", null);

        // Assert
        button.Locator.Should().NotBeNull();
        button.Locator.Value.Should().Be("submitBtn");
        button.Locator.Strategy.Should().Be(LocatorStrategy.TestId);
    }

    [Fact]
    public void Constructor_WithLocator_SetsLocator()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var context = new BlazorTestContext(mockPage.Object);
        var locator = By.Id("myButton");

        // Act
        var button = new ButtonControl(context, locator, null);

        // Assert
        button.Locator.Should().Be(locator);
    }

    [Fact]
    public async Task BC003_ClickAsync_CallsLocatorClick()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100; // Short timeout for tests
        var button = new ButtonControl(context, "submitBtn", null);

        // Act
        await button.ClickAsync();

        // Assert
        mockLocator.Verify(l => l.ClickAsync(It.IsAny<LocatorClickOptions?>()), Times.Once);
    }

    [Fact]
    public async Task IsExistsAsync_WhenCountGreaterThanZero_ReturnsTrue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 1);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var button = new ButtonControl(context, "submitBtn", null);

        // Act
        var exists = await button.IsExistsAsync();

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task IsExistsAsync_WhenCountIsZero_ReturnsFalse()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 0);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var button = new ButtonControl(context, "submitBtn", null);

        // Act
        var exists = await button.IsExistsAsync();

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task IsVisibleAsync_WhenVisible_ReturnsTrue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(visible: true);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var button = new ButtonControl(context, "submitBtn", null);

        // Act
        var visible = await button.IsVisibleAsync();

        // Assert
        visible.Should().BeTrue();
    }

    [Fact]
    public async Task IsVisibleAsync_WhenNotVisible_ReturnsFalse()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(visible: false);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var button = new ButtonControl(context, "submitBtn", null);

        // Act
        var visible = await button.IsVisibleAsync();

        // Assert
        visible.Should().BeFalse();
    }

    [Fact]
    public async Task IsEnabledAsync_WhenEnabled_ReturnsTrue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(enabled: true);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var button = new ButtonControl(context, "submitBtn", null);

        // Act
        var enabled = await button.IsEnabledAsync();

        // Assert
        enabled.Should().BeTrue();
    }

    [Fact]
    public async Task IsEnabledAsync_WhenDisabled_ReturnsFalse()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(enabled: false);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var button = new ButtonControl(context, "submitBtn", null);

        // Act
        var enabled = await button.IsEnabledAsync();

        // Assert
        enabled.Should().BeFalse();
    }

    [Fact]
    public async Task GetTextAsync_ReturnsInnerText()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(text: "Click Me");
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100; // Short timeout for tests
        var button = new ButtonControl(context, "submitBtn", null);

        // Act
        var text = await button.GetTextAsync();

        // Assert
        text.Should().Be("Click Me");
    }
}
