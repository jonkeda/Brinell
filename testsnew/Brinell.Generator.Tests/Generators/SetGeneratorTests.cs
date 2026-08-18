using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Brinell.Generator.Analysis;
using Brinell.Generator.Generators;
using Brinell.Generator.Models;

namespace Brinell.Generator.Tests.Generators;

public class SetGeneratorTests
{
    private const string SimpleSetterClass = @"namespace Brinell.Maui.Controls;

public abstract class SimpleSetter<TScope>
    where TScope : IScope<TScope>
{
    protected virtual void SetTextCore(IMauiElement element, string text, int? timeoutMs = null)
    {
        element.SendKeys(text);
    }
}";

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
    public void Matches_ProtectedVirtualSetCoreMethod_ReturnsTrue()
    {
        var method = FirstMethod(SimpleSetterClass);
        var generator = new SetGenerator();

        Assert.True(generator.Matches(method), "protected virtual Set*Core should match");
    }

    [Fact]
    public void Matches_SetCoreWithoutValueParameter_ReturnsFalse()
    {
        // A setter needs the element plus something to write.
        var code = @"
public class Test {
    protected virtual void SetFocusCore(IMauiElement element) { }
}";
        var generator = new SetGenerator();

        Assert.False(generator.Matches(FirstMethod(code)),
            "Set*Core with no value parameter is an action, not a setter");
    }

    [Fact]
    public void Matches_NonVirtualSetCore_ReturnsFalse()
    {
        var code = @"
public class Test {
    protected void SetTextCore(IMauiElement element, string text) { }
}";
        var generator = new SetGenerator();

        Assert.False(generator.Matches(FirstMethod(code)), "non-virtual should not match");
    }

    [Fact]
    public void Matches_OverrideSetCore_ReturnsFalse()
    {
        var code = @"
public class Test {
    protected override void SetTextCore(IMauiElement element, string text) { }
}";
        var generator = new SetGenerator();

        Assert.False(generator.Matches(FirstMethod(code)),
            "override should not regenerate the base wrapper");
    }

    [Fact]
    public void ActionGenerator_DoesNotClaimSetCore()
    {
        var method = FirstMethod(SimpleSetterClass);

        Assert.False(new ActionGenerator().Matches(method),
            "Set*Core belongs to SetGenerator, not the action family");
    }

    [Fact]
    public void Extract_StripsCoreAndSkipsElement()
    {
        var method = FirstMethod(SimpleSetterClass);
        var generator = new SetGenerator();

        var info = generator.Extract(method);

        Assert.Equal("SetText", info.PublicMethodName);
        Assert.Equal(2, info.Parameters.Count);
        Assert.Equal("text", info.Parameters[0].ParameterName);
        Assert.Equal("timeoutMs", info.Parameters[1].ParameterName);
    }

    [Fact]
    public void Generate_UsesRunSetWithElementAndForwardsTimeout()
    {
        var method = FirstMethod(SimpleSetterClass);
        var generator = new SetGenerator();
        var info = generator.Extract(method);
        var context = ContextFor(SimpleSetterClass);

        var generated = generator.Generate(info, context);

        Assert.Contains("public TScope SetText(string text, int? timeoutMs = null)", generated);
        Assert.Contains("RunSetWithElement(text,", generated);
        Assert.Contains("SetTextCore(element, text, timeoutMs)", generated);
        // timeoutMs forwarded as the RunSetWithElement timeout argument.
        Assert.Contains("}, timeoutMs);", generated);
    }
}
