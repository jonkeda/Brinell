using Microsoft.Playwright;
using System.Collections.ObjectModel;

namespace Brinell.Blazor.Tests.ControlObject6.Mocks;

/// <summary>
/// Factory for creating mock Playwright page and locator for unit testing.
/// Playwright interfaces are mockable since they use interfaces.
/// </summary>
public static class MockPlaywrightFactory
{
    /// <summary>
    /// Creates a mock IPage with basic setup.
    /// </summary>
    public static Mock<IPage> CreateMockPage()
    {
        var mockPage = new Mock<IPage>();
        
        return mockPage;
    }

    /// <summary>
    /// Creates a mock ILocator for testing.
    /// </summary>
    public static Mock<ILocator> CreateMockLocator(
        string text = "Test Text",
        bool visible = true,
        bool enabled = true,
        int count = 1)
    {
        var mockLocator = new Mock<ILocator>();
        
        mockLocator.Setup(l => l.CountAsync())
            .ReturnsAsync(count);
        mockLocator.Setup(l => l.IsVisibleAsync(It.IsAny<LocatorIsVisibleOptions?>()))
            .ReturnsAsync(visible);
        mockLocator.Setup(l => l.IsEnabledAsync(It.IsAny<LocatorIsEnabledOptions?>()))
            .ReturnsAsync(enabled);
        mockLocator.Setup(l => l.InnerTextAsync(It.IsAny<LocatorInnerTextOptions?>()))
            .ReturnsAsync(text);
        mockLocator.Setup(l => l.InputValueAsync(It.IsAny<LocatorInputValueOptions?>()))
            .ReturnsAsync(text);
        
        // Default WaitForAsync setup (no timeout)
        mockLocator.Setup(l => l.WaitForAsync(It.IsAny<LocatorWaitForOptions>()))
            .Returns(Task.CompletedTask);
        
        // Default click setup
        mockLocator.Setup(l => l.ClickAsync(It.IsAny<LocatorClickOptions?>()))
            .Returns(Task.CompletedTask);
        
        // Default focus setup
        mockLocator.Setup(l => l.FocusAsync(It.IsAny<LocatorFocusOptions?>()))
            .Returns(Task.CompletedTask);
        
        // Default clear setup
        mockLocator.Setup(l => l.ClearAsync(It.IsAny<LocatorClearOptions?>()))
            .Returns(Task.CompletedTask);
        
        // Default fill setup
        mockLocator.Setup(l => l.FillAsync(It.IsAny<string>(), It.IsAny<LocatorFillOptions?>()))
            .Returns(Task.CompletedTask);
        
        return mockLocator;
    }

    /// <summary>
    /// Sets up a page to return a locator for a given selector.
    /// </summary>
    public static void SetupLocator(
        Mock<IPage> mockPage, 
        Mock<ILocator> mockLocator, 
        string? selector = null)
    {
        if (selector is null)
        {
            mockPage.Setup(p => p.Locator(It.IsAny<string>(), It.IsAny<PageLocatorOptions?>()))
                .Returns(mockLocator.Object);
        }
        else
        {
            mockPage.Setup(p => p.Locator(selector, It.IsAny<PageLocatorOptions?>()))
                .Returns(mockLocator.Object);
        }
        
        // Also set up GetByTestId
        mockPage.Setup(p => p.GetByTestId(It.IsAny<string>()))
            .Returns(mockLocator.Object);
        
        // Also set up GetByText
        mockPage.Setup(p => p.GetByText(It.IsAny<string>(), It.IsAny<PageGetByTextOptions?>()))
            .Returns(mockLocator.Object);
        
        // Also set up GetByLabel
        mockPage.Setup(p => p.GetByLabel(It.IsAny<string>(), It.IsAny<PageGetByLabelOptions?>()))
            .Returns(mockLocator.Object);
        
        // Also set up GetByRole
        mockPage.Setup(p => p.GetByRole(It.IsAny<AriaRole>(), It.IsAny<PageGetByRoleOptions?>()))
            .Returns(mockLocator.Object);
        
        // Also set up GetByPlaceholder
        mockPage.Setup(p => p.GetByPlaceholder(It.IsAny<string>(), It.IsAny<PageGetByPlaceholderOptions?>()))
            .Returns(mockLocator.Object);
        
        // Also set up GetByTitle
        mockPage.Setup(p => p.GetByTitle(It.IsAny<string>(), It.IsAny<PageGetByTitleOptions?>()))
            .Returns(mockLocator.Object);
    }

    /// <summary>
    /// Sets up a locator to timeout on WaitForAsync.
    /// </summary>
    public static void SetupLocatorTimeout(Mock<ILocator> mockLocator)
    {
        mockLocator.Setup(l => l.WaitForAsync(It.IsAny<LocatorWaitForOptions>()))
            .ThrowsAsync(new TimeoutException("Element not found within timeout"));
    }

    /// <summary>
    /// Sets up a locator to not find any elements.
    /// </summary>
    public static void SetupLocatorNotFound(Mock<ILocator> mockLocator)
    {
        mockLocator.Setup(l => l.CountAsync())
            .ReturnsAsync(0);
        mockLocator.Setup(l => l.IsVisibleAsync(It.IsAny<LocatorIsVisibleOptions?>()))
            .ReturnsAsync(false);
    }
}
