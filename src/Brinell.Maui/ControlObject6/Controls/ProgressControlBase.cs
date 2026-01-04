using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;
using Brinell.Maui.ControlObject6.Context;

namespace Brinell.Maui.ControlObject6.Controls;

/// <summary>
/// Base class for progress bar controls in MAUI.
/// Provides common functionality for progress value and completion state.
/// </summary>
public abstract class ProgressControlBase : ControlObjectBase, IProgressControlObject
{
    /// <summary>
    /// Creates a new progress control.
    /// </summary>
    protected ProgressControlBase(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new progress control using AutomationId.
    /// </summary>
    protected ProgressControlBase(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    #region Progress Value

    /// <inheritdoc/>
    public virtual double GetProgress(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var value = element.GetAttribute("RangeValue.Value")
                   ?? element.GetAttribute("Progress")
                   ?? element.GetAttribute("Value");

        var progress = double.TryParse(value, out var v) ? v : 0;
        Log($"GetProgress: {progress}");
        return progress;
    }

    /// <inheritdoc/>
    public virtual bool WaitProgress(double? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;

        var timeout = timeoutMs ?? DefaultTimeoutMs;
        var deadline = DateTime.Now.AddMilliseconds(timeout);

        while (DateTime.Now < deadline)
        {
            var current = GetProgress(timeoutMs);
            if (Math.Abs(current - expected.Value) < 0.001)
                return true;

            Thread.Sleep(DefaultPollingIntervalMs);
        }

        return false;
    }

    /// <inheritdoc/>
    public virtual void AssertProgress(double? expected, double? tolerance = null, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        var actual = GetProgress(timeoutMs);
        var tol = tolerance ?? 0.001;

        if (Math.Abs(actual - expected.Value) > tol)
        {
            var msg = message ?? $"Expected progress {expected} but was {actual}";
            throw new AssertionException(msg, Locator.Value, "AssertProgress");
        }
    }

    #endregion

    #region Progress Range

    /// <inheritdoc/>
    public virtual (double min, double max) GetMinMax(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);

        var minStr = element.GetAttribute("RangeValue.Minimum")
                    ?? element.GetAttribute("Minimum")
                    ?? "0";
        var maxStr = element.GetAttribute("RangeValue.Maximum")
                    ?? element.GetAttribute("Maximum")
                    ?? "1";

        var min = double.TryParse(minStr, out var minVal) ? minVal : 0;
        var max = double.TryParse(maxStr, out var maxVal) ? maxVal : 1;

        Log($"GetMinMax: ({min}, {max})");
        return (min, max);
    }

    /// <inheritdoc/>
    public virtual double GetProgressPercent(int? timeoutMs = null)
    {
        var progress = GetProgress(timeoutMs);
        var (min, max) = GetMinMax(timeoutMs);

        if (Math.Abs(max - min) < 0.001)
            return 0;

        var percent = (progress - min) / (max - min) * 100;
        Log($"GetProgressPercent: {percent}%");
        return percent;
    }

    #endregion

    #region Completion State

    /// <inheritdoc/>
    public virtual bool IsComplete(int? timeoutMs = null)
    {
        var (_, max) = GetMinMax(timeoutMs);
        var progress = GetProgress(timeoutMs);
        return Math.Abs(progress - max) < 0.001;
    }

    /// <inheritdoc/>
    public virtual bool WaitComplete(int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? DefaultTimeoutMs;
        var deadline = DateTime.Now.AddMilliseconds(timeout);

        while (DateTime.Now < deadline)
        {
            if (IsComplete(timeoutMs))
                return true;

            Thread.Sleep(DefaultPollingIntervalMs);
        }

        return false;
    }

    /// <inheritdoc/>
    public virtual void AssertComplete(string? message = null, int? timeoutMs = null)
    {
        if (!WaitComplete(timeoutMs))
        {
            var actual = GetProgress(timeoutMs);
            var (_, max) = GetMinMax(timeoutMs);
            var msg = message ?? $"Expected progress to be complete ({max}) but was {actual}";
            throw new AssertionException(msg, Locator.Value, "AssertComplete");
        }
    }

    #endregion
}
