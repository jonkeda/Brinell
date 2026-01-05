# PLAN: MAUI Sample App Implementation

**Date:** January 2026  
**Status:** Ready for Implementation  
**Reference:** SPEC-004, SPEC-006

---

## Objective

Create a comprehensive MAUI sample app that demonstrates all 150+ controls from SPEC-006 for UI test automation, using MVVM pattern with `Brinell.Samples.Shared` infrastructure.

**Reference Design:** DES-001c-MAUI-SAMPLE-APP-DESIGN.md (Version 2.0)

**SPEC-006 Interface Coverage:** 40+ interfaces including Foundation, Input, Toggle, Selection, Range, DateTime, Collection, Container, Display, Media, Navigation, and Validation.

---

## Current State

### Existing Files
- MainPage.xaml - Counter, text input, toggles, slider, picker, activity indicator
- Brinell.Samples.Shared - ViewModelBase, AsyncRelayCommand, IViewVisible

### Empty Folders (to be populated)
- Models/
- ViewModels/
- Pages/

---

## MVVM Architecture

### Shared Infrastructure (Brinell.Samples.Shared)

```
Brinell.Samples.Shared/
├── ViewModels/
│   ├── ViewModelBase.cs         # INotifyPropertyChanged, IViewVisible, IsBusy
│   ├── IViewVisible.cs          # View visibility interface
│   └── ICurrentViewModelContainer.cs
├── Commands/
│   ├── AsyncRelayCommand.cs     # Async command with busy tracking
│   ├── AsyncRelayCommand<T>.cs  # Generic async command
│   ├── RelayCommand.cs          # Sync command
│   └── IAsyncRelayCommand.cs    # Interfaces
└── Navigation/
    └── (navigation services)
```

### App-Specific Implementation

```
Brinell.Samples.Maui.App/
├── Models/
│   ├── UserProfile.cs
│   ├── FormData.cs
│   ├── SelectionItem.cs
│   ├── ValidationResult.cs
│   └── SampleDataItem.cs
├── ViewModels/
│   ├── DashboardViewModel.cs        # Tabs, KPIs, progress, loading
│   ├── UserFormViewModel.cs         # Form data, toggles, selections, ranges
│   ├── DataGridViewModel.cs         # Collections, selection, grouping
│   ├── MediaGalleryViewModel.cs     # Images, media, web content
│   ├── NavigationDemoViewModel.cs   # Expanders, navigation actions
│   ├── ValidationViewModel.cs       # Form validation logic
│   └── AdvancedViewModel.cs         # Gesture handling, swipe actions
├── Pages/
│   ├── DashboardPage.xaml          # Tabs, Progress, ActivityIndicator, RefreshView
│   ├── UserFormPage.xaml            # Entry, Editor, SearchBar, Picker, DatePicker, TimePicker, Switch, CheckBox, RadioButton, Slider, Stepper
│   ├── DataGridPage.xaml            # CollectionView, ListView, SwipeView, GroupedList, Multi-select
│   ├── MediaGalleryPage.xaml        # Image, MediaElement, WebView
│   ├── NavigationDemoPage.xaml      # Expander, Flyout, Toolbar, Menu
│   ├── ValidationPage.xaml          # Entry with validation, error messages
│   └── AdvancedPage.xaml            # Gestures, SwipeView, Containers
└── Converters/
    └── (value converters)
```

---

## Models

### UserProfile.cs

```csharp
namespace Brinell.Samples.Maui.App.Models;

public class UserProfile
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; } = DateTime.Today.AddYears(-25);
    public TimeSpan PreferredTime { get; set; } = new TimeSpan(9, 0, 0);
    public string Country { get; set; } = string.Empty;
    public bool SubscribeNewsletter { get; set; }
    public bool AcceptTerms { get; set; }
    public string SubscriptionTier { get; set; } = "Basic";
}
```

### SelectionItem.cs

```csharp
namespace Brinell.Samples.Maui.App.Models;

public class SelectionItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
}
```

### SampleDataItem.cs

```csharp
namespace Brinell.Samples.Maui.App.Models;

public class SampleDataItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
```

### ValidationResult.cs

```csharp
namespace Brinell.Samples.Maui.App.Models;

public class ValidationResult
{
    public bool IsValid { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}
```

---

## ViewModels

### MainPageViewModel.cs (Example)

```csharp
using Brinell.Samples.Shared.ViewModels;
using Brinell.Samples.Shared.Commands;

namespace Brinell.Samples.Maui.App.ViewModels;

public class MainPageViewModel : ViewModelBase
{
    private int _counter;
    private string _name = string.Empty;
    private string _greeting = string.Empty;
    private double _volume = 50;
    private bool _notificationsEnabled = true;
    private bool _isLoading;

    public int Counter
    {
        get => _counter;
        set => SetProperty(ref _counter, value);
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string Greeting
    {
        get => _greeting;
        set => SetProperty(ref _greeting, value);
    }

    public double Volume
    {
        get => _volume;
        set => SetProperty(ref _volume, value);
    }

    public bool NotificationsEnabled
    {
        get => _notificationsEnabled;
        set => SetProperty(ref _notificationsEnabled, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    // Commands
    public IAsyncRelayCommand IncrementCommand { get; }
    public IAsyncRelayCommand DecrementCommand { get; }
    public IAsyncRelayCommand ResetCommand { get; }
    public IAsyncRelayCommand GreetCommand { get; }
    public IAsyncRelayCommand ToggleLoadingCommand { get; }

    public MainPageViewModel()
    {
        IncrementCommand = new AsyncRelayCommand(this, () => { Counter++; return Task.CompletedTask; });
        DecrementCommand = new AsyncRelayCommand(this, () => { Counter--; return Task.CompletedTask; });
        ResetCommand = new AsyncRelayCommand(this, () => { Counter = 0; return Task.CompletedTask; });
        GreetCommand = new AsyncRelayCommand(this, GreetAsync);
        ToggleLoadingCommand = new AsyncRelayCommand(this, ToggleLoadingAsync);
    }

    private async Task GreetAsync()
    {
        Greeting = string.IsNullOrWhiteSpace(Name) 
            ? "Please enter your name" 
            : $"Hello, {Name}!";
        await Task.CompletedTask;
    }

    private async Task ToggleLoadingAsync()
    {
        IsLoading = !IsLoading;
        await Task.CompletedTask;
    }
}
```

---

## Pages to Create (Aligned with DES-001c v2.0)

### 1. DashboardPage.xaml

**SPEC-006 Interfaces:** ITabControlObject, IProgressControlObject, IActivityIndicatorControlObject, IRefreshableControlObject, IClickableControlObject, ILabelControlObject, IImageControlObject

| Control | AutomationId | SPEC-006 Interface |
|---------|--------------|-------------------|
| TabbedPage | DashboardTabs | ITabControlObject |
| Label | DashboardTitleLabel | ILabelControlObject |
| Image | DashboardLogoImage | IImageControlObject |
| ProgressBar | DashboardLoadProgress | IProgressControlObject |
| ProgressBar | DashboardIndeterminateProgress | IProgressControlObject |
| ActivityIndicator | DashboardLoadingIndicator | IActivityIndicatorControlObject |
| Button | DashboardRefreshButton | IClickableControlObject |
| RefreshView | DashboardRefreshView | IRefreshableControlObject |
| CollectionView | DashboardStatusList | IItemsControlObject |
| Label | DashboardKpi1Value | ILabelControlObject |
| Label | DashboardLastUpdated | ILabelControlObject |

### 2. UserFormPage.xaml

**SPEC-006 Interfaces:** ITextControlObject, IEditableTextControlObject, ISearchControlObject, IPickerControlObject, IDateControlObject, ITimeControlObject, ISwitchControlObject, ICheckBoxControlObject, IRadioButtonControlObject, ISliderControlObject, IStepperControlObject

| Control | AutomationId | SPEC-006 Interface |
|---------|--------------|-------------------|
| Entry | FormFirstNameEntry | ITextControlObject |
| Entry | FormEmailEntry | ITextControlObject |
| Entry | FormPasswordEntry | ITextControlObject |
| SearchBar | FormSearchBar | ISearchControlObject |
| Editor | FormBioEditor | IEditableTextControlObject |
| Picker | FormCountryPicker | IPickerControlObject |
| DatePicker | FormBirthDatePicker | IDateControlObject |
| TimePicker | FormContactTimePicker | ITimeControlObject |
| Switch | FormNewsletterSwitch | ISwitchControlObject |
| CheckBox | FormTermsCheckBox | ICheckBoxControlObject |
| CheckBox | FormIndeterminateCheckBox | ICheckBoxControlObject |
| RadioButton | FormTierBasicRadio | IRadioButtonControlObject |
| RadioButton | FormTierPremiumRadio | IRadioButtonControlObject |
| Slider | FormFontSizeSlider | ISliderControlObject |
| Slider | FormVolumeSlider | ISliderControlObject |
| Stepper | FormQuantityStepper | IStepperControlObject |
| Button | FormSubmitButton | IClickableControlObject |
| ImageButton | FormImageButton | IClickableControlObject |

### 3. DataGridPage.xaml

**SPEC-006 Interfaces:** IItemsControlObject, ISelectableItemsControlObject, IMultiSelectableItemsControlObject, IScrollableItemsControlObject, IGroupedItemsControlObject, ISwipeableControlObject, IRefreshableControlObject, ISearchControlObject

| Control | AutomationId | SPEC-006 Interface |
|---------|--------------|-------------------|
| SearchBar | GridSearchBar | ISearchControlObject |
| RefreshView | GridRefreshView | IRefreshableControlObject |
| ListView | GridGroupedList | IGroupedItemsControlObject |
| CollectionView | GridSingleSelectList | ISelectableItemsControlObject |
| CollectionView | GridMultiSelectList | IMultiSelectableItemsControlObject |
| CollectionView | GridScrollableList | IScrollableItemsControlObject |
| CarouselView | GridCarousel | IItemsControlObject |
| SwipeView | GridSwipeItem1 | ISwipeableControlObject |
| Button | GridSelectAllButton | IClickableControlObject |
| Button | GridUnselectAllButton | IClickableControlObject |
| Label | GridSelectedCountLabel | ILabelControlObject |

### 4. MediaGalleryPage.xaml

**SPEC-006 Interfaces:** IImageControlObject, IMediaControlObject, IWebViewControlObject, IClickableControlObject, ISliderControlObject, ISwitchControlObject

| Control | AutomationId | SPEC-006 Interface |
|---------|--------------|-------------------|
| Image | MediaLocalImage | IImageControlObject |
| Image | MediaRemoteImage | IImageControlObject |
| ImageButton | MediaThumbnail1 | IClickableControlObject |
| ImageButton | MediaThumbnail2 | IClickableControlObject |
| Image | MediaFullSizeImage | IImageControlObject |
| MediaElement | MediaVideoPlayer | IMediaControlObject |
| Button | MediaPlayButton | IClickableControlObject |
| Button | MediaPauseButton | IClickableControlObject |
| Slider | MediaSeekSlider | ISliderControlObject |
| Slider | MediaVolumeSlider | ISliderControlObject |
| Switch | MediaMuteSwitch | ISwitchControlObject |
| WebView | MediaWebView | IWebViewControlObject |
| Button | MediaWebBackButton | IClickableControlObject |
| Entry | MediaWebUrlEntry | ITextControlObject |
| ActivityIndicator | MediaWebLoadingIndicator | IActivityIndicatorControlObject |

### 5. NavigationDemoPage.xaml

**SPEC-006 Interfaces:** IToolbarControlObject, IExpanderControlObject, IFlyoutControlObject, IMenuControlObject, IClickableControlObject

| Control | AutomationId | SPEC-006 Interface |
|---------|--------------|-------------------|
| Button | NavPushPageButton | IClickableControlObject |
| Button | NavPopPageButton | IClickableControlObject |
| Button | NavModalPageButton | IClickableControlObject |
| ToolbarItem | NavToolbarSave | IToolbarControlObject |
| ToolbarItem | NavToolbarEdit | IToolbarControlObject |
| ToolbarItem | NavToolbarDelete | IToolbarControlObject |
| Button | NavOpenFlyoutButton | IClickableControlObject |
| Expander | NavExpander1 | IExpanderControlObject |
| Expander | NavExpander2 | IExpanderControlObject |
| Expander | NavExpander3 | IExpanderControlObject |
| Button | NavExpandAllButton | IClickableControlObject |
| Button | NavCollapseAllButton | IClickableControlObject |

### 6. ValidationPage.xaml

**SPEC-006 Interfaces:** IValidatableControlObject, ITextControlObject, IClickableControlObject, ILabelControlObject

| Control | AutomationId | SPEC-006 Interface |
|---------|--------------|-------------------|
| Entry | ValidationRequiredEntry | IValidatableControlObject |
| Entry | ValidationEmailEntry | IValidatableControlObject |
| Entry | ValidationPhoneEntry | IValidatableControlObject |
| Entry | ValidationMinLengthEntry | IValidatableControlObject |
| Entry | ValidationMaxLengthEntry | IValidatableControlObject |
| Entry | ValidationRangeEntry | IValidatableControlObject |
| Entry | ValidationRegexEntry | IValidatableControlObject |
| Label | ValidationRequiredError | ILabelControlObject |
| Label | ValidationEmailError | ILabelControlObject |
| Label | ValidationSummary | ILabelControlObject |
| Button | ValidationSubmitButton | IClickableControlObject |
| Button | ValidationClearButton | IClickableControlObject |
| Label | ValidationSuccessLabel | ILabelControlObject |

### 7. AdvancedPage.xaml

**SPEC-006 Interfaces:** ISwipeableControlObject, IContainerControlObject, IControlObject (gestures)

| Control | AutomationId | SPEC-006 Interface |
|---------|--------------|-------------------|
| Frame | AdvancedTapFrame | IControlObject |
| Frame | AdvancedPanFrame | IControlObject |
| Frame | AdvancedPinchFrame | IControlObject |
| Image | AdvancedPinchImage | IImageControlObject |
| SwipeView | AdvancedSwipeView | ISwipeableControlObject |
| Border | AdvancedBorder1 | IContainerControlObject |
| Border | AdvancedBorder2 | IContainerControlObject |
| ContentView | AdvancedContentView | IContainerControlObject |
| Label | AdvancedTapCountLabel | ILabelControlObject |

### 8. DisplayControlsPage.xaml

| Control | AutomationId | Purpose |
|---------|--------------|---------|
| Label | PlainLabel | Plain text |
| Label | FormattedLabel | Formatted text |
| Label | TruncatedLabel | Truncation test |
| Image | LocalImage | Local image |
| Image | RemoteImage | Remote URL image |
| WebView | WebContent | Web content |

### 9. NavigationPage.xaml

| Control | AutomationId | Purpose |
|---------|--------------|---------|
| Button | NavigateButton | Navigate to page |
| Button | BackButton | Go back |
| Button | PopToRootButton | Pop to root |
| ToolbarItem | ToolbarAction | Toolbar action |
| ToolbarItem | ToolbarMenu | Toolbar menu |

### 10. ValidationPage.xaml

| Control | AutomationId | Purpose |
|---------|--------------|---------|
| Entry | RequiredEntry | Required validation |
| Entry | EmailEntry | Email validation |
| Entry | PhoneEntry | Phone validation |
| Entry | RangeEntry | Range validation |
| Label | ErrorLabel | Error message |
| Button | ValidateButton | Trigger validation |
| Button | ClearErrorsButton | Clear validation |

---

## App Structure

```
Brinell.Samples.Maui.App/
├── App.xaml
├── App.xaml.cs
├── MauiProgram.cs
├── AppShell.xaml              # Shell with Flyout navigation
├── AppShell.xaml.cs
├── Pages/
│   ├── DashboardPage.xaml     # Tabs, Progress, ActivityIndicator
│   ├── UserFormPage.xaml      # All input, toggle, selection, range controls
│   ├── DataGridPage.xaml      # Collections, selection, grouping, swipe
│   ├── MediaGalleryPage.xaml  # Image, MediaElement, WebView
│   ├── NavigationDemoPage.xaml # Expander, Flyout, Toolbar
│   ├── ValidationPage.xaml    # Form validation
│   └── AdvancedPage.xaml      # Gestures, SwipeView, Containers
├── ViewModels/
│   ├── DashboardViewModel.cs
│   ├── UserFormViewModel.cs
│   ├── DataGridViewModel.cs
│   ├── MediaGalleryViewModel.cs
│   ├── NavigationDemoViewModel.cs
│   ├── ValidationViewModel.cs
│   └── AdvancedViewModel.cs
├── Models/
│   ├── UserProfile.cs
│   ├── SelectionItem.cs
│   ├── SampleDataItem.cs
│   ├── ValidationResult.cs
│   └── MediaItem.cs
└── Resources/
    └── Images/
```

---

## Implementation Tasks

### Phase 1: Models

1. Create UserProfile.cs
2. Create SelectionItem.cs
3. Create SampleDataItem.cs
4. Create ValidationResult.cs
5. Create FormData.cs

### Phase 2: ViewModels

1. Create MainPageViewModel.cs (update existing MainPage)
2. Create InputControlsViewModel.cs
3. Create ToggleControlsViewModel.cs
4. Create SelectionControlsViewModel.cs
5. Create RangeControlsViewModel.cs
6. Create CollectionControlsViewModel.cs
7. Create ContainerControlsViewModel.cs
8. Create DisplayControlsViewModel.cs
9. Create NavigationViewModel.cs
10. Create ValidationViewModel.cs

### Phase 3: Setup Shell Navigation

1. Create AppShell.xaml with flyout menu
2. Register all pages in Shell
3. Add navigation tabs

### Phase 4: Create Pages (Priority Order)

1. DashboardPage - Tabs, Progress, ActivityIndicator, RefreshView (ITabControlObject, IProgressControlObject)
2. UserFormPage - All input, toggle, selection, range, date/time controls (ITextControlObject, IPickerControlObject, IDateControlObject, ITimeControlObject, ISwitchControlObject, ICheckBoxControlObject, IRadioButtonControlObject, ISliderControlObject, IStepperControlObject, ISearchControlObject)
3. DataGridPage - Collections, selection, grouping, swipe (IItemsControlObject, ISelectableItemsControlObject, IMultiSelectableItemsControlObject, IScrollableItemsControlObject, IGroupedItemsControlObject, ISwipeableControlObject)
4. MediaGalleryPage - Image, MediaElement, WebView (IImageControlObject, IMediaControlObject, IWebViewControlObject)
5. NavigationDemoPage - Expander, Flyout, Toolbar, Menu (IExpanderControlObject, IFlyoutControlObject, IToolbarControlObject, IMenuControlObject)
6. ValidationPage - Form validation (IValidatableControlObject)
7. AdvancedPage - Gestures, SwipeView, Containers (IContainerControlObject, ISwipeableControlObject)

### Phase 5: Unit Tests

1. Create Brinell.Samples.Maui.App.Tests project (extract to testable assembly)
2. Create ViewModel tests for each ViewModel
3. Create Model tests
4. Create AsyncRelayCommand tests
5. Create ViewModelBase tests
6. Ensure 80%+ coverage on ViewModels

### Phase 6: Styling

1. Apply consistent AutomationId naming
2. Add semantic properties
3. Ensure accessibility

---

## AutomationId Convention

```
<PagePrefix><ControlType><Purpose>

Examples:
- MainPageTitleLabel
- InputPagePasswordEntry
- TogglePageNotificationSwitch
- SelectionPageColorPicker
- RangePageVolumeSlider
- CollectionPageItemsList
```

---

## Test Coverage Goals (SPEC-006 Aligned)

| SPEC-006 Interface | Control Type | Count | Pages |
|--------------------|--------------|-------|-------|
| IClickableControlObject | Button, ImageButton | 25+ | All pages |
| ITextControlObject | Entry | 12+ | UserForm, Validation |
| IEditableTextControlObject | Editor | 2+ | UserForm |
| ISearchControlObject | SearchBar | 2+ | UserForm, DataGrid |
| ILabelControlObject | Label | 35+ | All pages |
| ISwitchControlObject | Switch | 4+ | UserForm, MediaGallery |
| ICheckBoxControlObject | CheckBox | 3+ | UserForm |
| IRadioButtonControlObject | RadioButton | 6+ | UserForm |
| IPickerControlObject | Picker | 4+ | UserForm |
| IDateControlObject | DatePicker | 2+ | UserForm |
| ITimeControlObject | TimePicker | 2+ | UserForm |
| ISliderControlObject | Slider | 5+ | UserForm, MediaGallery |
| IStepperControlObject | Stepper | 2+ | UserForm |
| IProgressControlObject | ProgressBar | 2+ | Dashboard |
| IActivityIndicatorControlObject | ActivityIndicator | 2+ | Dashboard, MediaGallery |
| IItemsControlObject | CollectionView, CarouselView | 5+ | DataGrid |
| ISelectableItemsControlObject | CollectionView (Single) | 2+ | DataGrid |
| IMultiSelectableItemsControlObject | CollectionView (Multiple) | 2+ | DataGrid |
| IScrollableItemsControlObject | CollectionView | 2+ | DataGrid |
| IGroupedItemsControlObject | ListView (grouped) | 1+ | DataGrid |
| IScrollableControlObject | ScrollView | 3+ | UserForm, DataGrid |
| IContainerControlObject | Frame, Border, ContentView | 8+ | All pages |
| IExpanderControlObject | Expander | 3+ | NavigationDemo |
| IRefreshableControlObject | RefreshView | 2+ | Dashboard, DataGrid |
| ISwipeableControlObject | SwipeView | 4+ | DataGrid, Advanced |
| IImageControlObject | Image | 6+ | Dashboard, MediaGallery |
| IMediaControlObject | MediaElement | 1+ | MediaGallery |
| IWebViewControlObject | WebView | 1+ | MediaGallery |
| ITabControlObject | TabbedPage | 1+ | Dashboard |
| IToolbarControlObject | ToolbarItem | 4+ | NavigationDemo |
| IValidatableControlObject | Entry with validation | 7+ | Validation |
| **TOTAL** | All MAUI controls | **150+** | 7 pages |

---

## Unit Tests Project

### Project Setup: Brinell.Samples.Maui.App.Tests

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
    <PackageReference Include="Moq" Version="4.*" />
    <PackageReference Include="FluentAssertions" Version="6.*" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Brinell.Samples.Shared\Brinell.Samples.Shared.csproj" />
    <!-- Note: Cannot reference MAUI app directly - extract testable logic -->
  </ItemGroup>
</Project>
```

### Critical: Platform-Specific Test Initialization

MAUI projects require special handling to avoid compile errors when testing ViewModels that reference MAUI types.

**Option 1: Extract ViewModels to Shared Project**

Move ViewModels to `Brinell.Samples.Shared` so they don't depend on MAUI types.

**Option 2: Use Conditional Compilation**

```csharp
#if !UNIT_TEST
using Microsoft.Maui.Controls;
#endif
```

**Option 3: Abstraction Layer**

```csharp
// INavigationService in Shared project
public interface INavigationService
{
    Task NavigateToAsync(string route);
    Task GoBackAsync();
}

// MAUI implementation in App
public class MauiNavigationService : INavigationService
{
    public Task NavigateToAsync(string route) => Shell.Current.GoToAsync(route);
    public Task GoBackAsync() => Shell.Current.GoToAsync("..");
}
```

### Test Structure

```
Brinell.Samples.Maui.App.Tests/
├── ViewModels/
│   ├── MainPageViewModelTests.cs
│   ├── InputControlsViewModelTests.cs
│   ├── ToggleControlsViewModelTests.cs
│   ├── SelectionControlsViewModelTests.cs
│   ├── RangeControlsViewModelTests.cs
│   ├── CollectionControlsViewModelTests.cs
│   └── ValidationViewModelTests.cs
├── Models/
│   ├── UserProfileTests.cs
│   ├── SelectionItemTests.cs
│   └── ValidationResultTests.cs
├── Commands/
│   ├── AsyncRelayCommandTests.cs
│   └── RelayCommandTests.cs
└── Helpers/
    └── TestHelpers.cs
```

### Sample ViewModel Tests

```csharp
using Xunit;
using FluentAssertions;
using Brinell.Samples.Maui.App.ViewModels;

namespace Brinell.Samples.Maui.App.Tests.ViewModels;

public class MainPageViewModelTests
{
    [Fact]
    public void Counter_InitialValue_IsZero()
    {
        // Arrange
        var vm = new MainPageViewModel();
        
        // Assert
        vm.Counter.Should().Be(0);
    }

    [Fact]
    public async Task IncrementCommand_Execute_IncrementsCounter()
    {
        // Arrange
        var vm = new MainPageViewModel();
        
        // Act
        await vm.IncrementCommand.ExecuteAsync(null);
        
        // Assert
        vm.Counter.Should().Be(1);
    }

    [Fact]
    public async Task DecrementCommand_Execute_DecrementsCounter()
    {
        // Arrange
        var vm = new MainPageViewModel { Counter = 5 };
        
        // Act
        await vm.DecrementCommand.ExecuteAsync(null);
        
        // Assert
        vm.Counter.Should().Be(4);
    }

    [Fact]
    public async Task ResetCommand_Execute_ResetsCounterToZero()
    {
        // Arrange
        var vm = new MainPageViewModel { Counter = 10 };
        
        // Act
        await vm.ResetCommand.ExecuteAsync(null);
        
        // Assert
        vm.Counter.Should().Be(0);
    }

    [Fact]
    public async Task GreetCommand_WithName_SetsGreeting()
    {
        // Arrange
        var vm = new MainPageViewModel { Name = "Alice" };
        
        // Act
        await vm.GreetCommand.ExecuteAsync(null);
        
        // Assert
        vm.Greeting.Should().Be("Hello, Alice!");
    }

    [Fact]
    public async Task GreetCommand_WithoutName_SetsPromptMessage()
    {
        // Arrange
        var vm = new MainPageViewModel { Name = "" };
        
        // Act
        await vm.GreetCommand.ExecuteAsync(null);
        
        // Assert
        vm.Greeting.Should().Be("Please enter your name");
    }

    [Fact]
    public void PropertyChanged_Counter_RaisesEvent()
    {
        // Arrange
        var vm = new MainPageViewModel();
        var raised = false;
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainPageViewModel.Counter))
                raised = true;
        };
        
        // Act
        vm.Counter = 5;
        
        // Assert
        raised.Should().BeTrue();
    }
}
```

### AsyncRelayCommand Tests

```csharp
using Xunit;
using FluentAssertions;
using Brinell.Samples.Shared.Commands;
using Brinell.Samples.Shared.ViewModels;

namespace Brinell.Samples.Maui.App.Tests.Commands;

public class AsyncRelayCommandTests
{
    private class TestViewModel : ViewModelBase { }

    [Fact]
    public async Task ExecuteAsync_CallsExecuteDelegate()
    {
        // Arrange
        var executed = false;
        var command = new AsyncRelayCommand(() => { executed = true; return Task.CompletedTask; });
        
        // Act
        await command.ExecuteAsync(null);
        
        // Assert
        executed.Should().BeTrue();
    }

    [Fact]
    public void CanExecute_WhenRunning_ReturnsFalse()
    {
        // Arrange
        var tcs = new TaskCompletionSource();
        var command = new AsyncRelayCommand(() => tcs.Task);
        
        // Act - start execution but don't complete
        _ = command.ExecuteAsync(null);
        
        // Assert
        command.CanExecute(null).Should().BeFalse();
        
        // Cleanup
        tcs.SetResult();
    }

    [Fact]
    public async Task ExecuteAsync_WithViewModel_TracksBusy()
    {
        // Arrange
        var vm = new TestViewModel();
        var busyDuringExecution = false;
        var command = new AsyncRelayCommand(vm, async () =>
        {
            busyDuringExecution = vm.IsBusy;
            await Task.Delay(10);
        });
        
        // Act
        await command.ExecuteAsync(null);
        
        // Assert
        busyDuringExecution.Should().BeTrue();
        vm.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_WithViewNotVisible_DoesNotExecute()
    {
        // Arrange
        var vm = new TestViewModel { ViewVisible = false };
        var executed = false;
        var command = new AsyncRelayCommand(vm, () => { executed = true; return Task.CompletedTask; });
        
        // Act
        await command.ExecuteAsync(null);
        
        // Assert
        executed.Should().BeFalse();
    }
}
```

---

## SPEC-006 Locator Integration

### Default Locator Strategy

MAUI pages use `AutomationId` as the default locator strategy:

```csharp
public abstract class MauiPageBase : PageObjectBase
{
    public override LocatorStrategy DefaultLocatorStrategy => LocatorStrategy.AutomationId;
}
```

### Page Object Example

```csharp
public class MainPageObject : MauiPageBase
{
    // Simple string locators using page default (AutomationId)
    public MauiButton IncrementButton => new("IncrementButton", this);
    public MauiButton DecrementButton => new("DecrementButton", this);
    public MauiButton ResetButton => new("ResetButton", this);
    public MauiLabel CounterLabel => new("CounterLabel", this);
    public MauiEntry NameEntry => new("NameEntry", this);
    public MauiLabel GreetingLabel => new("GreetingLabel", this);
    public MauiButton GreetButton => new("GreetButton", this);
    public MauiSwitch NotificationSwitch => new("NotificationSwitch", this);
    public MauiSlider VolumeSlider => new("VolumeSlider", this);
    public MauiProgressBar VolumeProgress => new("VolumeProgress", this);
    
    public MainPageObject(MauiTestContext context) : base(context) { }
    
    public override void NavigateTo() => _context.LaunchApp();
    public override bool IsLoaded(int? timeoutMs = null) => IncrementButton.IsVisible(timeoutMs);
}
```

---

## Deliverables

1. 7 XAML pages with code-behind (aligned with DES-001c v2.0)
2. 7 ViewModels using Shared infrastructure
3. 5+ Models (UserProfile, SelectionItem, SampleDataItem, ValidationResult, MediaItem)
4. AppShell with Flyout navigation
5. **150+ unique AutomationIds**
6. Unit test project with 50+ tests
7. **All 40+ SPEC-006 interfaces represented**:
   - Foundation: IControlObject, IInteractiveControlObject, IFocusableControlObject
   - Input: IClickableControlObject, ITextControlObject, IEditableTextControlObject, ISearchControlObject
   - Toggle: ISwitchControlObject, ICheckBoxControlObject, IRadioButtonControlObject
   - Selection: IPickerControlObject, IMultiSelectorControlObject
   - Range: ISliderControlObject, IStepperControlObject
   - DateTime: IDateControlObject, ITimeControlObject
   - Collection: IItemsControlObject, ISelectableItemsControlObject, IMultiSelectableItemsControlObject, IScrollableItemsControlObject, IGroupedItemsControlObject
   - Container: IContainerControlObject, IScrollableControlObject, IExpanderControlObject, IRefreshableControlObject, ISwipeableControlObject
   - Display: ILabelControlObject, IImageControlObject, IProgressControlObject, IActivityIndicatorControlObject
   - Media: IMediaControlObject, IWebViewControlObject
   - Navigation: ITabControlObject, IMenuControlObject, IFlyoutControlObject, IToolbarControlObject
   - Validation: IValidatableControlObject
8. Page objects using SPEC-006 string locator pattern
