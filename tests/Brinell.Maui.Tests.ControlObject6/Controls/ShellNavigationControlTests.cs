using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;
using Brinell.Maui.Tests.ControlObject6.Mocks;
using FluentAssertions;
using Moq;
using Xunit;

namespace Brinell.Maui.Tests.ControlObject6.Controls;

/// <summary>
/// Unit tests for ShellControl and FlyoutItemControl.
/// Test IDs: SH-001 to SH-012, FI-001 to FI-010
/// </summary>
public class ShellNavigationControlTests
{
    #region ShellControl Tests

    [Fact(DisplayName = "SH-001: ShellControl constructor with AutomationId sets Locator")]
    public void ShellControl_Constructor_WithAutomationId_SetsLocator()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var automationId = "TestShell";

        // Act
        var control = new TestableShellControl(context, automationId);

        // Assert
        control.Locator.Should().NotBeNull();
        control.Locator.Value.Should().Be(automationId);
        control.Locator.Strategy.Should().Be(LocatorStrategy.AutomationId);
    }

    [Fact(DisplayName = "SH-002: ShellControl constructor with Locator sets Locator")]
    public void ShellControl_Constructor_WithLocator_SetsLocator()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var locator = By.AutomationId("TestShell");

        // Act
        var control = new TestableShellControl(context, locator);

        // Assert
        control.Locator.Should().BeSameAs(locator);
    }

    [Fact(DisplayName = "SH-003: IsFlyoutOpen returns false initially")]
    public void ShellControl_IsFlyoutOpen_Initially_ReturnsFalse()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("shell");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableShellControl(context, "TestShell");

        // Act
        var isOpen = control.IsFlyoutOpen();

        // Assert
        isOpen.Should().BeFalse();
    }

    [Fact(DisplayName = "SH-004: OpenFlyout opens the flyout")]
    public void ShellControl_OpenFlyout_OpensFlyout()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("shell");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableShellControl(context, "TestShell");

        // Act
        control.OpenFlyout();

        // Assert
        control.IsFlyoutOpen().Should().BeTrue();
    }

    [Fact(DisplayName = "SH-005: CloseFlyout closes the flyout")]
    public void ShellControl_CloseFlyout_ClosesFlyout()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("shell");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableShellControl(context, "TestShell");
        control.OpenFlyout();

        // Act
        control.CloseFlyout();

        // Assert
        control.IsFlyoutOpen().Should().BeFalse();
    }

    [Fact(DisplayName = "SH-006: NavigateToRoute navigates without error")]
    public void ShellControl_NavigateToRoute_NavigatesWithoutError()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("shell");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableShellControl(context, "TestShell");

        // Act & Assert
        var action = () => control.NavigateToRoute("//main/settings");
        action.Should().NotThrow();
    }

    [Fact(DisplayName = "SH-007: GetFlyoutItem returns flyout item control")]
    public void ShellControl_GetFlyoutItem_ReturnsFlyoutItemControl()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("shell");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableShellControl(context, "TestShell");

        // Act
        var flyoutItem = control.GetFlyoutItem("Settings");

        // Assert
        flyoutItem.Should().NotBeNull();
        flyoutItem.Locator.Value.Should().Be("Settings");
    }

    [Fact(DisplayName = "SH-008: GetTabBar returns tab bar control")]
    public void ShellControl_GetTabBar_ReturnsTabBarControl()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableShellControl(context, "TestShell");

        // Act
        var tabBar = control.GetTabBar();

        // Assert
        tabBar.Should().NotBeNull();
        tabBar.Locator.Value.Should().Be("ShellTabBar");
    }

    [Fact(DisplayName = "SH-009: AssertFlyoutOpen passes when open")]
    public void ShellControl_AssertFlyoutOpen_WhenOpen_Passes()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("shell");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableShellControl(context, "TestShell");
        control.OpenFlyout();

        // Act & Assert
        var action = () => control.AssertFlyoutOpen();
        action.Should().NotThrow();
    }

    [Fact(DisplayName = "SH-010: AssertFlyoutOpen throws when closed")]
    public void ShellControl_AssertFlyoutOpen_WhenClosed_Throws()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("shell");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableShellControl(context, "TestShell");

        // Act & Assert
        var action = () => control.AssertFlyoutOpen();
        action.Should().Throw<AssertionException>();
    }

    [Fact(DisplayName = "SH-011: AssertFlyoutClosed passes when closed")]
    public void ShellControl_AssertFlyoutClosed_WhenClosed_Passes()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("shell");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableShellControl(context, "TestShell");

        // Act & Assert
        var action = () => control.AssertFlyoutClosed();
        action.Should().NotThrow();
    }

    [Fact(DisplayName = "SH-012: AssertFlyoutClosed throws when open")]
    public void ShellControl_AssertFlyoutClosed_WhenOpen_Throws()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("shell");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableShellControl(context, "TestShell");
        control.OpenFlyout();

        // Act & Assert
        var action = () => control.AssertFlyoutClosed();
        action.Should().Throw<AssertionException>();
    }

    #endregion

    #region FlyoutItemControl Tests

    [Fact(DisplayName = "FI-001: FlyoutItemControl constructor with AutomationId sets Locator")]
    public void FlyoutItemControl_Constructor_WithAutomationId_SetsLocator()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var automationId = "SettingsItem";

        // Act
        var control = new TestableFlyoutItemControl(context, automationId);

        // Assert
        control.Locator.Should().NotBeNull();
        control.Locator.Value.Should().Be(automationId);
    }

    [Fact(DisplayName = "FI-002: IsSelected returns false initially")]
    public void FlyoutItemControl_IsSelected_Initially_ReturnsFalse()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("flyoutitem");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableFlyoutItemControl(context, "SettingsItem");

        // Act
        var isSelected = control.IsSelected();

        // Assert
        isSelected.Should().BeFalse();
    }

    [Fact(DisplayName = "FI-003: Select selects the flyout item")]
    public void FlyoutItemControl_Select_SelectsItem()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("flyoutitem");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableFlyoutItemControl(context, "SettingsItem");

        // Act
        control.Select();

        // Assert
        control.IsSelected().Should().BeTrue();
    }

    [Fact(DisplayName = "FI-004: GetIcon returns icon")]
    public void FlyoutItemControl_GetIcon_ReturnsIcon()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("flyoutitem");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableFlyoutItemControl(context, "SettingsItem");
        control.SetIcon("settings_icon.png");

        // Act
        var icon = control.GetIcon();

        // Assert
        icon.Should().Be("settings_icon.png");
    }

    [Fact(DisplayName = "FI-005: AssertSelected passes when selected")]
    public void FlyoutItemControl_AssertSelected_WhenSelected_Passes()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("flyoutitem");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableFlyoutItemControl(context, "SettingsItem");
        control.SetSelected(true);

        // Act & Assert
        var action = () => control.AssertSelected();
        action.Should().NotThrow();
    }

    [Fact(DisplayName = "FI-006: AssertSelected throws when not selected")]
    public void FlyoutItemControl_AssertSelected_WhenNotSelected_Throws()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("flyoutitem");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableFlyoutItemControl(context, "SettingsItem");

        // Act & Assert
        var action = () => control.AssertSelected();
        action.Should().Throw<AssertionException>();
    }

    [Fact(DisplayName = "FI-007: AssertNotSelected passes when not selected")]
    public void FlyoutItemControl_AssertNotSelected_WhenNotSelected_Passes()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("flyoutitem");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableFlyoutItemControl(context, "SettingsItem");

        // Act & Assert
        var action = () => control.AssertNotSelected();
        action.Should().NotThrow();
    }

    [Fact(DisplayName = "FI-008: AssertNotSelected throws when selected")]
    public void FlyoutItemControl_AssertNotSelected_WhenSelected_Throws()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("flyoutitem");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableFlyoutItemControl(context, "SettingsItem");
        control.SetSelected(true);

        // Act & Assert
        var action = () => control.AssertNotSelected();
        action.Should().Throw<AssertionException>();
    }

    [Fact(DisplayName = "FI-009: IsExists returns true when exists")]
    public void FlyoutItemControl_IsExists_WhenExists_ReturnsTrue()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("flyoutitem");
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableFlyoutItemControl(context, "SettingsItem");

        // Act
        var exists = control.IsExists();

        // Assert
        exists.Should().BeTrue();
    }

    [Fact(DisplayName = "FI-010: IsVisible returns true when visible")]
    public void FlyoutItemControl_IsVisible_WhenVisible_ReturnsTrue()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockElement = MockAppiumFactory.CreateMockElement("flyoutitem");
        mockElement.Setup(e => e.Displayed).Returns(true);
        MockAppiumFactory.SetupFindElement(mockDriverWrapper, mockElement);
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);
        var control = new TestableFlyoutItemControl(context, "SettingsItem");

        // Act
        var visible = control.IsVisible();

        // Assert
        visible.Should().BeTrue();
    }

    #endregion
}
