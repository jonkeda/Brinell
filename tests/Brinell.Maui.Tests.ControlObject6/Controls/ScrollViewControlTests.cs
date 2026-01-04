using Brinell.Core.ControlObject6.Locators;
using Brinell.Maui.Tests.ControlObject6.Mocks;

namespace Brinell.Maui.Tests.ControlObject6.Controls;

/// <summary>
/// Unit tests for TestableScrollViewControl.
/// Tests cover scroll position, scroll actions, and scroll capabilities.
/// Test IDs: SV-001 to SV-012
/// </summary>
public class ScrollViewControlTests
{
    #region Scroll Capabilities (SV-001 to SV-002)

    [Fact]
    public void SV001_CanScrollVertically_WhenEnabled_ReturnsTrue()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableScrollViewControl(context, "mainScrollView");
        control.SetScrollCapabilities(false, true);

        // Act
        var canScroll = control.CanScrollVertically();

        // Assert
        canScroll.Should().BeTrue();
    }

    [Fact]
    public void SV002_CanScrollHorizontally_WhenDisabled_ReturnsFalse()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableScrollViewControl(context, "scrollView");
        control.SetScrollCapabilities(false, true);

        // Act
        var canScroll = control.CanScrollHorizontally();

        // Assert
        canScroll.Should().BeFalse();
    }

    #endregion

    #region Scroll Position (SV-003 to SV-004)

    [Fact]
    public void SV003_GetScrollPosition_ReturnsCurrentPosition()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableScrollViewControl(context, "scrollView");

        // Act
        var (h, v) = control.GetScrollPosition();

        // Assert
        h.Should().Be(0);
        v.Should().Be(0);
    }

    [Fact]
    public void SV004_ScrollTo_UpdatesScrollPosition()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableScrollViewControl(context, "scrollView");

        // Act
        control.ScrollTo(25, 50);

        // Assert
        var (h, v) = control.GetScrollPosition();
        h.Should().Be(25);
        v.Should().Be(50);
    }

    #endregion

    #region Scroll Actions (SV-005 to SV-010)

    [Fact]
    public void SV005_ScrollToTop_SetsVerticalPositionToZero()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableScrollViewControl(context, "scrollView");
        control.ScrollTo(null, 50);

        // Act
        control.ScrollToTop();

        // Assert
        var (_, v) = control.GetScrollPosition();
        v.Should().Be(0);
    }

    [Fact]
    public void SV006_ScrollToBottom_SetsVerticalPositionTo100()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableScrollViewControl(context, "scrollView");

        // Act
        control.ScrollToBottom();

        // Assert
        var (_, v) = control.GetScrollPosition();
        v.Should().Be(100);
    }

    [Fact]
    public void SV007_ScrollToLeft_SetsHorizontalPositionToZero()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableScrollViewControl(context, "scrollView");
        control.ScrollTo(50, null);

        // Act
        control.ScrollToLeft();

        // Assert
        var (h, _) = control.GetScrollPosition();
        h.Should().Be(0);
    }

    [Fact]
    public void SV008_ScrollToRight_SetsHorizontalPositionTo100()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableScrollViewControl(context, "scrollView");

        // Act
        control.ScrollToRight();

        // Assert
        var (h, _) = control.GetScrollPosition();
        h.Should().Be(100);
    }

    [Fact]
    public void SV009_ScrollDown_IncreasesVerticalPosition()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableScrollViewControl(context, "scrollView");
        var (_, initialV) = control.GetScrollPosition();

        // Act
        control.ScrollDown(20);

        // Assert
        var (_, newV) = control.GetScrollPosition();
        newV.Should().BeGreaterThan(initialV);
    }

    [Fact]
    public void SV010_ScrollUp_DecreasesVerticalPosition()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableScrollViewControl(context, "scrollView");
        control.ScrollTo(null, 50);
        var (_, initialV) = control.GetScrollPosition();

        // Act
        control.ScrollUp(20);

        // Assert
        var (_, newV) = control.GetScrollPosition();
        newV.Should().BeLessThan(initialV);
    }

    #endregion

    #region Wait Operations (SV-011 to SV-012)

    [Fact]
    public void SV011_WaitScrollComplete_ReturnsTrue()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableScrollViewControl(context, "scrollView");

        // Act
        var result = control.WaitScrollComplete();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void SV012_ScrollToElement_DoesNotThrow()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableScrollViewControl(context, "scrollView");
        var targetControl = new TestableButtonControl(context, "targetButton");

        // Act & Assert - Should not throw
        var action = () => control.ScrollToElement(targetControl);
        action.Should().NotThrow();
    }

    #endregion
}
