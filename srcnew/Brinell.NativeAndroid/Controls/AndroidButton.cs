namespace Brinell.NativeAndroid.Controls;

public class AndroidButton<TScope> : NativeAndroidControl<TScope>, IClickableControlObject<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    public AndroidButton(Locator locator, INativeAndroidScope<TScope> scope)
        : base(locator, scope)
    {
    }

    public AndroidButton(string locatorValue, INativeAndroidScope<TScope> scope)
        : base(locatorValue, scope)
    {
    }

    public bool? IsClickable()
    {
        var element = TryFindElement();
        return element is null ? null : element.Visible && element.Enabled;
    }

    public TScope Click(int? timeoutMs = null)
    {
        var element = FindElementForAction(timeoutMs);
        element.Click();
        return ContainingScope;
    }

    public bool TryClick(int? timeoutMs = null)
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

    public TScope DoubleClick(int? timeoutMs = null)
    {
        FindElementForAction(timeoutMs).DoubleClick();
        return ContainingScope;
    }

    public TScope RightClick(int? timeoutMs = null)
    {
        FindElementForAction(timeoutMs).RightClick();
        return ContainingScope;
    }

    public bool WaitClickable(bool? expected, int? timeoutMs = null)
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

    public TScope AssertClickable(bool? expected, string? message = null, int? timeoutMs = null)
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

    public TScope Hover(int? timeoutMs = null)
    {
        FindElementForAction(timeoutMs).Hover();
        return ContainingScope;
    }

    public TScope LongPress(int? durationMs = null, int? timeoutMs = null)
    {
        FindElementForAction(timeoutMs).LongPress(durationMs ?? 1000);
        return ContainingScope;
    }
}
