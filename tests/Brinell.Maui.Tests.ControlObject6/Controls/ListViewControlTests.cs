using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Maui.Tests.ControlObject6.Mocks;

namespace Brinell.Maui.Tests.ControlObject6.Controls;

/// <summary>
/// Tests for ListViewControl (LV-001 to LV-020).
/// Uses testable wrappers to avoid Moq issues with non-virtual AppiumDriver members.
/// </summary>
[Trait("Category", "Collection")]
[Trait("Platform", "MAUI")]
public class ListViewControlTests
{
    #region Constructor Tests (LV-001 to LV-002)

    [Fact]
    [Trait("Priority", "P0")]
    public void LV001_Constructor_WithAutomationId_SetsLocator()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);

        // Act
        var listView = new TestableListViewControl(context, "itemsList", null);

        // Assert
        listView.Locator.Should().NotBeNull();
        listView.Locator.Value.Should().Be("itemsList");
        listView.Locator.Strategy.Should().Be(LocatorStrategy.AutomationId);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void LV002_Constructor_WithLocator_SetsLocator()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var locator = By.Id("myListView");

        // Act
        var listView = new TestableListViewControl(context, locator, null);

        // Assert
        listView.Locator.Should().Be(locator);
    }

    #endregion

    #region State Tests (LV-003 to LV-008)

    [Fact]
    [Trait("Priority", "P0")]
    public void LV003_IsExists_WhenElementFound_ReturnsTrue()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var listView = new TestableListViewControl(context, "itemsList", null);

        // Act
        var exists = listView.IsExists();

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void LV004_IsExists_WhenElementNotFound_ReturnsFalse()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        MockAppiumFactory.SetupElementNotFound(mockDriverWrapper);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var listView = new TestableListViewControl(context, "itemsList", null);

        // Act
        var exists = listView.IsExists();

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void LV005_IsVisible_WhenElementDisplayed_ReturnsTrue()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement(displayed: true);
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var listView = new TestableListViewControl(context, "itemsList", null);

        // Act
        var visible = listView.IsVisible();

        // Assert
        visible.Should().BeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void LV006_IsVisible_WhenElementNotDisplayed_ReturnsFalse()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement(displayed: false);
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var listView = new TestableListViewControl(context, "itemsList", null);

        // Act
        var visible = listView.IsVisible();

        // Assert
        visible.Should().BeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void LV007_IsEnabled_WhenElementEnabled_ReturnsTrue()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement(enabled: true);
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var listView = new TestableListViewControl(context, "itemsList", null);

        // Act
        var enabled = listView.IsEnabled();

        // Assert
        enabled.Should().BeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void LV008_IsEnabled_WhenElementDisabled_ReturnsFalse()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement(enabled: false);
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var listView = new TestableListViewControl(context, "itemsList", null);

        // Act
        var enabled = listView.IsEnabled();

        // Assert
        enabled.Should().BeFalse();
    }

    #endregion

    #region Item Count Tests (LV-009 to LV-012)

    [Fact]
    [Trait("Priority", "P0")]
    public void LV009_GetItemCount_ReturnsNumberOfItems()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        
        // Setup child elements for item count
        var mockItems = new List<Mock<OpenQA.Selenium.IWebElement>>();
        for (int i = 0; i < 5; i++)
        {
            mockItems.Add(MockAppiumFactory.CreateMockElement());
        }
        mockElement.Setup(e => e.FindElements(It.IsAny<OpenQA.Selenium.By>()))
            .Returns(mockItems.Select(m => m.Object).ToList().AsReadOnly());
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var listView = new TestableListViewControl(context, "itemsList", null);

        // Act
        var count = listView.GetItemCount();

        // Assert
        count.Should().Be(5);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void LV010_IsEmpty_WhenNoItems_ReturnsTrue()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        mockElement.Setup(e => e.FindElements(It.IsAny<OpenQA.Selenium.By>()))
            .Returns(new List<OpenQA.Selenium.IWebElement>().AsReadOnly());
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var listView = new TestableListViewControl(context, "itemsList", null);

        // Act
        var isEmpty = listView.IsEmpty();

        // Assert
        isEmpty.Should().BeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void LV011_IsEmpty_WhenHasItems_ReturnsFalse()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        var mockItem = MockAppiumFactory.CreateMockElement();
        mockElement.Setup(e => e.FindElements(It.IsAny<OpenQA.Selenium.By>()))
            .Returns(new List<OpenQA.Selenium.IWebElement> { mockItem.Object }.AsReadOnly());
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var listView = new TestableListViewControl(context, "itemsList", null);

        // Act
        var isEmpty = listView.IsEmpty();

        // Assert
        isEmpty.Should().BeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void LV012_AssertItemCount_WithMatchingCount_Passes()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        var mockItems = new List<Mock<OpenQA.Selenium.IWebElement>>();
        for (int i = 0; i < 3; i++)
        {
            mockItems.Add(MockAppiumFactory.CreateMockElement());
        }
        mockElement.Setup(e => e.FindElements(It.IsAny<OpenQA.Selenium.By>()))
            .Returns(mockItems.Select(m => m.Object).ToList().AsReadOnly());
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var listView = new TestableListViewControl(context, "itemsList", null);

        // Act & Assert - should not throw
        Action act = () => listView.AssertItemCount(3);
        act.Should().NotThrow();
    }

    #endregion

    #region Selection Tests (LV-013 to LV-016)

    [Fact]
    [Trait("Priority", "P0")]
    public void LV013_SelectItemByIndex_SelectsCorrectItem()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        var clickedIndex = -1;
        
        var mockItems = new List<Mock<OpenQA.Selenium.IWebElement>>();
        for (int i = 0; i < 5; i++)
        {
            var mockItem = MockAppiumFactory.CreateMockElement();
            int itemIndex = i;
            mockItem.Setup(e => e.Click()).Callback(() => clickedIndex = itemIndex);
            mockItems.Add(mockItem);
        }
        mockElement.Setup(e => e.FindElements(It.IsAny<OpenQA.Selenium.By>()))
            .Returns(mockItems.Select(m => m.Object).ToList().AsReadOnly());
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var listView = new TestableListViewControl(context, "itemsList", null);

        // Act
        listView.SelectItemByIndex(2);

        // Assert
        clickedIndex.Should().Be(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void LV014_SelectItemByText_SelectsMatchingItem()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        var clickedItemText = "";
        
        var itemTexts = new[] { "Apple", "Banana", "Cherry" };
        var mockItems = new List<Mock<OpenQA.Selenium.IWebElement>>();
        for (int i = 0; i < itemTexts.Length; i++)
        {
            var mockItem = MockAppiumFactory.CreateMockElement(text: itemTexts[i]);
            string text = itemTexts[i];
            mockItem.Setup(e => e.Click()).Callback(() => clickedItemText = text);
            mockItems.Add(mockItem);
        }
        mockElement.Setup(e => e.FindElements(It.IsAny<OpenQA.Selenium.By>()))
            .Returns(mockItems.Select(m => m.Object).ToList().AsReadOnly());
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var listView = new TestableListViewControl(context, "itemsList", null);

        // Act
        listView.SelectItemByText("Banana");

        // Assert
        clickedItemText.Should().Be("Banana");
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void LV015_GetSelectedItem_ReturnsSelectedItemText()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        context.DefaultTimeoutMs = 100;
        var listView = new TestableListViewControl(context, "itemsList", null);

        // Select an item first
        listView.SelectItem(0);

        // Act - use the interface method from the base class
        var selectedText = ((ISelectableItemsControlObject)listView).GetSelectedItemText();

        // Assert - should return the item at index 0 from the internal list
        selectedText.Should().NotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void LV016_HasSelectedItem_WhenItemSelected_ReturnsTrue()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        var mockSelectedItem = MockAppiumFactory.CreateMockElement();
        mockSelectedItem.Setup(e => e.GetAttribute("selected")).Returns("true");
        
        mockElement.Setup(e => e.FindElements(It.IsAny<OpenQA.Selenium.By>()))
            .Returns(new List<OpenQA.Selenium.IWebElement> { mockSelectedItem.Object }.AsReadOnly());
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var listView = new TestableListViewControl(context, "itemsList", null);

        // Act
        var hasSelected = listView.HasSelectedItem();

        // Assert
        hasSelected.Should().BeTrue();
    }

    #endregion

    #region Scroll Tests (LV-017 to LV-020)

    [Fact]
    [Trait("Priority", "P1")]
    public void LV017_ScrollToItem_ByIndex_ScrollsToCorrectItem()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        var mockItems = new List<Mock<OpenQA.Selenium.IWebElement>>();
        for (int i = 0; i < 10; i++)
        {
            mockItems.Add(MockAppiumFactory.CreateMockElement());
        }
        mockElement.Setup(e => e.FindElements(It.IsAny<OpenQA.Selenium.By>()))
            .Returns(mockItems.Select(m => m.Object).ToList().AsReadOnly());
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var listView = new TestableListViewControl(context, "itemsList", null);

        // Act - should not throw
        Action act = () => listView.ScrollToItemByIndex(5);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void LV018_ScrollToTop_ScrollsToFirstItem()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var listView = new TestableListViewControl(context, "itemsList", null);

        // Act - should not throw
        Action act = () => listView.ScrollToTop();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void LV019_ScrollToBottom_ScrollsToLastItem()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var listView = new TestableListViewControl(context, "itemsList", null);

        // Act - should not throw
        Action act = () => listView.ScrollToBottom();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    [Trait("Priority", "P2")]
    public void LV020_GetItemAtIndex_ReturnsItemText()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        var mockItems = new[]
        {
            MockAppiumFactory.CreateMockElement(text: "First"),
            MockAppiumFactory.CreateMockElement(text: "Second"),
            MockAppiumFactory.CreateMockElement(text: "Third")
        };
        mockElement.Setup(e => e.FindElements(It.IsAny<OpenQA.Selenium.By>()))
            .Returns(mockItems.Select(m => m.Object).ToList().AsReadOnly());
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        context.DefaultTimeoutMs = 100;
        var listView = new TestableListViewControl(context, "itemsList", null);

        // Act
        var itemText = listView.GetItemTextAtIndex(1);

        // Assert
        itemText.Should().Be("Second");
    }

    #endregion
}
