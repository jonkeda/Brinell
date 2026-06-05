namespace Brinell.NativeAndroid.Controls;

public abstract class AndroidProgressControlBase<TScope> : NativeAndroidControl<TScope>, IProgressControlObject<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    protected AndroidProgressControlBase(Locator locator, INativeAndroidScope<TScope> scope)
        : base(locator, scope)
    {
    }

    protected AndroidProgressControlBase(string locatorValue, INativeAndroidScope<TScope> scope)
        : base(locatorValue, scope)
    {
    }

    public virtual bool? IsIndeterminate()
    {
        var element = TryFindElement();
        if (element is null)
        {
            return null;
        }

        var parsed = AndroidToggleControlBase<TScope>.TryParseBoolean(element.GetAttribute("indeterminate"));
        if (parsed is not null)
        {
            return parsed;
        }

        return GetProgressCore(element) is null && element.Visible;
    }

    public virtual double? GetProgress()
    {
        var element = TryFindElement();
        return GetProgressCore(element);
    }

    public virtual bool WaitProgress(double? expected, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return true;
        }

        return Poll(
            () =>
            {
                var actual = GetProgress();
                return actual is not null && Math.Abs(actual.Value - expected.Value) <= 0.001;
            },
            timeoutMs ?? DefaultTimeoutMs);
    }

    public virtual TScope AssertProgress(double? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return ContainingScope;
        }

        if (!WaitProgress(expected, timeoutMs))
        {
            Fail(message ?? $"Expected progress '{expected}', actual '{GetProgress()}'.", expected, GetProgress());
        }

        return ContainingScope;
    }

    public virtual bool WaitComplete(int? timeoutMs = null)
    {
        return Poll(
            () =>
            {
                if (!IsExists())
                {
                    return true;
                }

                var progress = GetProgress();
                return progress is not null && progress >= 1.0;
            },
            timeoutMs ?? DefaultTimeoutMs);
    }

    public virtual TScope AssertComplete(string? message = null, int? timeoutMs = null)
    {
        if (!WaitComplete(timeoutMs))
        {
            Fail(message ?? $"Expected progress to complete, actual '{GetProgress()}'.", 1.0, GetProgress());
        }

        return ContainingScope;
    }

    protected virtual double? GetProgressCore(NativeAndroidElement? element)
    {
        if (element is null)
        {
            return null;
        }

        var rawValue = AndroidRangeControlBase<TScope>.FirstDouble(
            element.GetAttribute("progress"),
            element.GetAttribute("value"),
            element.GetAttribute("content-desc"),
            element.Text);

        if (rawValue is null)
        {
            return null;
        }

        if (rawValue.Value is >= 0 and <= 1)
        {
            return rawValue.Value;
        }

        var max = AndroidRangeControlBase<TScope>.FirstDouble(
            element.GetAttribute("max"),
            element.GetAttribute("maximum"))
            ?? 100;

        return max <= 0 ? null : Math.Clamp(rawValue.Value / max, 0, 1);
    }
}
