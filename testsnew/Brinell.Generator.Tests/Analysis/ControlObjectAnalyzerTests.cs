using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Brinell.Generator.Analysis;
using Brinell.Generator.Generators;
using Brinell.Generator.Tests.Fixtures;

namespace Brinell.Generator.Tests.Analysis;

public class ControlObjectAnalyzerTests
{
    private static int CountMatches(string code, string? className, IMemberGenerator generator)
    {
        var analyzer = new ControlObjectAnalyzer();
        var (classDecl, _) = analyzer.FindTarget(code, className);
        Assert.NotNull(classDecl);
        return analyzer.CoreMethods(classDecl!).Count(generator.Matches);
    }

    [Fact]
    public void FindsClassInFileScopedNamespace()
    {
        var analyzer = new ControlObjectAnalyzer();

        var (classDecl, _) = analyzer.FindTarget(SampleCodeFixtures.SimpleClickableClass, "SimpleClickable");

        Assert.NotNull(classDecl);
        Assert.Equal("SimpleClickable", classDecl!.Identifier.Text);
    }

    [Fact]
    public void CoreMethods_MatchedByActionGenerator()
    {
        var count = CountMatches(SampleCodeFixtures.MultiMethodClass, "MultiMethod", new ActionGenerator());

        Assert.Equal(2, count);
    }

    [Fact]
    public void CoreMethods_ReturnsNoActionMatchesForEmptyClass()
    {
        var count = CountMatches(SampleCodeFixtures.NoMethodsClass, "Empty", new ActionGenerator());

        Assert.Equal(0, count);
    }

    [Fact]
    public void BuildContext_ExtractsTypeParametersCorrectly()
    {
        var analyzer = new ControlObjectAnalyzer();
        var (classDecl, root) = analyzer.FindTarget(SampleCodeFixtures.SimpleClickableClass);

        var context = analyzer.BuildContext(classDecl!, root);

        Assert.Equal("<TScope>", context.TypeParameters);
    }

    [Fact]
    public void BuildContext_ReturnsCorrectNamespace()
    {
        var analyzer = new ControlObjectAnalyzer();
        var (classDecl, root) = analyzer.FindTarget(SampleCodeFixtures.SimpleClickableClass);

        var context = analyzer.BuildContext(classDecl!, root);

        Assert.Equal("Brinell.Maui.Controls", context.Namespace);
    }

    [Fact]
    public void BuildContext_ReturnsEmptyUsingsWhenNonePresent()
    {
        var analyzer = new ControlObjectAnalyzer();
        var (classDecl, root) = analyzer.FindTarget(SampleCodeFixtures.SimpleClickableClass);

        var context = analyzer.BuildContext(classDecl!, root);

        Assert.Empty(context.Usings);
    }

    [Fact]
    public void BuildContext_DetectsElementType()
    {
        var analyzer = new ControlObjectAnalyzer();
        var (classDecl, root) = analyzer.FindTarget(SampleCodeFixtures.SimpleClickableClass);

        var context = analyzer.BuildContext(classDecl!, root);

        Assert.Equal("IMauiElement", context.ElementType);
    }

    [Fact]
    public void FindTarget_ReturnsNullClassDeclWhenClassNotFound()
    {
        var analyzer = new ControlObjectAnalyzer();

        var (classDecl, _) = analyzer.FindTarget(SampleCodeFixtures.SimpleClickableClass, "NonExistentClass");

        Assert.Null(classDecl);
    }

    [Fact]
    public void PreservesGenericConstraints()
    {
        var tree = CSharpSyntaxTree.ParseText(SampleCodeFixtures.SimpleClickableClass);
        var root = (CompilationUnitSyntax)tree.GetRoot();
        var classDecl = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First();

        var constraints = classDecl.ConstraintClauses;

        Assert.NotEmpty(constraints);
    }
}
