using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;
using Brinell.Maui.Tests.ControlObject6.Mocks;
using FluentAssertions;
using Moq;
using Xunit;

namespace Brinell.Maui.Tests.ControlObject6.Controls;

/// <summary>
/// Unit tests for WebViewControl.
/// Test IDs: WV-001 to WV-015
/// </summary>
public class WebViewControlTests
{
    [Fact(DisplayName = "WV-001: Constructor with AutomationId sets Locator")]
    public void Constructor_WithAutomationId_SetsLocator()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var automationId = "TestWebView";

        // Act
        var control = new TestableWebViewControl(context, automationId);

        // Assert
        control.Locator.Should().NotBeNull();
        control.Locator.Value.Should().Be(automationId);
        control.Locator.Strategy.Should().Be(LocatorStrategy.AutomationId);
    }

    [Fact(DisplayName = "WV-002: Constructor with Locator sets Locator")]
    public void Constructor_WithLocator_SetsLocator()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var locator = By.AutomationId("TestWebView");

        // Act
        var control = new TestableWebViewControl(context, locator);

        // Assert
        control.Locator.Should().BeSameAs(locator);
    }

    [Fact(DisplayName = "WV-003: GetCurrentUrl returns current URL")]
    public void GetCurrentUrl_ReturnsCurrentUrl()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("webview");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableWebViewControl(context, "TestWebView");
        control.SetUrl("https://example.com/page");

        // Act
        var url = control.GetCurrentUrl();

        // Assert
        url.Should().Be("https://example.com/page");
    }

    [Fact(DisplayName = "WV-004: GetTitle returns page title")]
    public void GetTitle_ReturnsPageTitle()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("webview");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableWebViewControl(context, "TestWebView");
        control.SetTitle("Test Page Title");

        // Act
        var title = control.GetTitle();

        // Assert
        title.Should().Be("Test Page Title");
    }

    [Fact(DisplayName = "WV-005: IsLoading returns loading state")]
    public void IsLoading_ReturnsLoadingState()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("webview");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableWebViewControl(context, "TestWebView");
        control.SetLoading(true);

        // Act
        var isLoading = control.IsLoading();

        // Assert
        isLoading.Should().BeTrue();
    }

    [Fact(DisplayName = "WV-006: NavigateTo sets URL")]
    public void NavigateTo_SetsUrl()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("webview");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableWebViewControl(context, "TestWebView");

        // Act
        control.NavigateTo("https://new-url.com");

        // Assert
        control.GetCurrentUrl().Should().Be("https://new-url.com");
    }

    [Fact(DisplayName = "WV-007: GoBack navigates back")]
    public void GoBack_NavigatesBack()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("webview");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableWebViewControl(context, "TestWebView");

        // Act & Assert - just verify no exception
        var action = () => control.GoBack();
        action.Should().NotThrow();
    }

    [Fact(DisplayName = "WV-008: GoForward navigates forward")]
    public void GoForward_NavigatesForward()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("webview");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableWebViewControl(context, "TestWebView");

        // Act & Assert - just verify no exception
        var action = () => control.GoForward();
        action.Should().NotThrow();
    }

    [Fact(DisplayName = "WV-009: Reload reloads the page")]
    public void Reload_ReloadsPage()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("webview");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableWebViewControl(context, "TestWebView");

        // Act & Assert - just verify no exception
        var action = () => control.Reload();
        action.Should().NotThrow();
    }

    [Fact(DisplayName = "WV-010: WaitForPageLoad returns true when loaded")]
    public void WaitForPageLoad_WhenLoaded_ReturnsTrue()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableWebViewControl(context, "TestWebView");
        control.SetLoading(false);

        // Act
        var result = control.WaitForPageLoad();

        // Assert
        result.Should().BeTrue();
    }

    [Fact(DisplayName = "WV-011: AssertUrl passes when URL matches")]
    public void AssertUrl_WhenMatches_Passes()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("webview");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableWebViewControl(context, "TestWebView");
        control.SetUrl("https://example.com");

        // Act & Assert
        var action = () => control.AssertUrl("https://example.com");
        action.Should().NotThrow();
    }

    [Fact(DisplayName = "WV-012: AssertUrl throws when URL mismatches")]
    public void AssertUrl_WhenMismatch_Throws()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("webview");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableWebViewControl(context, "TestWebView");
        control.SetUrl("https://example.com");

        // Act & Assert
        var action = () => control.AssertUrl("https://different.com");
        action.Should().Throw<AssertionException>();
    }

    [Fact(DisplayName = "WV-013: AssertUrlContains passes when URL contains text")]
    public void AssertUrlContains_WhenContains_Passes()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("webview");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableWebViewControl(context, "TestWebView");
        control.SetUrl("https://example.com/page");

        // Act & Assert
        var action = () => control.AssertUrlContains("example");
        action.Should().NotThrow();
    }

    [Fact(DisplayName = "WV-014: AssertTitle passes when title matches")]
    public void AssertTitle_WhenMatches_Passes()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("webview");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableWebViewControl(context, "TestWebView");
        control.SetTitle("My Page Title");

        // Act & Assert
        var action = () => control.AssertTitle("My Page Title");
        action.Should().NotThrow();
    }

    [Fact(DisplayName = "WV-015: AssertLoaded passes when not loading")]
    public void AssertLoaded_WhenNotLoading_Passes()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("webview");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableWebViewControl(context, "TestWebView");
        control.SetLoading(false);

        // Act & Assert
        var action = () => control.AssertLoaded();
        action.Should().NotThrow();
    }

    [Fact(DisplayName = "WV-016: AssertLoaded throws when still loading")]
    public void AssertLoaded_WhenLoading_Throws()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("webview");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableWebViewControl(context, "TestWebView");
        control.SetLoading(true);

        // Act & Assert
        var action = () => control.AssertLoaded();
        action.Should().Throw<AssertionException>();
    }
}
