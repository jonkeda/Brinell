using Brinell.Blazor.ControlObject6.Context;
using Brinell.Blazor.ControlObject6.Controls;
using Brinell.Blazor.Tests.ControlObject6.Mocks;
using Brinell.Core.ControlObject6.Locators;
using Microsoft.Playwright;

namespace Brinell.Blazor.Tests.ControlObject6.Controls;

/// <summary>
/// Tests for RadioButtonControl (BRB-001 to BRB-012).
/// </summary>
[Trait("Category", "Toggle")]
[Trait("Platform", "Blazor")]
public class RadioButtonControlTests
{
    #region Constructor Tests (BRB-001 to BRB-002)

    [Fact]
    [Trait("Priority", "P0")]
    public void BRB001_Constructor_WithTestId_SetsLocator()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var context = new BlazorTestContext(mockPage.Object);

        // Act
        var radio = new RadioButtonControl(context, "optionA", null);

        // Assert
        radio.Locator.Should().NotBeNull();
        radio.Locator.Value.Should().Be("optionA");
        radio.Locator.Strategy.Should().Be(LocatorStrategy.TestId);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void BRB002_Constructor_WithLocator_SetsLocator()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var context = new BlazorTestContext(mockPage.Object);
        var locator = By.Id("myRadio");

        // Act
        var radio = new RadioButtonControl(context, locator, null);

        // Assert
        radio.Locator.Should().Be(locator);
    }

    #endregion

    #region State Tests (BRB-003 to BRB-004)

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BRB003_IsCheckedAsync_WhenChecked_ReturnsTrue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.IsCheckedAsync(It.IsAny<LocatorIsCheckedOptions?>()))
            .ReturnsAsync(true);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var radio = new RadioButtonControl(context, "optionA", null);

        // Act
        var isChecked = await radio.IsCheckedAsync();

        // Assert
        isChecked.Should().BeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BRB004_IsCheckedAsync_WhenNotChecked_ReturnsFalse()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.IsCheckedAsync(It.IsAny<LocatorIsCheckedOptions?>()))
            .ReturnsAsync(false);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var radio = new RadioButtonControl(context, "optionA", null);

        // Act
        var isChecked = await radio.IsCheckedAsync();

        // Assert
        isChecked.Should().BeFalse();
    }

    #endregion

    #region Action Tests (BRB-005 to BRB-006)

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BRB005_SelectAsync_SelectsRadioButton()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.CheckAsync(It.IsAny<LocatorCheckOptions?>()))
            .Returns(Task.CompletedTask);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var radio = new RadioButtonControl(context, "optionA", null);

        // Act
        await radio.SelectAsync();

        // Assert
        mockLocator.Verify(l => l.CheckAsync(It.IsAny<LocatorCheckOptions?>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BRB006_ClickAsync_SelectsRadioButton()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var radio = new RadioButtonControl(context, "optionA", null);

        // Act
        await radio.ClickAsync();

        // Assert
        mockLocator.Verify(l => l.ClickAsync(It.IsAny<LocatorClickOptions?>()), Times.Once);
    }

    #endregion

    #region Value Tests (BRB-007 to BRB-008)

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BRB007_GetValueAsync_ReturnsValue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.GetAttributeAsync("value", It.IsAny<LocatorGetAttributeOptions?>()))
            .ReturnsAsync("option1");
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var radio = new RadioButtonControl(context, "optionA", null);

        // Act
        var value = await radio.GetValueAsync();

        // Assert
        value.Should().Be("option1");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BRB008_GetGroupNameAsync_ReturnsGroupName()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.GetAttributeAsync("name", It.IsAny<LocatorGetAttributeOptions?>()))
            .ReturnsAsync("colorGroup");
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var radio = new RadioButtonControl(context, "optionA", null);

        // Act
        var groupName = await radio.GetGroupNameAsync();

        // Assert
        groupName.Should().Be("colorGroup");
    }

    #endregion

    #region Assertion Tests (BRB-009)

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BRB009_AssertCheckedAsync_WhenMatches_Passes()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.IsCheckedAsync(It.IsAny<LocatorIsCheckedOptions?>()))
            .ReturnsAsync(true);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var radio = new RadioButtonControl(context, "optionA", null);

        // Act & Assert - should not throw
        await radio.Invoking(r => r.AssertCheckedAsync(true)).Should().NotThrowAsync();
    }

    #endregion

    #region Common State Tests (BRB-010 to BRB-012)

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BRB010_IsExistsAsync_WhenExists_ReturnsTrue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 1);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var radio = new RadioButtonControl(context, "optionA", null);

        // Act
        var exists = await radio.IsExistsAsync();

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BRB011_IsVisibleAsync_WhenVisible_ReturnsTrue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(visible: true);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var radio = new RadioButtonControl(context, "optionA", null);

        // Act
        var visible = await radio.IsVisibleAsync();

        // Assert
        visible.Should().BeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BRB012_IsEnabledAsync_WhenEnabled_ReturnsTrue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(enabled: true);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var radio = new RadioButtonControl(context, "optionA", null);

        // Act
        var enabled = await radio.IsEnabledAsync();

        // Assert
        enabled.Should().BeTrue();
    }

    #endregion
}
