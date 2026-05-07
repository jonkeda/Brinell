# Step 5.10 — Validate Generated Code (Roslyn)

## Objective

Use Roslyn to parse and validate generated C# code. Perform syntax validation, control type validation against a dynamic registry (built-in + site custom controls), and locator method validation. Auto-retry on failure by re-prompting the LLM with error details.

## Dependencies

- Step 5.9 (parsed code blocks from LLM response)
- Step 5.7 (`IControlRegistry` for custom control types)
- NuGet: `Microsoft.CodeAnalysis.CSharp`

## Implementation

### NuGet package

```xml
<PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.*" />
```

### ValidationResult / CodeError models

```csharp
// Models/ValidationResult.cs
public sealed class ValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<CodeError> Errors { get; init; } = [];
    public List<CodeWarning> Warnings { get; init; } = [];
}

public sealed record CodeError(string Message, int Line, int Column);
public sealed record CodeWarning(string Message, int Line, int Column);
```

### CodeValidator — Syntax validation

```csharp
// Services/CodeValidator.cs
public static class CodeValidator
{
    /// <summary>
    /// Parse the code with Roslyn and collect syntax errors.
    /// </summary>
    public static ValidationResult Validate(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return new ValidationResult
            {
                Errors = [new CodeError("Empty code", 0, 0)]
            };

        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var diagnostics = syntaxTree.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        var result = new ValidationResult
        {
            Errors = diagnostics.Select(d => new CodeError(
                d.GetMessage(),
                d.Location.GetLineSpan().StartLinePosition.Line + 1,
                d.Location.GetLineSpan().StartLinePosition.Character + 1
            )).ToList()
        };

        // Additional semantic checks (don't fail, just warn)
        if (result.IsValid)
        {
            var warnings = new List<CodeWarning>();
            ValidateControlTypes(syntaxTree, warnings);
            ValidateLocators(syntaxTree, warnings);
            result.Warnings.AddRange(warnings);
        }

        return result;
    }
}
```

### Control type validation (dynamic registry)

```csharp
/// <summary>
/// Validates that all control type references in the code are known
/// (built-in Brinell types or site-specific custom controls).
/// </summary>
public static ValidationResult ValidateWithRegistry(
    string code, IControlRegistry registry)
{
    var result = Validate(code);
    if (!result.IsValid) return result;

    var knownTypes = GetKnownControlTypes(registry);
    var syntaxTree = CSharpSyntaxTree.ParseText(code);
    var root = syntaxTree.GetRoot();

    // Find all generic type references (e.g., TextInputControl<LoginPage>)
    var genericNames = root.DescendantNodes()
        .OfType<GenericNameSyntax>()
        .Select(g => g.Identifier.Text)
        .Distinct();

    foreach (var typeName in genericNames)
    {
        if (!knownTypes.Contains(typeName) &&
            !IsFrameworkType(typeName))
        {
            result.Warnings.Add(new CodeWarning(
                $"Unknown control type: '{typeName}'. Not found in built-in types or control registry.",
                0, 0));
        }
    }

    return result;
}

private static HashSet<string> GetKnownControlTypes(IControlRegistry registry)
{
    // Built-in Brinell controls
    var known = new HashSet<string>(StringComparer.Ordinal)
    {
        "TextInputControl", "ButtonControl", "SelectControl",
        "LabelControl", "CheckBoxControl", "RadioButtonControl",
        "LinkControl", "FileInputControl", "TextAreaControl",
        "ImageControl", "ElementControl"
    };

    // Add site-specific custom controls from registry
    var customControls = registry.GetAllControlsAsync()
        .GetAwaiter().GetResult(); // sync context OK for validation
    foreach (var custom in customControls)
        known.Add(custom.Name);

    return known;
}

private static bool IsFrameworkType(string name) =>
    name is "HtmlPageObjectBase" or "ContainerBase" or "List"
        or "IReadOnlyList" or "Task" or "ObservableCollection";
```

### Locator validation

```csharp
private static void ValidateLocators(SyntaxTree syntaxTree, List<CodeWarning> warnings)
{
    var root = syntaxTree.GetRoot();

    // Find all Locator.By*() invocations
    var invocations = root.DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Where(inv => inv.Expression is MemberAccessExpressionSyntax ma &&
                      ma.Expression is IdentifierNameSyntax id &&
                      id.Identifier.Text == "Locator");

    var validMethods = new HashSet<string>
    {
        "ByText", "ByLinkText", "ByPartialLinkText",
        "ByDataTestId", "ByAriaLabel", "ById", "ByCss"
    };

    foreach (var inv in invocations)
    {
        var memberAccess = (MemberAccessExpressionSyntax)inv.Expression;
        var methodName = memberAccess.Name.Identifier.Text;

        // Check method name is valid
        if (!validMethods.Contains(methodName))
        {
            var lineSpan = inv.GetLocation().GetLineSpan();
            warnings.Add(new CodeWarning(
                $"Unknown locator method: Locator.{methodName}()",
                lineSpan.StartLinePosition.Line + 1,
                lineSpan.StartLinePosition.Character + 1));
            continue;
        }

        // Warn if ByCss is used (last-resort locator)
        if (methodName == "ByCss")
        {
            var lineSpan = inv.GetLocation().GetLineSpan();
            warnings.Add(new CodeWarning(
                "ByCss is a last-resort locator. Consider ByText, ByDataTestId, or ByAriaLabel instead.",
                lineSpan.StartLinePosition.Line + 1,
                lineSpan.StartLinePosition.Character + 1));
        }

        // Verify argument is a non-empty string literal
        var args = inv.ArgumentList.Arguments;
        if (args.Count == 0 ||
            args[0].Expression is not LiteralExpressionSyntax literal ||
            literal.Kind() != SyntaxKind.StringLiteralExpression ||
            string.IsNullOrWhiteSpace(literal.Token.ValueText))
        {
            var lineSpan = inv.GetLocation().GetLineSpan();
            warnings.Add(new CodeWarning(
                $"Locator.{methodName}() should have a non-empty string literal argument.",
                lineSpan.StartLinePosition.Line + 1,
                lineSpan.StartLinePosition.Character + 1));
        }
    }
}

private static void ValidateControlTypes(SyntaxTree syntaxTree, List<CodeWarning> warnings)
{
    // Basic check: ensure at least one class declaration exists
    var root = syntaxTree.GetRoot();
    var classDecl = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
    if (classDecl is null)
    {
        warnings.Add(new CodeWarning("No class declaration found in generated code.", 0, 0));
    }
}
```

### Auto-retry logic

```csharp
// Services/RetryService.cs
public sealed class RetryService
{
    private readonly ICopilotService _copilotService;
    private readonly ILogger<RetryService> _logger;
    private const int MaxRetries = 2;

    public async Task<(string Code, ValidationResult Validation)> ValidateWithRetryAsync(
        string code,
        IControlRegistry registry,
        CancellationToken ct = default)
    {
        var validation = CodeValidator.ValidateWithRegistry(code, registry);

        for (var attempt = 0; attempt < MaxRetries && !validation.IsValid; attempt++)
        {
            _logger.LogWarning(
                "Code validation failed (attempt {Attempt}/{Max}), retrying — Errors: {ErrorCount}",
                attempt + 1, MaxRetries, validation.Errors.Count);

            var retryPrompt = BuildRetryPrompt(code, validation);
            var response = await _copilotService.GenerateAsync(retryPrompt, ct);
            var blocks = CodeBlockParser.ExtractCSharpBlocks(response);

            if (blocks.Count == 0)
            {
                _logger.LogWarning("Retry produced no code blocks, keeping original");
                break;
            }

            code = blocks[0];
            validation = CodeValidator.ValidateWithRegistry(code, registry);
        }

        if (!validation.IsValid)
        {
            _logger.LogError(
                "Code validation failed after {MaxRetries} retries — Errors: {Errors}",
                MaxRetries,
                string.Join("; ", validation.Errors.Select(e => e.Message)));
        }

        return (code, validation);
    }

    private static string BuildRetryPrompt(string failedCode, ValidationResult validation)
    {
        var sb = new StringBuilder();
        sb.AppendLine("The generated code has these errors. Please fix and regenerate the complete class:");
        sb.AppendLine();
        foreach (var error in validation.Errors)
        {
            sb.AppendLine($"  Line {error.Line}, Col {error.Column}: {error.Message}");
        }
        sb.AppendLine();
        sb.AppendLine("Original code:");
        sb.AppendLine("```csharp");
        sb.AppendLine(failedCode);
        sb.AppendLine("```");
        return sb.ToString();
    }
}
```

## Checklist

- [ ] `Microsoft.CodeAnalysis.CSharp` NuGet package referenced
- [ ] `CodeValidator.Validate()` parses code with Roslyn and returns syntax errors with line/column
- [ ] `CodeValidator.ValidateWithRegistry()` checks control types against built-in + custom registry
- [ ] Built-in types: TextInputControl, ButtonControl, SelectControl, LabelControl, CheckBoxControl, RadioButtonControl, LinkControl, FileInputControl, TextAreaControl, ImageControl, ElementControl
- [ ] Unknown control types flagged as warnings (not errors)
- [ ] Locator method names validated: ByText, ByLinkText, ByPartialLinkText, ByDataTestId, ByAriaLabel, ById, ByCss
- [ ] `ByCss` usage emits a warning (last-resort locator)
- [ ] Locator arguments validated as non-empty string literals
- [ ] `RetryService.ValidateWithRetryAsync()` re-prompts LLM on validation failure
- [ ] Maximum 2 retry attempts before surfacing errors to user
- [ ] Retry prompt includes original code and specific error messages
- [ ] Empty/null code input handled gracefully
- [ ] Validation and retry attempts logged at Warning/Error levels
