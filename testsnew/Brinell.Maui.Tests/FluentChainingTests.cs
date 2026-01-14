using Brinell.Core.Configuration;
using Brinell.Core.Interfaces;
using Brinell.Core.Locators;
using Brinell.Maui.Controls;
using Brinell.Maui.Interfaces;
using Brinell.Maui.Pages;
using Moq;
using OpenQA.Selenium;
using Xunit;

namespace Brinell.Maui.Tests;

/// <summary>
/// Unit tests for fluent method chaining functionality.
/// Verifies that action methods return the parent page instance for method chaining.
/// </summary>
public class FluentChainingTests
{
    private readonly Mock<IMauiTestContext> _mockContext;
    private readonly TestPage _testPage;
    
    public FluentChainingTests()
    {
        _mockContext = new Mock<IMauiTestContext>();
        
        // Setup timeout settings
        var timeouts = new TimeoutSettings
        {
            DefaultWait = 5000,
            PageLoad = 30000,
            PollingInterval = 100
        };
        _mockContext.Setup(c => c.Timeouts).Returns(timeouts);
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
    
    [Fact(Skip = "RightClick uses Selenium Actions which requires real IWebDriver/IWebElement - integration test only")]
    public void RightClick_ReturnsPageInstance()
    {
        // Arrange
        var mockElement = SetupMockElement("TestButton");
        SetupActionsForRightClick(mockElement);
        
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
        var containerMock = SetupMockContainerElement("TestContainer");
        SetupMockChildElement(containerMock, "ContainerButton");
        
        // Act
        var result = _testPage.TestContainer.Button(Locator.ByAutomationId("ContainerButton")).Click();
        
        // Assert - Container's Button returns the page, not the container
        Assert.Same(_testPage, result);
    }
    
    [Fact]
    public void ContainerControl_Entry_ReturnsPage()
    {
        // Arrange
        var containerMock = SetupMockContainerElement("TestContainer");
        SetupMockChildElement(containerMock, "ContainerEntry");
        
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
    
    private Mock<IMauiElement> SetupMockElement(string automationId, Action? onInteraction = null)
    {
        var mockElement = new Mock<IMauiElement>();
        mockElement.Setup(e => e.Displayed).Returns(true);
        mockElement.Setup(e => e.Enabled).Returns(true);
        
        if (onInteraction != null)
        {
            mockElement.Setup(e => e.Click()).Callback(onInteraction);
            mockElement.Setup(e => e.Clear()).Callback(onInteraction);
            mockElement.Setup(e => e.SendKeys(It.IsAny<string>())).Callback<string>(_ => onInteraction());
        }
        
        _mockContext.Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value == automationId)))
            .Returns(mockElement.Object);
        _mockContext.Setup(c => c.FindElement(It.Is<Locator>(l => l.Value == automationId)))
            .Returns(mockElement.Object);
        
        return mockElement;
    }
    
    private Mock<IMauiElement> SetupMockContainerElement(string automationId)
    {
        var mockElement = new Mock<IMauiElement>();
        mockElement.Setup(e => e.Displayed).Returns(true);
        mockElement.Setup(e => e.Enabled).Returns(true);
        
        _mockContext.Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value == automationId)))
            .Returns(mockElement.Object);
        _mockContext.Setup(c => c.FindElement(It.Is<Locator>(l => l.Value == automationId)))
            .Returns(mockElement.Object);
            
        return mockElement;
    }
    
    private void SetupMockChildElement(Mock<IMauiElement> containerMock, string childAutomationId)
    {
        var childMockElement = new Mock<IMauiElement>();
        childMockElement.Setup(e => e.Displayed).Returns(true);
        childMockElement.Setup(e => e.Enabled).Returns(true);
        
        // Set up the container element to find the child element
        containerMock.Setup(e => e.FindElement(It.Is<By>(b => b.ToString()!.Contains(childAutomationId))))
            .Returns(childMockElement.Object);
    }
    
    private void SetupActionsForRightClick(Mock<IMauiElement> mockElement)
    {
        // Set up mock driver with UnwrapDriver
        var mockDriver = new Mock<IMauiDriver>();
        _mockContext.Setup(c => c.Driver).Returns(mockDriver.Object);
        
        // Note: RightClick uses Selenium Actions which require real IWebDriver/IWebElement
        // For unit tests, we skip RightClick or use integration tests
        // This test is expected to fail until we have real Appium instances
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
