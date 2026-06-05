namespace Brinell.NativeAndroid.Controls;

public abstract class AndroidExpandableControlBase<TScope> : AndroidClickableControlBase<TScope>, IExpandableControlObject<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    protected AndroidExpandableControlBase(Locator locator, INativeAndroidScope<TScope> scope)
        : base(locator, scope)
    {
    }

    protected AndroidExpandableControlBase(string locatorValue, INativeAndroidScope<TScope> scope)
        : base(locatorValue, scope)
    {
    }

    public virtual bool? IsExpanded()
    {
        var element = TryFindElement();
        if (element is null)
        {
            return null;
        }

        foreach (var attribute in new[] { "expanded", "checked", "selected" })
        {
            var parsed = AndroidToggleControlBase<TScope>.TryParseBoolean(element.GetAttribute(attribute));
            if (parsed is not null)
            {
                return parsed;
            }
        }

        return null;
    }

    public virtual TScope Expand(int? timeoutMs = null)
        => SetExpanded(true, timeoutMs);

    public virtual TScope Collapse(int? timeoutMs = null)
        => SetExpanded(false, timeoutMs);

    public virtual TScope ToggleExpanded(int? timeoutMs = null)
    {
        Click(timeoutMs);
        return ContainingScope;
    }

    public virtual bool WaitExpanded(bool? expected, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return true;
        }

        return Poll(() => IsExpanded() == expected.Value, timeoutMs ?? DefaultTimeoutMs);
    }

    public virtual TScope AssertExpanded(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return ContainingScope;
        }

        if (!WaitExpanded(expected, timeoutMs))
        {
            Fail(message ?? $"Expected expanded state to be {expected}, actual {IsExpanded()}.", expected, IsExpanded());
        }

        return ContainingScope;
    }

    protected virtual TScope SetExpanded(bool expanded, int? timeoutMs = null)
    {
        var current = IsExpanded();
        if (current != expanded)
        {
            ToggleExpanded(timeoutMs);
            WaitExpanded(expanded, timeoutMs ?? 500);
        }

        return ContainingScope;
    }
}
