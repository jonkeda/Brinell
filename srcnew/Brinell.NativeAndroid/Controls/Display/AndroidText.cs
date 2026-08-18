namespace Brinell.NativeAndroid.Controls;

public class AndroidText<TScope> : NativeAndroidControl<TScope>, ITextControlObject<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    public AndroidText(Locator locator, INativeAndroidScope<TScope> scope)
        : base(locator, scope)
    {
    }

    public AndroidText(string locatorValue, INativeAndroidScope<TScope> scope)
        : base(locatorValue, scope)
    {
    }

    public bool WaitTextEquals(string? expected, int? timeoutMs = null)
        => WaitText(expected, timeoutMs);

    public bool? WaitTextContains(string? expected, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return null;
        }

        return Poll(
            () => GetText()?.Contains(expected, StringComparison.Ordinal) == true,
            timeoutMs ?? DefaultTimeoutMs);
    }

    public TScope AssertTextMatches(string? pattern, string? message = null, int? timeoutMs = null)
    {
        if (pattern is null)
        {
            return ContainingScope;
        }

        var passed = Poll(
            () => Regex.IsMatch(GetText() ?? string.Empty, pattern),
            timeoutMs ?? DefaultTimeoutMs);

        if (!passed)
        {
            Fail(message ?? $"Expected text to match '{pattern}', actual '{GetText()}'.", pattern, GetText());
        }

        return ContainingScope;
    }

    public TScope AssertTextStartsWith(string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return ContainingScope;
        }

        var passed = Poll(
            () => GetText()?.StartsWith(expected, StringComparison.Ordinal) == true,
            timeoutMs ?? DefaultTimeoutMs);

        if (!passed)
        {
            Fail(message ?? $"Expected text to start with '{expected}', actual '{GetText()}'.", expected, GetText());
        }

        return ContainingScope;
    }

    public TScope AssertTextEndsWith(string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return ContainingScope;
        }

        var passed = Poll(
            () => GetText()?.EndsWith(expected, StringComparison.Ordinal) == true,
            timeoutMs ?? DefaultTimeoutMs);

        if (!passed)
        {
            Fail(message ?? $"Expected text to end with '{expected}', actual '{GetText()}'.", expected, GetText());
        }

        return ContainingScope;
    }

    public TScope AssertTextEmpty(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return ContainingScope;
        }

        var passed = Poll(
            () => string.IsNullOrEmpty(GetText()) == expected.Value,
            timeoutMs ?? DefaultTimeoutMs);

        if (!passed)
        {
            Fail(message ?? $"Expected text empty state to be {expected}, actual '{GetText()}'.", expected, GetText());
        }

        return ContainingScope;
    }
}
