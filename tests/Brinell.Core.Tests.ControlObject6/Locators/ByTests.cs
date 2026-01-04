using Brinell.Core.ControlObject6.Locators;
using FluentAssertions;

namespace Brinell.Core.Tests.ControlObject6.Locators;

/// <summary>
/// Tests for By static factory class (BY-001 to BY-016).
/// </summary>
public class ByTests
{
    [Fact]
    public void BY001_AutomationId_ReturnsLocatorWithAutomationIdStrategy()
    {
        // Arrange & Act
        var locator = By.AutomationId("btnSubmit");

        // Assert
        locator.Strategy.Should().Be(LocatorStrategy.AutomationId);
        locator.Value.Should().Be("btnSubmit");
    }

    [Fact]
    public void BY002_Id_ReturnsLocatorWithIdStrategy()
    {
        // Arrange & Act
        var locator = By.Id("elementId");

        // Assert
        locator.Strategy.Should().Be(LocatorStrategy.Id);
        locator.Value.Should().Be("elementId");
    }

    [Fact]
    public void BY003_Name_ReturnsLocatorWithNameStrategy()
    {
        // Arrange & Act
        var locator = By.Name("inputName");

        // Assert
        locator.Strategy.Should().Be(LocatorStrategy.Name);
        locator.Value.Should().Be("inputName");
    }

    [Fact]
    public void BY004_ClassName_ReturnsLocatorWithClassNameStrategy()
    {
        // Arrange & Act
        var locator = By.ClassName("btn-primary");

        // Assert
        locator.Strategy.Should().Be(LocatorStrategy.ClassName);
        locator.Value.Should().Be("btn-primary");
    }

    [Fact]
    public void BY005_XPath_ReturnsLocatorWithXPathStrategy()
    {
        // Arrange & Act
        var locator = By.XPath("//div[@id='test']");

        // Assert
        locator.Strategy.Should().Be(LocatorStrategy.XPath);
        locator.Value.Should().Be("//div[@id='test']");
    }

    [Fact]
    public void BY006_Css_ReturnsLocatorWithCssStrategy()
    {
        // Arrange & Act
        var locator = By.Css("div.container > button");

        // Assert
        locator.Strategy.Should().Be(LocatorStrategy.Css);
        locator.Value.Should().Be("div.container > button");
    }

    [Fact]
    public void BY007_Text_ReturnsLocatorWithTextStrategy()
    {
        // Arrange & Act
        var locator = By.Text("Submit");

        // Assert
        locator.Strategy.Should().Be(LocatorStrategy.Text);
        locator.Value.Should().Be("Submit");
    }

    [Fact]
    public void BY008_PartialText_ReturnsLocatorWithPartialTextStrategy()
    {
        // Arrange & Act
        var locator = By.PartialText("Sub");

        // Assert
        locator.Strategy.Should().Be(LocatorStrategy.PartialText);
        locator.Value.Should().Be("Sub");
    }

    [Fact]
    public void BY009_AccessibilityId_ReturnsLocatorWithAccessibilityIdStrategy()
    {
        // Arrange & Act
        var locator = By.AccessibilityId("submitButton");

        // Assert
        locator.Strategy.Should().Be(LocatorStrategy.AccessibilityId);
        locator.Value.Should().Be("submitButton");
    }

    [Fact]
    public void BY010_TagName_ReturnsLocatorWithTagNameStrategy()
    {
        // Arrange & Act
        var locator = By.TagName("button");

        // Assert
        locator.Strategy.Should().Be(LocatorStrategy.TagName);
        locator.Value.Should().Be("button");
    }

    [Fact]
    public void BY011_Label_ReturnsLocatorWithLabelStrategy()
    {
        // Arrange & Act
        var locator = By.Label("Username");

        // Assert
        locator.Strategy.Should().Be(LocatorStrategy.Label);
        locator.Value.Should().Be("Username");
    }

    [Fact]
    public void BY012_Placeholder_ReturnsLocatorWithPlaceholderStrategy()
    {
        // Arrange & Act
        var locator = By.Placeholder("Enter name...");

        // Assert
        locator.Strategy.Should().Be(LocatorStrategy.Placeholder);
        locator.Value.Should().Be("Enter name...");
    }

    [Fact]
    public void BY013_Title_ReturnsLocatorWithTitleStrategy()
    {
        // Arrange & Act
        var locator = By.Title("Click to submit");

        // Assert
        locator.Strategy.Should().Be(LocatorStrategy.Title);
        locator.Value.Should().Be("Click to submit");
    }

    [Fact]
    public void BY014_Role_ReturnsLocatorWithRoleStrategy()
    {
        // Arrange & Act
        var locator = By.Role("button");

        // Assert
        locator.Strategy.Should().Be(LocatorStrategy.Role);
        locator.Value.Should().Be("button");
    }

    [Fact]
    public void BY015_TestId_ReturnsLocatorWithTestIdStrategy()
    {
        // Arrange & Act
        var locator = By.TestId("submit-btn");

        // Assert
        locator.Strategy.Should().Be(LocatorStrategy.TestId);
        locator.Value.Should().Be("submit-btn");
    }

    [Fact]
    public void BY016_DataAttribute_ReturnsLocatorWithDataAttributeStrategyAndName()
    {
        // Arrange & Act
        var locator = By.DataAttribute("custom-id", "myValue");

        // Assert
        locator.Strategy.Should().Be(LocatorStrategy.DataAttribute);
        locator.Value.Should().Be("myValue");
        locator.DataAttributeName.Should().Be("custom-id");
    }

    [Fact]
    public void AllByMethods_ReturnNonNullLocator()
    {
        // Assert - all should return non-null locators
        By.AutomationId("x").Should().NotBeNull();
        By.Id("x").Should().NotBeNull();
        By.Name("x").Should().NotBeNull();
        By.ClassName("x").Should().NotBeNull();
        By.XPath("x").Should().NotBeNull();
        By.Css("x").Should().NotBeNull();
        By.Text("x").Should().NotBeNull();
        By.PartialText("x").Should().NotBeNull();
        By.AccessibilityId("x").Should().NotBeNull();
        By.TagName("x").Should().NotBeNull();
        By.Label("x").Should().NotBeNull();
        By.Placeholder("x").Should().NotBeNull();
        By.Title("x").Should().NotBeNull();
        By.Role("x").Should().NotBeNull();
        By.TestId("x").Should().NotBeNull();
        By.DataAttribute("attr", "x").Should().NotBeNull();
    }
}
