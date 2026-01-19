using Brinell.Maui.UITests.Pages;

namespace Brinell.Maui.UITests.Tests;

/// <summary>
/// Tests for single container patterns.
/// Demonstrates accessing controls within a container scope.
/// Uses xUnit Assert per SPEC-017b design principles (never FluentAssertions).
/// </summary>
/// <remarks>
/// These tests navigate to the ContainerDemoPage via TabbedPage tab navigation.
/// Tab navigation uses Name-based XPath fallback for Windows TabbedPage (see SPEC-023).
/// </remarks>
[Collection("Appium")]
[Trait("Category", "UITest")]
[Trait("Pattern", "Container")]
public class SingleContainerTests
{
    private readonly AppiumFixture _fixture;
    private ContainerDemoPage Page => _fixture.ContainerDemoPage;

    public SingleContainerTests(AppiumFixture fixture)
    {
        _fixture = fixture;
        _fixture.NavigateToContainerDemo();
    }

    #region Container Existence Tests

    /// <summary>
    /// Verifies the user profile container exists.
    /// </summary>
    [Fact]
    [Trait("Method", "IsExists")]
    public void UserProfileContainer_IsExists_ReturnsTrue()
    {
        // Assert
        Page.UserProfile.AssertExists();
    }

    /// <summary>
    /// Verifies controls within the container exist.
    /// </summary>
    [Fact]
    [Trait("Method", "IsExists")]
    public void UserProfileContainer_ChildControls_Exist()
    {
        // Assert - all controls within container scope
        Page.UserProfile.TitleLabel.AssertExists();
        Page.UserProfile.NameEntry.AssertExists();
        Page.UserProfile.EmailEntry.AssertExists();
        Page.UserProfile.SaveButton.AssertExists();
    }

    #endregion

    #region Fluent Navigation Tests

    /// <summary>
    /// Demonstrates Parent property for navigating up.
    /// </summary>
    [Fact]
    [Trait("Pattern", "FluentNavigation")]
    public void UserProfileContainer_Parent_ReturnsPage()
    {
        // Act - navigate down into container, then back to page
        var page = Page.UserProfile.Parent;

        // Assert
        Assert.Same(Page, page);
    }

    /// <summary>
    /// Demonstrates Self property for fluent container access.
    /// </summary>
    [Fact]
    [Trait("Pattern", "FluentNavigation")]
    public void UserProfileContainer_Self_ReturnsSameContainer()
    {
        // Act
        var self = Page.UserProfile.Self;

        // Assert
        Assert.NotNull(self);
        self.TitleLabel.AssertExists();
    }

    #endregion

    #region Button Interaction Tests

    /// <summary>
    /// Verifies button within container is clickable.
    /// </summary>
    [Fact]
    [Trait("Method", "IsClickable")]
    public void UserProfileContainer_SaveButton_IsClickable()
    {
        // Assert
        Page.UserProfile.SaveButton.AssertClickable();
    }

    /// <summary>
    /// Demonstrates clicking button within container scope.
    /// </summary>
    [Fact]
    [Trait("Method", "Click")]
    public void UserProfileContainer_SaveButton_Click_Works()
    {
        // Act - click returns container scope
        var container = Page.UserProfile.SaveButton.Click();

        // Assert - verify we got the container back
        Assert.NotNull(container);
        container.TitleLabel.AssertExists();
    }

    #endregion
}
