namespace MUnit.Interaction;

/// <summary>
/// An interaction a user could not perform: the element is hidden, disabled, read-only, or has
/// nothing that answers it. MUnit refuses rather than run a handler the user could never reach.
/// </summary>
public sealed class ElementNotInteractableException(string message) : InvalidOperationException(message);
