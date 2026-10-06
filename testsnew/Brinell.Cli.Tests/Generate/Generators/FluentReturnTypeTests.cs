using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Brinell.Generator.Analysis;
using Brinell.Generator.Generators;
using Brinell.Generator.Models;

namespace Brinell.Generator.Tests.Generators;

/// <summary>
/// Tests for fluent-return-type resolution. Controls return their containing scope
/// (<c>TScope</c>); containers and collections return themselves (<c>TSelf</c>) so a
/// chain stays inside the container.
/// </summary>
public class FluentReturnTypeTests
{
    private readonly IsWaitAssertGenerator _stateGenerator = new();
    private readonly ActionGenerator _actionGenerator = new();
    private readonly ControlObjectAnalyzer _analyzer = new();

    private ControlObjectContext ContextFor(string code, string? className = null)
    {
        var (classDecl, root) = _analyzer.FindTarget(code, className);
        return _analyzer.BuildContext(classDecl!, root);
    }

    private static MethodDeclarationSyntax FirstMethod(string code)
    {
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = (CompilationUnitSyntax)tree.GetRoot();
        return root.DescendantNodes().OfType<MethodDeclarationSyntax>().First();
    }

    #region Resolution rules

    [Fact]
    public void SingleTypeParameter_ResolvesToThatParameter()
    {
        var code = "public class Control<TScope> { }";
        var (classDecl, _) = _analyzer.FindTarget(code);

        Assert.Equal("TScope", _analyzer.ResolveFluentReturnType(classDecl!));
    }

    [Fact]
    public void SingleTypeParameter_WithNonStandardName_ResolvesToIt()
    {
        var code = "public class Control<TOwner> { }";
        var (classDecl, _) = _analyzer.FindTarget(code);

        Assert.Equal("TOwner", _analyzer.ResolveFluentReturnType(classDecl!));
    }

    [Fact]
    public void TSelf_WinsOverOtherTypeParameters()
    {
        var code = "public class Container<TParent, TSelf> { }";
        var (classDecl, _) = _analyzer.FindTarget(code);

        Assert.Equal("TSelf", _analyzer.ResolveFluentReturnType(classDecl!));
    }

    [Fact]
    public void TSelf_WinsRegardlessOfPosition()
    {
        var code = "public class Collection<TSelf, TParent, TItem> { }";
        var (classDecl, _) = _analyzer.FindTarget(code);

        Assert.Equal("TSelf", _analyzer.ResolveFluentReturnType(classDecl!));
    }

    [Fact]
    public void NoTypeParameters_ResolvesToEmpty()
    {
        var code = "public class Control { }";
        var (classDecl, _) = _analyzer.FindTarget(code);

        Assert.Equal("", _analyzer.ResolveFluentReturnType(classDecl!));
    }

    [Fact]
    public void ClosedSelfReference_ResolvesToTheClassItself()
    {
        // A concrete collection closes the self-reference its base passes on, so its
        // members return the collection, not the parent scope.
        var code = "public class Toolbar<TParent> : CollectionObjectBase<TParent, Toolbar<TParent>, ToolbarItem<TParent>> { }";
        var (classDecl, _) = _analyzer.FindTarget(code);

        Assert.Equal("Toolbar<TParent>", _analyzer.ResolveFluentReturnType(classDecl!));
    }

    [Fact]
    public void ClosedSelfReference_IsFoundThroughAQualifiedBaseName()
    {
        var code = "public class ToolbarItem<TParent> : Base.ClickableItemBase<Toolbar<TParent>, ToolbarItem<TParent>> { }";
        var (classDecl, _) = _analyzer.FindTarget(code);

        Assert.Equal("ToolbarItem<TParent>", _analyzer.ResolveFluentReturnType(classDecl!));
    }

    [Fact]
    public void ControlBase_IsNotMistakenForASelfReference()
    {
        // A control passes its scope to its base, not itself: the single-parameter rule
        // must still win.
        var code = "public class Button<TScope> : ClickableControlBase<TScope> { }";
        var (classDecl, _) = _analyzer.FindTarget(code);

        Assert.Equal("TScope", _analyzer.ResolveFluentReturnType(classDecl!));
    }

    [Fact]
    public void FluentReturnAttribute_OverridesInference()
    {
        var code = @"
[FluentReturn(""TParent"")]
public class Container<TParent, TSelf> { }";
        var (classDecl, _) = _analyzer.FindTarget(code);

        Assert.Equal("TParent", _analyzer.ResolveFluentReturnType(classDecl!));
    }

    [Fact]
    public void FluentReturnAttribute_AcceptsNameof()
    {
        var code = @"
[FluentReturn(nameof(TParent))]
public class Container<TParent, TSelf> { }";
        var (classDecl, _) = _analyzer.FindTarget(code);

        Assert.Equal("TParent", _analyzer.ResolveFluentReturnType(classDecl!));
    }

    [Fact]
    public void FluentReturnAttribute_AcceptsAttributeSuffix()
    {
        var code = @"
[FluentReturnAttribute(""TParent"")]
public class Container<TParent, TSelf> { }";
        var (classDecl, _) = _analyzer.FindTarget(code);

        Assert.Equal("TParent", _analyzer.ResolveFluentReturnType(classDecl!));
    }

    [Fact]
    public void FluentReturnAttribute_NamingUndeclaredParameter_Throws()
    {
        var code = @"
[FluentReturn(""TNope"")]
public class Container<TParent, TSelf> { }";
        var (classDecl, _) = _analyzer.FindTarget(code);

        var ex = Assert.Throws<InvalidOperationException>(
            () => _analyzer.ResolveFluentReturnType(classDecl!));
        Assert.Contains("TNope", ex.Message);
    }

    /// <summary>
    /// Ambiguity must fail loudly rather than pick a parameter at random.
    /// </summary>
    [Fact]
    public void MultipleTypeParameters_WithoutTSelfOrAttribute_Throws()
    {
        var code = "public class Container<TParent, TChild> { }";
        var (classDecl, _) = _analyzer.FindTarget(code);

        var ex = Assert.Throws<InvalidOperationException>(
            () => _analyzer.ResolveFluentReturnType(classDecl!));
        Assert.Contains("FluentReturn", ex.Message);
    }

    #endregion

    #region Emitted signatures

    [Fact]
    public void Control_EmitsAssertReturningTScope()
    {
        var code = @"
public class Control<TScope> {
    protected virtual bool? IsVisibleCore(IMauiElement? element) => element?.Visible;
}";
        var generated = _stateGenerator.Generate(
            _stateGenerator.Extract(FirstMethod(code)), ContextFor(code));

        Assert.Contains("public TScope AssertVisible(", generated);
    }

    /// <summary>
    /// The reason this work exists: a container's own members must return the
    /// container, not its parent.
    /// </summary>
    [Fact]
    public void Container_EmitsAssertReturningTSelf()
    {
        var code = @"
public class Container<TParent, TSelf> {
    protected virtual bool? IsVisibleCore(IMauiElement? element) => element?.Visible;
}";
        var generated = _stateGenerator.Generate(
            _stateGenerator.Extract(FirstMethod(code)), ContextFor(code));

        Assert.Contains("public TSelf AssertVisible(", generated);
        Assert.DoesNotContain("public TScope", generated);
        Assert.DoesNotContain("TParent, TSelf Assert", generated);
    }

    [Fact]
    public void Container_EmitsGetterAssertReturningTSelf()
    {
        var code = @"
public class Container<TParent, TSelf> {
    protected virtual string? GetTextCore(IMauiElement element) => element.Text;
}";
        var generated = _stateGenerator.Generate(
            _stateGenerator.Extract(FirstMethod(code)), ContextFor(code));

        Assert.Contains("public TSelf AssertText(", generated);
    }

    [Fact]
    public void Control_EmitsActionReturningTScope()
    {
        var code = @"
public class Control<TScope> {
    protected virtual void ClickCore(IMauiElement element) { }
}";
        var generated = _actionGenerator.Generate(
            _actionGenerator.Extract(FirstMethod(code)), ContextFor(code));

        Assert.Contains("public TScope Click(", generated);
    }

    /// <summary>
    /// Guards the ActionGenerator bug directly: it previously used the whole type
    /// parameter list, emitting the uncompilable "public TParent, TSelf Click(...)".
    /// </summary>
    [Fact]
    public void Container_EmitsActionReturningTSelf()
    {
        var code = @"
public class Container<TParent, TSelf> {
    protected virtual void ClickCore(IMauiElement element) { }
}";
        var generated = _actionGenerator.Generate(
            _actionGenerator.Extract(FirstMethod(code)), ContextFor(code));

        Assert.Contains("public TSelf Click(", generated);
        Assert.DoesNotContain("TParent, TSelf Click", generated);
    }

    /// <summary>
    /// A type-parameterless class has no scope to return, so the action is void and
    /// must not return the helper's result.
    /// </summary>
    [Fact]
    public void NoTypeParameters_EmitsVoidActionWithoutReturn()
    {
        var code = @"
public class Control {
    protected virtual void ClickCore(IMauiElement element) { }
}";
        var generated = _actionGenerator.Generate(
            _actionGenerator.Extract(FirstMethod(code)), ContextFor(code));

        Assert.Contains("public void Click(", generated);
        Assert.DoesNotContain("return RunDoWithElement", generated);
        Assert.Contains("RunDoWithElement(", generated);
    }

    #endregion
}
