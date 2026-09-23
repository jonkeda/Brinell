namespace Brinell.Presenter.Controls;

/// <summary>
/// Reads the Presenter palette by key so the tree controls pick up theme colours without a
/// compile-time reference to the resource dictionary.
/// </summary>
internal static class TreeViewColors
{
    /// <summary>The colour behind <paramref name="key"/>, or <paramref name="fallback"/> when the key is absent.</summary>
    public static Color Resource(string key, string fallback) =>
        Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color
            ? color
            : Color.FromArgb(fallback);
}
