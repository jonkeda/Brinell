using Brinell.Maui.Controls;
using Brinell.Maui.UITests.Containers;
using Brinell.Maui.UITests.Pages;

namespace Brinell.Maui.UITests.Tests.Container;

/// <summary>
/// Tests for indexed container patterns without List wrapper.
/// Demonstrates direct indexed container access.
/// Uses xUnit Assert per SPEC-017b design principles (never FluentAssertions).
/// </summary>
[Collection("Appium")]
[Trait("Category", "UITest")]
[Trait("Pattern", "IndexedContainer")]
public class IndexedContainerTests
{
    private readonly AppiumFixture _fixture;
    private ContainerDemoPage Page => _fixture.ContainerDemoPage;

    public IndexedContainerTests(AppiumFixture fixture)
    {
        _fixture = fixture;
        _fixture.NavigateToContainerDemo();
    }

    #region Existence Tests

    /// <summary>
    /// Verifies contact containers exist.
    /// </summary>
    [Fact]
    [Trait("Method", "IsExists")]
    public void Contact_ByIndex_Exists()
    {
        // Assert - contacts should exist
        Page.Contact(0).AssertExists();
        Page.Contact(1).AssertExists();
        Page.Contact(2).AssertExists();
    }

    /// <summary>
    /// Verifies contact controls exist.
    /// </summary>
    [Fact]
    [Trait("Method", "IsExists")]
    public void Contact_FindsChildren()
    {
        // Arrange
        var contact = Page.Contact(0);

        // Assert
        contact.NameLabel.AssertExists();
        contact.EmailLabel.AssertExists();
        contact.CallButton.AssertExists();
    }

    #endregion

    #region Scoping Tests

    /// <summary>
    /// Verifies each contact has its own scoped data.
    /// </summary>
    [Fact]
    [Trait("Pattern", "Scoping")]
    public void Contact_GetName()
    {
        // Act & Assert - different contacts have different names
        Assert.Equal("Alice Johnson", Page.Contact(0).NameLabel.GetText());
        Assert.Equal("Bob Smith", Page.Contact(1).NameLabel.GetText());
        Assert.Equal("Carol White", Page.Contact(2).NameLabel.GetText());
    }

    /// <summary>
    /// Verifies contact email values.
    /// </summary>
    [Fact]
    [Trait("Pattern", "Scoping")]
    public void Contact_GetEmail()
    {
        // Assert
        Assert.Equal("alice@example.com", Page.Contact(0).EmailLabel.GetText());
    }

    /// <summary>
    /// Verifies contact controls are correctly scoped.
    /// </summary>
    [Fact]
    [Trait("Pattern", "Scoping")]
    public void Contact_Controls_AreScoped()
    {
        // Arrange
        var contact = Page.Contact(0);

        // Act
        var name = contact.NameLabel.GetText();
        var email = contact.EmailLabel.GetText();

        // Assert - values are not empty
        Assert.False(string.IsNullOrEmpty(name));
        Assert.False(string.IsNullOrEmpty(email));
    }

    #endregion

    #region Index Property Tests

    /// <summary>
    /// Verifies contact index property is correct.
    /// </summary>
    [Fact]
    [Trait("Property", "Index")]
    public void Contact_Index_IsCorrect()
    {
        // Assert
        Assert.Equal(0, Page.Contact(0).Index);
        Assert.Equal(1, Page.Contact(1).Index);
        Assert.Equal(2, Page.Contact(2).Index);
    }

    #endregion

    #region Button Tests

    /// <summary>
    /// Verifies contact call button is clickable.
    /// </summary>
    [Fact]
    [Trait("Method", "IsClickable")]
    public void Contact_CallButton_IsClickable()
    {
        // Arrange
        var contact = Page.Contact(0);

        contact.CallButton.ScrollIntoView();
        // Assert
        contact.CallButton.AssertClickable();
    }

    /// <summary>
    /// Verifies clicking contact button returns container scope.
    /// </summary>
    [Fact]
    [Trait("Pattern", "FluentReturn")]
    public void Contact_ButtonClick_ReturnsContactContainer()
    {
        // Arrange
        var contact = Page.Contact(0);

        // Act
        var result = contact.CallButton.Click();

        // Assert
        Assert.NotNull(result);
        result.NameLabel.AssertExists();
    }

    #endregion

    #region Parent Navigation Tests

    /// <summary>
    /// Verifies contact Parent returns page.
    /// </summary>
    [Fact]
    [Trait("Pattern", "ParentNavigation")]
    public void Contact_Parent_ReturnsPage()
    {
        // Arrange
        var contact = Page.Contact(0);

        // Act
        var parent = contact.Parent;

        // Assert
        Assert.Same(Page, parent);
    }

    #endregion

    #region Multiple Contact Tests

    /// <summary>
    /// Verifies iterating over multiple contacts by index.
    /// </summary>
    [Fact]
    [Trait("Pattern", "IndexIteration")]
    public void Contacts_CanIterateByIndex()
    {
        // Act & Assert - iterate first three contacts
        for (int i = 0; i < 3; i++)
        {
            var contact = Page.Contact(i);
            contact.AssertExists();
            contact.NameLabel.AssertExists();
        }
    }

    #endregion
}
