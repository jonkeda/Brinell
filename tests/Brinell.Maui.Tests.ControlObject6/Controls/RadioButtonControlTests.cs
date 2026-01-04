using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;
using Brinell.Maui.Tests.ControlObject6.Mocks;

namespace Brinell.Maui.Tests.ControlObject6.Controls;

/// <summary>
/// Unit tests for TestableRadioButtonControl.
/// Tests cover toggle operations, check state, and radio button-specific behavior.
/// Test IDs: RB-001 to RB-012
/// </summary>
public class RadioButtonControlTests
{
    #region Basic State Tests (RB-001 to RB-003)

    [Fact]
    public void RB001_IsChecked_WhenUnchecked_ReturnsFalse()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableRadioButtonControl(context, "radioOption1");

        // Act
        var result = control.IsChecked();

        // Assert
        result.Should().BeFalse(); // Default unchecked state
    }

    [Fact]
    public void RB002_IsChecked_WhenChecked_ReturnsTrue()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableRadioButtonControl(context, "radioOption1");
        control.Check(); // Set to checked state

        // Act
        var result = control.IsChecked();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void RB003_IsChecked_WhenElementNotFound_ReturnsFalse()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        MockAppiumFactory.SetupElementNotFound(mockDriver);
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableRadioButtonControl(context, "missingRadio");

        // Act
        var result = control.IsChecked();

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Check/Toggle Operations (RB-004 to RB-007)

    [Fact]
    public void RB004_Check_WhenUnchecked_BecomesChecked()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableRadioButtonControl(context, "radioOption1");
        control.IsChecked().Should().BeFalse();

        // Act
        control.Check();

        // Assert
        control.IsChecked().Should().BeTrue();
    }

    [Fact]
    public void RB005_Check_WhenAlreadyChecked_StaysChecked()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableRadioButtonControl(context, "radioOption1");
        control.Check();
        control.IsChecked().Should().BeTrue();

        // Act
        control.Check(); // Check again

        // Assert
        control.IsChecked().Should().BeTrue();
    }

    [Fact]
    public void RB006_Select_AliasForCheck_SetsChecked()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableRadioButtonControl(context, "radioOption1");

        // Act
        control.Select();

        // Assert
        control.IsChecked().Should().BeTrue();
    }

    [Fact]
    public void RB007_Uncheck_DoesNothing_RadioButtonsCannotBeUnchecked()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableRadioButtonControl(context, "radioOption1");
        control.Check();
        control.IsChecked().Should().BeTrue();

        // Act - Uncheck should be no-op for radio buttons
        control.Uncheck();

        // Assert - Should still be checked
        control.IsChecked().Should().BeTrue();
    }

    #endregion

    #region Toggle Operation (RB-008)

    [Fact]
    public void RB008_Toggle_WhenUnchecked_BecomesChecked()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableRadioButtonControl(context, "radioOption1");

        // Act
        control.Toggle();

        // Assert
        control.IsChecked().Should().BeTrue();
    }

    #endregion

    #region Assertion Tests (RB-009 to RB-011)

    [Fact]
    public void RB009_AssertChecked_WhenMatchesExpected_Passes()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableRadioButtonControl(context, "radioOption1");
        control.Check();

        // Act & Assert - Should not throw
        control.AssertChecked(true);
    }

    [Fact]
    public void RB010_AssertChecked_WhenMismatch_ThrowsAssertionException()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableRadioButtonControl(context, "radioOption1");

        // Act & Assert
        var action = () => control.AssertChecked(true);
        action.Should().Throw<AssertionException>()
            .WithMessage("*Expected control to be checked*");
    }

    [Fact]
    public void RB011_AssertChecked_WithNullExpected_Passes()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableRadioButtonControl(context, "radioOption1");

        // Act & Assert - Should not throw when expected is null
        control.AssertChecked(null);
    }

    #endregion

    #region SetChecked Tests (RB-012)

    [Fact]
    public void RB012_SetChecked_WhenTrue_SetsToChecked()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement();
        MockAppiumFactory.SetupFindElement(mockDriver, mockElement);
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableRadioButtonControl(context, "radioOption1");

        // Act
        control.SetChecked(true);

        // Assert
        control.IsChecked().Should().BeTrue();
    }

    #endregion
}
