using Brinell.Maui.Infrastructure;
using OpenQA.Selenium;

namespace Brinell.Maui.Services;

/// <summary>
/// Alert/dialog handling service implementation for Appium.
/// </summary>
public class AlertService : IAlertService
{
    private readonly AppiumTestContext _context;

    public AlertService(AppiumTestContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc/>
    public bool IsAlertPresent()
    {
        try
        {
            _context.Driver.CurrentDriver.SwitchTo().Alert();
            return true;
        }
        catch (NoAlertPresentException)
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public string? GetAlertTitle()
    {
        try
        {
            var alert = _context.Driver.CurrentDriver.SwitchTo().Alert();
            var text = alert.Text;
            // Title is typically the first line
            var lines = text?.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            return lines?.FirstOrDefault();
        }
        catch (NoAlertPresentException)
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public string? GetAlertMessage()
    {
        try
        {
            var alert = _context.Driver.CurrentDriver.SwitchTo().Alert();
            return alert.Text;
        }
        catch (NoAlertPresentException)
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public void AcceptAlert()
    {
        try
        {
            var alert = _context.Driver.CurrentDriver.SwitchTo().Alert();
            alert.Accept();
        }
        catch (NoAlertPresentException)
        {
            throw new InvalidOperationException("No alert present to accept.");
        }
    }

    /// <inheritdoc/>
    public void DismissAlert()
    {
        try
        {
            var alert = _context.Driver.CurrentDriver.SwitchTo().Alert();
            alert.Dismiss();
        }
        catch (NoAlertPresentException)
        {
            throw new InvalidOperationException("No alert present to dismiss.");
        }
    }

    /// <inheritdoc/>
    public void EnterPromptText(string text)
    {
        try
        {
            var alert = _context.Driver.CurrentDriver.SwitchTo().Alert();
            alert.SendKeys(text);
        }
        catch (NoAlertPresentException)
        {
            throw new InvalidOperationException("No alert present to enter text.");
        }
    }

    /// <inheritdoc/>
    public void TapAlertButton(string buttonText)
    {
        if (string.IsNullOrEmpty(buttonText))
            throw new ArgumentNullException(nameof(buttonText));
        
        // Try to find and click the button element directly
        // This works for non-native alerts
        var button = _context.Driver.FindElementDirect(buttonText);
        if (button != null)
        {
            button.Click();
            return;
        }
        
        // For native alerts, try platform-specific button finding
        try
        {
            var buttons = _context.Driver.CurrentDriver.FindElements(By.XPath($"//*[@text='{buttonText}' or @label='{buttonText}' or @name='{buttonText}']"));
            var alertButton = buttons.FirstOrDefault(b => b.Displayed);
            if (alertButton != null)
            {
                alertButton.Click();
                return;
            }
        }
        catch
        {
            // Fall through to accept/dismiss logic
        }
        
        // Fallback to accept/dismiss based on common button texts
        var lowerText = buttonText.ToLowerInvariant();
        if (lowerText is "ok" or "yes" or "accept" or "confirm" or "allow")
        {
            AcceptAlert();
        }
        else if (lowerText is "cancel" or "no" or "dismiss" or "deny")
        {
            DismissAlert();
        }
        else
        {
            throw new InvalidOperationException($"Could not find alert button '{buttonText}'.");
        }
    }

    /// <inheritdoc/>
    public bool WaitForAlert(int? timeoutMs = null)
    {
        return _context.WaitFor(IsAlertPresent, timeoutMs, "alert to appear");
    }

    /// <inheritdoc/>
    public bool WaitForAlertDismissed(int? timeoutMs = null)
    {
        return _context.WaitFor(() => !IsAlertPresent(), timeoutMs, "alert to dismiss");
    }

    /// <inheritdoc/>
    public void HandlePermissionDialog(bool allow)
    {
        // Platform-specific permission dialog handling
        var platform = _context.Driver.PlatformName.ToLowerInvariant();
        
        Thread.Sleep(500); // Wait for dialog to appear
        
        if (platform.Contains("android"))
        {
            // Android permission dialogs have Allow/Deny buttons
            var buttonText = allow ? "Allow" : "Deny";
            TapAlertButton(buttonText);
        }
        else if (platform.Contains("ios"))
        {
            // iOS permission dialogs have Allow/Don't Allow buttons
            var buttonText = allow ? "Allow" : "Don't Allow";
            TapAlertButton(buttonText);
        }
        else
        {
            // Windows/Mac - try generic approach
            if (allow)
                AcceptAlert();
            else
                DismissAlert();
        }
    }
}
