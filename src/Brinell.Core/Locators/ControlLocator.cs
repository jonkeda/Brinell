namespace Brinell.Core.Locators;

/// <summary>
/// Represents a control locator that can be used across platforms.
/// Encapsulates the strategy and value used to find elements.
/// </summary>
public class ControlLocator
{
    /// <summary>
    /// The locator strategy.
    /// </summary>
    public LocatorStrategy Strategy { get; }

    /// <summary>
    /// The locator value (selector, id, xpath, etc.).
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Optional parent locator for chained lookups.
    /// </summary>
    public ControlLocator? Parent { get; private set; }

    /// <summary>
    /// Creates a new control locator.
    /// </summary>
    /// <param name="strategy">The locator strategy to use.</param>
    /// <param name="value">The locator value.</param>
    public ControlLocator(LocatorStrategy strategy, string value)
    {
        Strategy = strategy;
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Creates a chained locator where this locator is used first, then the child.
    /// </summary>
    /// <param name="child">The child locator to chain.</param>
    /// <returns>A new chained locator.</returns>
    public ControlLocator Then(ControlLocator child)
    {
        if (child == null) throw new ArgumentNullException(nameof(child));

        return new ControlLocator(LocatorStrategy.Chained, child.Value)
        {
            Parent = this
        };
    }

    /// <summary>
    /// Gets the locator chain as a flat list (from parent to child).
    /// </summary>
    /// <returns>List of locators in chain order.</returns>
    public IReadOnlyList<ControlLocator> GetChain()
    {
        var chain = new List<ControlLocator>();
        var current = this;

        while (current != null)
        {
            if (current.Parent != null)
            {
                chain.Insert(0, current.Parent);
            }
            if (current.Strategy != LocatorStrategy.Chained)
            {
                chain.Add(current);
            }
            current = current.Parent;
        }

        return chain;
    }

    /// <summary>
    /// Implicitly convert a string to a ControlLocator using AutomationId strategy.
    /// </summary>
    public static implicit operator ControlLocator(string automationId)
        => new(LocatorStrategy.AutomationId, automationId);

    /// <summary>
    /// Returns a string representation of the locator.
    /// </summary>
    public override string ToString()
    {
        if (Parent != null)
        {
            return $"{Parent} -> {Strategy}:{Value}";
        }
        return $"{Strategy}:{Value}";
    }

    /// <summary>
    /// Determines equality based on strategy and value.
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is ControlLocator other)
        {
            return Strategy == other.Strategy && 
                   Value == other.Value &&
                   Equals(Parent, other.Parent);
        }
        return false;
    }

    /// <summary>
    /// Gets hash code based on strategy and value.
    /// </summary>
    public override int GetHashCode()
    {
        return HashCode.Combine(Strategy, Value, Parent);
    }
}
