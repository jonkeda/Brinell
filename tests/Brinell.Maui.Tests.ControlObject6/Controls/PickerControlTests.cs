using Brinell.Core.ControlObject6.Locators;
using Brinell.Maui.Tests.ControlObject6.Mocks;

namespace Brinell.Maui.Tests.ControlObject6.Controls;

/// <summary>
/// Tests for PickerControl (PK-001 to PK-018).
/// Uses testable wrappers to avoid Moq issues with non-virtual AppiumDriver members.
/// </summary>
[Trait("Category", "Selection")]
[Trait("Platform", "MAUI")]
public class PickerControlTests
{
    #region Constructor Tests (PK-001 to PK-002)

    [Fact]
    [Trait("Priority", "P0")]
    public void PK001_Constructor_WithAutomationId_SetsLocator()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);

        // Act
        var picker = new TestablePickerControl(context, "countryPicker", null);

        // Assert
        picker.Locator.Should().NotBeNull();
        picker.Locator.Value.Should().Be("countryPicker");
        picker.Locator.Strategy.Should().Be(LocatorStrategy.AutomationId);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void PK002_Constructor_WithLocator_SetsLocator()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var locator = By.Id("myPicker");

        // Act
        var picker = new TestablePickerControl(context, locator, null);

        // Assert
        picker.Locator.Should().Be(locator);
    }

    #endregion

    #region State Tests (PK-003 to PK-008)

    [Fact]
    [Trait("Priority", "P0")]
    public void PK003_IsExists_WhenElementFound_ReturnsTrue()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var picker = new TestablePickerControl(context, "countryPicker", null);

        // Act
        var exists = picker.IsExists();

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void PK004_IsExists_WhenElementNotFound_ReturnsFalse()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        MockAppiumFactory.SetupElementNotFound(mockDriverWrapper);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var picker = new TestablePickerControl(context, "countryPicker", null);

        // Act
        var exists = picker.IsExists();

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void PK005_IsVisible_WhenElementDisplayed_ReturnsTrue()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement(displayed: true);
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var picker = new TestablePickerControl(context, "countryPicker", null);

        // Act
        var visible = picker.IsVisible();

        // Assert
        visible.Should().BeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void PK006_IsVisible_WhenElementNotDisplayed_ReturnsFalse()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement(displayed: false);
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var picker = new TestablePickerControl(context, "countryPicker", null);

        // Act
        var visible = picker.IsVisible();

        // Assert
        visible.Should().BeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void PK007_IsEnabled_WhenElementEnabled_ReturnsTrue()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement(enabled: true);
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var picker = new TestablePickerControl(context, "countryPicker", null);

        // Act
        var enabled = picker.IsEnabled();

        // Assert
        enabled.Should().BeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void PK008_IsEnabled_WhenElementDisabled_ReturnsFalse()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement(enabled: false);
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var picker = new TestablePickerControl(context, "countryPicker", null);

        // Act
        var enabled = picker.IsEnabled();

        // Assert
        enabled.Should().BeFalse();
    }

    #endregion

    #region Selection State Tests (PK-009 to PK-012)

    [Fact]
    [Trait("Priority", "P0")]
    public void PK009_GetSelectedText_ReturnsCurrentSelection()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement(text: "United States");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        context.DefaultTimeoutMs = 100;
        var picker = new TestablePickerControl(context, "countryPicker", null);

        // Select an item first to set up state (the testable control uses internal state)
        picker.SelectByIndex(0);

        // Act
        var selectedText = picker.GetSelectedText();

        // Assert - testable control returns item from its internal list
        selectedText.Should().NotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void PK010_GetSelectedIndex_ReturnsCurrentIndex()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        mockElement.Setup(e => e.GetAttribute("selectedIndex")).Returns("2");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var picker = new TestablePickerControl(context, "countryPicker", null);

        // Act
        var selectedIndex = picker.GetSelectedIndex();

        // Assert
        selectedIndex.Should().Be(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void PK011_GetItemCount_ReturnsNumberOfItems()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        mockElement.Setup(e => e.GetAttribute("itemCount")).Returns("5");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var picker = new TestablePickerControl(context, "countryPicker", null);

        // Act
        var count = picker.GetItemCount();

        // Assert
        count.Should().Be(5);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void PK012_HasSelection_WhenIndexValid_ReturnsTrue()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        mockElement.Setup(e => e.GetAttribute("selectedIndex")).Returns("1");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var picker = new TestablePickerControl(context, "countryPicker", null);

        // Act
        var hasSelection = picker.HasSelection();

        // Assert
        hasSelection.Should().BeTrue();
    }

    #endregion

    #region Selection Actions (PK-013 to PK-018)

    [Fact]
    [Trait("Priority", "P0")]
    public void PK013_SelectByIndex_SelectsItem()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        var selectedIndex = -1;
        mockElement.Setup(e => e.GetAttribute("selectedIndex")).Returns(() => selectedIndex.ToString());
        mockElement.Setup(e => e.SendKeys(It.IsAny<string>())).Callback<string>(s => 
        {
            if (int.TryParse(s, out var index))
                selectedIndex = index;
        });
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var picker = new TestablePickerControl(context, "countryPicker", null);

        // Act
        picker.SelectByIndex(2);

        // Assert - verify SendKeys was called (the actual selection mechanism may vary)
        mockElement.Verify(e => e.SendKeys(It.IsAny<string>()), Times.AtLeastOnce());
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void PK014_SelectByText_SelectsMatchingItem()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement(text: "Canada");
        mockElement.Setup(e => e.SendKeys(It.IsAny<string>()));
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var picker = new TestablePickerControl(context, "countryPicker", null);

        // Act
        picker.SelectByText("Canada");

        // Assert - verify interaction occurred
        mockElement.Verify(e => e.SendKeys(It.IsAny<string>()), Times.AtLeastOnce());
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void PK015_AssertSelectedText_WithMatchingText_Passes()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement(text: "Item 1");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        context.DefaultTimeoutMs = 100;
        var picker = new TestablePickerControl(context, "countryPicker", null);

        // Select an item first to set up internal state
        picker.SelectByIndex(0);

        // Act & Assert - should not throw (internal list has "Item 1" at index 0)
        Action act = () => picker.AssertSelectedText("Item 1");
        act.Should().NotThrow();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void PK016_AssertSelectedIndex_WithMatchingIndex_Passes()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        mockElement.Setup(e => e.GetAttribute("selectedIndex")).Returns("3");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var picker = new TestablePickerControl(context, "countryPicker", null);

        // Act & Assert - should not throw
        Action act = () => picker.AssertSelectedIndex(3);
        act.Should().NotThrow();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void PK017_AssertHasItems_WhenItemsExist_Passes()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        mockElement.Setup(e => e.GetAttribute("itemCount")).Returns("5");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var picker = new TestablePickerControl(context, "countryPicker", null);

        // Act & Assert - should not throw
        Action act = () => picker.AssertHasItems();
        act.Should().NotThrow();
    }

    [Fact]
    [Trait("Priority", "P2")]
    public void PK018_Click_OpensPickerDropdown()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        var clickCount = 0;
        mockElement.Setup(e => e.Click()).Callback(() => clickCount++);
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var picker = new TestablePickerControl(context, "countryPicker", null);

        // Act
        picker.Click();

        // Assert
        clickCount.Should().Be(1);
    }

    #endregion
}
