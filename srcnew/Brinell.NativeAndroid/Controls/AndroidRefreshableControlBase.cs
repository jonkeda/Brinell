namespace Brinell.NativeAndroid.Controls;

public abstract class AndroidRefreshableControlBase<TScope> : AndroidScrollableControlBase<TScope>, IRefreshableControlObject<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    protected AndroidRefreshableControlBase(Locator locator, INativeAndroidScope<TScope> scope)
        : base(locator, scope)
    {
    }

    protected AndroidRefreshableControlBase(string locatorValue, INativeAndroidScope<TScope> scope)
        : base(locatorValue, scope)
    {
    }

    public virtual bool? IsRefreshing()
    {
        var element = TryFindElement();
        if (element is null)
        {
            return null;
        }

        var parsed = AndroidToggleControlBase<TScope>.TryParseBoolean(element.GetAttribute("refreshing"));
        if (parsed is not null)
        {
            return parsed;
        }

        return element.FindElements(NativeAndroidLocator.ByClass("android.widget.ProgressBar"))
            .Any(progress => progress.Visible);
    }

    public virtual bool WaitRefreshing(bool? expected, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return true;
        }

        return Poll(() => IsRefreshing() == expected.Value, timeoutMs ?? DefaultTimeoutMs);
    }

    public virtual TScope AssertRefreshing(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return ContainingScope;
        }

        if (!WaitRefreshing(expected, timeoutMs))
        {
            Fail(message ?? $"Expected refreshing state to be {expected}, actual {IsRefreshing()}.", expected, IsRefreshing());
        }

        return ContainingScope;
    }

    public virtual TScope PullToRefresh(int? timeoutMs = null)
    {
        SwipeDown(timeoutMs);
        return ContainingScope;
    }
}
