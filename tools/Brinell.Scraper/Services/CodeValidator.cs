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

    public static ValidationResult ValidateWithRegistry(
        string mainCode,
        IReadOnlyList<GeneratedControl> registry,
        IReadOnlyList<string> containerCodes)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(mainCode))
        {
            result.Errors.Add(new CodeError("Empty main code", 0, 0));
            return result;
        }

        var mainTree = CSharpSyntaxTree.ParseText(mainCode);
        AddSyntaxErrors(mainTree, result.Errors);

        var containerTrees = new List<SyntaxTree>(containerCodes.Count);
        foreach (var c in containerCodes)
        {
            if (string.IsNullOrWhiteSpace(c)) continue;
            var tree = CSharpSyntaxTree.ParseText(c);
            AddSyntaxErrors(tree, result.Errors);
            containerTrees.Add(tree);
        }

        if (!result.IsValid)
            return result;

        var mainRoot = mainTree.GetRoot();
        var containerRoots = containerTrees.Select(t => t.GetRoot()).ToList();

        var registryNames = new HashSet<string>(
            registry.Select(c => c.Name), StringComparer.Ordinal);
        var inlineContainerNames = new HashSet<string>(
            containerRoots
                .SelectMany(r => r.DescendantNodes().OfType<ClassDeclarationSyntax>())
                .Select(c => c.Identifier.Text)
                .Concat(mainRoot.DescendantNodes().OfType<ClassDeclarationSyntax>()
                    .Select(c => c.Identifier.Text)),
            StringComparer.Ordinal);

        var knownTypes = new HashSet<string>(BuiltInControlTypes, StringComparer.Ordinal);
        knownTypes.UnionWith(FrameworkTypes);
        knownTypes.UnionWith(registryNames);
        knownTypes.UnionWith(inlineContainerNames);

        var allRoots = new List<SyntaxNode> { mainRoot };
        allRoots.AddRange(containerRoots);

        // Type resolution — Error if a generic type identifier is unknown.
        foreach (var root in allRoots)
        {
            foreach (var generic in root.DescendantNodes().OfType<GenericNameSyntax>())
            {
                var name = generic.Identifier.Text;
                if (knownTypes.Contains(name)) continue;

                var lineSpan = generic.GetLocation().GetLineSpan();
                result.Errors.Add(new CodeError(
                    $"Unresolved type '{name}'. Not in built-ins, framework types, control registry, " +
                    "or inline containers.",
                    lineSpan.StartLinePosition.Line + 1,
                    lineSpan.StartLinePosition.Character + 1));
            }
        }

        // Locator usage — ByCss is a Warning.
        foreach (var root in allRoots)
            CollectByCssWarnings(root, result.Warnings);

        // Property name uniqueness within each class — Error on duplicates.
        foreach (var root in allRoots)
        {
            foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                var dupes = classDecl.Members
                    .OfType<PropertyDeclarationSyntax>()
                    .GroupBy(p => p.Identifier.Text, StringComparer.Ordinal)
                    .Where(g => g.Count() > 1);

                foreach (var dup in dupes)
                {
                    var second = dup.Skip(1).First();
                    var lineSpan = second.GetLocation().GetLineSpan();
                    result.Errors.Add(new CodeError(
                        $"Duplicate property '{dup.Key}' in class '{classDecl.Identifier.Text}'.",
                        lineSpan.StartLinePosition.Line + 1,
                        lineSpan.StartLinePosition.Character + 1));
                }
            }
        }

        // Main class must derive from HtmlPageObjectBase<Self>.
        var mainClass = mainRoot.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
        if (mainClass is null)
        {
            result.Errors.Add(new CodeError(
                "Main code does not contain a class declaration.", 0, 0));
        }
        else if (!DerivesFromGeneric(mainClass, "HtmlPageObjectBase", out var pageArgs)
            || pageArgs.Count != 1
            || !string.Equals(pageArgs[0], mainClass.Identifier.Text, StringComparison.Ordinal))
        {
            var lineSpan = mainClass.GetLocation().GetLineSpan();
            result.Errors.Add(new CodeError(
                $"Class '{mainClass.Identifier.Text}' must derive from " +
                $"HtmlPageObjectBase<{mainClass.Identifier.Text}>.",
                lineSpan.StartLinePosition.Line + 1,
                lineSpan.StartLinePosition.Character + 1));
        }

        // Each inline container must derive from ContainerBase<TParent, TScope>.
        foreach (var root in containerRoots)
        {
            foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                if (!DerivesFromGeneric(classDecl, "ContainerBase", out var args) || args.Count != 2)
                {
                    var lineSpan = classDecl.GetLocation().GetLineSpan();
                    result.Errors.Add(new CodeError(
                        $"Inline container '{classDecl.Identifier.Text}' must derive from " +
                        "ContainerBase<TParent, TScope>.",
                        lineSpan.StartLinePosition.Line + 1,
                        lineSpan.StartLinePosition.Character + 1));
                }
            }
        }

        return result;
    }

    private static void AddSyntaxErrors(SyntaxTree tree, List<CodeError> errors)
    {
        foreach (var d in tree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error))
        {
            var span = d.Location.GetLineSpan();
            errors.Add(new CodeError(
                d.GetMessage(),
                span.StartLinePosition.Line + 1,
                span.StartLinePosition.Character + 1));
        }
    }

    private static void CollectByCssWarnings(SyntaxNode root, List<CodeWarning> warnings)
    {
        var invocations = root.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(inv => inv.Expression is MemberAccessExpressionSyntax ma &&
                          ma.Expression is IdentifierNameSyntax id &&
                          id.Identifier.Text == "Locator" &&
                          ma.Name.Identifier.Text == "ByCss");

        foreach (var inv in invocations)
        {
            var lineSpan = inv.GetLocation().GetLineSpan();
            warnings.Add(new CodeWarning(
                "ByCss is a last-resort locator. Consider ByText, ByDataTestId, or ByAriaLabel instead.",
                lineSpan.StartLinePosition.Line + 1,
                lineSpan.StartLinePosition.Character + 1));
        }
    }

    private static bool DerivesFromGeneric(
        ClassDeclarationSyntax classDecl, string baseName, out List<string> typeArgs)
    {
        typeArgs = [];
        if (classDecl.BaseList is null) return false;
        foreach (var b in classDecl.BaseList.Types)
        {
            if (b.Type is GenericNameSyntax g && g.Identifier.Text == baseName)
            {
                typeArgs = g.TypeArgumentList.Arguments.Select(a => a.ToString()).ToList();
                return true;
            }
        }
        return false;
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
