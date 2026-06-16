using Brinell.Maui.UITests.Pages2;

namespace Brinell.Maui.UITests.Tests.Container;

/// <summary>
/// Tests for nested container patterns.
/// Demonstrates accessing controls within nested containers.
/// Uses xUnit Assert per SPEC-017b design principles (never FluentAssertions).
/// </summary>
[Collection("Appium")]
[Trait("Category", "UITest")]
[Trait("Pattern", "NestedContainer")]
public class NestedContainerTests
{
    private readonly AppiumFixture _fixture;
    private ContainerDemoPage Page => _fixture.ContainerDemoPage;

    public NestedContainerTests(AppiumFixture fixture)
    {
        _fixture = fixture;
        _fixture.NavigateToContainerDemo();
    }

    #region Nested Container Existence Tests

    /// <summary>
    /// Verifies outer container exists.
    /// </summary>
    [Fact]
    [Trait("Method", "IsExists")]
    public void OuterContainer_IsExists()
    {
        // Assert
        Page.Outer.AssertExists();
    }

    /// <summary>
    /// Verifies inner container exists within outer.
    /// </summary>
    [Fact]
    [Trait("Method", "IsExists")]
    public void InnerContainer_IsExists()
    {
        // Assert - inner exists within outer
        Page.Outer.InnerBorder.AssertExists();
    }

    /// <summary>
    /// Verifies controls in inner container exist.
    /// </summary>
    [Fact]
    [Trait("Method", "IsExists")]
    public void InnerContainer_FindsChildren()
    {
        // Assert - inner container controls
        Page.Outer.InnerBorder.InnerTitle.AssertExists();
        Page.Outer.InnerBorder.InnerEntry.AssertExists();
        Page.Outer.InnerBorder.InnerButton.AssertExists();
    }

    /// <summary>
    /// Verifies outer container finds its own children.
    /// </summary>
    [Fact]
    [Trait("Method", "IsExists")]
    public void OuterContainer_FindsOwnChildren()
    {
        // Assert - outer container controls
        Page.Outer.OuterTitle.AssertExists();
        Page.Outer.OuterButton.AssertExists();
    }

    #endregion

    #region Scoping Tests

    /// <summary>
    /// Verifies controls are correctly scoped to their respective containers.
    /// </summary>
    [Fact]
    [Trait("Pattern", "Scoping")]
    public void NestedContainers_Controls_AreCorrectlyScoped()
    {
        // Act
        var outerText = Page.Outer.OuterTitle.GetText();
        var innerText = Page.Outer.InnerBorder.InnerTitle.GetText();

        // Assert - each label has its own text
        Assert.Contains("Outer", outerText);
        Assert.Contains("Inner", innerText);
    }

    #endregion

    #region Parent Navigation Tests

    /// <summary>
    /// Demonstrates navigating from inner to outer container via Parent.
    /// </summary>
    [Fact]
    [Trait("Pattern", "ParentNavigation")]
    public void InnerContainer_Parent_ReturnsOuterContainer()
    {
        // Arrange
        var inner = Page.Outer.InnerBorder;

        // Act
        var outer = inner.Parent;

        // Assert
        Assert.NotNull(outer);
        outer.OuterTitle.AssertExists();
    }

    /// <summary>
    /// Demonstrates navigating from nested container to page.
    /// </summary>
    [Fact]
    [Trait("Pattern", "ParentNavigation")]
    public void NestedContainer_Page_ReturnsPageObject()
    {
        // Arrange
        var inner = Page.Outer.InnerBorder;

        // Act - navigate up twice
        var page = inner.Parent.Parent;

        // Assert
        Assert.Same(Page, page);
    }

    #endregion

    #region Fluent Chaining Tests

    /// <summary>
    /// Demonstrates fluent chaining through nested containers.
    /// </summary>
    [Fact]
    [Trait("Pattern", "FluentChaining")]
    public void NestedContainers_FluentChaining_Works()
    {
        // Act - fluent chain from page down into nested container and back
        Page.Outer
            .OuterTitle.AssertExists()
            .InnerBorder
            .InnerTitle.AssertExists()
            .InnerButton.AssertClickable();
    }

    /// <summary>
    /// Demonstrates button click in nested container returns correct scope.
    /// </summary>
    [Fact]
    [Trait("Method", "Click")]
    public void InnerContainer_ButtonClick_ReturnsInnerContainer()
    {
        // Act - click button inside nested container
        var inner = Page.Outer.InnerBorder.InnerButton.Click();

        // Assert - got inner container back
        Assert.NotNull(inner);
        inner.InnerTitle.AssertExists();
    }

    /// <summary>
    /// Demonstrates deep fluent chaining with interactions.
    /// </summary>
    [Fact]
    [Trait("Pattern", "FluentChaining")]
    public void NestedContainer_DeepFluentChaining()
    {
        // Act - Clear and Enter return the container scope (InnerContainer)
        Page.Outer.InnerBorder.InnerEntry.Clear();
        Page.Outer.InnerBorder.InnerEntry.Enter("Nested value");
        
        Page.Outer.InnerBorder.InnerButton.Click();
        
        // Then access outer
        Page.Outer.OuterButton.Click();
        
        // Assert
        Page.Outer.AssertExists();
    }

    #endregion
}
