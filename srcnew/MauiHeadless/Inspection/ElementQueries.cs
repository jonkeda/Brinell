using Microsoft.Maui.Controls;

namespace MauiHeadless.Inspection;

/// <summary>
/// Finds elements in the tree under an element, as bUnit's <c>Find</c> and <c>FindAll</c> do in markup:
/// by type, by <c>AutomationId</c> and by text. Every query first runs the work the context has queued.
/// </summary>
public static class ElementQueries
{
    /// <summary>
    /// Every element of type <typeparamref name="T"/> under <paramref name="root"/>, the root included,
    /// in reading order; with <paramref name="automationId"/>, only those with that id.
    /// </summary>
    public static IReadOnlyList<T> FindAll<T>(this Element root, string? automationId = null) where T : Element
        => Nodes(root)
            .Where(node => node.Element is T && (automationId is null || node.AutomationId == automationId))
            .Select(node => (T)node.Element)
            .ToList();

    /// <summary>
    /// The one element of type <typeparamref name="T"/> under <paramref name="root"/>, with
    /// <paramref name="automationId"/> when given.
    /// </summary>
    /// <exception cref="ElementNotFoundException">None matches, or more than one does.</exception>
    public static T Find<T>(this Element root, string? automationId = null) where T : Element
    {
        var tree = Read(root);
        var matches = tree.DescendantsAndSelf()
            .Where(node => node.Element is T && (automationId is null || node.AutomationId == automationId))
            .ToList();
        return (T)Single(matches, tree, Describe(typeof(T), automationId, text: null)).Element;
    }

    /// <summary>The one element with this <c>AutomationId</c> under <paramref name="root"/>, of any type.</summary>
    /// <exception cref="ElementNotFoundException">None matches, or more than one does.</exception>
    public static Element FindByAutomationId(this Element root, string automationId)
    {
        ArgumentNullException.ThrowIfNull(automationId);
        return root.Find<Element>(automationId);
    }

    /// <summary>The one element of type <typeparamref name="T"/> under <paramref name="root"/> showing exactly <paramref name="text"/>.</summary>
    /// <exception cref="ElementNotFoundException">None matches, or more than one does.</exception>
    public static T FindByText<T>(this Element root, string text) where T : Element
    {
        ArgumentNullException.ThrowIfNull(text);
        var tree = Read(root);
        var matches = tree.DescendantsAndSelf()
            .Where(node => node.Element is T && node.Text == text)
            .ToList();
        return (T)Single(matches, tree, Describe(typeof(T), automationId: null, text)).Element;
    }

    /// <summary>
    /// The tree under <paramref name="root"/> as it is now, after the queued work has run. It is read as
    /// the UI thread would, with the context's application current, because reading makes rows whose
    /// templates may look up the app's resources.
    /// </summary>
    public static VisualNode Read(Element root)
    {
        ArgumentNullException.ThrowIfNull(root);
        if (MauiTestContext.Of(root) is not { } context)
            return VisualTree.Read(root);

        context.Dispatcher.RunPending();
        return context.Dispatcher.Run(() => VisualTree.Read(root));
    }

    private static IEnumerable<VisualNode> Nodes(Element root) => Read(root).DescendantsAndSelf();

    private static VisualNode Single(List<VisualNode> matches, VisualNode tree, string wanted)
    {
        return matches.Count switch
        {
            1 => matches[0],
            0 => throw new ElementNotFoundException($"No {wanted} was found.", tree),
            _ => throw new ElementNotFoundException(
                $"{matches.Count} elements match {wanted}; Find wants exactly one. Narrow it with an AutomationId, or use FindAll:\n"
                + string.Join('\n', matches.Select(match => "  " + match)),
                tree),
        };
    }

    private static string Describe(Type type, string? automationId, string? text)
        => type.Name
           + (automationId is null ? "" : $" with AutomationId '{automationId}'")
           + (text is null ? "" : $" showing '{text}'");
}
