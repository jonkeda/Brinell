using Brinell.Core.Abstractions;
using Brinell.Maui.Controls.Base;
using Brinell.Maui.Infrastructure;

namespace Brinell.Maui.Controls;

/// <summary>
/// MAUI ActivityIndicator control wrapper.
/// Provides busy/loading indicator functionality.
/// </summary>
public class ActivityIndicatorControl : ControlBase
{
    public ActivityIndicatorControl(AppiumTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public ActivityIndicatorControl(AppiumTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Check if the activity indicator is running (animating).
    /// </summary>
    public bool IsRunning()
    {
        var element = FindElement();
        if (element == null) return false;
        
        var isRunning = element.GetAttribute("isRunning") ?? element.GetAttribute("running");
        return isRunning?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
    }

    /// <summary>
    /// Wait for the activity indicator to stop.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds.</param>
    public bool WaitForStop(int? timeoutMs = null)
    {
        Log("WaitForStop()");
        return _context.WaitFor(() => !IsRunning(), timeoutMs, "activity indicator stop");
    }

    /// <summary>
    /// Wait for the activity indicator to start.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds.</param>
    public bool WaitForStart(int? timeoutMs = null)
    {
        Log("WaitForStart()");
        return _context.WaitFor(IsRunning, timeoutMs, "activity indicator start");
    }

    #region Assert Methods

    /// <summary>
    /// Assert the activity indicator is running.
    /// </summary>
    public void AssertRunning(string? message = null)
    {
        CheckVisible(expected: true);
        if (!IsRunning())
        {
            ThrowAssertionFailed("Running", "false", "true",
                message ?? "Expected activity indicator to be running.");
        }
        LogAssertPass("Running", "true", "true");
    }

    /// <summary>
    /// Assert the activity indicator is not running.
    /// </summary>
    public void AssertNotRunning(string? message = null)
    {
        if (IsRunning())
        {
            ThrowAssertionFailed("NotRunning", "true", "false",
                message ?? "Expected activity indicator to not be running.");
        }
        LogAssertPass("NotRunning", "false", "false");
    }

    #endregion
}
