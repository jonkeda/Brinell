using Brinell.Blazor.ControlObject6.Context;
using Brinell.Blazor.ControlObject6.Controls;
using Brinell.Blazor.Tests.ControlObject6.Mocks;
using Brinell.Core.ControlObject6.Locators;
using Microsoft.Playwright;

namespace Brinell.Blazor.Tests.ControlObject6.Controls;

/// <summary>
/// Tests for VideoControl (VC-001 to VC-012).
/// </summary>
[Trait("Category", "Media")]
[Trait("Platform", "Blazor")]
[Trait("Priority", "P2")]
public class VideoControlTests
{
    #region Constructor Tests (VC-001 to VC-002)

    [Fact]
    public void VC001_Constructor_WithTestId_SetsLocator()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var context = new BlazorTestContext(mockPage.Object);

        // Act
        var video = new VideoControl(context, "productVideo", null);

        // Assert
        video.Locator.Should().NotBeNull();
        video.Locator.Value.Should().Be("productVideo");
        video.Locator.Strategy.Should().Be(LocatorStrategy.TestId);
    }

    [Fact]
    public void VC002_Constructor_WithLocator_SetsLocator()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var context = new BlazorTestContext(mockPage.Object);
        var locator = By.Id("mainVideo");

        // Act
        var video = new VideoControl(context, locator, null);

        // Assert
        video.Locator.Should().Be(locator);
    }

    #endregion

    #region Playback Tests (VC-003 to VC-006)

    [Fact]
    public async Task VC003_PlayAsync_CallsPlayOnElement()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.EvaluateAsync(It.IsAny<string>(), It.IsAny<object?>()))
            .ReturnsAsync((System.Text.Json.JsonElement?)null);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var video = new VideoControl(context, "productVideo", null);

        // Act
        await video.PlayAsync();

        // Assert
        mockLocator.Verify(l => l.EvaluateAsync("video => video.play()", It.IsAny<object?>()), Times.Once);
    }

    [Fact]
    public async Task VC004_PauseAsync_CallsPauseOnElement()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.EvaluateAsync(It.IsAny<string>(), It.IsAny<object?>()))
            .ReturnsAsync((System.Text.Json.JsonElement?)null);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var video = new VideoControl(context, "productVideo", null);

        // Act
        await video.PauseAsync();

        // Assert
        mockLocator.Verify(l => l.EvaluateAsync("video => video.pause()", It.IsAny<object?>()), Times.Once);
    }

    [Fact]
    public async Task VC005_IsPlayingAsync_WhenPlaying_ReturnsTrue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.EvaluateAsync<bool>(It.Is<string>(s => s.Contains("paused") && s.Contains("ended")), It.IsAny<object?>()))
            .ReturnsAsync(true);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var video = new VideoControl(context, "productVideo", null);

        // Act
        var isPlaying = await video.IsPlayingAsync();

        // Assert
        isPlaying.Should().BeTrue();
    }

    [Fact]
    public async Task VC006_IsPausedAsync_WhenPaused_ReturnsTrue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.EvaluateAsync<bool>(It.Is<string>(s => s == "video => video.paused"), It.IsAny<object?>()))
            .ReturnsAsync(true);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var video = new VideoControl(context, "productVideo", null);

        // Act
        var isPaused = await video.IsPausedAsync();

        // Assert
        isPaused.Should().BeTrue();
    }

    #endregion

    #region Time Tests (VC-007 to VC-008)

    [Fact]
    public async Task VC007_GetCurrentTimeAsync_ReturnsTime()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.EvaluateAsync<double>(It.Is<string>(s => s.Contains("currentTime")), It.IsAny<object?>()))
            .ReturnsAsync(45.5);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var video = new VideoControl(context, "productVideo", null);

        // Act
        var currentTime = await video.GetCurrentTimeAsync();

        // Assert
        currentTime.Should().Be(45.5);
    }

    [Fact]
    public async Task VC008_GetDurationAsync_ReturnsDuration()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.EvaluateAsync<double>(It.Is<string>(s => s.Contains("duration")), It.IsAny<object?>()))
            .ReturnsAsync(120.0);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var video = new VideoControl(context, "productVideo", null);

        // Act
        var duration = await video.GetDurationAsync();

        // Assert
        duration.Should().Be(120.0);
    }

    #endregion

    #region Volume Tests (VC-009 to VC-010)

    [Fact]
    public async Task VC009_GetVolumeAsync_ReturnsVolume()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.EvaluateAsync<double>(It.Is<string>(s => s == "video => video.volume"), It.IsAny<object?>()))
            .ReturnsAsync(0.75);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var video = new VideoControl(context, "productVideo", null);

        // Act
        var volume = await video.GetVolumeAsync();

        // Assert
        volume.Should().Be(0.75);
    }

    [Fact]
    public async Task VC010_IsMutedAsync_WhenMuted_ReturnsTrue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.EvaluateAsync<bool>(It.Is<string>(s => s.Contains("muted")), It.IsAny<object?>()))
            .ReturnsAsync(true);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var video = new VideoControl(context, "productVideo", null);

        // Act
        var isMuted = await video.IsMutedAsync();

        // Assert
        isMuted.Should().BeTrue();
    }

    #endregion

    #region Source Tests (VC-011 to VC-012)

    [Fact]
    public async Task VC011_GetSourceAsync_ReturnsSourceUrl()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.GetAttributeAsync("src", It.IsAny<LocatorGetAttributeOptions?>()))
            .ReturnsAsync("https://example.com/video.mp4");
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var video = new VideoControl(context, "productVideo", null);

        // Act
        var source = await video.GetSourceAsync();

        // Assert
        source.Should().Be("https://example.com/video.mp4");
    }

    [Fact]
    public async Task VC012_GetPosterAsync_ReturnsPosterUrl()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.GetAttributeAsync("poster", It.IsAny<LocatorGetAttributeOptions?>()))
            .ReturnsAsync("https://example.com/poster.jpg");
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var video = new VideoControl(context, "productVideo", null);

        // Act
        var poster = await video.GetPosterAsync();

        // Assert
        poster.Should().Be("https://example.com/poster.jpg");
    }

    #endregion
}
