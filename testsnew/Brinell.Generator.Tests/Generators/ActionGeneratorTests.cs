using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Brinell.Generator.Analysis;
using Brinell.Generator.Generators;
using Brinell.Generator.Models;
using Brinell.Generator.Tests.Fixtures;

namespace Brinell.Generator.Tests.Generators;

public class ActionGeneratorTests
{
    private static MethodDeclarationSyntax FirstMethod(string code)
    {
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = (CompilationUnitSyntax)tree.GetRoot();
        var classDecl = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First();
        return classDecl.Members.OfType<MethodDeclarationSyntax>().First();
    }

    private static ControlObjectContext ContextFor(string code, string? className = null)
    {
        var analyzer = new ControlObjectAnalyzer();
        var (classDecl, root) = analyzer.FindTarget(code, className);
        return analyzer.BuildContext(classDecl!, root);
    }

    [Fact]
    public void Matches_ProtectedVirtualCoreMethod_ReturnsTrue()
    {
        var method = FirstMethod(SampleCodeFixtures.SimpleClickableClass);
        var generator = new ActionGenerator();

        var result = generator.Matches(method);

        Assert.True(result, "Protected virtual ClickCore method should match");
    }

    [Fact]
    public void Matches_NonCoreMethod_ReturnsFalse()
    {
        var method = FirstMethod(SampleCodeFixtures.MethodWithoutCoreSuffix);
        var generator = new ActionGenerator();

        var result = generator.Matches(method);

        Assert.False(result, "Method without Core suffix should not match");
    }

    [Fact]
    public void Matches_PublicMethod_ReturnsFalse()
    {
        var method = FirstMethod(SampleCodeFixtures.PublicCoreMethod);
        var generator = new ActionGenerator();

        var result = generator.Matches(method);

        Assert.False(result, "Public method should not match (must be protected)");
    }

    [Fact]
    public void Matches_NonVirtualMethod_ReturnsFalse()
    {
        var method = FirstMethod(SampleCodeFixtures.ProtectedNonVirtualMethod);
        var generator = new ActionGenerator();

        var result = generator.Matches(method);

        Assert.False(result, "Non-virtual method should not match (must be virtual)");
    }

    [Fact]
    public void Matches_IsCoreStateQuery_ReturnsFalse()
    {
        // Arrange - Is*Core belongs to the Is/Wait/Assert family, not actions.
        var code = @"
public class Test {
    protected virtual bool? IsVisibleCore(IMauiElement? element) => element?.Visible;
}";
        var method = FirstMethod(code);
        var generator = new ActionGenerator();

        // Act
        var result = generator.Matches(method);

        // Assert
        Assert.False(result, "Is*Core should be excluded from the action family");
    }

    [Fact]
    public void Matches_GetCoreValueQuery_ReturnsFalse()
    {
        // Arrange - Get*Core belongs to the Is/Wait/Assert family, not actions.
        var code = @"
public class Test {
    protected virtual string? GetTextCore(IMauiElement element) => element.Text;
}";
        var method = FirstMethod(code);
        var generator = new ActionGenerator();

        // Act
        var result = generator.Matches(method);

        // Assert
        Assert.False(result, "Get*Core should be excluded from the action family");
    }

    [Fact]
    public void Extract_StripsCoreFromMethodName()
    {
        var method = FirstMethod(SampleCodeFixtures.SimpleClickableClass);
        var generator = new ActionGenerator();

        var methodInfo = generator.Extract(method);

        Assert.NotNull(methodInfo);
        Assert.Equal("Click", methodInfo.PublicMethodName);
    }

    [Fact]
    public void Extract_PreservesParameters_SkipsFirst()
    {
        var method = FirstMethod(SampleCodeFixtures.SimpleClickableClass);
        var generator = new ActionGenerator();

        var methodInfo = generator.Extract(method);

        Assert.NotNull(methodInfo);
        Assert.Single(methodInfo.Parameters);
        var param = methodInfo.Parameters.First();
        Assert.Equal("int?", param.TypeName);
        Assert.Equal("timeoutMs", param.ParameterName);
        Assert.Equal("null", param.DefaultValue);
    }

    [Fact]
    public void Extract_MultipleParameters()
    {
        var method = FirstMethod(SampleCodeFixtures.MultipleParameters);
        var generator = new ActionGenerator();

        var methodInfo = generator.Extract(method);

        Assert.NotNull(methodInfo);
        Assert.Equal(3, methodInfo.Parameters.Count);

        Assert.Equal("int?", methodInfo.Parameters[0].TypeName);
        Assert.Equal("timeoutMs", methodInfo.Parameters[0].ParameterName);

        Assert.Equal("string?", methodInfo.Parameters[1].TypeName);
        Assert.Equal("context", methodInfo.Parameters[1].ParameterName);

        Assert.Equal("bool", methodInfo.Parameters[2].TypeName);
        Assert.Equal("force", methodInfo.Parameters[2].ParameterName);
    }

    [Fact]
    public void Extract_PreservesXmlDocumentation()
    {
        var method = FirstMethod(SampleCodeFixtures.WithXmlDocumentation);
        var generator = new ActionGenerator();

        var methodInfo = generator.Extract(method);

        Assert.NotNull(methodInfo);
        Assert.NotNull(methodInfo.XmlDocumentation);
        Assert.NotEmpty(methodInfo.XmlDocumentation);
        Assert.Contains("Clicks the element", methodInfo.XmlDocumentation);
    }

    [Fact]
    public void Generate_EmitsRunDoWithElementAndClickableGuard()
    {
        var method = FirstMethod(SampleCodeFixtures.SimpleClickableClass);
        var generator = new ActionGenerator();
        var info = generator.Extract(method);
        var context = ContextFor(SampleCodeFixtures.SimpleClickableClass);

        var generated = generator.Generate(info, context);

        Assert.Contains("public TScope Click(int? timeoutMs = null)", generated);
        Assert.Contains("RunDoWithElement", generated);
        Assert.Contains("ClickCore(element", generated);
    }
}
