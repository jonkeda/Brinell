using Bruinell.Html.Interfaces;

namespace Bruinell.Html.Collections;

/// <summary>How an item row is identified (design 7.6).</summary>
public enum ItemKeyKind
{
    /// <summary>By its item: the row's <c>data-item-key</c>. Survives reordering and node recycling.</summary>
    Logical,

    /// <summary>By its place in the list. Cannot tell a recycled row from its old item: use only on fixed lists.</summary>
    Position,
}

/// <summary>
/// Which item a row object stands for. A row re-finds its element by this key on every lookup, so
/// it follows its item through a re-render and never acts on another (R0, design 7.6).
/// </summary>
/// <param name="Kind">How the row is identified.</param>
/// <param name="Value">The <c>data-item-key</c> value, or the position as text.</param>
public readonly record struct ItemKey(ItemKeyKind Kind, string Value)
{
    /// <summary>The attribute a list row carries its item's key in.</summary>
    public const string Attribute = "data-item-key";

    /// <summary>The key of <paramref name="row"/>: its <c>data-item-key</c>, or its position.</summary>
    public static ItemKey Of(IHtmlElement row, int position)
        => row.GetAttribute(Attribute) is { Length: > 0 } key
            ? new ItemKey(ItemKeyKind.Logical, key)
            : new ItemKey(ItemKeyKind.Position, position.ToString(System.Globalization.CultureInfo.InvariantCulture));

    /// <summary>"item 'b93c…'" or "row 3", for messages.</summary>
    public override string ToString() => Kind == ItemKeyKind.Logical ? $"item '{Value}'" : $"row {Value}";
}
