using Brinell.Blazor.ControlObject6.Context;
using Brinell.Blazor.ControlObject6.Controls;
using Brinell.Blazor.Tests.ControlObject6.Mocks;
using Brinell.Core.ControlObject6.Locators;
using Microsoft.Playwright;

namespace Brinell.Blazor.Tests.ControlObject6.Controls;

/// <summary>
/// Tests for InputControl (IC-001 to IC-032).
/// </summary>
public class InputControlTests
{
    [Fact]
    public void Constructor_WithTestId_SetsLocator()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var context = new BlazorTestContext(mockPage.Object);

        // Act
        var input = new InputControl(context, "usernameInput", null);

        // Assert
        input.Locator.Should().NotBeNull();
        input.Locator.Value.Should().Be("usernameInput");
        input.Locator.Strategy.Should().Be(LocatorStrategy.TestId);
    }

    [Fact]
    public void Constructor_WithLocator_SetsLocator()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var context = new BlazorTestContext(mockPage.Object);
        var locator = By.Id("myInput");

        // Act
        var input = new InputControl(context, locator, null);

        // Assert
        input.Locator.Should().Be(locator);
    }

    #region Text Input Operations (IC-010 to IC-016)

    [Fact]
    public async Task IC010_EnterAsync_WithNull_DoesNothing()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var input = new InputControl(context, "input", null);

        // Act & Assert - should not throw
        await input.Invoking(i => i.EnterAsync(null)).Should().NotThrowAsync();
    }

    [Fact]
    public async Task IC011_EnterAsync_WithText_ClearsAndFills()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100; // Short timeout for tests
        var input = new InputControl(context, "input", null);

        // Act
        await input.EnterAsync("hello");

        // Assert
        mockLocator.Verify(l => l.ClearAsync(It.IsAny<LocatorClearOptions?>()), Times.Once);
        mockLocator.Verify(l => l.FillAsync("hello", It.IsAny<LocatorFillOptions?>()), Times.Once);
    }

    [Fact]
    public async Task IC012_ClearAsync_ClearsElement()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100; // Short timeout for tests
        var input = new InputControl(context, "input", null);

        // Act
        await input.ClearAsync();

        // Assert
        mockLocator.Verify(l => l.ClearAsync(It.IsAny<LocatorClearOptions?>()), Times.Once);
    }

    [Fact]
    public async Task IC013_ClearAndEnterAsync_WithNull_OnlyClears()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100; // Short timeout for tests
        var input = new InputControl(context, "input", null);

        // Act
        await input.ClearAndEnterAsync(null);

        // Assert
        mockLocator.Verify(l => l.ClearAsync(It.IsAny<LocatorClearOptions?>()), Times.Once);
        mockLocator.Verify(l => l.FillAsync(It.IsAny<string>(), It.IsAny<LocatorFillOptions?>()), Times.Never);
    }

    [Fact]
    public async Task IC014_ClearAndEnterAsync_WithText_ClearsAndFills()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100; // Short timeout for tests
        var input = new InputControl(context, "input", null);

        // Act
        await input.ClearAndEnterAsync("world");

        // Assert
        mockLocator.Verify(l => l.ClearAsync(It.IsAny<LocatorClearOptions?>()), Times.Once);
        mockLocator.Verify(l => l.FillAsync("world", It.IsAny<LocatorFillOptions?>()), Times.Once);
    }

    [Fact]
    public async Task IC015_AppendAsync_WithNull_DoesNothing()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var input = new InputControl(context, "input", null);

        // Act & Assert - should not throw
        await input.Invoking(i => i.AppendAsync(null)).Should().NotThrowAsync();
    }

    [Fact]
    public async Task IC016_AppendAsync_WithText_TypesSequentially()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);
        
        // Setup for PressSequentiallyAsync
        mockLocator.Setup(l => l.PressSequentiallyAsync(It.IsAny<string>(), It.IsAny<LocatorPressSequentiallyOptions?>()))
            .Returns(Task.CompletedTask);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100; // Short timeout for tests
        var input = new InputControl(context, "input", null);

        // Act
        await input.AppendAsync("appended");

        // Assert
        mockLocator.Verify(l => l.PressSequentiallyAsync("appended", It.IsAny<LocatorPressSequentiallyOptions?>()), Times.Once);
    }

    #endregion

    #region State Methods

    [Fact]
    public async Task IsExistsAsync_WhenCountGreaterThanZero_ReturnsTrue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 1);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var input = new InputControl(context, "input", null);

        // Act
        var exists = await input.IsExistsAsync();

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task IsVisibleAsync_WhenVisible_ReturnsTrue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(visible: true);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var input = new InputControl(context, "input", null);

        // Act
        var visible = await input.IsVisibleAsync();

        // Assert
        visible.Should().BeTrue();
    }

    [Fact]
    public async Task GetTextAsync_ReturnsInputValue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(text: "Input Value");
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100; // Short timeout for tests
        var input = new InputControl(context, "input", null);

        // Act
        var text = await input.GetTextAsync();

        // Assert - InputControl uses InputValueAsync for text
        text.Should().Be("Input Value");
    }

    [Fact]
    public async Task GetTextLengthAsync_ReturnsCorrectLength()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(text: "Hello");
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100; // Short timeout for tests
        var input = new InputControl(context, "input", null);

        // Act
        var length = await input.GetTextLengthAsync();

        // Assert
        length.Should().Be(5);
    }

    #endregion

    #region Focus Operations

    [Fact]
    public async Task FocusAsync_CallsFocusOnLocator()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100; // Short timeout for tests
        var input = new InputControl(context, "input", null);

        // Act
        await input.FocusAsync();

        // Assert
        mockLocator.Verify(l => l.FocusAsync(It.IsAny<LocatorFocusOptions?>()), Times.Once);
    }

    #endregion
}
