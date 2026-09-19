using System.Globalization;

namespace Brinell.Maui.Containers;

/// <summary>What an <see cref="ItemKey"/> identifies a row by, strongest first.</summary>
public enum ItemKeyKind
{
    /// <summary>The row's position in the data, from the platform (<c>PositionInSet</c>).</summary>
    Logical,

    /// <summary>
    /// The row's automation id: an id-based item strategy (<c>Task_0</c>, <c>Task_1</c>), or an id
    /// no other realized row carries.
    /// </summary>
    AutomationId,

    /// <summary>
    /// Where the row is among the realized rows. The weakest key: a row found again by position
    /// may hold another item, and nothing on the element can tell.
    /// </summary>
    Position
}

/// <summary>
/// Which item a row holds, recorded when the row object is created, so the row can be found again
/// and checked (<c>.my/stale-readiness/design.md</c>, section 7.6, Q7).
/// </summary>
/// <param name="Kind">What the key identifies the row by.</param>
/// <param name="Value">The logical index, automation id or position.</param>
public readonly record struct ItemKey(ItemKeyKind Kind, string Value)
{
    /// <summary>A logical-index key.</summary>
    public static ItemKey Logical(int index) => new(ItemKeyKind.Logical, index.ToString(CultureInfo.InvariantCulture));

    /// <summary>An automation-id key.</summary>
    public static ItemKey AutomationId(string automationId) => new(ItemKeyKind.AutomationId, automationId);

    /// <summary>A position key.</summary>
    public static ItemKey Position(int position) => new(ItemKeyKind.Position, position.ToString(CultureInfo.InvariantCulture));

    /// <summary>The index, for a logical or position key.</summary>
    public int Index => int.Parse(Value, CultureInfo.InvariantCulture);

    /// <summary>
    /// Whether <paramref name="itemRoot"/> still holds this item, as far as the element can say.
    /// </summary>
    /// <remarks>
    /// One read for a logical or id key. A position key cannot be checked, so it always answers
    /// yes. A removed element answers with <see cref="StaleElementException"/>.
    /// </remarks>
    public bool IsHeldBy(IMauiElement itemRoot) => Kind switch
    {
        ItemKeyKind.Logical => LogicalIndexOf(itemRoot) == Index,
        ItemKeyKind.AutomationId => itemRoot.AutomationId == Value,
        _ => true
    };

    /// <summary>
    /// The row's logical index (<c>PositionInSet</c>, 1-based on the platform), or null when the
    /// platform does not publish one.
    /// </summary>
    internal static int? LogicalIndexOf(IMauiElement itemRoot)
        => itemRoot.PositionInSet is > 0 and var position ? position - 1 : null;

    /// <summary>"logical 3", "id Task_3", "position 3", for messages.</summary>
    public override string ToString() => Kind switch
    {
        ItemKeyKind.Logical => $"logical {Value}",
        ItemKeyKind.AutomationId => $"id {Value}",
        _ => $"position {Value}"
    };
}
