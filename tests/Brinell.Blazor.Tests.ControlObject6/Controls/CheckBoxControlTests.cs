using Brinell.Blazor.ControlObject6.Context;
using Brinell.Blazor.ControlObject6.Controls;
using Brinell.Blazor.Tests.ControlObject6.Mocks;
using Brinell.Core.ControlObject6.Locators;
using Microsoft.Playwright;

namespace Brinell.Blazor.Tests.ControlObject6.Controls;

/// <summary>
/// Tests for CheckBoxControl (BCB-001 to BCB-018).
/// </summary>
[Trait("Category", "Toggle")]
[Trait("Platform", "Blazor")]
public class CheckBoxControlTests
{
    #region Constructor Tests (BCB-001 to BCB-002)

    [Fact]
    [Trait("Priority", "P0")]
    public void BCB001_Constructor_WithTestId_SetsLocator()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var context = new BlazorTestContext(mockPage.Object);

        // Act
        var checkBox = new CheckBoxControl(context, "agreeCheckbox", null);

        // Assert
        checkBox.Locator.Should().NotBeNull();
        checkBox.Locator.Value.Should().Be("agreeCheckbox");
        checkBox.Locator.Strategy.Should().Be(LocatorStrategy.TestId);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void BCB002_Constructor_WithLocator_SetsLocator()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var context = new BlazorTestContext(mockPage.Object);
        var locator = By.Id("myCheckbox");

        // Act
        var checkBox = new CheckBoxControl(context, locator, null);

        // Assert
        checkBox.Locator.Should().Be(locator);
    }

    #endregion

    #region State Tests (BCB-003 to BCB-006)

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BCB003_IsCheckedAsync_WhenChecked_ReturnsTrue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.IsCheckedAsync(It.IsAny<LocatorIsCheckedOptions?>()))
            .ReturnsAsync(true);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var checkBox = new CheckBoxControl(context, "checkbox", null);

        // Act
        var isChecked = await checkBox.IsCheckedAsync();

        // Assert
        isChecked.Should().BeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BCB004_IsCheckedAsync_WhenNotChecked_ReturnsFalse()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.IsCheckedAsync(It.IsAny<LocatorIsCheckedOptions?>()))
            .ReturnsAsync(false);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var checkBox = new CheckBoxControl(context, "checkbox", null);

        // Act
        var isChecked = await checkBox.IsCheckedAsync();

        // Assert
        isChecked.Should().BeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BCB005_IsExistsAsync_WhenExists_ReturnsTrue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(count: 1);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var checkBox = new CheckBoxControl(context, "checkbox", null);

        // Act
        var exists = await checkBox.IsExistsAsync();

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BCB006_IsEnabledAsync_WhenEnabled_ReturnsTrue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(enabled: true);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var checkBox = new CheckBoxControl(context, "checkbox", null);

        // Act
        var enabled = await checkBox.IsEnabledAsync();

        // Assert
        enabled.Should().BeTrue();
    }

    #endregion

    #region Action Tests (BCB-007 to BCB-012)

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BCB007_CheckAsync_SetsCheckedToTrue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.SetCheckedAsync(It.IsAny<bool>(), It.IsAny<LocatorSetCheckedOptions?>()))
            .Returns(Task.CompletedTask);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var checkBox = new CheckBoxControl(context, "checkbox", null);

        // Act
        await checkBox.CheckAsync();

        // Assert
        mockLocator.Verify(l => l.SetCheckedAsync(true, It.IsAny<LocatorSetCheckedOptions?>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BCB008_UncheckAsync_SetsCheckedToFalse()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.SetCheckedAsync(It.IsAny<bool>(), It.IsAny<LocatorSetCheckedOptions?>()))
            .Returns(Task.CompletedTask);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var checkBox = new CheckBoxControl(context, "checkbox", null);

        // Act
        await checkBox.UncheckAsync();

        // Assert
        mockLocator.Verify(l => l.SetCheckedAsync(false, It.IsAny<LocatorSetCheckedOptions?>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BCB009_SetCheckedAsync_WithTrue_SetsChecked()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.SetCheckedAsync(It.IsAny<bool>(), It.IsAny<LocatorSetCheckedOptions?>()))
            .Returns(Task.CompletedTask);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var checkBox = new CheckBoxControl(context, "checkbox", null);

        // Act
        await checkBox.SetCheckedAsync(true);

        // Assert
        mockLocator.Verify(l => l.SetCheckedAsync(true, It.IsAny<LocatorSetCheckedOptions?>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BCB010_SetCheckedAsync_WithNull_DoesNothing()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var checkBox = new CheckBoxControl(context, "checkbox", null);

        // Act & Assert - should not throw
        await checkBox.Invoking(c => c.SetCheckedAsync(null)).Should().NotThrowAsync();
        mockLocator.Verify(l => l.SetCheckedAsync(It.IsAny<bool>(), It.IsAny<LocatorSetCheckedOptions?>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BCB011_ToggleAsync_WhenChecked_Unchecks()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.IsCheckedAsync(It.IsAny<LocatorIsCheckedOptions?>()))
            .ReturnsAsync(true);
        mockLocator.Setup(l => l.SetCheckedAsync(It.IsAny<bool>(), It.IsAny<LocatorSetCheckedOptions?>()))
            .Returns(Task.CompletedTask);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var checkBox = new CheckBoxControl(context, "checkbox", null);

        // Act
        await checkBox.ToggleAsync();

        // Assert
        mockLocator.Verify(l => l.SetCheckedAsync(false, It.IsAny<LocatorSetCheckedOptions?>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BCB012_ToggleAsync_WhenUnchecked_Checks()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.IsCheckedAsync(It.IsAny<LocatorIsCheckedOptions?>()))
            .ReturnsAsync(false);
        mockLocator.Setup(l => l.SetCheckedAsync(It.IsAny<bool>(), It.IsAny<LocatorSetCheckedOptions?>()))
            .Returns(Task.CompletedTask);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var checkBox = new CheckBoxControl(context, "checkbox", null);

        // Act
        await checkBox.ToggleAsync();

        // Assert
        mockLocator.Verify(l => l.SetCheckedAsync(true, It.IsAny<LocatorSetCheckedOptions?>()), Times.Once);
    }

    #endregion

    #region Click Tests (BCB-013 to BCB-014)

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BCB013_ClickAsync_CallsLocatorClick()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var checkBox = new CheckBoxControl(context, "checkbox", null);

        // Act
        await checkBox.ClickAsync();

        // Assert
        mockLocator.Verify(l => l.ClickAsync(It.IsAny<LocatorClickOptions?>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task BCB014_IsVisibleAsync_WhenVisible_ReturnsTrue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator(visible: true);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var checkBox = new CheckBoxControl(context, "checkbox", null);

        // Act
        var visible = await checkBox.IsVisibleAsync();

        // Assert
        visible.Should().BeTrue();
    }

    #endregion

    #region Assertion Tests (BCB-015 to BCB-018)

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BCB015_AssertCheckedAsync_WhenMatches_Passes()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.IsCheckedAsync(It.IsAny<LocatorIsCheckedOptions?>()))
            .ReturnsAsync(true);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var checkBox = new CheckBoxControl(context, "checkbox", null);

        // Act & Assert - should not throw
        await checkBox.Invoking(c => c.AssertCheckedAsync(true)).Should().NotThrowAsync();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BCB016_AssertCheckedAsync_WhenMismatch_Throws()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.IsCheckedAsync(It.IsAny<LocatorIsCheckedOptions?>()))
            .ReturnsAsync(false);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var checkBox = new CheckBoxControl(context, "checkbox", null);

        // Act & Assert - should throw
        await checkBox.Invoking(c => c.AssertCheckedAsync(true)).Should().ThrowAsync<Exception>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BCB017_AssertCheckedAsync_WithNull_DoesNothing()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        var checkBox = new CheckBoxControl(context, "checkbox", null);

        // Act & Assert - should not throw
        await checkBox.Invoking(c => c.AssertCheckedAsync(null)).Should().NotThrowAsync();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task BCB018_WaitCheckedAsync_WhenStateMatches_ReturnsTrue()
    {
        // Arrange
        var mockPage = MockPlaywrightFactory.CreateMockPage();
        var mockLocator = MockPlaywrightFactory.CreateMockLocator();
        mockLocator.Setup(l => l.IsCheckedAsync(It.IsAny<LocatorIsCheckedOptions?>()))
            .ReturnsAsync(true);
        MockPlaywrightFactory.SetupLocator(mockPage, mockLocator);

        var context = new BlazorTestContext(mockPage.Object);
        context.DefaultTimeoutMs = 100;
        var checkBox = new CheckBoxControl(context, "checkbox", null);

        // Act
        var result = await checkBox.WaitCheckedAsync(true, 100);

        // Assert
        result.Should().BeTrue();
    }

    #endregion
}
