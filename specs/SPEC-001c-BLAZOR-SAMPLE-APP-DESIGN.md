# SPEC-001c-BLAZOR-SAMPLE-APP-DESIGN

**Version:** 1.0  
**Status:** Design  
**Date:** January 2026

---

## Sample Blazor Application

A comprehensive test sample application demonstrating all major Blazor component types and interactions.

---

## Application Structure

### App.razor
```
Router
├── MainLayout (LayoutComponentBase)
│   ├── NavMenu (Sidebar Navigation)
│   └── @Body (Main Content)
├── Pages
│   ├── Dashboard.razor
│   ├── UserForm.razor
│   ├── DataTable.razor
│   ├── FileUpload.razor
│   ├── DynamicContent.razor
│   └── Advanced.razor
└── Shared
    ├── MainLayout.razor
    ├── NavMenu.razor
    └── Components
```

---

## Shared Component: MainLayout

**Purpose:** Main application layout with navigation

**Components:**
- Router (App routing)
- CascadingValue<T> (Theme context)
- HeadOutlet (Document head)
- NavMenu (Navigation sidebar)
  - NavLink (Dashboard)
  - NavLink (User Form)
  - NavLink (Data Table)
  - NavLink (File Upload)
  - NavLink (Dynamic)
  - NavLink (Advanced)
- PageTitle (Dynamic page title)
- @Body (Page content)

---

## Shared Component: NavMenu

**Purpose:** Navigation menu

**Components:**
- div (Nav container)
  - ul (Nav list)
    - li (Nav item)
      - NavLink (href="/" Match="NavLinkMatch.All")
        - span (Dashboard)
      - NavLink (href="/userform")
        - span (User Form)
      - NavLink (href="/datatable")
        - span (Data Table)
      - NavLink (href="/fileupload")
        - span (File Upload)
      - NavLink (href="/dynamic")
        - span (Dynamic)
      - NavLink (href="/advanced")
        - span (Advanced)

---

## Page 1: Dashboard.razor

**Purpose:** Display key information and statistics

**Components:**
- PageTitle (Dashboard)
- h1 (Dashboard Title)
- div (Dashboard Container)
  - div (KPI Grid - CSS Grid 3 columns)
    - div (KPI Card)
      - h3 (KPI 1 Label)
      - p (KPI 1 Value)
    - div (KPI Card)
      - h3 (KPI 2 Label)
      - p (KPI 2 Value)
    - div (KPI Card)
      - h3 (KPI 3 Label)
      - p (KPI 3 Value)

  - div (Status Section)
    - h2 (Status)
    - p (Last Updated: @DateTime.Now)
    - DynamicComponent (Type: StatusIndicator)

  - div (Chart Section)
    - h2 (Chart)
    - p (Chart placeholder)

  - div (Quick Links)
    - NavLink (href="/userform")
      - button (Go to Form)
    - NavLink (href="/datatable")
      - button (View Data)
    - NavLink (href="/fileupload")
      - button (Upload File)

---

## Page 2: UserForm.razor

**Purpose:** Demonstrate form input and validation controls

**Components:**
- PageTitle (User Form)
- h1 (User Form)
- EditForm (Model binding)
  - DataAnnotationsValidator (Validation)
  - ValidationSummary (Error display)

  - div (Form Group)
    - label (First Name)
    - InputText (placeholder="First Name")
    - ValidationMessage(For: p => p.FirstName)

  - div (Form Group)
    - label (Last Name)
    - InputText (placeholder="Last Name")
    - ValidationMessage(For: p => p.LastName)

  - div (Form Group)
    - label (Email)
    - InputText (type="email")
    - ValidationMessage(For: p => p.Email)

  - div (Form Group)
    - label (Phone)
    - InputText (placeholder="Phone")
    - ValidationMessage(For: p => p.Phone)

  - div (Form Group)
    - label (Bio)
    - InputTextArea (rows="5")
    - ValidationMessage(For: p => p.Bio)

  - div (Form Group)
    - label (Country)
    - InputSelect
      - option (value="")
      - option (United States)
      - option (Canada)
      - option (UK)
    - ValidationMessage(For: p => p.Country)

  - div (Form Group)
    - label (Department)
    - InputSelect
      - option (Sales)
      - option (Engineering)
      - option (Marketing)
      - option (HR)
    - ValidationMessage(For: p => p.Department)

  - div (Form Group)
    - label (Birth Date)
    - InputDate (TValue: DateTime)
    - ValidationMessage(For: p => p.BirthDate)

  - div (Form Group)
    - label (Date Range Start)
    - InputDateRange (TValue: DateRange)
    - ValidationMessage(For: p => p.DateRange)

  - div (Form Group)
    - label (Age)
    - InputNumber (TValue: int)
    - ValidationMessage(For: p => p.Age)

  - div (Form Group)
    - label (Salary)
    - InputNumber (TValue: decimal)
    - ValidationMessage(For: p => p.Salary)

  - div (Form Group)
    - label (Newsletter)
    - InputCheckbox
    - ValidationMessage(For: p => p.SubscribeNewsletter)

  - div (Form Group)
    - label (Privacy Policy)
    - InputCheckbox (required)
    - ValidationMessage(For: p => p.AcceptPrivacy)

  - div (Form Group)
    - label (Subscription Tier)
    - InputRadioGroup (TValue: string)
      - InputRadio (value="basic")
        - span (Basic)
      - InputRadio (value="premium")
        - span (Premium)
      - InputRadio (value="enterprise")
        - span (Enterprise)
    - ValidationMessage(For: p => p.SubscriptionTier)

  - div (Form Group)
    - label (Preferences)
    - InputRadioGroup (TValue: string)
      - InputRadio (value="email")
        - span (Email)
      - InputRadio (value="sms")
        - span (SMS)
      - InputRadio (value="push")
        - span (Push)

  - div (Button Group)
    - button (type="submit" Submit)
    - button (type="reset" Clear)
    - button (type="button" Cancel)

  - div (Error Display)
    - @if (showError)
      - div (class="alert alert-danger")
        - p (@errorMessage)

---

## Page 3: DataTable.razor

**Purpose:** Demonstrate data display and interactive controls

**Components:**
- PageTitle (Data Table)
- h1 (Data Grid)

- div (Filter Section)
  - label (Search)
  - InputText (@bind-Value="searchTerm")
  - button (Filter)
  - button (Clear)

- div (Sort Section)
  - label (Sort By)
  - InputSelect (@bind-Value="sortColumn")
    - option (Name)
    - option (Email)
    - option (Status)
  - button (Ascending)
  - button (Descending)

- Virtualize (Items: filteredUsers)
  - table
    - thead
      - tr
        - th (ID)
        - th (Name)
        - th (Email)
        - th (Status)
        - th (Actions)
    - tbody
      - tr (for each user)
        - td (@user.Id)
        - td (@user.Name)
        - td (@user.Email)
        - td (@user.Status)
        - td
          - button (Edit)
          - button (Delete)
          - button (View Details)

- div (Pagination)
  - button (Previous)
  - span (@CurrentPage of @TotalPages)
  - button (Next)

- div (Statistics)
  - p (Total Records: @TotalCount)
  - p (Filtered: @FilteredCount)

---

## Page 4: FileUpload.razor

**Purpose:** Demonstrate file input and upload controls

**Components:**
- PageTitle (File Upload)
- h1 (File Upload Demo)

- div (Upload Form)
  - label (Select File)
  - InputFile (OnChange: OnFileSelected)
  - ValidationMessage (File validation)

  - label (File Description)
  - InputTextArea
  - ValidationMessage

  - label (File Category)
  - InputSelect
    - option (Document)
    - option (Image)
    - option (Video)
    - option (Audio)
    - option (Other)

  - button (Upload)
  - button (Clear)

- @if (uploadProgress > 0)
  - div (Progress Bar)
    - div (Progress Fill - width: @uploadProgress%)

- @if (uploadComplete)
  - div (Success Message)
    - p (File uploaded successfully)
    - p (File Name: @uploadedFileName)
    - p (File Size: @uploadedFileSize bytes)
    - p (Upload Time: @uploadTime)

- div (File List)
  - h3 (Uploaded Files)
  - table
    - thead
      - tr
        - th (File Name)
        - th (Size)
        - th (Type)
        - th (Uploaded)
        - th (Actions)
    - tbody
      - tr (for each file)
        - td (@file.Name)
        - td (@FormatBytes(file.Size))
        - td (@file.Type)
        - td (@file.UploadDate)
        - td
          - button (Download)
          - button (Delete)
          - button (Preview)

---

## Page 5: DynamicContent.razor

**Purpose:** Demonstrate dynamic component rendering

**Components:**
- PageTitle (Dynamic Content)
- h1 (Dynamic Components)

- div (Component Selector)
  - label (Select Component)
  - InputSelect (@bind-Value="selectedComponent")
    - option (StatusCard)
    - option (DataCard)
    - option (FormCard)
    - option (ChartCard)

  - button (Load Component)

- DynamicComponent
  - Type: @GetComponentType(selectedComponent)
  - Parameters: @componentParameters

- div (Component Info)
  - p (@selectedComponent loaded at @DateTime.Now)

- CascadingValue (Value: theme)
  - div (Cascading context available)

---

## Page 6: Advanced.razor

**Purpose:** Demonstrate advanced features and interactions

**Components:**
- PageTitle (Advanced Features)
- h1 (Advanced Features)

- ErrorBoundary (Error handling)
  - div (Content)
    - h2 (Feature 1: Error Handling)
    - button (Trigger Error)

  - ErrorContent (@context)
    - div (Error: @context.Message)

- div (Feature 2: Focus Management)
  - FocusOnNavigate (Restore: true)
  - h2 (Focus on Navigate)
  - p (Focus moved to this section)

- div (Feature 3: Form Events)
  - EditForm (@OnSubmit: HandleSubmit)
    - InputText (@bind-Value="formValue")
    - button (type="submit" Submit)

  - @if (submitMessage != null)
    - div (class="alert")
      - p (@submitMessage)

- div (Feature 4: Virtualization)
  - h2 (Virtual List)
  - Virtualize (Items: largeList, OverscanCount: 3)
    - div (@item)

- div (Feature 5: HeadContent)
  - HeadContent
    - meta (name="description")
    - meta (name="keywords")

- div (Feature 6: Custom Validation)
  - EditForm (@OnValidSubmit: ValidateCustom)
    - InputText (@bind-Value="customValue")
    - CustomValidation (TValue: string)
    - ValidationMessage
    - button (Validate)

---

## Shared Components

### StatusIndicator.razor
```csharp
@implements IComponent
@if (IsLoading)
  <span>Loading...</span>
@else if (IsError)
  <span class="error">@ErrorMessage</span>
@else if (IsSuccess)
  <span class="success">@SuccessMessage</span>
```

### DataCard.razor
```csharp
<div class="card">
  <div class="card-header">@Title</div>
  <div class="card-body">@ChildContent</div>
  <div class="card-footer">@Footer</div>
</div>
```

### FormCard.razor
```csharp
<EditForm Model="@Model">
  @ChildContent
</EditForm>
```

---

## Component Count Summary

| Category | Count | Components |
|----------|-------|----------|
| Layout | 5+ | MainLayout, NavMenu, Router, PageTitle, CascadingValue |
| Form | 15+ | EditForm, InputText, InputTextArea, InputNumber, InputCheckbox, InputRadio, InputSelect, InputDate, InputDateRange, InputFile, ValidationMessage, ValidationSummary, DataAnnotationsValidator, CustomValidation |
| Navigation | 3 | Router, NavLink, RouteView |
| Utility | 8+ | DynamicComponent, Virtualize, HeadContent, PageTitle, ErrorBoundary, FocusOnNavigate, CascadingValue, CascadingParameter |
| Display | 3 | Tables, Lists, Cards |
| Validation | 4 | DataAnnotationsValidator, ValidationMessage, ValidationSummary, CustomValidation |
| Custom | 4 | StatusIndicator, DataCard, FormCard |
| **TOTAL** | **36+** | All major Blazor components |

---

## Test Scenarios

### Form Input Testing
- Text input (Entry)
- Multi-line text (TextArea)
- Number input (InputNumber)
- Date input (InputDate)
- Date range input (InputDateRange)
- File input (InputFile)
- Input validation

### Selection Testing
- Single select (InputSelect)
- Multiple select (CheckBox)
- Radio button selection
- Radio group selection
- Clear selection

### Validation Testing
- Required field validation
- Pattern validation
- Length validation
- Custom validation
- Error display
- Validation message

### Navigation Testing
- NavLink navigation
- Route parameters
- Page navigation
- Focus on navigate

### Dynamic Content Testing
- Dynamic component loading
- Component parameter passing
- Cascading values
- Component unload

### File Upload Testing
- File selection
- File upload
- Progress tracking
- File listing
- File operations

### Data Display Testing
- Table rendering
- List virtualization
- Pagination
- Sorting
- Filtering
- Search

### Advanced Testing
- Error boundary
- Validation handling
- Form submission
- Cascading parameters
- Component lifecycle

---

**Last Updated:** January 3, 2026
