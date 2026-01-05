using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;
using Brinell.Maui.Tests.ControlObject6.Mocks;
using FluentAssertions;
using Moq;
using Xunit;

namespace Brinell.Maui.Tests.ControlObject6.Controls;

/// <summary>
/// Extended unit tests for container controls (Border, Frame, ContentView).
/// Test IDs: BD-001 to BD-010, FR-001 to FR-010, CV-001 to CV-010
/// </summary>
public class ContainerControlExtendedTests
{
    #region BorderControl Tests

    [Fact(DisplayName = "BD-001: BorderControl constructor with AutomationId sets Locator")]
    public void BorderControl_Constructor_WithAutomationId_SetsLocator()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var automationId = "TestBorder";

        // Act
        var control = new TestableBorderControl(context, automationId);

        // Assert
        control.Locator.Should().NotBeNull();
        control.Locator.Value.Should().Be(automationId);
        control.Locator.Strategy.Should().Be(LocatorStrategy.AutomationId);
    }

    [Fact(DisplayName = "BD-002: BorderControl constructor with Locator sets Locator")]
    public void BorderControl_Constructor_WithLocator_SetsLocator()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var locator = By.AutomationId("TestBorder");

        // Act
        var control = new TestableBorderControl(context, locator);

        // Assert
        control.Locator.Should().BeSameAs(locator);
    }

    [Fact(DisplayName = "BD-003: BorderControl GetStrokeColor returns color")]
    public void BorderControl_GetStrokeColor_ReturnsColor()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("border");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableBorderControl(context, "TestBorder");

        // Act
        var color = control.GetStrokeColor();

        // Assert
        color.Should().NotBeNullOrEmpty();
    }

    [Fact(DisplayName = "BD-004: BorderControl GetStrokeThickness returns thickness")]
    public void BorderControl_GetStrokeThickness_ReturnsThickness()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("border");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableBorderControl(context, "TestBorder");

        // Act
        var thickness = control.GetStrokeThickness();

        // Assert
        thickness.Should().BeGreaterOrEqualTo(0);
    }

    [Fact(DisplayName = "BD-005: BorderControl IsExists returns true when element exists")]
    public void BorderControl_IsExists_WhenElementExists_ReturnsTrue()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("border");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableBorderControl(context, "TestBorder");

        // Act
        var exists = control.IsExists();

        // Assert
        exists.Should().BeTrue();
    }

    [Fact(DisplayName = "BD-006: BorderControl GetChildCount returns child count")]
    public void BorderControl_GetChildCount_ReturnsChildCount()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("border");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableBorderControl(context, "TestBorder");
        control.SetChildCount(3);

        // Act
        var count = control.GetChildCount();

        // Assert
        count.Should().Be(3);
    }

    [Fact(DisplayName = "BD-007: BorderControl AssertChildCount passes when count matches")]
    public void BorderControl_AssertChildCount_WhenMatches_Passes()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("border");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableBorderControl(context, "TestBorder");
        control.SetChildCount(2);

        // Act & Assert
        var action = () => control.AssertChildCount(2);
        action.Should().NotThrow();
    }

    [Fact(DisplayName = "BD-008: BorderControl AssertChildCount throws when count mismatches")]
    public void BorderControl_AssertChildCount_WhenMismatch_Throws()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("border");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableBorderControl(context, "TestBorder");
        control.SetChildCount(2);

        // Act & Assert
        var action = () => control.AssertChildCount(5);
        action.Should().Throw<AssertionException>();
    }

    #endregion

    #region FrameControl Tests

    [Fact(DisplayName = "FR-001: FrameControl constructor with AutomationId sets Locator")]
    public void FrameControl_Constructor_WithAutomationId_SetsLocator()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var automationId = "TestFrame";

        // Act
        var control = new TestableFrameControl(context, automationId);

        // Assert
        control.Locator.Should().NotBeNull();
        control.Locator.Value.Should().Be(automationId);
    }

    [Fact(DisplayName = "FR-002: FrameControl GetBorderColor returns color")]
    public void FrameControl_GetBorderColor_ReturnsColor()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("frame");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableFrameControl(context, "TestFrame");

        // Act
        var color = control.GetBorderColor();

        // Assert
        color.Should().NotBeNullOrEmpty();
    }

    [Fact(DisplayName = "FR-003: FrameControl GetCornerRadius returns radius")]
    public void FrameControl_GetCornerRadius_ReturnsRadius()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("frame");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableFrameControl(context, "TestFrame");

        // Act
        var radius = control.GetCornerRadius();

        // Assert
        radius.Should().BeGreaterOrEqualTo(0);
    }

    [Fact(DisplayName = "FR-004: FrameControl HasShadow returns shadow state")]
    public void FrameControl_HasShadow_ReturnsShadowState()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("frame");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableFrameControl(context, "TestFrame");

        // Act
        var hasShadow = control.HasShadow();

        // Assert
        hasShadow.Should().BeTrue();
    }

    [Fact(DisplayName = "FR-005: FrameControl IsVisible returns true when visible")]
    public void FrameControl_IsVisible_WhenVisible_ReturnsTrue()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("frame");
        mockElement.Setup(e => e.Displayed).Returns(true);
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableFrameControl(context, "TestFrame");

        // Act
        var visible = control.IsVisible();

        // Assert
        visible.Should().BeTrue();
    }

    [Fact(DisplayName = "FR-006: FrameControl GetChildCount returns child count")]
    public void FrameControl_GetChildCount_ReturnsChildCount()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("frame");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableFrameControl(context, "TestFrame");
        control.SetChildCount(1);

        // Act
        var count = control.GetChildCount();

        // Assert
        count.Should().Be(1);
    }

    #endregion

    #region ContentViewControl Tests

    [Fact(DisplayName = "CV-001: ContentViewControl constructor with AutomationId sets Locator")]
    public void ContentViewControl_Constructor_WithAutomationId_SetsLocator()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var automationId = "TestContentView";

        // Act
        var control = new TestableContentViewControl(context, automationId);

        // Assert
        control.Locator.Should().NotBeNull();
        control.Locator.Value.Should().Be(automationId);
    }

    [Fact(DisplayName = "CV-002: ContentViewControl HasContent returns true when has content")]
    public void ContentViewControl_HasContent_WhenHasContent_ReturnsTrue()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("contentview");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableContentViewControl(context, "TestContentView");
        control.SetHasContent(true);

        // Act
        var hasContent = control.HasContent();

        // Assert
        hasContent.Should().BeTrue();
    }

    [Fact(DisplayName = "CV-003: ContentViewControl HasContent returns false when empty")]
    public void ContentViewControl_HasContent_WhenEmpty_ReturnsFalse()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("contentview");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableContentViewControl(context, "TestContentView");
        control.SetHasContent(false);

        // Act
        var hasContent = control.HasContent();

        // Assert
        hasContent.Should().BeFalse();
    }

    [Fact(DisplayName = "CV-004: ContentViewControl AssertHasContent passes when has content")]
    public void ContentViewControl_AssertHasContent_WhenHasContent_Passes()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("contentview");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableContentViewControl(context, "TestContentView");
        control.SetHasContent(true);

        // Act & Assert
        var action = () => control.AssertHasContent();
        action.Should().NotThrow();
    }

    [Fact(DisplayName = "CV-005: ContentViewControl AssertHasContent throws when empty")]
    public void ContentViewControl_AssertHasContent_WhenEmpty_Throws()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("contentview");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableContentViewControl(context, "TestContentView");
        control.SetHasContent(false);

        // Act & Assert
        var action = () => control.AssertHasContent();
        action.Should().Throw<AssertionException>();
    }

    [Fact(DisplayName = "CV-006: ContentViewControl IsExists returns true when exists")]
    public void ContentViewControl_IsExists_WhenExists_ReturnsTrue()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("contentview");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableContentViewControl(context, "TestContentView");

        // Act
        var exists = control.IsExists();

        // Assert
        exists.Should().BeTrue();
    }

    [Fact(DisplayName = "CV-007: ContentViewControl GetChildCount returns count")]
    public void ContentViewControl_GetChildCount_ReturnsCount()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("contentview");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableContentViewControl(context, "TestContentView");
        control.SetChildCount(4);

        // Act
        var count = control.GetChildCount();

        // Assert
        count.Should().Be(4);
    }

    #endregion
}
