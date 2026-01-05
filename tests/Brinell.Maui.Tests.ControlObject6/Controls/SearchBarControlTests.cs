using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;
using Brinell.Maui.Tests.ControlObject6.Mocks;
using FluentAssertions;
using Moq;
using Xunit;

namespace Brinell.Maui.Tests.ControlObject6.Controls;

/// <summary>
/// Unit tests for SearchBarControl.
/// Test IDs: SB-001 to SB-012
/// </summary>
public class SearchBarControlTests
{
    [Fact(DisplayName = "SB-001: Constructor with AutomationId sets Locator")]
    public void Constructor_WithAutomationId_SetsLocator()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var automationId = "TestSearchBar";

        // Act
        var control = new TestableSearchBarControl(context, automationId);

        // Assert
        control.Locator.Should().NotBeNull();
        control.Locator.Value.Should().Be(automationId);
        control.Locator.Strategy.Should().Be(LocatorStrategy.AutomationId);
    }

    [Fact(DisplayName = "SB-002: Constructor with Locator sets Locator")]
    public void Constructor_WithLocator_SetsLocator()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var locator = By.AutomationId("TestSearchBar");

        // Act
        var control = new TestableSearchBarControl(context, locator);

        // Assert
        control.Locator.Should().BeSameAs(locator);
    }

    [Fact(DisplayName = "SB-003: GetSearchText returns current search text")]
    public void GetSearchText_ReturnsCurrentSearchText()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("searchbar");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableSearchBarControl(context, "TestSearchBar");
        control.SetSearchText("test query");

        // Act
        var text = control.GetSearchText();

        // Assert
        text.Should().Be("test query");
    }

    [Fact(DisplayName = "SB-004: Search sets text and submits")]
    public void Search_SetsTextAndSubmits()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("searchbar");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableSearchBarControl(context, "TestSearchBar");

        // Act
        control.Search("search term");

        // Assert
        control.GetSearchText().Should().Be("search term");
        control.WasSubmitted().Should().BeTrue();
    }

    [Fact(DisplayName = "SB-005: Submit submits the search")]
    public void Submit_SubmitsTheSearch()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("searchbar");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableSearchBarControl(context, "TestSearchBar");

        // Act
        control.Submit();

        // Assert
        control.WasSubmitted().Should().BeTrue();
    }

    [Fact(DisplayName = "SB-006: ClearSearch clears the search text")]
    public void ClearSearch_ClearsSearchText()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("searchbar");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableSearchBarControl(context, "TestSearchBar");
        control.SetSearchText("some text");

        // Act
        control.ClearSearch();

        // Assert
        control.GetSearchText().Should().BeEmpty();
    }

    [Fact(DisplayName = "SB-007: GetPlaceholder returns placeholder text")]
    public void GetPlaceholder_ReturnsPlaceholderText()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("searchbar");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableSearchBarControl(context, "TestSearchBar");
        control.SetPlaceholder("Enter search term");

        // Act
        var placeholder = control.GetPlaceholder();

        // Assert
        placeholder.Should().Be("Enter search term");
    }

    [Fact(DisplayName = "SB-008: IsExists returns true when element exists")]
    public void IsExists_WhenElementExists_ReturnsTrue()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("searchbar");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableSearchBarControl(context, "TestSearchBar");

        // Act
        var exists = control.IsExists();

        // Assert
        exists.Should().BeTrue();
    }

    [Fact(DisplayName = "SB-009: IsVisible returns true when visible")]
    public void IsVisible_WhenVisible_ReturnsTrue()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("searchbar");
        mockElement.Setup(e => e.Displayed).Returns(true);
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableSearchBarControl(context, "TestSearchBar");

        // Act
        var visible = control.IsVisible();

        // Assert
        visible.Should().BeTrue();
    }

    [Fact(DisplayName = "SB-010: IsEnabled returns true when enabled")]
    public void IsEnabled_WhenEnabled_ReturnsTrue()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("searchbar");
        mockElement.Setup(e => e.Enabled).Returns(true);
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableSearchBarControl(context, "TestSearchBar");

        // Act
        var enabled = control.IsEnabled();

        // Assert
        enabled.Should().BeTrue();
    }

    [Fact(DisplayName = "SB-011: Enter sets search text")]
    public void Enter_SetsSearchText()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("searchbar");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableSearchBarControl(context, "TestSearchBar");

        // Act
        control.Enter("new search");

        // Assert - Enter uses base class which interacts with element
        // The element.SendKeys was called
        mockElement.Verify(e => e.SendKeys("new search"), Times.Once);
    }

    [Fact(DisplayName = "SB-012: Clear clears the search bar")]
    public void Clear_ClearsSearchBar()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("searchbar");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableSearchBarControl(context, "TestSearchBar");

        // Act
        control.Clear();

        // Assert
        mockElement.Verify(e => e.Clear(), Times.Once);
    }
}
