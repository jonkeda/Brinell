using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;
using Brinell.Maui.ControlObject6.Context;

namespace Brinell.Maui.ControlObject6.Controls;

/// <summary>
/// Base class for activity indicator controls in MAUI.
/// Provides common functionality for checking running/loading state.
/// </summary>
public abstract class ActivityIndicatorControlBase : ControlObjectBase, IActivityIndicatorControlObject
{
    /// <summary>
    /// Creates a new activity indicator control.
    /// </summary>
    protected ActivityIndicatorControlBase(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new activity indicator control using AutomationId.
    /// </summary>
    protected ActivityIndicatorControlBase(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    #region Running State

    /// <inheritdoc/>
    public virtual bool IsRunning(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var isRunning = element.GetAttribute("IsRunning")
                       ?? element.GetAttribute("IsActive");
        return isRunning == "True" || isRunning == "true" || element.Displayed;
    }

    /// <inheritdoc/>
    public virtual bool WaitRunning(bool? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;

        var timeout = timeoutMs ?? DefaultTimeoutMs;
        var deadline = DateTime.Now.AddMilliseconds(timeout);

        while (DateTime.Now < deadline)
        {
            try
            {
                if (IsRunning(timeoutMs) == expected.Value)
                    return true;
            }
            catch
            {
                // Element not found, consider as not running
                if (!expected.Value)
                    return true;
            }

            Thread.Sleep(DefaultPollingIntervalMs);
        }

        return false;
    }

    /// <inheritdoc/>
    public virtual void AssertRunning(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        if (!WaitRunning(expected, timeoutMs))
        {
            var actual = IsRunning(timeoutMs);
            var msg = message ?? $"Expected activity indicator to be {(expected.Value ? "running" : "stopped")} but was {(actual ? "running" : "stopped")}";
            throw new AssertionException(msg, Locator.Value, "AssertRunning");
        }
    }

    #endregion

    #region Wait Helpers

    /// <inheritdoc/>
    public virtual void WaitUntilStopped(int? timeoutMs = null)
    {
        Log("WaitUntilStopped()");
        WaitRunning(false, timeoutMs);
    }

    /// <inheritdoc/>
    public virtual void WaitUntilStarted(int? timeoutMs = null)
    {
        Log("WaitUntilStarted()");
        WaitRunning(true, timeoutMs);
    }

    #endregion
}
