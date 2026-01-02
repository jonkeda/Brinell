# 8. IsBusy-Based State Tracking - Code Examples

**Parent:** [IsBusy-Based State Tracking](21d8_IsBusyStateTracking.md)  
**Version:** 3.0 (December 2025)

> **v3 Note:** BusyPageBase is now platform-specific. Each platform project has its own
> `PageBase` and `BusyPageBase` in `Controls/Base/`. Navigation methods return void.

---

## 8.1 BusyPageBase Implementation (WPF)

```csharp
namespace Oravey.UITestFramework.Wpf.Controls.Base;

using Oravey.UITestFramework.Core.Abstractions;
using Oravey.UITestFramework.Wpf.Infrastructure;

/// <summary>
/// Base class for WPF pages with IsBusy state tracking.
/// Each platform has its own BusyPageBase with native driver access.
/// </summary>
public abstract class BusyPageBase : PageBase
{
    /// <summary>
    /// AutomationId of the busy indicator control.
    /// Override in derived classes to specify the indicator.
    /// </summary>
    protected abstract string BusyIndicatorId { get; }
    
    /// <summary>
    /// Optional: Timeout specific to this page's busy state.
    /// Override for pages with known long-loading content.
    /// </summary>
    protected virtual int BusyTimeoutMs => Context.DefaultTimeoutMs;
    
    protected FlaUITestContext Context { get; }
    
    protected BusyPageBase(FlaUITestContext context, string pageName)
        : base(context, pageName)
    {
        Context = context;
    }
    
    #region IsBusy State
    
    /// <summary>
    /// Check if page is currently in busy/loading state.
    /// </summary>
    public virtual bool IsBusy()
    {
        var indicator = Context.Driver.FindElement(BusyIndicatorId);
        if (indicator == null)
        {
            return false;  // No indicator means not busy
        }
        
        // Busy if indicator is visible
        return Context.Driver.IsVisible(indicator);
    }
    
    /// <summary>
    /// Wait for page to exit busy state.
    /// </summary>
    public virtual bool WaitForNotBusy(int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? BusyTimeoutMs;
        
        return Context.WaitFor(
            condition: () => !IsBusy(),
            timeoutMs: timeout,
            description: $"'{PageName}' not busy");
    }
    
    /// <summary>
    /// Wait for page to enter busy state.
    /// Useful for testing that loading indicators appear.
    /// </summary>
    public virtual bool WaitForBusy(int? timeoutMs = null)
    {
        return Context.WaitFor(
            condition: IsBusy,
            timeoutMs: timeoutMs ?? Context.ShortTimeoutMs,
            description: $"'{PageName}' busy");
    }
    
    #endregion
    
    #region Page Ready (overrides base)
    
    /// <summary>
    /// Wait for page to be fully ready for interaction.
    /// Combines IsDisplayed and IsBusy checks.
    /// </summary>
    public override void WaitForPageReady(int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? BusyTimeoutMs;
        
        Log("Waiting for page ready...");
        
        // Step 1: Wait for page to be displayed
        WaitForDisplayed(timeout);
        
        // Step 2: Wait for busy indicator to clear
        if (!WaitForNotBusy(timeout))
        {
            Logger.LogError(
                Context.TestName,
                PageName,
                BusyIndicatorId,
                "PageNotReady",
                $"Page '{PageName}' did not become ready within {timeout}ms",
                null);
            
            throw new AssertionException(
                $"Page '{PageName}' is still busy after {timeout}ms");
        }
        
        Log("Page ready");
    }
    
    /// <summary>
    /// Check that page is ready, throw if not.
    /// </summary>
    public virtual void CheckPageReady(int? timeoutMs = null)
    {
        CheckDisplayed(timeoutMs);
        
        if (IsBusy())
        {
            throw new AssertionException(
                $"Page '{PageName}' is busy. Expected: ready");
        }
    }
    
    /// <summary>
    /// Assert page ready state with logging.
    /// </summary>
    public virtual void AssertPageReady(int? timeoutMs = null)
    {
        var displayed = IsDisplayed();
        var busy = IsBusy();
        var ready = displayed && !busy;
        
        Logger.LogAssertion(
            Context.TestName,
            PageName,
            null,
            "AssertPageReady",
            $"displayed={displayed},busy={busy}",
            "ready=true",
            ready,
            ready ? null : $"Page not ready: displayed={displayed}, busy={busy}");
        
        if (!ready)
        {
            throw new AssertionException(
                $"Page '{PageName}' is not ready. Displayed: {displayed}, Busy: {busy}");
        }
    }
    
    #endregion
}
```

---

## 8.2 Indicator Control (WPF)

```csharp
namespace Oravey.UITestFramework.Wpf.Controls;

using Oravey.UITestFramework.Core.Abstractions;
using Oravey.UITestFramework.Wpf.Controls.Base;
using Oravey.UITestFramework.Wpf.Infrastructure;

/// <summary>
/// WPF indicator control (ProgressBar, busy indicators).
/// Each platform has its own indicator implementation.
/// </summary>
public class Indicator : ControlBase
{
    public Indicator(FlaUITestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }
    
    /// <summary>
    /// Check if indicator is currently active (visible and showing activity).
    /// </summary>
    public virtual bool IsActive()
    {
        return IsVisible();
    }
    
    /// <summary>
    /// Wait for indicator to become active.
    /// </summary>
    public virtual bool WaitForActive(bool active = true, int? timeoutMs = null)
    {
        return Context.WaitFor(
            () => IsActive() == active,
            timeoutMs,
            $"'{AutomationId}' active = {active}");
    }
    
    /// <summary>
    /// Check indicator active state, throw on failure.
    /// </summary>
    public virtual void CheckActive(bool active = true, int? timeoutMs = null)
    {
        if (!WaitForActive(active, timeoutMs))
        {
            throw new AssertionException(
                $"Indicator '{AutomationId}' active check failed. " +
                $"Expected: {active}, Actual: {IsActive()}");
        }
    }
    
    /// <summary>
    /// Assert indicator active state with logging.
    /// </summary>
    public virtual void AssertActive(bool active = true, int? timeoutMs = null)
    {
        var actual = IsActive();
        var passed = WaitForActive(active, timeoutMs);
        var finalActual = IsActive();
        
        Logger.LogAssertion(
            Context.TestName,
            Page?.PageName,
            AutomationId,
            "AssertActive",
            finalActual.ToString(),
            active.ToString(),
            passed,
            passed ? null : $"Expected active={active}, was {finalActual}");
        
        if (!passed)
        {
            throw new AssertionException(
                $"Indicator '{AutomationId}' assertion failed. " +
                $"Expected active: {active}, Actual: {finalActual}");
        }
    }
}
```

---

## 8.3 Concrete Page Example (WPF)

```csharp
namespace Oravey.Tools.Wpf.UITests.PageObjects;

using Oravey.UITestFramework.Wpf.Controls;
using Oravey.UITestFramework.Wpf.Controls.Base;
using Oravey.UITestFramework.Wpf.Infrastructure;

/// <summary>
/// Settings page with IsBusy state tracking.
/// Uses platform-specific FlaUITestContext.
/// </summary>
public class SettingsPage : BusyPageBase
{
    // Busy indicator AutomationId
    protected override string BusyIndicatorId => "SettingsPageBusyIndicator";
    
    // Override timeout for this page (loads user preferences)
    protected override int BusyTimeoutMs => 15000;
    
    // Page controls - use simplified WPF control names
    public Label PageTitle { get; }
    public TextBox UsernameInput { get; }
    public ComboBox ThemeDropdown { get; }
    public CheckBox DarkModeToggle { get; }
    public Button SaveButton { get; }
    public Button CancelButton { get; }
    public Indicator LoadingIndicator { get; }
    
    public SettingsPage(FlaUITestContext context) : base(context, "Settings")
    {
        PageTitle = new Label(context, this, "SettingsPageTitle");
        UsernameInput = new TextBox(context, this, "UsernameInput");
        ThemeDropdown = new ComboBox(context, this, "ThemeDropdown");
        DarkModeToggle = new CheckBox(context, this, "DarkModeToggle");
        SaveButton = new Button(context, this, "SaveButton");
        CancelButton = new Button(context, this, "CancelButton");
        LoadingIndicator = new Indicator(context, this, BusyIndicatorId);
    }
    
    public override bool IsDisplayed()
    {
        return PageTitle.IsVisible();
    }
    
    /// <summary>
    /// Save settings and wait for completion.
    /// </summary>
    public void SaveAndWait()
    {
        Log("Saving settings...");
        
        SaveButton.Click();
        
        // Wait for save to start (indicator appears)
        WaitForBusy(Context.ShortTimeoutMs);
        
        // Wait for save to complete (indicator disappears)
        WaitForNotBusy();
        
        Log("Settings saved");
    }
    
    /// <summary>
    /// Navigate back to shell.
    /// Caller creates ShellPage and waits for ready.
    /// </summary>
    public void NavigateToHome()
    {
        CancelButton.Click();
    }
}
```

---

## 8.4 Shell Page with Navigation (v3 - Returns Void)

```csharp
namespace Oravey.Tools.Wpf.UITests.PageObjects;

using Oravey.UITestFramework.Wpf.Controls;
using Oravey.UITestFramework.Wpf.Controls.Base;
using Oravey.UITestFramework.Wpf.Infrastructure;

/// <summary>
/// Main shell page with navigation.
/// Navigation methods return void - tests create page objects.
/// </summary>
public class ShellPage : BusyPageBase
{
    protected override string BusyIndicatorId => "ShellBusyIndicator";
    
    public Button HomeButton { get; }
    public Button WorldBrowserButton { get; }
    public Button SettingsButton { get; }
    public Indicator GlobalLoadingIndicator { get; }
    
    public ShellPage(FlaUITestContext context) : base(context, "Shell")
    {
        HomeButton = new Button(context, this, "HomeButton");
        WorldBrowserButton = new Button(context, this, "WorldBrowserButton");
        SettingsButton = new Button(context, this, "SettingsButton");
        GlobalLoadingIndicator = new Indicator(context, this, BusyIndicatorId);
    }
    
    public override bool IsDisplayed()
    {
        return HomeButton.IsVisible() || SettingsButton.IsVisible();
    }
    
    /// <summary>
    /// Navigate to Settings page.
    /// Caller creates SettingsPage and calls WaitForReady().
    /// </summary>
    public void NavigateToSettings()
    {
        Log("Navigating to Settings");
        SettingsButton.Click();
    }
    
    /// <summary>
    /// Navigate to World Browser page.
    /// Caller creates WorldBrowserPage and calls WaitForReady().
    /// </summary>
    public void NavigateToWorldBrowser()
    {
        Log("Navigating to World Browser");
        WorldBrowserButton.Click();
    }
}
```

---

## 8.5 Test Examples

```csharp
namespace Oravey.Tools.Wpf.UITests.Tests;

using FluentAssertions;
using Oravey.Tools.Wpf.UITests.Infrastructure;
using Oravey.Tools.Wpf.UITests.PageObjects;
using Xunit;
using Xunit.Abstractions;

[Trait("Category", "UITest")]
[Collection("UITests")]
public class IsBusyStateTests : UITestBase
{
    public IsBusyStateTests(ITestOutputHelper output) : base(output) { }
    
    [Fact]
    public void Page_WaitForPageReady_Waits_For_Loading()
    {
        // Arrange
        var shell = new ShellPage(Context);
        shell.WaitForReady();
        
        // Act - Navigate to settings (triggers data load)
        shell.NavigateToSettings();
        var settings = new SettingsPage(Context);
        settings.WaitForReady();
        
        // Assert - By the time we get here, page should be ready
        settings.PageTitle.AssertText("Settings");
        settings.SaveButton.AssertVisible(true);
    }
    
    [Fact]
    public void Save_Operation_Shows_And_Hides_Indicator()
    {
        // Arrange
        var shell = new ShellPage(Context);
        shell.NavigateToSettings();
        var settings = new SettingsPage(Context);
        settings.WaitForReady();
        
        // Make a change
        settings.DarkModeToggle.Toggle();
        
        // Act - Save settings
        settings.SaveButton.Click();
        
        // Assert - Indicator should appear during save
        settings.LoadingIndicator.WaitForActive(true, Context.ShortTimeoutMs);
        
        // Assert - Indicator should disappear when complete
        settings.LoadingIndicator.WaitForActive(false, Context.DefaultTimeoutMs);
    }
    
    [Fact]
    public void IsBusy_Returns_False_When_Page_Ready()
    {
        // Arrange
        var shell = new ShellPage(Context);
        shell.WaitForReady();
        
        shell.NavigateToSettings();
        var settings = new SettingsPage(Context);
        settings.WaitForReady();
        
        // Assert - IsBusy should be false after WaitForReady
        settings.IsBusy().Should().BeFalse();
    }
    
    [Fact]
    public void Multiple_Busy_Regions_Are_Tracked()
    {
        // Arrange
        var shell = new ShellPage(Context);
        var dashboard = shell.NavigateToDashboard();
        
        // Wait for main page ready
        dashboard.WaitForPageReady();
        
        // Act - Wait for specific regions
        dashboard.ChartLoadingIndicator.WaitForActive(false);
        dashboard.TableLoadingIndicator.WaitForActive(false);
        
        // Assert - All content should be loaded
        dashboard.ChartPanel.AssertVisible(true);
        dashboard.DataTable.AssertItemCount(atLeast: 1);
    }
}
```

---

## 8.6 XAML Busy Indicator Examples

### 8.6.1 WPF

```xml
<Window x:Class="Oravey.Tools.Wpf.Views.SettingsView"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    
    <Grid>
        <!-- Main content -->
        <Grid IsEnabled="{Binding IsNotBusy}">
            <TextBlock AutomationProperties.AutomationId="SettingsPageTitle"
                       Text="Settings" />
            <!-- Other controls -->
        </Grid>
        
        <!-- Busy indicator overlay -->
        <Border Background="#80000000"
                Visibility="{Binding IsBusy, Converter={StaticResource BoolToVis}}">
            <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center">
                <ProgressBar AutomationProperties.AutomationId="SettingsPageBusyIndicator"
                             IsIndeterminate="True"
                             Width="200" Height="20" />
                <TextBlock Text="Loading..." Foreground="White" />
            </StackPanel>
        </Border>
    </Grid>
</Window>
```

### 8.6.2 MAUI

```xml
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             x:Class="Oravey.Tools.Maui.Views.SettingsPage">
    
    <Grid>
        <!-- Main content -->
        <StackLayout IsEnabled="{Binding IsNotBusy}">
            <Label AutomationId="SettingsPageTitle" Text="Settings" />
            <!-- Other controls -->
        </StackLayout>
        
        <!-- Busy indicator -->
        <ActivityIndicator AutomationId="SettingsPageBusyIndicator"
                           IsRunning="{Binding IsBusy}"
                           IsVisible="{Binding IsBusy}"
                           HorizontalOptions="Center"
                           VerticalOptions="Center" />
    </Grid>
</ContentPage>
```

### 8.6.3 HTML

```html
<div class="settings-page">
    <!-- Main content -->
    <div class="content" :class="{ disabled: isBusy }">
        <h1 data-automation-id="SettingsPageTitle">Settings</h1>
        <!-- Other controls -->
    </div>
    
    <!-- Busy indicator -->
    <div data-automation-id="SettingsPageBusyIndicator"
         class="loading-overlay"
         v-show="isBusy">
        <div class="spinner"></div>
        <span>Loading...</span>
    </div>
</div>
```

---

*Related: [Page Object Pattern Code Examples](21d9_PageObjectPattern_CodeExamples.md)*
