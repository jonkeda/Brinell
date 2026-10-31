namespace MauiHeadless.Interaction;

/// <summary>Why an interaction was refused.</summary>
public enum InteractionRefusal
{
    /// <summary>The element or an ancestor is hidden.</summary>
    Hidden,

    /// <summary>The element or an ancestor is disabled, or its command cannot execute.</summary>
    Disabled,

    /// <summary>The input is read-only.</summary>
    ReadOnly,

    /// <summary>The element has nothing that answers this interaction: no recognizer, no such item, selection off.</summary>
    NotOffered,
}

/// <summary>
/// An interaction a user could not perform: the element is hidden, disabled, read-only, or has
/// nothing that answers it. MauiHeadless refuses rather than run a handler the user could never reach.
/// </summary>
public sealed class ElementNotInteractableException(string message, InteractionRefusal reason = InteractionRefusal.NotOffered)
    : InvalidOperationException(message)
{
    /// <summary>Why it was refused.</summary>
    public InteractionRefusal Reason { get; } = reason;
}
