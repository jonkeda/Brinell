using Brinell.Blazor.ControlObject6.Context;
using Brinell.Blazor.Tests.ControlObject6.Mocks;
using Microsoft.Playwright;

namespace Brinell.Blazor.Tests.ControlObject6.Context;

/// <summary>
/// Tests for BlazorTestContext (BTC-001 to BTC-034).
/// </summary>
public class BlazorTestContextTests
{
    #region Constructor and Properties (BTC-001 to BTC-007)

    [Fact]
    public void BTC001_Constructor_WithNullPage_ThrowsArgumentNullException()
    {
        // Arrange & Act
        Action act = () => new BlazorTestContext(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void BTC002_Constructor_SetsPageProperty()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();

        // Act
        var context = new BlazorTestContext(mockPage.Object);

        // Assert
        context.Page.Should().NotBeNull();
        context.Page.Should().BeSameAs(mockPage.Object);
    }

    [Fact]
    public void BTC003_DefaultTimeoutMs_DefaultIs30000()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var context = new BlazorTestContext(mockPage.Object);

        // Act & Assert
        context.DefaultTimeoutMs.Should().Be(30000);
    }

    [Fact]
    public void BTC004_DefaultPollingIntervalMs_DefaultIs100()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var context = new BlazorTestContext(mockPage.Object);

        // Act & Assert
        context.DefaultPollingIntervalMs.Should().Be(100);
    }

    [Fact]
    public void BTC005_CurrentPage_IsNullInitially()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var context = new BlazorTestContext(mockPage.Object);

        // Act & Assert
        context.CurrentPage.Should().BeNull();
    }

    [Fact]
    public void BTC006_DefaultTimeoutMs_CanBeChanged()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var context = new BlazorTestContext(mockPage.Object);

        // Act
        context.DefaultTimeoutMs = 60000;

        // Assert
        context.DefaultTimeoutMs.Should().Be(60000);
    }

    [Fact]
    public void BTC007_DefaultPollingIntervalMs_CanBeChanged()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var context = new BlazorTestContext(mockPage.Object);

        // Act
        context.DefaultPollingIntervalMs = 200;

        // Assert
        context.DefaultPollingIntervalMs.Should().Be(200);
    }

    #endregion

    #region Navigation (BTC-010 to BTC-014)

    [Fact]
    public async Task BTC010_NavigateToAsync_WithNull_DoesNothing()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var context = new BlazorTestContext(mockPage.Object);

        // Act & Assert - should not throw
        await context.Invoking(c => c.NavigateToAsync(null)).Should().NotThrowAsync();
    }

    [Fact]
    public async Task BTC011_NavigateToAsync_WithRoute_Navigates()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockResponse = new Mock<IResponse>();
        mockPage.Setup(p => p.GotoAsync(It.IsAny<string>(), It.IsAny<PageGotoOptions?>()))
            .ReturnsAsync(mockResponse.Object);

        var context = new BlazorTestContext(mockPage.Object);

        // Act
        await context.NavigateToAsync("https://example.com");

        // Assert
        mockPage.Verify(p => p.GotoAsync(
            It.Is<string>(s => s == "https://example.com"),
            It.IsAny<PageGotoOptions?>()), 
            Times.Once);
    }

    #endregion

    #region Screenshot and Logging (BTC-030 to BTC-034)

    [Fact]
    public async Task BTC030_TakeScreenshotAsync_WithNull_DoesNothing()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var context = new BlazorTestContext(mockPage.Object);

        // Act & Assert - should not throw
        await context.Invoking(c => c.TakeScreenshotAsync(null)).Should().NotThrowAsync();
    }

    [Fact]
    public void BTC032_Log_WithNull_DoesNothing()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var context = new BlazorTestContext(mockPage.Object);

        // Act & Assert - should not throw
        var act = () => context.Log(null);
        act.Should().NotThrow();
    }

    [Fact]
    public void BTC033_Log_WithMessage_DoesNotThrow()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var context = new BlazorTestContext(mockPage.Object);

        // Act & Assert - should not throw
        var act = () => context.Log("Test message");
        act.Should().NotThrow();
    }

    [Fact]
    public void BTC034_LogError_WithMessage_DoesNotThrow()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var context = new BlazorTestContext(mockPage.Object);

        // Act & Assert - should not throw
        var act = () => context.LogError("Error message");
        act.Should().NotThrow();
    }

    #endregion
}
