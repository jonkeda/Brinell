namespace Bruinell.NativeAndroid.Collections;

/// <summary>How an item row is identified (design 7).</summary>
public enum ItemKeyKind
{
    /// <summary>By its item: a key read from the row. Survives scrolling and row recycling.</summary>
    Logical,

    /// <summary>By its place among the rows shown. Cannot tell a recycled row from its old item: use only on fixed lists.</summary>
    Position,
}

/// <summary>
/// Which item a row object stands for. A row re-finds its view by this key on every lookup, so it
/// follows its item when a <c>RecyclerView</c> rebinds its views, and never acts on another (R0).
/// </summary>
/// <param name="Kind">How the row is identified.</param>
/// <param name="Value">The key read from the row, or the position as text.</param>
public readonly record struct ItemKey(ItemKeyKind Kind, string Value)
{
    private static readonly Locator FirstText = Locator.ByClassName("android.widget.TextView");

    /// <summary>
    /// The key of <paramref name="row"/>: the text of its <paramref name="keyView"/> when the
    /// collection declares one, else its content description, else its first text, else its position.
    /// </summary>
    public static ItemKey Of(INativeAndroidElement row, int position, Locator? keyView = null)
    {
        ArgumentNullException.ThrowIfNull(row);
        var key = (keyView is null ? null : Present(row.TryFindElement(keyView)?.Text))
            ?? Present(row.ContentDescription)
            ?? Present(row.TryFindElement(FirstText)?.Text);
        return key is null
            ? new ItemKey(ItemKeyKind.Position, position.ToString(CultureInfo.InvariantCulture))
            : new ItemKey(ItemKeyKind.Logical, key);
    }

    /// <summary>"item 'Invoice 12'" or "row 3", for messages.</summary>
    public override string ToString() => Kind == ItemKeyKind.Logical ? $"item '{Value}'" : $"row {Value}";

    private static string? Present(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
