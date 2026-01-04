using Brinell.Core.ControlObject6.Locators;
using Brinell.Maui.Tests.ControlObject6.Mocks;

namespace Brinell.Maui.Tests.ControlObject6.Controls;

/// <summary>
/// Tests for CollectionViewControl (CV-001 to CV-020).
/// Uses testable wrappers to avoid Moq issues with non-virtual AppiumDriver members.
/// </summary>
[Trait("Category", "Collection")]
[Trait("Platform", "MAUI")]
public class CollectionViewControlTests
{
    #region Constructor Tests (CV-001 to CV-002)

    [Fact]
    [Trait("Priority", "P0")]
    public void CV001_Constructor_WithAutomationId_SetsLocator()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);

        // Act
        var collectionView = new TestableCollectionViewControl(context, "productsGrid", null);

        // Assert
        collectionView.Locator.Should().NotBeNull();
        collectionView.Locator.Value.Should().Be("productsGrid");
        collectionView.Locator.Strategy.Should().Be(LocatorStrategy.AutomationId);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void CV002_Constructor_WithLocator_SetsLocator()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var locator = By.Id("myCollectionView");

        // Act
        var collectionView = new TestableCollectionViewControl(context, locator, null);

        // Assert
        collectionView.Locator.Should().Be(locator);
    }

    #endregion

    #region State Tests (CV-003 to CV-008)

    [Fact]
    [Trait("Priority", "P0")]
    public void CV003_IsExists_WhenElementFound_ReturnsTrue()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var collectionView = new TestableCollectionViewControl(context, "productsGrid", null);

        // Act
        var exists = collectionView.IsExists();

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void CV004_IsExists_WhenElementNotFound_ReturnsFalse()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        MockAppiumFactory.SetupElementNotFound(mockDriverWrapper);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var collectionView = new TestableCollectionViewControl(context, "productsGrid", null);

        // Act
        var exists = collectionView.IsExists();

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void CV005_IsVisible_WhenElementDisplayed_ReturnsTrue()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement(displayed: true);
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var collectionView = new TestableCollectionViewControl(context, "productsGrid", null);

        // Act
        var visible = collectionView.IsVisible();

        // Assert
        visible.Should().BeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void CV006_IsVisible_WhenElementNotDisplayed_ReturnsFalse()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement(displayed: false);
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var collectionView = new TestableCollectionViewControl(context, "productsGrid", null);

        // Act
        var visible = collectionView.IsVisible();

        // Assert
        visible.Should().BeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void CV007_IsEnabled_WhenElementEnabled_ReturnsTrue()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement(enabled: true);
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var collectionView = new TestableCollectionViewControl(context, "productsGrid", null);

        // Act
        var enabled = collectionView.IsEnabled();

        // Assert
        enabled.Should().BeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void CV008_IsEnabled_WhenElementDisabled_ReturnsFalse()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement(enabled: false);
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var collectionView = new TestableCollectionViewControl(context, "productsGrid", null);

        // Act
        var enabled = collectionView.IsEnabled();

        // Assert
        enabled.Should().BeFalse();
    }

    #endregion

    #region Item Count Tests (CV-009 to CV-012)

    [Fact]
    [Trait("Priority", "P0")]
    public void CV009_GetItemCount_ReturnsNumberOfItems()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        
        var mockItems = new List<Mock<OpenQA.Selenium.IWebElement>>();
        for (int i = 0; i < 8; i++)
        {
            mockItems.Add(MockAppiumFactory.CreateMockElement());
        }
        mockElement.Setup(e => e.FindElements(It.IsAny<OpenQA.Selenium.By>()))
            .Returns(mockItems.Select(m => m.Object).ToList().AsReadOnly());
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var collectionView = new TestableCollectionViewControl(context, "productsGrid", null);

        // Act
        var count = collectionView.GetItemCount();

        // Assert
        count.Should().Be(8);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void CV010_IsEmpty_WhenNoItems_ReturnsTrue()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        mockElement.Setup(e => e.FindElements(It.IsAny<OpenQA.Selenium.By>()))
            .Returns(new List<OpenQA.Selenium.IWebElement>().AsReadOnly());
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var collectionView = new TestableCollectionViewControl(context, "productsGrid", null);

        // Act
        var isEmpty = collectionView.IsEmpty();

        // Assert
        isEmpty.Should().BeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void CV011_IsEmpty_WhenHasItems_ReturnsFalse()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        var mockItem = MockAppiumFactory.CreateMockElement();
        mockElement.Setup(e => e.FindElements(It.IsAny<OpenQA.Selenium.By>()))
            .Returns(new List<OpenQA.Selenium.IWebElement> { mockItem.Object }.AsReadOnly());
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var collectionView = new TestableCollectionViewControl(context, "productsGrid", null);

        // Act
        var isEmpty = collectionView.IsEmpty();

        // Assert
        isEmpty.Should().BeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void CV012_AssertItemCount_WithMatchingCount_Passes()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        var mockItems = new List<Mock<OpenQA.Selenium.IWebElement>>();
        for (int i = 0; i < 6; i++)
        {
            mockItems.Add(MockAppiumFactory.CreateMockElement());
        }
        mockElement.Setup(e => e.FindElements(It.IsAny<OpenQA.Selenium.By>()))
            .Returns(mockItems.Select(m => m.Object).ToList().AsReadOnly());
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var collectionView = new TestableCollectionViewControl(context, "productsGrid", null);

        // Act & Assert - should not throw
        Action act = () => collectionView.AssertItemCount(6);
        act.Should().NotThrow();
    }

    #endregion

    #region Selection Tests (CV-013 to CV-016)

    [Fact]
    [Trait("Priority", "P0")]
    public void CV013_SelectItemByIndex_SelectsCorrectItem()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        var clickedIndex = -1;
        
        var mockItems = new List<Mock<OpenQA.Selenium.IWebElement>>();
        for (int i = 0; i < 6; i++)
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
        var collectionView = new TestableCollectionViewControl(context, "productsGrid", null);

        // Act
        collectionView.SelectItemByIndex(3);

        // Assert
        clickedIndex.Should().Be(3);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void CV014_SelectItemByText_SelectsMatchingItem()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        var clickedItemText = "";
        
        var itemTexts = new[] { "Product A", "Product B", "Product C" };
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
        var collectionView = new TestableCollectionViewControl(context, "productsGrid", null);

        // Act
        collectionView.SelectItemByText("Product B");

        // Assert
        clickedItemText.Should().Be("Product B");
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void CV015_GetSelectedItems_ReturnsSelectedItemTexts()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        
        var mockItem1 = MockAppiumFactory.CreateMockElement(text: "Selected 1");
        mockItem1.Setup(e => e.GetAttribute("selected")).Returns("true");
        var mockItem2 = MockAppiumFactory.CreateMockElement(text: "Not Selected");
        mockItem2.Setup(e => e.GetAttribute("selected")).Returns("false");
        var mockItem3 = MockAppiumFactory.CreateMockElement(text: "Selected 2");
        mockItem3.Setup(e => e.GetAttribute("selected")).Returns("true");
        
        mockElement.Setup(e => e.FindElements(It.IsAny<OpenQA.Selenium.By>()))
            .Returns(new List<OpenQA.Selenium.IWebElement> 
            { 
                mockItem1.Object, 
                mockItem2.Object, 
                mockItem3.Object 
            }.AsReadOnly());
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        context.DefaultTimeoutMs = 100;
        var collectionView = new TestableCollectionViewControl(context, "productsGrid", null);

        // Act
        var selectedItems = collectionView.GetSelectedItemsText();

        // Assert
        selectedItems.Should().HaveCount(2);
        selectedItems.Should().Contain("Selected 1");
        selectedItems.Should().Contain("Selected 2");
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void CV016_HasSelectedItems_WhenItemsSelected_ReturnsTrue()
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
        var collectionView = new TestableCollectionViewControl(context, "productsGrid", null);

        // Act
        var hasSelected = collectionView.HasSelectedItems();

        // Assert
        hasSelected.Should().BeTrue();
    }

    #endregion

    #region Scroll Tests (CV-017 to CV-020)

    [Fact]
    [Trait("Priority", "P1")]
    public void CV017_ScrollToItem_ByIndex_ScrollsToCorrectItem()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        var mockItems = new List<Mock<OpenQA.Selenium.IWebElement>>();
        for (int i = 0; i < 20; i++)
        {
            mockItems.Add(MockAppiumFactory.CreateMockElement());
        }
        mockElement.Setup(e => e.FindElements(It.IsAny<OpenQA.Selenium.By>()))
            .Returns(mockItems.Select(m => m.Object).ToList().AsReadOnly());
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var collectionView = new TestableCollectionViewControl(context, "productsGrid", null);

        // Act - should not throw
        Action act = () => collectionView.ScrollToItemByIndex(15);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void CV018_ScrollToTop_ScrollsToFirstItem()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var collectionView = new TestableCollectionViewControl(context, "productsGrid", null);

        // Act - should not throw
        Action act = () => collectionView.ScrollToTop();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void CV019_ScrollToBottom_ScrollsToLastItem()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var collectionView = new TestableCollectionViewControl(context, "productsGrid", null);

        // Act - should not throw
        Action act = () => collectionView.ScrollToBottom();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    [Trait("Priority", "P2")]
    public void CV020_GetItemAtIndex_ReturnsItemText()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        var mockItems = new[]
        {
            MockAppiumFactory.CreateMockElement(text: "Item 0"),
            MockAppiumFactory.CreateMockElement(text: "Item 1"),
            MockAppiumFactory.CreateMockElement(text: "Item 2"),
            MockAppiumFactory.CreateMockElement(text: "Item 3")
        };
        mockElement.Setup(e => e.FindElements(It.IsAny<OpenQA.Selenium.By>()))
            .Returns(mockItems.Select(m => m.Object).ToList().AsReadOnly());
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        context.DefaultTimeoutMs = 100;
        var collectionView = new TestableCollectionViewControl(context, "productsGrid", null);

        // Act
        var itemText = collectionView.GetItemTextAtIndex(2);

        // Assert
        itemText.Should().Be("Item 2");
    }

    #endregion
}
