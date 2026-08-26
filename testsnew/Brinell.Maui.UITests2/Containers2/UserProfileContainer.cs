using Brinell.Maui.Containers;
using Brinell.Maui.UITests.Pages2;

namespace Brinell.Maui.UITests.Containers2;

/// <summary>
/// Container for the user profile section.
/// Demonstrates single container with controls and child container.
/// </summary>
public class UserProfileContainer : ContainerObjectBase<ContainerDemoPage, UserProfileContainer>
{
    public UserProfileContainer(IMauiScope<ContainerDemoPage> parentScope, string automationId)
        : base(parentScope, new Locator(LocatorStrategy.AutomationId, automationId))
    {
    }

    #region Labels

    /// <summary>
    /// The profile title label.
    /// </summary>
    public Label<UserProfileContainer> TitleLabel => new(this,"ProfileTitle");

    /// <summary>
    /// The profile status label.
    /// </summary>
    public Label<UserProfileContainer> StatusLabel => new(this,"ProfileStatusLabel");

    #endregion

    #region Entry Controls

    /// <summary>
    /// The profile name entry.
    /// </summary>
    public Entry<UserProfileContainer> NameEntry => new(this,"ProfileNameEntry");

    /// <summary>
    /// The profile email entry.
    /// </summary>
    public Entry<UserProfileContainer> EmailEntry => new(this,"ProfileEmailEntry");

    #endregion

    #region Buttons

    /// <summary>
    /// The save profile button.
    /// </summary>
    public Button<UserProfileContainer> SaveButton => new(this,"ProfileSaveButton");

    #endregion
}
