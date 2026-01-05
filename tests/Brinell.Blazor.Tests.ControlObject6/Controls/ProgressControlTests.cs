using Brinell.Blazor.ControlObject6.Context;
using Brinell.Blazor.ControlObject6.Controls;
using Brinell.Blazor.Tests.ControlObject6.Mocks;
using Brinell.Core.ControlObject6.Locators;
using Microsoft.Playwright;

namespace Brinell.Blazor.Tests.ControlObject6.Controls;

/// <summary>
/// Tests for ProgressControl (BPR-001 to BPR-008).
/// </summary>
[Trait("Category", "Progress")]
[Trait("Platform", "Blazor")]
public class ProgressControlTests
{
    #region Constructor Tests (BPR-001 to BPR-002)

    [Fact]
    [Trait("Priority", "P0")]
    public void BPR001_Constructor_WithTestId_SetsLocator()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var context = new BlazorTestContext(mockPage.Object);

        // Act
        var progress = new ProgressControl(context, "loadProgress", null);

        // Assert
        progress.Locator.Should().NotBeNull();
        progress.Locator.Value.Should().Be("loadProgress");
        progress.Locator.Strategy.Should().Be(LocatorStrategy.TestId);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void BPR002_Constructor_WithLocator_SetsLocator()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var context = new BlazorTestContext(mockPage.Object);
        var locator = By.Id("myProgress");

        // Act
        var progress = new ProgressControl(context, locator, null);

        // Assert
        progress.Locator.Should().Be(locator);
    }

    #endregion

    #region State Tests (BPR-003 to BPR-004)

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BPR003_GetProgressAsync_ReturnsCurrentProgress()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.GetAttributeAsync("value", It.IsAny<LocatorGetAttributeOptions?>()))
            .ReturnsAsync("50");
        mockLocator.Setup(l => l.GetAttributeAsync("max", It.IsAny<LocatorGetAttributeOptions?>()))
            .ReturnsAsync("100");
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var progress = new ProgressControl(context, "loadProgress", null);

        // Act
        var value = await progress.GetProgressAsync();

        // Assert
        value.Should().BeApproximately(0.5, 0.01); // 50/100 = 0.5
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BPR004_GetProgressAsync_WithNoMax_UsesDefaultMax()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.GetAttributeAsync("value", It.IsAny<LocatorGetAttributeOptions?>()))
            .ReturnsAsync("0.75");
        mockLocator.Setup(l => l.GetAttributeAsync("max", It.IsAny<LocatorGetAttributeOptions?>()))
            .ReturnsAsync((string?)null);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var progress = new ProgressControl(context, "loadProgress", null);

        // Act
        var value = await progress.GetProgressAsync();

        // Assert
        value.Should().BeApproximately(0.75, 0.01); // 0.75/1 = 0.75
    }

    #endregion

    #region Assertion Tests (BPR-005)

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BPR005_AssertProgressAsync_WhenMatches_Passes()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.GetAttributeAsync("value", It.IsAny<LocatorGetAttributeOptions?>()))
            .ReturnsAsync("75");
        mockLocator.Setup(l => l.GetAttributeAsync("max", It.IsAny<LocatorGetAttributeOptions?>()))
            .ReturnsAsync("100");
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var progress = new ProgressControl(context, "loadProgress", null);

        // Act & Assert - should not throw
        await progress.Invoking(p => p.AssertProgressAsync(0.75)).Should().NotThrowAsync();
    }

    #endregion

    #region Common State Tests (BPR-006 to BPR-008)

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BPR006_IsExistsAsync_WhenExists_ReturnsTrue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 1);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var progress = new ProgressControl(context, "loadProgress", null);

        // Act
        var exists = await progress.IsExistsAsync();

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BPR007_IsVisibleAsync_WhenVisible_ReturnsTrue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(visible: true);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var progress = new ProgressControl(context, "loadProgress", null);

        // Act
        var visible = await progress.IsVisibleAsync();

        // Assert
        visible.Should().BeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BPR008_IsIndeterminateAsync_WhenNoValue_ReturnsTrue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.GetAttributeAsync("value", It.IsAny<LocatorGetAttributeOptions?>()))
            .ReturnsAsync((string?)null);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var progress = new ProgressControl(context, "loadProgress", null);

        // Act
        var isIndeterminate = await progress.IsIndeterminateAsync();

        // Assert
        isIndeterminate.Should().BeTrue();
    }

    #endregion
}
