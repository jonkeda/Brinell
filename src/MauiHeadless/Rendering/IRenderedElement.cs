using Microsoft.Maui.Controls;
using MauiHeadless.Inspection;
using MauiHeadless.Snapshots;

namespace MauiHeadless.Rendering;

/// <summary>
/// An element rendered by a <see cref="MauiTestContext"/>, in its own window: bUnit's
/// <c>IRenderedComponent</c>. Queries search what the window shows now, so after a push they find
/// the new page; to search only the element itself, query <see cref="Instance"/>.
/// </summary>
/// <typeparam name="T">The rendered element's type.</typeparam>
public interface IRenderedElement<out T> : IDisposable where T : Element
{
    /// <summary>The context that rendered it.</summary>
    MauiTestContext Context { get; }

    /// <summary>The element as made: the page, view or layout asked for.</summary>
    T Instance { get; }

    /// <summary>The window it was rendered into.</summary>
    Window Window { get; }

    /// <summary>The page the window shows now: the top modal, else the current page down through navigation, Shell and tabs.</summary>
    Page CurrentPage { get; }

    /// <summary>The window's navigation: its stacks, back, and assertions on where it is.</summary>
    TestNavigator Navigation { get; }

    /// <summary>The tree the window shows now, read after the queued work has run.</summary>
    VisualNode Tree { get; }

    /// <inheritdoc cref="ElementQueries.Find{T}"/>
    TElement Find<TElement>(string? automationId = null) where TElement : Element;

    /// <inheritdoc cref="ElementQueries.FindAll{T}"/>
    IReadOnlyList<TElement> FindAll<TElement>(string? automationId = null) where TElement : Element;

    /// <inheritdoc cref="ElementQueries.FindByAutomationId"/>
    Element FindByAutomationId(string automationId);

    /// <inheritdoc cref="ElementQueries.FindByText{T}"/>
    TElement FindByText<TElement>(string text) where TElement : Element;

    /// <summary>The tree the window shows now as indented text (see <see cref="TreeSnapshot"/>).</summary>
    string ToSnapshot(SnapshotOptions? options = null);

    /// <summary>
    /// Runs <paramref name="assertion"/> until it passes, draining the queue between tries; after
    /// <paramref name="timeout"/> (default: the context's <c>WaitTimeout</c>) its last failure is thrown.
    /// </summary>
    void WaitForAssertion(Action assertion, TimeSpan? timeout = null);

    /// <summary>Drains the queue until <paramref name="predicate"/> holds, or throws a <see cref="TimeoutException"/>.</summary>
    void WaitForState(Func<bool> predicate, TimeSpan? timeout = null);
}
