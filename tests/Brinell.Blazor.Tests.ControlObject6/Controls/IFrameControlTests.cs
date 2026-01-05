using Brinell.Blazor.ControlObject6.Context;
using Brinell.Blazor.ControlObject6.Controls;
using Brinell.Blazor.Tests.ControlObject6.Mocks;
using Brinell.Core.ControlObject6.Locators;
using Microsoft.Playwright;

namespace Brinell.Blazor.Tests.ControlObject6.Controls;

/// <summary>
/// Tests for IFrameControl (IF-001 to IF-010).
/// </summary>
[Trait("Category", "Media")]
[Trait("Platform", "Blazor")]
[Trait("Priority", "P2")]
public class IFrameControlTests
{
    #region Constructor Tests (IF-001 to IF-002)

    [Fact]
    public void IF001_Constructor_WithTestId_SetsLocator()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var context = new BlazorTestContext(mockPage.Object);

        // Act
        var iframe = new IFrameControl(context, "embeddedContent", null);

        // Assert
        iframe.Locator.Should().NotBeNull();
        iframe.Locator.Value.Should().Be("embeddedContent");
        iframe.Locator.Strategy.Should().Be(LocatorStrategy.TestId);
    }

    [Fact]
    public void IF002_Constructor_WithLocator_SetsLocator()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var context = new BlazorTestContext(mockPage.Object);
        var locator = By.Id("externalFrame");

        // Act
        var iframe = new IFrameControl(context, locator, null);

        // Assert
        iframe.Locator.Should().Be(locator);
    }

    #endregion

    #region Source Tests (IF-003 to IF-004)

    [Fact]
    public async Task IF003_GetSourceAsync_ReturnsSourceUrl()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.GetAttributeAsync("src", It.IsAny<LocatorGetAttributeOptions?>()))
            .ReturnsAsync("https://example.com/embedded");
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var iframe = new IFrameControl(context, "embeddedContent", null);

        // Act
        var source = await iframe.GetSourceAsync();

        // Assert
        source.Should().Be("https://example.com/embedded");
    }

    [Fact]
    public async Task IF004_GetTitleAsync_ReturnsTitle()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.GetAttributeAsync("title", It.IsAny<LocatorGetAttributeOptions?>()))
            .ReturnsAsync("Embedded Document");
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var iframe = new IFrameControl(context, "embeddedContent", null);

        // Act
        var title = await iframe.GetTitleAsync();

        // Assert
        title.Should().Be("Embedded Document");
    }

    #endregion

    #region Name Tests (IF-005)

    [Fact]
    public async Task IF005_GetNameAsync_ReturnsName()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.GetAttributeAsync("name", It.IsAny<LocatorGetAttributeOptions?>()))
            .ReturnsAsync("contentFrame");
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var iframe = new IFrameControl(context, "embeddedContent", null);

        // Act
        var name = await iframe.GetNameAsync();

        // Assert
        name.Should().Be("contentFrame");
    }

    #endregion

    #region Frame Interaction Tests (IF-006 to IF-008)

    [Fact]
    public async Task IF006_ClickInsideAsync_ClicksElementInFrame()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        var mockFrameLocator = new Mock<IFrameLocator>();
        var mockInnerLocator = new Mock<ILocator>();
        mockInnerLocator.Setup(l => l.ClickAsync(It.IsAny<LocatorClickOptions?>()))
            .Returns(Task.CompletedTask);
        mockFrameLocator.Setup(f => f.Locator(It.IsAny<string>(), It.IsAny<FrameLocatorLocatorOptions?>()))
            .Returns(mockInnerLocator.Object);
        mockLocator.Setup(l => l.FrameLocator(It.IsAny<string>()))
            .Returns(mockFrameLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var iframe = new IFrameControl(context, "embeddedContent", null);

        // Act
        await iframe.ClickInsideAsync("#submitButton");

        // Assert
        mockInnerLocator.Verify(l => l.ClickAsync(It.IsAny<LocatorClickOptions?>()), Times.Once);
    }

    [Fact]
    public async Task IF007_FillInsideAsync_FillsTextFieldInFrame()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        var mockFrameLocator = new Mock<IFrameLocator>();
        var mockInnerLocator = new Mock<ILocator>();
        mockInnerLocator.Setup(l => l.FillAsync(It.IsAny<string>(), It.IsAny<LocatorFillOptions?>()))
            .Returns(Task.CompletedTask);
        mockFrameLocator.Setup(f => f.Locator(It.IsAny<string>(), It.IsAny<FrameLocatorLocatorOptions?>()))
            .Returns(mockInnerLocator.Object);
        mockLocator.Setup(l => l.FrameLocator(It.IsAny<string>()))
            .Returns(mockFrameLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var iframe = new IFrameControl(context, "embeddedContent", null);

        // Act
        await iframe.FillInsideAsync("#nameInput", "John Doe");

        // Assert
        mockInnerLocator.Verify(l => l.FillAsync("John Doe", It.IsAny<LocatorFillOptions?>()), Times.Once);
    }

    [Fact]
    public async Task IF008_GetTextInsideAsync_ReturnsTextFromFrame()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        var mockFrameLocator = new Mock<IFrameLocator>();
        var mockInnerLocator = new Mock<ILocator>();
        mockInnerLocator.Setup(l => l.InnerTextAsync(It.IsAny<LocatorInnerTextOptions?>()))
            .ReturnsAsync("Welcome to the embedded page");
        mockFrameLocator.Setup(f => f.Locator(It.IsAny<string>(), It.IsAny<FrameLocatorLocatorOptions?>()))
            .Returns(mockInnerLocator.Object);
        mockLocator.Setup(l => l.FrameLocator(It.IsAny<string>()))
            .Returns(mockFrameLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var iframe = new IFrameControl(context, "embeddedContent", null);

        // Act
        var text = await iframe.GetTextInsideAsync("#heading");

        // Assert
        text.Should().Be("Welcome to the embedded page");
    }

    #endregion

    #region Element Exists Tests (IF-009 to IF-010)

    [Fact]
    public async Task IF009_ElementExistsInsideAsync_WhenExists_ReturnsTrue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        var mockFrameLocator = new Mock<IFrameLocator>();
        var mockInnerLocator = new Mock<ILocator>();
        mockInnerLocator.Setup(l => l.CountAsync()).ReturnsAsync(1);
        mockFrameLocator.Setup(f => f.Locator(It.IsAny<string>(), It.IsAny<FrameLocatorLocatorOptions?>()))
            .Returns(mockInnerLocator.Object);
        mockLocator.Setup(l => l.FrameLocator(It.IsAny<string>()))
            .Returns(mockFrameLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var iframe = new IFrameControl(context, "embeddedContent", null);

        // Act
        var exists = await iframe.ElementExistsInsideAsync("#submitButton");

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task IF010_ElementExistsInsideAsync_WhenNotExists_ReturnsFalse()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        var mockFrameLocator = new Mock<IFrameLocator>();
        var mockInnerLocator = new Mock<ILocator>();
        mockInnerLocator.Setup(l => l.CountAsync()).ReturnsAsync(0);
        mockFrameLocator.Setup(f => f.Locator(It.IsAny<string>(), It.IsAny<FrameLocatorLocatorOptions?>()))
            .Returns(mockInnerLocator.Object);
        mockLocator.Setup(l => l.FrameLocator(It.IsAny<string>()))
            .Returns(mockFrameLocator.Object);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var iframe = new IFrameControl(context, "embeddedContent", null);

        // Act
        var exists = await iframe.ElementExistsInsideAsync("#nonExistent");

        // Assert
        exists.Should().BeFalse();
    }

    #endregion
}
