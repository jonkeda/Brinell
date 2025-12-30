# 6. Test Execution & Patterns

**Parent:** [Documentation Index](30d0_StrideUITestFramework_Index.md)  
**Previous:** [Page Objects](30d5_PageObjects.md)  
**Next:** [Implementation Roadmap](30d7_ImplementationRoadmap.md)  
**Version:** 1.0 (Proposal - January 2025)

---

## 6.1 Test Structure

### 6.1.1 xUnit Test Collection

All Stride UI tests share a single game instance:

```csharp
/// <summary>
/// Collection definition for Stride UI tests.
/// All tests in this collection share the game fixture.
/// </summary>
[CollectionDefinition("Stride UI Tests")]
public class StrideUITestCollection : ICollectionFixture<StrideGameFixture>
{
}

/// <summary>
/// Fixture that manages game lifecycle for all tests.
/// </summary>
public class StrideGameFixture : IAsyncLifetime
{
    private StrideGameDriver? _gameDriver;
    
    public StrideTestContext Context { get; private set; } = null!;
    public StrideTestOptions Options { get; }
    
    public StrideGameFixture()
    {
        Options = LoadTestOptions();
    }
    
    public async Task InitializeAsync()
    {
        // Start the game
        _gameDriver = new StrideGameDriver();
        await _gameDriver.StartGameAsync(Options);
        
        // Create test context
        Context = new StrideTestContext(_gameDriver.Channel, Options);
        
        // Wait for game ready
        if (!Context.WaitForGameReady(Options.StartupTimeoutMs))
        {
            throw new InvalidOperationException(
                "Game did not become ready within timeout");
        }
        
        // Set up CSV logging
        if (Options.EnableCsvLogging)
        {
            var logPath = Path.Combine(Options.LogDirectory, 
                $"uitest-{DateTime.Now:yyyyMMdd-HHmmss}.csv");
            Context.SetLogger(new CsvTestLogger(logPath));
        }
    }
    
    public async Task DisposeAsync()
    {
        Context?.Dispose();
        
        if (_gameDriver != null)
        {
            await _gameDriver.StopGameAsync();
            _gameDriver.Dispose();
        }
    }
    
    private StrideTestOptions LoadTestOptions()
    {
        // Load from configuration or use defaults
        return new StrideTestOptions
        {
            GameExecutablePath = FindGameExecutable(),
            StartupTimeoutMs = 30000,
            DefaultTimeoutMs = 10000,
            CaptureScreenshotOnFailure = true
        };
    }
    
    private string FindGameExecutable()
    {
        // Find game executable relative to test assembly
        var testDir = AppContext.BaseDirectory;
        var possiblePaths = new[]
        {
            Path.Combine(testDir, "..", "..", "..", "..", "Game", "Oravey.Game", "bin", "Debug", "net10.0", "Oravey.Game.exe"),
            Path.Combine(testDir, "Oravey.Game.exe"),
            Environment.GetEnvironmentVariable("ORAVEY_GAME_PATH") ?? ""
        };
        
        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
                return Path.GetFullPath(path);
        }
        
        throw new FileNotFoundException("Could not find Oravey.Game executable");
    }
}
```

### 6.1.2 Base Test Class

```csharp
/// <summary>
/// Base class for Stride UI tests.
/// </summary>
[Collection("Stride UI Tests")]
public abstract class StrideUITestBase : IAsyncLifetime
{
    protected readonly StrideGameFixture Fixture;
    protected StrideTestContext Context => Fixture.Context;
    
    protected StrideUITestBase(StrideGameFixture fixture)
    {
        Fixture = fixture;
    }
    
    public virtual Task InitializeAsync()
    {
        // Set test name for logging
        Context.TestName = GetType().Name + "." + GetTestMethodName();
        
        // Navigate to known state if needed
        return NavigateToStartStateAsync();
    }
    
    public virtual Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Override to navigate to required starting state for tests.
    /// </summary>
    protected virtual Task NavigateToStartStateAsync()
    {
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Take screenshot on test failure.
    /// </summary>
    protected void CaptureFailureScreenshot(string testName)
    {
        if (Fixture.Options.CaptureScreenshotOnFailure)
        {
            var screenshotName = $"failure-{testName}-{DateTime.Now:HHmmss}";
            Context.TakeScreenshot(screenshotName);
        }
    }
    
    private string GetTestMethodName()
    {
        // Get current test method name from xUnit
        return "Test"; // Simplified - actual implementation would use xUnit reflection
    }
}
```

---

## 6.2 Test Examples

### 6.2.1 Smoke Tests

```csharp
/// <summary>
/// Critical path smoke tests that verify basic functionality.
/// </summary>
[Trait("Category", "Smoke")]
public class ApplicationStartupTests : StrideUITestBase
{
    public ApplicationStartupTests(StrideGameFixture fixture) : base(fixture) { }
    
    [Fact]
    public void Game_Starts_ShowsMainMenu()
    {
        // Arrange
        var mainMenu = new MainMenuPage(Context);
        
        // Assert
        mainMenu.WaitForPageReady();
        mainMenu.AssertDisplayed();
        mainMenu.TitleText.AssertTextEquals("ORAVEY");
    }
    
    [Fact]
    public void MainMenu_AllButtons_AreVisible()
    {
        // Arrange
        var mainMenu = new MainMenuPage(Context);
        mainMenu.WaitForPageReady();
        
        // Assert
        mainMenu.NewGameButton.AssertVisible();
        mainMenu.LoadGameButton.AssertVisible();
        mainMenu.ExitButton.AssertVisible();
    }
    
    [Fact]
    public void MainMenu_NewGame_NavigatesToNameInput()
    {
        // Arrange
        var mainMenu = new MainMenuPage(Context);
        mainMenu.WaitForPageReady();
        
        // Act
        mainMenu.NavigateToNewGame();
        
        // Assert
        var nameInput = new NameInputPage(Context);
        nameInput.WaitForPageReady();
        nameInput.AssertDisplayed();
    }
}
```

### 6.2.2 Main Menu Tests

```csharp
/// <summary>
/// Tests for main menu functionality.
/// </summary>
[Trait("Category", "UI.MainMenu")]
public class MainMenuTests : StrideUITestBase
{
    public MainMenuTests(StrideGameFixture fixture) : base(fixture) { }
    
    protected override Task NavigateToStartStateAsync()
    {
        // Ensure we're at main menu
        var mainMenu = new MainMenuPage(Context);
        if (!mainMenu.IsDisplayed())
        {
            // Navigate back to main menu from wherever we are
            NavigateToMainMenu();
        }
        return Task.CompletedTask;
    }
    
    private void NavigateToMainMenu()
    {
        // Try various ways to get to main menu
        // 1. Check if in game, press ESC for pause menu
        Context.PressKey(VirtualKeyCode.ESCAPE);
        Thread.Sleep(500);
        
        var pauseMenu = new PauseMenuPage(Context);
        if (pauseMenu.IsDisplayed())
        {
            pauseMenu.NavigateToMainMenu();
        }
        
        // 2. Check for other dialogs and cancel them
        Context.PressKey(VirtualKeyCode.ESCAPE);
    }
    
    [Fact]
    public void ContinueButton_WithNoSaves_IsDisabled()
    {
        // This test assumes fresh install with no saves
        // Arrange
        var mainMenu = new MainMenuPage(Context);
        mainMenu.WaitForPageReady();
        
        // Assert
        if (!mainMenu.HasSaves)
        {
            mainMenu.ContinueButton.AssertDisabled();
        }
    }
    
    [Fact]
    public void KeyboardNavigation_UpDown_ChangesSelection()
    {
        // Arrange
        var mainMenu = new MainMenuPage(Context);
        mainMenu.WaitForPageReady();
        
        // Act - Navigate down
        mainMenu.NavigateDown();
        Thread.Sleep(100);
        
        // Note: Verifying selection state would require 
        // automation support for tracking focused element
    }
    
    [Fact]
    public void NewGame_ThenCancel_ReturnsToMainMenu()
    {
        // Arrange
        var mainMenu = new MainMenuPage(Context);
        mainMenu.WaitForPageReady();
        
        // Act
        mainMenu.NavigateToNewGame();
        
        var nameInput = new NameInputPage(Context);
        nameInput.WaitForPageReady();
        nameInput.Cancel();
        
        // Assert
        mainMenu.WaitForPageReady();
        mainMenu.AssertDisplayed();
    }
}
```

### 6.2.3 New Game Flow Tests

```csharp
/// <summary>
/// Tests for new game creation flow.
/// </summary>
[Trait("Category", "UI.NewGame")]
public class NewGameFlowTests : StrideUITestBase
{
    public NewGameFlowTests(StrideGameFixture fixture) : base(fixture) { }
    
    [Fact]
    public void NameInput_EmptyName_DisablesStart()
    {
        // Arrange
        var mainMenu = new MainMenuPage(Context);
        mainMenu.WaitForPageReady();
        mainMenu.NavigateToNewGame();
        
        var nameInput = new NameInputPage(Context);
        nameInput.WaitForPageReady();
        
        // Act
        nameInput.NameInput.Clear();
        
        // Assert
        nameInput.StartButton.AssertDisabled();
    }
    
    [Fact]
    public void NameInput_ValidName_EnablesStart()
    {
        // Arrange
        var mainMenu = new MainMenuPage(Context);
        mainMenu.WaitForPageReady();
        mainMenu.NavigateToNewGame();
        
        var nameInput = new NameInputPage(Context);
        nameInput.WaitForPageReady();
        
        // Act
        nameInput.NameInput.ClearAndEnter("TestHero");
        
        // Assert
        nameInput.StartButton.AssertEnabled();
    }
    
    [Fact]
    public void NameInput_Start_ShowsLoadingThenGame()
    {
        // Arrange
        var mainMenu = new MainMenuPage(Context);
        mainMenu.WaitForPageReady();
        mainMenu.NavigateToNewGame();
        
        var nameInput = new NameInputPage(Context);
        nameInput.WaitForPageReady();
        
        // Act
        nameInput.StartNewGame("TestHero");
        
        // Assert - Loading screen should appear
        var loading = new LoadingScreenPage(Context);
        if (loading.IsDisplayed())
        {
            loading.CheckLoadingComplete(30000);
        }
        
        // Assert - Game UI should appear
        var inGameUI = new InGameUIPage(Context);
        inGameUI.WaitForPageReady();
        inGameUI.AssertDisplayed();
    }
}
```

### 6.2.4 Load Game Tests

```csharp
/// <summary>
/// Tests for load game functionality.
/// </summary>
[Trait("Category", "UI.LoadGame")]
public class LoadGameTests : StrideUITestBase
{
    public LoadGameTests(StrideGameFixture fixture) : base(fixture) { }
    
    [Fact]
    public void LoadGame_WithSaves_ShowsSaveList()
    {
        // Arrange
        var mainMenu = new MainMenuPage(Context);
        mainMenu.WaitForPageReady();
        
        // Skip if no saves
        if (!mainMenu.HasSaves)
        {
            return; // Or use Skip.If from xUnit
        }
        
        // Act
        mainMenu.NavigateToLoadGame();
        
        var loadGame = new LoadGamePage(Context);
        loadGame.WaitForPageReady();
        
        // Assert
        loadGame.AssertDisplayed();
        loadGame.SaveList.AssertVisible();
        Assert.True(loadGame.SaveCount > 0, "Expected at least one save");
    }
    
    [Fact]
    public void LoadGame_SelectSave_EnablesLoad()
    {
        // Arrange
        var mainMenu = new MainMenuPage(Context);
        mainMenu.WaitForPageReady();
        
        if (!mainMenu.HasSaves)
            return;
        
        mainMenu.NavigateToLoadGame();
        
        var loadGame = new LoadGamePage(Context);
        loadGame.WaitForPageReady();
        
        // Act
        loadGame.SelectSave(0);
        
        // Assert
        loadGame.LoadButton.AssertEnabled();
    }
    
    [Fact]
    public void LoadGame_Back_ReturnsToMainMenu()
    {
        // Arrange
        var mainMenu = new MainMenuPage(Context);
        mainMenu.WaitForPageReady();
        mainMenu.NavigateToLoadGame();
        
        var loadGame = new LoadGamePage(Context);
        loadGame.WaitForPageReady();
        
        // Act
        loadGame.NavigateBack();
        
        // Assert
        mainMenu.WaitForPageReady();
        mainMenu.AssertDisplayed();
    }
}
```

### 6.2.5 In-Game UI Tests

```csharp
/// <summary>
/// Tests for in-game UI elements.
/// </summary>
[Trait("Category", "UI.InGame")]
public class InGameUITests : StrideUITestBase
{
    public InGameUITests(StrideGameFixture fixture) : base(fixture) { }
    
    protected override async Task NavigateToStartStateAsync()
    {
        // Need to be in-game for these tests
        // This would start a new game or load a test save
        await StartTestGameAsync();
    }
    
    private async Task StartTestGameAsync()
    {
        var mainMenu = new MainMenuPage(Context);
        
        if (!mainMenu.IsDisplayed())
            return; // Might already be in game
        
        mainMenu.WaitForPageReady();
        mainMenu.NavigateToNewGame();
        
        var nameInput = new NameInputPage(Context);
        nameInput.WaitForPageReady();
        nameInput.StartNewGame("UITestHero");
        
        // Wait for loading
        var loading = new LoadingScreenPage(Context);
        if (loading.IsDisplayed())
        {
            loading.WaitForLoadingComplete(30000);
        }
        
        // Wait for game UI
        var inGame = new InGameUIPage(Context);
        inGame.WaitForPageReady(10000);
    }
    
    [Fact]
    public void InGame_PressEscape_ShowsPauseMenu()
    {
        // Arrange
        var inGame = new InGameUIPage(Context);
        inGame.CheckDisplayed();
        
        // Act
        inGame.OpenPauseMenu();
        
        // Assert
        var pauseMenu = new PauseMenuPage(Context);
        pauseMenu.WaitForPageReady();
        pauseMenu.AssertDisplayed();
        
        // Cleanup - close pause menu
        pauseMenu.Resume();
    }
    
    [Fact]
    public void PauseMenu_Resume_ReturnsToGame()
    {
        // Arrange
        var inGame = new InGameUIPage(Context);
        inGame.OpenPauseMenu();
        
        var pauseMenu = new PauseMenuPage(Context);
        pauseMenu.WaitForPageReady();
        
        // Act
        pauseMenu.Resume();
        
        // Assert
        inGame.WaitForPageReady();
        inGame.AssertDisplayed();
        pauseMenu.AssertNotDisplayed();
    }
    
    [Fact]
    public void InGame_ToggleDebug_ShowsFPS()
    {
        // Arrange
        var inGame = new InGameUIPage(Context);
        inGame.CheckDisplayed();
        
        // Act
        inGame.ToggleDebugOverlay();
        Thread.Sleep(100);
        
        // Assert
        inGame.FpsCounter.WaitVisible(true, 2000);
        
        // Cleanup
        inGame.ToggleDebugOverlay();
    }
}
```

---

## 6.3 Test Categories and Traits

### 6.3.1 Category Definitions

| Category | Description | Run When |
|----------|-------------|----------|
| `Smoke` | Critical path tests | Every build |
| `UI.MainMenu` | Main menu tests | UI test runs |
| `UI.NewGame` | New game flow tests | UI test runs |
| `UI.LoadGame` | Load game tests | UI test runs |
| `UI.InGame` | In-game UI tests | UI test runs |
| `UI.Navigation` | Navigation between screens | UI test runs |
| `Visual` | Visual regression tests | Nightly |
| `Performance` | Performance-related UI tests | Weekly |

### 6.3.2 Filtering Tests

```bash
# Run only smoke tests
dotnet test --filter "Category=Smoke"

# Run all UI tests
dotnet test --filter "Category~UI"

# Run specific category
dotnet test --filter "Category=UI.MainMenu"

# Exclude slow tests
dotnet test --filter "Category!=Visual&Category!=Performance"
```

---

## 6.4 Wait Patterns

### 6.4.1 Explicit Waits

```csharp
// Wait for specific condition
Context.WaitFor(() => button.IsEnabled(), 5000, "button enabled");

// Wait for page
page.WaitForPageReady();

// Wait for element
control.WaitVisible(true, 3000);
```

### 6.4.2 Implicit Waits (Built-in)

All `Check*` and `Assert*` methods include implicit waits:

```csharp
// These wait before asserting
control.CheckVisible();     // Waits up to DefaultTimeoutMs
control.AssertEnabled();    // Waits up to DefaultTimeoutMs
```

### 6.4.3 Transition Waits

```csharp
// Wait for page transition
mainMenu.NavigateToNewGame();
var nameInput = new NameInputPage(Context);
nameInput.WaitForPageReady();  // Waits for animation/transition
```

---

## 6.5 Error Handling

### 6.5.1 Screenshot on Failure

```csharp
public class TestWithScreenshot : StrideUITestBase
{
    public TestWithScreenshot(StrideGameFixture fixture) : base(fixture) { }
    
    [Fact]
    public void SomeTest()
    {
        try
        {
            // Test code
            var page = new SomePage(Context);
            page.SomeButton.Click();
            page.OtherControl.AssertVisible();
        }
        catch (Exception ex)
        {
            CaptureFailureScreenshot(nameof(SomeTest));
            throw;
        }
    }
}
```

### 6.5.2 Automatic Screenshot via xUnit

```csharp
/// <summary>
/// Attribute to capture screenshot on test failure.
/// </summary>
public class CaptureScreenshotOnFailureAttribute : BeforeAfterTestAttribute
{
    public override void After(MethodInfo methodUnderTest)
    {
        // Check if test failed and capture screenshot
        // This requires integration with xUnit's test result reporting
    }
}
```

---

## 6.6 Test Data Management

### 6.6.1 Test Save Files

```csharp
/// <summary>
/// Fixture that creates a test save file.
/// </summary>
public class TestSaveFixture : IAsyncLifetime
{
    private const string TestSaveName = "UITestSave";
    
    public async Task InitializeAsync()
    {
        // Create a test save file with known state
        await CreateTestSaveAsync();
    }
    
    public async Task DisposeAsync()
    {
        // Clean up test save
        await DeleteTestSaveAsync();
    }
    
    private async Task CreateTestSaveAsync()
    {
        // Copy pre-created test save to save directory
        var testDataDir = Path.Combine(AppContext.BaseDirectory, "TestData", "TestSaveFiles");
        var saveDir = GetSaveDirectory();
        
        var sourceFile = Path.Combine(testDataDir, "test-save.json");
        var destFile = Path.Combine(saveDir, $"{TestSaveName}.json");
        
        File.Copy(sourceFile, destFile, overwrite: true);
    }
}
```

### 6.6.2 Data-Driven Tests

```csharp
[Theory]
[InlineData("Hero")]
[InlineData("Test Character")]
[InlineData("Player123")]
public void NameInput_ValidNames_AllAccepted(string name)
{
    var mainMenu = new MainMenuPage(Context);
    mainMenu.WaitForPageReady();
    mainMenu.NavigateToNewGame();
    
    var nameInput = new NameInputPage(Context);
    nameInput.WaitForPageReady();
    nameInput.NameInput.ClearAndEnter(name);
    
    nameInput.StartButton.AssertEnabled($"Name '{name}' should be valid");
    
    // Cancel to reset for next iteration
    nameInput.Cancel();
}
```

---

## 6.7 Best Practices

### 6.7.1 Test Independence

Each test should be able to run independently:

```csharp
// Good: Navigate to required state
[Fact]
public void GoodTest()
{
    NavigateToMainMenu();  // Ensure known state
    var mainMenu = new MainMenuPage(Context);
    // ... test
}

// Bad: Assumes previous test state
[Fact]
public void BadTest()
{
    // Assumes we're already on some page
    var page = new SomePage(Context);
    // This will fail if previous test left us elsewhere
}
```

### 6.7.2 Minimal Assertions

Test one thing per test:

```csharp
// Good: Single focus
[Fact]
public void NewGameButton_IsEnabled()
{
    var mainMenu = new MainMenuPage(Context);
    mainMenu.WaitForPageReady();
    mainMenu.NewGameButton.AssertEnabled();
}

// Bad: Too many assertions
[Fact]
public void MainMenu_Everything()
{
    var mainMenu = new MainMenuPage(Context);
    mainMenu.NewGameButton.AssertEnabled();
    mainMenu.LoadGameButton.AssertEnabled();
    mainMenu.TitleText.AssertTextEquals("ORAVEY");
    // ... many more assertions
}
```

### 6.7.3 Descriptive Names

```csharp
// Good: Clear what is tested
[Fact]
public void MainMenu_NewGameButton_NavigatesToNameInput()

// Bad: Vague
[Fact]
public void Test1()
```

### 6.7.4 Clean Up After Tests

```csharp
[Fact]
public void TestWithCleanup()
{
    try
    {
        // Arrange
        var mainMenu = new MainMenuPage(Context);
        mainMenu.NavigateToNewGame();
        
        // Act & Assert
        var nameInput = new NameInputPage(Context);
        nameInput.AssertDisplayed();
    }
    finally
    {
        // Always return to main menu
        NavigateToMainMenu();
    }
}
```

---

*Document Version: 1.0*  
*Last Updated: January 2025*
