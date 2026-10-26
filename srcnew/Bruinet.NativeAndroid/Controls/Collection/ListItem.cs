using Bruinet.NativeAndroid.Collections;
using Bruinet.NativeAndroid.Controls.Buttons;
using Bruinet.NativeAndroid.Controls.Text;
using Bruinet.NativeAndroid.Controls.Toggle;

namespace Bruinet.NativeAndroid.Controls.Collection;

/// <summary>
/// A row of a <see cref="RecyclerView{TParent}"/> or <see cref="ListView{TParent}"/>, when the page
/// object needs no row type of its own: the row as a button, and its views by resource id.
/// </summary>
public sealed class ListItem : ItemObjectBase<ListItem>
{
    /// <summary>The row of <paramref name="key"/> in <paramref name="collection"/>.</summary>
    public ListItem(INativeAndroidCollection collection, ItemKey key, int position)
        : base(collection, key, position)
    {
    }

    /// <inheritdoc />
    public override ListItem Self => this;

    /// <summary>The row itself, to tap or long-press.</summary>
    public Button<ListItem> Row => new(this, Locator.ByXPath("."));

    /// <summary>A text in the row, by resource id.</summary>
    public TextView<ListItem> Text(string resourceId) => new(this, resourceId);

    /// <summary>A button in the row, by resource id.</summary>
    public Button<ListItem> Button(string resourceId) => new(this, resourceId);

    /// <summary>A check box in the row, by resource id.</summary>
    public CheckBox<ListItem> CheckBox(string resourceId) => new(this, resourceId);
}
