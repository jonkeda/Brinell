# PLAN: Blazor Sample App Implementation

**Date:** January 2026  
**Status:** Ready for Implementation  
**Reference:** SPEC-004, SPEC-006

---

## Objective

Create a comprehensive Blazor Server sample app that demonstrates all 200+ components from SPEC-006 for UI test automation with Playwright, including models, services, and unit tests.

**Reference Design:** DES-002c-BLAZOR-SAMPLE-APP-DESIGN.md (Version 2.0)

**SPEC-006 Interface Coverage:** 40+ interfaces including Foundation, Input, Toggle, Selection, Range, DateTime, Collection, Container, Display, Media, Navigation, and Validation.

---

## Current State

### Existing Files
- Components/Pages/Index.razor
- Components/Pages/Counter.razor
- Components/Pages/Dashboard.razor
- Components/Pages/FormControls.razor
- Components/Pages/Login.razor
- Models/ (empty)
- Services/ (empty)

---

## Models

### Models/UserFormModel.cs

```csharp
using System.ComponentModel.DataAnnotations;

namespace Brinell.Samples.Blazor.App.Models;

public class UserFormModel
{
    [Required(ErrorMessage = "First name is required")]
    [StringLength(50, MinimumLength = 2)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(50, MinimumLength = 2)]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Invalid phone format")]
    public string? Phone { get; set; }

    [StringLength(500)]
    public string? Bio { get; set; }

    [Required]
    public string Country { get; set; } = string.Empty;

    public DateTime? BirthDate { get; set; }

    [Range(18, 120, ErrorMessage = "Age must be between 18 and 120")]
    public int? Age { get; set; }

    public bool SubscribeNewsletter { get; set; }

    [Required(ErrorMessage = "You must accept the terms")]
    public bool AcceptTerms { get; set; }

    public string SubscriptionTier { get; set; } = "basic";
}
```

### Models/DataItem.cs

```csharp
namespace Brinell.Samples.Blazor.App.Models;

public class DataItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
```

### Models/FileUploadResult.cs

```csharp
namespace Brinell.Samples.Blazor.App.Models;

public class FileUploadResult
{
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.Now;
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}
```

---

## Services

### Services/IDataService.cs

```csharp
namespace Brinell.Samples.Blazor.App.Services;

public interface IDataService
{
    Task<IEnumerable<DataItem>> GetItemsAsync(int page = 1, int pageSize = 10);
    Task<DataItem?> GetByIdAsync(int id);
    Task<DataItem> CreateAsync(DataItem item);
    Task<DataItem> UpdateAsync(DataItem item);
    Task DeleteAsync(int id);
    Task<int> GetTotalCountAsync();
}
```

### Services/DataService.cs

```csharp
namespace Brinell.Samples.Blazor.App.Services;

public class DataService : IDataService
{
    private readonly List<DataItem> _items = new();
    private int _nextId = 1;

    public DataService()
    {
        // Seed data
        for (int i = 0; i < 100; i++)
        {
            _items.Add(new DataItem
            {
                Id = _nextId++,
                Name = $"User {i + 1}",
                Email = $"user{i + 1}@example.com",
                Status = i % 3 == 0 ? "Inactive" : "Active",
                CreatedAt = DateTime.Now.AddDays(-i)
            });
        }
    }

    public Task<IEnumerable<DataItem>> GetItemsAsync(int page = 1, int pageSize = 10)
    {
        var items = _items
            .Skip((page - 1) * pageSize)
            .Take(pageSize);
        return Task.FromResult(items);
    }

    public Task<DataItem?> GetByIdAsync(int id) =>
        Task.FromResult(_items.FirstOrDefault(x => x.Id == id));

    public Task<DataItem> CreateAsync(DataItem item)
    {
        item.Id = _nextId++;
        _items.Add(item);
        return Task.FromResult(item);
    }

    public Task<DataItem> UpdateAsync(DataItem item)
    {
        var existing = _items.FirstOrDefault(x => x.Id == item.Id);
        if (existing != null)
        {
            existing.Name = item.Name;
            existing.Email = item.Email;
            existing.Status = item.Status;
        }
        return Task.FromResult(item);
    }

    public Task DeleteAsync(int id)
    {
        _items.RemoveAll(x => x.Id == id);
        return Task.CompletedTask;
    }

    public Task<int> GetTotalCountAsync() =>
        Task.FromResult(_items.Count);
}
```

### Services/IValidationService.cs

```csharp
namespace Brinell.Samples.Blazor.App.Services;

public interface IValidationService
{
    bool ValidateEmail(string email);
    bool ValidatePhone(string phone);
    bool ValidateRequired(string value);
    (bool IsValid, string? ErrorMessage) ValidateAge(int? age);
}
```

---

## Pages to Create

### 1. Dashboard.razor

**SPEC-006 Interfaces:** ITabControlObject, IProgressControlObject, IActivityIndicatorControlObject, IClickableControlObject, ILabelControlObject, IImageControlObject

| Component | data-automation-id | SPEC-006 Interface |
|-----------|-------------------|-------------------|
| TabContainer | DashboardTabs | ITabControlObject |
| h1 | DashboardTitle | ILabelControlObject |
| progress (determinate) | DashboardProgressBar | IProgressControlObject |
| progress (indeterminate) | DashboardIndeterminateProgress | IProgressControlObject |
| Spinner | DashboardSpinner | IActivityIndicatorControlObject |
| button | DashboardRefreshButton | IClickableControlObject |
| button | DashboardExportButton | IClickableControlObject |
| img | DashboardLogo | IImageControlObject |
| NavLink | DashboardLinkForm | IClickableControlObject |
| span | DashboardKpi1Value | ILabelControlObject |

### 2. UserForm.razor

**SPEC-006 Interfaces:** ITextControlObject, IEditableTextControlObject, ISearchControlObject, ISelectorControlObject, IPickerControlObject, IDateControlObject, ITimeControlObject, IDateTimeControlObject, IToggleControlObject, ICheckBoxControlObject, ISwitchControlObject, IRadioButtonControlObject, ISliderControlObject, IStepperControlObject

| Component | data-automation-id | SPEC-006 Interface |
|-----------|-------------------|-------------------|
| InputText (First Name) | FormFirstNameInput | ITextControlObject |
| InputText (Email) | FormEmailInput | ITextControlObject |
| InputText (Password) | FormPasswordInput | ITextControlObject |
| input[type="search"] | FormSearchInput | ISearchControlObject |
| InputTextArea | FormBioTextarea | IEditableTextControlObject |
| InputSelect (Country) | FormCountrySelect | ISelectorControlObject |
| Custom Dropdown | FormCustomPicker | IPickerControlObject |
| InputDate (Birth Date) | FormBirthDateInput | IDateControlObject |
| input[type="time"] | FormContactTimeInput | ITimeControlObject |
| input[type="datetime-local"] | FormMeetingDateTimeInput | IDateTimeControlObject |
| InputCheckbox (Newsletter) | FormNewsletterCheckbox | IToggleControlObject |
| InputCheckbox (Terms) | FormTermsCheckbox | ICheckBoxControlObject |
| Custom Toggle | FormToggleSwitch | ISwitchControlObject |
| InputRadio (Basic) | FormTierBasicRadio | IRadioButtonControlObject |
| InputRadio (Premium) | FormTierPremiumRadio | IRadioButtonControlObject |
| input[type="range"] | FormFontSizeSlider | ISliderControlObject |
| Counter component | FormQuantityStepper | IStepperControlObject |
| button | FormSubmitButton | IClickableControlObject |

### 3. DataTable.razor

**SPEC-006 Interfaces:** IItemsControlObject, ISelectableItemsControlObject, IMultiSelectableItemsControlObject, IScrollableItemsControlObject, IGroupedItemsControlObject, ISearchControlObject, IRefreshableControlObject, IMultiSelectorControlObject, IScrollableControlObject

| Component | data-automation-id | SPEC-006 Interface |
|-----------|-------------------|-------------------|
| input[type="search"] | DataSearchInput | ISearchControlObject |
| button | DataRefreshButton | IRefreshableControlObject |
| select[multiple] | DataMultiSelect | IMultiSelectorControlObject |
| div (scrollable) | DataScrollContainer | IScrollableControlObject |
| Virtualize | DataVirtualList | IScrollableItemsControlObject |
| table | DataTable | IItemsControlObject |
| table (selectable) | DataSelectableTable | ISelectableItemsControlObject |
| table (multi-select) | DataMultiSelectTable | IMultiSelectableItemsControlObject |
| table (grouped) | DataGroupedTable | IGroupedItemsControlObject |
| button | DataSelectAllButton | IClickableControlObject |
| button | DataUnselectAllButton | IClickableControlObject |
| span | DataSelectedCount | ILabelControlObject |

### 4. MediaGallery.razor

**SPEC-006 Interfaces:** IImageControlObject, IMediaControlObject, IWebViewControlObject, IClickableControlObject, ISliderControlObject, IToggleControlObject, IActivityIndicatorControlObject

| Component | data-automation-id | SPEC-006 Interface |
|-----------|-------------------|-------------------|
| img (Local) | MediaLocalImage | IImageControlObject |
| img (Remote) | MediaRemoteImage | IImageControlObject |
| button (Thumbnail 1) | MediaThumbnail1 | IClickableControlObject |
| button (Thumbnail 2) | MediaThumbnail2 | IClickableControlObject |
| img (Full Size) | MediaFullSizeImage | IImageControlObject |
| video | MediaVideoPlayer | IMediaControlObject |
| audio | MediaAudioPlayer | IMediaControlObject |
| button | MediaPlayButton | IClickableControlObject |
| button | MediaPauseButton | IClickableControlObject |
| input[type="range"] (Seek) | MediaSeekSlider | ISliderControlObject |
| input[type="range"] (Volume) | MediaVolumeSlider | ISliderControlObject |
| InputCheckbox | MediaMuteCheckbox | IToggleControlObject |
| iframe | MediaIframe | IWebViewControlObject |
| Spinner | MediaWebSpinner | IActivityIndicatorControlObject |

### 5. FileUpload.razor

**SPEC-006 Interfaces:** IProgressControlObject, IEditableTextControlObject, ISelectorControlObject, IClickableControlObject, IItemsControlObject

| Component | data-automation-id | SPEC-006 Interface |
|-----------|-------------------|-------------------|
| InputFile | UploadFileInput | IControlObject |
| InputTextArea | UploadDescriptionInput | IEditableTextControlObject |
| InputSelect | UploadCategorySelect | ISelectorControlObject |
| button | UploadSubmitButton | IClickableControlObject |
| progress | UploadProgressBar | IProgressControlObject |
| table | UploadFileList | IItemsControlObject |
| button | UploadDownloadButton | IClickableControlObject |
| button | UploadDeleteButton | IClickableControlObject |

### 6. Validation.razor

**SPEC-006 Interfaces:** IValidatableControlObject, ITextControlObject, IClickableControlObject, ILabelControlObject

| Component | data-automation-id | SPEC-006 Interface |
|-----------|-------------------|-------------------|
| EditForm | ValidateForm | IContainerControlObject |
| ValidationSummary | ValidateSummary | ILabelControlObject |
| InputText (Required) | ValidateRequiredInput | IValidatableControlObject |
| ValidationMessage | ValidateRequiredError | ILabelControlObject |
| InputText (Email) | ValidateEmailInput | IValidatableControlObject |
| ValidationMessage | ValidateEmailError | ILabelControlObject |
| InputText (Phone) | ValidatePhoneInput | IValidatableControlObject |
| InputText (Min Length) | ValidateMinLengthInput | IValidatableControlObject |
| InputText (Max Length) | ValidateMaxLengthInput | IValidatableControlObject |
| InputNumber (Range) | ValidateRangeInput | IValidatableControlObject |
| InputText (Regex) | ValidateRegexInput | IValidatableControlObject |
| button | ValidateSubmitButton | IClickableControlObject |
| button | ValidateClearButton | IClickableControlObject |

### 7. Navigation.razor

**SPEC-006 Interfaces:** IMenuControlObject, IToolbarControlObject, IFlyoutControlObject, IExpanderControlObject, ITabControlObject, IClickableControlObject

| Component | data-automation-id | SPEC-006 Interface |
|-----------|-------------------|-------------------|
| nav | NavMenuDemo | IMenuControlObject |
| button | NavMenuItem1 | IClickableControlObject |
| button | NavMenuItem2 | IClickableControlObject |
| Toolbar | NavToolbar | IToolbarControlObject |
| button | NavToolbarSave | IClickableControlObject |
| button | NavToolbarEdit | IClickableControlObject |
| Dropdown/Popover | NavFlyout | IFlyoutControlObject |
| button | NavOpenFlyoutButton | IClickableControlObject |
| details (Expander 1) | NavExpander1 | IExpanderControlObject |
| details (Expander 2) | NavExpander2 | IExpanderControlObject |
| Accordion | NavAccordion | IExpanderControlObject |
| button | NavExpandAllButton | IClickableControlObject |
| TabContainer | NavTabs | ITabControlObject |
| Tab 1 | NavTab1 | IClickableControlObject |

### 8. Advanced.razor

**SPEC-006 Interfaces:** ISwipeableControlObject, IScrollableItemsControlObject, ISelectorControlObject, IFocusableControlObject, ITextControlObject

| Component | data-automation-id | SPEC-006 Interface |
|-----------|-------------------|-------------------|
| ErrorBoundary | AdvancedErrorBoundary | IControlObject |
| button | AdvancedTriggerErrorButton | IClickableControlObject |
| FocusOnNavigate | AdvancedFocusSection | IFocusableControlObject |
| EditForm | AdvancedForm | IContainerControlObject |
| InputText | AdvancedFormInput | ITextControlObject |
| Virtualize | AdvancedVirtualList | IScrollableItemsControlObject |
| DynamicComponent | AdvancedDynamicComponent | IControlObject |
| InputSelect | AdvancedComponentSelect | ISelectorControlObject |
| div (Swipeable) | AdvancedSwipeContainer | ISwipeableControlObject |

### 9. ModalControls.razor

| Component | data-automation-id | Purpose |
|-----------|-------------------|---------|
| div.modal | BasicModal | Modal dialog |
| div.modal | ConfirmModal | Confirmation modal |
| button | OpenModalButton | Open modal |
| button | CloseModalButton | Close modal |
| button | ConfirmButton | Confirm action |
| button | CancelButton | Cancel action |
| h2 | ModalTitle | Modal title |
| div | ModalContent | Modal content |
| div.toast | ToastNotification | Toast message |

### 10. AccordionControls.razor

| Component | data-automation-id | Purpose |
|-----------|-------------------|---------|
| div.accordion | AccordionContainer | Accordion |
| div.accordion-item | AccordionItem_{index} | Accordion items |
| button | AccordionHeader_{index} | Item headers |
| div | AccordionPanel_{index} | Item panels |
| button | ExpandAllButton | Expand all |
| button | CollapseAllButton | Collapse all |

### 11. FormValidation.razor

| Component | data-automation-id | Purpose |
|-----------|-------------------|---------|
| EditForm | ValidationForm | Form container |
| InputText | RequiredInput | Required field |
| InputText | EmailInput | Email validation |
| InputText | PhoneInput | Phone validation |
| InputNumber | AgeInput | Range validation |
| ValidationMessage | ErrorMessage_{field} | Field errors |
| ValidationSummary | ValidationSummary | All errors |
| button | SubmitFormButton | Submit form |
| button | ResetFormButton | Reset form |

### 12. PaginationControls.razor

| Component | data-automation-id | Purpose |
|-----------|-------------------|---------|
| nav.pagination | Pagination | Pagination container |
| button | FirstPageButton | First page |
| button | PrevPageButton | Previous page |
| button | NextPageButton | Next page |
| button | LastPageButton | Last page |
| button | PageButton_{num} | Page numbers |
| span | CurrentPageLabel | Current page |
| span | TotalPagesLabel | Total pages |
| select | PageSizeSelect | Items per page |

---

## App Structure (Updated for SPEC-006 Coverage)

```
Brinell.Samples.Blazor.App/
├── Program.cs
├── App.razor
├── Components/
│   ├── App.razor
│   ├── Routes.razor
│   ├── _Imports.razor
│   ├── Layout/
│   │   ├── MainLayout.razor     # IMenuControlObject
│   │   ├── NavMenu.razor        # IMenuControlObject
│   │   ├── TabNavigation.razor  # ITabControlObject
│   │   └── Footer.razor
│   ├── Pages/
│   │   ├── Dashboard.razor      # Tabs, Progress, ActivityIndicator
│   │   ├── UserForm.razor       # All input, toggle, selection, range, date/time
│   │   ├── DataTable.razor      # Collections, selection, grouping, scrolling
│   │   ├── MediaGallery.razor   # Image, video, audio, iframe
│   │   ├── FileUpload.razor     # InputFile, progress
│   │   ├── Validation.razor     # Form validation
│   │   ├── Navigation.razor     # Menu, Toolbar, Flyout, Expander, Tabs
│   │   └── Advanced.razor       # ErrorBoundary, Virtualize, Swipe
│   └── Shared/
│       ├── Tabs.razor           # ITabControlObject
│       ├── Accordion.razor      # IExpanderControlObject
│       ├── Toolbar.razor        # IToolbarControlObject
│       ├── Flyout.razor         # IFlyoutControlObject
│       ├── Spinner.razor        # IActivityIndicatorControlObject
│       ├── Counter.razor        # IStepperControlObject
│       ├── ToggleSwitch.razor   # ISwitchControlObject
│       └── DataTable.razor      # IItemsControlObject
├── Models/
│   ├── UserFormModel.cs
│   ├── DataItem.cs
│   ├── MediaItem.cs
│   └── FileUploadResult.cs
├── Services/
│   ├── IDataService.cs
│   ├── DataService.cs
│   ├── IValidationService.cs
│   └── ValidationService.cs
└── wwwroot/
    ├── css/
    └── js/
```

---

## DI Registration (Program.cs)

```csharp
// Add services
builder.Services.AddSingleton<IDataService, DataService>();
builder.Services.AddScoped<IValidationService, ValidationService>();
```

---

## Implementation Tasks

### Phase 1: Models & Services

1. Create UserFormModel.cs with validation attributes
2. Create DataItem.cs
3. Create FileUploadResult.cs
4. Create IDataService.cs interface
5. Create DataService.cs implementation
6. Create IValidationService.cs interface
7. Create ValidationService.cs implementation
8. Register services in Program.cs

### Phase 2: Shared Components

1. Create Modal.razor component
2. Create Toast.razor component
3. Create Tabs.razor component
4. Create Accordion.razor component
5. Create Pagination.razor component
6. Create DataTable.razor component

### Phase 3: Create Pages (Priority Order)

1. Dashboard.razor - Tabs, Progress, ActivityIndicator (ITabControlObject, IProgressControlObject, IActivityIndicatorControlObject)
2. UserForm.razor - All input, toggle, selection, range, date/time controls (ITextControlObject, IEditableTextControlObject, ISearchControlObject, ISelectorControlObject, IPickerControlObject, IDateControlObject, ITimeControlObject, IDateTimeControlObject, IToggleControlObject, ICheckBoxControlObject, ISwitchControlObject, IRadioButtonControlObject, ISliderControlObject, IStepperControlObject)
3. DataTable.razor - Collections, selection, grouping, scrolling (IItemsControlObject, ISelectableItemsControlObject, IMultiSelectableItemsControlObject, IScrollableItemsControlObject, IGroupedItemsControlObject, IMultiSelectorControlObject)
4. MediaGallery.razor - Image, video, audio, iframe (IImageControlObject, IMediaControlObject, IWebViewControlObject)
5. FileUpload.razor - InputFile, progress (IProgressControlObject)
6. Validation.razor - Form validation (IValidatableControlObject)
7. Navigation.razor - Menu, Toolbar, Flyout, Expander, Tabs (IMenuControlObject, IToolbarControlObject, IFlyoutControlObject, IExpanderControlObject, ITabControlObject)
8. Advanced.razor - ErrorBoundary, Virtualize, Swipe (ISwipeableControlObject, IScrollableItemsControlObject)

### Phase 4: Update Navigation

1. Update NavMenu.razor with all pages
2. Add page routes
3. Add breadcrumbs

### Phase 5: Unit Tests

1. Create Brinell.Samples.Blazor.App.Tests project
2. Add bUnit package for component testing
3. Create component tests for each page
4. Create service tests
5. Create model validation tests
6. Ensure 80%+ coverage

---

## data-automation-id Convention

```
<PagePrefix><ComponentType><Purpose>

Examples:
- InputPageBasicInput
- TogglePageNotificationCheckbox
- SelectionPageColorSelect
- RangePageVolumeSlider
- TablePageDataTable
- ModalPageConfirmModal
```

---

## Test Coverage Goals (SPEC-006 Aligned)

| SPEC-006 Interface | Component Type | Count | Pages |
|--------------------|----------------|-------|-------|
| IClickableControlObject | button, a, NavLink | 40+ | All pages |
| ITextControlObject | InputText | 12+ | UserForm, Validation |
| IEditableTextControlObject | InputTextArea | 3+ | UserForm, FileUpload |
| ISearchControlObject | input[type="search"] | 2+ | UserForm, DataTable |
| ILabelControlObject | h1-h6, p, span, label | 45+ | All pages |
| IToggleControlObject | InputCheckbox | 3+ | UserForm |
| ICheckBoxControlObject | InputCheckbox (indeterminate) | 3+ | UserForm |
| ISwitchControlObject | Custom toggle | 2+ | UserForm |
| IRadioButtonControlObject | InputRadio | 6+ | UserForm |
| ISelectorControlObject | InputSelect | 6+ | UserForm, DataTable, Advanced |
| IPickerControlObject | Custom dropdown | 2+ | UserForm |
| IMultiSelectorControlObject | select[multiple] | 2+ | DataTable |
| ISliderControlObject | input[type="range"] | 4+ | UserForm, MediaGallery |
| IStepperControlObject | Counter component | 2+ | UserForm |
| IDateControlObject | InputDate | 2+ | UserForm |
| ITimeControlObject | input[type="time"] | 2+ | UserForm |
| IDateTimeControlObject | input[type="datetime-local"] | 1+ | UserForm |
| IItemsControlObject | table, ul, Virtualize | 5+ | DataTable, FileUpload |
| ISelectableItemsControlObject | Table with selection | 2+ | DataTable |
| IMultiSelectableItemsControlObject | Multi-select table | 2+ | DataTable |
| IScrollableItemsControlObject | Virtualize | 3+ | DataTable, Advanced |
| IGroupedItemsControlObject | Grouped table | 1+ | DataTable |
| IScrollableControlObject | overflow:auto div | 3+ | DataTable |
| IContainerControlObject | div, section, article | 10+ | All pages |
| IExpanderControlObject | details/summary, Accordion | 4+ | Navigation |
| IRefreshableControlObject | Refresh button pattern | 2+ | DataTable |
| ISwipeableControlObject | CSS-based swipe | 2+ | Advanced |
| IImageControlObject | img | 6+ | Dashboard, MediaGallery |
| IProgressControlObject | progress | 3+ | Dashboard, FileUpload |
| IActivityIndicatorControlObject | Spinner | 3+ | Dashboard, MediaGallery |
| IMediaControlObject | video, audio | 2+ | MediaGallery |
| IWebViewControlObject | iframe | 1+ | MediaGallery |
| ITabControlObject | Tab component | 2+ | Dashboard, Navigation |
| IMenuControlObject | nav, NavMenu | 2+ | Navigation |
| IFlyoutControlObject | Dropdown/Popover | 1+ | Navigation |
| IToolbarControlObject | Toolbar | 1+ | Navigation |
| IValidatableControlObject | ValidationMessage | 8+ | Validation |
| **TOTAL** | All Blazor components | **200+** | 8 pages |

---

## Playwright Selectors

```javascript
// By data-automation-id
page.locator('[data-automation-id="BasicInput"]')

// By id
page.locator('#InputPageBasicInput')

// Combined
page.getByTestId('BasicInput')
```

---

## Unit Tests Project

### Project Setup: Brinell.Samples.Blazor.App.Tests

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
    <PackageReference Include="xunit" Version="2.*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.*" />
    <PackageReference Include="bunit" Version="1.*" />
    <PackageReference Include="Moq" Version="4.*" />
    <PackageReference Include="FluentAssertions" Version="6.*" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Brinell.Samples.Blazor.App\Brinell.Samples.Blazor.App.csproj" />
  </ItemGroup>
</Project>
```

### Test Structure

```
Brinell.Samples.Blazor.App.Tests/
├── Components/
│   ├── CounterTests.cs
│   ├── InputControlsTests.cs
│   ├── ToggleControlsTests.cs
│   ├── SelectionControlsTests.cs
│   ├── TableControlsTests.cs
│   ├── ModalControlsTests.cs
│   └── FormValidationTests.cs
├── Services/
│   ├── DataServiceTests.cs
│   └── ValidationServiceTests.cs
├── Models/
│   ├── UserFormModelValidationTests.cs
│   └── DataItemTests.cs
└── Helpers/
    └── TestContext.cs
```

### Critical: bUnit Test Setup

Blazor component tests require bUnit for proper rendering and interaction testing:

```csharp
using Bunit;
using Xunit;

namespace Brinell.Samples.Blazor.App.Tests.Components;

public class CounterTests : TestContext
{
    [Fact]
    public void Counter_InitialValue_IsZero()
    {
        // Arrange & Act
        var cut = RenderComponent<Counter>();
        
        // Assert
        cut.Find("[data-automation-id='CounterValue']")
           .TextContent.Should().Contain("0");
    }

    [Fact]
    public void Counter_ClickIncrement_IncreasesValue()
    {
        // Arrange
        var cut = RenderComponent<Counter>();
        
        // Act
        cut.Find("[data-automation-id='IncrementButton']").Click();
        
        // Assert
        cut.Find("[data-automation-id='CounterValue']")
           .TextContent.Should().Contain("1");
    }
}
```

### Service Tests

```csharp
using Xunit;
using FluentAssertions;
using Brinell.Samples.Blazor.App.Services;

namespace Brinell.Samples.Blazor.App.Tests.Services;

public class DataServiceTests
{
    private readonly DataService _sut;

    public DataServiceTests()
    {
        _sut = new DataService();
    }

    [Fact]
    public async Task GetItemsAsync_ReturnsPagedResults()
    {
        // Act
        var items = await _sut.GetItemsAsync(page: 1, pageSize: 10);
        
        // Assert
        items.Should().HaveCount(10);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsItem()
    {
        // Act
        var item = await _sut.GetByIdAsync(1);
        
        // Assert
        item.Should().NotBeNull();
        item!.Id.Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_AddsNewItem()
    {
        // Arrange
        var newItem = new DataItem { Name = "Test", Email = "test@test.com" };
        var initialCount = await _sut.GetTotalCountAsync();
        
        // Act
        var created = await _sut.CreateAsync(newItem);
        
        // Assert
        created.Id.Should().BeGreaterThan(0);
        (await _sut.GetTotalCountAsync()).Should().Be(initialCount + 1);
    }

    [Fact]
    public async Task DeleteAsync_RemovesItem()
    {
        // Arrange
        var initialCount = await _sut.GetTotalCountAsync();
        
        // Act
        await _sut.DeleteAsync(1);
        
        // Assert
        (await _sut.GetTotalCountAsync()).Should().Be(initialCount - 1);
        (await _sut.GetByIdAsync(1)).Should().BeNull();
    }
}
```

### Model Validation Tests

```csharp
using System.ComponentModel.DataAnnotations;
using Xunit;
using FluentAssertions;
using Brinell.Samples.Blazor.App.Models;

namespace Brinell.Samples.Blazor.App.Tests.Models;

public class UserFormModelValidationTests
{
    private static IList<ValidationResult> ValidateModel(object model)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, context, results, true);
        return results;
    }

    [Fact]
    public void ValidModel_PassesValidation()
    {
        // Arrange
        var model = new UserFormModel
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            Country = "USA",
            AcceptTerms = true
        };
        
        // Act
        var results = ValidateModel(model);
        
        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public void MissingFirstName_FailsValidation()
    {
        // Arrange
        var model = new UserFormModel
        {
            FirstName = "",
            LastName = "Doe",
            Email = "john@example.com",
            Country = "USA",
            AcceptTerms = true
        };
        
        // Act
        var results = ValidateModel(model);
        
        // Assert
        results.Should().Contain(r => r.MemberNames.Contains("FirstName"));
    }

    [Fact]
    public void InvalidEmail_FailsValidation()
    {
        // Arrange
        var model = new UserFormModel
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "not-an-email",
            Country = "USA",
            AcceptTerms = true
        };
        
        // Act
        var results = ValidateModel(model);
        
        // Assert
        results.Should().Contain(r => r.MemberNames.Contains("Email"));
    }

    [Theory]
    [InlineData(17)]
    [InlineData(121)]
    public void InvalidAge_FailsValidation(int age)
    {
        // Arrange
        var model = new UserFormModel
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            Country = "USA",
            AcceptTerms = true,
            Age = age
        };
        
        // Act
        var results = ValidateModel(model);
        
        // Assert
        results.Should().Contain(r => r.MemberNames.Contains("Age"));
    }
}
```

---

## SPEC-006 Locator Integration

### Default Locator Strategy

Blazor pages use `TestId` (data-automation-id) as the default locator strategy:

```csharp
public abstract class BlazorPageBase : PageObjectBase
{
    public override LocatorStrategy DefaultLocatorStrategy => LocatorStrategy.TestId;
}
```

### Page Object Example

```csharp
public class CounterPageObject : BlazorPageBase
{
    // Simple string locators using page default (TestId/data-automation-id)
    public BlazorButton IncrementButton => new("IncrementButton", this);
    public BlazorButton DecrementButton => new("DecrementButton", this);
    public BlazorLabel CounterValue => new("CounterValue", this);
    
    public CounterPageObject(BlazorTestContext context) : base(context) { }
    
    public override void NavigateTo() => _context.Page.GotoAsync("/counter").Wait();
    public override bool IsLoaded(int? timeoutMs = null) => IncrementButton.IsVisible(timeoutMs);
}

public class InputControlsPageObject : BlazorPageBase
{
    public BlazorInput BasicInput => new("BasicInput", this);
    public BlazorInput PasswordInput => new("PasswordInput", this);
    public BlazorTextArea MessageTextArea => new("MessageTextArea", this);
    public BlazorButton SubmitButton => new("SubmitButton", this);
    public BlazorLabel ResultLabel => new("ResultLabel", this);
    
    public InputControlsPageObject(BlazorTestContext context) : base(context) { }
    
    public override void NavigateTo() => _context.Page.GotoAsync("/input-controls").Wait();
    public override bool IsLoaded(int? timeoutMs = null) => BasicInput.IsVisible(timeoutMs);
}
```

### Playwright Selector Mapping

```javascript
// By data-automation-id (TestId strategy)
page.locator('[data-automation-id="BasicInput"]')

// Playwright's built-in test id
page.getByTestId('BasicInput')

// CSS selector
page.locator('#InputPageBasicInput')

// Text content
page.getByText('Submit')

// Role-based
page.getByRole('button', { name: 'Submit' })
```

---

## Deliverables

1. 8 Razor pages (aligned with DES-002c v2.0)
2. 8 shared components (Tabs, Accordion, Toolbar, Flyout, Spinner, Counter, ToggleSwitch, DataTable)
3. Updated NavMenu with all pages
4. 4+ models with validation (UserFormModel, DataItem, MediaItem, FileUploadResult)
5. 2+ services with interfaces (IDataService, IValidationService)
6. Unit test project with 50+ tests (bUnit + xUnit)
7. **200+ unique data-automation-ids**
8. **All 40+ SPEC-006 interfaces represented**:
   - Foundation: IControlObject, IInteractiveControlObject, IFocusableControlObject
   - Input: IClickableControlObject, ITextControlObject, IEditableTextControlObject, ISearchControlObject
   - Toggle: IToggleControlObject, ICheckBoxControlObject, ISwitchControlObject, IRadioButtonControlObject
   - Selection: ISelectorControlObject, IPickerControlObject, IMultiSelectorControlObject
   - Range: ISliderControlObject, IStepperControlObject
   - DateTime: IDateControlObject, ITimeControlObject, IDateTimeControlObject
   - Collection: IItemsControlObject, ISelectableItemsControlObject, IMultiSelectableItemsControlObject, IScrollableItemsControlObject, IGroupedItemsControlObject
   - Container: IContainerControlObject, IScrollableControlObject, IExpanderControlObject, IRefreshableControlObject, ISwipeableControlObject
   - Display: ILabelControlObject, IImageControlObject, IProgressControlObject, IActivityIndicatorControlObject
   - Media: IMediaControlObject, IWebViewControlObject
   - Navigation: ITabControlObject, IMenuControlObject, IFlyoutControlObject, IToolbarControlObject
   - Validation: IValidatableControlObject
9. Page objects using SPEC-006 string locator pattern
