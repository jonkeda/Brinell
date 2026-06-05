namespace Brinell.NativeAndroid.Controls;

public sealed class AndroidTab<TScope> : AndroidButton<TScope>, ITabControlObject<TScope>
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

    public string Title => GetText() ?? Locator.Value;

    public bool? IsSelected()
        => TryFindElement()?.Selected;

    public bool WaitSelected(bool? expected, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return true;
        }

        return Poll(() => IsSelected() == expected.Value, timeoutMs ?? DefaultTimeoutMs);
    }

    public TScope AssertSelected(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return ContainingScope;
        }

        if (!WaitSelected(expected, timeoutMs))
        {
            Fail(message ?? $"Expected selected state to be {expected}, actual {IsSelected()}.", expected, IsSelected());
        }

        return ContainingScope;
    }
}
