using Brinell.Blazor.ControlObject6.Context;
using Brinell.Blazor.ControlObject6.Controls;
using Brinell.Blazor.ControlObject6.Pages;
using Brinell.Core.ControlObject6.Locators;

namespace Brinell.Samples.Blazor.UITests.ControlObject6.PageObjects;

/// <summary>
/// Page object for the Login page using ControlObject6 async API.
/// </summary>
public class LoginPage6 : AsyncPageObjectBase
{
    public override string Name => "Login";

    protected override ControlLocator PageLocator => By.TestId("login-form");

    public LoginPage6(BlazorTestContext context) : base(context)
    {
    }

    #region Controls

    /// <summary>
    /// Username input field.
    /// </summary>
    public InputControl UsernameInput => Input("username-input");

    /// <summary>
    /// Password input field.
    /// </summary>
    public InputControl PasswordInput => Input("password-input");

    /// <summary>
    /// Login button.
    /// </summary>
    public ButtonControl LoginButton => Button("login-btn");

    /// <summary>
    /// Error message label.
    /// </summary>
    public ButtonControl ErrorMessage => Button("error-message");

    /// <summary>
    /// Remember me checkbox (using button for now as we don't have CheckBoxControl).
    /// </summary>
    public ButtonControl RememberMeCheckbox => Button("remember-me");

    #endregion

    #region Actions

    /// <summary>
    /// Enter username.
    /// </summary>
    public async Task<LoginPage6> EnterUsernameAsync(string username)
    {
        await UsernameInput.EnterAsync(username);
        return this;
    }

    /// <summary>
    /// Enter password.
    /// </summary>
    public async Task<LoginPage6> EnterPasswordAsync(string password)
    {
        await PasswordInput.EnterAsync(password);
        return this;
    }

    /// <summary>
    /// Click login button.
    /// </summary>
    public async Task<LoginPage6> ClickLoginAsync()
    {
        await LoginButton.ClickAsync();
        return this;
    }

    /// <summary>
    /// Perform login with credentials.
    /// </summary>
    public async Task LoginAsync(string username, string password)
    {
        await EnterUsernameAsync(username);
        await EnterPasswordAsync(password);
        await ClickLoginAsync();
    }

    /// <summary>
    /// Clear the form.
    /// </summary>
    public async Task ClearFormAsync()
    {
        await UsernameInput.ClearAsync();
        await PasswordInput.ClearAsync();
    }

    #endregion
}
