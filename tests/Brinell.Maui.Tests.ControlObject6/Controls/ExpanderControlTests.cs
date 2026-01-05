using Brinell.Core.ControlObject6.Locators;
using Brinell.Maui.Tests.ControlObject6.Mocks;

namespace Brinell.Maui.Tests.ControlObject6.Controls;

/// <summary>
/// Tests for ExpanderControl (EX-001 to EX-008).
/// </summary>
[Trait("Category", "Container")]
[Trait("Platform", "MAUI")]
[Trait("Priority", "P2")]
public class ExpanderControlTests
{
    #region Constructor Tests (EX-001 to EX-002)

    [Fact]
    public void EX001_Constructor_WithAutomationId_SetsLocator()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        // Act
        var expander = new TestableExpanderControl(context, "expanderSettings", null);

        // Assert
        expander.Locator.Should().NotBeNull();
        expander.Locator.Value.Should().Be("expanderSettings");
        expander.Locator.Strategy.Should().Be(LocatorStrategy.AutomationId);
    }

    [Fact]
    public void EX002_Constructor_WithLocator_SetsLocator()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);
        var locator = By.AutomationId("myExpander");

        // Act
        var expander = new TestableExpanderControl(context, locator, null);

        // Assert
        expander.Locator.Should().Be(locator);
    }

    #endregion

    #region State Tests (EX-003 to EX-004)

    [Fact]
    public void EX003_IsExpanded_WhenCollapsed_ReturnsFalse()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);

        var context = new TestableMauiTestContext(mockDriver.Object);
        var expander = new TestableExpanderControl(context, "expanderSettings", null);
        expander.SetExpanded(false);

        // Act
        var isExpanded = expander.IsExpanded();

        // Assert
        isExpanded.Should().BeFalse();
    }

    [Fact]
    public void EX004_IsExpanded_WhenExpanded_ReturnsTrue()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);

        var context = new TestableMauiTestContext(mockDriver.Object);
        var expander = new TestableExpanderControl(context, "expanderSettings", null);
        expander.SetExpanded(true);

        // Act
        var isExpanded = expander.IsExpanded();

        // Assert
        isExpanded.Should().BeTrue();
    }

    #endregion

    #region Action Tests (EX-005 to EX-007)

    [Fact]
    public void EX005_Expand_SetsExpandedToTrue()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);

        var context = new TestableMauiTestContext(mockDriver.Object);
        var expander = new TestableExpanderControl(context, "expanderSettings", null);
        expander.SetExpanded(false);

        // Act
        expander.Expand();

        // Assert
        expander.IsExpanded().Should().BeTrue();
    }

    [Fact]
    public void EX006_Collapse_SetsExpandedToFalse()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);

        var context = new TestableMauiTestContext(mockDriver.Object);
        var expander = new TestableExpanderControl(context, "expanderSettings", null);
        expander.SetExpanded(true);

        // Act
        expander.Collapse();

        // Assert
        expander.IsExpanded().Should().BeFalse();
    }

    [Fact]
    public void EX007_Toggle_TogglesExpandedState()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);

        var context = new TestableMauiTestContext(mockDriver.Object);
        var expander = new TestableExpanderControl(context, "expanderSettings", null);
        expander.SetExpanded(false);

        // Act - Toggle to expand
        expander.Toggle();
        var expandedAfterFirst = expander.IsExpanded();

        // Act - Toggle to collapse
        expander.Toggle();
        var expandedAfterSecond = expander.IsExpanded();

        // Assert
        expandedAfterFirst.Should().BeTrue();
        expandedAfterSecond.Should().BeFalse();
    }

    #endregion

    #region Header Tests (EX-008)

    [Fact]
    public void EX008_GetHeaderText_ReturnsHeaderText()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);

        var context = new TestableMauiTestContext(mockDriver.Object);
        var expander = new TestableExpanderControl(context, "expanderSettings", null);
        expander.SetHeaderText("Advanced Settings");

        // Act
        var headerText = expander.GetHeaderText();

        // Assert
        headerText.Should().Be("Advanced Settings");
    }

    #endregion
}
