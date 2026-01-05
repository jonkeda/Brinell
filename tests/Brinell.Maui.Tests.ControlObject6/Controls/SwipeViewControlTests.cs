using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;
using Brinell.Maui.Tests.ControlObject6.Mocks;
using FluentAssertions;
using Moq;
using Xunit;

namespace Brinell.Maui.Tests.ControlObject6.Controls;

/// <summary>
/// Unit tests for SwipeViewControl.
/// Test IDs: SW-001 to SW-012
/// </summary>
public class SwipeViewControlTests
{
    [Fact(DisplayName = "SW-001: Constructor with AutomationId sets Locator")]
    public void Constructor_WithAutomationId_SetsLocator()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var automationId = "TestSwipeView";

        // Act
        var control = new TestableSwipeViewControl(context, automationId);

        // Assert
        control.Locator.Should().NotBeNull();
        control.Locator.Value.Should().Be(automationId);
        control.Locator.Strategy.Should().Be(LocatorStrategy.AutomationId);
    }

    [Fact(DisplayName = "SW-002: Constructor with Locator sets Locator")]
    public void Constructor_WithLocator_SetsLocator()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var locator = By.AutomationId("TestSwipeView");

        // Act
        var control = new TestableSwipeViewControl(context, locator);

        // Assert
        control.Locator.Should().BeSameAs(locator);
    }

    [Fact(DisplayName = "SW-003: SwipeLeft reveals right actions")]
    public void SwipeLeft_RevealsRightActions()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("swipeview");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableSwipeViewControl(context, "TestSwipeView");

        // Act
        control.SwipeLeft();

        // Assert
        control.IsRightSwipeOpen().Should().BeTrue();
        control.IsLeftSwipeOpen().Should().BeFalse();
    }

    [Fact(DisplayName = "SW-004: SwipeRight reveals left actions")]
    public void SwipeRight_RevealsLeftActions()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("swipeview");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableSwipeViewControl(context, "TestSwipeView");

        // Act
        control.SwipeRight();

        // Assert
        control.IsLeftSwipeOpen().Should().BeTrue();
        control.IsRightSwipeOpen().Should().BeFalse();
    }

    [Fact(DisplayName = "SW-005: CloseSwipe closes all swipe actions")]
    public void CloseSwipe_ClosesAllSwipeActions()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("swipeview");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableSwipeViewControl(context, "TestSwipeView");
        control.SwipeLeft();

        // Act
        control.CloseSwipe();

        // Assert
        control.IsLeftSwipeOpen().Should().BeFalse();
        control.IsRightSwipeOpen().Should().BeFalse();
    }

    [Fact(DisplayName = "SW-006: IsLeftSwipeOpen returns false initially")]
    public void IsLeftSwipeOpen_Initially_ReturnsFalse()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableSwipeViewControl(context, "TestSwipeView");

        // Act
        var isOpen = control.IsLeftSwipeOpen();

        // Assert
        isOpen.Should().BeFalse();
    }

    [Fact(DisplayName = "SW-007: IsRightSwipeOpen returns false initially")]
    public void IsRightSwipeOpen_Initially_ReturnsFalse()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableSwipeViewControl(context, "TestSwipeView");

        // Act
        var isOpen = control.IsRightSwipeOpen();

        // Assert
        isOpen.Should().BeFalse();
    }

    [Fact(DisplayName = "SW-008: AssertLeftSwipeOpen passes when open")]
    public void AssertLeftSwipeOpen_WhenOpen_Passes()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("swipeview");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableSwipeViewControl(context, "TestSwipeView");
        control.SwipeRight();

        // Act & Assert
        var action = () => control.AssertLeftSwipeOpen();
        action.Should().NotThrow();
    }

    [Fact(DisplayName = "SW-009: AssertLeftSwipeOpen throws when closed")]
    public void AssertLeftSwipeOpen_WhenClosed_Throws()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableSwipeViewControl(context, "TestSwipeView");

        // Act & Assert
        var action = () => control.AssertLeftSwipeOpen();
        action.Should().Throw<AssertionException>();
    }

    [Fact(DisplayName = "SW-010: AssertRightSwipeOpen passes when open")]
    public void AssertRightSwipeOpen_WhenOpen_Passes()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("swipeview");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableSwipeViewControl(context, "TestSwipeView");
        control.SwipeLeft();

        // Act & Assert
        var action = () => control.AssertRightSwipeOpen();
        action.Should().NotThrow();
    }

    [Fact(DisplayName = "SW-011: AssertRightSwipeOpen throws when closed")]
    public void AssertRightSwipeOpen_WhenClosed_Throws()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableSwipeViewControl(context, "TestSwipeView");

        // Act & Assert
        var action = () => control.AssertRightSwipeOpen();
        action.Should().Throw<AssertionException>();
    }

    [Fact(DisplayName = "SW-012: IsExists returns true when element exists")]
    public void IsExists_WhenElementExists_ReturnsTrue()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("swipeview");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableSwipeViewControl(context, "TestSwipeView");

        // Act
        var exists = control.IsExists();

        // Assert
        exists.Should().BeTrue();
    }
}
