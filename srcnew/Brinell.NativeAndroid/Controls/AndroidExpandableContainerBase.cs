namespace Brinell.NativeAndroid.Controls;

public abstract class AndroidExpandableContainerBase<TParent, TSelf> :
    AndroidContainerBase<TParent, TSelf>,
    IExpandableControlObject<TParent>
    where TParent : INativeAndroidScope<TParent>
    where TSelf : AndroidExpandableContainerBase<TParent, TSelf>
{
    protected AndroidExpandableContainerBase(Locator locator, INativeAndroidScope<TParent> parentScope)
        : base(locator, parentScope)
    {
    }

    protected AndroidExpandableContainerBase(string locatorValue, INativeAndroidScope<TParent> parentScope)
        : base(locatorValue, parentScope)
    {
    }

    public virtual bool? IsClickable()
        => IsVisible() == true && IsEnabled() == true;

    public virtual TParent Click(int? timeoutMs = null)
    {
        FindElementForAction(timeoutMs).Click();
        return ContainingScope;
    }

    public virtual TParent DoubleClick(int? timeoutMs = null)
    {
        FindElementForAction(timeoutMs).DoubleClick();
        return ContainingScope;
    }

    public virtual TParent RightClick(int? timeoutMs = null)
    {
        FindElementForAction(timeoutMs).RightClick();
        return ContainingScope;
    }

    public virtual TParent Hover(int? timeoutMs = null)
    {
        FindElementForAction(timeoutMs).Hover();
        return ContainingScope;
    }

    public virtual TParent LongPress(int? durationMs = null, int? timeoutMs = null)
    {
        FindElementForAction(timeoutMs).LongPress(durationMs ?? 1000);
        return ContainingScope;
    }

    public virtual bool WaitClickable(bool? expected, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return true;
        }

        return Poll(() => IsClickable() == expected.Value, timeoutMs ?? DefaultTimeoutMs);
    }

    public virtual TParent AssertClickable(bool? expected, string? message = null, int? timeoutMs = null)
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

    public virtual bool? IsExpanded()
    {
        foreach (var attribute in new[] { "expanded", "checked", "selected" })
        {
            var parsed = AndroidToggleControlBase<TParent>.TryParseBoolean(ContainerRoot.GetAttribute(attribute));
            if (parsed is not null)
            {
                return parsed;
            }
        }

        return null;
    }

    public virtual TParent Expand(int? timeoutMs = null)
        => SetExpanded(true, timeoutMs);

    public virtual TParent Collapse(int? timeoutMs = null)
        => SetExpanded(false, timeoutMs);

    public virtual TParent ToggleExpanded(int? timeoutMs = null)
        => Click(timeoutMs);

    public virtual bool WaitExpanded(bool? expected, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return true;
        }

        return Poll(() => IsExpanded() == expected.Value, timeoutMs ?? DefaultTimeoutMs);
    }

    public virtual TParent AssertExpanded(bool? expected, string? message = null, int? timeoutMs = null)
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

    protected virtual TParent SetExpanded(bool expanded, int? timeoutMs)
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
