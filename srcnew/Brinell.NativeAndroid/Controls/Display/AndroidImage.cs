namespace Brinell.NativeAndroid.Controls;

public sealed class AndroidImage<TScope> : NativeAndroidControl<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    public AndroidImage(Locator locator, INativeAndroidScope<TScope> scope)
        : base(locator, scope)
    {
    }

    public AndroidImage(string locatorValue, INativeAndroidScope<TScope> scope)
        : base(locatorValue, scope)
    {
    }

    public string? GetContentDescription()
        => GetAttribute("content-desc");

    public TScope AssertContentDescription(string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return ContainingScope;
        }

        var passed = Poll(
            () => string.Equals(GetContentDescription(), expected, StringComparison.Ordinal),
            timeoutMs ?? DefaultTimeoutMs);

        if (!passed)
        {
            Fail(message ?? $"Expected content description '{expected}', actual '{GetContentDescription()}'.", expected, GetContentDescription());
        }

        return ContainingScope;
    }
}
