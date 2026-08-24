namespace Brinell.Core.Interfaces;

/// <summary>
/// Declares which type parameter generated public members return for fluent chaining.
/// </summary>
/// <remarks>
/// Only needed when the generator cannot infer it: a class with several type parameters
/// and none named <c>TSelf</c>. Controls (<c>&lt;TScope&gt;</c>) and containers
/// (<c>&lt;TParent, TSelf&gt;</c>) are both inferred, so this attribute is rarely required.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class FluentReturnAttribute : Attribute
{
    /// <summary>
    /// Creates the attribute naming the fluent return type parameter.
    /// </summary>
    /// <param name="typeParameterName">
    /// The name of a type parameter the class declares, e.g. <c>nameof(TSelf)</c>.
    /// </param>
    public FluentReturnAttribute(string typeParameterName)
    {
        TypeParameterName = typeParameterName;
    }

    /// <summary>
    /// Gets the name of the type parameter public members return.
    /// </summary>
    public string TypeParameterName { get; }
}
