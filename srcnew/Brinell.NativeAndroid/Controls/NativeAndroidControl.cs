namespace Brinell.NativeAndroid.Controls;

public class NativeAndroidControl<TScope> : ControlObjectBase<TScope>, IControlObject<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    private readonly INativeAndroidScope<TScope> nativeScope;

    public NativeAndroidControl(Locator locator, INativeAndroidScope<TScope> scope)
        : base(locator, scope)
    {
        nativeScope = scope ?? throw new ArgumentNullException(nameof(scope));
    }

    public NativeAndroidControl(string locatorValue, INativeAndroidScope<TScope> scope)
        : this(new Locator(scope?.DefaultLocatorStrategy ?? LocatorStrategy.Id, locatorValue), scope!)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(locatorValue);
    }

    protected TScope ContainingScope => nativeScope.Self;

    protected NativeAndroidTestContext Context => nativeScope.Context;

    protected int DefaultTimeoutMs => Context.Timeouts.DefaultWait;

    protected int PollingIntervalMs => Context.Timeouts.PollingInterval;

    protected virtual NativeAndroidElement? TryFindElement()
        => nativeScope.TryFindElement(Locator);

    protected virtual NativeAndroidElement FindElement(int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? Context.Timeouts.ElementFind;
        return Context.Driver.FindElement(Locator, timeout);
    }

    protected virtual NativeAndroidElement FindElementForAction(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs ?? DefaultTimeoutMs);
        if (!element.Visible)
        {
            element.ScrollIntoView(timeoutMs ?? DefaultTimeoutMs);
        }

        return element;
    }

    protected bool Poll(Func<bool> condition, int timeoutMs)
    {
        var deadline = DateTimeOffset.UtcNow.AddMilliseconds(timeoutMs);
        do
        {
            try
            {
                if (condition())
                {
                    return true;
                }
            }
            catch (WebDriverException)
            {
                // Appium state can be transient while a native screen settles.
            }
            catch (ElementNotFoundException)
            {
            }

            Thread.Sleep(PollingIntervalMs);
        }
        while (DateTimeOffset.UtcNow < deadline);

        return condition();
    }

    protected TScope Fail(string message, object? expected = null, object? actual = null)
        => throw new AssertionException(message, expected, actual, Locator.ToString());

    public virtual bool IsExists() => TryFindElement() is not null;

    public virtual bool? IsVisible() => TryFindElement()?.Visible;

    public virtual bool? IsEnabled() => TryFindElement()?.Enabled;

    public virtual bool WaitExists(bool? expected, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return true;
        }

        return Poll(() => IsExists() == expected.Value, timeoutMs ?? DefaultTimeoutMs);
    }

    public virtual bool WaitVisible(bool? expected, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return true;
        }

        return Poll(
            () =>
            {
                var actual = IsVisible();
                return expected.Value ? actual == true : actual != true;
            },
            timeoutMs ?? DefaultTimeoutMs);
    }

    public virtual bool WaitEnabled(bool? expected, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return true;
        }

        return Poll(
            () =>
            {
                var actual = IsEnabled();
                return expected.Value ? actual == true : actual != true;
            },
            timeoutMs ?? DefaultTimeoutMs);
    }

    public virtual TScope AssertExists(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return ContainingScope;
        }

        if (!WaitExists(expected, timeoutMs))
        {
            Fail(message ?? $"Expected existence to be {expected}, actual {IsExists()}.", expected, IsExists());
        }

        return ContainingScope;
    }

    public virtual TScope AssertVisible(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return ContainingScope;
        }

        if (!WaitVisible(expected, timeoutMs))
        {
            Fail(message ?? $"Expected visibility to be {expected}, actual {IsVisible()}.", expected, IsVisible());
        }

        return ContainingScope;
    }

    public virtual TScope AssertEnabled(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return ContainingScope;
        }

        if (!WaitEnabled(expected, timeoutMs))
        {
            Fail(message ?? $"Expected enabled state to be {expected}, actual {IsEnabled()}.", expected, IsEnabled());
        }

        return ContainingScope;
    }

    public virtual string? GetText(int? timeoutMs = null)
    {
        if (timeoutMs is null)
        {
            return TryFindElement()?.Text;
        }

        try
        {
            return FindElement(timeoutMs).Text;
        }
        catch (ElementNotFoundException)
        {
            return null;
        }
    }

    public virtual bool WaitText(string? expected, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return true;
        }

        return Poll(
            () => string.Equals(GetText(), expected, StringComparison.Ordinal),
            timeoutMs ?? DefaultTimeoutMs);
    }

    public virtual TScope AssertText(string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return ContainingScope;
        }

        if (!WaitText(expected, timeoutMs))
        {
            Fail(message ?? $"Expected text '{expected}', actual '{GetText()}'.", expected, GetText());
        }

        return ContainingScope;
    }

    public virtual TScope AssertTextContains(string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return ContainingScope;
        }

        var passed = Poll(
            () => GetText()?.Contains(expected, StringComparison.Ordinal) == true,
            timeoutMs ?? DefaultTimeoutMs);

        if (!passed)
        {
            Fail(message ?? $"Expected text to contain '{expected}', actual '{GetText()}'.", expected, GetText());
        }

        return ContainingScope;
    }

    public virtual string? GetAttribute(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return TryFindElement()?.GetAttribute(name);
    }
}
