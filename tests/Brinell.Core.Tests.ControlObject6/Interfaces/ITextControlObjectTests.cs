using Brinell.Core.ControlObject6.Interfaces;
using FluentAssertions;

namespace Brinell.Core.Tests.ControlObject6.Interfaces;

/// <summary>
/// Tests for ITextControlObject interface contract (ITC-001 to ITC-007).
/// </summary>
public class ITextControlObjectTests
{
    private readonly Type _interfaceType = typeof(ITextControlObject);

    [Fact]
    public void ITC001_Interface_ExtendsIFocusableControlObject()
    {
        // Assert
        _interfaceType.GetInterfaces().Should().Contain(typeof(IFocusableControlObject));
    }

    [Fact]
    public void ITC002_Interface_DefinesEnterMethod()
    {
        // Arrange & Act
        var method = _interfaceType.GetMethod("Enter");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(void));
        method.GetParameters().Should().HaveCountGreaterOrEqualTo(1);
        method.GetParameters()[0].ParameterType.Should().Be<string>();
    }

    [Fact]
    public void ITC003_Interface_DefinesClearMethod()
    {
        // Arrange & Act
        var method = _interfaceType.GetMethod("Clear");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(void));
    }

    [Fact]
    public void ITC004_Interface_DefinesClearAndEnterMethod()
    {
        // Arrange & Act
        var method = _interfaceType.GetMethod("ClearAndEnter");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(void));
    }

    [Fact]
    public void ITC005_Interface_DefinesAppendMethod()
    {
        // Arrange & Act
        var method = _interfaceType.GetMethod("Append");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(void));
    }

    [Fact]
    public void ITC006_Interface_DefinesIsReadOnlyMethod()
    {
        // Arrange & Act
        var method = _interfaceType.GetMethod("IsReadOnly");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be<bool>();
    }

    [Fact]
    public void ITC007_Interface_DefinesGetTextLengthMethod()
    {
        // Arrange & Act
        var method = _interfaceType.GetMethod("GetTextLength");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be<int>();
    }
}
