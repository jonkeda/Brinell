using Brinell.Core.ControlObject6.Interfaces;
using FluentAssertions;

namespace Brinell.Core.Tests.ControlObject6.Interfaces;

/// <summary>
/// Tests for IClickableControlObject interface contract (ICC-001 to ICC-006).
/// </summary>
public class IClickableControlObjectTests
{
    private readonly Type _interfaceType = typeof(IClickableControlObject);

    [Fact]
    public void ICC001_Interface_ExtendsIInteractiveControlObject()
    {
        // Assert
        _interfaceType.GetInterfaces().Should().Contain(typeof(IInteractiveControlObject));
    }

    [Fact]
    public void ICC002_Interface_DefinesClickMethod()
    {
        // Arrange & Act
        var method = _interfaceType.GetMethod("Click");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(void));
    }

    [Fact]
    public void ICC003_Interface_DefinesDoubleClickMethod()
    {
        // Arrange & Act
        var method = _interfaceType.GetMethod("DoubleClick");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(void));
    }

    [Fact]
    public void ICC004_Interface_DefinesRightClickMethod()
    {
        // Arrange & Act
        var method = _interfaceType.GetMethod("RightClick");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(void));
    }

    [Fact]
    public void ICC005_Interface_DefinesHoverMethod()
    {
        // Arrange & Act
        var method = _interfaceType.GetMethod("Hover");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(void));
    }

    [Fact]
    public void ICC006_Interface_DefinesLongPressMethod()
    {
        // Arrange & Act
        var method = _interfaceType.GetMethod("LongPress");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(void));
    }
}
