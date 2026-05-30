using System.Reflection;
using System.Text;

namespace Brinell.Uat;

public static class UatNameInference
{
    private static readonly string[] Suffixes =
    [
        "Page",
        "Button",
        "Input",
        "Entry",
        "Field",
        "TextBox",
        "CheckBox",
        "Checkbox",
        "Switch",
        "Toggle",
        "Picker",
        "Dropdown",
        "List",
        "Grid",
        "Label",
        "Text",
        "Control"
    ];

    public static string FromType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return FromIdentifier(type.Name);
    }

    public static string FromMember(MemberInfo member)
    {
        ArgumentNullException.ThrowIfNull(member);
        return FromIdentifier(member.Name);
    }

    public static string FromIdentifier(string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

        var withoutGeneric = identifier.Split('`')[0];
        var withoutSuffix = StripSuffix(withoutGeneric);
        var words = SplitWords(withoutSuffix);
        return string.Join(' ', words);
    }

    public static bool HasKnownSuffix(string identifier)
    {
        return Suffixes.Any(suffix => identifier.EndsWith(suffix, StringComparison.Ordinal));
    }

    private static string StripSuffix(string value)
    {
        foreach (var suffix in Suffixes)
        {
            if (value.Length > suffix.Length && value.EndsWith(suffix, StringComparison.Ordinal))
            {
                return value[..^suffix.Length];
            }
        }

        return value;
    }

    private static IReadOnlyList<string> SplitWords(string value)
    {
        var normalized = value.Replace('_', ' ').Replace('-', ' ');
        List<string> words = [];
        StringBuilder current = new();

        for (var i = 0; i < normalized.Length; i++)
        {
            var character = normalized[i];
            if (char.IsWhiteSpace(character))
            {
                Flush();
                continue;
            }

            if (current.Length > 0 && char.IsUpper(character))
            {
                var previous = current[^1];
                var nextIsLower = i + 1 < normalized.Length && char.IsLower(normalized[i + 1]);
                if (char.IsLower(previous) || (char.IsUpper(previous) && nextIsLower))
                {
                    Flush();
                }
            }

            current.Append(character);
        }

        Flush();
        return words;

        void Flush()
        {
            if (current.Length == 0)
            {
                return;
            }

            words.Add(ToDisplayWord(current.ToString()));
            current.Clear();
        }
    }

    private static string ToDisplayWord(string word)
    {
        if (word.All(char.IsUpper))
        {
            return word;
        }

        return word.Length == 1
            ? word.ToUpperInvariant()
            : char.ToUpperInvariant(word[0]) + word[1..];
    }
}
