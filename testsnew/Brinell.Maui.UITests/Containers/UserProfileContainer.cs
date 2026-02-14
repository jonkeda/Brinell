using Brinell.Maui.Controls;
using Brinell.Maui.UITests.Pages;

namespace Brinell.Maui.UITests.Containers;

/// <summary>
/// Container for the user profile section.
/// Demonstrates single container with controls and child container.
/// </summary>
public class UserProfileContainer : ContainerBase<ContainerDemoPage, UserProfileContainer>
{
    public UserProfileContainer(IMauiScope<ContainerDemoPage> parentScope, string automationId)
        : base(parentScope, new Locator(LocatorStrategy.AutomationId, automationId))
    {
    }

    #region Labels

    /// <summary>
    /// The profile title label.
    /// </summary>
    public ControlBase<UserProfileContainer> TitleLabel => new Control<UserProfileContainer>(this, "ProfileTitle");

    /// <summary>
    /// The profile status label.
    /// </summary>
    public ControlBase<UserProfileContainer> StatusLabel => new Control<UserProfileContainer>(this, "ProfileStatusLabel");

    #endregion

    #region Entry Controls

    /// <summary>
    /// The profile name entry.
    /// </summary>
    public Entry<UserProfileContainer> NameEntry => Entry("ProfileNameEntry");

    /// <summary>
    /// The profile email entry.
    /// </summary>
    public Entry<UserProfileContainer> EmailEntry => Entry("ProfileEmailEntry");

    #endregion

    #region Buttons

    /// <summary>
    /// The save profile button.
    /// </summary>
    public Button<UserProfileContainer> SaveButton => Button("ProfileSaveButton");

    #endregion
}
