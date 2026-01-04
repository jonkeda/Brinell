using Brinell.Blazor.ControlObject6.Context;
using Brinell.Blazor.ControlObject6.Controls;
using Brinell.Blazor.ControlObject6.Pages;
using Brinell.Core.ControlObject6.Locators;

namespace Brinell.Samples.Blazor.UITests.ControlObject6.PageObjects;

/// <summary>
/// Page object for the Home page using ControlObject6 async API.
/// </summary>
public class HomePage6 : AsyncPageObjectBase
{
    public override string Name => "Home";

    protected override ControlLocator PageLocator => By.TestId("home-title");

    public HomePage6(BlazorTestContext context) : base(context)
    {
    }

    #region Controls

    /// <summary>
    /// The home page title.
    /// </summary>
    public ButtonControl HomeTitle => Button("home-title");

    /// <summary>
    /// Navigation link to Counter page.
    /// </summary>
    public ButtonControl CounterLink => Button("counter-link");

    /// <summary>
    /// Navigation link to Login page.
    /// </summary>
    public ButtonControl LoginLink => Button("login-link");

    /// <summary>
    /// Welcome message display.
    /// </summary>
    public ButtonControl WelcomeMessage => Button("welcome-message");

    #endregion

    #region Actions

    /// <summary>
    /// Navigate to Counter page.
    /// </summary>
    public async Task NavigateToCounterAsync()
    {
        await CounterLink.ClickAsync();
    }

    /// <summary>
    /// Navigate to Login page.
    /// </summary>
    public async Task NavigateToLoginAsync()
    {
        await LoginLink.ClickAsync();
    }

    #endregion
}
