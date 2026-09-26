using System.Reflection;

namespace Brinell.Uat;

// CommandId, Literal and HasLiteral belong to control verbs; on a phrase method they are an error (AD-011).
internal static class UatPhraseMethods
{
    public static IReadOnlyList<UatPhraseAttribute> GetPhrases(MethodInfo method)
    {
        var phrases = method.GetCustomAttributes<UatPhraseAttribute>().ToArray();
        foreach (var phrase in phrases)
        {
            var error = ControlVerbOnlyError(method, phrase);
            if (error is not null)
            {
                throw new InvalidOperationException(error);
            }
        }

        return phrases;
    }

    public static string? ControlVerbOnlyError(MethodInfo method, UatPhraseAttribute phrase)
    {
        string? property = phrase.CommandId is not null ? nameof(UatPhraseAttribute.CommandId)
            : phrase.HasLiteral ? nameof(UatPhraseAttribute.HasLiteral)
            : phrase.Literal ? nameof(UatPhraseAttribute.Literal)
            : null;
        return property is null
            ? null
            : $"UAT phrase '{phrase.Phrase}' on {method.DeclaringType?.FullName}.{method.Name} sets " +
              $"{property}, which only a control verb may set.";
    }
}
