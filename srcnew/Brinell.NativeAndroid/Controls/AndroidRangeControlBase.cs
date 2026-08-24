namespace Brinell.NativeAndroid.Controls;

public abstract class AndroidRangeControlBase<TScope> : NativeAndroidControl<TScope>, IRangeControlObject<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    protected AndroidRangeControlBase(Locator locator, INativeAndroidScope<TScope> scope)
        : base(locator, scope)
    {
    }

    protected AndroidRangeControlBase(string locatorValue, INativeAndroidScope<TScope> scope)
        : base(locatorValue, scope)
    {
    }

    public virtual double? GetValue(int? timeoutMs = null)
    {
        var element = GetElementForRead(timeoutMs);
        return GetValueCore(element);
    }

    public virtual TScope SetValue(double? value, int? timeoutMs = null)
    {
        if (value is null)
        {
            return ContainingScope;
        }

        var element = FindElementForAction(timeoutMs);
        SetValueCore(element, value.Value);
        return ContainingScope;
    }

    public virtual double? GetMinimum(int? timeoutMs = null)
    {
        var element = GetElementForRead(timeoutMs);
        return GetMinimumCore(element);
    }

    public virtual double? GetMaximum(int? timeoutMs = null)
    {
        var element = GetElementForRead(timeoutMs);
        return GetMaximumCore(element);
    }

    public virtual double? GetStep(int? timeoutMs = null)
    {
        var element = GetElementForRead(timeoutMs);
        return GetStepCore(element);
    }

    public virtual TScope Increment(int? timeoutMs = null)
    {
        var current = GetValue(timeoutMs) ?? GetMinimum(timeoutMs) ?? 0;
        SetValue(current + (GetStep(timeoutMs) ?? 1), timeoutMs);
        return ContainingScope;
    }

    public virtual TScope Decrement(int? timeoutMs = null)
    {
        var current = GetValue(timeoutMs) ?? GetMinimum(timeoutMs) ?? 0;
        SetValue(current - (GetStep(timeoutMs) ?? 1), timeoutMs);
        return ContainingScope;
    }

    public virtual bool? WaitValue(double? expected, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return null;
        }

        return WaitValueWithin(expected, 0, timeoutMs);
    }

    public virtual TScope AssertValue(double? expected, string? message = null, int? timeoutMs = null)
    {
        return AssertValueWithin(expected, 0, message, timeoutMs);
    }

    public virtual bool WaitValueWithin(double? expected, double tolerance, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return true;
        }

        return Poll(
            () =>
            {
                var actual = GetValue();
                return actual is not null && Math.Abs(actual.Value - expected.Value) <= tolerance;
            },
            timeoutMs ?? DefaultTimeoutMs);
    }

    public virtual TScope AssertValueWithin(double? expected, double tolerance, string? message = null, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return ContainingScope;
        }

        if (!WaitValueWithin(expected, tolerance, timeoutMs))
        {
            Fail(message ?? $"Expected value '{expected}' (+/- {tolerance}), actual '{GetValue()}'.", expected, GetValue());
        }

        return ContainingScope;
    }

    protected virtual double? GetValueCore(NativeAndroidElement? element)
    {
        if (element is null)
        {
            return null;
        }

        return FirstDouble(
            element.GetAttribute("value"),
            element.GetAttribute("progress"),
            element.GetAttribute("RangeValue.Value"),
            element.GetAttribute("content-desc"),
            element.Text);
    }

    protected virtual void SetValueCore(NativeAndroidElement element, double value)
    {
        var minimum = GetMinimumCore(element) ?? 0;
        var maximum = GetMaximumCore(element) ?? 100;

        if (maximum > minimum)
        {
            var percent = Math.Clamp((value - minimum) / (maximum - minimum), 0, 1);
            var rect = element.Rect;
            var y = rect.Top + (rect.Height / 2);
            var startX = rect.Left + Math.Max(1, rect.Width / 2);
            var endX = rect.Left + Math.Max(1, (int)Math.Round(rect.Width * percent));
            element.Swipe(startX, y, endX, y);
            return;
        }

        var text = value.ToString(CultureInfo.InvariantCulture);
        try
        {
            element.Clear();
            element.SendKeys(text, TextInputMethod.SetValue);
        }
        catch (WebDriverException)
        {
            element.SendKeys(text);
        }
    }

    protected virtual double? GetMinimumCore(NativeAndroidElement? element)
    {
        if (element is null)
        {
            return null;
        }

        return FirstDouble(
            element.GetAttribute("min"),
            element.GetAttribute("minimum"),
            element.GetAttribute("RangeValue.Minimum"));
    }

    protected virtual double? GetMaximumCore(NativeAndroidElement? element)
    {
        if (element is null)
        {
            return null;
        }

        return FirstDouble(
            element.GetAttribute("max"),
            element.GetAttribute("maximum"),
            element.GetAttribute("RangeValue.Maximum"));
    }

    protected virtual double? GetStepCore(NativeAndroidElement? element)
    {
        if (element is null)
        {
            return null;
        }

        return FirstDouble(
            element.GetAttribute("step"),
            element.GetAttribute("smallChange"),
            element.GetAttribute("RangeValue.SmallChange"))
            ?? 1;
    }

    internal static double? FirstDouble(params string?[] values)
    {
        foreach (var value in values)
        {
            var parsed = ParseDouble(value);
            if (parsed is not null)
            {
                return parsed;
            }
        }

        return null;
    }

    internal static double? ParseDouble(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            || double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out parsed))
        {
            return parsed;
        }

        var match = Regex.Match(value, @"-?\d+(?:[\.,]\d+)?");
        if (!match.Success)
        {
            return null;
        }

        var normalized = match.Value.Replace(',', '.');
        return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed)
            ? parsed
            : null;
    }

    private NativeAndroidElement? GetElementForRead(int? timeoutMs)
    {
        if (timeoutMs is null)
        {
            return TryFindElement();
        }

        try
        {
            return FindElement(timeoutMs.Value);
        }
        catch (ElementNotFoundException)
        {
            return null;
        }
    }
}
