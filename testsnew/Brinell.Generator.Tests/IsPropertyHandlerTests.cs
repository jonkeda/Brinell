using Brinell.Generator.Analysis;
using Brinell.Generator.Generation;
using Brinell.Generator.Handlers;

namespace Brinell.Generator.Tests;

/// <summary>
/// Tests for IsPropertyHandler and PropertyMethodAnalyzer/PropertyWrapperGenerator.
/// Validates that Is/Wait/Assert patterns are correctly generated from ControlBase patterns.
/// </summary>
public class IsPropertyHandlerTests
{
    private readonly IsPropertyHandler _handler = new();

    #region Matching Tests

    [Fact]
    public void Matches_WithIsVisibleCore_ReturnsTrue()
    {
        // Arrange
        var code = @"
public class Test {
    protected virtual bool? IsVisibleCore(IMauiElement? element)
    {
        return element?.Visible;
    }
}";
        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
        var root = (Microsoft.CodeAnalysis.CSharp.Syntax.CompilationUnitSyntax)tree.GetRoot();
        var method = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>().First();

        // Act
        var matches = _handler.Matches(method);

        // Assert
        Assert.True(matches);
    }

    [Fact]
    public void Matches_WithIsEnabledCore_ReturnsTrue()
    {
        // Arrange
        var code = @"
public class Test {
    protected virtual bool? IsEnabledCore(IMauiElement? element)
    {
        return element?.Enabled;
    }
}";
        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
        var root = (Microsoft.CodeAnalysis.CSharp.Syntax.CompilationUnitSyntax)tree.GetRoot();
        var method = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>().First();

        // Act
        var matches = _handler.Matches(method);

        // Assert
        Assert.True(matches);
    }

    [Fact]
    public void Matches_WithIsExistsCore_ReturnsTrue()
    {
        // Arrange
        var code = @"
public class Test {
    protected virtual bool? IsExistsCore(IMauiElement? element)
    {
        return element != null;
    }
}";
        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
        var root = (Microsoft.CodeAnalysis.CSharp.Syntax.CompilationUnitSyntax)tree.GetRoot();
        var method = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>().First();

        // Act
        var matches = _handler.Matches(method);

        // Assert
        Assert.True(matches);
    }

    [Fact]
    public void Matches_WithSendKeysCore_ReturnsFalse()
    {
        // Arrange - SendKeysCore doesn't match: wrong prefix (no "Is"), wrong return type (void)
        var code = @"
public class Test {
    protected virtual void SendKeysCore(IMauiElement element, string keys)
    {
        element.SendKeys(keys);
    }
}";
        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
        var root = (Microsoft.CodeAnalysis.CSharp.Syntax.CompilationUnitSyntax)tree.GetRoot();
        var method = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>().First();

        // Act
        var matches = _handler.Matches(method);

        // Assert
        Assert.False(matches);
    }

    [Fact]
    public void Matches_WithWrongPrefix_ReturnsFalse()
    {
        // Arrange - GetVisibleCore doesn't match: wrong prefix
        var code = @"
public class Test {
    protected virtual bool? GetVisibleCore(IMauiElement? element)
    {
        return element?.Visible;
    }
}";
        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
        var root = (Microsoft.CodeAnalysis.CSharp.Syntax.CompilationUnitSyntax)tree.GetRoot();
        var method = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>().First();

        // Act
        var matches = _handler.Matches(method);

        // Assert
        Assert.False(matches);
    }

    [Fact]
    public void Matches_WithoutVirtualModifier_ReturnsFalse()
    {
        // Arrange - missing virtual modifier
        var code = @"
public class Test {
    protected bool? IsVisibleCore(IMauiElement? element)
    {
        return element?.Visible;
    }
}";
        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
        var root = (Microsoft.CodeAnalysis.CSharp.Syntax.CompilationUnitSyntax)tree.GetRoot();
        var method = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>().First();

        // Act
        var matches = _handler.Matches(method);

        // Assert
        Assert.False(matches);
    }

    #endregion

    #region Extraction Tests

    [Fact]
    public void Extract_WithIsVisibleCore_ExtractsPropertyNameVisible()
    {
        // Arrange
        var code = @"
public class Test {
    protected virtual bool? IsVisibleCore(IMauiElement? element)
    {
        return element?.Visible;
    }
}";
        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
        var root = (Microsoft.CodeAnalysis.CSharp.Syntax.CompilationUnitSyntax)tree.GetRoot();
        var method = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>().First();

        // Act
        var info = _handler.Extract(method);

        // Assert
        Assert.Equal("IsVisibleCore", info.MethodName);
        Assert.Equal("Visible", info.PublicMethodName);
        Assert.Equal("bool?", info.ReturnType);
    }

    [Fact]
    public void Extract_WithIsEnabledCore_ExtractsPropertyNameEnabled()
    {
        // Arrange
        var code = @"
public class Test {
    protected virtual bool? IsEnabledCore(IMauiElement? element)
    {
        return element?.Enabled;
    }
}";
        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
        var root = (Microsoft.CodeAnalysis.CSharp.Syntax.CompilationUnitSyntax)tree.GetRoot();
        var method = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>().First();

        // Act
        var info = _handler.Extract(method);

        // Assert
        Assert.Equal("IsEnabledCore", info.MethodName);
        Assert.Equal("Enabled", info.PublicMethodName);
    }

    [Fact]
    public void Extract_WithIsExistsCore_ExtractsPropertyNameExists()
    {
        // Arrange
        var code = @"
public class Test {
    protected virtual bool? IsExistsCore(IMauiElement? element)
    {
        return element != null;
    }
}";
        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
        var root = (Microsoft.CodeAnalysis.CSharp.Syntax.CompilationUnitSyntax)tree.GetRoot();
        var method = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>().First();

        // Act
        var info = _handler.Extract(method);

        // Assert
        Assert.Equal("IsExistsCore", info.MethodName);
        Assert.Equal("Exists", info.PublicMethodName);
    }

    #endregion

    #region Generation Tests

    [Fact]
    public void GenerateWrapper_WithIsVisibleCore_GeneratesThreeMethods()
    {
        // Arrange
        var code = @"
public class Test {
    protected virtual bool? IsVisibleCore(IMauiElement? element)
    {
        return element?.Visible;
    }
}";
        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
        var root = (Microsoft.CodeAnalysis.CSharp.Syntax.CompilationUnitSyntax)tree.GetRoot();
        var method = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>().First();

        var methodInfo = _handler.Extract(method);

        // Act
        var generated = _handler.GenerateWrapper(methodInfo, "ControlBase<TScope>", "<TScope>");

        // Assert - should contain all three methods
        Assert.Contains("public bool IsVisible()", generated);
        Assert.Contains("public bool WaitVisible(bool? expected, int? timeoutMs = null)", generated);
        Assert.Contains("public TScope AssertVisible(bool? expected, string? message = null, int? timeoutMs = null)", generated);
    }

    [Fact]
    public void GenerateWrapper_IsMethod_CallsTryFindElement()
    {
        // Arrange
        var code = @"
public class Test {
    protected virtual bool? IsVisibleCore(IMauiElement? element)
    {
        return element?.Visible;
    }
}";
        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
        var root = (Microsoft.CodeAnalysis.CSharp.Syntax.CompilationUnitSyntax)tree.GetRoot();
        var method = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>().First();

        var methodInfo = _handler.Extract(method);

        // Act
        var generated = _handler.GenerateWrapper(methodInfo, "ControlBase<TScope>", "<TScope>");

        // Assert
        Assert.Contains("return IsVisibleCore(TryFindElement()) == true;", generated);
    }

    [Fact]
    public void GenerateWrapper_WaitMethod_IncludesNullCheck()
    {
        // Arrange
        var code = @"
public class Test {
    protected virtual bool? IsVisibleCore(IMauiElement? element)
    {
        return element?.Visible;
    }
}";
        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
        var root = (Microsoft.CodeAnalysis.CSharp.Syntax.CompilationUnitSyntax)tree.GetRoot();
        var method = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>().First();

        var methodInfo = _handler.Extract(method);

        // Act
        var generated = _handler.GenerateWrapper(methodInfo, "ControlBase<TScope>", "<TScope>");

        // Assert
        Assert.Contains("if (expected == null) return true;", generated);
    }

    [Fact]
    public void GenerateWrapper_AssertMethod_UsesRunAssertWithElement()
    {
        // Arrange
        var code = @"
public class Test {
    protected virtual bool? IsVisibleCore(IMauiElement? element)
    {
        return element?.Visible;
    }
}";
        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
        var root = (Microsoft.CodeAnalysis.CSharp.Syntax.CompilationUnitSyntax)tree.GetRoot();
        var method = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>().First();

        var methodInfo = _handler.Extract(method);

        // Act
        var generated = _handler.GenerateWrapper(methodInfo, "ControlBase<TScope>", "<TScope>");

        // Assert
        Assert.Contains("RunAssertWithElement(expected,", generated);
        Assert.Contains("IsVisibleCore, (actual, expected1) => (actual == expected1),", generated);
    }

    #endregion

    #region PropertyMethodAnalyzer Tests

    [Fact]
    public void PropertyMethodAnalyzer_WithControlBase_MatchesThreePatterns()
    {
        // Arrange
        var code = @"
public abstract class ControlBase<TScope>
{
    protected virtual bool? IsVisibleCore(IMauiElement? element)
    {
        return element?.Visible;
    }

    protected virtual bool? IsEnabledCore(IMauiElement? element)
    {
        return element?.Enabled;
    }

    protected virtual bool? IsExistsCore(IMauiElement? element)
    {
        return element != null;
    }
}";
        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
        var root = (Microsoft.CodeAnalysis.CSharp.Syntax.CompilationUnitSyntax)tree.GetRoot();
        var classDecl = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>().First();

        var analyzer = new PropertyMethodAnalyzer();

        // Act
        var groups = analyzer.Analyze(classDecl);

        // Assert
        Assert.Equal(3, groups.Count);
        Assert.Contains(groups, g => g.CoreMethod.PublicMethodName == "Visible");
        Assert.Contains(groups, g => g.CoreMethod.PublicMethodName == "Enabled");
        Assert.Contains(groups, g => g.CoreMethod.PublicMethodName == "Exists");
    }

    #endregion

    #region PropertyWrapperGenerator Tests

    [Fact]
    public void PropertyWrapperGenerator_GenerateAllMethods_GeneratesNineMethods()
    {
        // Arrange
        var code = @"
public abstract class ControlBase<TScope>
{
    protected virtual bool? IsVisibleCore(IMauiElement? element) => element?.Visible;
    protected virtual bool? IsEnabledCore(IMauiElement? element) => element?.Enabled;
    protected virtual bool? IsExistsCore(IMauiElement? element) => element != null;
}";
        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
        var root = (Microsoft.CodeAnalysis.CSharp.Syntax.CompilationUnitSyntax)tree.GetRoot();
        var classDecl = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>().First();

        var analyzer = new PropertyMethodAnalyzer();
        var groups = analyzer.Analyze(classDecl);

        var generator = new PropertyWrapperGenerator();

        // Act
        var methods = generator.GenerateAllMethods(groups, "ControlBase<TScope>", "<TScope>");

        // Assert
        var allGenerated = string.Concat(methods);
        
        // Check for Is methods
        Assert.Contains("public bool IsVisible()", allGenerated);
        Assert.Contains("public bool IsEnabled()", allGenerated);
        Assert.Contains("public bool IsExists()", allGenerated);

        // Check for Wait methods
        Assert.Contains("public bool WaitVisible(", allGenerated);
        Assert.Contains("public bool WaitEnabled(", allGenerated);
        Assert.Contains("public bool WaitExists(", allGenerated);

        // Check for Assert methods
        Assert.Contains("public TScope AssertVisible(", allGenerated);
        Assert.Contains("public TScope AssertEnabled(", allGenerated);
        Assert.Contains("public TScope AssertExists(", allGenerated);
    }

    #endregion

    #region Test Data File Tests

    [Fact]
    public void TestData_ControlBaseInput_CanBeParsed()
    {
        // Arrange
        var inputFile = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "Input", "ControlBase.input.cs");

        // Act
        var content = File.ReadAllText(inputFile);
        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(content);
        var root = (Microsoft.CodeAnalysis.CSharp.Syntax.CompilationUnitSyntax)tree.GetRoot();

        // Assert
        Assert.NotNull(root);
        var classDecl = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>().FirstOrDefault();
        Assert.NotNull(classDecl);
    }

    [Fact]
    public void TestData_ControlBaseInput_ContainsSendKeysAndIsCoreMethods()
    {
        // Arrange
        var inputFile = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "Input", "ControlBase.input.cs");
        var content = File.ReadAllText(inputFile);
        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(content);
        var root = (Microsoft.CodeAnalysis.CSharp.Syntax.CompilationUnitSyntax)tree.GetRoot();
        var classDecl = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>().First();

        // Act
        var methods = classDecl.Members.OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>().ToList();

        // Assert - Should have SendKeysCore, IsVisibleCore, IsEnabledCore, IsExistsCore
        Assert.Contains(methods, m => m.Identifier.Text == "SendKeysCore");
        Assert.Contains(methods, m => m.Identifier.Text == "IsVisibleCore");
        Assert.Contains(methods, m => m.Identifier.Text == "IsEnabledCore");
        Assert.Contains(methods, m => m.Identifier.Text == "IsExistsCore");
    }

    [Fact]
    public void TestData_ControlBaseExpected_ContainsBothGeneratedMethods()
    {
        // Arrange
        var expectedFile = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "Expected", "ControlBase.expected.cs");
        var content = File.ReadAllText(expectedFile);

        // Act & Assert - Check for CoreMethodHandler generated SendKeys
        Assert.Contains("public TScope SendKeys(string keys, int? timeoutMs = null)", content);

        // Check for IsPropertyHandler generated methods
        Assert.Contains("public bool IsVisible()", content);
        Assert.Contains("public bool IsEnabled()", content);
        Assert.Contains("public bool IsExists()", content);

        Assert.Contains("public bool WaitVisible(bool? expected, int? timeoutMs = null)", content);
        Assert.Contains("public bool WaitEnabled(bool? expected, int? timeoutMs = null)", content);
        Assert.Contains("public bool WaitExists(bool? expected, int? timeoutMs = null)", content);

        Assert.Contains("public TScope AssertVisible(bool? expected, string? message = null, int? timeoutMs = null)", content);
        Assert.Contains("public TScope AssertEnabled(bool? expected, string? message = null, int? timeoutMs = null)", content);
        Assert.Contains("public TScope AssertExists(bool? expected, string? message = null, int? timeoutMs = null)", content);
    }

    #endregion
}
