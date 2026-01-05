using Brinell.Blazor.ControlObject6.Context;
using Brinell.Blazor.ControlObject6.Controls;
using Brinell.Blazor.Tests.ControlObject6.Mocks;
using Brinell.Core.ControlObject6.Locators;
using Microsoft.Playwright;

namespace Brinell.Blazor.Tests.ControlObject6.Controls;

/// <summary>
/// Tests for TimeInputControl (BTI-001 to BTI-010).
/// </summary>
[Trait("Category", "Time")]
[Trait("Platform", "Blazor")]
public class TimeInputControlTests
{
    #region Constructor Tests (BTI-001 to BTI-002)

    [Fact]
    [Trait("Priority", "P0")]
    public void BTI001_Constructor_WithTestId_SetsLocator()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var context = new BlazorTestContext(mockPage.Object);

        // Act
        var timeInput = new TimeInputControl(context, "appointmentTime", null);

        // Assert
        timeInput.Locator.Should().NotBeNull();
        timeInput.Locator.Value.Should().Be("appointmentTime");
        timeInput.Locator.Strategy.Should().Be(LocatorStrategy.TestId);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void BTI002_Constructor_WithLocator_SetsLocator()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var context = new BlazorTestContext(mockPage.Object);
        var locator = By.Id("myTimeInput");

        // Act
        var timeInput = new TimeInputControl(context, locator, null);

        // Assert
        timeInput.Locator.Should().Be(locator);
    }

    #endregion

    #region State Tests (BTI-003 to BTI-007)

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BTI003_GetTimeAsync_ReturnsCurrentTime()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.InputValueAsync(It.IsAny<LocatorInputValueOptions?>()))
            .ReturnsAsync("14:30");
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var timeInput = new TimeInputControl(context, "appointmentTime", null);

        // Act
        var time = await timeInput.GetTimeAsync();

        // Assert
        time.Should().Be(new TimeOnly(14, 30));
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BTI004_SetTimeAsync_SetsNewTime()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var timeInput = new TimeInputControl(context, "appointmentTime", null);

        // Act
        await timeInput.SetTimeAsync(new TimeOnly(10, 15));

        // Assert
        mockLocator.Verify(l => l.FillAsync("10:15", It.IsAny<LocatorFillOptions?>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BTI005_SetTimeAsync_WithNull_DoesNothing()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var timeInput = new TimeInputControl(context, "appointmentTime", null);

        // Act & Assert - should not throw and should not call Fill
        await timeInput.Invoking(t => t.SetTimeAsync(null)).Should().NotThrowAsync();
        mockLocator.Verify(l => l.FillAsync(It.IsAny<string>(), It.IsAny<LocatorFillOptions?>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BTI006_GetMinTimeAsync_ReturnsMinTime()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.GetAttributeAsync("min", It.IsAny<LocatorGetAttributeOptions?>()))
            .ReturnsAsync("09:00");
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var timeInput = new TimeInputControl(context, "appointmentTime", null);

        // Act
        var minTime = await timeInput.GetMinTimeAsync();

        // Assert
        minTime.Should().Be(new TimeOnly(9, 0));
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BTI007_GetMaxTimeAsync_ReturnsMaxTime()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.GetAttributeAsync("max", It.IsAny<LocatorGetAttributeOptions?>()))
            .ReturnsAsync("17:00");
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var timeInput = new TimeInputControl(context, "appointmentTime", null);

        // Act
        var maxTime = await timeInput.GetMaxTimeAsync();

        // Assert
        maxTime.Should().Be(new TimeOnly(17, 0));
    }

    #endregion

    #region Assertion Tests (BTI-008)

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BTI008_AssertTimeAsync_WhenMatches_Passes()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.InputValueAsync(It.IsAny<LocatorInputValueOptions?>()))
            .ReturnsAsync("14:30");
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var timeInput = new TimeInputControl(context, "appointmentTime", null);

        // Act & Assert - should not throw
        await timeInput.Invoking(t => t.AssertTimeAsync(new TimeOnly(14, 30))).Should().NotThrowAsync();
    }

    #endregion

    #region Common State Tests (BTI-009 to BTI-010)

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BTI009_IsExistsAsync_WhenExists_ReturnsTrue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 1);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var timeInput = new TimeInputControl(context, "appointmentTime", null);

        // Act
        var exists = await timeInput.IsExistsAsync();

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BTI010_IsEnabledAsync_WhenEnabled_ReturnsTrue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(enabled: true);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var timeInput = new TimeInputControl(context, "appointmentTime", null);

        // Act
        var enabled = await timeInput.IsEnabledAsync();

        // Assert
        enabled.Should().BeTrue();
    }

    #endregion
}
