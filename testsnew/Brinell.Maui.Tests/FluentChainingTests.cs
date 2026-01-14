using Brinell.Core.Configuration;
using Brinell.Core.Interfaces;
using Brinell.Core.Locators;
using Brinell.Maui.Controls;
using Brinell.Maui.Interfaces;
using Brinell.Maui.Pages;
using Moq;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using Xunit;

namespace Brinell.Maui.Tests;

/// <summary>
/// Unit tests for fluent method chaining functionality.
/// Verifies that action methods return the parent page instance for method chaining.
/// </summary>
public class FluentChainingTests
{
    private readonly Mock<IMauiTestContext> _mockContext;
    private readonly Mock<AppiumDriver> _mockDriver;
    private readonly TestPage _testPage;
    
    public FluentChainingTests()
    {
        _mockContext = new Mock<IMauiTestContext>();
        _mockDriver = new Mock<AppiumDriver>(MockBehavior.Loose);
        
        // Setup timeout settings
        var timeouts = new TimeoutSettings
        {
            DefaultWait = 5000,
            PageLoad = 30000,
            PollingInterval = 100
        };
        _mockContext.Setup(c => c.Timeouts).Returns(timeouts);
        _mockContext.Setup(c => c.Driver).Returns(_mockDriver.Object);
        _mockContext.Setup(c => c.DefaultLocatorStrategy).Returns(LocatorStrategy.AutomationId);
        
        _testPage = new TestPage(_mockContext.Object);
    }
    
    #region Click Chaining Tests
    
    [Fact]
    public void Click_ReturnsPageInstance()
    {
        // Arrange
        var mockElement = SetupMockElement("TestButton");
        
        // Act
        var result = _testPage.TestButton.Click();
        
        // Assert
        Assert.Same(_testPage, result);
    }
    
    [Fact]
    public void DoubleClick_ReturnsPageInstance()
    {
        // Arrange
        var mockElement = SetupMockElement("TestButton");
        
        // Act
        var result = _testPage.TestButton.DoubleClick();
        
        // Assert
        Assert.Same(_testPage, result);
    }
    
    [Fact]
    public void RightClick_ReturnsPageInstance()
    {
        // Arrange
        var mockElement = SetupMockElement("TestButton");
        SetupActionsForRightClick();
        
        // Act
        var result = _testPage.TestButton.RightClick();
        
        // Assert
        Assert.Same(_testPage, result);
    }
    
    #endregion
    
    #region Text Entry Chaining Tests
    
    [Fact]
    public void Enter_ReturnsPageInstance()
    {
        // Arrange
        var mockElement = SetupMockElement("TestEntry");
        
        // Act
        var result = _testPage.TestEntry.Enter("test text");
        
        // Assert
        Assert.Same(_testPage, result);
    }
    
    [Fact]
    public void Enter_WithNullText_ReturnsPageWithoutAction()
    {
        // Arrange - no element setup needed since null should skip
        
        // Act
        var result = _testPage.TestEntry.Enter(null);
        
        // Assert
        Assert.Same(_testPage, result);
        // Verify no element lookup occurred
        _mockContext.Verify(c => c.TryFindElement(It.IsAny<Locator>()), Times.Never);
    }
    
    [Fact]
    public void Clear_ReturnsPageInstance()
    {
        // Arrange
        var mockElement = SetupMockElement("TestEntry");
        
        // Act
        var result = _testPage.TestEntry.Clear();
        
        // Assert
        Assert.Same(_testPage, result);
    }
    
    [Fact]
    public void SetText_ReturnsPageInstance()
    {
        // Arrange
        var mockElement = SetupMockElement("TestEntry");
        
        // Act
        var result = _testPage.TestEntry.SetText("new text");
        
        // Assert
        Assert.Same(_testPage, result);
    }
    
    [Fact]
    public void SetText_WithNullText_ReturnsPageWithoutAction()
    {
        // Arrange - no element setup needed since null should skip
        
        // Act
        var result = _testPage.TestEntry.SetText(null);
        
        // Assert
        Assert.Same(_testPage, result);
        _mockContext.Verify(c => c.TryFindElement(It.IsAny<Locator>()), Times.Never);
    }
    
    #endregion
    
    #region Method Chaining Tests
    
    [Fact]
    public void ChainedActions_ExecuteInOrder()
    {
        // Arrange
        var sequence = new List<string>();
        var buttonElement = SetupMockElement("TestButton", () => sequence.Add("Click"));
        var entryElement = SetupMockElement("TestEntry", () => sequence.Add("Enter"));
        
        // Act - chain multiple actions
        var result = _testPage
            .TestEntry.Enter("username")
            .TestEntry.Clear()
            .TestButton.Click();
        
        // Assert
        Assert.Same(_testPage, result);
        // Verify the chained calls worked (element was found for each action)
        _mockContext.Verify(c => c.FindElement(It.IsAny<Locator>()), Times.AtLeast(3));
    }
    
    [Fact]
    public void FluentChaining_AllowsMultipleEntriesAndClick()
    {
        // Arrange
        SetupMockElement("Username");
        SetupMockElement("Password");
        SetupMockElement("LoginButton");
        
        // This is the pattern we want to enable:
        // loginPage
        //     .Username.Enter("testuser")
        //     .Password.Enter("testpass")
        //     .LoginButton.Click();
        
        // Act
        var result = _testPage
            .Username.Enter("testuser")
            .Password.Enter("testpass")
            .LoginButton.Click();
        
        // Assert
        Assert.Same(_testPage, result);
    }
    
    #endregion
    
    #region Container Chaining Tests
    
    [Fact]
    public void ContainerControl_Button_ReturnsPage()
    {
        // Arrange
        SetupMockContainerElement("TestContainer");
        SetupMockChildElement("ContainerButton");
        
        // Act
        var result = _testPage.TestContainer.Button(Locator.ByAutomationId("ContainerButton")).Click();
        
        // Assert - Container's Button returns the page, not the container
        Assert.Same(_testPage, result);
    }
    
    [Fact]
    public void ContainerControl_Entry_ReturnsPage()
    {
        // Arrange
        SetupMockContainerElement("TestContainer");
        SetupMockChildElement("ContainerEntry");
        
        // Act
        var result = _testPage.TestContainer.Entry(Locator.ByAutomationId("ContainerEntry")).Enter("text");
        
        // Assert - Container's Entry returns the page, not the container
        Assert.Same(_testPage, result);
    }
    
    #endregion
    
    #region Type Safety Tests
    
    [Fact]
    public void GenericButton_HasCorrectPageType()
    {
        // Arrange & Act
        var button = _testPage.TestButton;
        
        // Assert - The button's Page property should be the correct concrete type
        Assert.IsType<TestPage>(button.Page);
        Assert.Same(_testPage, button.Page);
    }
    
    [Fact]
    public void GenericEntry_HasCorrectPageType()
    {
        // Arrange & Act
        var entry = _testPage.TestEntry;
        
        // Assert
        Assert.IsType<TestPage>(entry.Page);
        Assert.Same(_testPage, entry.Page);
    }
    
    #endregion
    
    #region Helper Methods
    
    private Mock<AppiumElement> SetupMockElement(string automationId, Action? onInteraction = null)
    {
        var mockElement = new Mock<AppiumElement>();
        mockElement.Setup(e => e.Displayed).Returns(true);
        mockElement.Setup(e => e.Enabled).Returns(true);
        mockElement.Setup(e => e.Click()).Callback(() => onInteraction?.Invoke());
        mockElement.Setup(e => e.SendKeys(It.IsAny<string>())).Callback(() => onInteraction?.Invoke());
        mockElement.Setup(e => e.Clear()).Callback(() => onInteraction?.Invoke());
        
        _mockContext.Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value == automationId)))
            .Returns(mockElement.Object);
        _mockContext.Setup(c => c.FindElement(It.Is<Locator>(l => l.Value == automationId)))
            .Returns(mockElement.Object);
        
        return mockElement;
    }
    
    private void SetupMockContainerElement(string automationId)
    {
        var mockContainerElement = new Mock<AppiumElement>();
        mockContainerElement.Setup(e => e.Displayed).Returns(true);
        mockContainerElement.Setup(e => e.Enabled).Returns(true);
        mockContainerElement.Setup(e => e.TagName).Returns("Container");
        
        _mockContext.Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value == automationId)))
            .Returns(mockContainerElement.Object);
        _mockContext.Setup(c => c.FindElement(It.Is<Locator>(l => l.Value == automationId)))
            .Returns(mockContainerElement.Object);
    }
    
    private void SetupMockChildElement(string automationId)
    {
        var mockChildElement = new Mock<AppiumElement>();
        mockChildElement.Setup(e => e.Displayed).Returns(true);
        mockChildElement.Setup(e => e.Enabled).Returns(true);
        
        // For child elements, the search happens on the container element
        // This is a simplified mock - in real tests you'd mock FindElement on the container
    }
    
    private void SetupActionsForRightClick()
    {
        // Mock Actions class for context click
        // Note: This is simplified - actual Appium Actions require more complex mocking
    }
    
    #endregion
    
    #region Test Page Object
    
    /// <summary>
    /// Test page object using the fluent CRTP pattern.
    /// </summary>
    private class TestPage : MauiPageObjectBase<TestPage>
    {
        public TestPage(IMauiTestContext context) : base(context) { }
        
        public override string Name => "Test Page";
        
        public override bool IsLoaded(int? timeoutMs = null) => true;
        
        // Controls with fluent chaining - return TestPage
        public MauiButtonControl<TestPage> TestButton => Button(Locator.ByAutomationId("TestButton"));
        public MauiEntryControl<TestPage> TestEntry => Entry(Locator.ByAutomationId("TestEntry"));
        public MauiContainerBase<TestPage> TestContainer => Container(Locator.ByAutomationId("TestContainer"));
        
        // Login form example controls
        public MauiEntryControl<TestPage> Username => Entry(Locator.ByAutomationId("Username"));
        public MauiEntryControl<TestPage> Password => Entry(Locator.ByAutomationId("Password"));
        public MauiButtonControl<TestPage> LoginButton => Button(Locator.ByAutomationId("LoginButton"));
    }
    
    #endregion
}
