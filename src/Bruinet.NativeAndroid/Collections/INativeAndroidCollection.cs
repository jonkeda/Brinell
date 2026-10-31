namespace Bruinet.NativeAndroid.Collections;

/// <summary>
/// A scope that hands out item rows: what an <see cref="ItemObjectBase{TSelf}"/> asks for its root.
/// </summary>
public interface INativeAndroidCollection : INativeAndroidElementScope
{
    /// <summary>The row view of <paramref name="key"/> now, or <c>null</c> when it is not shown. One attempt.</summary>
    INativeAndroidElement? TryGetItemRoot(ItemKey key);
}
