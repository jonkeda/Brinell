using Brinell.Core.ControlObject6.Interfaces;
using FluentAssertions;

namespace Brinell.Core.Tests.ControlObject6.Interfaces;

/// <summary>
/// Tests for IFocusableControlObject interface contract (IFC-001 to IFC-004).
/// </summary>
public class IFocusableControlObjectTests
{
    private readonly Type _interfaceType = typeof(IFocusableControlObject);

    [Fact]
    public void IFC001_Interface_ExtendsIInteractiveControlObject()
    {
        // Assert
        _interfaceType.GetInterfaces().Should().Contain(typeof(IInteractiveControlObject));
    }

    [Fact]
    public void IFC002_Interface_DefinesIsFocusedMethod()
    {
        // Arrange & Act
        var method = _interfaceType.GetMethod("IsFocused");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be<bool>();
    }

    [Fact]
    public void IFC003_Interface_DefinesFocusMethod()
    {
        // Arrange & Act
        var method = _interfaceType.GetMethod("Focus");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(void));
    }

    [Fact]
    public void IFC004_Interface_DefinesBlurMethod()
    {
        // Arrange & Act
        var method = _interfaceType.GetMethod("Blur");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(void));
    }
}
