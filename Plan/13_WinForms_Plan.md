# Brinell WinForms Platform Plan

## Overview

Add `Brinell.WinForms` as a new platform package for testing Windows Forms applications using FlaUI (Windows UI Automation).

**Why WinForms?** WinForms is a mature, widely-used desktop framework for enterprise applications. Many organizations still maintain legacy WinForms applications that need UI testing. By leveraging the same FlaUI infrastructure as WPF, we can provide seamless testing support.

---

## Architecture Decision

### Comparison: WinForms vs WPF
| Aspect | WinForms | WPF |
|--------|----------|-----|
| UI Automation | FlaUI (UI Automation) ✓ | FlaUI (UI Automation) ✓ |
| Control Set | Standard .NET controls | Rich customizable controls |
| Technology Age | Mature (1+ decades) | Modern (1+ decade) |
| Enterprise Usage | High (legacy systems) | Growing |
| Testing Complexity | Lower (simpler controls) | Higher (custom behaviors) |

### Decision
Create `Brinell.WinForms` as parallel to `Brinell.Wpf`:
- Reuse FlaUI infrastructure from `Brinell.Wpf`
- Extend `Brinell.Core.Testing` abstractions
- Follow identical patterns for consistency
- Share UI test patterns and best practices

---

## Package Structure

```
src/Brinell.WinForms/
├── Brinell.WinForms.csproj
├── Controls/                           # WinForms-specific control wrappers
│   ├── Base/
│   │   └── WinFormsControlBase.cs      # Base for all controls
│   ├── TextBoxControl.cs               # System.Windows.Forms.TextBox
│   ├── ButtonControl.cs                # System.Windows.Forms.Button
│   ├── CheckBoxControl.cs              # System.Windows.Forms.CheckBox
│   ├── RadioButtonControl.cs           # System.Windows.Forms.RadioButton
│   ├── ComboBoxControl.cs              # System.Windows.Forms.ComboBox
│   ├── ListBoxControl.cs               # System.Windows.Forms.ListBox
│   ├── DataGridViewControl.cs          # System.Windows.Forms.DataGridView
│   ├── TreeViewControl.cs              # System.Windows.Forms.TreeView
│   ├── LabelControl.cs                 # System.Windows.Forms.Label
│   ├── PictureBoxControl.cs            # System.Windows.Forms.PictureBox
│   ├── TabControlControl.cs            # System.Windows.Forms.TabControl
│   ├── MenuItemControl.cs              # System.Windows.Forms.MenuItem
│   ├── ProgressBarControl.cs           # System.Windows.Forms.ProgressBar
│   └── MessageBoxDialog.cs             # System.Windows.Forms.MessageBox
├── Infrastructure/
│   ├── FlaUIDriverAdapter.cs           # Reuse from WPF
│   └── WinFormsUITestContext.cs        # WinForms-specific context
├── Testing/
│   ├── WinFormsUITestBase.cs           # Base for WinForms tests
│   └── FlaUITestContext.cs             # Reuse from WPF
└── Extensions/
    └── WinFormsWaitExtensions.cs       # Wait helpers specific to WinForms
```

---

## Key Differences from WPF

### Control Access Patterns
**WPF (XAML-based):**
```csharp
var button = FindControl<ButtonControl>("MyButton");  // Name from XAML
```

**WinForms (Designer or Code-based):**
```csharp
var button = FindControl<ButtonControl>("btnSubmit");  // Name property
```

### Control Properties
**WinForms advantages:**
- Simpler control hierarchy (no visual tree complexity)
- Direct property access (Text, Visible, Enabled)
- No data binding complexity

**WinForms challenges:**
- Legacy naming conventions (inconsistent)
- No automation ID support by default
- Nested modal forms behavior

### Modal Forms
WinForms uses ShowDialog() for modal windows:
```csharp
// WinForms - must handle modal form separately
var dialog = FindControl<MessageBoxDialog>("ConfirmDialog");
dialog.WaitForDisplay();  // Modal form may not be in same tree
```

---

## Implementation Phases

### Phase 1: Project Setup (1 day)
- [ ] Create `Brinell.WinForms.csproj` 
- [ ] Add dependencies: FlaUI.Core, FlaUI.UIA3
- [ ] Create infrastructure classes
- [ ] Create `WinFormsUITestBase`
- [ ] Reference `Brinell.Core`

### Phase 2: Basic Controls (2 days)
- [ ] Implement TextBoxControl
- [ ] Implement ButtonControl
- [ ] Implement CheckBoxControl, RadioButtonControl
- [ ] Implement ComboBoxControl, ListBoxControl
- [ ] Implement LabelControl
- [ ] Add control base class with common patterns

### Phase 3: Data Controls (1.5 days)
- [ ] Implement DataGridViewControl with cell access
- [ ] Implement TreeViewControl with node navigation
- [ ] Add sorting and selection patterns

### Phase 4: Dialogs & Forms (1 day)
- [ ] Implement MessageBoxDialog
- [ ] Add modal form handling
- [ ] Create form page object patterns

### Phase 5: Sample Application (2 days)
- [ ] Create `Brinell.Samples.WinForms.App` (Windows Forms app)
- [ ] Create sample features (Forms, DataGrid, etc.)
- [ ] Create `Brinell.Samples.WinForms.UITests` project
- [ ] Write 12+ sample tests

### Phase 6: Documentation (1 day)
- [ ] Create `docs/platform-guides/winforms.md`
- [ ] Document control wrappers
- [ ] Create testing best practices guide
- [ ] Add migration guide for WPF → WinForms testing

**Total: ~8.5 days**

---

## Project Dependencies

### Brinell.WinForms.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFrameworks>net8.0-windows;net9.0-windows;net10.0-windows</TargetFrameworks>
    <RootNamespace>Brinell.WinForms</RootNamespace>
    <Description>WinForms UI testing support using FlaUI for Windows UI Automation. Part of the Brinell UI testing framework.</Description>
    <PackageId>Brinell.WinForms</PackageId>
    <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
    <!-- Suppress FlaUI's System.Drawing.Common vulnerability warning -->
    <NoWarn>$(NoWarn);NU1904</NoWarn>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="FlaUI.Core" />
    <PackageReference Include="FlaUI.UIA3" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Brinell.Core\Brinell.Core.csproj" />
  </ItemGroup>
  
  <ItemGroup>
    <None Include="..\..\README.md" Pack="true" PackagePath="" />
  </ItemGroup>

</Project>
```

### Brinell.Samples.WinForms.App.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk.WindowsDesktop">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net9.0-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <RootNamespace>Brinell.Samples.WinForms.App</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Brinell.Samples.Shared\Brinell.Samples.Shared.csproj" />
  </ItemGroup>
</Project>
```

### Brinell.Samples.WinForms.UITests.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Brinell.WinForms\Brinell.WinForms.csproj" />
    <ProjectReference Include="..\Brinell.Samples.WinForms.App\Brinell.Samples.WinForms.App.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="FluentAssertions" />
  </ItemGroup>
</Project>
```

---

## Sample Application Structure

```
samples/
├── Brinell.Samples.Shared/               # Shared MVVM infrastructure
│
├── Brinell.Samples.WinForms.App/         # WinForms sample application
│   ├── Brinell.Samples.WinForms.App.csproj
│   ├── Program.cs
│   ├── MainForm.cs                       # Main application window
│   ├── Forms/
│   │   ├── LoginForm.cs                  # Login dialog
│   │   ├── DashboardForm.cs              # Main dashboard
│   │   ├── DataGridForm.cs               # DataGrid with CRUD
│   │   ├── FormControlsForm.cs           # Demonstrate form controls
│   │   ├── ConfirmDialog.cs              # Confirmation dialog
│   │   └── AboutDialog.cs                # About dialog
│   ├── Models/
│   │   ├── User.cs
│   │   └── Product.cs
│   └── Resources/
│       └── app.ico
│
└── Brinell.Samples.WinForms.UITests/     # UI test project
    ├── Brinell.Samples.WinForms.UITests.csproj
    ├── TestBase/
    │   └── WinFormsSampleTestBase.cs
    ├── PageObjects/
    │   ├── MainPage.cs
    │   ├── LoginPage.cs
    │   ├── DashboardPage.cs
    │   ├── DataGridPage.cs
    │   ├── FormControlsPage.cs
    │   ├── ConfirmDialog.cs
    │   └── AboutDialog.cs
    └── Tests/
        ├── LoginTests.cs
        ├── NavigationTests.cs
        ├── FormControlsTests.cs
        ├── DataGridTests.cs
        ├── DialogTests.cs
        ├── MenuTests.cs
        └── IsBusyTests.cs
```

---

## Features to Demonstrate

### 1. Authentication Flow
- Login form with validation
- Username/Password fields
- Modal dialog behavior
- Error message display
- Navigation on success

### 2. Main Window Navigation
- Menu bar with file/tools/help
- Status bar with messages
- Toolbar with buttons
- Form switching

### 3. Form Controls
- TextBox with data binding
- NumericUpDown
- CheckBox and RadioButton groups
- ComboBox (dropdown list)
- ListBox (multi-select)
- DateTimePicker
- TrackBar

### 4. Data Grid
- DataGridView with columns
- Sorting and filtering
- Cell editing
- Row selection
- Add/Delete operations

### 5. Tree Navigation
- TreeView with hierarchy
- Node selection
- Expand/Collapse

### 6. Dialogs
- Modal confirmation dialogs
- Message boxes
- File open/save dialogs
- About dialog

### 7. Async Operations
- Background worker patterns
- Loading indicators
- Button disable during operations

---

## Implementation Details

### WinFormsUITestBase Pattern
```csharp
public abstract class WinFormsUITestBase : UITestBase<FlaUITestContext>
{
    protected void LaunchApplication(string? arguments = null)
    {
        var logger = CsvTestLogger.CreateDefault(TestName);
        _driver = new FlaUIDriverAdapter(ApplicationPath, arguments);
        var context = new FlaUITestContext(_driver, Log);
        InitializeContext(context, logger);
    }
    
    protected abstract string ApplicationPath { get; }
    
    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _driver?.Dispose();
        base.Dispose(disposing);
    }
}
```

### Control Naming Convention
WinForms developers use naming conventions:
- `btn` = Button (e.g., `btnSubmit`, `btnCancel`)
- `txt` = TextBox (e.g., `txtUsername`)
- `chk` = CheckBox (e.g., `chkRemember`)
- `cbo` = ComboBox (e.g., `cboCategory`)
- `lst` = ListBox (e.g., `lstItems`)
- `dgv` = DataGridView (e.g., `dgvProducts`)
- `lbl` = Label (e.g., `lblStatus`)

Test pattern:
```csharp
var loginButton = FindControl<ButtonControl>("btnLogin");
var usernameBox = FindControl<TextBoxControl>("txtUsername");
```

### Modal Dialog Handling
```csharp
// WinForms modal forms are shown with ShowDialog()
// They may appear in separate UI Automation tree

var dialog = new ConfirmDialog(Context);
dialog.WaitForDisplay(TimeSpan.FromSeconds(3));
dialog.ClickYes();
dialog.WaitForClose();
```

---

## Test Examples

### LoginTests.cs
```csharp
[UITest]
public class LoginTests : WinFormsSampleTestBase
{
    [Fact]
    public void Login_WithValidCredentials_NavigatesToDashboard()
    {
        // Arrange
        LaunchApplication();
        var loginPage = new LoginPage(Context);
        
        // Act
        loginPage.EnterUsername("demo");
        loginPage.EnterPassword("password");
        loginPage.ClickLogin();
        
        // Assert
        var dashboard = new DashboardPage(Context);
        Assert.True(dashboard.IsDisplayed);
    }
    
    [Fact]
    public void Login_WithEmptyUsername_ShowsValidationError()
    {
        LaunchApplication();
        var loginPage = new LoginPage(Context);
        
        loginPage.ClickLogin();
        
        Assert.True(loginPage.UsernameErrorLabel.IsDisplayed);
    }
}
```

### DataGridTests.cs
```csharp
[UITest]
public class DataGridTests : WinFormsSampleTestBase
{
    [Fact]
    public void DataGrid_AddRow_DisplaysNewItem()
    {
        LaunchApplication();
        NavigateToDashboard();
        var gridPage = new DataGridPage(Context);
        
        // Arrange
        gridPage.ClickAddButton();
        gridPage.EnterProductName("New Widget");
        gridPage.EnterPrice("99.99");
        
        // Act
        gridPage.ClickSaveButton();
        
        // Assert
        var rows = gridPage.GetGridRows();
        Assert.Contains(rows, r => r.Contains("New Widget"));
    }
    
    [Fact]
    public void DataGrid_DeleteRow_RemovesItem()
    {
        LaunchApplication();
        var gridPage = new DataGridPage(Context);
        
        gridPage.SelectGridRow(0);
        gridPage.ClickDeleteButton();
        
        var dialog = new ConfirmDialog(Context);
        dialog.WaitForDisplay();
        dialog.ClickYes();
        
        var rows = gridPage.GetGridRows();
        Assert.Equal(initialRowCount - 1, rows.Count);
    }
}
```

### DialogTests.cs
```csharp
[UITest]
public class DialogTests : WinFormsSampleTestBase
{
    [Fact]
    public void ConfirmDialog_ClickYes_ClosesDialog()
    {
        LaunchApplication();
        var mainPage = new MainPage(Context);
        
        mainPage.TriggerConfirmDialog();
        
        var dialog = new ConfirmDialog(Context);
        dialog.WaitForDisplay();
        Assert.True(dialog.IsDisplayed);
        
        dialog.ClickYes();
        
        dialog.WaitForClose();
        Assert.False(dialog.IsDisplayed);
    }
}
```

---

## Page Object Examples

### LoginPage.cs
```csharp
public class LoginPage : PageBase
{
    public TextBoxControl UsernameTextBox => FindControl<TextBoxControl>("txtUsername");
    public TextBoxControl PasswordTextBox => FindControl<TextBoxControl>("txtPassword");
    public ButtonControl LoginButton => FindControl<ButtonControl>("btnLogin");
    public LabelControl UsernameErrorLabel => FindControl<LabelControl>("lblUsernameError");
    
    public LoginPage(FlaUITestContext context) : base(context) { }
    
    public void EnterUsername(string username) => UsernameTextBox.SetText(username);
    public void EnterPassword(string password) => PasswordTextBox.SetText(password);
    public void ClickLogin() => LoginButton.Click();
    
    public void Login(string username, string password)
    {
        EnterUsername(username);
        EnterPassword(password);
        ClickLogin();
    }
}
```

### DataGridPage.cs
```csharp
public class DataGridPage : PageBase
{
    public DataGridViewControl ProductsGrid => FindControl<DataGridViewControl>("dgvProducts");
    public ButtonControl AddButton => FindControl<ButtonControl>("btnAdd");
    public ButtonControl DeleteButton => FindControl<ButtonControl>("btnDelete");
    public TextBoxControl ProductNameBox => FindControl<TextBoxControl>("txtProductName");
    
    public DataGridPage(FlaUITestContext context) : base(context) { }
    
    public void ClickAddButton() => AddButton.Click();
    public void ClickDeleteButton() => DeleteButton.Click();
    
    public void EnterProductName(string name) => ProductNameBox.SetText(name);
    
    public List<string> GetGridRows() => ProductsGrid.GetRowTexts();
    
    public void SelectGridRow(int rowIndex) => ProductsGrid.SelectRow(rowIndex);
}
```

### ConfirmDialog.cs
```csharp
public class ConfirmDialog : PageBase
{
    public LabelControl MessageLabel => FindControl<LabelControl>("lblMessage");
    public ButtonControl YesButton => FindControl<ButtonControl>("btnYes");
    public ButtonControl NoButton => FindControl<ButtonControl>("btnNo");
    
    public ConfirmDialog(FlaUITestContext context) : base(context) { }
    
    public string Message => MessageLabel.Text;
    
    public void ClickYes() => YesButton.Click();
    public void ClickNo() => NoButton.Click();
    
    public void WaitForDisplay(TimeSpan? timeout = null)
        => YesButton.WaitForVisibility(timeout ?? TimeSpan.FromSeconds(5));
    
    public void WaitForClose(TimeSpan? timeout = null)
        => YesButton.WaitForInvisibility(timeout ?? TimeSpan.FromSeconds(5));
}
```

---

## WinForms-Specific Challenges & Solutions

| Challenge | Cause | Solution |
|-----------|-------|----------|
| Modal form not found | Modal forms may load asynchronously | Use `WaitForDisplay()` before interacting |
| Control naming inconsistent | Developer naming conventions vary | Use automation IDs or Name property consistently |
| TextBox.Text vs Value | Different controls use different properties | Abstract in control wrapper |
| DataGridView cell access | Complex hierarchical structure | Provide dedicated `GetCell()` method |
| Context menu clicks | Right-click menus are separate windows | Use UI Automation menu patterns |
| MDI child form handling | MDI forms have container overhead | Handle container window finding |
| Enabled property timing | Controls may disable briefly | Use `WaitForCondition()` with retries |

---

## Key Advantages Over WPF

| Aspect | WinForms | WPF |
|--------|----------|-----|
| Control naming | Simple (Name property) | Complex (x:Name, x:Key) |
| Learning curve | Lower | Higher |
| Legacy code support | Excellent | Good |
| Enterprise adoption | Very high | Growing |
| Simple forms | Faster to build | More boilerplate |
| Testing overhead | Lower | Higher |

---

## Success Criteria

- [ ] `Brinell.WinForms` package compiles and publishes
- [ ] All major WinForms controls have wrappers (TextBox, Button, ComboBox, DataGridView, TreeView, etc.)
- [ ] `Brinell.Samples.WinForms.App` builds and runs successfully
- [ ] 15+ UI tests in sample project, all passing
- [ ] Modal dialog handling works reliably
- [ ] DataGridView with CRUD operations testable
- [ ] Page objects follow consistent patterns
- [ ] Documentation covers WinForms-specific patterns
- [ ] Added to CI build pipeline
- [ ] Package published to NuGet

---

## Integration with Existing Platforms

### Platform Compatibility Matrix
```
Brinell.Core ← Base abstractions
├── Brinell.Html (Selenium) ← Web apps
├── Brinell.Wpf (FlaUI) ← WPF apps
├── Brinell.WinForms (FlaUI) ← NEW: WinForms apps
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

Create `docs/platform-guides/winforms.md`:
1. Getting Started
   - Project setup
   - Installing `Brinell.WinForms` NuGet
   - Basic test structure

2. Control Wrappers
   - Table of all supported controls
   - Property mapping (WinForms → Wrapper)
   - Usage examples for each

3. Page Objects
   - Creating page object classes
   - Naming conventions
   - Action methods vs assertions

4. Modal Forms
   - Detecting modal dialogs
   - Waiting for display/close
   - Button click patterns

5. DataGrid Testing
   - Reading rows/columns
   - Editing cells
   - Sorting and filtering
   - CRUD operations

6. Async Operations
   - BackgroundWorker testing
   - IsBusy pattern
   - Wait strategies

7. Best Practices
   - Automation ID assignment
   - Control naming conventions
   - Test data setup
   - Performance tips

---

## Migration Path: WPF Tests → WinForms

For developers with WPF test experience:

```csharp
// WPF test
public class LoginTests : WpfUITestBase
{
    protected override string ApplicationPath => GetWpfAppPath();
}

// WinForms equivalent (same structure!)
public class LoginTests : WinFormsUITestBase
{
    protected override string ApplicationPath => GetWinFormsAppPath();
}

// Page objects are identical - only the base class changes
public class LoginPage : PageBase
{
    public TextBoxControl UsernameTextBox => FindControl<TextBoxControl>("txtUsername");
    // ... same implementation
}
```

---

## Timeline & Dependencies

**Start date:** After Stride automation completed (estimated)
**Duration:** ~8.5 days
**Dependencies:** 
- Brinell.Core (existing)
- FlaUI (via NuGet)
- .NET 8/9/10 SDK

**Blocking:** None - can start immediately after WPF infrastructure is proven

---

## References

- [WinForms Controls](https://learn.microsoft.com/dotnet/desktop/winforms/controls/)
- [FlaUI Documentation](https://github.com/FlaUI/FlaUI)
- [UI Automation Patterns](https://learn.microsoft.com/en-us/windows/win32/winauto/ui-automation-control-patterns)
- [WinForms Best Practices](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/best-practices)

