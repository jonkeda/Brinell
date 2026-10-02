using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Brinell.Generator.Analysis;

/// <summary>
/// What a shortcut forwards to, read from its declaration.
/// </summary>
public enum ShortcutKind
{
    /// <summary><c>Is*Shortcut</c> returning <c>bool?</c>: forwards the Is/Wait/Assert trio.</summary>
    State,

    /// <summary><c>Get*Shortcut</c> returning a value: forwards the Get/Wait/Assert trio and its comparison variants.</summary>
    Getter,

    /// <summary>Anything else: the shortcut itself, made public under its new name.</summary>
    Action
}

/// <summary>
/// A component's one-line forward to a member of one of its parts.
/// </summary>
/// <remarks>
/// <para>
/// <c>protected bool? IsPlayingShortcut() => PlayPauseButton.IsPlaying();</c> declares that the
/// component's <c>IsPlaying</c>, <c>WaitPlaying</c> and <c>AssertPlaying</c> are the part's own.
/// The generator emits them as single calls on the part, with no wrapper of their own, so the
/// part's call is the only unit of work: one readiness check, one poll, one log entry.
/// </para>
/// <para>
/// Everything is read from syntax. The part's type is never inspected: when the part lacks a
/// forwarded member, the generated code does not compile, and that error names the member.
/// </para>
/// </remarks>
public sealed class ShortcutMethod
{
    /// <summary>The suffix that marks a shortcut, as <c>Core</c> marks generated behaviour.</summary>
    public const string Suffix = "Shortcut";

    private const string TimeoutParameterName = "timeoutMs";

    private ShortcutMethod()
    {
    }

    /// <summary>The shortcut's own name, e.g. <c>IsPlayingShortcut</c>.</summary>
    public string MethodName { get; private init; } = "";

    /// <summary>The name without the suffix, e.g. <c>IsPlaying</c>.</summary>
    public string PublicName { get; private init; } = "";

    /// <summary>What the shortcut forwards.</summary>
    public ShortcutKind Kind { get; private init; }

    /// <summary>
    /// The component's stem for a query: <c>Playing</c> for <c>IsPlayingShortcut</c>,
    /// <c>Progress</c> for <c>GetProgressShortcut</c>. Empty for an action.
    /// </summary>
    public string Stem { get; private init; } = "";

    /// <summary>The part the shortcut calls, e.g. <c>PlayPauseButton</c>.</summary>
    public string Part { get; private init; } = "";

    /// <summary>The part member called, e.g. <c>IsPlaying</c> or <c>GetValue</c>.</summary>
    public string PartMember { get; private init; } = "";

    /// <summary>
    /// The part's stem for a query: <c>Playing</c> for <c>IsPlaying</c>, <c>Value</c> for
    /// <c>GetValue</c>. The forwarded Wait and Assert members are named from it.
    /// </summary>
    public string PartStem { get; private init; } = "";

    /// <summary>The shortcut's return type as written.</summary>
    public string ReturnType { get; private init; } = "";

    /// <summary>
    /// The shortcut's parameters, excluding <c>timeoutMs</c>, as <c>(type, name)</c>.
    /// A query's parameters lead every forwarded signature.
    /// </summary>
    public IReadOnlyList<(string TypeName, string ParameterName)> Parameters { get; private init; } = [];

    /// <summary>
    /// The arguments passed to the part, excluding a bare <c>timeoutMs</c>, as written. They lead
    /// every forwarded call, before the expected value, message and timeout.
    /// </summary>
    public IReadOnlyList<string> Arguments { get; private init; } = [];

    /// <summary>An action's parameter list as written, parentheses included.</summary>
    public string ParameterListText { get; private init; } = "()";

    /// <summary>The call on the part as written, e.g. <c>PlayPauseButton.Play(timeoutMs)</c>.</summary>
    public string Invocation { get; private init; } = "";

    /// <summary>
    /// Whether a method is meant as a shortcut: its name ends in <see cref="Suffix"/>.
    /// </summary>
    /// <remarks>
    /// Intent is taken from the name alone, so a shortcut that is malformed in any other way is
    /// reported by <see cref="TryParse"/> instead of being skipped.
    /// </remarks>
    public static bool IsCandidate(MethodDeclarationSyntax method)
    {
        var name = method.Identifier.Text;
        return name.Length > Suffix.Length && name.EndsWith(Suffix, StringComparison.Ordinal);
    }

    /// <summary>
    /// Reads a shortcut, or explains why the declaration cannot be one.
    /// </summary>
    /// <param name="method">A method for which <see cref="IsCandidate"/> is true.</param>
    /// <param name="shortcut">The shortcut, when the declaration is well-formed.</param>
    /// <param name="error">Why it is not, naming the rule it breaks.</param>
    /// <returns>True when the declaration is a well-formed shortcut.</returns>
    public static bool TryParse(MethodDeclarationSyntax method, out ShortcutMethod? shortcut, out string? error)
    {
        shortcut = null;
        var name = method.Identifier.Text;

        error = CheckModifiers(method.Modifiers);
        if (error != null)
        {
            error = $"Shortcut '{name}' {error}";
            return false;
        }

        if (method.ExpressionBody?.Expression is not InvocationExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax
                {
                    Expression: IdentifierNameSyntax part,
                    Name: IdentifierNameSyntax member
                }
            } invocation)
        {
            error = $"Shortcut '{name}' must be expression-bodied with one call on a part: " +
                    "'=> Part.Member(arguments);'. The generator forwards to that call and nothing else.";
            return false;
        }

        var publicName = name[..^Suffix.Length];
        var returnType = method.ReturnType.ToString();
        var partMember = member.Identifier.Text;
        var kind = KindOf(publicName, returnType);

        var parameters = method.ParameterList.Parameters
            .Where(p => p.Identifier.Text != TimeoutParameterName)
            .Select(p => (p.Type?.ToString() ?? "object", p.Identifier.Text))
            .ToList();

        var arguments = invocation.ArgumentList.Arguments
            .Where(a => a.Expression is not IdentifierNameSyntax { Identifier.Text: TimeoutParameterName })
            .Select(a => a.ToString())
            .ToList();

        var stem = "";
        var partStem = "";

        if (kind == ShortcutKind.State)
        {
            if (!partMember.StartsWith("Is", StringComparison.Ordinal) || partMember.Length == 2)
            {
                error = $"Shortcut '{name}' is a state query, so it must call an 'Is*' member of the " +
                        $"part, not '{partMember}': WaitX and AssertX are named from it.";
                return false;
            }

            stem = publicName[2..];
            partStem = partMember[2..];
        }
        else if (kind == ShortcutKind.Getter)
        {
            if (!partMember.StartsWith("Get", StringComparison.Ordinal) || partMember.Length == 3)
            {
                error = $"Shortcut '{name}' is a value query, so it must call a 'Get*' member of the " +
                        $"part, not '{partMember}': WaitX and AssertX are named from it.";
                return false;
            }

            stem = publicName[3..];
            partStem = partMember[3..];
        }

        shortcut = new ShortcutMethod
        {
            MethodName = name,
            PublicName = publicName,
            Kind = kind,
            Stem = stem,
            Part = part.Identifier.Text,
            PartMember = partMember,
            PartStem = partStem,
            ReturnType = returnType,
            Parameters = parameters,
            Arguments = arguments,
            ParameterListText = method.ParameterList.ToString(),
            Invocation = invocation.ToString()
        };
        error = null;
        return true;
    }

    private static ShortcutKind KindOf(string publicName, string returnType)
    {
        if (publicName.StartsWith("Is", StringComparison.Ordinal) && publicName.Length > 2
            && returnType is "bool?" or "bool")
            return ShortcutKind.State;

        if (publicName.StartsWith("Get", StringComparison.Ordinal) && publicName.Length > 3
            && returnType != "void")
            return ShortcutKind.Getter;

        return ShortcutKind.Action;
    }

    private static string? CheckModifiers(SyntaxTokenList modifiers)
    {
        if (!modifiers.Any(m => m.IsKind(SyntaxKind.ProtectedKeyword)))
            return "must be 'protected': it is a declaration for the generator, not API.";

        if (modifiers.Any(m => m.IsKind(SyntaxKind.VirtualKeyword)
                               || m.IsKind(SyntaxKind.OverrideKeyword)
                               || m.IsKind(SyntaxKind.AbstractKeyword)))
            return "must not be virtual, override or abstract: the generated members call the part " +
                   "directly, so overriding the shortcut would change nothing.";

        if (modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)))
            return "must not be static: it calls a part of this instance.";

        return null;
    }
}
