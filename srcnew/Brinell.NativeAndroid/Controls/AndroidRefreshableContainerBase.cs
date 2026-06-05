namespace Brinell.NativeAndroid.Controls;

public abstract class AndroidRefreshableContainerBase<TParent, TSelf> :
    AndroidScrollableContainerBase<TParent, TSelf>,
    IRefreshableControlObject<TParent>
    where TParent : INativeAndroidScope<TParent>
    where TSelf : AndroidRefreshableContainerBase<TParent, TSelf>
{
    protected AndroidRefreshableContainerBase(Locator locator, INativeAndroidScope<TParent> parentScope)
        : base(locator, parentScope)
    {
    }

    protected AndroidRefreshableContainerBase(string locatorValue, INativeAndroidScope<TParent> parentScope)
        : base(locatorValue, parentScope)
    {
    }

    public virtual bool? IsRefreshing()
    {
        var parsed = AndroidToggleControlBase<TParent>.TryParseBoolean(ContainerRoot.GetAttribute("refreshing"));
        if (parsed is not null)
        {
            return parsed;
        }

        return ContainerRoot.FindElements(NativeAndroidLocator.ByClass("android.widget.ProgressBar"))
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

    public virtual TParent AssertRefreshing(bool? expected, string? message = null, int? timeoutMs = null)
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

    public virtual TParent PullToRefresh(int? timeoutMs = null)
        => SwipeDown(timeoutMs);
}
