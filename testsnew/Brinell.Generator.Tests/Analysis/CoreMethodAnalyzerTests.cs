using Microsoft.CodeAnalysis.CSharp;
using Brinell.Generator.Analysis;
using Brinell.Generator.Handlers;
using Brinell.Generator.Models;
using Brinell.Generator.Tests.Fixtures;

namespace Brinell.Generator.Tests.Analysis;

public class CoreMethodAnalyzerTests
{
    [Fact]
    public void FindsClassInFileScopedNamespace()
    {
        // Arrange
        var analyzer = new CoreMethodAnalyzer();

        // Act
        var result = analyzer.AnalyzeCode(
            SampleCodeFixtures.SimpleClickableClass,
            new List<IMethodHandler> { new CoreMethodHandler() },
            "SimpleClickable");

        // Assert
        Assert.NotNull(result.ClassDecl);
        Assert.Equal("SimpleClickable", result.ClassDecl.Identifier.Text);
    }

    [Fact]
    public void ExtractsMethodsUsingHandlers()
    {
        // Arrange
        var analyzer = new CoreMethodAnalyzer();

        // Act
        var result = analyzer.AnalyzeCode(
            SampleCodeFixtures.MultiMethodClass,
            new List<IMethodHandler> { new CoreMethodHandler() },
            "MultiMethod");

        // Assert
        Assert.NotNull(result.ClassDecl);
        Assert.Equal(2, result.Methods.Count);
        Assert.Contains(result.Methods, m => m.PublicMethodName == "Click");
        Assert.Contains(result.Methods, m => m.PublicMethodName == "Hover");
    }

    [Fact]
    public void ReturnsEmptyListWhenNoMethodsMatch()
    {
        // Arrange
        var analyzer = new CoreMethodAnalyzer();

        // Act
        var result = analyzer.AnalyzeCode(
            SampleCodeFixtures.NoMethodsClass,
            new List<IMethodHandler> { new CoreMethodHandler() },
            "Empty");

        // Assert
        Assert.NotNull(result.ClassDecl);
        Assert.Empty(result.Methods);
    }

    [Fact]
    public void ExtractsTypeParametersCorrectly()
    {
        // Arrange
        var tree = CSharpSyntaxTree.ParseText(SampleCodeFixtures.SimpleClickableClass);
        var root = (Microsoft.CodeAnalysis.CSharp.Syntax.CompilationUnitSyntax)tree.GetRoot();
        var analyzer = new CoreMethodAnalyzer();

        // Act
        var typeParams = analyzer.GetTypeParameters(root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>()
            .First());

        // Assert
        Assert.Equal("<TScope>", typeParams);
    }

    [Fact]
    public void GetNamespace_ReturnsCorrectNamespace()
    {
        // Arrange
        var tree = CSharpSyntaxTree.ParseText(SampleCodeFixtures.SimpleClickableClass);
        var root = (Microsoft.CodeAnalysis.CSharp.Syntax.CompilationUnitSyntax)tree.GetRoot();
        var analyzer = new CoreMethodAnalyzer();

        // Act
        var ns = analyzer.GetNamespace(root);

        // Assert
        Assert.Equal("Brinell.Maui.Controls", ns);
    }

    [Fact]
    public void GetUsingStatements_ReturnsEmptyListWhenNonePresent()
    {
        // Arrange
        var tree = CSharpSyntaxTree.ParseText(SampleCodeFixtures.SimpleClickableClass);
        var root = (Microsoft.CodeAnalysis.CSharp.Syntax.CompilationUnitSyntax)tree.GetRoot();
        var analyzer = new CoreMethodAnalyzer();

        // Act
        var usings = analyzer.GetUsingStatements(root);

        // Assert
        Assert.Empty(usings);
    }

    [Fact]
    public void ReturnsNullClassDeclWhenClassNotFound()
    {
        // Arrange
        var analyzer = new CoreMethodAnalyzer();

        // Act
        var result = analyzer.AnalyzeCode(
            SampleCodeFixtures.SimpleClickableClass,
            new List<IMethodHandler> { new CoreMethodHandler() },
            "NonExistentClass");

        // Assert
        Assert.Null(result.ClassDecl);
        Assert.Empty(result.Methods);
    }

    [Fact]
    public void PreservesGenericConstraints()
    {
        // Arrange
        var tree = CSharpSyntaxTree.ParseText(SampleCodeFixtures.SimpleClickableClass);
        var root = (Microsoft.CodeAnalysis.CSharp.Syntax.CompilationUnitSyntax)tree.GetRoot();
        var classDecl = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>()
            .First();

        // Act
        var constraints = classDecl.ConstraintClauses;

        // Assert
        Assert.NotEmpty(constraints);
    }
}

