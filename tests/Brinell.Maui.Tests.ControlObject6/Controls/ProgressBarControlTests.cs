using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;
using Brinell.Maui.Tests.ControlObject6.Mocks;

namespace Brinell.Maui.Tests.ControlObject6.Controls;

/// <summary>
/// Unit tests for TestableProgressBarControl.
/// Tests cover progress value, percentage, completion state, and assertions.
/// Test IDs: PB-001 to PB-010
/// </summary>
public class ProgressBarControlTests
{
    #region Progress Value (PB-001 to PB-003)

    [Fact]
    public void PB001_GetProgress_ReturnsCurrentProgress()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableProgressBarControl(context, "downloadProgress");

        // Act
        var progress = control.GetProgress();

        // Assert
        progress.Should().Be(0); // Default value
    }

    [Fact]
    public void PB002_GetProgress_AfterSettingValue_ReturnsSetValue()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableProgressBarControl(context, "downloadProgress");
        control.SetProgress(0.75);

        // Act
        var progress = control.GetProgress();

        // Assert
        progress.Should().Be(0.75);
    }

    [Fact]
    public void PB003_WaitProgress_WhenMatches_ReturnsTrue()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableProgressBarControl(context, "uploadProgress");
        control.SetProgress(0.5);

        // Act
        var result = control.WaitProgress(0.5);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Progress Range (PB-004 to PB-005)

    [Fact]
    public void PB004_GetMinMax_ReturnsMinAndMax()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableProgressBarControl(context, "progress");
        control.SetMinMax(0, 1);

        // Act
        var (min, max) = control.GetMinMax();

        // Assert
        min.Should().Be(0);
        max.Should().Be(1);
    }

    [Fact]
    public void PB005_GetProgressPercent_ReturnsCorrectPercentage()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableProgressBarControl(context, "progress");
        control.SetMinMax(0, 1);
        control.SetProgress(0.5);

        // Act
        var percent = control.GetProgressPercent();

        // Assert
        percent.Should().Be(50); // 50%
    }

    #endregion

    #region Completion State (PB-006 to PB-008)

    [Fact]
    public void PB006_IsComplete_WhenNotComplete_ReturnsFalse()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableProgressBarControl(context, "progress");
        control.SetMinMax(0, 1);
        control.SetProgress(0.5);

        // Act
        var isComplete = control.IsComplete();

        // Assert
        isComplete.Should().BeFalse();
    }

    [Fact]
    public void PB007_IsComplete_WhenComplete_ReturnsTrue()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableProgressBarControl(context, "progress");
        control.SetMinMax(0, 1);
        control.SetProgress(1);

        // Act
        var isComplete = control.IsComplete();

        // Assert
        isComplete.Should().BeTrue();
    }

    [Fact]
    public void PB008_WaitComplete_WhenComplete_ReturnsTrue()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableProgressBarControl(context, "progress");
        control.SetMinMax(0, 1);
        control.SetProgress(1);

        // Act
        var result = control.WaitComplete();

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Assertions (PB-009 to PB-010)

    [Fact]
    public void PB009_AssertProgress_WhenMatches_Passes()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableProgressBarControl(context, "progress");
        control.SetProgress(0.75);

        // Act & Assert - Should not throw
        control.AssertProgress(0.75);
    }

    [Fact]
    public void PB010_AssertComplete_WhenNotComplete_ThrowsAssertionException()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableProgressBarControl(context, "progress");
        control.SetMinMax(0, 1);
        control.SetProgress(0.5);

        // Act & Assert
        var action = () => control.AssertComplete();
        action.Should().Throw<AssertionException>()
            .WithMessage("*complete*");
    }

    #endregion
}
