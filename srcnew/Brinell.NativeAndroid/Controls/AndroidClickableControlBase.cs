namespace Brinell.NativeAndroid.Controls;

public abstract class AndroidClickableControlBase<TScope> : NativeAndroidControl<TScope>, IClickableControlObject<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    protected AndroidClickableControlBase(Locator locator, INativeAndroidScope<TScope> scope)
        : base(locator, scope)
    {
    }

    protected AndroidClickableControlBase(string locatorValue, INativeAndroidScope<TScope> scope)
        : base(locatorValue, scope)
    {
    }

    public virtual bool? IsClickable()
    {
        var element = TryFindElement();
        return element is null ? null : element.Visible && element.Enabled;
    }

    public virtual TScope Click(int? timeoutMs = null)
    {
        var element = FindElementForAction(timeoutMs);
        element.Click();
        return ContainingScope;
    }

    public virtual bool TryClick(int? timeoutMs = null)
    {
        try
        {
            Click(timeoutMs);
            return true;
        }
        catch (ElementNotFoundException)
        {
            return false;
        }
    }

    public virtual TScope DoubleClick(int? timeoutMs = null)
    {
        FindElementForAction(timeoutMs).DoubleClick();
        return ContainingScope;
    }

    public virtual TScope RightClick(int? timeoutMs = null)
    {
        FindElementForAction(timeoutMs).RightClick();
        return ContainingScope;
    }

    public virtual bool WaitClickable(bool? expected, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return true;
        }

        return Poll(
            () =>
            {
                var actual = IsClickable();
                return expected.Value ? actual == true : actual != true;
            },
            timeoutMs ?? DefaultTimeoutMs);
    }

    public virtual TScope AssertClickable(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return ContainingScope;
        }

        if (!WaitClickable(expected, timeoutMs))
        {
            Fail(message ?? $"Expected clickable state to be {expected}, actual {IsClickable()}.", expected, IsClickable());
        }

        return ContainingScope;
    }

    public virtual TScope Hover(int? timeoutMs = null)
    {
        FindElementForAction(timeoutMs).Hover();
        return ContainingScope;
    }

    public virtual TScope LongPress(int? durationMs = null, int? timeoutMs = null)
    {
        FindElementForAction(timeoutMs).LongPress(durationMs ?? 1000);
        return ContainingScope;
    }
}
