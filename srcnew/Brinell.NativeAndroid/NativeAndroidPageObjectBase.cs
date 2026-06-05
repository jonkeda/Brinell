namespace Brinell.NativeAndroid;

public abstract class NativeAndroidPageObjectBase<TSelf> :
    IPageObject<NativeAndroidElement>,
    INativeAndroidScope<TSelf>
    where TSelf : NativeAndroidPageObjectBase<TSelf>
{
    protected NativeAndroidPageObjectBase(NativeAndroidTestContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public virtual string Name => GetType().Name;

    public NativeAndroidTestContext Context { get; }

    public TSelf Self => (TSelf)this;

    public virtual LocatorStrategy DefaultLocatorStrategy => Context.DefaultLocatorStrategy;

    public IPageObject? Page => this;

    protected virtual Locator? ReadyLocator => null;

    public virtual bool IsLoaded(int? timeoutMs = null)
    {
        var readyLocator = ReadyLocator;
        if (readyLocator is null)
        {
            return true;
        }

        return Context.Driver.TryFindElement(
            readyLocator,
            out var element,
            timeoutMs ?? Context.Timeouts.PageLoad)
            && element is not null
            && element.Visible;
    }

    public virtual bool WaitLoaded(bool? expected, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return true;
        }

        return Poll(() => IsLoaded(0) == expected.Value, timeoutMs ?? Context.Timeouts.PageLoad);
    }

    public virtual void AssertLoaded(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return;
        }

        if (!WaitLoaded(expected, timeoutMs))
        {
            throw new PageLoadException(message ?? $"Expected page '{Name}' loaded state to be {expected}.");
        }
    }

    public virtual string? GetTitle(int? timeoutMs = null)
        => Context.Driver.GetCapability("appActivity") ?? Name;

    public virtual bool WaitTitle(string? expected, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return true;
        }

        return Poll(
            () => string.Equals(GetTitle(0), expected, StringComparison.Ordinal),
            timeoutMs ?? Context.Timeouts.PageLoad);
    }

    public virtual void AssertTitle(string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return;
        }

        var actual = GetTitle(timeoutMs);
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
        {
            throw new AssertionException(message ?? $"Expected title '{expected}', actual '{actual}'.", expected, actual);
        }
    }

    public virtual void TakeScreenshot(string? filename = null, int? timeoutMs = null)
    {
        var path = filename ?? Path.Combine(
            Environment.CurrentDirectory,
            "artifacts",
            $"{Name}-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.png");

        Context.Driver.SaveScreenshot(path);
    }

    public virtual bool IsReady(int? timeoutMs = null) => IsLoaded(timeoutMs);

    public virtual bool WaitReady(int? timeoutMs = null) => WaitLoaded(true, timeoutMs);

    public NativeAndroidElement? TryFindElement(Locator locator)
        => Context.Driver.TryFindElement(locator, out var element, 0) ? element : null;

    public NativeAndroidElement FindElement(Locator locator)
        => Context.Driver.FindElement(locator, Context.Timeouts.ElementFind);

    public IReadOnlyList<NativeAndroidElement> FindElements(Locator locator)
        => Context.Driver.FindElements(locator);

    public AndroidButton<TSelf> Button(Locator locator) => new(locator, Self);

    public AndroidText<TSelf> Text(Locator locator) => new(locator, Self);

    public AndroidEditText<TSelf> EditText(Locator locator) => new(locator, Self);

    public AndroidRecyclerView<TSelf> RecyclerView(Locator locator) => new(locator, Self);

    public AndroidToolbar<TSelf> Toolbar(Locator locator) => new(locator, Self);

    public AndroidDialog<TSelf> Dialog(Locator locator) => new(locator, Self);

    public AndroidPermissionDialog<TSelf> PermissionDialog() => new(Self);

    public AndroidTab<TSelf> Tab(Locator locator) => new(locator, Self);

    protected bool Poll(Func<bool> condition, int timeoutMs)
    {
        var deadline = DateTimeOffset.UtcNow.AddMilliseconds(timeoutMs);
        do
        {
            if (condition())
            {
                return true;
            }

            Thread.Sleep(Context.Timeouts.PollingInterval);
        }
        while (DateTimeOffset.UtcNow < deadline);

        return condition();
    }
}
