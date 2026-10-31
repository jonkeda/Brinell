using Microsoft.Maui.Controls;
using MauiHeadless.Inspection;
using MauiHeadless.Internals;
using MauiHeadless.Snapshots;

namespace MauiHeadless.Assertions;

/// <summary>
/// Assertions on one element, read as a user sees it: text, visibility, enablement, identity, its
/// binding context and its bindings. Each reads after the queued work has run, and a failure shows
/// the element's line and the screen it is on.
/// </summary>
/// <typeparam name="T">The element's type.</typeparam>
public sealed class ElementAssertions<T> where T : Element
{
    internal ElementAssertions(T subject)
    {
        Subject = subject;
    }

    /// <summary>The element asserted on.</summary>
    public T Subject { get; }

    /// <summary>The same assertions again, for chaining: <c>.HaveText("Save").And.BeEnabled()</c>.</summary>
    public ElementAssertions<T> And => this;

    /// <summary>Asserts the element shows exactly <paramref name="expected"/>.</summary>
    public ElementAssertions<T> HaveText(string? expected)
    {
        var actual = Node().Text;
        return actual == expected ? this : Fail($"to have text {Show(expected)}, but it was {Show(actual)}");
    }

    /// <summary>Asserts the element's text contains <paramref name="expected"/>, ordinal.</summary>
    public ElementAssertions<T> ContainText(string expected)
    {
        ArgumentNullException.ThrowIfNull(expected);
        var actual = Node().Text;
        return actual is not null && actual.Contains(expected, StringComparison.Ordinal)
            ? this
            : Fail($"to contain text {Show(expected)}, but it was {Show(actual)}");
    }

    /// <summary>Asserts the element and every ancestor are visible.</summary>
    public ElementAssertions<T> BeVisible()
        => Node().IsVisible ? this : Fail("to be visible, but it or an ancestor has IsVisible false");

    /// <summary>Asserts the element or an ancestor is hidden.</summary>
    public ElementAssertions<T> NotBeVisible()
        => !Node().IsVisible ? this : Fail("not to be visible, but it is");

    /// <summary>Asserts the element and every ancestor are enabled.</summary>
    public ElementAssertions<T> BeEnabled()
        => Node().IsEnabled ? this : Fail("to be enabled, but it or an ancestor is disabled (or its command cannot execute)");

    /// <summary>Asserts the element or an ancestor is disabled.</summary>
    public ElementAssertions<T> BeDisabled()
        => !Node().IsEnabled ? this : Fail("to be disabled, but it is enabled");

    /// <summary>Asserts the element's <c>AutomationId</c>.</summary>
    public ElementAssertions<T> HaveAutomationId(string expected)
        => Subject.AutomationId == expected
            ? this
            : Fail($"to have AutomationId {Show(expected)}, but it was {Show(Subject.AutomationId)}");

    /// <summary>Asserts the binding context is a <typeparamref name="TContext"/>, and satisfies <paramref name="predicate"/> when given.</summary>
    public ElementAssertions<T> HaveBindingContext<TContext>(Func<TContext, bool>? predicate = null)
    {
        var context = Pumped().BindingContext;
        if (context is not TContext typed)
            return Fail($"to have a {typeof(TContext).Name} binding context, but it was {context?.GetType().Name ?? "null"}");
        return predicate is null || predicate(typed)
            ? this
            : Fail($"to have a {typeof(TContext).Name} binding context that satisfies the condition, but it did not");
    }

    /// <summary>
    /// Asserts <paramref name="property"/> is bound with <paramref name="path"/>: the markup's
    /// <c>{Binding Path}</c>, read from the binding MAUI keeps, not inferred from values.
    /// </summary>
    public ElementAssertions<T> BeBoundTo(BindableProperty property, string path)
    {
        ArgumentNullException.ThrowIfNull(property);
        var binding = MauiInternals.GetBinding(Pumped(), property);
        var actual = binding is Binding plain ? plain.Path : binding?.GetType().Name;
        return binding is not null && actual == path
            ? this
            : Fail($"to have {property.PropertyName} bound to {Show(path)}, but it was {(binding is null ? "not bound" : $"bound to {Show(actual)}")}");
    }

    /// <summary>Asserts the element satisfies <paramref name="predicate"/>, named by <paramref name="description"/> in the failure.</summary>
    public ElementAssertions<T> Satisfy(Func<T, bool> predicate, string description)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return predicate(Pumped()) ? this : Fail(description);
    }

    internal ElementAssertions<T> Fail(string expectation)
    {
        var node = VisualTree.Read(Subject);
        var screen = Screen(Subject);
        throw new MauiHeadlessAssertionException(
            $"Expected {TreeSnapshot.Describe(node)} {expectation}.\n\nScreen:\n{TreeSnapshot.Format(screen, TreeSnapshot.ForMessages)}");
    }

    private VisualNode Node() => ElementQueries.Read(Subject);

    private T Pumped()
    {
        MauiTestContext.Of(Subject)?.Dispatcher.RunPending();
        return Subject;
    }

    private static VisualNode Screen(Element element)
    {
        var top = element;
        while (top.Parent is { } parent)
            top = parent;
        return VisualTree.Read(top);
    }

    private static string Show(string? text) => text is null ? "null" : $"\"{text}\"";
}
