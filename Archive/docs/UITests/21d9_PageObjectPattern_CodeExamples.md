# 9. Page Object Pattern - Code Examples

**Parent:** [Page Object Pattern](21d9_PageObjectPattern.md)  
**Version:** 3.0 (Updated December 2025)

**Note (v3):** PageBase classes are platform-specific. Navigation methods return void - tests create target page objects explicitly.

---

## 9.1 IPageObject Interface (Core)

```csharp
namespace Oravey.UITestFramework.Core.Abstractions;

/// <summary>
/// Interface for page objects. Platform projects provide PageBase implementations.
/// </summary>
public interface IPageObject
{
    /// <summary>Page identifier for logging.</summary>
    string Name { get; }
    
    /// <summary>Test context reference.</summary>
    ITestContext Context { get; }
    
    /// <summary>Check if page is displayed.</summary>
    bool IsDisplayed();
    
    /// <summary>Wait for page to be displayed.</summary>
    void WaitForDisplayed(TimeSpan? timeout = null);
    
    /// <summary>Wait for page to be hidden.</summary>
    void WaitForHidden(TimeSpan? timeout = null);
}
```

---

## 9.2 WPF PageBase Implementation

```csharp
namespace Oravey.UITestFramework.Wpf.Controls.Base;

using Oravey.UITestFramework.Core.Abstractions;
using Oravey.UITestFramework.Core.Logging;

/// <summary>
/// WPF-specific base class for page objects.
/// Uses FlaUI directly for element operations.
/// </summary>
public abstract class PageBase : IPageObject
{
    protected readonly FlaUITestContext Context;
    
    public string Name { get; }
    ITestContext IPageObject.Context => Context;
    
    protected ITestLogger? Logger => Context.Logger;
    
    protected PageBase(FlaUITestContext context, string pageName)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        Name = pageName ?? throw new ArgumentNullException(nameof(pageName));
    }
    
    /// <summary>
    /// Check if this page is displayed.
    /// Override to check for a key control that identifies this page.
    /// </summary>
    public abstract bool IsDisplayed();
    
    public virtual void WaitForDisplayed(TimeSpan? timeout = null)
    {
        var success = Context.WaitFor(
            IsDisplayed,
            timeout,
            $"'{Name}' displayed");
        
        if (!success)
        {
            throw new AssertionException($"Page '{Name}' not displayed after timeout");
        }
    }
    
    public virtual void WaitForHidden(TimeSpan? timeout = null)
    {
        var success = Context.WaitFor(
            () => !IsDisplayed(),
            timeout,
            $"'{Name}' hidden");
        
        if (!success)
        {
            throw new AssertionException($"Page '{Name}' still displayed after timeout");
        }
    }
    
    /// <summary>
    /// Wait for page ready. Override in BusyPageBase.
    /// </summary>
    public virtual void WaitForReady(TimeSpan? timeout = null)
    {
        Log("Waiting for page ready...");
        WaitForDisplayed(timeout);
        Log("Page ready");
    }
    
    protected virtual void Log(string message)
    {
        Logger?.LogInfo(Context.TestName, Name, message);
    }
}
```

---

## 9.2 Complete Page Object Example

```csharp
namespace Oravey.Tools.Wpf.UITests.PageObjects;

using Oravey.UITestFramework.Core.Abstractions;
using Oravey.UITestFramework.Core.Pages.Base;
using Oravey.UITestFramework.Wpf.Controls;
using Oravey.UITestFramework.Wpf.Infrastructure;

/// <summary>
/// Settings page with full IsBusy support.
/// </summary>
public class SettingsPage : BusyPageBase
{
    #region Busy Indicator Configuration
    
    protected override string BusyIndicatorId => "SettingsPageBusyIndicator";
    protected override int BusyTimeoutMs => 15000;  // Settings loads user prefs
    
    #endregion
    
    #region Controls
    
    public LabelControl PageTitle { get; }
    public LabelControl VersionLabel { get; }
    
    // User Settings
    public TextBoxControl UsernameInput { get; }
    public TextBoxControl EmailInput { get; }
    
    // Theme Settings
    public ComboBoxControl ThemeDropdown { get; }
    public CheckBoxControl DarkModeToggle { get; }
    public CheckBoxControl HighContrastToggle { get; }
    
    // Actions
    public ButtonControl SaveButton { get; }
    public ButtonControl CancelButton { get; }
    public ButtonControl ResetButton { get; }
    
    // Indicators
    public IndicatorControl LoadingIndicator { get; }
    public LabelControl StatusLabel { get; }
    
    #endregion
    
    #region Constructor
    
    public SettingsPage(ITestContext context) : base(context, "Settings")
    {
        var wpfContext = (FlaUITestContext)context;
        
        PageTitle = new LabelControl(wpfContext, this, "SettingsPageTitle");
        VersionLabel = new LabelControl(wpfContext, this, "VersionLabel");
        
        UsernameInput = new TextBoxControl(wpfContext, this, "UsernameInput");
        EmailInput = new TextBoxControl(wpfContext, this, "EmailInput");
        
        ThemeDropdown = new ComboBoxControl(wpfContext, this, "ThemeDropdown");
        DarkModeToggle = new CheckBoxControl(wpfContext, this, "DarkModeToggle");
        HighContrastToggle = new CheckBoxControl(wpfContext, this, "HighContrastToggle");
        
        SaveButton = new ButtonControl(wpfContext, this, "SaveButton");
        CancelButton = new ButtonControl(wpfContext, this, "CancelButton");
        ResetButton = new ButtonControl(wpfContext, this, "ResetButton");
        
        LoadingIndicator = new IndicatorControl(wpfContext, this, BusyIndicatorId);
        StatusLabel = new LabelControl(wpfContext, this, "StatusLabel");
    }
    
    #endregion
    
    #region IsDisplayed
    
    public override bool IsDisplayed()
    {
        return PageTitle.IsVisible();
    }
    
    #endregion
    
    #region Actions
    
    /// <summary>
    /// Update username.
    /// </summary>
    public void SetUsername(string username)
    {
        Log($"Setting username to: {username}");
        UsernameInput.EnterText(username);
    }
    
    /// <summary>
    /// Update email.
    /// </summary>
    public void SetEmail(string email)
    {
        Log($"Setting email to: {email}");
        EmailInput.EnterText(email);
    }
    
    /// <summary>
    /// Select theme from dropdown.
    /// </summary>
    public void SelectTheme(string themeName)
    {
        Log($"Selecting theme: {themeName}");
        ThemeDropdown.SelectItem(themeName);
    }
    
    /// <summary>
    /// Toggle dark mode.
    /// </summary>
    public void SetDarkMode(bool enabled)
    {
        Log($"Setting dark mode: {enabled}");
        DarkModeToggle.SetChecked(enabled);
    }
    
    /// <summary>
    /// Save settings and wait for completion.
    /// </summary>
    public void SaveAndWait()
    {
        Log("Saving settings...");
        
        SaveButton.Click();
        
        // Wait for save operation to complete
        WaitForNotBusy();
        
        Log("Settings saved");
    }
    
    /// <summary>
    /// Reset settings to defaults.
    /// </summary>
    public void ResetToDefaults()
    {
        Log("Resetting to defaults...");
        
        ResetButton.Click();
        
        // Handle confirmation dialog if present
        var confirmDialog = new ConfirmDialog(Context);
        if (confirmDialog.IsDisplayed())
        {
            confirmDialog.Confirm();
        }
        
        WaitForNotBusy();
        
        Log("Settings reset");
    }
    
    #endregion
    
    #region Navigation
    
    /// <summary>
    /// Navigate back to shell (cancel without saving).
    /// </summary>
    public ShellPage NavigateToHome()
    {
        Log("Navigating to Home");
        
        Logger.LogNavigation(Context.TestName, PageName, "Shell", null);
        
        CancelButton.Click();
        
        var shell = new ShellPage(Context);
        shell.WaitForPageReady();
        
        return shell;
    }
    
    #endregion
    
    #region Queries
    
    /// <summary>
    /// Get current username value.
    /// </summary>
    public string GetUsername() => UsernameInput.GetText();
    
    /// <summary>
    /// Get current email value.
    /// </summary>
    public string GetEmail() => EmailInput.GetText();
    
    /// <summary>
    /// Check if dark mode is enabled.
    /// </summary>
    public bool IsDarkModeEnabled() => DarkModeToggle.IsChecked();
    
    /// <summary>
    /// Check if save button is enabled (changes made).
    /// </summary>
    public bool HasUnsavedChanges() => SaveButton.IsEnabled();
    
    #endregion
}
```

---

## 9.3 Shell Page with Navigation (v3 - Returns Void)

```csharp
namespace Oravey.Tools.Wpf.UITests.PageObjects;

using Oravey.UITestFramework.Wpf.Controls;
using Oravey.UITestFramework.Wpf.Controls.Base;
using Oravey.UITestFramework.Wpf.Infrastructure;

/// <summary>
/// Main shell page providing navigation to all sections.
/// Navigation methods return void - tests create target pages.
/// </summary>
public class ShellPage : BusyPageBase
{
    protected override string BusyIndicatorId => "ShellBusyIndicator";
    
    #region Controls
    
    public Button HomeButton { get; }
    public Button WorldBrowserButton { get; }
    public Button ToolsButton { get; }
    public Button SettingsButton { get; }
    public Label StatusLabel { get; }
    
    #endregion
    
    public ShellPage(FlaUITestContext context) : base(context, "Shell")
    {
        HomeButton = new Button(context, this, "HomeButton");
        WorldBrowserButton = new Button(context, this, "WorldBrowserButton");
        ToolsButton = new Button(context, this, "ToolsButton");
        SettingsButton = new Button(context, this, "SettingsButton");
        StatusLabel = new Label(context, this, "StatusLabel");
    }
    
    public override bool IsDisplayed()
    {
        return HomeButton.IsVisible() || SettingsButton.IsVisible();
    }
    
    #region Navigation Methods (Return Void)
    
    /// <summary>
    /// Navigate to Settings page.
    /// Caller creates SettingsPage and calls WaitForReady().
    /// </summary>
    public void NavigateToSettings()
    {
        Log("Navigating to Settings");
        SettingsButton.Click();
    }
    
    /// <summary>
    /// Navigate to World Browser page.
    /// </summary>
    public void NavigateToWorldBrowser()
    {
        Log("Navigating to World Browser");
        WorldBrowserButton.Click();
    }
    
    /// <summary>
    /// Navigate to Tools page.
    /// </summary>
    public void NavigateToTools()
    {
        Log("Navigating to Tools");
        ToolsButton.Click();
    }
    
    #endregion
}
```

---

## 9.4 Dialog Page Object

```csharp
namespace Oravey.Tools.Wpf.UITests.PageObjects;

using Oravey.UITestFramework.Core.Abstractions;
using Oravey.UITestFramework.Core.Pages.Base;
using Oravey.UITestFramework.Wpf.Controls;
using Oravey.UITestFramework.Wpf.Infrastructure;

/// <summary>
/// Generic confirmation dialog.
/// </summary>
public class ConfirmDialog : PageObjectBase
{
    public LabelControl TitleLabel { get; }
    public LabelControl MessageLabel { get; }
    public ButtonControl ConfirmButton { get; }
    public ButtonControl CancelButton { get; }
    
    public ConfirmDialog(ITestContext context) : base(context, "ConfirmDialog")
    {
        var wpfContext = (FlaUITestContext)context;
        
        TitleLabel = new LabelControl(wpfContext, this, "ConfirmDialogTitle");
        MessageLabel = new LabelControl(wpfContext, this, "ConfirmDialogMessage");
        ConfirmButton = new ButtonControl(wpfContext, this, "ConfirmDialogConfirmButton");
        CancelButton = new ButtonControl(wpfContext, this, "ConfirmDialogCancelButton");
    }
    
    public override bool IsDisplayed()
    {
        return MessageLabel.IsVisible() || ConfirmButton.IsVisible();
    }
    
    /// <summary>
    /// Click confirm and wait for dialog to close.
    /// </summary>
    public void Confirm()
    {
        Log("Confirming dialog");
        ConfirmButton.Click();
        
        // Wait for dialog to disappear
        Context.WaitFor(
            () => !IsDisplayed(),
            Context.ShortTimeoutMs,
            "dialog to close");
    }
    
    /// <summary>
    /// Click cancel and wait for dialog to close.
    /// </summary>
    public void Cancel()
    {
        Log("Canceling dialog");
        CancelButton.Click();
        
        Context.WaitFor(
            () => !IsDisplayed(),
            Context.ShortTimeoutMs,
            "dialog to close");
    }
    
    /// <summary>
    /// Get the dialog message text.
    /// </summary>
    public string GetMessage() => MessageLabel.GetText();
}

/// <summary>
/// Welcome dialog shown on first launch.
/// </summary>
public class WelcomeDialog : PageObjectBase
{
    public LabelControl WelcomeMessage { get; }
    public CheckBoxControl DontShowAgainCheckbox { get; }
    public ButtonControl GetStartedButton { get; }
    public ButtonControl DismissButton { get; }
    
    public WelcomeDialog(ITestContext context) : base(context, "WelcomeDialog")
    {
        var wpfContext = (FlaUITestContext)context;
        
        WelcomeMessage = new LabelControl(wpfContext, this, "WelcomeMessage");
        DontShowAgainCheckbox = new CheckBoxControl(wpfContext, this, "DontShowAgainCheckbox");
        GetStartedButton = new ButtonControl(wpfContext, this, "GetStartedButton");
        DismissButton = new ButtonControl(wpfContext, this, "WelcomeDismissButton");
    }
    
    public override bool IsDisplayed()
    {
        return WelcomeMessage.IsVisible();
    }
    
    /// <summary>
    /// Dismiss and optionally don't show again.
    /// </summary>
    public void Dismiss(bool dontShowAgain = true)
    {
        if (dontShowAgain && DontShowAgainCheckbox.IsVisible())
        {
            DontShowAgainCheckbox.SetChecked(true);
        }
        
        DismissButton.Click();
        
        Context.WaitFor(() => !IsDisplayed());
    }
}
```

---

## 9.5 Test Using Page Objects

```csharp
namespace Oravey.Tools.Wpf.UITests.Tests;

using FluentAssertions;
using Oravey.Tools.Wpf.UITests.Infrastructure;
using Oravey.Tools.Wpf.UITests.PageObjects;
using Xunit;
using Xunit.Abstractions;

[Trait("Category", "UITest")]
[Collection("UITests")]
public class SettingsPageTests : UITestBase
{
    public SettingsPageTests(ITestOutputHelper output) : base(output) { }
    
    [Fact]
    public void Settings_Page_Displays_After_Navigation()
    {
        // Arrange
        var shell = new ShellPage(Context);
        shell.WaitForShellReady();
        
        // Act
        var settings = shell.NavigateToSettings();
        
        // Assert
        settings.PageTitle.AssertText("Settings");
        settings.SaveButton.AssertVisible(true);
        settings.SaveButton.AssertEnabled(false);  // No changes yet
    }
    
    [Fact]
    public void Settings_Changes_Enable_Save_Button()
    {
        // Arrange
        var shell = new ShellPage(Context);
        shell.WaitForShellReady();
        var settings = shell.NavigateToSettings();
        
        // Verify initial state
        settings.SaveButton.AssertEnabled(false);
        
        // Act - Make a change
        settings.SetDarkMode(true);
        
        // Assert - Save should be enabled
        settings.SaveButton.AssertEnabled(true);
        settings.HasUnsavedChanges().Should().BeTrue();
    }
    
    [Fact]
    public void Settings_Save_And_Verify_Persisted()
    {
        // Arrange
        var shell = new ShellPage(Context);
        shell.WaitForShellReady();
        var settings = shell.NavigateToSettings();
        
        // Act - Change username and save
        var newUsername = $"TestUser_{DateTime.Now.Ticks}";
        settings.SetUsername(newUsername);
        settings.SaveAndWait();
        
        // Navigate away and back
        var shellAgain = settings.NavigateToHome();
        var settingsAgain = shellAgain.NavigateToSettings();
        
        // Assert - Value should be persisted
        settingsAgain.GetUsername().Should().Be(newUsername);
    }
}
```

---

*Related: [WireMock API Mocking Code Examples](21d10_WireMockApiMocking_CodeExamples.md)*
