using System.Text.Json;
using System.Text.RegularExpressions;

namespace Brinell.TestData;

/// <summary>
/// Reads test-set files strictly and runs their rules. The xUnit gate and the <c>brinell-data</c>
/// tool both call this, so they cannot disagree.
/// </summary>
public static partial class TestDataValidator
{
    /// <summary>Validates every test set of <paramref name="definition"/>.</summary>
    public static TestDataReport Validate(ITestDataDefinition definition)
        => new(definition.TestSetNames.SelectMany(name => definition.ValidateFile(definition.FileOf(name))).ToList());

    /// <summary>Validates one file against <paramref name="definition"/>.</summary>
    public static TestDataReport Validate(ITestDataDefinition definition, string path)
        => new(definition.ValidateFile(path));

    internal static IReadOnlyList<TestDataDiagnostic> ValidateFile<TSet>(
        string path,
        JsonSerializerOptions options,
        TestDataRules<TSet> rules,
        IReadOnlyList<Type> knownTypes)
        where TSet : class
    {
        var text = File.ReadAllText(path);
        TSet? set;
        try
        {
            set = JsonSerializer.Deserialize<TSet>(text, options);
        }
        catch (JsonException exception)
        {
            return [FromJsonException(path, exception, options, knownTypes)];
        }

        if (set is null)
        {
            return [new TestDataDiagnostic(path, 1, TestDataCodes.Empty, null, "The file holds no test set.")];
        }

        var lines = JsonLineMap.Build(text);
        List<TestDataDiagnostic> diagnostics = [];
        var context = new TestDataRuleContext((jsonPath, message, code)
            => diagnostics.Add(new TestDataDiagnostic(path, lines.LineOf(jsonPath), code, jsonPath, message)));

        foreach (var rule in rules.All)
        {
            try
            {
                rule(set, context);
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                diagnostics.Add(new TestDataDiagnostic(path, null, TestDataCodes.RuleCrashed, null, $"A rule threw {exception.GetType().Name}: {exception.Message}"));
            }
        }

        return diagnostics.OrderBy(d => d.Line ?? int.MaxValue).ToList();
    }

    private static TestDataDiagnostic FromJsonException(
        string path,
        JsonException exception,
        JsonSerializerOptions options,
        IReadOnlyList<Type> knownTypes)
    {
        var line = exception.LineNumber is { } zeroBased ? (int)zeroBased + 1 : (int?)null;
        var jsonPath = exception.Path is { Length: > 1 } p ? p.TrimStart('$', '.') : null;
        var message = exception.Message;

        if (UnmappedRegex().Match(message) is { Success: true } unmapped)
        {
            var field = unmapped.Groups["field"].Value;
            var type = unmapped.Groups["type"].Value;
            var hint = TestDataTypes.Suggest(field, TestDataTypes.JsonNamesOf(options, knownTypes, type)) is { } suggestion
                ? $" Did you mean '{suggestion}'?"
                : string.Empty;
            return new TestDataDiagnostic(path, line, TestDataCodes.UnknownField, jsonPath,
                $"unknown field '{field}' on {ShortName(type)}.{hint}");
        }

        if (MissingRegex().Match(message) is { Success: true } missing)
        {
            return new TestDataDiagnostic(path, line, TestDataCodes.MissingField, jsonPath,
                $"required field(s) {missing.Groups["fields"].Value} missing on {ShortName(missing.Groups["type"].Value)}.");
        }

        if (message.Contains("could not be converted", StringComparison.Ordinal) ||
            message.Contains("doesn't allow null", StringComparison.Ordinal) ||
            message.Contains("does not allow null", StringComparison.Ordinal))
        {
            return new TestDataDiagnostic(path, line, TestDataCodes.WrongValue, jsonPath, WrongValueMessage(message, options, knownTypes, jsonPath));
        }

        return new TestDataDiagnostic(path, line, TestDataCodes.Syntax, jsonPath, message);
    }

    private static string WrongValueMessage(string message, JsonSerializerOptions options, IReadOnlyList<Type> knownTypes, string? jsonPath)
    {
        if (ConvertRegex().Match(message) is not { Success: true } convert)
        {
            return message;
        }

        // The converter names the type holding the field, so the field is found by the path's last segment.
        var typeName = convert.Groups["type"].Value;
        var fieldName = jsonPath?.Split('.', '[')[^1];
        var field = knownTypes.FirstOrDefault(t => t.FullName == typeName) is { } holder && fieldName is not null
            ? TestDataTypes.FieldsOf(options, holder).FirstOrDefault(f => f.JsonName == fieldName)
            : null;

        return field switch
        {
            { Values.Count: > 0 } => $"the value is not one of {string.Join(", ", field.Values)}.",
            not null => $"the value cannot be read as {field.TypeName}.",
            _ => $"the value cannot be read as {ShortName(typeName)}.",
        };
    }

    private static string ShortName(string fullName)
    {
        var name = fullName.Split('`')[0];
        return name[(name.LastIndexOf('.') + 1)..];
    }

    [GeneratedRegex("The JSON property '(?<field>[^']+)' could not be mapped to any \\.NET member contained in type '(?<type>[^']+)'")]
    private static partial Regex UnmappedRegex();

    [GeneratedRegex("type '(?<type>[^']+)' was missing required properties including: (?<fields>.+?)\\.?$", RegexOptions.Multiline)]
    private static partial Regex MissingRegex();

    [GeneratedRegex("could not be converted to (?<type>[^.\\s]+(\\.[^.\\s]+)*)\\.")]
    private static partial Regex ConvertRegex();
}
