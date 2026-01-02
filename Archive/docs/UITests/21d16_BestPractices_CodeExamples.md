# 16. Best Practices - Code Examples

**Parent:** [Best Practices](21d16_BestPractices.md)

---

## 16.1 Well-Structured Test Example

```csharp
namespace Oravey.Tools.Wpf.UITests.Tests;

using FluentAssertions;
using Oravey.Tools.Wpf.UITests.Infrastructure;
using Oravey.Tools.Wpf.UITests.PageObjects;
using Oravey.Tools.Wpf.UITests.TestData;
using Oravey.UITestFramework.Core.Testing;
using Oravey.UITestFramework.Core.Testing.Attributes;
using Xunit;
using Xunit.Abstractions;

/// <summary>
/// Well-structured settings tests demonstrating best practices.
/// </summary>
[UITest]
[Platform(TestTraits.Platforms.Windows)]
[Feature(TestTraits.Features.Settings)]
[Collection("UITests")]
public class SettingsBestPracticeTests : UITestBase
{
    public SettingsBestPracticeTests(ITestOutputHelper output) : base(output) { }
    
    #region Test Structure Examples
    
    /// <summary>
    /// Good: Single responsibility - tests ONE thing.
    /// Good: Clear name describes what and expected outcome.
    /// Good: Arrange-Act-Assert pattern.
    /// </summary>
    [Fact]
    [Trait("Priority", "1")]
    public void Settings_Username_Change_Enables_Save_Button()
    {
        // Arrange - Setup preconditions
        var shell = new ShellPage(Context);
        shell.WaitForShellReady();
        var settings = shell.NavigateToSettings();
        
        // Verify precondition (save should be disabled initially)
        settings.SaveButton.AssertEnabled(false);
        
        // Act - Perform single action
        settings.SetUsername(TestHelpers.UniqueValue("User"));
        
        // Assert - Verify single outcome
        settings.SaveButton.AssertEnabled(true);
    }
    
    /// <summary>
    /// Good: Independent - creates own test data.
    /// Good: Uses unique values to avoid conflicts.
    /// Good: Verifies persistence through navigation.
    /// </summary>
    [Fact]
    [Trait("Priority", "2")]
    public void Settings_Changes_Persist_After_Navigation()
    {
        // Arrange
        var shell = new ShellPage(Context);
        shell.WaitForShellReady();
        var settings = shell.NavigateToSettings();
        
        var newUsername = TestHelpers.UniqueValue("Persist");
        
        // Act - Make change and save
        settings.SetUsername(newUsername);
        settings.SaveAndWait();
        
        // Navigate away and back
        var returnedShell = settings.NavigateToHome();
        var settingsAgain = returnedShell.NavigateToSettings();
        
        // Assert - Value should persist
        settingsAgain.UsernameInput.AssertText(newUsername);
    }
    
    /// <summary>
    /// Good: Tests edge case explicitly.
    /// Good: Verifies error handling behavior.
    /// </summary>
    [Fact]
    [Trait("Priority", "2")]
    public void Settings_Empty_Username_Shows_Validation_Error()
    {
        // Arrange
        var shell = new ShellPage(Context);
        shell.WaitForShellReady();
        var settings = shell.NavigateToSettings();
        
        // Act - Enter invalid value
        settings.UsernameInput.ClearText();
        settings.UsernameInput.Focus();  // Trigger validation
        settings.SaveButton.Click();      // Attempt save
        
        // Assert - Validation error appears
        settings.ValidationError.WaitForVisible();
        settings.ValidationError.AssertText("Username is required");
        
        // Save should still be enabled (for retry)
        settings.SaveButton.AssertEnabled(true);
    }
    
    #endregion
}
```

---

## 16.2 Proper Wait Pattern Implementation

```csharp
namespace Oravey.UITestFramework.Core.Controls.Base;

/// <summary>
/// Demonstrates proper wait pattern implementation.
/// </summary>
public abstract partial class ControlObjectBase
{
    #region Is Methods (Query State)
    
    /// <summary>
    /// Query current visibility state. No waiting.
    /// </summary>
    public virtual bool IsVisible()
    {
        try
        {
            return GetElement()?.IsVisible ?? false;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Query current enabled state. No waiting.
    /// </summary>
    public virtual bool IsEnabled()
    {
        try
        {
            return GetElement()?.IsEnabled ?? false;
        }
        catch
        {
            return false;
        }
    }
    
    #endregion
    
    #region Wait Methods (Poll + Return Bool)
    
    /// <summary>
    /// Wait for visibility. Returns true if visible within timeout.
    /// Does NOT throw on timeout.
    /// </summary>
    public virtual bool WaitForVisible(int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? Context.DefaultTimeoutMs;
        var result = Context.WaitFor(IsVisible, timeout, $"{ControlId} visible");
        
        LogWait("WaitForVisible", IsVisible().ToString(), "true", result);
        
        return result;
    }
    
    /// <summary>
    /// Wait for not visible. Returns true if not visible within timeout.
    /// </summary>
    public virtual bool WaitForNotVisible(int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? Context.DefaultTimeoutMs;
        var result = Context.WaitFor(() => !IsVisible(), timeout, $"{ControlId} not visible");
        
        LogWait("WaitForNotVisible", (!IsVisible()).ToString(), "true", result);
        
        return result;
    }
    
    /// <summary>
    /// Wait for enabled. Returns true if enabled within timeout.
    /// </summary>
    public virtual bool WaitForEnabled(int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? Context.DefaultTimeoutMs;
        var result = Context.WaitFor(IsEnabled, timeout, $"{ControlId} enabled");
        
        LogWait("WaitForEnabled", IsEnabled().ToString(), "true", result);
        
        return result;
    }
    
    #endregion
    
    #region Check Methods (Wait + Throw)
    
    /// <summary>
    /// Check visibility - waits and throws if not visible.
    /// Use before actions that require visibility.
    /// </summary>
    public virtual void CheckVisible(int? timeoutMs = null)
    {
        if (!WaitForVisible(timeoutMs))
        {
            throw new ElementNotVisibleException(
                $"Element '{ControlId}' is not visible after {timeoutMs ?? Context.DefaultTimeoutMs}ms");
        }
    }
    
    /// <summary>
    /// Check enabled - waits and throws if not enabled.
    /// Use before actions that require enabled state.
    /// </summary>
    public virtual void CheckEnabled(int? timeoutMs = null)
    {
        if (!WaitForEnabled(timeoutMs))
        {
            throw new ElementNotEnabledException(
                $"Element '{ControlId}' is not enabled after {timeoutMs ?? Context.DefaultTimeoutMs}ms");
        }
    }
    
    #endregion
    
    #region Assert Methods (Verify + Log)
    
    /// <summary>
    /// Assert visibility with logging.
    /// Use in test assertions.
    /// </summary>
    public virtual void AssertVisible(bool expected = true, int? timeoutMs = null)
    {
        // Wait for expected state
        var waitResult = expected
            ? WaitForVisible(timeoutMs)
            : WaitForNotVisible(timeoutMs);
        
        var actual = IsVisible();
        var passed = actual == expected;
        
        // Log assertion
        LogAssertion("AssertVisible", actual.ToString(), expected.ToString(), passed,
            passed ? null : $"Expected visible={expected}, actual={actual}");
        
        // Throw if failed
        if (!passed)
        {
            throw new AssertionException(
                $"Element '{ControlId}': expected visible={expected}, but was {actual}");
        }
    }
    
    /// <summary>
    /// Assert enabled with logging.
    /// </summary>
    public virtual void AssertEnabled(bool expected = true, int? timeoutMs = null)
    {
        var waitResult = expected
            ? WaitForEnabled(timeoutMs)
            : Context.WaitFor(() => !IsEnabled(), timeoutMs);
        
        var actual = IsEnabled();
        var passed = actual == expected;
        
        LogAssertion("AssertEnabled", actual.ToString(), expected.ToString(), passed,
            passed ? null : $"Expected enabled={expected}, actual={actual}");
        
        if (!passed)
        {
            throw new AssertionException(
                $"Element '{ControlId}': expected enabled={expected}, but was {actual}");
        }
    }
    
    #endregion
    
    #region Action Methods (Check First)
    
    /// <summary>
    /// Click with pre-checks. This is the proper pattern.
    /// </summary>
    public virtual void Click()
    {
        // 1. Log intent
        Logger.LogInfo(Context.TestName, PageName, $"Clicking {ControlId}");
        
        // 2. Check preconditions
        CheckVisible();
        CheckEnabled();
        
        // 3. Perform action
        try
        {
            PerformClick();
            LogAction("Click", null, true, null);
        }
        catch (Exception ex)
        {
            LogAction("Click", null, false, ex.Message);
            throw;
        }
    }
    
    /// <summary>
    /// Enter text with pre-checks.
    /// </summary>
    public virtual void EnterText(string text)
    {
        Logger.LogInfo(Context.TestName, PageName, $"Entering text in {ControlId}");
        
        CheckVisible();
        CheckEnabled();
        
        try
        {
            PerformClearText();
            PerformEnterText(text);
            LogAction("EnterText", text, true, null);
        }
        catch (Exception ex)
        {
            LogAction("EnterText", text, false, ex.Message);
            throw;
        }
    }
    
    #endregion
}
```

---

## 16.3 Proper Page Object Example

```csharp
namespace Oravey.Tools.Wpf.UITests.PageObjects;

using Oravey.UITestFramework.Core.Abstractions;
using Oravey.UITestFramework.Core.Pages.Base;
using Oravey.UITestFramework.Wpf.Controls;
using Oravey.UITestFramework.Wpf.Infrastructure;

/// <summary>
/// Well-designed page object demonstrating best practices.
/// </summary>
public class SettingsPage : BusyPageBase
{
    #region Configuration (Override for page-specific settings)
    
    protected override string BusyIndicatorId => "SettingsLoadingIndicator";
    protected override int BusyTimeoutMs => 15000;  // Settings loads data
    
    #endregion
    
    #region Controls (Private where possible)
    
    // Make controls private when they're only used internally
    private LabelControl PageTitleLabel { get; }
    private ButtonControl SaveButtonControl { get; }
    private ButtonControl CancelButtonControl { get; }
    private TextBoxControl UsernameInputControl { get; }
    private CheckBoxControl DarkModeCheckBox { get; }
    private LabelControl ValidationErrorLabel { get; }
    
    // Expose only what tests need to assert
    public LabelControl ValidationError => ValidationErrorLabel;
    public ButtonControl SaveButton => SaveButtonControl;
    public TextBoxControl UsernameInput => UsernameInputControl;
    
    #endregion
    
    #region Constructor
    
    public SettingsPage(ITestContext context) : base(context, "Settings")
    {
        var wpfContext = (FlaUITestContext)context;
        
        PageTitleLabel = new LabelControl(wpfContext, this, "SettingsTitle");
        SaveButtonControl = new ButtonControl(wpfContext, this, "SaveButton");
        CancelButtonControl = new ButtonControl(wpfContext, this, "CancelButton");
        UsernameInputControl = new TextBoxControl(wpfContext, this, "UsernameInput");
        DarkModeCheckBox = new CheckBoxControl(wpfContext, this, "DarkModeToggle");
        ValidationErrorLabel = new LabelControl(wpfContext, this, "ValidationError");
    }
    
    #endregion
    
    #region IsDisplayed (Required by base)
    
    public override bool IsDisplayed()
    {
        return PageTitleLabel.IsVisible();
    }
    
    #endregion
    
    #region Actions (Expose behavior, not structure)
    
    /// <summary>
    /// Set username value.
    /// Encapsulates the control interaction.
    /// </summary>
    public SettingsPage SetUsername(string username)
    {
        Log($"Setting username to: {username}");
        UsernameInputControl.EnterText(username);
        return this;  // Enable fluent chaining
    }
    
    /// <summary>
    /// Get current username value.
    /// </summary>
    public string GetUsername()
    {
        return UsernameInputControl.GetText();
    }
    
    /// <summary>
    /// Set dark mode setting.
    /// </summary>
    public SettingsPage SetDarkMode(bool enabled)
    {
        Log($"Setting dark mode: {enabled}");
        DarkModeCheckBox.SetChecked(enabled);
        return this;
    }
    
    /// <summary>
    /// Check if dark mode is enabled.
    /// </summary>
    public bool IsDarkModeEnabled()
    {
        return DarkModeCheckBox.IsChecked();
    }
    
    /// <summary>
    /// Save settings and wait for completion.
    /// Encapsulates save + busy wait pattern.
    /// </summary>
    public SettingsPage SaveAndWait()
    {
        Log("Saving settings");
        
        SaveButtonControl.Click();
        WaitForNotBusy();  // Wait for save to complete
        
        Log("Settings saved");
        return this;
    }
    
    #endregion
    
    #region Navigation (Return target page objects)
    
    /// <summary>
    /// Navigate back to shell without saving.
    /// Returns the target page object.
    /// </summary>
    public ShellPage NavigateToHome()
    {
        Log("Navigating to Home (cancel)");
        Logger.LogNavigation(Context.TestName, PageName, "Shell", null);
        
        CancelButtonControl.Click();
        
        // Handle unsaved changes dialog if present
        HandleUnsavedChangesDialog();
        
        var shell = new ShellPage(Context);
        shell.WaitForPageReady();  // Always wait after navigation
        
        return shell;
    }
    
    /// <summary>
    /// Save and navigate to shell.
    /// </summary>
    public ShellPage SaveAndNavigateToHome()
    {
        SaveAndWait();
        return NavigateToHome();
    }
    
    #endregion
    
    #region Private Helpers
    
    private void HandleUnsavedChangesDialog()
    {
        var dialog = new UnsavedChangesDialog(Context);
        if (dialog.IsDisplayed())
        {
            Log("Dismissing unsaved changes dialog");
            dialog.DiscardChanges();
        }
    }
    
    #endregion
}
```

---

## 16.4 Anti-Pattern vs Good Practice Comparison

```csharp
namespace Oravey.Tools.Wpf.UITests.Examples;

/// <summary>
/// Examples contrasting anti-patterns with good practices.
/// </summary>
public class AntiPatternExamples
{
    #region Anti-Pattern: Thread.Sleep
    
    // BAD - Arbitrary wait, wastes time
    public void BadClick()
    {
        Thread.Sleep(5000);  // Why 5 seconds?
        button.Click();
    }
    
    // GOOD - Wait for specific condition
    public void GoodClick()
    {
        button.CheckVisible();   // Poll until visible
        button.CheckEnabled();   // Poll until enabled
        button.Click();
    }
    
    #endregion
    
    #region Anti-Pattern: Hardcoded Values
    
    // BAD - Breaks on other machines
    public void BadPath()
    {
        App.Launch(@"C:\Users\John\Projects\App.exe");
    }
    
    // GOOD - Use configuration
    public void GoodPath()
    {
        var appPath = Environment.GetEnvironmentVariable("APP_PATH")
            ?? GetDefaultAppPath();
        App.Launch(appPath);
    }
    
    #endregion
    
    #region Anti-Pattern: Test Interdependence
    
    // BAD - Depends on Test1 running first
    public void BadTest2()
    {
        // Assumes user created in Test1 exists
        var user = GetUser("test_user");
        DeleteUser(user);
    }
    
    // GOOD - Self-contained
    public void GoodTest()
    {
        // Creates own test data
        var user = CreateTestUser();
        DeleteUser(user);
        VerifyDeleted(user);
    }
    
    #endregion
    
    #region Anti-Pattern: Assertions in Page Objects
    
    // BAD - Test logic in page object
    public class BadPage
    {
        public void VerifyUsername(string expected)
        {
            UsernameInput.GetText().Should().Be(expected);  // NO!
        }
    }
    
    // GOOD - Expose data, assert in test
    public class GoodPage
    {
        public string GetUsername() => UsernameInput.GetText();
    }
    
    public void GoodTest()
    {
        page.GetUsername().Should().Be("expected");
        // OR better:
        page.UsernameInput.AssertText("expected");
    }
    
    #endregion
    
    #region Anti-Pattern: Raw Selectors in Tests
    
    // BAD - Duplicated, brittle
    public void BadTest()
    {
        var button = window.FindFirstDescendant(cf => 
            cf.ByAutomationId("SaveSettingsButton"));
        button.Click();
    }
    
    // GOOD - Encapsulated in page object
    public void GoodTest()
    {
        settings.SaveButton.Click();
    }
    
    #endregion
    
    #region Anti-Pattern: Ignoring IsBusy
    
    // BAD - Race condition
    public void BadNavigation()
    {
        SettingsButton.Click();
        UsernameInput.EnterText("test");  // Page may not be loaded
    }
    
    // GOOD - Wait for ready
    public void GoodNavigation()
    {
        var settings = shell.NavigateToSettings();
        settings.WaitForPageReady();  // Waits for displayed AND not busy
        settings.UsernameInput.EnterText("test");
    }
    
    #endregion
    
    #region Anti-Pattern: Swallowing Exceptions
    
    // BAD - Hides real failures
    public bool BadIsVisible()
    {
        try
        {
            return element.IsVisible;
        }
        catch
        {
            return false;  // What exception? Why?
        }
    }
    
    // GOOD - Handle specific cases
    public bool GoodIsVisible()
    {
        try
        {
            var elem = GetElement();
            return elem?.IsVisible ?? false;
        }
        catch (ElementNotFoundException)
        {
            return false;  // Element doesn't exist = not visible
        }
        // Let other exceptions propagate
    }
    
    #endregion
    
    #region Anti-Pattern: Console.WriteLine for Logging
    
    // BAD - Not structured, hard to parse
    public void BadLogging()
    {
        Console.WriteLine("Clicking save button...");
    }
    
    // GOOD - Structured CSV logging
    public void GoodLogging()
    {
        Logger.LogAction(TestName, PageName, "SaveButton", "Click", null, true, null);
    }
    
    #endregion
}
```

---

## 16.5 Test Naming Conventions

```csharp
/// <summary>
/// Test naming pattern: [Unit]_[Scenario]_[ExpectedBehavior]
/// </summary>
public class NamingConventionExamples
{
    // Pattern: What_When_Then
    
    [Fact]
    public void SaveButton_WhenNoChanges_IsDisabled() { }
    
    [Fact]
    public void SaveButton_WhenUsernameChanged_BecomesEnabled() { }
    
    [Fact]
    public void Username_WhenEmpty_ShowsValidationError() { }
    
    [Fact]
    public void Settings_WhenSaved_PersistsOnReopen() { }
    
    [Fact]
    public void Navigation_WhenSettingsClicked_DisplaysSettingsPage() { }
    
    // Alternative pattern: Method_Condition_Result
    
    [Fact]
    public void Login_ValidCredentials_NavigatesToHome() { }
    
    [Fact]
    public void Login_InvalidPassword_ShowsError() { }
    
    [Fact]
    public void Delete_ConfirmationAccepted_RemovesItem() { }
}
```

---

*Related: [Troubleshooting Code Examples](21d17_Troubleshooting_CodeExamples.md)*
