using Brinell.Blazor.ControlObject6.Context;
using Brinell.Blazor.ControlObject6.Controls;
using Brinell.Blazor.Tests.ControlObject6.Mocks;
using Brinell.Core.ControlObject6.Locators;
using Microsoft.Playwright;

namespace Brinell.Blazor.Tests.ControlObject6.Controls;

/// <summary>
/// Tests for TextAreaControl (BTA-001 to BTA-020).
/// </summary>
[Trait("Category", "TextInput")]
[Trait("Platform", "Blazor")]
public class TextAreaControlTests
{
    #region Constructor Tests (BTA-001 to BTA-002)

    [Fact]
    [Trait("Priority", "P0")]
    public void BTA001_Constructor_WithTestId_SetsLocator()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var context = new BlazorTestContext(mockPage.Object);

        // Act
        var textArea = new TextAreaControl(context, "descriptionArea", null);

        // Assert
        textArea.Locator.Should().NotBeNull();
        textArea.Locator.Value.Should().Be("descriptionArea");
        textArea.Locator.Strategy.Should().Be(LocatorStrategy.TestId);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void BTA002_Constructor_WithLocator_SetsLocator()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var context = new BlazorTestContext(mockPage.Object);
        var locator = By.Id("myTextArea");

        // Act
        var textArea = new TextAreaControl(context, locator, null);

        // Assert
        textArea.Locator.Should().Be(locator);
    }

    #endregion

    #region Text Entry Tests (BTA-003 to BTA-008)

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BTA003_EnterAsync_WithNull_DoesNothing()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var textArea = new TextAreaControl(context, "textArea", null);

        // Act & Assert - should not throw
        await textArea.Invoking(t => t.EnterAsync(null)).Should().NotThrowAsync();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BTA004_EnterAsync_WithText_ClearsAndFills()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var textArea = new TextAreaControl(context, "textArea", null);

        // Act
        await textArea.EnterAsync("This is multiline\ntext content.");

        // Assert
        mockLocator.Verify(l => l.ClearAsync(It.IsAny<LocatorClearOptions?>()), Times.Once);
        mockLocator.Verify(l => l.FillAsync("This is multiline\ntext content.", It.IsAny<LocatorFillOptions?>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BTA005_ClearAsync_ClearsElement()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var textArea = new TextAreaControl(context, "textArea", null);

        // Act
        await textArea.ClearAsync();

        // Assert
        mockLocator.Verify(l => l.ClearAsync(It.IsAny<LocatorClearOptions?>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BTA006_ClearAndEnterAsync_WithNull_OnlyClears()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var textArea = new TextAreaControl(context, "textArea", null);

        // Act
        await textArea.ClearAndEnterAsync(null);

        // Assert
        mockLocator.Verify(l => l.ClearAsync(It.IsAny<LocatorClearOptions?>()), Times.Once);
        mockLocator.Verify(l => l.FillAsync(It.IsAny<string>(), It.IsAny<LocatorFillOptions?>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BTA007_ClearAndEnterAsync_WithText_ClearsAndFills()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var textArea = new TextAreaControl(context, "textArea", null);

        // Act
        await textArea.ClearAndEnterAsync("New content");

        // Assert
        mockLocator.Verify(l => l.ClearAsync(It.IsAny<LocatorClearOptions?>()), Times.Once);
        mockLocator.Verify(l => l.FillAsync("New content", It.IsAny<LocatorFillOptions?>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BTA008_AppendAsync_WithText_TypesSequentially()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);
        
        mockLocator.Setup(l => l.PressSequentiallyAsync(It.IsAny<string>(), It.IsAny<LocatorPressSequentiallyOptions?>()))
            .Returns(Task.CompletedTask);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var textArea = new TextAreaControl(context, "textArea", null);

        // Act
        await textArea.AppendAsync("appended text");

        // Assert
        mockLocator.Verify(l => l.PressSequentiallyAsync("appended text", It.IsAny<LocatorPressSequentiallyOptions?>()), Times.Once);
    }

    #endregion

    #region State Tests (BTA-009 to BTA-013)

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BTA009_IsExistsAsync_WhenExists_ReturnsTrue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 1);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var textArea = new TextAreaControl(context, "textArea", null);

        // Act
        var exists = await textArea.IsExistsAsync();

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BTA010_IsExistsAsync_WhenNotExists_ReturnsFalse()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 0);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var textArea = new TextAreaControl(context, "textArea", null);

        // Act
        var exists = await textArea.IsExistsAsync();

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BTA011_IsVisibleAsync_WhenVisible_ReturnsTrue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(visible: true);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var textArea = new TextAreaControl(context, "textArea", null);

        // Act
        var visible = await textArea.IsVisibleAsync();

        // Assert
        visible.Should().BeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BTA012_IsVisibleAsync_WhenNotVisible_ReturnsFalse()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(visible: false);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var textArea = new TextAreaControl(context, "textArea", null);

        // Act
        var visible = await textArea.IsVisibleAsync();

        // Assert
        visible.Should().BeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BTA013_GetTextAsync_ReturnsInputValue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(text: "Multiline\nContent");
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var textArea = new TextAreaControl(context, "textArea", null);

        // Act
        var text = await textArea.GetTextAsync();

        // Assert
        text.Should().Be("Multiline\nContent");
    }

    #endregion

    #region TextArea-Specific Tests (BTA-014 to BTA-018)

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BTA014_GetRowsAsync_ReturnsRowsAttribute()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.GetAttributeAsync("rows", It.IsAny<LocatorGetAttributeOptions?>()))
            .ReturnsAsync("5");
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var textArea = new TextAreaControl(context, "textArea", null);

        // Act
        var rows = await textArea.GetRowsAsync();

        // Assert
        rows.Should().Be(5);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BTA015_GetColsAsync_ReturnsColsAttribute()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.GetAttributeAsync("cols", It.IsAny<LocatorGetAttributeOptions?>()))
            .ReturnsAsync("40");
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var textArea = new TextAreaControl(context, "textArea", null);

        // Act
        var cols = await textArea.GetColsAsync();

        // Assert
        cols.Should().Be(40);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BTA016_GetMaxLengthAsync_ReturnsMaxLengthAttribute()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.GetAttributeAsync("maxlength", It.IsAny<LocatorGetAttributeOptions?>()))
            .ReturnsAsync("1000");
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var textArea = new TextAreaControl(context, "textArea", null);

        // Act
        var maxLength = await textArea.GetMaxLengthAsync();

        // Assert
        maxLength.Should().Be(1000);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BTA017_GetRowsAsync_WhenNoAttribute_ReturnsNull()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.GetAttributeAsync("rows", It.IsAny<LocatorGetAttributeOptions?>()))
            .ReturnsAsync((string?)null);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var textArea = new TextAreaControl(context, "textArea", null);

        // Act
        var rows = await textArea.GetRowsAsync();

        // Assert
        rows.Should().BeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BTA018_GetTextLengthAsync_ReturnsCorrectLength()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(text: "Hello World");
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var textArea = new TextAreaControl(context, "textArea", null);

        // Act
        var length = await textArea.GetTextLengthAsync();

        // Assert
        length.Should().Be(11);
    }

    #endregion

    #region Focus Tests (BTA-019 to BTA-020)

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BTA019_FocusAsync_CallsFocusOnLocator()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var textArea = new TextAreaControl(context, "textArea", null);

        // Act
        await textArea.FocusAsync();

        // Assert
        mockLocator.Verify(l => l.FocusAsync(It.IsAny<LocatorFocusOptions?>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BTA020_IsEnabledAsync_WhenEnabled_ReturnsTrue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(enabled: true);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var textArea = new TextAreaControl(context, "textArea", null);

        // Act
        var enabled = await textArea.IsEnabledAsync();

        // Assert
        enabled.Should().BeTrue();
    }

    #endregion
}
