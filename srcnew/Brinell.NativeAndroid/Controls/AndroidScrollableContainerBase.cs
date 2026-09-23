namespace Brinell.NativeAndroid.Controls;

public abstract class AndroidScrollableContainerBase<TParent, TSelf> :
    AndroidContainerBase<TParent, TSelf>,
    IScrollableControlObject<TParent>,
    ISwipeableControlObject<TParent>
    where TParent : INativeAndroidScope<TParent>
    where TSelf : AndroidScrollableContainerBase<TParent, TSelf>
{
    protected AndroidScrollableContainerBase(Locator locator, INativeAndroidScope<TParent> parentScope)
        : base(locator, parentScope)
    {
    }

    protected AndroidScrollableContainerBase(string locatorValue, INativeAndroidScope<TParent> parentScope)
        : base(locatorValue, parentScope)
    {
    }

    public virtual TParent SwipeLeft(int? timeoutMs = null)
        => SwipeByPercent(0.8, 0.5, 0.2, 0.5);

    public virtual TParent SwipeRight(int? timeoutMs = null)
        => SwipeByPercent(0.2, 0.5, 0.8, 0.5);

    public virtual TParent SwipeUp(int? timeoutMs = null)
        => SwipeByPercent(0.5, 0.8, 0.5, 0.2);

    public virtual TParent SwipeDown(int? timeoutMs = null)
        => SwipeByPercent(0.5, 0.2, 0.5, 0.8);

    public virtual TParent Swipe(int startX, int startY, int endX, int endY, int? timeoutMs = null)
    {
        var root = ContainerRoot;
        var rect = root.Rect;
        root.Swipe(rect.Left + startX, rect.Top + startY, rect.Left + endX, rect.Top + endY);
        return ContainingScope;
    }

    public virtual TParent ScrollToTop(int? timeoutMs = null)
    {
        RepeatSwipeDown();
        return ContainingScope;
    }

    public virtual TParent ScrollToEnd(int? timeoutMs = null)
    {
        RepeatSwipeUp();
        return ContainingScope;
    }

    public virtual TParent ScrollBy(int deltaX, int deltaY, int? timeoutMs = null)
    {
        var root = ContainerRoot;
        var rect = root.Rect;
        var startX = rect.Left + (rect.Width / 2);
        var startY = rect.Top + (rect.Height / 2);
        var endX = Math.Clamp(startX - deltaX, rect.Left + 1, rect.Right - 1);
        var endY = Math.Clamp(startY - deltaY, rect.Top + 1, rect.Bottom - 1);
        root.Swipe(startX, startY, endX, endY);
        return ContainingScope;
    }

    public virtual TParent ScrollTo(Locator locator, int? timeoutMs = null)
    {
        ArgumentNullException.ThrowIfNull(locator);
        ContainerRoot.FindElement(locator, timeoutMs ?? DefaultTimeoutMs).ScrollIntoView(timeoutMs ?? DefaultTimeoutMs);
        return ContainingScope;
    }

    public virtual double? GetScrollPosition(int? timeoutMs = null)
        => AndroidRangeControlBase<TParent>.FirstDouble(
            ContainerRoot.GetAttribute("scrollY"),
            ContainerRoot.GetAttribute("scroll-y"),
            ContainerRoot.GetAttribute("verticalScrollOffset"));

    public virtual TParent SetScrollPosition(double percent, int? timeoutMs = null)
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

    public virtual TParent AssertScrollPosition(double? expected, double tolerance = 1.0, string? message = null, int? timeoutMs = null)
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
        => IsScrollable();

    public virtual bool? CanScrollUp(int? timeoutMs = null)
        => IsScrollable();

    protected TParent SwipeByPercent(double startX, double startY, double endX, double endY)
    {
        var root = ContainerRoot;
        var rect = root.Rect;
        root.Swipe(
            rect.Left + (int)Math.Round(rect.Width * startX),
            rect.Top + (int)Math.Round(rect.Height * startY),
            rect.Left + (int)Math.Round(rect.Width * endX),
            rect.Top + (int)Math.Round(rect.Height * endY));
        return ContainingScope;
    }

    protected void RepeatSwipeUp(int count = 5)
    {
        for (var i = 0; i < count; i++)
        {
            SwipeByPercent(0.5, 0.8, 0.5, 0.2);
        }
    }

    protected void RepeatSwipeDown(int count = 5)
    {
        for (var i = 0; i < count; i++)
        {
            SwipeByPercent(0.5, 0.2, 0.5, 0.8);
        }
    }

    private bool? IsScrollable()
    {
        var parsed = AndroidToggleControlBase<TParent>.TryParseBoolean(ContainerRoot.GetAttribute("scrollable"));
        if (parsed is not null)
        {
            return parsed;
        }

        var className = ContainerRoot.ClassName;
        return className?.Contains("Scroll", StringComparison.OrdinalIgnoreCase) == true
            || className?.Contains("Recycler", StringComparison.OrdinalIgnoreCase) == true;
    }
}
