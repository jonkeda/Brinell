using Brinell.Core.ControlObject6.Interfaces;
using FluentAssertions;

namespace Brinell.Core.Tests.ControlObject6.Interfaces;

/// <summary>
/// Tests for IInteractiveControlObject interface contract (IIC-001 to IIC-005).
/// </summary>
public class IInteractiveControlObjectTests
{
    private readonly Type _interfaceType = typeof(IInteractiveControlObject);

    [Fact]
    public void IIC001_Interface_ExtendsIControlObject()
    {
        // Assert
        _interfaceType.GetInterfaces().Should().Contain(typeof(IControlObject));
    }

    [Fact]
    public void IIC002_Interface_DefinesIsEnabledMethod()
    {
        // Arrange & Act
        var method = _interfaceType.GetMethod("IsEnabled");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be<bool>();
    }

    [Fact]
    public void IIC003_Interface_DefinesWaitEnabledMethod()
    {
        // Arrange & Act
        var method = _interfaceType.GetMethod("WaitEnabled");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be<bool>();
        method.GetParameters().Should().HaveCount(2);
        method.GetParameters()[0].ParameterType.Should().Be<bool?>();
        method.GetParameters()[1].ParameterType.Should().Be<int?>();
    }

    [Fact]
    public void IIC004_Interface_DefinesCheckEnabledMethod()
    {
        // Arrange & Act
        var method = _interfaceType.GetMethod("CheckEnabled");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(void));
    }

    [Fact]
    public void IIC005_Interface_DefinesAssertEnabledMethod()
    {
        // Arrange & Act
        var method = _interfaceType.GetMethod("AssertEnabled");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(void));
    }
}
