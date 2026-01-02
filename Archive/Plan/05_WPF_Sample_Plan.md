# Brinell WPF Sample Application Plan

## Overview

Create a comprehensive WPF sample application with UI tests to demonstrate Brinell.Wpf capabilities.

**Goal**: Reference implementation showing best practices for WPF UI testing with Brinell.

---

## Sample Application Structure

```
samples/
├── Brinell.Samples.Shared/           # ✅ Already created (MVVM infrastructure)
│
├── Brinell.Samples.Wpf.App/          # WPF sample application
│   ├── Brinell.Samples.Wpf.App.csproj
│   ├── App.xaml / App.xaml.cs
│   ├── Features/
│   │   ├── Shell/
│   │   │   ├── Views/ShellWindow.xaml
│   │   │   └── ViewModels/ShellViewModel.cs
│   │   ├── Login/
│   │   │   ├── Views/LoginPage.xaml
│   │   │   └── ViewModels/LoginViewModel.cs
│   │   ├── Dashboard/
│   │   │   ├── Views/DashboardPage.xaml
│   │   │   └── ViewModels/DashboardViewModel.cs
│   │   ├── Forms/
│   │   │   ├── Views/FormPage.xaml
│   │   │   └── ViewModels/FormViewModel.cs
│   │   ├── DataGrid/
│   │   │   ├── Views/DataGridPage.xaml
│   │   │   └── ViewModels/DataGridViewModel.cs
│   │   └── Dialogs/
│   │       ├── Views/ConfirmDialog.xaml
│   │       └── ViewModels/ConfirmDialogViewModel.cs
│   ├── Infrastructure/
│   │   ├── Navigation/NavigationService.cs
│   │   └── Converters/
│   └── Models/
│       ├── User.cs
│       └── TodoItem.cs
│
└── Brinell.Samples.Wpf.UITests/      # UI test project
    ├── Brinell.Samples.Wpf.UITests.csproj
    ├── TestBase/
    │   └── WpfSampleTestBase.cs
    ├── PageObjects/
    │   ├── ShellPage.cs
    │   ├── LoginPage.cs
    │   ├── DashboardPage.cs
    │   ├── FormPage.cs
    │   ├── DataGridPage.cs
    │   └── ConfirmDialog.cs
    └── Tests/
        ├── LoginTests.cs
        ├── NavigationTests.cs
        ├── FormValidationTests.cs
        ├── DataGridTests.cs
        ├── DialogTests.cs
        └── IsBusyTests.cs
```

---

## Features to Demonstrate

### 1. Authentication Flow
- Login form with validation
- Username/password fields
- Error message display
- Navigation on success

### 2. Navigation
- Shell with sidebar menu
- Page transitions
- Back navigation
- ViewVisible protection

### 3. Form Controls
- TextBox, PasswordBox
- ComboBox, CheckBox, RadioButton
- DatePicker, Slider
- Form validation with error messages

### 4. Data Display
- DataGrid with sorting
- ListView with selection
- Item details panel
- CRUD operations

### 5. Dialogs
- Confirmation dialogs
- Modal windows
- Message boxes

### 6. IsBusy Pattern
- Loading indicators
- Button disable during operations
- Test waiting for IsBusy=false

---

## Implementation Phases

### Phase 1: Project Setup (1 day)
- [ ] Create `Brinell.Samples.Wpf.App` project
- [ ] Create `Brinell.Samples.Wpf.UITests` project
- [ ] Add project references to Brinell.Samples.Shared
- [ ] Add references to Brinell.Wpf
- [ ] Setup basic App.xaml with resources

### Phase 2: Shell & Navigation (1 day)
- [ ] Create ShellWindow with sidebar
- [ ] Implement NavigationService
- [ ] Create ShellViewModel with menu items
- [ ] Add navigation commands

### Phase 3: Login Feature (0.5 day)
- [ ] Create LoginPage.xaml with form
- [ ] Implement LoginViewModel with validation
- [ ] Add IsBusy indicator
- [ ] Create LoginPage page object
- [ ] Write LoginTests

### Phase 4: Dashboard Feature (0.5 day)
- [ ] Create DashboardPage with summary cards
- [ ] Implement DashboardViewModel
- [ ] Create DashboardPage page object
- [ ] Write NavigationTests

### Phase 5: Forms Feature (1 day)
- [ ] Create FormPage with all control types
- [ ] Implement FormViewModel with validation
- [ ] Add validation error display
- [ ] Create FormPage page object
- [ ] Write FormValidationTests

### Phase 6: DataGrid Feature (1 day)
- [ ] Create DataGridPage with data table
- [ ] Implement DataGridViewModel with CRUD
- [ ] Add sorting and selection
- [ ] Create DataGridPage page object
- [ ] Write DataGridTests

### Phase 7: Dialogs (0.5 day)
- [ ] Create ConfirmDialog
- [ ] Add dialog service
- [ ] Create ConfirmDialog page object
- [ ] Write DialogTests

### Phase 8: IsBusy Demo (0.5 day)
- [ ] Add loading overlays
- [ ] Demonstrate async operations
- [ ] Write IsBusyTests showing wait patterns

**Total: ~6 days**

---

## Project Dependencies

### Brinell.Samples.Wpf.App.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net9.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <RootNamespace>Brinell.Samples.Wpf.App</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Brinell.Samples.Shared\Brinell.Samples.Shared.csproj" />
  </ItemGroup>
</Project>
```

### Brinell.Samples.Wpf.UITests.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Brinell.Wpf\Brinell.Wpf.csproj" />
    <ProjectReference Include="..\Brinell.Samples.Wpf.App\Brinell.Samples.Wpf.App.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="FluentAssertions" />
  </ItemGroup>
</Project>
```

---

## Test Examples

### LoginTests.cs
```csharp
[UITest]
public class LoginTests : WpfSampleTestBase
{
    [Fact]
    public void Login_WithValidCredentials_NavigatesToDashboard()
    {
        // Arrange
        var loginPage = new LoginPage(Context);
        
        // Act
        loginPage.EnterUsername("demo");
        loginPage.EnterPassword("password");
        loginPage.ClickLogin();
        
        // Assert
        var dashboard = new DashboardPage(Context);
        dashboard.WaitForLoad();
        Assert.True(dashboard.IsDisplayed);
    }
    
    [Fact]
    public void Login_WithEmptyUsername_ShowsValidationError()
    {
        var loginPage = new LoginPage(Context);
        
        loginPage.ClickLogin();
        
        Assert.True(loginPage.UsernameError.IsDisplayed);
        Assert.Equal("Username is required", loginPage.UsernameError.Text);
    }
}
```

### IsBusyTests.cs
```csharp
[UITest]
public class IsBusyTests : WpfSampleTestBase
{
    [Fact]
    public void LongOperation_ShowsBusyIndicator_ThenCompletes()
    {
        var dashboard = NavigateToDashboard();
        
        // Act - start long operation
        dashboard.ClickRefreshData();
        
        // Assert - busy indicator shown
        Assert.True(dashboard.LoadingOverlay.IsDisplayed);
        Assert.False(dashboard.RefreshButton.IsEnabled);
        
        // Wait for completion
        dashboard.WaitForNotBusy();
        
        // Assert - busy indicator hidden
        Assert.False(dashboard.LoadingOverlay.IsDisplayed);
        Assert.True(dashboard.RefreshButton.IsEnabled);
    }
}
```

---

## Page Object Examples

### LoginPage.cs
```csharp
public class LoginPage : PageBase
{
    public TextBoxControl UsernameTextBox => FindControl<TextBoxControl>("UsernameTextBox");
    public TextBoxControl PasswordTextBox => FindControl<TextBoxControl>("PasswordTextBox");
    public ButtonControl LoginButton => FindControl<ButtonControl>("LoginButton");
    public LabelControl UsernameError => FindControl<LabelControl>("UsernameErrorLabel");
    public LabelControl PasswordError => FindControl<LabelControl>("PasswordErrorLabel");
    
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

---

## Success Criteria

- [ ] Sample app builds and runs
- [ ] All 5 features implemented with proper MVVM
- [ ] Uses Brinell.Samples.Shared for ViewModelBase/Commands
- [ ] 15+ UI tests passing
- [ ] IsBusy pattern demonstrated and tested
- [ ] Page objects for all pages/dialogs
- [ ] README with running instructions
- [ ] Added to CI build

---

## UI Controls Demonstrated

| Control | Page | Test Coverage |
|---------|------|---------------|
| TextBox | Login, Forms | ✓ |
| PasswordBox | Login | ✓ |
| Button | All | ✓ |
| ComboBox | Forms | ✓ |
| CheckBox | Forms | ✓ |
| RadioButton | Forms | ✓ |
| DatePicker | Forms | ✓ |
| Slider | Forms | ✓ |
| DataGrid | DataGrid | ✓ |
| ListView | Dashboard | ✓ |
| Menu | Shell | ✓ |
| Dialog/Window | Dialogs | ✓ |
| ProgressBar | IsBusy | ✓ |
