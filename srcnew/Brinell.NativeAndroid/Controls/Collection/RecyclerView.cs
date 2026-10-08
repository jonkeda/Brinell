using Brinell.NativeAndroid.Collections;

namespace Brinell.NativeAndroid.Controls.Collection;

/// <summary>
/// A <c>RecyclerView</c> whose rows are <see cref="ListItem"/>s. For rows with controls of their
/// own, derive from <see cref="CollectionObjectBase{TParent, TSelf, TItem}"/> with a row type.
/// </summary>
/// <typeparam name="TParent">The scope it is declared in.</typeparam>
public class RecyclerView<TParent> : CollectionObjectBase<TParent, RecyclerView<TParent>, ListItem>
    where TParent : INativeAndroidScope<TParent>
{
    private readonly Locator? _itemKey;

    /// <summary>The list found by <paramref name="locator"/>; <paramref name="itemKey"/> is the row view whose text keys a row.</summary>
    public RecyclerView(INativeAndroidScope<TParent> parent, Locator locator, Locator? itemKey = null)
        : base(parent, locator)
    {
        _itemKey = itemKey;
    }

    /// <summary>The list with resource id <paramref name="resourceId"/>; <paramref name="itemKeyId"/> is the row view whose text keys a row.</summary>
    public RecyclerView(INativeAndroidScope<TParent> parent, string resourceId, string? itemKeyId = null)
        : this(parent, Locator.ById(resourceId), itemKeyId is null ? null : Locator.ById(itemKeyId))
    {
    }

    /// <inheritdoc />
    public override RecyclerView<TParent> Self => this;

    /// <inheritdoc />
    protected override Locator? ItemKeyLocator => _itemKey;

    /// <inheritdoc />
    protected override ListItem CreateItem(ItemKey key, int position) => new(this, key, position);
}

/// <summary>A <c>ListView</c> whose rows are <see cref="ListItem"/>s; the same as <see cref="RecyclerView{TParent}"/>.</summary>
/// <typeparam name="TParent">The scope it is declared in.</typeparam>
public class ListView<TParent> : CollectionObjectBase<TParent, ListView<TParent>, ListItem>
    where TParent : INativeAndroidScope<TParent>
{
    private readonly Locator? _itemKey;

    /// <summary>The list found by <paramref name="locator"/>; <paramref name="itemKey"/> is the row view whose text keys a row.</summary>
    public ListView(INativeAndroidScope<TParent> parent, Locator locator, Locator? itemKey = null)
        : base(parent, locator)
    {
        _itemKey = itemKey;
    }

    /// <summary>The list with resource id <paramref name="resourceId"/>; <paramref name="itemKeyId"/> is the row view whose text keys a row.</summary>
    public ListView(INativeAndroidScope<TParent> parent, string resourceId, string? itemKeyId = null)
        : this(parent, Locator.ById(resourceId), itemKeyId is null ? null : Locator.ById(itemKeyId))
    {
    }

    /// <inheritdoc />
    public override ListView<TParent> Self => this;

    /// <inheritdoc />
    protected override Locator? ItemKeyLocator => _itemKey;

    /// <inheritdoc />
    protected override ListItem CreateItem(ItemKey key, int position) => new(this, key, position);
}
