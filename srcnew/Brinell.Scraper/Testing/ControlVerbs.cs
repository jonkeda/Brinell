using System.Reflection;
using Brinell.Core.Interfaces;
using Brinell.Core.Testing;
using Brinell.Scraper.Code;

namespace Brinell.Scraper.Testing;

/// <summary>
/// What a control type can do, read from Brinell's own controls (<c>Brinell.Html</c>, whose names the
/// Blazor controls share) or, for a project's control, from the Brinell control it derives from; and the
/// catalog phrase of a verb, from the <c>[BddWhen]</c>/<c>[BddThen]</c> on the core interfaces.
/// </summary>
public static class ControlVerbs
{
    private static readonly Assembly Controls = typeof(Brinell.Html.Controls.ContainerBase<,>).Assembly;

    /// <summary>The core interfaces (generic definitions) a control type implements; empty when it is unknown.</summary>
    /// <param name="type">The member's type as written (<c>TextInputControl&lt;UserFormPage&gt;</c>).</param>
    /// <param name="classes">The project's classes, for its own controls.</param>
    public static IReadOnlySet<Type> Interfaces(string type, IReadOnlyList<CodeClass> classes)
    {
        var name = Short(type);
        for (var depth = 0; depth < 5; depth++)
        {
            var own = classes.FirstOrDefault(c => c.Kind == CodeKind.Control && Short(c.Name) == name);
            if (own?.Base is null) break;
            name = own.Base;
        }
        var brinell = Controls.GetTypes().FirstOrDefault(t => t.IsPublic && Short(t.Name) == name);
        return brinell is null
            ? new HashSet<Type>()
            : brinell.GetInterfaces().Select(i => i.IsGenericType ? i.GetGenericTypeDefinition() : i).ToHashSet();
    }

    /// <summary>The first-person phrase on an interface's method, for a literal when the phrase has one; null when it has none.</summary>
    public static string? Phrase(Type contract, string method, bool? literal = null)
    {
        var attributes = contract.GetMethods()
            .Where(m => m.Name == method)
            .SelectMany(m => m.GetCustomAttributes<BddStepAttribute>())
            .Where(a => a.Phrase.StartsWith("I ", StringComparison.Ordinal) || a.Phrase.StartsWith("{control}", StringComparison.Ordinal));
        return attributes.FirstOrDefault(a => literal is null || !a.HasLiteral || a.Literal == literal)?.Phrase;
    }

    /// <summary>The interfaces a verb is checked against, in the order they are tried.</summary>
    internal static readonly Type Clickable = typeof(IClickableControlObject<>);

    internal static readonly Type Editable = typeof(IEditableTextControlObject<>);

    internal static readonly Type Selector = typeof(ISelectorControlObject<>);

    internal static readonly Type Toggle = typeof(IToggleControlObject<>);

    internal static readonly Type Text = typeof(ITextControlObject<>);

    internal static readonly Type Element = typeof(IElementObject<>);

    /// <summary>A type's name without its namespace and type arguments.</summary>
    public static string Short(string type) => type.Split('<')[0].Split('`')[0].Split('.')[^1];
}
