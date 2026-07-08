using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Brinell.Generator.Handlers;
using Brinell.Generator.Tests.Fixtures;

namespace Brinell.Generator.Tests.Handlers;

public class CoreMethodHandlerTests
{
    [Fact]
    public void Matches_ProtectedVirtualCoreMethod_ReturnsTrue()
    {
        // Arrange
        var tree = CSharpSyntaxTree.ParseText(SampleCodeFixtures.SimpleClickableClass);
        var root = (CompilationUnitSyntax)tree.GetRoot();
        var classDecl = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First();
        var method = classDecl.Members.OfType<MethodDeclarationSyntax>().First();
        var handler = new CoreMethodHandler();

        // Act
        var result = handler.Matches(method);

        // Assert
        Assert.True(result, "Protected virtual ClickCore method should match");
    }

    [Fact]
    public void Matches_NonCoreMethod_ReturnsFalse()
    {
        // Arrange
        var tree = CSharpSyntaxTree.ParseText(SampleCodeFixtures.MethodWithoutCoreSuffix);
        var root = (CompilationUnitSyntax)tree.GetRoot();
        var classDecl = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First();
        var method = classDecl.Members.OfType<MethodDeclarationSyntax>().First();
        var handler = new CoreMethodHandler();

        // Act
        var result = handler.Matches(method);

        // Assert
        Assert.False(result, "Method without Core suffix should not match");
    }

    [Fact]
    public void Matches_PublicMethod_ReturnsFalse()
    {
        // Arrange
        var tree = CSharpSyntaxTree.ParseText(SampleCodeFixtures.PublicCoreMethod);
        var root = (CompilationUnitSyntax)tree.GetRoot();
        var classDecl = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First();
        var method = classDecl.Members.OfType<MethodDeclarationSyntax>().First();
        var handler = new CoreMethodHandler();

        // Act
        var result = handler.Matches(method);

        // Assert
        Assert.False(result, "Public method should not match (must be protected)");
    }

    [Fact]
    public void Matches_NonVirtualMethod_ReturnsFalse()
    {
        // Arrange
        var tree = CSharpSyntaxTree.ParseText(SampleCodeFixtures.ProtectedNonVirtualMethod);
        var root = (CompilationUnitSyntax)tree.GetRoot();
        var classDecl = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First();
        var method = classDecl.Members.OfType<MethodDeclarationSyntax>().First();
        var handler = new CoreMethodHandler();

        // Act
        var result = handler.Matches(method);

        // Assert
        Assert.False(result, "Non-virtual method should not match (must be virtual)");
    }

    [Fact]
    public void Extract_StripsCoreFromMethodName()
    {
        // Arrange
        var tree = CSharpSyntaxTree.ParseText(SampleCodeFixtures.SimpleClickableClass);
        var root = (CompilationUnitSyntax)tree.GetRoot();
        var classDecl = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First();
        var method = classDecl.Members.OfType<MethodDeclarationSyntax>().First();
        var handler = new CoreMethodHandler();

        // Act
        var methodInfo = handler.Extract(method);

        // Assert
        Assert.NotNull(methodInfo);
        Assert.Equal("Click", methodInfo.PublicMethodName);
    }

    [Fact]
    public void Extract_PreservesParameters_SkipsFirst()
    {
        // Arrange
        var tree = CSharpSyntaxTree.ParseText(SampleCodeFixtures.SimpleClickableClass);
        var root = (CompilationUnitSyntax)tree.GetRoot();
        var classDecl = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First();
        var method = classDecl.Members.OfType<MethodDeclarationSyntax>().First();
        var handler = new CoreMethodHandler();

        // Act
        var methodInfo = handler.Extract(method);

        // Assert
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
        // Arrange
        var tree = CSharpSyntaxTree.ParseText(SampleCodeFixtures.MultipleParameters);
        var root = (CompilationUnitSyntax)tree.GetRoot();
        var classDecl = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First();
        var method = classDecl.Members.OfType<MethodDeclarationSyntax>().First();
        var handler = new CoreMethodHandler();

        // Act
        var methodInfo = handler.Extract(method);

        // Assert
        Assert.NotNull(methodInfo);
        Assert.Equal(3, methodInfo.Parameters.Count);
        
        // Second param (first after element)
        Assert.Equal("int?", methodInfo.Parameters[0].TypeName);
        Assert.Equal("timeoutMs", methodInfo.Parameters[0].ParameterName);
        
        // Third param
        Assert.Equal("string?", methodInfo.Parameters[1].TypeName);
        Assert.Equal("context", methodInfo.Parameters[1].ParameterName);
        
        // Fourth param
        Assert.Equal("bool", methodInfo.Parameters[2].TypeName);
        Assert.Equal("force", methodInfo.Parameters[2].ParameterName);
    }

    [Fact]
    public void Extract_PreservesXmlDocumentation()
    {
        // Arrange
        var tree = CSharpSyntaxTree.ParseText(SampleCodeFixtures.WithXmlDocumentation);
        var root = (CompilationUnitSyntax)tree.GetRoot();
        var classDecl = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First();
        var method = classDecl.Members.OfType<MethodDeclarationSyntax>().First();
        var handler = new CoreMethodHandler();

        // Act
        var methodInfo = handler.Extract(method);

        // Assert
        Assert.NotNull(methodInfo);
        Assert.NotNull(methodInfo.XmlDocumentation);
        Assert.NotEmpty(methodInfo.XmlDocumentation);
        Assert.Contains("Clicks the element", methodInfo.XmlDocumentation);
    }
}
