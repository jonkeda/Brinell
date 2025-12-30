# 5. Page Objects for Game UI

**Parent:** [Documentation Index](30d0_StrideUITestFramework_Index.md)  
**Previous:** [Control Objects](30d4_ControlObjects.md)  
**Next:** [Test Patterns](30d6_TestPatterns.md)  
**Version:** 1.0 (Proposal - January 2025)

---

## 5.1 Page Object Pattern for Stride

### 5.1.1 Design Principles

Following the v3 architecture of `Oravey.UITestFramework`:

1. **Core defines `IPageObject` interface only**
2. **StridePageBase provides Stride-specific implementation**
3. **Navigation methods return void**
4. **Tests create page objects explicitly**
5. **Each screen has its own page object class**

### 5.1.2 Page Lifecycle

```
1. Test creates page object: new MainMenuPage(context)
2. Page checks for key control: IsDisplayed()
3. Test waits for page ready: WaitForPageReady()
4. Test interacts via controls: NewGameButton.Click()
5. Test creates next page: new NameInputPage(context)
```

---

## 5.2 StridePageBase

```csharp
/// <summary>
/// Base class for all Stride UI page objects.
/// </summary>
public abstract class StridePageBase : IPageObject
{
    protected readonly StrideTestContext Context;
    
    public string PageName { get; }
    ITestContext IPageObject.Context => Context;
    
    /// <summary>
    /// Create a new page object.
    /// </summary>
    protected StridePageBase(StrideTestContext context, string pageName)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        PageName = pageName ?? throw new ArgumentNullException(nameof(pageName));
    }
    
    #region Display Detection
    
    /// <summary>
    /// Get the key control used to detect if this page is displayed.
    /// </summary>
    protected abstract StrideControlBase KeyControl { get; }
    
    /// <summary>
    /// Check if page is currently displayed.
    /// </summary>
    public virtual bool IsDisplayed()
    {
        return KeyControl.IsVisible();
    }
    
    /// <summary>
    /// Wait for page to be displayed.
    /// </summary>
    public bool WaitForDisplayed(int? timeoutMs = null)
    {
        return Context.WaitFor(
            () => IsDisplayed(),
            timeoutMs,
            $"page '{PageName}' displayed");
    }
    
    /// <summary>
    /// Check page is displayed - throws if not.
    /// </summary>
    public void CheckDisplayed(int? timeoutMs = null)
    {
        if (!WaitForDisplayed(timeoutMs))
        {
            throw new CheckFailedException(
                $"Page '{PageName}' is not displayed. Key control: '{KeyControl.AutomationId}'");
        }
    }
    
    /// <summary>
    /// Assert page is displayed.
    /// </summary>
    public void AssertDisplayed(string? message = null)
    {
        var displayed = IsDisplayed();
        LogAssertion("AssertDisplayed", expected: true, actual: displayed);
        
        if (!displayed)
        {
            throw new AssertionException(
                message ?? $"Page '{PageName}' should be displayed but is not.");
        }
    }
    
    /// <summary>
    /// Assert page is not displayed.
    /// </summary>
    public void AssertNotDisplayed(string? message = null)
    {
        var displayed = IsDisplayed();
        LogAssertion("AssertNotDisplayed", expected: false, actual: displayed);
        
        if (displayed)
        {
            throw new AssertionException(
                message ?? $"Page '{PageName}' should not be displayed but is.");
        }
    }
    
    #endregion
    
    #region Page Ready State
    
    /// <summary>
    /// Check if page is ready for interaction (not loading).
    /// Override in pages with loading indicators.
    /// </summary>
    public virtual bool IsPageReady() => IsDisplayed();
    
    /// <summary>
    /// Wait for page to be ready for interaction.
    /// </summary>
    public bool WaitForPageReady(int? timeoutMs = null)
    {
        // First wait for display
        if (!WaitForDisplayed(timeoutMs))
            return false;
        
        // Then wait for ready state
        return Context.WaitFor(
            () => IsPageReady(),
            timeoutMs,
            $"page '{PageName}' ready");
    }
    
    /// <summary>
    /// Check page is ready - throws if not.
    /// </summary>
    public void CheckPageReady(int? timeoutMs = null)
    {
        if (!WaitForPageReady(timeoutMs))
        {
            throw new CheckFailedException(
                $"Page '{PageName}' is not ready for interaction.");
        }
    }
    
    #endregion
    
    #region Logging
    
    protected void Log(string message)
    {
        Context.Log($"[{PageName}] {message}");
    }
    
    protected void LogAssertion(string assertion, object expected, object actual)
    {
        var success = expected?.Equals(actual) ?? actual == null;
        Context.Logger?.LogAssertion(
            Context.TestName,
            PageName,
            "",
            assertion,
            expected?.ToString() ?? "",
            actual?.ToString() ?? "",
            success ? LogResult.Pass : LogResult.Fail);
    }
    
    #endregion
}

/// <summary>
/// Base class for pages with busy/loading indicators.
/// </summary>
public abstract class StrideBusyPageBase : StridePageBase
{
    /// <summary>
    /// Automation ID of the busy indicator element.
    /// </summary>
    protected abstract string? BusyIndicatorId { get; }
    
    protected StrideBusyPageBase(StrideTestContext context, string pageName)
        : base(context, pageName)
    {
    }
    
    /// <summary>
    /// Check if page is currently busy (loading).
    /// </summary>
    public virtual bool IsBusy()
    {
        if (string.IsNullOrEmpty(BusyIndicatorId))
            return false;
        
        return Context.ElementIsVisible(BusyIndicatorId);
    }
    
    /// <summary>
    /// Page is ready when displayed and not busy.
    /// </summary>
    public override bool IsPageReady()
    {
        return IsDisplayed() && !IsBusy();
    }
    
    /// <summary>
    /// Wait for busy state to clear.
    /// </summary>
    public bool WaitForNotBusy(int? timeoutMs = null)
    {
        return Context.WaitFor(
            () => !IsBusy(),
            timeoutMs,
            $"page '{PageName}' not busy");
    }
}
```

---

## 5.3 Oravey Game Page Objects

### 5.3.1 MainMenuPage

```csharp
/// <summary>
/// Page object for the main menu screen.
/// </summary>
public class MainMenuPage : StridePageBase
{
    // Controls
    public StrideTextBlockControl TitleText { get; }
    public StrideButtonControl ContinueButton { get; }
    public StrideButtonControl NewGameButton { get; }
    public StrideButtonControl LoadGameButton { get; }
    public StrideButtonControl SettingsButton { get; }
    public StrideButtonControl ExitButton { get; }
    
    protected override StrideControlBase KeyControl => NewGameButton;
    
    public MainMenuPage(StrideTestContext context) : base(context, "MainMenu")
    {
        TitleText = new StrideTextBlockControl(context, this, "MainMenu.Title");
        ContinueButton = new StrideButtonControl(context, this, "MainMenu.Continue");
        NewGameButton = new StrideButtonControl(context, this, "MainMenu.NewGame");
        LoadGameButton = new StrideButtonControl(context, this, "MainMenu.LoadGame");
        SettingsButton = new StrideButtonControl(context, this, "MainMenu.Settings");
        ExitButton = new StrideButtonControl(context, this, "MainMenu.Exit");
    }
    
    #region State Queries
    
    /// <summary>
    /// Check if Continue button is available (saves exist).
    /// </summary>
    public bool HasSaves => ContinueButton.IsEnabled();
    
    #endregion
    
    #region Navigation Methods (return void per v3 pattern)
    
    /// <summary>
    /// Navigate to new game name input.
    /// </summary>
    public void NavigateToNewGame()
    {
        Log("Navigating to New Game");
        NewGameButton.Click();
    }
    
    /// <summary>
    /// Navigate to load game screen.
    /// </summary>
    public void NavigateToLoadGame()
    {
        Log("Navigating to Load Game");
        LoadGameButton.Click();
    }
    
    /// <summary>
    /// Continue most recent save.
    /// </summary>
    public void ContinueGame()
    {
        Log("Continuing game");
        ContinueButton.Click();
    }
    
    /// <summary>
    /// Exit the game.
    /// </summary>
    public void ExitGame()
    {
        Log("Exiting game");
        ExitButton.Click();
    }
    
    #endregion
    
    #region Keyboard Navigation
    
    /// <summary>
    /// Navigate menu using keyboard.
    /// </summary>
    public void NavigateUp()
    {
        Context.PressKey(VirtualKeyCode.UP);
    }
    
    public void NavigateDown()
    {
        Context.PressKey(VirtualKeyCode.DOWN);
    }
    
    public void SelectCurrent()
    {
        Context.PressKey(VirtualKeyCode.RETURN);
    }
    
    #endregion
}
```

### 5.3.2 NameInputPage

```csharp
/// <summary>
/// Page object for character name input dialog.
/// </summary>
public class NameInputPage : StridePageBase
{
    // Controls
    public StrideTextBlockControl TitleText { get; }
    public StrideEditTextControl NameInput { get; }
    public StrideButtonControl StartButton { get; }
    public StrideButtonControl CancelButton { get; }
    
    protected override StrideControlBase KeyControl => NameInput;
    
    public NameInputPage(StrideTestContext context) : base(context, "NameInput")
    {
        TitleText = new StrideTextBlockControl(context, this, "NameInput.Title");
        NameInput = new StrideEditTextControl(context, this, "NameInput.NameField");
        StartButton = new StrideButtonControl(context, this, "NameInput.Start");
        CancelButton = new StrideButtonControl(context, this, "NameInput.Cancel");
    }
    
    #region Actions
    
    /// <summary>
    /// Enter character name and start game.
    /// </summary>
    public void StartNewGame(string characterName)
    {
        Log($"Starting new game with character: {characterName}");
        
        NameInput.ClearAndEnter(characterName);
        StartButton.Click();
    }
    
    /// <summary>
    /// Cancel and return to main menu.
    /// </summary>
    public void Cancel()
    {
        Log("Cancelling name input");
        CancelButton.Click();
    }
    
    /// <summary>
    /// Get current name in input field.
    /// </summary>
    public string GetCurrentName() => NameInput.GetText();
    
    #endregion
    
    #region Validation
    
    /// <summary>
    /// Check if Start button is enabled (name is valid).
    /// </summary>
    public bool IsNameValid => StartButton.IsEnabled();
    
    /// <summary>
    /// Assert name validation state.
    /// </summary>
    public void AssertNameValid()
    {
        StartButton.AssertEnabled("Character name should be valid");
    }
    
    public void AssertNameInvalid()
    {
        StartButton.AssertDisabled("Character name should be invalid");
    }
    
    #endregion
}
```

### 5.3.3 LoadGamePage

```csharp
/// <summary>
/// Page object for load game screen.
/// </summary>
public class LoadGamePage : StrideBusyPageBase
{
    // Controls
    public StrideTextBlockControl TitleText { get; }
    public StrideListBoxControl SaveList { get; }
    public StrideButtonControl LoadButton { get; }
    public StrideButtonControl DeleteButton { get; }
    public StrideButtonControl BackButton { get; }
    public StrideTextBlockControl LoadingIndicator { get; }
    
    protected override StrideControlBase KeyControl => SaveList;
    protected override string? BusyIndicatorId => "LoadGame.Loading";
    
    public LoadGamePage(StrideTestContext context) : base(context, "LoadGame")
    {
        TitleText = new StrideTextBlockControl(context, this, "LoadGame.Title");
        SaveList = new StrideListBoxControl(context, this, "LoadGame.SaveList");
        LoadButton = new StrideButtonControl(context, this, "LoadGame.Load");
        DeleteButton = new StrideButtonControl(context, this, "LoadGame.Delete");
        BackButton = new StrideButtonControl(context, this, "LoadGame.Back");
        LoadingIndicator = new StrideTextBlockControl(context, this, "LoadGame.Loading");
    }
    
    #region Queries
    
    /// <summary>
    /// Get list of available save names.
    /// </summary>
    public List<string> GetSaveNames() => SaveList.GetItems();
    
    /// <summary>
    /// Get number of available saves.
    /// </summary>
    public int SaveCount => SaveList.GetItemCount();
    
    /// <summary>
    /// Get selected save name.
    /// </summary>
    public string SelectedSave => SaveList.GetSelectedText();
    
    #endregion
    
    #region Actions
    
    /// <summary>
    /// Select a save by name.
    /// </summary>
    public void SelectSave(string saveName)
    {
        Log($"Selecting save: {saveName}");
        SaveList.SelectByText(saveName);
    }
    
    /// <summary>
    /// Select a save by index.
    /// </summary>
    public void SelectSave(int index)
    {
        Log($"Selecting save at index: {index}");
        SaveList.SelectByIndex(index);
    }
    
    /// <summary>
    /// Load the selected save.
    /// </summary>
    public void LoadSelectedSave()
    {
        Log($"Loading save: {SelectedSave}");
        LoadButton.Click();
    }
    
    /// <summary>
    /// Delete the selected save.
    /// </summary>
    public void DeleteSelectedSave()
    {
        Log($"Deleting save: {SelectedSave}");
        DeleteButton.Click();
    }
    
    /// <summary>
    /// Go back to main menu.
    /// </summary>
    public void NavigateBack()
    {
        Log("Navigating back to main menu");
        BackButton.Click();
    }
    
    /// <summary>
    /// Select and load a save by name.
    /// </summary>
    public void LoadSave(string saveName)
    {
        SelectSave(saveName);
        LoadSelectedSave();
    }
    
    #endregion
}
```

### 5.3.4 PauseMenuPage

```csharp
/// <summary>
/// Page object for in-game pause menu.
/// </summary>
public class PauseMenuPage : StridePageBase
{
    // Controls
    public StrideTextBlockControl TitleText { get; }
    public StrideButtonControl ResumeButton { get; }
    public StrideButtonControl SaveButton { get; }
    public StrideButtonControl SettingsButton { get; }
    public StrideButtonControl MainMenuButton { get; }
    public StrideButtonControl ExitButton { get; }
    
    protected override StrideControlBase KeyControl => ResumeButton;
    
    public PauseMenuPage(StrideTestContext context) : base(context, "PauseMenu")
    {
        TitleText = new StrideTextBlockControl(context, this, "PauseMenu.Title");
        ResumeButton = new StrideButtonControl(context, this, "PauseMenu.Resume");
        SaveButton = new StrideButtonControl(context, this, "PauseMenu.Save");
        SettingsButton = new StrideButtonControl(context, this, "PauseMenu.Settings");
        MainMenuButton = new StrideButtonControl(context, this, "PauseMenu.MainMenu");
        ExitButton = new StrideButtonControl(context, this, "PauseMenu.Exit");
    }
    
    #region Actions
    
    /// <summary>
    /// Resume gameplay.
    /// </summary>
    public void Resume()
    {
        Log("Resuming game");
        ResumeButton.Click();
    }
    
    /// <summary>
    /// Save current game.
    /// </summary>
    public void SaveGame()
    {
        Log("Saving game");
        SaveButton.Click();
    }
    
    /// <summary>
    /// Return to main menu.
    /// </summary>
    public void NavigateToMainMenu()
    {
        Log("Navigating to main menu");
        MainMenuButton.Click();
    }
    
    /// <summary>
    /// Exit game entirely.
    /// </summary>
    public void ExitGame()
    {
        Log("Exiting game");
        ExitButton.Click();
    }
    
    #endregion
    
    #region Keyboard Shortcuts
    
    /// <summary>
    /// Press Escape to toggle pause menu.
    /// </summary>
    public static void TogglePause(StrideTestContext context)
    {
        context.PressKey(VirtualKeyCode.ESCAPE);
    }
    
    #endregion
}
```

### 5.3.5 LoadingScreenPage

```csharp
/// <summary>
/// Page object for loading screen.
/// </summary>
public class LoadingScreenPage : StridePageBase
{
    // Controls
    public StrideTextBlockControl LoadingText { get; }
    public StrideSliderControl ProgressBar { get; }
    public StrideTextBlockControl ProgressText { get; }
    
    protected override StrideControlBase KeyControl => LoadingText;
    
    public LoadingScreenPage(StrideTestContext context) : base(context, "LoadingScreen")
    {
        LoadingText = new StrideTextBlockControl(context, this, "Loading.Text");
        ProgressBar = new StrideSliderControl(context, this, "Loading.ProgressBar");
        ProgressText = new StrideTextBlockControl(context, this, "Loading.ProgressText");
    }
    
    #region Queries
    
    /// <summary>
    /// Get current loading progress (0-100).
    /// </summary>
    public double Progress => ProgressBar.GetValue();
    
    /// <summary>
    /// Check if loading is complete.
    /// </summary>
    public bool IsComplete => Progress >= 100 || !IsDisplayed();
    
    #endregion
    
    #region Waits
    
    /// <summary>
    /// Wait for loading to complete.
    /// </summary>
    public bool WaitForLoadingComplete(int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? Context.DefaultTimeoutMs * 3; // Loading can take time
        
        return Context.WaitFor(
            () => IsComplete,
            timeout,
            "loading complete");
    }
    
    /// <summary>
    /// Wait for loading to complete and assert success.
    /// </summary>
    public void CheckLoadingComplete(int? timeoutMs = null)
    {
        if (!WaitForLoadingComplete(timeoutMs))
        {
            throw new CheckFailedException(
                $"Loading did not complete. Progress: {Progress}%");
        }
    }
    
    #endregion
}
```

### 5.3.6 InGameUIPage (HUD)

```csharp
/// <summary>
/// Page object for in-game HUD elements.
/// </summary>
public class InGameUIPage : StridePageBase
{
    // Mini Map
    public StrideControlBase MiniMap { get; }
    
    // Area Name Popup
    public StrideTextBlockControl AreaNamePopup { get; }
    
    // Debug Overlay (optional)
    public StrideTextBlockControl FpsCounter { get; }
    public StrideTextBlockControl DebugInfo { get; }
    
    protected override StrideControlBase KeyControl => MiniMap;
    
    public InGameUIPage(StrideTestContext context) : base(context, "InGameUI")
    {
        MiniMap = new StrideControlBase(context, this, "InGame.MiniMap");
        AreaNamePopup = new StrideTextBlockControl(context, this, "InGame.AreaName");
        FpsCounter = new StrideTextBlockControl(context, this, "InGame.FPS");
        DebugInfo = new StrideTextBlockControl(context, this, "InGame.DebugInfo");
    }
    
    #region Game Actions
    
    /// <summary>
    /// Open pause menu.
    /// </summary>
    public void OpenPauseMenu()
    {
        Log("Opening pause menu");
        Context.PressKey(VirtualKeyCode.ESCAPE);
    }
    
    /// <summary>
    /// Toggle debug overlay.
    /// </summary>
    public void ToggleDebugOverlay()
    {
        Log("Toggling debug overlay");
        Context.PressKey(VirtualKeyCode.F3);
    }
    
    /// <summary>
    /// Toggle full map view.
    /// </summary>
    public void ToggleFullMap()
    {
        Log("Toggling full map");
        Context.PressKey(VirtualKeyCode.M);
    }
    
    #endregion
    
    #region State Queries
    
    /// <summary>
    /// Get current area name if popup is visible.
    /// </summary>
    public string? GetCurrentAreaName()
    {
        if (AreaNamePopup.IsVisible())
            return AreaNamePopup.GetText();
        return null;
    }
    
    /// <summary>
    /// Check if debug overlay is visible.
    /// </summary>
    public bool IsDebugVisible => FpsCounter.IsVisible();
    
    #endregion
}
```

---

## 5.4 Page Object Guidelines

### 5.4.1 Naming Conventions

| Element | Convention | Example |
|---------|------------|---------|
| Page class | `{Screen}Page` | `MainMenuPage` |
| Control property | `{Name}{Type}` | `NewGameButton`, `NameInput` |
| Navigation method | `NavigateTo{Target}()` | `NavigateToLoadGame()` |
| Action method | Verb phrase | `StartNewGame()`, `SaveGame()` |
| Query property | Noun/adjective | `HasSaves`, `SaveCount` |

### 5.4.2 Control Declaration Pattern

```csharp
public class SomePage : StridePageBase
{
    // 1. Declare controls as public readonly properties
    public StrideButtonControl SomeButton { get; }
    public StrideEditTextControl SomeInput { get; }
    
    // 2. Override KeyControl for display detection
    protected override StrideControlBase KeyControl => SomeButton;
    
    // 3. Initialize in constructor
    public SomePage(StrideTestContext context) : base(context, "SomePage")
    {
        SomeButton = new StrideButtonControl(context, this, "Page.Button");
        SomeInput = new StrideEditTextControl(context, this, "Page.Input");
    }
}
```

### 5.4.3 Automation ID Conventions

```
{Screen}.{Element}

Examples:
- MainMenu.NewGame
- MainMenu.LoadGame
- NameInput.NameField
- NameInput.Start
- LoadGame.SaveList
- LoadGame.Load
- PauseMenu.Resume
- InGame.MiniMap
```

---

## 5.5 Page Registration in Game

For the automation layer to find UI elements, they must be registered when created:

```csharp
// In MainMenuUI.cs
private void CreateUI()
{
    // ... existing code ...
    
    _continueButton = CreateMenuButton("Continue", OnContinueClick)
        .WithAutomationId("MainMenu.Continue");
    
    var newGameButton = CreateMenuButton("New Game", OnNewGameClick)
        .WithAutomationId("MainMenu.NewGame");
    
    var loadGameButton = CreateMenuButton("Load Game", OnLoadGameClick)
        .WithAutomationId("MainMenu.LoadGame");
    
    // ... etc ...
}
```

---

*Document Version: 1.0*  
*Last Updated: January 2025*
