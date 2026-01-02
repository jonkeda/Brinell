# Brinell VS Code Extensions Testing Plan

## Overview

Add `Brinell.VsCode` as a new platform package for automated testing of Visual Studio Code extensions using the VS Code Extension Test Runner and Playwright for UI automation.

**Why VS Code Extensions?** VS Code is the most popular code editor with a thriving extension ecosystem. Organizations build internal extensions for development workflows, linting, code generation, and custom tooling. Testing these extensions requires specialized UI automation that understands the VS Code command palette, editor, sidebar, and webview components.

---

## Architecture Decision

### VS Code Testing Approaches

| Approach | Scope | Complexity | Use Case |
|----------|-------|-----------|----------|
| VS Code Test Runner (xUnit) | Unit tests in extension | Low | Testing extension logic |
| Playwright + Webdriver | UI automation | Medium | Testing webviews, UI panels |
| **Brinell.VsCode (Recommended)** | Full UI + extension integration | Medium-High | End-to-end extension testing |
| Custom browser automation | Custom scripts | High | Limited reusability |

### Architecture Decision

Create `Brinell.VsCode` as a new platform extending `Brinell.Core`:
- Use **Playwright** for browser automation (VS Code webviews are Chromium-based)
- Leverage VS Code **Extension Test Runner** for extension lifecycle
- Provide abstraction layer for VS Code-specific concepts (Command Palette, Editor, Sidebar, Themes)
- Follow identical test patterns as other Brinell platforms
- Support both single-window and multi-window testing scenarios

**Why Playwright?**
- VS Code's webviews are Chromium-based (Playwright native support)
- Can interact with main VS Code UI and webviews simultaneously
- Better performance than Selenium for modern web apps
- Active development and strong community support

---

## Package Structure

```
src/Brinell.VsCode/
├── Brinell.VsCode.csproj
├── Controls/                           # VS Code UI component wrappers
│   ├── Base/
│   │   └── VsCodeControlBase.cs        # Base for all controls
│   ├── CommandPaletteControl.cs        # Cmd+Shift+P / Ctrl+Shift+P
│   ├── EditorControl.cs                # Code editor with tabs
│   ├── FileExplorerControl.cs          # Sidebar file tree
│   ├── SearchControl.cs                # Find/Replace panel
│   ├── TerminalControl.cs              # Integrated terminal
│   ├── SidebarControl.cs               # Activity bar + panels
│   ├── StatusBarControl.cs             # Bottom status bar
│   ├── ProblemsPanelControl.cs         # Diagnostics panel
│   ├── DebugConsoleControl.cs          # Debug output
│   ├── WebviewControl.cs               # Webview container (key!)
│   ├── NotificationControl.cs          # Toast notifications
│   ├── InputBoxControl.cs              # Input prompts
│   ├── QuickPickControl.cs             # Selection dialogs
│   └── SettingsControl.cs              # Settings UI
├── Infrastructure/
│   ├── PlaywrightVsCodeDriver.cs       # Playwright wrapper for VS Code
│   ├── VsCodeExtensionContext.cs       # Extension-specific context
│   ├── CommandExecutor.cs              # Execute VS Code commands
│   ├── ExtensionActivationWaiter.cs    # Wait for extension activation
│   └── WebviewInteractionHelper.cs     # Webview-specific helpers
├── Testing/
│   ├── VsCodeUITestBase.cs             # Base for VS Code tests
│   └── VsCodeTestContext.cs            # Context for VS Code tests
└── Extensions/
    ├── VsCodeWaitExtensions.cs         # Wait for specific VS Code events
    └── CommandPaletteExtensions.cs     # Command palette helpers
```

---

## VS Code-Specific Concepts

### 1. Command Palette
The command palette (Cmd+Shift+P) is the primary interaction method:

```csharp
// Open command palette and execute command
var palette = Context.GetCommandPalette();
await palette.OpenAsync();
await palette.TypeAsync("Git: Push");
await palette.PressEnterAsync();
```

### 2. Webviews
Extensions often render custom UI in webviews (Chromium iframes):

```csharp
var webviewControl = FindControl<WebviewControl>("myExtensionWebview");
var button = webviewControl.FindElement("button-id");
await button.ClickAsync();
```

### 3. Editor Groups
VS Code supports multiple editor groups (split view):

```csharp
var editor = FindControl<EditorControl>("editor-1");
editor.OpenFile("path/to/file.ts");
var selection = editor.GetSelectedText();
```

### 4. Sidebar Panels
Sidebar contains Explorer, Search, Source Control, Debug, Extensions:

```csharp
var sidebar = FindControl<SidebarControl>("sidebar");
await sidebar.ClickPanelAsync("explorer");
var fileTree = sidebar.GetFileExplorer();
```

### 5. Status Bar
Bottom status bar shows git branch, language mode, cursor position:

```csharp
var statusBar = FindControl<StatusBarControl>("statusBar");
var gitBranch = statusBar.GetGitBranch();  // e.g., "main"
var language = statusBar.GetLanguageMode();  // e.g., "TypeScript"
```

### 6. Notifications
VS Code shows toast notifications for status/errors:

```csharp
var notification = WaitForNotification("Task completed", TimeSpan.FromSeconds(5));
notification.Click();  // Click on notification
```

### 7. Quick Open / Quick Pick
Dialogs for file selection or option choosing:

```csharp
var quickPick = FindControl<QuickPickControl>("quickPick");
await quickPick.SelectAsync("Option 1");
```

---

## Implementation Phases

### Phase 1: Project Setup & Infrastructure (2 days)
- [ ] Create `Brinell.VsCode.csproj`
- [ ] Setup Playwright dependency and configuration
- [ ] Create `PlaywrightVsCodeDriver` wrapper
- [ ] Implement `VsCodeUITestBase` test base class
- [ ] Create `VsCodeTestContext` with extension awareness
- [ ] Add VS Code launch configuration

### Phase 2: Core Controls (2 days)
- [ ] Implement `CommandPaletteControl`
- [ ] Implement `EditorControl` with tab/file support
- [ ] Implement `SidebarControl` with panel switching
- [ ] Implement `FileExplorerControl` for tree navigation
- [ ] Implement `SearchControl` for Find/Replace
- [ ] Add command execution helpers

### Phase 3: Advanced Controls (1.5 days)
- [ ] Implement `WebviewControl` for extension UI
- [ ] Implement `TerminalControl` for terminal interaction
- [ ] Implement `StatusBarControl` for status display
- [ ] Implement `NotificationControl` for toast handling
- [ ] Implement `InputBoxControl` and `QuickPickControl` for dialogs

### Phase 4: Extension Testing Helpers (1.5 days)
- [ ] Create `ExtensionActivationWaiter` service
- [ ] Create `WebviewInteractionHelper` for webview testing
- [ ] Create `CommandExecutor` with command discovery
- [ ] Add settings UI testing support
- [ ] Add debugging integration

### Phase 5: Sample Extension Project (2 days)
- [ ] Create `Brinell.Samples.VsCode.Extension` (TypeScript/JavaScript)
- [ ] Implement example features (webview, commands, sidebar panel)
- [ ] Create `Brinell.Samples.VsCode.UITests` (.NET test project)
- [ ] Write 15+ sample tests

### Phase 6: Documentation (1.5 days)
- [ ] Create `docs/platform-guides/vscode.md`
- [ ] Document all controls and their usage
- [ ] Create sample test walkthroughs
- [ ] Add extension development guidelines for testability
- [ ] Create troubleshooting guide for common issues

**Total: ~10.5 days**

---

## Project Dependencies

### Brinell.VsCode.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>
    <RootNamespace>Brinell.VsCode</RootNamespace>
    <Description>VS Code extension UI testing support using Playwright. Part of the Brinell UI testing framework.</Description>
    <PackageId>Brinell.VsCode</PackageId>
    <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Playwright" />
    <PackageReference Include="Microsoft.Playwright.NUnit" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Brinell.Core\Brinell.Core.csproj" />
  </ItemGroup>
  
  <ItemGroup>
    <None Include="..\..\README.md" Pack="true" PackagePath="" />
  </ItemGroup>

</Project>
```

### Test Project Setup
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Brinell.VsCode\Brinell.VsCode.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="Microsoft.Playwright" />
  </ItemGroup>
</Project>
```

---

## Sample Extension Project Structure

### Extension Application (TypeScript)
```
samples/Brinell.Samples.VsCode.Extension/
├── src/
│   ├── extension.ts                    # Main extension entry
│   ├── extension.test.ts               # Unit tests
│   ├── commands/
│   │   ├── helloWorld.ts
│   │   ├── showWebview.ts
│   │   └── codeGeneration.ts
│   ├── panels/
│   │   └── TasksPanel.ts              # Custom sidebar panel
│   ├── providers/
│   │   ├── CodeLensProvider.ts
│   │   └── CompletionProvider.ts
│   └── webviews/
│       ├── taskList/
│       │   ├── index.html
│       │   ├── index.css
│       │   └── index.ts
│       └── codePreview/
│           ├── index.html
│           ├── index.css
│           └── index.ts
├── .vscode/
│   ├── launch.json
│   └── settings.json
├── package.json
├── tsconfig.json
└── webpack.config.js
```

### Test Project (.NET)
```
samples/Brinell.Samples.VsCode.UITests/
├── TestBase/
│   └── VsCodeSampleTestBase.cs
├── PageObjects/
│   ├── CommandPalettePage.cs
│   ├── WebviewPage.cs
│   ├── TasksPanelPage.cs
│   ├── EditorPage.cs
│   └── ExplorerPage.cs
└── Tests/
    ├── CommandExecutionTests.cs
    ├── WebviewInteractionTests.cs
    ├── CodeGenerationTests.cs
    ├── SidebarPanelTests.cs
    ├── NotificationTests.cs
    ├── EditorIntegrationTests.cs
    └── MultiWindowTests.cs
```

---

## Test Examples

### CommandExecutionTests.cs
```csharp
[UITest]
public class CommandExecutionTests : VsCodeSampleTestBase
{
    [Fact]
    public async Task ExecuteHelloWorldCommand_ShowsNotification()
    {
        // Arrange
        await InitializeVsCodeAsync();
        var commandPalette = Context.GetCommandPalette();
        
        // Act
        await commandPalette.OpenAsync();
        await commandPalette.TypeAsync("Sample: Hello World");
        await commandPalette.PressEnterAsync();
        
        // Assert
        var notification = WaitForNotification("Hello from Sample Extension!", 
            TimeSpan.FromSeconds(3));
        Assert.NotNull(notification);
    }
    
    [Fact]
    public async Task ExecuteUnknownCommand_DoesNothing()
    {
        await InitializeVsCodeAsync();
        var palette = Context.GetCommandPalette();
        
        await palette.OpenAsync();
        await palette.TypeAsync("NonExistent: Command");
        
        // No match should show
        Assert.False(await palette.HasResultAsync());
    }
}
```

### WebviewInteractionTests.cs
```csharp
[UITest]
public class WebviewInteractionTests : VsCodeSampleTestBase
{
    [Fact]
    public async Task WebviewPanel_ClickButton_UpdatesUI()
    {
        // Arrange
        await InitializeVsCodeAsync();
        var palette = Context.GetCommandPalette();
        
        // Open webview via command
        await palette.ExecuteCommandAsync("Sample: Show Task List");
        
        var webview = FindControl<WebviewControl>("sample-tasks");
        await webview.WaitForLoadAsync(TimeSpan.FromSeconds(5));
        
        // Act
        var addButton = webview.FindElement("add-task-button");
        await addButton.ClickAsync();
        
        var input = webview.FindElement("task-input");
        await input.FillAsync("New Task");
        
        var confirmButton = webview.FindElement("confirm-button");
        await confirmButton.ClickAsync();
        
        // Assert
        var taskList = webview.FindElements("task-item");
        Assert.NotEmpty(taskList);
    }
    
    [Fact]
    public async Task WebviewPanel_SendsMessageToExtension()
    {
        await InitializeVsCodeAsync();
        await Context.ExecuteCommandAsync("Sample: Show Task List");
        
        var webview = FindControl<WebviewControl>("sample-tasks");
        await webview.WaitForLoadAsync();
        
        // Send message from webview to extension
        var result = await webview.PostMessageAsync(new 
        { 
            command = "getTasks",
            filter = "completed"
        });
        
        Assert.NotNull(result);
    }
}
```

### CodeGenerationTests.cs
```csharp
[UITest]
public class CodeGenerationTests : VsCodeSampleTestBase
{
    [Fact]
    public async Task GenerateCode_InsertsTextAtCursor()
    {
        // Arrange
        await InitializeVsCodeAsync();
        var editor = FindControl<EditorControl>("editor-1");
        
        // Open a TypeScript file
        await Context.ExecuteCommandAsync("File: Open File");
        // ... select file ...
        
        // Act - position cursor and run code generation
        editor.SetCursorLine(5);
        var palette = Context.GetCommandPalette();
        await palette.ExecuteCommandAsync("Sample: Generate Interface");
        
        // Assert
        var content = editor.GetContent();
        Assert.Contains("interface", content);
    }
    
    [Fact]
    public async Task CodeLens_ShowsQuickAction()
    {
        await InitializeVsCodeAsync();
        var editor = FindControl<EditorControl>("editor-1");
        
        editor.OpenFile("sample.ts");
        
        // Hover over code to show code lens
        var codeLens = editor.GetCodeLensAt(line: 10);
        Assert.NotNull(codeLens);
        Assert.Contains("Generate", codeLens.Title);
        
        // Click code lens action
        await codeLens.ClickAsync();
        
        var content = editor.GetContent();
        Assert.Contains("generated", content);
    }
}
```

### SidebarPanelTests.cs
```csharp
[UITest]
public class SidebarPanelTests : VsCodeSampleTestBase
{
    [Fact]
    public async Task TasksPanel_DisplaysInSidebar()
    {
        // Arrange
        await InitializeVsCodeAsync();
        var sidebar = FindControl<SidebarControl>("sidebar");
        
        // Act - click on custom activity icon
        await sidebar.ClickActivityIconAsync("tasks");
        
        var taskPanel = sidebar.GetPanel("tasks");
        
        // Assert
        Assert.True(await taskPanel.IsVisibleAsync());
        var tasks = taskPanel.FindElements("task-item");
        Assert.NotEmpty(tasks);
    }
    
    [Fact]
    public async Task SwitchPanels_SwitchesActivePanel()
    {
        await InitializeVsCodeAsync();
        var sidebar = FindControl<SidebarControl>("sidebar");
        
        // Switch to Explorer
        await sidebar.ClickActivityIconAsync("explorer");
        Assert.True(await sidebar.GetPanel("explorer").IsVisibleAsync());
        
        // Switch to Tasks (custom)
        await sidebar.ClickActivityIconAsync("tasks");
        Assert.True(await sidebar.GetPanel("tasks").IsVisibleAsync());
        
        // Explorer should be hidden
        Assert.False(await sidebar.GetPanel("explorer").IsVisibleAsync());
    }
}
```

### EditorIntegrationTests.cs
```csharp
[UITest]
public class EditorIntegrationTests : VsCodeSampleTestBase
{
    [Fact]
    public async Task DiagnosticsPanel_ShowsErrors()
    {
        // Arrange
        await InitializeVsCodeAsync();
        var editor = FindControl<EditorControl>("editor-1");
        
        // Open a file with errors
        editor.OpenFile("bad-code.ts");
        
        // Wait for diagnostics
        await editor.WaitForDiagnosticsAsync(TimeSpan.FromSeconds(3));
        
        // Assert
        var problems = FindControl<ProblemsPanelControl>("problems");
        var errors = problems.GetDiagnosticsOfType("error");
        
        Assert.NotEmpty(errors);
        Assert.True(errors.Any(e => e.Message.Contains("is not defined")));
    }
    
    [Fact]
    public async Task EditorSelection_CanBeQueried()
    {
        await InitializeVsCodeAsync();
        var editor = FindControl<EditorControl>("editor-1");
        
        editor.OpenFile("sample.ts");
        editor.SetCursorPosition(line: 5, column: 10);
        editor.SelectText(fromLine: 5, fromColumn: 10, toLine: 5, toColumn: 20);
        
        var selected = editor.GetSelectedText();
        Assert.NotEmpty(selected);
    }
}
```

### MultiWindowTests.cs
```csharp
[UITest]
public class MultiWindowTests : VsCodeSampleTestBase
{
    [Fact]
    public async Task OpenSecondWindow_CanTestBothWindows()
    {
        // Arrange
        await InitializeVsCodeAsync();
        
        // Act - open new window
        var palette = Context.GetCommandPalette();
        await palette.ExecuteCommandAsync("Workbench: New Window");
        
        // Get both window contexts
        var window1 = Context;  // Original
        var window2 = await Context.GetOtherWindowAsync();  // New window
        
        // Assert
        Assert.NotNull(window2);
        Assert.NotEqual(window1.ProcessId, window2.ProcessId);
    }
}
```

---

## Page Object Examples

### CommandPalettePage.cs
```csharp
public class CommandPalettePage : PageBase
{
    private readonly IPage _page;
    
    public CommandPalettePage(VsCodeTestContext context, IPage page) 
        : base(context)
    {
        _page = page;
    }
    
    public async Task OpenAsync()
    {
        // Ctrl+Shift+P on Windows/Linux, Cmd+Shift+P on macOS
        await _page.Keyboard.PressAsync("Control+Shift+P");
        await WaitForPaletteVisibleAsync();
    }
    
    public async Task TypeAsync(string command)
    {
        var input = _page.Locator("[placeholder='> Command Palette']");
        await input.FillAsync(command);
    }
    
    public async Task PressEnterAsync()
        => await _page.Keyboard.PressAsync("Enter");
    
    public async Task ExecuteCommandAsync(string command)
    {
        await OpenAsync();
        await TypeAsync(command);
        await PressEnterAsync();
    }
    
    public async Task<bool> HasResultAsync()
    {
        var results = _page.Locator("[role='option']");
        return await results.CountAsync() > 0;
    }
    
    private async Task WaitForPaletteVisibleAsync()
        => await _page.WaitForSelectorAsync(".monaco-inputbox");
}
```

### WebviewPage.cs
```csharp
public class WebviewPage : PageBase
{
    private readonly IFrame _webviewFrame;
    
    public WebviewPage(VsCodeTestContext context, IFrame webviewFrame) 
        : base(context)
    {
        _webviewFrame = webviewFrame;
    }
    
    public async Task WaitForLoadAsync(TimeSpan timeout)
    {
        await _webviewFrame.WaitForLoadStateAsync(LoadState.NetworkIdle, 
            new() { Timeout = (float)timeout.TotalMilliseconds });
    }
    
    public ILocator FindElement(string selector)
        => _webviewFrame.Locator(selector);
    
    public IReadOnlyList<ILocator> FindElements(string selector)
        => _webviewFrame.Locator(selector).All;
    
    public async Task<object?> PostMessageAsync(dynamic message)
    {
        return await _webviewFrame.EvaluateAsync<object?>(
            $"window.vscode.postMessage({JsonConvert.SerializeObject(message)})");
    }
    
    public async Task<string> GetTitleAsync()
        => await _webviewFrame.Locator("h1").TextContentAsync() ?? "";
}
```

### EditorPage.cs
```csharp
public class EditorPage : PageBase
{
    private readonly IPage _page;
    
    public EditorPage(VsCodeTestContext context, IPage page) : base(context) 
    {
        _page = page;
    }
    
    public void OpenFile(string filePath)
    {
        // Implementation would use command palette
        // or file explorer interaction
    }
    
    public string GetContent()
    {
        // Get editor content via VS Code API
        return "";  // Placeholder
    }
    
    public void SetCursorLine(int line)
    {
        // Position cursor at specific line
    }
    
    public void SetCursorPosition(int line, int column)
    {
        // Position cursor at specific line:column
    }
    
    public void SelectText(int fromLine, int fromColumn, int toLine, int toColumn)
    {
        // Select text range
    }
    
    public string GetSelectedText()
    {
        // Get currently selected text
        return "";  // Placeholder
    }
    
    public async Task WaitForDiagnosticsAsync(TimeSpan timeout)
    {
        // Wait for error/warning diagnostics to appear
    }
    
    public CodeLens? GetCodeLensAt(int line)
    {
        // Get code lens at specific line
        return null;  // Placeholder
    }
}
```

---

## VS Code-Specific Challenges & Solutions

| Challenge | Cause | Solution |
|-----------|-------|----------|
| Webview iframe isolation | Webviews run in isolated iframes | Use Playwright frame API with proper waits |
| Extension activation timing | Extensions may take time to load | Use `ExtensionActivationWaiter` service |
| Command palette flakiness | UI state changes rapidly | Add explicit visibility waits, retry logic |
| Multiple editor groups | Split editor support | Track editor group selectors, use group IDs |
| Terminal output capture | Terminal is complex pseudo-terminal | Use vscode.workspace API for structured data |
| Settings persistence | Some settings are user-specific | Use test profile or reset settings before test |
| Dark/Light theme variations | UI changes based on theme | Use theme-independent selectors |
| Performance: Large files | Opening 100MB+ files is slow | Use small test files, mock large data |

---

## Benefits Over Manual Testing

| Aspect | Manual | Brinell.VsCode |
|--------|--------|-----------------|
| Regression detection | Slow, error-prone | Automated, repeatable |
| Multi-window testing | Difficult | Supported out-of-box |
| Webview interaction | Complex | Abstracted controls |
| CI/CD integration | Limited | Full pipeline support |
| Performance tracking | Manual | Automated metrics |
| Configuration testing | Time-consuming | Parametrized |

---

## Success Criteria

- [ ] `Brinell.VsCode` package compiles and publishes
- [ ] Playwright integration works with VS Code
- [ ] All core controls implemented (Command Palette, Editor, Sidebar, Webview)
- [ ] Sample extension builds and installs
- [ ] 15+ UI tests passing reliably
- [ ] Webview interaction tested and working
- [ ] Multi-window testing supported
- [ ] Command execution helpers working
- [ ] Notification detection reliable
- [ ] Documentation complete with examples
- [ ] CI/CD pipeline integration tested
- [ ] Package published to NuGet

---

## Integration with Existing Platforms

### Platform Compatibility Matrix
```
Brinell.Core ← Base abstractions
├── Brinell.Html (Selenium) ← Web apps
├── Brinell.Wpf (FlaUI) ← WPF apps
├── Brinell.WinForms (FlaUI) ← WinForms apps
├── Brinell.VsCode (Playwright) ← NEW: VS Code extensions
├── Brinell.Blazor (Selenium) ← Blazor apps
├── Brinell.Maui (Appium) ← Mobile/Maui apps
└── Brinell.Stride (Custom) ← Game testing
```

All platforms inherit from `UITestBase<TContext>` providing:
- Consistent test structure
- Logging infrastructure
- CSV test reports
- Page object patterns
- Wait strategies

---

## Documentation Structure

Create `docs/platform-guides/vscode.md`:

1. **Getting Started**
   - Installing Brinell.VsCode NuGet
   - Setting up VS Code test environment
   - Extension project requirements for testability

2. **Core Concepts**
   - Extension lifecycle management
   - Activity bar and sidebar panels
   - Command palette interaction
   - Webview communication

3. **Controls Reference**
   - CommandPaletteControl
   - EditorControl with multi-group support
   - SidebarControl with panel switching
   - WebviewControl with frame handling
   - TerminalControl, StatusBarControl, etc.

4. **Advanced Topics**
   - Testing custom sidebar panels
   - Webview message protocol
   - Debugging extension code
   - Code lens and decorations
   - Multi-window scenarios

5. **Best Practices**
   - Making extensions testable
   - Using vs code.window API for testing
   - Handling async activation
   - Performance considerations

6. **Troubleshooting**
   - Common Playwright issues
   - Extension not loading
   - Webview frame not found
   - Selector maintenance

---

## Sample Extension Features

### 1. Hello World Command
Simple command that shows a notification

### 2. Task List Webview
Custom webview panel showing task list with add/delete operations

### 3. Code Generation
CodeLens action that generates boilerplate code

### 4. Sidebar Panel
Custom panel in sidebar with real-time data

### 5. Search Integration
Integration with VS Code search panel

### 6. Settings UI
Custom settings panel for extension options

---

## Technology Stack

**Core**
- .NET 8/9/10
- C# 11+
- xUnit for tests

**UI Automation**
- Playwright (browser automation)
- VS Code Extension Test Runner
- Chromium browser (embedded in VS Code)

**Logging & Reporting**
- Serilog
- CSV test reports (inherited from Brinell.Core)

**Sample Extension**
- TypeScript / JavaScript
- VS Code API
- Webpack

---

## Timeline & Dependencies

**Start date:** After WinForms/Stride completion
**Duration:** ~10.5 days
**Dependencies:** 
- Brinell.Core (existing)
- Playwright (via NuGet)
- VS Code SDK (for sample extension)
- .NET 8/9/10 SDK

**Blocking:** None - can start independently

---

## Unique Value Proposition

Brinell.VsCode fills a gap in the VS Code extension testing ecosystem:
- **Existing tools**: Unit test runners (don't test UI), manual testing (slow)
- **Brinell.VsCode**: Full UI automation with extension awareness
- **Consistency**: Same patterns as WPF, WinForms, Blazor testing
- **Enterprise ready**: Logging, reporting, CI/CD integration

---

## References

- [VS Code Extension API](https://code.visualstudio.com/api)
- [VS Code Test Runner](https://github.com/Microsoft/vscode-test)
- [Playwright Documentation](https://playwright.dev)
- [VS Code Extension Examples](https://github.com/microsoft/vscode-extension-samples)
- [Webview Testing Patterns](https://code.visualstudio.com/api/extension-guides/webview)

