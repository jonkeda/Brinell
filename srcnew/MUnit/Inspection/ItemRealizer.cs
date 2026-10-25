using System.Runtime.CompilerServices;
using Microsoft.Maui.Controls;

namespace MUnit.Inspection;

/// <summary>
/// Makes the views a <see cref="ItemsView"/> would show: every item through its template, no
/// virtualization. Without a platform handler a CollectionView has no item views at all.
/// </summary>
/// <remarks>
/// A view is kept per item, by reference, so state typed into a row survives the next query; a row
/// whose item left the source is dropped. Grouping is not realized yet (see the proposal's roadmap).
/// </remarks>
internal static class ItemRealizer
{
    private static readonly ConditionalWeakTable<ItemsView, Dictionary<object, View>> Rows = new();
    private static readonly object NullItem = new();

    public static IEnumerable<Element> Realize(ItemsView itemsView)
    {
        if (itemsView is StructuredItemsView { Header: View header })
            yield return Adopt(header, itemsView);

        var items = itemsView.ItemsSource?.Cast<object?>().ToList() ?? [];
        if (items.Count == 0)
        {
            if (Empty(itemsView) is { } empty)
                yield return empty;
        }
        else
        {
            foreach (var row in RowsFor(itemsView, items))
                yield return row;
        }

        if (itemsView is StructuredItemsView { Footer: View footer })
            yield return Adopt(footer, itemsView);
    }

    /// <summary>The item a realized row shows, or null when <paramref name="row"/> is no row.</summary>
    public static bool TryGetItem(Element row, out object? item)
    {
        item = null;
        if (row.Parent is not ItemsView itemsView || !Rows.TryGetValue(itemsView, out var rows))
            return false;

        foreach (var (key, view) in rows)
        {
            if (ReferenceEquals(view, row))
            {
                item = ReferenceEquals(key, NullItem) ? null : key;
                return true;
            }
        }

        return false;
    }

    private static List<View> RowsFor(ItemsView itemsView, List<object?> items)
    {
        var rows = Rows.GetValue(itemsView, _ => new Dictionary<object, View>(ReferenceEqualityComparer.Instance));
        var keys = items.Select(item => item ?? NullItem).ToList();
        foreach (var gone in rows.Keys.Except(keys, ReferenceEqualityComparer.Instance).ToList())
            rows.Remove(gone);

        var result = new List<View>(items.Count);
        foreach (var key in keys)
        {
            if (!rows.TryGetValue(key, out var view))
            {
                var item = ReferenceEquals(key, NullItem) ? null : key;
                view = Make(itemsView, item);
                rows[key] = view;
            }

            result.Add(view);
        }

        return result;
    }

    private static View Make(ItemsView itemsView, object? item)
    {
        var template = itemsView.ItemTemplate;
        if (template is DataTemplateSelector selector)
            template = selector.SelectTemplate(item, itemsView);

        // Without a template the platforms show the item's ToString().
        var view = template?.CreateContent() as View ?? new Label { Text = item?.ToString() };
        view.BindingContext = item;
        return Adopt(view, itemsView);
    }

    private static View? Empty(ItemsView itemsView)
    {
        switch (itemsView.EmptyView)
        {
            case View view:
                return Adopt(view, itemsView);
            case string text:
                var label = EmptyLabels.GetValue(itemsView, _ => new Label());
                label.Text = text;
                return Adopt(label, itemsView);
            default:
                return null;
        }
    }

    private static readonly ConditionalWeakTable<ItemsView, Label> EmptyLabels = new();

    private static T Adopt<T>(T view, ItemsView itemsView) where T : Element
    {
        if (view.Parent is null)
            view.Parent = itemsView;
        return view;
    }
}
