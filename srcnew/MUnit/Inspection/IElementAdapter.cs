using Microsoft.Maui.Controls;

namespace MUnit.Inspection;

/// <summary>
/// How MUnit reads one kind of element: its children in the tree a test sees, and its text. A
/// control whose children or text MAUI's own interfaces do not show (a third-party chart, a custom
/// items control) gets an adapter of its own, registered on the context's options.
/// </summary>
/// <remarks>
/// Adapters are asked in order, the context's own first and <see cref="DefaultElementAdapter"/>
/// last; the first that answers <see cref="CanAdapt"/> decides.
/// </remarks>
public interface IElementAdapter
{
    /// <summary>Whether this adapter reads <paramref name="element"/>.</summary>
    bool CanAdapt(Element element);

    /// <summary>The children a user would see under <paramref name="element"/>, in order.</summary>
    IEnumerable<Element> GetChildren(Element element);

    /// <summary>The text a user would read on <paramref name="element"/>, or null when it shows none.</summary>
    string? GetText(Element element);
}
