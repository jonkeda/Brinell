using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Brinell.Generator.Analysis;
using Brinell.Generator.Generators;
using Brinell.Generator.Models;

namespace Brinell.Generator.Tests.Generators;

/// <summary>
/// Tests for <c>[AbsenceTolerant]</c>, which makes a generated Wait*/Assert* pair resolve
/// the element optionally.
/// </summary>
/// <remarks>
/// Without it, <c>WaitVisible(false)</c> and <c>AssertExists(false)</c> resolve the element
/// before comparing and raise <c>ElementNotFoundException</c> for exactly the state they
/// are being asked about. Value comparisons must keep the strict helpers: a missing element
/// is a genuine failure for a text check.
/// </remarks>
public class AbsenceToleranceTests
{
    private readonly IsWaitAssertGenerator _generator = new();
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

    private const string ToleratedClass = """
        public class Control<TScope>
        {
            [AbsenceTolerant]
            protected virtual bool? IsVisibleCore(IMauiElement? element) => element?.Visible;
        }
        """;

    private const string StrictClass = """
        public class Control<TScope>
        {
            protected virtual bool? IsEnabledCore(IMauiElement? element) => element?.Enabled;
        }
        """;

    #region Extraction

    [Fact]
    public void Extract_WithAttribute_SetsTheFlag()
    {
        var info = _generator.Extract(FirstMethod(ToleratedClass));

        Assert.True(info.IsAbsenceTolerant);
    }

    [Fact]
    public void Extract_WithoutAttribute_LeavesTheFlagFalse()
    {
        var info = _generator.Extract(FirstMethod(StrictClass));

        Assert.False(info.IsAbsenceTolerant);
    }

    /// <summary>
    /// The attribute is matched syntactically, so the "Attribute" suffix is equivalent.
    /// </summary>
    [Fact]
    public void Extract_WithAttributeSuffix_SetsTheFlag()
    {
        var code = """
            public class Control<TScope>
            {
                [AbsenceTolerantAttribute]
                protected virtual bool? IsVisibleCore(IMauiElement? element) => element?.Visible;
            }
            """;

        Assert.True(_generator.Extract(FirstMethod(code)).IsAbsenceTolerant);
    }

    /// <summary>A qualified attribute name resolves the same way.</summary>
    [Fact]
    public void Extract_WithQualifiedName_SetsTheFlag()
    {
        var code = """
            public class Control<TScope>
            {
                [Brinell.Core.Interfaces.AbsenceTolerant]
                protected virtual bool? IsVisibleCore(IMauiElement? element) => element?.Visible;
            }
            """;

        Assert.True(_generator.Extract(FirstMethod(code)).IsAbsenceTolerant);
    }

    #endregion

    #region Emitted helpers

    [Fact]
    public void Generate_WhenTolerant_EmitsTheOptionalHelpers()
    {
        var info = _generator.Extract(FirstMethod(ToleratedClass));

        var output = _generator.Generate(info, ContextFor(ToleratedClass));

        Assert.Contains("RunWaitWithOptionalElement", output);
        Assert.Contains("RunAssertWithOptionalElement", output);
    }

    [Fact]
    public void Generate_WhenNotTolerant_EmitsTheStrictHelpers()
    {
        var info = _generator.Extract(FirstMethod(StrictClass));

        var output = _generator.Generate(info, ContextFor(StrictClass));

        Assert.Contains("RunWaitWithElement", output);
        Assert.Contains("RunAssertWithElement", output);
        Assert.DoesNotContain("RunWaitWithOptionalElement", output);
        Assert.DoesNotContain("RunAssertWithOptionalElement", output);
    }

    /// <summary>
    /// The Is* member is unaffected — it already used the null-tolerant lookup, which is
    /// the asymmetry that made this defect visible in the first place.
    /// </summary>
    [Fact]
    public void Generate_WhenTolerant_LeavesTheIsMemberUnchanged()
    {
        var info = _generator.Extract(FirstMethod(ToleratedClass));

        var output = _generator.Generate(info, ContextFor(ToleratedClass));

        Assert.Contains("IsVisibleCore(TryFindElement())", output);
    }

    /// <summary>Tolerance composes with the resolved fluent return type.</summary>
    [Fact]
    public void Generate_WhenTolerantOnAContainer_ReturnsTSelf()
    {
        var code = """
            public class Container<TParent, TSelf>
            {
                [AbsenceTolerant]
                protected virtual bool? IsVisibleCore(IMauiElement? element) => element?.Visible;
            }
            """;

        var info = _generator.Extract(FirstMethod(code));

        var output = _generator.Generate(info, ContextFor(code));

        Assert.Contains("public TSelf AssertVisible", output);
        Assert.Contains("RunAssertWithOptionalElement", output);
    }

    #endregion
}
