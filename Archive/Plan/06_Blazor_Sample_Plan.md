# Brinell Blazor Sample Application Plan

## Overview

Create a comprehensive Blazor sample application with UI tests to demonstrate Brinell.Blazor capabilities (to be created per Plan 03).

**Goal**: Reference implementation showing best practices for Blazor UI testing with Brinell.

**Prerequisite**: Brinell.Blazor package must be implemented first (see 03_Blazor_Plan.md).

---

## Sample Application Structure

```
samples/
├── Brinell.Samples.Shared/           # ✅ Already created (MVVM infrastructure)
│
├── Brinell.Samples.Blazor.App/       # Blazor sample application
│   ├── Brinell.Samples.Blazor.App.csproj
│   ├── Program.cs
│   ├── App.razor
│   ├── Components/
│   │   ├── Layout/
│   │   │   ├── MainLayout.razor
│   │   │   └── NavMenu.razor
│   │   └── Shared/
│   │       ├── LoadingOverlay.razor
│   │       └── ValidationSummary.razor
│   ├── Pages/
│   │   ├── Login.razor
│   │   ├── Dashboard.razor
│   │   ├── Forms.razor
│   │   ├── DataTable.razor
│   │   └── Counter.razor              # Classic Blazor demo
│   ├── Services/
│   │   ├── AuthService.cs
│   │   └── DataService.cs
│   └── Models/
│       ├── LoginModel.cs
│       └── TodoItem.cs
│
└── Brinell.Samples.Blazor.UITests/   # UI test project
    ├── Brinell.Samples.Blazor.UITests.csproj
    ├── TestBase/
    │   └── BlazorSampleTestBase.cs
    ├── PageObjects/
    │   ├── LoginPage.cs
    │   ├── DashboardPage.cs
    │   ├── FormsPage.cs
    │   ├── DataTablePage.cs
    │   └── CounterPage.cs
    └── Tests/
        ├── LoginTests.cs
        ├── NavigationTests.cs
        ├── FormValidationTests.cs
        ├── DataTableTests.cs
        ├── CounterTests.cs
        └── BlazorWaitTests.cs
```

---

## Blazor Hosting Models

Support both hosting models with same test project:

### Blazor Server
- SignalR circuit connection
- Server-side rendering
- Real-time updates

### Blazor WebAssembly  
- Client-side execution
- Static file hosting
- Offline capable

### Configuration
```csharp
public class BlazorSampleTestBase : BlazorUITestBase
{
    protected override BlazorHostingModel HostingModel => 
        Environment.GetEnvironmentVariable("BLAZOR_MODE") == "wasm" 
            ? BlazorHostingModel.WebAssembly 
            : BlazorHostingModel.Server;
}
```

---

## Features to Demonstrate

### 1. Counter (Classic Blazor Demo)
- Button click incrementing counter
- Component state management
- Basic interaction testing

### 2. Authentication with EditForm
- EditForm with DataAnnotations validation
- Form submission
- Validation message display
- Navigation on success

### 3. Navigation
- NavLink active state
- Route parameters
- Programmatic navigation
- Browser back/forward

### 4. Forms with Validation
- InputText, InputNumber
- InputSelect, InputCheckbox
- InputDate, InputRadio
- Custom validation
- ValidationMessage components

### 5. Data Table
- Table with sorting
- Pagination
- Row selection
- CRUD operations

### 6. Blazor-Specific Patterns
- SignalR reconnection (Server)
- Loading states
- Streaming rendering
- JavaScript interop

---

## Implementation Phases

### Phase 1: Project Setup (0.5 day)
- [ ] Create `Brinell.Samples.Blazor.App` (Blazor Server)
- [ ] Create `Brinell.Samples.Blazor.UITests`
- [ ] Add project references
- [ ] Configure for both Server and WASM builds

### Phase 2: Layout & Navigation (0.5 day)
- [ ] Create MainLayout with sidebar
- [ ] Create NavMenu with links
- [ ] Setup routing
- [ ] Create navigation page objects

### Phase 3: Counter Feature (0.5 day)
- [ ] Create Counter.razor (classic demo)
- [ ] Create CounterPage page object
- [ ] Write CounterTests
- [ ] Demonstrate WaitForRender()

### Phase 4: Login Feature (1 day)
- [ ] Create Login.razor with EditForm
- [ ] Add DataAnnotations validation
- [ ] Implement AuthService
- [ ] Create LoginPage page object
- [ ] Write LoginTests

### Phase 5: Forms Feature (1 day)
- [ ] Create Forms.razor with all input types
- [ ] Add complex validation scenarios
- [ ] Create FormsPage page object
- [ ] Write FormValidationTests

### Phase 6: Data Table Feature (1 day)
- [ ] Create DataTable.razor
- [ ] Add sorting and pagination
- [ ] Implement DataService
- [ ] Create DataTablePage page object
- [ ] Write DataTableTests

### Phase 7: Blazor-Specific Tests (1 day)
- [ ] Write BlazorWaitTests
- [ ] Test SignalR reconnection (Server)
- [ ] Test WASM initialization
- [ ] Test streaming rendering

### Phase 8: WASM Build (0.5 day)
- [ ] Add WASM project configuration
- [ ] Verify tests work on both modes
- [ ] Document differences

**Total: ~6 days**

---

## Project Dependencies

### Brinell.Samples.Blazor.App.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <RootNamespace>Brinell.Samples.Blazor.App</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Brinell.Samples.Shared\Brinell.Samples.Shared.csproj" />
  </ItemGroup>
</Project>
```

### Brinell.Samples.Blazor.UITests.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <!-- Depends on Brinell.Blazor which extends Brinell.Html -->
    <ProjectReference Include="..\..\src\Brinell.Blazor\Brinell.Blazor.csproj" />
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

### CounterTests.cs
```csharp
[UITest]
[Platform(Platform.Blazor)]
public class CounterTests : BlazorSampleTestBase
{
    [Fact]
    public async Task ClickButton_IncrementsCounter()
    {
        // Arrange
        await NavigateTo("/counter");
        await WaitForBlazorReady();
        
        var counterPage = new CounterPage(Context);
        
        // Act
        await counterPage.ClickIncrementButton();
        await WaitForRender();
        
        // Assert
        Assert.Equal("1", counterPage.CurrentCount);
    }
    
    [Fact]
    public async Task ClickButtonMultipleTimes_AccumulatesCount()
    {
        await NavigateTo("/counter");
        await WaitForBlazorReady();
        
        var counterPage = new CounterPage(Context);
        
        for (int i = 0; i < 5; i++)
        {
            await counterPage.ClickIncrementButton();
            await WaitForRender();
        }
        
        Assert.Equal("5", counterPage.CurrentCount);
    }
}
```

### LoginTests.cs
```csharp
[UITest]
[Platform(Platform.Blazor)]
public class LoginTests : BlazorSampleTestBase
{
    [Fact]
    public async Task Login_WithValidCredentials_NavigatesToDashboard()
    {
        await NavigateTo("/login");
        await WaitForBlazorReady();
        
        var loginPage = new LoginPage(Context);
        
        await loginPage.EnterEmail("user@example.com");
        await loginPage.EnterPassword("password123");
        await loginPage.ClickLogin();
        
        await WaitForNavigation("/dashboard");
        
        var dashboard = new DashboardPage(Context);
        Assert.True(await dashboard.IsDisplayed());
    }
    
    [Fact]
    public async Task Login_WithInvalidEmail_ShowsValidationError()
    {
        await NavigateTo("/login");
        await WaitForBlazorReady();
        
        var loginPage = new LoginPage(Context);
        
        await loginPage.EnterEmail("not-an-email");
        await loginPage.ClickLogin();
        await WaitForRender();
        
        Assert.True(loginPage.EmailValidationMessage.IsDisplayed);
        Assert.Contains("valid email", loginPage.EmailValidationMessage.Text);
    }
}
```

### BlazorWaitTests.cs
```csharp
[UITest]
[Platform(Platform.Blazor)]
public class BlazorWaitTests : BlazorSampleTestBase
{
    [Fact]
    public async Task WaitForBlazorReady_WaitsForCircuit()
    {
        // Navigate and wait for Blazor to initialize
        await NavigateTo("/");
        await WaitForBlazorReady();
        
        // Blazor should be fully interactive
        Assert.True(await IsBlazorConnected());
    }
    
    [Fact]
    [SkipIfWasm] // Only for Blazor Server
    public async Task SignalRReconnect_WaitsForReconnection()
    {
        await NavigateTo("/dashboard");
        await WaitForBlazorReady();
        
        // Simulate disconnect (implementation detail)
        await SimulateDisconnect();
        
        // Wait for reconnection
        await WaitForCircuitReconnect(timeout: TimeSpan.FromSeconds(10));
        
        Assert.True(await IsBlazorConnected());
    }
    
    [Fact]
    public async Task StreamingRendering_WaitsForCompletion()
    {
        await NavigateTo("/data-table");
        
        // Wait for streaming content to finish
        await WaitForStreamingComplete();
        
        var table = new DataTablePage(Context);
        Assert.True(table.Rows.Count > 0);
    }
}
```

---

## Page Object Examples

### CounterPage.cs
```csharp
public class CounterPage : BlazorPageBase
{
    public BlazorButtonControl IncrementButton => 
        FindControl<BlazorButtonControl>("increment-btn");
    
    public LabelControl CountDisplay => 
        FindControl<LabelControl>("count-display");
    
    public string CurrentCount => CountDisplay.Text;
    
    public CounterPage(BlazorTestContext context) : base(context) { }
    
    public async Task ClickIncrementButton()
    {
        await IncrementButton.ClickAsync();
        await WaitForRender();
    }
}
```

### LoginPage.cs
```csharp
public class LoginPage : BlazorPageBase
{
    // EditForm controls
    public BlazorInputControl EmailInput => 
        FindControl<BlazorInputControl>("email-input");
    
    public BlazorInputControl PasswordInput => 
        FindControl<BlazorInputControl>("password-input");
    
    public BlazorButtonControl LoginButton => 
        FindControl<BlazorButtonControl>("login-btn");
    
    // Validation messages
    public ValidationMessageControl EmailValidationMessage => 
        FindValidationMessage("Email");
    
    public ValidationMessageControl PasswordValidationMessage => 
        FindValidationMessage("Password");
    
    public LoginPage(BlazorTestContext context) : base(context) { }
    
    public async Task EnterEmail(string email) => 
        await EmailInput.SetTextAsync(email);
    
    public async Task EnterPassword(string password) => 
        await PasswordInput.SetTextAsync(password);
    
    public async Task ClickLogin()
    {
        await LoginButton.ClickAsync();
        await WaitForRender();
    }
}
```

---

## Blazor Component Examples

### Login.razor
```razor
@page "/login"
@inject NavigationManager Navigation
@inject IAuthService AuthService

<h1>Login</h1>

<EditForm Model="@loginModel" OnValidSubmit="@HandleLogin">
    <DataAnnotationsValidator />
    
    <div class="form-group">
        <label for="email">Email</label>
        <InputText id="email-input" @bind-Value="loginModel.Email" class="form-control" />
        <ValidationMessage For="@(() => loginModel.Email)" />
    </div>
    
    <div class="form-group">
        <label for="password">Password</label>
        <InputText id="password-input" type="password" @bind-Value="loginModel.Password" class="form-control" />
        <ValidationMessage For="@(() => loginModel.Password)" />
    </div>
    
    <button id="login-btn" type="submit" class="btn btn-primary" disabled="@isLoading">
        @if (isLoading)
        {
            <span>Loading...</span>
        }
        else
        {
            <span>Login</span>
        }
    </button>
</EditForm>

@code {
    private LoginModel loginModel = new();
    private bool isLoading = false;
    
    private async Task HandleLogin()
    {
        isLoading = true;
        try
        {
            await AuthService.LoginAsync(loginModel.Email, loginModel.Password);
            Navigation.NavigateTo("/dashboard");
        }
        finally
        {
            isLoading = false;
        }
    }
}
```

---

## Success Criteria

- [ ] Sample app builds as Blazor Server
- [ ] Sample app builds as Blazor WASM (optional)
- [ ] All 5 features implemented
- [ ] Uses Brinell.Samples.Shared where applicable
- [ ] 15+ UI tests passing
- [ ] Blazor-specific waits demonstrated
- [ ] EditForm validation tested
- [ ] Page objects for all pages
- [ ] README with running instructions
- [ ] Added to CI build

---

## Blazor Controls Demonstrated

| Control | Page | Test Coverage |
|---------|------|---------------|
| InputText | Login, Forms | ✓ |
| InputNumber | Forms | ✓ |
| InputCheckbox | Forms | ✓ |
| InputSelect | Forms | ✓ |
| InputDate | Forms | ✓ |
| InputRadio | Forms | ✓ |
| Button | All | ✓ |
| NavLink | Layout | ✓ |
| ValidationMessage | Login, Forms | ✓ |
| ValidationSummary | Forms | ✓ |
| Table | DataTable | ✓ |

---

## CI/CD Configuration

### Build Blazor Sample
```yaml
- name: Build Blazor Sample
  run: dotnet build samples/Brinell.Samples.Blazor.App/

- name: Start Blazor Server
  run: |
    dotnet run --project samples/Brinell.Samples.Blazor.App/ &
    sleep 10  # Wait for server to start

- name: Run Blazor UI Tests
  run: dotnet test samples/Brinell.Samples.Blazor.UITests/
  env:
    BLAZOR_APP_URL: http://localhost:5000
```

---

## Dependencies on Other Plans

| Dependency | Plan | Status |
|------------|------|--------|
| Brinell.Blazor package | 03_Blazor_Plan.md | Required |
| Brinell.Samples.Shared | 04_MVVM_Shared_Plan.md | ✅ Done |
