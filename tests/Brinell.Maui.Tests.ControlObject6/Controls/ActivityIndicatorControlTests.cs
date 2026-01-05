using Brinell.Core.ControlObject6.Locators;
using Brinell.Maui.Tests.ControlObject6.Mocks;

namespace Brinell.Maui.Tests.ControlObject6.Controls;

/// <summary>
/// Tests for ActivityIndicatorControl (AI-001 to AI-008).
/// </summary>
[Trait("Category", "ActivityIndicator")]
[Trait("Platform", "MAUI")]
[Trait("Priority", "P2")]
public class ActivityIndicatorControlTests
{
    #region Constructor Tests (AI-001 to AI-002)

    [Fact]
    public void AI001_ActivityIndicator_Constructor_WithAutomationId_SetsLocator()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        // Act
        var indicator = new TestableActivityIndicatorControl(context, "loadingIndicator", null);

        // Assert
        indicator.Locator.Should().NotBeNull();
        indicator.Locator.Value.Should().Be("loadingIndicator");
        indicator.Locator.Strategy.Should().Be(LocatorStrategy.AutomationId);
    }

    [Fact]
    public void AI002_ActivityIndicator_Constructor_WithLocator_SetsLocator()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);
        var locator = By.AutomationId("spinner");

        // Act
        var indicator = new TestableActivityIndicatorControl(context, locator, null);

        // Assert
        indicator.Locator.Should().Be(locator);
    }

    #endregion

    #region IsRunning State Tests (AI-003 to AI-004)

    [Fact]
    public void AI003_ActivityIndicator_IsRunning_WhenRunning_ReturnsTrue()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);

        var context = new TestableMauiTestContext(mockDriver.Object);
        var indicator = new TestableActivityIndicatorControl(context, "loadingIndicator", null);
        indicator.SetRunning(true);

        // Act
        var isRunning = indicator.IsRunning();

        // Assert
        isRunning.Should().BeTrue();
    }

    [Fact]
    public void AI004_ActivityIndicator_IsRunning_WhenStopped_ReturnsFalse()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);

        var context = new TestableMauiTestContext(mockDriver.Object);
        var indicator = new TestableActivityIndicatorControl(context, "loadingIndicator", null);
        indicator.SetRunning(false);

        // Act
        var isRunning = indicator.IsRunning();

        // Assert
        isRunning.Should().BeFalse();
    }

    #endregion

    #region Wait Tests (AI-005 to AI-006)

    [Fact]
    public void AI005_ActivityIndicator_WaitRunning_WhenRunning_ReturnsTrue()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);

        var context = new TestableMauiTestContext(mockDriver.Object);
        var indicator = new TestableActivityIndicatorControl(context, "loadingIndicator", null);
        indicator.SetRunning(true);

        // Act
        var result = indicator.WaitRunning(true, 1000);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void AI006_ActivityIndicator_WaitRunning_WhenNotRunning_ReturnsFalse()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);

        var context = new TestableMauiTestContext(mockDriver.Object);
        var indicator = new TestableActivityIndicatorControl(context, "loadingIndicator", null);
        indicator.SetRunning(false);

        // Act
        var result = indicator.WaitRunning(true, 100);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Assert Tests (AI-007 to AI-008)

    [Fact]
    public void AI007_ActivityIndicator_AssertRunning_WhenRunning_Passes()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);

        var context = new TestableMauiTestContext(mockDriver.Object);
        var indicator = new TestableActivityIndicatorControl(context, "loadingIndicator", null);
        indicator.SetRunning(true);

        // Act & Assert - should not throw
        indicator.Invoking(i => i.AssertRunning(true)).Should().NotThrow();
    }

    [Fact]
    public void AI008_ActivityIndicator_AssertRunning_WhenMismatch_Throws()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);

        var context = new TestableMauiTestContext(mockDriver.Object);
        var indicator = new TestableActivityIndicatorControl(context, "loadingIndicator", null);
        indicator.SetRunning(false);

        // Act & Assert - should throw
        indicator.Invoking(i => i.AssertRunning(true)).Should().Throw<Exception>();
    }

    #endregion
}
