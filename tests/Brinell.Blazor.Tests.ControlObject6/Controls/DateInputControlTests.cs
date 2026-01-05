using Brinell.Blazor.ControlObject6.Context;
using Brinell.Blazor.ControlObject6.Controls;
using Brinell.Blazor.Tests.ControlObject6.Mocks;
using Brinell.Core.ControlObject6.Locators;
using Microsoft.Playwright;

namespace Brinell.Blazor.Tests.ControlObject6.Controls;

/// <summary>
/// Tests for DateInputControl (BDI-001 to BDI-010).
/// </summary>
[Trait("Category", "Date")]
[Trait("Platform", "Blazor")]
public class DateInputControlTests
{
    #region Constructor Tests (BDI-001 to BDI-002)

    [Fact]
    [Trait("Priority", "P0")]
    public void BDI001_Constructor_WithTestId_SetsLocator()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var context = new BlazorTestContext(mockPage.Object);

        // Act
        var dateInput = new DateInputControl(context, "birthDate", null);

        // Assert
        dateInput.Locator.Should().NotBeNull();
        dateInput.Locator.Value.Should().Be("birthDate");
        dateInput.Locator.Strategy.Should().Be(LocatorStrategy.TestId);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void BDI002_Constructor_WithLocator_SetsLocator()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var context = new BlazorTestContext(mockPage.Object);
        var locator = By.Id("myDateInput");

        // Act
        var dateInput = new DateInputControl(context, locator, null);

        // Assert
        dateInput.Locator.Should().Be(locator);
    }

    #endregion

    #region State Tests (BDI-003 to BDI-007)

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BDI003_GetDateAsync_ReturnsCurrentDate()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.InputValueAsync(It.IsAny<LocatorInputValueOptions?>()))
            .ReturnsAsync("2025-01-15");
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var dateInput = new DateInputControl(context, "birthDate", null);

        // Act
        var date = await dateInput.GetDateAsync();

        // Assert
        date.Should().Be(new DateOnly(2025, 1, 15));
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BDI004_SetDateAsync_SetsNewDate()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var dateInput = new DateInputControl(context, "birthDate", null);

        // Act
        await dateInput.SetDateAsync(new DateOnly(2025, 6, 20));

        // Assert
        mockLocator.Verify(l => l.FillAsync("2025-06-20", It.IsAny<LocatorFillOptions?>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BDI005_SetDateAsync_WithNull_DoesNothing()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var dateInput = new DateInputControl(context, "birthDate", null);

        // Act & Assert - should not throw and should not call Fill
        await dateInput.Invoking(d => d.SetDateAsync(null)).Should().NotThrowAsync();
        mockLocator.Verify(l => l.FillAsync(It.IsAny<string>(), It.IsAny<LocatorFillOptions?>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BDI006_GetMinDateAsync_ReturnsMinDate()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.GetAttributeAsync("min", It.IsAny<LocatorGetAttributeOptions?>()))
            .ReturnsAsync("2020-01-01");
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var dateInput = new DateInputControl(context, "birthDate", null);

        // Act
        var minDate = await dateInput.GetMinDateAsync();

        // Assert
        minDate.Should().Be(new DateOnly(2020, 1, 1));
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BDI007_GetMaxDateAsync_ReturnsMaxDate()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.GetAttributeAsync("max", It.IsAny<LocatorGetAttributeOptions?>()))
            .ReturnsAsync("2030-12-31");
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var dateInput = new DateInputControl(context, "birthDate", null);

        // Act
        var maxDate = await dateInput.GetMaxDateAsync();

        // Assert
        maxDate.Should().Be(new DateOnly(2030, 12, 31));
    }

    #endregion

    #region Assertion Tests (BDI-008)

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BDI008_AssertDateAsync_WhenMatches_Passes()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.InputValueAsync(It.IsAny<LocatorInputValueOptions?>()))
            .ReturnsAsync("2025-01-15");
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var dateInput = new DateInputControl(context, "birthDate", null);

        // Act & Assert - should not throw
        await dateInput.Invoking(d => d.AssertDateAsync(new DateOnly(2025, 1, 15))).Should().NotThrowAsync();
    }

    #endregion

    #region Common State Tests (BDI-009 to BDI-010)

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BDI009_IsExistsAsync_WhenExists_ReturnsTrue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 1);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var dateInput = new DateInputControl(context, "birthDate", null);

        // Act
        var exists = await dateInput.IsExistsAsync();

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BDI010_IsEnabledAsync_WhenEnabled_ReturnsTrue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(enabled: true);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var dateInput = new DateInputControl(context, "birthDate", null);

        // Act
        var enabled = await dateInput.IsEnabledAsync();

        // Assert
        enabled.Should().BeTrue();
    }

    #endregion
}
