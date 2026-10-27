using System.Collections;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Controls;
using MauiUnit.Hosting;

namespace MauiUnit.Inspection;

/// <summary>
/// Makes the views a <see cref="ItemsView"/> would show: every item through its template, no
/// virtualization, and a grouped list's group headers and footers around each group's items. Without a
/// platform handler a CollectionView has no item views at all.
/// </summary>
/// <remarks>
/// A view is kept per item, by reference, so state typed into a row survives the next query; a row
/// whose item left the source is dropped. Each view loses its platform behaviors before it joins the
/// list (<see cref="HeadlessBehaviors"/>).
/// </remarks>
internal static class ItemRealizer
{
    private static readonly ConditionalWeakTable<ItemsView, Dictionary<object, View>> Rows = new();
    private static readonly ConditionalWeakTable<ItemsView, Dictionary<object, View>> GroupHeaders = new();
    private static readonly ConditionalWeakTable<ItemsView, Dictionary<object, View>> GroupFooters = new();
    private static readonly object NullItem = new();

    public static IEnumerable<Element> Realize(ItemsView itemsView)
    {
        if (itemsView is StructuredItemsView { Header: View header })
            yield return Adopt(header, itemsView);

        var source = itemsView.ItemsSource?.Cast<object?>().ToList() ?? [];
        if (source.Count == 0)
        {
            if (Empty(itemsView) is { } empty)
                yield return empty;
        }
        else if (itemsView is GroupableItemsView { IsGrouped: true } grouped)
        {
            foreach (var view in Groups(grouped, source))
                yield return view;
        }
        else
        {
            foreach (var row in RowsFor(itemsView, source))
                yield return row;
        }

        if (itemsView is StructuredItemsView { Footer: View footer })
            yield return Adopt(footer, itemsView);
    }

    /// <summary>Whether <paramref name="row"/> is a CarouselView's card other than the one at its Position.</summary>
    public static bool IsCardOffScreen(Element row)
    {
        if (row.Parent is not CarouselView carousel || !TryGetItem(row, out var item))
            return false;

        var index = carousel.ItemsSource?.Cast<object?>().ToList().IndexOf(item) ?? -1;
        return index >= 0 && index != carousel.Position;
    }

    /// <summary>The item a realized row shows, or false when <paramref name="row"/> is no row; a group header is none.</summary>
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

    private static List<View> Groups(GroupableItemsView list, List<object?> groups)
    {
        var items = groups.SelectMany(group => (group as IEnumerable)?.Cast<object?>() ?? Enumerable.Empty<object?>()).ToList();
        var rows = RowsFor(list, items);
        var headers = Views(GroupHeaders, list);
        var footers = Views(GroupFooters, list);
        var result = new List<View>();
        var next = 0;
        foreach (var group in groups.OfType<object>())
        {
            if (list.GroupHeaderTemplate is { } headerTemplate)
                result.Add(ViewFor(headers, list, group, headerTemplate));

            var count = (group as IEnumerable)?.Cast<object?>().Count() ?? 0;
            result.AddRange(rows.Skip(next).Take(count));
            next += count;

            if (list.GroupFooterTemplate is { } footerTemplate)
                result.Add(ViewFor(footers, list, group, footerTemplate));
        }

        Drop(headers, list, groups);
        Drop(footers, list, groups);
        return result;
    }

    private static List<View> RowsFor(ItemsView itemsView, List<object?> items)
    {
        var rows = Views(Rows, itemsView);
        var keys = items.Select(item => item ?? NullItem).ToList();
        Drop(rows, itemsView, keys);

        var result = new List<View>(items.Count);
        foreach (var key in keys)
        {
            if (!rows.TryGetValue(key, out var view))
            {
                var item = ReferenceEquals(key, NullItem) ? null : key;
                view = Make(itemsView, item, itemsView.ItemTemplate);
                rows[key] = view;
            }

            result.Add(view);
        }

        return result;
    }

    private static Dictionary<object, View> Views(ConditionalWeakTable<ItemsView, Dictionary<object, View>> table, ItemsView itemsView)
        => table.GetValue(itemsView, _ => new Dictionary<object, View>(ReferenceEqualityComparer.Instance));

    private static View ViewFor(Dictionary<object, View> views, ItemsView itemsView, object source, DataTemplate template)
    {
        if (!views.TryGetValue(source, out var view))
        {
            view = Make(itemsView, source, template);
            views[source] = view;
        }

        return view;
    }

    // A view whose item left is detached, as a platform's recycled row is: whoever still holds it
    // holds an element no longer on screen.
    private static void Drop(Dictionary<object, View> views, ItemsView itemsView, IEnumerable<object?> kept)
    {
        var keys = kept.Select(key => key ?? NullItem);
        foreach (var gone in views.Keys.Except(keys, ReferenceEqualityComparer.Instance).ToList())
        {
            if (views.Remove(gone, out var view) && ReferenceEquals(view.Parent, itemsView))
                view.Parent = null;
        }
    }

    private static View Make(ItemsView itemsView, object? item, DataTemplate? template)
    {
        if (template is DataTemplateSelector selector)
            template = selector.SelectTemplate(item, itemsView);

        // Without a template the platforms show the item's ToString().
        var view = template?.CreateContent() as View ?? new Label { Text = item?.ToString() };
        view.BindingContext = item;
        HeadlessBehaviors.Strip(view);
        return Adopt(view, itemsView);
    }

    private static View? Empty(ItemsView itemsView)
    {
        switch (itemsView.EmptyView)
        {
            case View view:
                HeadlessBehaviors.Strip(view);
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
