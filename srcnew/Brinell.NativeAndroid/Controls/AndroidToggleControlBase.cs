namespace Brinell.NativeAndroid.Controls;

public abstract class AndroidToggleControlBase<TScope> : AndroidClickableControlBase<TScope>, IToggleControlObject<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    protected AndroidToggleControlBase(Locator locator, INativeAndroidScope<TScope> scope)
        : base(locator, scope)
    {
    }

    protected AndroidToggleControlBase(string locatorValue, INativeAndroidScope<TScope> scope)
        : base(locatorValue, scope)
    {
    }

    public virtual bool? IsChecked()
    {
        var element = TryFindElement();
        if (element is null)
        {
            return null;
        }

        foreach (var attribute in new[] { "checked", "selected", "isChecked", "IsChecked", "value" })
        {
            var parsed = TryParseBoolean(element.GetAttribute(attribute));
            if (parsed is not null)
            {
                return parsed;
            }
        }

        return element.Selected;
    }

    public virtual TScope Toggle(int? timeoutMs = null)
    {
        Click(timeoutMs);
        return ContainingScope;
    }

    public virtual TScope SetChecked(bool? @checked, int? timeoutMs = null)
    {
        if (@checked is null)
        {
            return ContainingScope;
        }

        var current = IsChecked();
        if (current != @checked.Value)
        {
            Toggle(timeoutMs);
            WaitChecked(@checked.Value, timeoutMs ?? 500);
        }

        return ContainingScope;
    }

    public virtual TScope Check(int? timeoutMs = null)
        => SetChecked(true, timeoutMs);

    public virtual TScope Uncheck(int? timeoutMs = null)
        => SetChecked(false, timeoutMs);

    public virtual bool WaitChecked(bool? expected, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return true;
        }

        return Poll(() => IsChecked() == expected.Value, timeoutMs ?? DefaultTimeoutMs);
    }

    public virtual TScope AssertChecked(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return ContainingScope;
        }

        if (!WaitChecked(expected, timeoutMs))
        {
            Fail(message ?? $"Expected checked state to be {expected}, actual {IsChecked()}.", expected, IsChecked());
        }

        return ContainingScope;
    }

    internal static bool? TryParseBoolean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim() switch
        {
            "1" => true,
            "0" => false,
            var text when bool.TryParse(text, out var parsed) => parsed,
            var text when text.Equals("on", StringComparison.OrdinalIgnoreCase) => true,
            var text when text.Equals("off", StringComparison.OrdinalIgnoreCase) => false,
            var text when text.Equals("checked", StringComparison.OrdinalIgnoreCase) => true,
            var text when text.Equals("unchecked", StringComparison.OrdinalIgnoreCase) => false,
            _ => null
        };
    }
}
