namespace Brinell.NativeAndroid.Interfaces;

/// <summary>Helpers over <see cref="INativeAndroidElement"/>.</summary>
public static class NativeAndroidElementExtensions
{
    // An extension, not an interface member, so a mocked element needs only TryFindElement set up
    // (the same choice as MAUI's and Html's).

    /// <summary>The first matching descendant. One attempt; never waits.</summary>
    /// <exception cref="ElementNotFoundException">Nothing matches.</exception>
    public static INativeAndroidElement FindElement(this INativeAndroidElement element, Locator locator)
    {
        ArgumentNullException.ThrowIfNull(element);
        return element.TryFindElement(locator) ?? throw new ElementNotFoundException(locator);
    }
}
