namespace Brinell.NativeAndroid.Controls;

public sealed class AndroidTab<TScope> : AndroidButton<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    public AndroidTab(Locator locator, INativeAndroidScope<TScope> scope)
        : base(locator, scope)
    {
    }

    public AndroidTab(string locatorValue, INativeAndroidScope<TScope> scope)
        : base(locatorValue, scope)
    {
    }

    public bool? IsSelected()
        => TryFindElement()?.Selected;

    public TScope AssertSelected(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return ContainingScope;
        }

        var passed = Poll(
            () => IsSelected() == expected.Value,
            timeoutMs ?? DefaultTimeoutMs);

        if (!passed)
        {
            Fail(message ?? $"Expected selected state to be {expected}, actual {IsSelected()}.", expected, IsSelected());
        }

        return ContainingScope;
    }
}
