namespace Brinell.NativeAndroid.Controls;

public abstract class AndroidScrollableControlBase<TScope> : AndroidSwipeableControlBase<TScope>, IScrollableControlObject<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    protected AndroidScrollableControlBase(Locator locator, INativeAndroidScope<TScope> scope)
        : base(locator, scope)
    {
    }

    protected AndroidScrollableControlBase(string locatorValue, INativeAndroidScope<TScope> scope)
        : base(locatorValue, scope)
    {
    }

    public virtual TScope ScrollToTop(int? timeoutMs = null)
    {
        RepeatSwipeDown(timeoutMs);
        return ContainingScope;
    }

    public virtual TScope ScrollToEnd(int? timeoutMs = null)
    {
        RepeatSwipeUp(timeoutMs);
        return ContainingScope;
    }

    public virtual TScope ScrollBy(int deltaX, int deltaY, int? timeoutMs = null)
    {
        var element = FindElementForAction(timeoutMs);
        var rect = element.Rect;
        var startX = rect.Left + (rect.Width / 2);
        var startY = rect.Top + (rect.Height / 2);
        var endX = Math.Clamp(startX - deltaX, rect.Left + 1, rect.Right - 1);
        var endY = Math.Clamp(startY - deltaY, rect.Top + 1, rect.Bottom - 1);
        element.Swipe(startX, startY, endX, endY);
        return ContainingScope;
    }

    public virtual TScope ScrollTo(Locator locator, int? timeoutMs = null)
    {
        ArgumentNullException.ThrowIfNull(locator);

        var root = FindElement(timeoutMs);
        if (root.TryFindElement(locator, out var child, timeoutMs ?? 0))
        {
            child!.ScrollIntoView(timeoutMs ?? DefaultTimeoutMs);
            return ContainingScope;
        }

        Context.Driver.FindElement(locator, timeoutMs ?? DefaultTimeoutMs).ScrollIntoView(timeoutMs ?? DefaultTimeoutMs);
        return ContainingScope;
    }

    public virtual double? GetScrollPosition(int? timeoutMs = null)
    {
        var element = timeoutMs is null ? TryFindElement() : FindElement(timeoutMs.Value);
        return AndroidRangeControlBase<TScope>.FirstDouble(
            element?.GetAttribute("scrollY"),
            element?.GetAttribute("scroll-y"),
            element?.GetAttribute("verticalScrollOffset"));
    }

    public virtual TScope SetScrollPosition(double percent, int? timeoutMs = null)
    {
        percent = Math.Clamp(percent, 0, 100);
        if (percent <= 0)
        {
            return ScrollToTop(timeoutMs);
        }

        ScrollToTop(timeoutMs);

        var steps = Math.Max(1, (int)Math.Round(percent / 20));
        for (var i = 0; i < steps; i++)
        {
            SwipeUp(timeoutMs);
        }

        return ContainingScope;
    }

    public virtual bool WaitScrollPosition(double? expected, double tolerance = 1.0, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return true;
        }

        return Poll(
            () =>
            {
                var actual = GetScrollPosition();
                return actual is not null && Math.Abs(actual.Value - expected.Value) <= tolerance;
            },
            timeoutMs ?? DefaultTimeoutMs);
    }

    public virtual TScope AssertScrollPosition(double? expected, double tolerance = 1.0, string? message = null, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return ContainingScope;
        }

        if (!WaitScrollPosition(expected, tolerance, timeoutMs))
        {
            Fail(message ?? $"Expected scroll position '{expected}', actual '{GetScrollPosition()}'.", expected, GetScrollPosition());
        }

        return ContainingScope;
    }

    public virtual bool? CanScrollDown(int? timeoutMs = null)
        => IsScrollable(timeoutMs);

    public virtual bool? CanScrollUp(int? timeoutMs = null)
        => IsScrollable(timeoutMs);

    protected void RepeatSwipeUp(int? timeoutMs = null, int count = 5)
    {
        for (var i = 0; i < count; i++)
        {
            SwipeUp(timeoutMs);
        }
    }

    protected void RepeatSwipeDown(int? timeoutMs = null, int count = 5)
    {
        for (var i = 0; i < count; i++)
        {
            SwipeDown(timeoutMs);
        }
    }

    private bool? IsScrollable(int? timeoutMs)
    {
        var element = timeoutMs is null ? TryFindElement() : FindElement(timeoutMs.Value);
        if (element is null)
        {
            return null;
        }

        var parsed = AndroidToggleControlBase<TScope>.TryParseBoolean(element.GetAttribute("scrollable"));
        if (parsed is not null)
        {
            return parsed;
        }

        var className = element.ClassName;
        return className?.Contains("Scroll", StringComparison.OrdinalIgnoreCase) == true
            || className?.Contains("Recycler", StringComparison.OrdinalIgnoreCase) == true;
    }
}
