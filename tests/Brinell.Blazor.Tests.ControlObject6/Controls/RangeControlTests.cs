using Brinell.Blazor.ControlObject6.Context;
using Brinell.Blazor.ControlObject6.Controls;
using Brinell.Blazor.Tests.ControlObject6.Mocks;
using Brinell.Core.ControlObject6.Locators;
using Microsoft.Playwright;

namespace Brinell.Blazor.Tests.ControlObject6.Controls;

/// <summary>
/// Tests for RangeControl (BRG-001 to BRG-012).
/// </summary>
[Trait("Category", "Range")]
[Trait("Platform", "Blazor")]
public class RangeControlTests
{
    #region Constructor Tests (BRG-001 to BRG-002)

    [Fact]
    [Trait("Priority", "P0")]
    public void BRG001_Constructor_WithTestId_SetsLocator()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var context = new BlazorTestContext(mockPage.Object);

        // Act
        var range = new RangeControl(context, "volumeSlider", null);

        // Assert
        range.Locator.Should().NotBeNull();
        range.Locator.Value.Should().Be("volumeSlider");
        range.Locator.Strategy.Should().Be(LocatorStrategy.TestId);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void BRG002_Constructor_WithLocator_SetsLocator()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var context = new BlazorTestContext(mockPage.Object);
        var locator = By.Id("myRange");

        // Act
        var range = new RangeControl(context, locator, null);

        // Assert
        range.Locator.Should().Be(locator);
    }

    #endregion

    #region State Tests (BRG-003 to BRG-007)

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BRG003_GetValueAsync_ReturnsCurrentValue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.InputValueAsync(It.IsAny<LocatorInputValueOptions?>()))
            .ReturnsAsync("50");
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var range = new RangeControl(context, "volumeSlider", null);

        // Act
        var value = await range.GetValueAsync();

        // Assert
        value.Should().Be(50);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BRG004_SetValueAsync_SetsNewValue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var range = new RangeControl(context, "volumeSlider", null);

        // Act
        await range.SetValueAsync(75);

        // Assert
        mockLocator.Verify(l => l.FillAsync("75", It.IsAny<LocatorFillOptions?>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BRG005_GetMinimumAsync_ReturnsMinValue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.GetAttributeAsync("min", It.IsAny<LocatorGetAttributeOptions?>()))
            .ReturnsAsync("0");
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var range = new RangeControl(context, "volumeSlider", null);

        // Act
        var min = await range.GetMinimumAsync();

        // Assert
        min.Should().Be(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BRG006_GetMaximumAsync_ReturnsMaxValue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.GetAttributeAsync("max", It.IsAny<LocatorGetAttributeOptions?>()))
            .ReturnsAsync("100");
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var range = new RangeControl(context, "volumeSlider", null);

        // Act
        var max = await range.GetMaximumAsync();

        // Assert
        max.Should().Be(100);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BRG007_GetStepAsync_ReturnsStepValue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.GetAttributeAsync("step", It.IsAny<LocatorGetAttributeOptions?>()))
            .ReturnsAsync("5");
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var range = new RangeControl(context, "volumeSlider", null);

        // Act
        var step = await range.GetStepAsync();

        // Assert
        step.Should().Be(5);
    }

    #endregion

    #region Action Tests (BRG-008)

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BRG008_SetValueAsync_WithNull_DoesNothing()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var range = new RangeControl(context, "volumeSlider", null);

        // Act & Assert - should not throw and should not call Fill
        await range.Invoking(r => r.SetValueAsync(null)).Should().NotThrowAsync();
        mockLocator.Verify(l => l.FillAsync(It.IsAny<string>(), It.IsAny<LocatorFillOptions?>()), Times.Never);
    }

    #endregion

    #region Assertion Tests (BRG-009)

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BRG009_AssertValueAsync_WhenMatches_Passes()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.InputValueAsync(It.IsAny<LocatorInputValueOptions?>()))
            .ReturnsAsync("50");
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var range = new RangeControl(context, "volumeSlider", null);

        // Act & Assert - should not throw
        await range.Invoking(r => r.AssertValueAsync(50)).Should().NotThrowAsync();
    }

    #endregion

    #region Common State Tests (BRG-010 to BRG-012)

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BRG010_IsExistsAsync_WhenExists_ReturnsTrue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 1);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var range = new RangeControl(context, "volumeSlider", null);

        // Act
        var exists = await range.IsExistsAsync();

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BRG011_IsVisibleAsync_WhenVisible_ReturnsTrue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(visible: true);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var range = new RangeControl(context, "volumeSlider", null);

        // Act
        var visible = await range.IsVisibleAsync();

        // Assert
        visible.Should().BeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BRG012_IsEnabledAsync_WhenEnabled_ReturnsTrue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(enabled: true);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var range = new RangeControl(context, "volumeSlider", null);

        // Act
        var enabled = await range.IsEnabledAsync();

        // Assert
        enabled.Should().BeTrue();
    }

    #endregion
}
