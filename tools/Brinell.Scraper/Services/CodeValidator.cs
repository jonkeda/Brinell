using Brinell.Scraper.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Brinell.Scraper.Services;

public static class CodeValidator
{
    private static readonly HashSet<string> ValidLocatorMethods =
    [
        "ByText", "ByLinkText", "ByPartialLinkText",
        "ByDataTestId", "ByAriaLabel", "ById", "ByCss"
    ];

    private static readonly HashSet<string> BuiltInControlTypes =
    [
        "TextInputControl", "ButtonControl", "SelectControl",
        "LabelControl", "CheckBoxControl", "RadioButtonControl",
        "LinkControl", "FileInputControl", "TextAreaControl",
        "ImageControl", "ElementControl"
    ];

    private static readonly HashSet<string> FrameworkTypes =
    [
        "HtmlPageObjectBase", "ContainerBase", "List",
        "IReadOnlyList", "Task", "ObservableCollection", "Control"
    ];

    public static ValidationResult Validate(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return new ValidationResult { Errors = [new CodeError("Empty code", 0, 0)] };

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

        if (result.IsValid)
        {
            var warnings = new List<CodeWarning>();
            ValidateClassExists(syntaxTree, warnings);
            ValidateLocators(syntaxTree, warnings);
            result.Warnings.AddRange(warnings);
        }

        return result;
    }

    public static ValidationResult ValidateWithRegistry(string code, IControlRegistry registry)
    {
        var result = Validate(code);
        if (!result.IsValid) return result;

        var knownTypes = GetKnownControlTypes(registry);
        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var root = syntaxTree.GetRoot();

        var genericNames = root.DescendantNodes()
            .OfType<GenericNameSyntax>()
            .Select(g => g.Identifier.Text)
            .Distinct();

        foreach (var typeName in genericNames)
        {
            if (!knownTypes.Contains(typeName) && !FrameworkTypes.Contains(typeName))
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
        var known = new HashSet<string>(BuiltInControlTypes, StringComparer.Ordinal);

        var customControls = registry.GetAllControls();
        foreach (var custom in customControls)
            known.Add(custom.Name);

        return known;
    }

    private static void ValidateClassExists(SyntaxTree syntaxTree, List<CodeWarning> warnings)
    {
        var root = syntaxTree.GetRoot();
        var classDecl = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
        if (classDecl is null)
        {
            warnings.Add(new CodeWarning("No class declaration found in generated code.", 0, 0));
        }
    }

    private static void ValidateLocators(SyntaxTree syntaxTree, List<CodeWarning> warnings)
    {
        var root = syntaxTree.GetRoot();

        var invocations = root.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(inv => inv.Expression is MemberAccessExpressionSyntax ma &&
                          ma.Expression is IdentifierNameSyntax id &&
                          id.Identifier.Text == "Locator");

        foreach (var inv in invocations)
        {
            var memberAccess = (MemberAccessExpressionSyntax)inv.Expression;
            var methodName = memberAccess.Name.Identifier.Text;

            if (!ValidLocatorMethods.Contains(methodName))
            {
                var lineSpan = inv.GetLocation().GetLineSpan();
                warnings.Add(new CodeWarning(
                    $"Unknown locator method: Locator.{methodName}()",
                    lineSpan.StartLinePosition.Line + 1,
                    lineSpan.StartLinePosition.Character + 1));
                continue;
            }

            if (methodName == "ByCss")
            {
                var lineSpan = inv.GetLocation().GetLineSpan();
                warnings.Add(new CodeWarning(
                    "ByCss is a last-resort locator. Consider ByText, ByDataTestId, or ByAriaLabel instead.",
                    lineSpan.StartLinePosition.Line + 1,
                    lineSpan.StartLinePosition.Character + 1));
            }

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
}
