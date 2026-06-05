namespace Brinell.NativeAndroid.Controls;

public abstract class AndroidFocusableControlBase<TScope> : NativeAndroidControl<TScope>, IFocusableControlObject<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    protected AndroidFocusableControlBase(Locator locator, INativeAndroidScope<TScope> scope)
        : base(locator, scope)
    {
    }

    protected AndroidFocusableControlBase(string locatorValue, INativeAndroidScope<TScope> scope)
        : base(locatorValue, scope)
    {
    }

    public virtual bool? IsFocused()
    {
        var element = TryFindElement();
        if (element is null)
        {
            return null;
        }

        return AndroidToggleControlBase<TScope>.TryParseBoolean(element.GetAttribute("focused"));
    }

    public virtual TScope Focus(int? timeoutMs = null)
    {
        FindElementForAction(timeoutMs).Click();
        return ContainingScope;
    }

    public virtual TScope Blur(int? timeoutMs = null)
    {
        try
        {
            Context.Driver.ExecuteScript("mobile: hideKeyboard");
        }
        catch (WebDriverException)
        {
            Context.Driver.RawDriver.Navigate().Back();
        }

        return ContainingScope;
    }

    public virtual bool WaitFocused(bool? expected, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return true;
        }

        return Poll(() => IsFocused() == expected.Value, timeoutMs ?? DefaultTimeoutMs);
    }

    public virtual TScope AssertFocused(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return ContainingScope;
        }

        if (!WaitFocused(expected, timeoutMs))
        {
            Fail(message ?? $"Expected focused state to be {expected}, actual {IsFocused()}.", expected, IsFocused());
        }

        return ContainingScope;
    }
}
