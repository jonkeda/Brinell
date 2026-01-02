# 7. Wait/Check/Is/Assert Pattern - Code Examples

**Parent:** [Wait/Check/Is/Assert Pattern](21d7_WaitCheckIsAssertPattern.md)  
**Version:** 3.0 (December 2025)

> **v3 Note:** Examples show platform-specific context types (e.g., `FlaUITestContext`).
> Control classes use platform base classes with native driver access.

---

## 7.1 Complete Method Implementation (WPF)

```csharp
namespace Oravey.UITestFramework.Wpf.Controls.Base;

// Example: Visibility methods showing the full pattern
// Each platform has its own ControlBase with native driver access

public abstract partial class ControlBase
{
    #region Visibility Pattern - All Four Methods
    
    /// <summary>
    /// Is* - Immediate state check, no waiting, no logging.
    /// Use for: Conditional logic, current state queries.
    /// </summary>
    public virtual bool IsVisible()
    {
        var element = GetElement();
        return element != null && Context.Driver.IsVisible(element);
    }
    
    /// <summary>
    /// Wait* - Poll until condition met or timeout.
    /// Use for: Async operations, dynamic content.
    /// Returns: true if condition met, false if timeout.
    /// </summary>
    public virtual bool WaitVisible(bool expected = true, int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? Context.DefaultTimeoutMs;
        var description = $"'{AutomationId}' visible = {expected}";
        
        return Context.WaitFor(
            condition: () => IsVisible() == expected,
            timeoutMs: timeout,
            description: description);
    }
    
    /// <summary>
    /// Check* - Wait + throw on failure.
    /// Use for: Preconditions before actions.
    /// Throws: AssertionException if condition not met.
    /// </summary>
    public virtual void CheckVisible(bool expected = true, int? timeoutMs = null)
    {
        if (!WaitVisible(expected, timeoutMs))
        {
            var actual = IsVisible();
            
            // Log error only
            Logger.LogError(
                Context.TestName,
                Page?.PageName,
                AutomationId,
                "CheckFailed",
                $"Visibility check failed. Expected: {expected}, Actual: {actual}",
                null);
            
            throw new AssertionException(
                $"Control '{AutomationId}' visibility check failed. " +
                $"Expected: {expected}, Actual: {actual}");
        }
    }
    
    /// <summary>
    /// Assert* - Semantic assertion with full logging.
    /// Use for: Test assertions (verify expected behavior).
    /// Logs: Always logs result (pass or fail) to CSV.
    /// Throws: AssertionException if condition not met.
    /// </summary>
    public virtual void AssertVisible(bool expected = true, int? timeoutMs = null)
    {
        // Capture initial state for logging
        var actualBefore = IsVisible();
        
        // Wait for expected state
        var passed = WaitVisible(expected, timeoutMs);
        
        // Capture final state
        var actualAfter = IsVisible();
        
        // ALWAYS log the assertion result
        Logger.LogAssertion(
            testName: Context.TestName,
            pageName: Page?.PageName,
            controlId: AutomationId,
            assertionType: "AssertVisible",
            actualValue: actualAfter.ToString(),
            expectedValue: expected.ToString(),
            passed: passed,
            message: passed ? null : $"Expected visible={expected}, was {actualAfter}");
        
        // Throw on failure
        if (!passed)
        {
            throw new AssertionException(
                $"Control '{AutomationId}' visibility assertion failed. " +
                $"Expected: {expected}, Actual: {actualAfter}");
        }
    }
    
    #endregion
}
```

---

## 7.2 Text Value Methods

```csharp
public abstract partial class ViewControlBase
{
    #region Text Pattern - Get/Wait/Check/Assert
    
    /// <summary>
    /// Get* - Immediate value retrieval.
    /// </summary>
    public virtual string GetText()
    {
        var element = GetElement();
        return element != null ? Context.Driver.GetText(element) : string.Empty;
    }
    
    /// <summary>
    /// Wait* - Wait for exact text value.
    /// </summary>
    public virtual bool WaitText(string expected, int? timeoutMs = null)
    {
        return Context.WaitFor(
            () => GetText() == expected,
            timeoutMs,
            $"'{AutomationId}' text = '{expected}'");
    }
    
    /// <summary>
    /// Wait* - Wait for text containing substring.
    /// </summary>
    public virtual bool WaitTextContains(string substring, int? timeoutMs = null)
    {
        return Context.WaitFor(
            () => GetText().Contains(substring, StringComparison.OrdinalIgnoreCase),
            timeoutMs,
            $"'{AutomationId}' text contains '{substring}'");
    }
    
    /// <summary>
    /// Check* - Wait for text, throw on failure.
    /// </summary>
    public virtual void CheckText(string expected, int? timeoutMs = null)
    {
        if (!WaitText(expected, timeoutMs))
        {
            var actual = GetText();
            throw new AssertionException(
                $"Control '{AutomationId}' text check failed. " +
                $"Expected: '{expected}', Actual: '{actual}'");
        }
    }
    
    /// <summary>
    /// Assert* - Assert exact text with logging.
    /// </summary>
    public virtual void AssertText(string expected, int? timeoutMs = null)
    {
        var actual = GetText();
        var passed = WaitText(expected, timeoutMs);
        var finalActual = GetText();
        
        Logger.LogAssertion(
            Context.TestName,
            Page?.PageName,
            AutomationId,
            "AssertText",
            finalActual,
            expected,
            passed,
            passed ? null : $"Expected '{expected}', was '{finalActual}'");
        
        if (!passed)
        {
            throw new AssertionException(
                $"Control '{AutomationId}' text assertion failed. " +
                $"Expected: '{expected}', Actual: '{finalActual}'");
        }
    }
    
    /// <summary>
    /// Assert* - Assert text contains substring with logging.
    /// </summary>
    public virtual void AssertTextContains(string substring, int? timeoutMs = null)
    {
        var actual = GetText();
        var passed = WaitTextContains(substring, timeoutMs);
        var finalActual = GetText();
        
        Logger.LogAssertion(
            Context.TestName,
            Page?.PageName,
            AutomationId,
            "AssertTextContains",
            finalActual,
            $"contains '{substring}'",
            passed,
            passed ? null : $"'{finalActual}' does not contain '{substring}'");
        
        if (!passed)
        {
            throw new AssertionException(
                $"Control '{AutomationId}' text assertion failed. " +
                $"Expected to contain: '{substring}', Actual: '{finalActual}'");
        }
    }
    
    /// <summary>
    /// Assert* - Assert text starts with prefix.
    /// </summary>
    public virtual void AssertTextStartsWith(string prefix, int? timeoutMs = null)
    {
        var passed = Context.WaitFor(
            () => GetText().StartsWith(prefix, StringComparison.OrdinalIgnoreCase),
            timeoutMs,
            $"'{AutomationId}' text starts with '{prefix}'");
        
        var actual = GetText();
        
        Logger.LogAssertion(
            Context.TestName,
            Page?.PageName,
            AutomationId,
            "AssertTextStartsWith",
            actual,
            $"starts with '{prefix}'",
            passed,
            passed ? null : $"'{actual}' does not start with '{prefix}'");
        
        if (!passed)
        {
            throw new AssertionException(
                $"Control '{AutomationId}' text assertion failed. " +
                $"Expected to start with: '{prefix}', Actual: '{actual}'");
        }
    }
    
    /// <summary>
    /// Assert* - Assert text matches regex pattern.
    /// </summary>
    public virtual void AssertTextMatches(string pattern, int? timeoutMs = null)
    {
        var regex = new System.Text.RegularExpressions.Regex(pattern);
        
        var passed = Context.WaitFor(
            () => regex.IsMatch(GetText()),
            timeoutMs,
            $"'{AutomationId}' text matches '{pattern}'");
        
        var actual = GetText();
        
        Logger.LogAssertion(
            Context.TestName,
            Page?.PageName,
            AutomationId,
            "AssertTextMatches",
            actual,
            $"matches '{pattern}'",
            passed,
            passed ? null : $"'{actual}' does not match pattern");
        
        if (!passed)
        {
            throw new AssertionException(
                $"Control '{AutomationId}' text assertion failed. " +
                $"Expected to match: '{pattern}', Actual: '{actual}'");
        }
    }
    
    #endregion
}
```

---

## 7.3 Action Methods with Check

```csharp
public abstract partial class ClickableControlBase
{
    /// <summary>
    /// Click action - ALWAYS checks clickable first.
    /// </summary>
    public virtual void Click()
    {
        // CRITICAL: Always check before action
        CheckClickable();
        
        // Perform action
        var element = GetRequiredElement();
        Context.Driver.Click(element);
        
        // Log success
        Logger.LogAction(
            Context.TestName,
            Page?.PageName,
            AutomationId,
            "Click",
            null,  // no value
            null,  // no expected value
            "Success",
            null);
    }
}

public abstract partial class InputControlBase
{
    /// <summary>
    /// EnterText action - ALWAYS checks enabled first.
    /// </summary>
    public virtual void EnterText(string text)
    {
        // CRITICAL: Always check before action
        CheckEnabled();
        
        // Perform action
        var element = GetRequiredElement();
        Context.Driver.Clear(element);
        Context.Driver.SendKeys(element, text);
        
        // Log success
        Logger.LogAction(
            Context.TestName,
            Page?.PageName,
            AutomationId,
            "EnterText",
            text,
            null,
            "Success",
            null);
    }
}

public abstract partial class ToggleControlBase
{
    /// <summary>
    /// SetChecked action - Checks clickable AND verifies current state.
    /// </summary>
    public virtual void SetChecked(bool value)
    {
        // CRITICAL: Always check before action
        CheckClickable();
        
        // Check current state to avoid unnecessary toggle
        var current = IsChecked();
        if (current == value)
        {
            Logger.LogAction(
                Context.TestName,
                Page?.PageName,
                AutomationId,
                "SetChecked",
                value.ToString(),
                value.ToString(),
                "Success",
                "Already in desired state");
            return;
        }
        
        // Toggle to change state
        var element = GetRequiredElement();
        Context.Driver.Click(element);
        
        // Log success
        Logger.LogAction(
            Context.TestName,
            Page?.PageName,
            AutomationId,
            "SetChecked",
            value.ToString(),
            null,
            "Success",
            $"Changed from {current} to {value}");
    }
}
```

---

## 7.4 Test Usage Examples

```csharp
namespace Oravey.Tools.Wpf.UITests.Tests;

[Trait("Category", "UITest")]
[Collection("UITests")]
public class PatternUsageExamples : UITestBase
{
    public PatternUsageExamples(ITestOutputHelper output) : base(output) { }
    
    [Fact]
    public void Is_Pattern_For_Conditional_Logic()
    {
        var shell = new ShellPage(Context);
        
        // Use Is* for conditional logic
        if (shell.WelcomeDialog.IsVisible())
        {
            shell.WelcomeDialog.CloseButton.Click();
        }
        
        // Continue with test...
        shell.SettingsButton.Click();
    }
    
    [Fact]
    public void Wait_Pattern_For_Async_Operations()
    {
        var shell = new ShellPage(Context);
        shell.NavigateToWorldBrowser();
        
        var worldBrowser = new WorldBrowserPage(Context);
        worldBrowser.WaitForReady();
        
        // Wait for loading to complete
        bool loadingComplete = worldBrowser.LoadingIndicator.WaitVisible(false, timeoutMs: 15000);
        
        if (!loadingComplete)
        {
            Output.WriteLine("Warning: Loading took longer than expected");
        }
        
        // Wait for content
        worldBrowser.WorldList.WaitItemCount(atLeast: 1);
    }
    
    [Fact]
    public void Check_Pattern_Is_Automatic_In_Actions()
    {
        var shell = new ShellPage(Context);
        
        // Click automatically calls CheckClickable()
        // If button is not clickable, throws clear AssertionException
        shell.SettingsButton.Click();
        
        var settings = new SettingsPage(Context);
        settings.WaitForPageReady();
        
        // EnterText automatically calls CheckEnabled()
        settings.UsernameInput.EnterText("TestUser");
    }
    
    [Fact]
    public void Assert_Pattern_For_Test_Verification()
    {
        var shell = new ShellPage(Context);
        shell.NavigateToSettings();
        
        var settings = new SettingsPage(Context);
        settings.WaitForReady();
        
        // Assert* logs to CSV and throws on failure
        settings.PageTitle.AssertText("Settings");
        settings.PageTitle.AssertVisible(true);
        
        // Assert toggle state
        settings.DarkModeToggle.AssertChecked(false);
        
        // Toggle and verify
        settings.DarkModeToggle.Toggle();
        settings.DarkModeToggle.AssertChecked(true);
        
        // Assert text contains
        settings.VersionLabel.AssertTextContains("1.0");
    }
    
    [Fact]
    public void Combined_Pattern_Usage()
    {
        var shell = new ShellPage(Context);
        
        // 1. Wait for page ready (Wait pattern)
        shell.WaitForDisplayed();
        
        // 2. Assert initial state (Assert pattern - logged)
        shell.SettingsButton.AssertVisible(true);
        shell.SettingsButton.AssertEnabled(true);
        
        // 3. Navigate (Click includes Check pattern internally)
        shell.NavigateToSettings();
        
        // 4. Create page and wait for async content (Wait pattern)
        var settings = new SettingsPage(Context);
        settings.WaitForReady();
        
        // 5. Conditional handling (Is pattern)
        if (settings.FirstTimeSetupPanel.IsVisible())
        {
            settings.SkipSetupButton.Click();
        }
        
        // 6. Final assertions (Assert pattern - logged)
        settings.PageTitle.AssertText("Settings");
        settings.SaveButton.AssertEnabled(false);  // No changes yet
        
        // 7. Make changes
        settings.ThemeDropdown.SelectItem("Dark");
        
        // 8. Assert save enabled after changes
        settings.SaveButton.AssertEnabled(true);
    }
}
```

---

## 7.5 Timeout Configuration Examples

```csharp
public class TimeoutExamples : UITestBase
{
    public TimeoutExamples(ITestOutputHelper output) : base(output) { }
    
    [Fact]
    public void Default_Timeout_Usage()
    {
        var control = new ButtonControl(Context, null, "MyButton");
        
        // Uses DefaultTimeoutMs (10000ms)
        control.WaitVisible(true);
        control.AssertVisible(true);
    }
    
    [Fact]
    public void Custom_Timeout_Usage()
    {
        var control = new ButtonControl(Context, null, "SlowButton");
        
        // Use longer timeout for slow operations
        control.WaitVisible(true, timeoutMs: 30000);
        
        // Use short timeout for quick checks
        control.WaitEnabled(true, timeoutMs: Context.ShortTimeoutMs);
    }
    
    [Fact]
    public void Timeout_Constants()
    {
        // Available timeout constants
        int defaultTimeout = Context.DefaultTimeoutMs;  // 10000
        int shortTimeout = Context.ShortTimeoutMs;       // 3000
        int pollingInterval = Context.PollingIntervalMs; // 250
        
        Output.WriteLine($"Default: {defaultTimeout}ms");
        Output.WriteLine($"Short: {shortTimeout}ms");
        Output.WriteLine($"Polling: {pollingInterval}ms");
    }
}
```

---

## 7.6 Error Messages

```csharp
// Example error messages from different patterns

// Check pattern error:
// AssertionException: Control 'SettingsButton' is not clickable.
//   Visible: false, Enabled: true

// Assert pattern error:
// AssertionException: Control 'PageTitle' text assertion failed.
//   Expected: 'Settings', Actual: 'Home'

// Wait pattern (no error, returns false):
// bool result = control.WaitVisible(true);
// result == false means timeout occurred
```

---

*Related: [IsBusy-Based State Tracking Code Examples](21d8_IsBusyStateTracking_CodeExamples.md)*
