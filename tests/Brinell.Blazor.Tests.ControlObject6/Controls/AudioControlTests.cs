using Brinell.Blazor.ControlObject6.Context;
using Brinell.Blazor.ControlObject6.Controls;
using Brinell.Blazor.Tests.ControlObject6.Mocks;
using Brinell.Core.ControlObject6.Locators;
using Microsoft.Playwright;

namespace Brinell.Blazor.Tests.ControlObject6.Controls;

/// <summary>
/// Tests for AudioControl (AC-001 to AC-012).
/// </summary>
[Trait("Category", "Media")]
[Trait("Platform", "Blazor")]
[Trait("Priority", "P2")]
public class AudioControlTests
{
    #region Constructor Tests (AC-001 to AC-002)

    [Fact]
    public void AC001_Constructor_WithTestId_SetsLocator()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var context = new BlazorTestContext(mockPage.Object);

        // Act
        var audio = new AudioControl(context, "backgroundMusic", null);

        // Assert
        audio.Locator.Should().NotBeNull();
        audio.Locator.Value.Should().Be("backgroundMusic");
        audio.Locator.Strategy.Should().Be(LocatorStrategy.TestId);
    }

    [Fact]
    public void AC002_Constructor_WithLocator_SetsLocator()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var context = new BlazorTestContext(mockPage.Object);
        var locator = By.Id("podcastAudio");

        // Act
        var audio = new AudioControl(context, locator, null);

        // Assert
        audio.Locator.Should().Be(locator);
    }

    #endregion

    #region Playback Tests (AC-003 to AC-006)

    [Fact]
    public async Task AC003_PlayAsync_CallsPlayOnElement()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.EvaluateAsync(It.IsAny<string>(), It.IsAny<object?>()))
            .ReturnsAsync((System.Text.Json.JsonElement?)null);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var audio = new AudioControl(context, "backgroundMusic", null);

        // Act
        await audio.PlayAsync();

        // Assert
        mockLocator.Verify(l => l.EvaluateAsync("audio => audio.play()", It.IsAny<object?>()), Times.Once);
    }

    [Fact]
    public async Task AC004_PauseAsync_CallsPauseOnElement()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.EvaluateAsync(It.IsAny<string>(), It.IsAny<object?>()))
            .ReturnsAsync((System.Text.Json.JsonElement?)null);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var audio = new AudioControl(context, "backgroundMusic", null);

        // Act
        await audio.PauseAsync();

        // Assert
        mockLocator.Verify(l => l.EvaluateAsync("audio => audio.pause()", It.IsAny<object?>()), Times.Once);
    }

    [Fact]
    public async Task AC005_IsPlayingAsync_WhenPlaying_ReturnsTrue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.EvaluateAsync<bool>(It.Is<string>(s => s.Contains("paused") && s.Contains("ended")), It.IsAny<object?>()))
            .ReturnsAsync(true);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var audio = new AudioControl(context, "backgroundMusic", null);

        // Act
        var isPlaying = await audio.IsPlayingAsync();

        // Assert
        isPlaying.Should().BeTrue();
    }

    [Fact]
    public async Task AC006_IsPausedAsync_WhenPaused_ReturnsTrue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.EvaluateAsync<bool>(It.Is<string>(s => s == "audio => audio.paused"), It.IsAny<object?>()))
            .ReturnsAsync(true);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var audio = new AudioControl(context, "backgroundMusic", null);

        // Act
        var isPaused = await audio.IsPausedAsync();

        // Assert
        isPaused.Should().BeTrue();
    }

    #endregion

    #region Time Tests (AC-007 to AC-008)

    [Fact]
    public async Task AC007_GetCurrentTimeAsync_ReturnsTime()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.EvaluateAsync<double>(It.Is<string>(s => s.Contains("currentTime")), It.IsAny<object?>()))
            .ReturnsAsync(30.5);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var audio = new AudioControl(context, "backgroundMusic", null);

        // Act
        var currentTime = await audio.GetCurrentTimeAsync();

        // Assert
        currentTime.Should().Be(30.5);
    }

    [Fact]
    public async Task AC008_GetDurationAsync_ReturnsDuration()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.EvaluateAsync<double>(It.Is<string>(s => s.Contains("duration")), It.IsAny<object?>()))
            .ReturnsAsync(180.0);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var audio = new AudioControl(context, "backgroundMusic", null);

        // Act
        var duration = await audio.GetDurationAsync();

        // Assert
        duration.Should().Be(180.0);
    }

    #endregion

    #region Volume Tests (AC-009 to AC-010)

    [Fact]
    public async Task AC009_GetVolumeAsync_ReturnsVolume()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.EvaluateAsync<double>(It.Is<string>(s => s == "audio => audio.volume"), It.IsAny<object?>()))
            .ReturnsAsync(0.5);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var audio = new AudioControl(context, "backgroundMusic", null);

        // Act
        var volume = await audio.GetVolumeAsync();

        // Assert
        volume.Should().Be(0.5);
    }

    [Fact]
    public async Task AC010_IsMutedAsync_WhenMuted_ReturnsTrue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.EvaluateAsync<bool>(It.Is<string>(s => s.Contains("muted")), It.IsAny<object?>()))
            .ReturnsAsync(true);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var audio = new AudioControl(context, "backgroundMusic", null);

        // Act
        var isMuted = await audio.IsMutedAsync();

        // Assert
        isMuted.Should().BeTrue();
    }

    #endregion

    #region Source Tests (AC-011 to AC-012)

    [Fact]
    public async Task AC011_GetSourceAsync_ReturnsSourceUrl()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.GetAttributeAsync("src", It.IsAny<LocatorGetAttributeOptions?>()))
            .ReturnsAsync("https://example.com/audio.mp3");
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var audio = new AudioControl(context, "backgroundMusic", null);

        // Act
        var source = await audio.GetSourceAsync();

        // Assert
        source.Should().Be("https://example.com/audio.mp3");
    }

    [Fact]
    public async Task AC012_IsEndedAsync_WhenEnded_ReturnsTrue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.EvaluateAsync<bool>(It.Is<string>(s => s.Contains("ended")), It.IsAny<object?>()))
            .ReturnsAsync(true);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var audio = new AudioControl(context, "backgroundMusic", null);

        // Act
        var isEnded = await audio.IsEndedAsync();

        // Assert
        isEnded.Should().BeTrue();
    }

    #endregion
}
