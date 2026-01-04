using Brinell.Core.ControlObject6.Locators;
using Brinell.Maui.Tests.ControlObject6.Mocks;

namespace Brinell.Maui.Tests.ControlObject6.Controls;

/// <summary>
/// Tests for EditorControl - multi-line text input (ED-001 to ED-018).
/// Uses testable wrappers to avoid Moq issues with non-virtual AppiumDriver members.
/// </summary>
[Trait("Category", "TextInput")]
[Trait("Platform", "MAUI")]
public class EditorControlTests
{
    #region Constructor Tests (ED-001 to ED-002)

    [Fact]
    [Trait("Priority", "P0")]
    public void ED001_Constructor_WithAutomationId_SetsLocator()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);

        // Act
        var editor = new TestableEditorControl(context, "notesEditor", null);

        // Assert
        editor.Locator.Should().NotBeNull();
        editor.Locator.Value.Should().Be("notesEditor");
        editor.Locator.Strategy.Should().Be(LocatorStrategy.AutomationId);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ED002_Constructor_WithLocator_SetsLocator()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var locator = By.Id("myEditor");

        // Act
        var editor = new TestableEditorControl(context, locator, null);

        // Assert
        editor.Locator.Should().Be(locator);
    }

    #endregion

    #region State Tests (ED-003 to ED-008)

    [Fact]
    [Trait("Priority", "P0")]
    public void ED003_IsExists_WhenElementFound_ReturnsTrue()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var editor = new TestableEditorControl(context, "notesEditor", null);

        // Act
        var exists = editor.IsExists();

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ED004_IsExists_WhenElementNotFound_ReturnsFalse()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        MockAppiumFactory.SetupElementNotFound(mockDriverWrapper);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var editor = new TestableEditorControl(context, "notesEditor", null);

        // Act
        var exists = editor.IsExists();

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ED005_IsVisible_WhenElementDisplayed_ReturnsTrue()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement(displayed: true);
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var editor = new TestableEditorControl(context, "notesEditor", null);

        // Act
        var visible = editor.IsVisible();

        // Assert
        visible.Should().BeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ED006_IsVisible_WhenElementNotDisplayed_ReturnsFalse()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement(displayed: false);
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var editor = new TestableEditorControl(context, "notesEditor", null);

        // Act
        var visible = editor.IsVisible();

        // Assert
        visible.Should().BeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void ED007_IsEnabled_WhenElementEnabled_ReturnsTrue()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement(enabled: true);
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var editor = new TestableEditorControl(context, "notesEditor", null);

        // Act
        var enabled = editor.IsEnabled();

        // Assert
        enabled.Should().BeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void ED008_IsEnabled_WhenElementDisabled_ReturnsFalse()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement(enabled: false);
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var editor = new TestableEditorControl(context, "notesEditor", null);

        // Act
        var enabled = editor.IsEnabled();

        // Assert
        enabled.Should().BeFalse();
    }

    #endregion

    #region Text Entry Tests (ED-009 to ED-012)

    [Fact]
    [Trait("Priority", "P0")]
    public void ED009_EnterText_SendsKeysToElement()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        string? sentKeys = null;
        mockElement.Setup(e => e.SendKeys(It.IsAny<string>())).Callback<string>(s => sentKeys = s);
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var editor = new TestableEditorControl(context, "notesEditor", null);

        // Act
        editor.EnterText("Line 1\nLine 2\nLine 3");

        // Assert
        sentKeys.Should().Be("Line 1\nLine 2\nLine 3");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ED010_Clear_ClearsElementText()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        var cleared = false;
        mockElement.Setup(e => e.Clear()).Callback(() => cleared = true);
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var editor = new TestableEditorControl(context, "notesEditor", null);

        // Act
        editor.Clear();

        // Assert
        cleared.Should().BeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ED011_GetText_ReturnsCurrentText()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement(text: "Multi-line\ntext content");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        context.DefaultTimeoutMs = 100;
        var editor = new TestableEditorControl(context, "notesEditor", null);

        // Act
        var text = editor.GetText();

        // Assert
        text.Should().Be("Multi-line\ntext content");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ED012_SetText_ClearsAndEntersText()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        var operations = new List<string>();
        mockElement.Setup(e => e.Clear()).Callback(() => operations.Add("clear"));
        mockElement.Setup(e => e.SendKeys(It.IsAny<string>())).Callback<string>(s => operations.Add($"sendKeys:{s}"));
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var editor = new TestableEditorControl(context, "notesEditor", null);

        // Act
        editor.SetText("New text content");

        // Assert
        operations.Should().ContainInOrder("clear", "sendKeys:New text content");
    }

    #endregion

    #region Append/Prepend Tests (ED-013 to ED-014)

    [Fact]
    [Trait("Priority", "P1")]
    public void ED013_AppendText_AddsTextAtEnd()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement(text: "Existing text");
        string? sentKeys = null;
        mockElement.Setup(e => e.SendKeys(It.IsAny<string>())).Callback<string>(s => sentKeys = s);
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var editor = new TestableEditorControl(context, "notesEditor", null);

        // Act
        editor.AppendText(" and more");

        // Assert
        sentKeys.Should().Be(" and more");
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void ED014_GetLineCount_ReturnsNumberOfLines()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement(text: "Line 1\nLine 2\nLine 3");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        context.DefaultTimeoutMs = 100;
        var editor = new TestableEditorControl(context, "notesEditor", null);

        // Act
        var lineCount = editor.GetLineCount();

        // Assert
        lineCount.Should().Be(3);
    }

    #endregion

    #region Assertion Tests (ED-015 to ED-018)

    [Fact]
    [Trait("Priority", "P1")]
    public void ED015_AssertText_WithMatchingText_Passes()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement(text: "Expected multi-line\ntext");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        context.DefaultTimeoutMs = 100;
        var editor = new TestableEditorControl(context, "notesEditor", null);

        // Act & Assert - should not throw
        Action act = () => editor.AssertText("Expected multi-line\ntext");
        act.Should().NotThrow();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void ED016_AssertTextContains_WithPartialMatch_Passes()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement(text: "Some text with keyword inside");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        context.DefaultTimeoutMs = 100;
        var editor = new TestableEditorControl(context, "notesEditor", null);

        // Act & Assert - should not throw
        Action act = () => editor.AssertTextContains("keyword");
        act.Should().NotThrow();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void ED017_AssertIsEmpty_WhenEmpty_Passes()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement(text: "");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        context.DefaultTimeoutMs = 100;
        var editor = new TestableEditorControl(context, "notesEditor", null);

        // Act & Assert - should not throw
        Action act = () => editor.AssertIsEmpty();
        act.Should().NotThrow();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void ED018_AssertIsNotEmpty_WhenHasText_Passes()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement(text: "Some content");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        context.DefaultTimeoutMs = 100;
        var editor = new TestableEditorControl(context, "notesEditor", null);

        // Act & Assert - should not throw
        Action act = () => editor.AssertIsNotEmpty();
        act.Should().NotThrow();
    }

    #endregion
}
