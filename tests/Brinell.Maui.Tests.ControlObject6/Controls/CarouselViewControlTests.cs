using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;
using Brinell.Maui.Tests.ControlObject6.Mocks;
using FluentAssertions;
using Moq;
using Xunit;

namespace Brinell.Maui.Tests.ControlObject6.Controls;

/// <summary>
/// Unit tests for CarouselViewControl.
/// Test IDs: CA-001 to CA-012
/// </summary>
public class CarouselViewControlTests
{
    [Fact(DisplayName = "CA-001: Constructor with AutomationId sets Locator")]
    public void Constructor_WithAutomationId_SetsLocator()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var automationId = "TestCarousel";

        // Act
        var control = new TestableCarouselViewControl(context, automationId);

        // Assert
        control.Locator.Should().NotBeNull();
        control.Locator.Value.Should().Be(automationId);
        control.Locator.Strategy.Should().Be(LocatorStrategy.AutomationId);
    }

    [Fact(DisplayName = "CA-002: Constructor with Locator sets Locator")]
    public void Constructor_WithLocator_SetsLocator()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var locator = By.AutomationId("TestCarousel");

        // Act
        var control = new TestableCarouselViewControl(context, locator);

        // Assert
        control.Locator.Should().BeSameAs(locator);
    }

    [Fact(DisplayName = "CA-003: GetCurrentPosition returns current position")]
    public void GetCurrentPosition_ReturnsCurrentPosition()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("carousel");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableCarouselViewControl(context, "TestCarousel");
        control.SetCurrentPosition(2);

        // Act
        var position = control.GetCurrentPosition();

        // Assert
        position.Should().Be(2);
    }

    [Fact(DisplayName = "CA-004: SwipeNext moves to next position")]
    public void SwipeNext_MovesToNextPosition()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("carousel");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableCarouselViewControl(context, "TestCarousel");
        control.SetCurrentPosition(0);

        // Act
        control.SwipeNext();

        // Assert
        control.GetCurrentPosition().Should().Be(1);
    }

    [Fact(DisplayName = "CA-005: SwipePrevious moves to previous position")]
    public void SwipePrevious_MovesToPreviousPosition()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("carousel");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableCarouselViewControl(context, "TestCarousel");
        control.SetCurrentPosition(2);

        // Act
        control.SwipePrevious();

        // Assert
        control.GetCurrentPosition().Should().Be(1);
    }

    [Fact(DisplayName = "CA-006: GoToPosition navigates to specific position")]
    public void GoToPosition_NavigatesToSpecificPosition()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("carousel");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableCarouselViewControl(context, "TestCarousel");

        // Act
        control.GoToPosition(3);

        // Assert
        control.GetCurrentPosition().Should().Be(3);
    }

    [Fact(DisplayName = "CA-007: GoToPosition throws for invalid position")]
    public void GoToPosition_WithInvalidPosition_Throws()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("carousel");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableCarouselViewControl(context, "TestCarousel");

        // Act & Assert
        var action = () => control.GoToPosition(100);
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "CA-008: IsAtStart returns true when at first position")]
    public void IsAtStart_WhenAtFirstPosition_ReturnsTrue()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("carousel");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableCarouselViewControl(context, "TestCarousel");
        control.SetCurrentPosition(0);

        // Act
        var isAtStart = control.IsAtStart();

        // Assert
        isAtStart.Should().BeTrue();
    }

    [Fact(DisplayName = "CA-009: IsAtEnd returns true when at last position")]
    public void IsAtEnd_WhenAtLastPosition_ReturnsTrue()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("carousel");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableCarouselViewControl(context, "TestCarousel");
        control.SetCurrentPosition(4); // 5 items (0-4)

        // Act
        var isAtEnd = control.IsAtEnd();

        // Assert
        isAtEnd.Should().BeTrue();
    }

    [Fact(DisplayName = "CA-010: AssertPosition passes when position matches")]
    public void AssertPosition_WhenMatches_Passes()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("carousel");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableCarouselViewControl(context, "TestCarousel");
        control.SetCurrentPosition(2);

        // Act & Assert
        var action = () => control.AssertPosition(2);
        action.Should().NotThrow();
    }

    [Fact(DisplayName = "CA-011: AssertPosition throws when position mismatches")]
    public void AssertPosition_WhenMismatch_Throws()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("carousel");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableCarouselViewControl(context, "TestCarousel");
        control.SetCurrentPosition(2);

        // Act & Assert
        var action = () => control.AssertPosition(5);
        action.Should().Throw<AssertionException>();
    }

    [Fact(DisplayName = "CA-012: GetItemCount returns item count")]
    public void GetItemCount_ReturnsItemCount()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("carousel");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableCarouselViewControl(context, "TestCarousel");

        // Act
        var count = control.GetItemCount();

        // Assert
        count.Should().Be(5); // Default items count from TestableItemsControlBase
    }
}
