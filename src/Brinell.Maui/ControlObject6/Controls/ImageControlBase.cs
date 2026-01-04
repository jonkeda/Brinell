using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;
using Brinell.Maui.ControlObject6.Context;

namespace Brinell.Maui.ControlObject6.Controls;

/// <summary>
/// Base class for image controls in MAUI.
/// Provides common functionality for image source, dimensions, and loading state.
/// </summary>
public abstract class ImageControlBase : ControlObjectBase, IImageControlObject
{
    /// <summary>
    /// Creates a new image control.
    /// </summary>
    protected ImageControlBase(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new image control using AutomationId.
    /// </summary>
    protected ImageControlBase(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    #region Image Source

    /// <inheritdoc/>
    public virtual string? GetSource(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var source = element.GetAttribute("Source") ?? element.GetAttribute("Name");
        Log($"GetSource: {source}");
        return source;
    }

    /// <inheritdoc/>
    public virtual bool HasSource(int? timeoutMs = null)
    {
        var source = GetSource(timeoutMs);
        return !string.IsNullOrEmpty(source);
    }

    /// <inheritdoc/>
    public virtual void AssertSource(string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        var actual = GetSource(timeoutMs);
        if (actual != expected)
        {
            var msg = message ?? $"Expected image source '{expected}' but was '{actual}'";
            throw new AssertionException(msg, Locator.Value, "AssertSource");
        }
    }

    #endregion

    #region Dimensions

    /// <inheritdoc/>
    public virtual (int width, int height) GetDimensions(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var size = element.Size;
        Log($"GetDimensions: {size.Width}x{size.Height}");
        return (size.Width, size.Height);
    }

    /// <inheritdoc/>
    public virtual void AssertDimensions(int? expectedWidth, int? expectedHeight, string? message = null, int? timeoutMs = null)
    {
        var (actualWidth, actualHeight) = GetDimensions(timeoutMs);

        if (expectedWidth.HasValue && actualWidth != expectedWidth.Value)
        {
            var msg = message ?? $"Expected width {expectedWidth} but was {actualWidth}";
            throw new AssertionException(msg, Locator.Value, "AssertDimensions");
        }

        if (expectedHeight.HasValue && actualHeight != expectedHeight.Value)
        {
            var msg = message ?? $"Expected height {expectedHeight} but was {actualHeight}";
            throw new AssertionException(msg, Locator.Value, "AssertDimensions");
        }
    }

    #endregion

    #region Loading State

    /// <inheritdoc/>
    public virtual bool IsLoading(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var isLoading = element.GetAttribute("IsLoading");
        return isLoading == "True" || isLoading == "true";
    }

    /// <inheritdoc/>
    public virtual bool WaitLoaded(int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? DefaultTimeoutMs;
        var deadline = DateTime.Now.AddMilliseconds(timeout);

        while (DateTime.Now < deadline)
        {
            if (!IsLoading(timeoutMs))
                return true;

            Thread.Sleep(DefaultPollingIntervalMs);
        }

        return false;
    }

    /// <inheritdoc/>
    public virtual void AssertLoaded(string? message = null, int? timeoutMs = null)
    {
        if (!WaitLoaded(timeoutMs))
        {
            var msg = message ?? "Image is still loading";
            throw new AssertionException(msg, Locator.Value, "AssertLoaded");
        }
    }

    #endregion
}
