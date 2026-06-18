namespace Brinell.NativeAndroid.Controls;

public class AndroidEditText<TScope> : AndroidText<TScope>, IEditableTextControlObject<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    public AndroidEditText(Locator locator, INativeAndroidScope<TScope> scope)
        : base(locator, scope)
    {
    }

    public AndroidEditText(string locatorValue, INativeAndroidScope<TScope> scope)
        : base(locatorValue, scope)
    {
    }

    public TScope Enter(string? text, int? timeoutMs = null)
    {
        if (text is null)
        {
            return ContainingScope;
        }

        FindElementForAction(timeoutMs).SendKeys(text);
        return ContainingScope;
    }

    public TScope Clear(int? timeoutMs = null)
    {
        FindElementForAction(timeoutMs).Clear();
        return ContainingScope;
    }

    public TScope SetText(string? text, int? timeoutMs = null)
    {
        if (text is null)
        {
            return ContainingScope;
        }

        var element = FindElementForAction(timeoutMs);
        element.Clear();
        element.SendKeys(text, TextInputMethod.SetValue);
        return ContainingScope;
    }

    public TScope Append(string? text, int? timeoutMs = null)
    {
        if (text is null)
        {
            return ContainingScope;
        }

        return Enter(text, timeoutMs);
    }

    public string? GetPlaceholder()
        => GetAttribute("hint", null);

    public bool WaitPlaceholder(string? expected, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return true;
        }

        return Poll(
            () => string.Equals(GetPlaceholder(), expected, StringComparison.Ordinal),
            timeoutMs ?? DefaultTimeoutMs);
    }

    public TScope AssertPlaceholder(string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return ContainingScope;
        }

        if (!WaitPlaceholder(expected, timeoutMs))
        {
            Fail(message ?? $"Expected placeholder '{expected}', actual '{GetPlaceholder()}'.", expected, GetPlaceholder());
        }

        return ContainingScope;
    }

    public bool? IsReadOnly()
    {
        var element = TryFindElement();
        if (element is null)
        {
            return null;
        }

        var focusable = element.GetAttribute("focusable");
        var enabled = element.GetAttribute("enabled");
        return string.Equals(focusable, "false", StringComparison.OrdinalIgnoreCase)
            || string.Equals(enabled, "false", StringComparison.OrdinalIgnoreCase)
            || !element.Enabled;
    }

    public bool WaitReadOnly(bool? expected, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return true;
        }

        return Poll(
            () => IsReadOnly() == expected.Value,
            timeoutMs ?? DefaultTimeoutMs);
    }

    public TScope AssertReadOnly(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return ContainingScope;
        }

        if (!WaitReadOnly(expected, timeoutMs))
        {
            Fail(message ?? $"Expected readonly state to be {expected}, actual {IsReadOnly()}.", expected, IsReadOnly());
        }

        return ContainingScope;
    }
}
