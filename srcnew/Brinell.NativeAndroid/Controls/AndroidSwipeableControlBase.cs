namespace Brinell.NativeAndroid.Controls;

public abstract class AndroidSwipeableControlBase<TScope> : NativeAndroidControl<TScope>, ISwipeableControlObject<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    protected AndroidSwipeableControlBase(Locator locator, INativeAndroidScope<TScope> scope)
        : base(locator, scope)
    {
    }

    protected AndroidSwipeableControlBase(string locatorValue, INativeAndroidScope<TScope> scope)
        : base(locatorValue, scope)
    {
    }

    public virtual TScope SwipeLeft(int? timeoutMs = null)
        => SwipeByPercent(0.8, 0.5, 0.2, 0.5, timeoutMs);

    public virtual TScope SwipeRight(int? timeoutMs = null)
        => SwipeByPercent(0.2, 0.5, 0.8, 0.5, timeoutMs);

    public virtual TScope SwipeUp(int? timeoutMs = null)
        => SwipeByPercent(0.5, 0.8, 0.5, 0.2, timeoutMs);

    public virtual TScope SwipeDown(int? timeoutMs = null)
        => SwipeByPercent(0.5, 0.2, 0.5, 0.8, timeoutMs);

    public virtual TScope Swipe(int startX, int startY, int endX, int endY, int? timeoutMs = null)
    {
        var element = FindElementForAction(timeoutMs);
        var rect = element.Rect;
        element.Swipe(rect.Left + startX, rect.Top + startY, rect.Left + endX, rect.Top + endY);
        return ContainingScope;
    }

    protected TScope SwipeByPercent(
        double startX,
        double startY,
        double endX,
        double endY,
        int? timeoutMs = null)
    {
        var element = FindElementForAction(timeoutMs);
        var rect = element.Rect;
        element.Swipe(
            rect.Left + (int)Math.Round(rect.Width * startX),
            rect.Top + (int)Math.Round(rect.Height * startY),
            rect.Left + (int)Math.Round(rect.Width * endX),
            rect.Top + (int)Math.Round(rect.Height * endY));
        return ContainingScope;
    }
}
