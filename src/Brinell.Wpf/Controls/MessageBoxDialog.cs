using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Tools;
using Brinell.Wpf.Controls.Base;
using Brinell.Wpf.Infrastructure;

namespace Brinell.Wpf.Controls;

/// <summary>
/// PageObject for Windows MessageBox dialogs (native Win32 #32770 dialogs).
/// Handles Yes/No, OK/Cancel, and OK-only message boxes explicitly.
/// 
/// Usage:
///   deleteButton.Click();
///   var messageBox = new MessageBoxDialog(Context);
///   messageBox.WaitForDisplayed();
///   messageBox.ClickYes();  // or ClickNo(), ClickOk(), ClickCancel()
/// </summary>
public class MessageBoxDialog : PageBase
{
    /// <summary>
    /// Standard Windows MessageBox class name.
    /// </summary>
    private const string MessageBoxClassName = "#32770";

    public MessageBoxDialog(FlaUITestContext context)
        : base(context, "MessageBox")
    {
    }

    /// <summary>
    /// Check if a MessageBox dialog is currently displayed.
    /// </summary>
    public override bool IsDisplayed()
    {
        return FindMessageBoxWindow() != null;
    }

    /// <summary>
    /// Get the message text from the MessageBox.
    /// </summary>
    public string GetMessage()
    {
        var window = FindMessageBoxWindow();
        if (window == null) return string.Empty;

        // Find the Static text control that contains the message
        var textElement = window.FindFirstDescendant(cf => 
            cf.ByControlType(ControlType.Text));
        
        return textElement?.Name ?? string.Empty;
    }

    /// <summary>
    /// Get the title/caption of the MessageBox.
    /// </summary>
    public string GetTitle()
    {
        var window = FindMessageBoxWindow();
        return window?.Name ?? string.Empty;
    }

    /// <summary>
    /// Check if the Yes button exists.
    /// </summary>
    public bool HasYesButton()
    {
        var window = FindMessageBoxWindow();
        return window?.FindFirstDescendant(cf => cf.ByName("Yes")) != null;
    }

    /// <summary>
    /// Check if the No button exists.
    /// </summary>
    public bool HasNoButton()
    {
        var window = FindMessageBoxWindow();
        return window?.FindFirstDescendant(cf => cf.ByName("No")) != null;
    }

    /// <summary>
    /// Check if the OK button exists.
    /// </summary>
    public bool HasOkButton()
    {
        var window = FindMessageBoxWindow();
        return window?.FindFirstDescendant(cf => cf.ByName("OK")) != null;
    }

    /// <summary>
    /// Check if the Cancel button exists.
    /// </summary>
    public bool HasCancelButton()
    {
        var window = FindMessageBoxWindow();
        return window?.FindFirstDescendant(cf => cf.ByName("Cancel")) != null;
    }

    /// <summary>
    /// Click the Yes button and wait for the dialog to close.
    /// Throws if Yes button not found.
    /// </summary>
    public void ClickYes()
    {
        ClickButton("Yes");
    }

    /// <summary>
    /// Click the No button and wait for the dialog to close.
    /// Throws if No button not found.
    /// </summary>
    public void ClickNo()
    {
        ClickButton("No");
    }

    /// <summary>
    /// Click the OK button and wait for the dialog to close.
    /// Throws if OK button not found.
    /// </summary>
    public void ClickOk()
    {
        ClickButton("OK");
    }

    /// <summary>
    /// Click the Cancel button and wait for the dialog to close.
    /// Throws if Cancel button not found.
    /// </summary>
    public void ClickCancel()
    {
        ClickButton("Cancel");
    }

    /// <summary>
    /// Click a button by name and wait for dialog to close.
    /// </summary>
    private void ClickButton(string buttonName)
    {
        var window = FindMessageBoxWindow();
        if (window == null)
        {
            ThrowPageNotDisplayed($"Click{buttonName}", "MessageBox is not displayed.");
            return;
        }

        var button = window.FindFirstDescendant(cf => cf.ByName(buttonName))?.AsButton();
        if (button == null)
        {
            ThrowPageNotReady($"Click{buttonName}", $"Button '{buttonName}' not found in MessageBox.");
            return;
        }

        Log($"Clicking '{buttonName}' button");
        button.Invoke();

        // Wait for dialog to close
        var closed = _context.WaitFor(() => !IsDisplayed(), _context.ShortTimeoutMs, "MessageBox closed");
        if (!closed)
        {
            Log($"Warning: MessageBox may not have closed after clicking '{buttonName}'");
        }

        LogNavigation($"Click{buttonName}");
    }

    /// <summary>
    /// Find the MessageBox window belonging to our process.
    /// Uses FlaUI's ModalWindows property which properly finds Win32 MessageBox dialogs.
    /// </summary>
    private Window? FindMessageBoxWindow()
    {
        try
        {
            var mainWindow = _context.MainWindow;
            
            // Method 1: Use ModalWindows property - this is the FlaUI recommended approach
            // for finding modal dialogs including native Win32 MessageBox
            var modalWindows = mainWindow.ModalWindows;
            foreach (var modalWindow in modalWindows)
            {
                try
                {
                    // Win32 MessageBox has class name #32770
                    if (modalWindow.ClassName == MessageBoxClassName)
                    {
                        return modalWindow;
                    }
                    
                    // Also check for Yes/No/OK buttons in case className differs
                    var hasMessageBoxButtons = 
                        modalWindow.FindFirstDescendant(cf => cf.ByName("Yes")) != null ||
                        modalWindow.FindFirstDescendant(cf => cf.ByName("OK")) != null;
                    if (hasMessageBoxButtons)
                    {
                        return modalWindow;
                    }
                }
                catch
                {
                    // Element might have become stale, continue
                }
            }
            
            // Method 2: Search desktop for #32770 windows owned by our process
            var processId = mainWindow.Properties.ProcessId.Value;
            var automation = _context.Driver.Automation;
            var desktop = automation.GetDesktop();
            
            // Find window by process ID and class name
            var messageBoxWindow = desktop.FindFirstChild(cf => 
                cf.ByProcessId(processId)
                  .And(cf.ByClassName(MessageBoxClassName)));
            
            if (messageBoxWindow != null)
            {
                return messageBoxWindow.AsWindow();
            }
            
            // Method 3: Find any window from our process that has MessageBox-like buttons
            var windows = desktop.FindAllChildren(cf => 
                cf.ByProcessId(processId)
                  .And(cf.ByControlType(ControlType.Window)));

            foreach (var window in windows)
            {
                try
                {
                    if (window.ClassName == MessageBoxClassName)
                    {
                        return window.AsWindow();
                    }
                    
                    // Skip main window
                    if (window.Equals(mainWindow))
                        continue;
                    
                    // Check if it has MessageBox-like buttons
                    var hasYes = window.FindFirstDescendant(cf => cf.ByName("Yes")) != null;
                    var hasOk = window.FindFirstDescendant(cf => cf.ByName("OK")) != null;
                    if (hasYes || hasOk)
                    {
                        return window.AsWindow();
                    }
                }
                catch
                {
                    // Element might have become stale
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            Log($"FindMessageBoxWindow failed: {ex.Message}");
            return null;
        }
    }
}
