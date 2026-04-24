# Phase 9 — Unit Tests

## Goal

Comprehensive unit test coverage for all non-UI services and MVVM infrastructure.

## Tasks

### 9.1 — Unit Tests for `ViewModelBase`

Test `SetProperty`, `OnPropertyChanged`, and equality checks.

**Implementation (xUnit):**

```csharp
public class ViewModelBaseTests
{
    private class TestViewModel : ViewModelBase
    {
        private string _name = "";
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private int _count;
        public int Count
        {
            get => _count;
            set => SetProperty(ref _count, value);
        }
    }

    [Fact]
    public void SetProperty_RaisesPropertyChanged_WhenValueChanges()
    {
        var vm = new TestViewModel();
        var raised = false;
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(TestViewModel.Name)) raised = true;
        };

        vm.Name = "Test";

        Assert.True(raised);
    }

    [Fact]
    public void SetProperty_DoesNotRaise_WhenValueUnchanged()
    {
        var vm = new TestViewModel { Name = "Test" };
        var raised = false;
        vm.PropertyChanged += (_, _) => raised = true;

        vm.Name = "Test";

        Assert.False(raised);
    }

    [Fact]
    public void SetProperty_ReturnsTrue_WhenValueChanges()
    {
        var vm = new TestViewModel();
        // SetProperty is protected, so we verify via the property setter behavior
        vm.Name = "New";
        Assert.Equal("New", vm.Name);
    }

    [Fact]
    public void SetProperty_ReturnsFalse_WhenValueUnchanged()
    {
        var vm = new TestViewModel { Name = "Same" };
        var changeCount = 0;
        vm.PropertyChanged += (_, _) => changeCount++;

        vm.Name = "Same";

        Assert.Equal(0, changeCount);
    }

    [Fact]
    public void SetProperty_WorksWithValueTypes()
    {
        var vm = new TestViewModel();
        var raised = false;
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(TestViewModel.Count)) raised = true;
        };

        vm.Count = 42;

        Assert.True(raised);
        Assert.Equal(42, vm.Count);
    }
}
```

---

### 9.2 — Unit Tests for `RelayCommand` / `AsyncRelayCommand`

Test execute, can-execute, cancellation.

**Implementation (xUnit):**

```csharp
public class RelayCommandTests
{
    [Fact]
    public void Execute_CallsAction()
    {
        var called = false;
        var command = new RelayCommand(() => called = true);

        command.Execute(null);

        Assert.True(called);
    }

    [Fact]
    public void CanExecute_ReturnsTrue_WhenNoPredicate()
    {
        var command = new RelayCommand(() => { });

        Assert.True(command.CanExecute(null));
    }

    [Fact]
    public void CanExecute_ReturnsFalse_WhenPredicateReturnsFalse()
    {
        var command = new RelayCommand(() => { }, () => false);

        Assert.False(command.CanExecute(null));
    }

    [Fact]
    public void RaiseCanExecuteChanged_FiresEvent()
    {
        var command = new RelayCommand(() => { });
        var fired = false;
        command.CanExecuteChanged += (_, _) => fired = true;

        command.RaiseCanExecuteChanged();

        Assert.True(fired);
    }
}

public class AsyncRelayCommandTests
{
    [Fact]
    public async Task Execute_CallsAsyncAction()
    {
        var called = false;
        var command = new AsyncRelayCommand(async () =>
        {
            await Task.Delay(10);
            called = true;
        });

        await command.ExecuteAsync();

        Assert.True(called);
    }

    [Fact]
    public async Task IsRunning_IsTrueDuringExecution()
    {
        var tcs = new TaskCompletionSource();
        bool wasRunning = false;
        var command = new AsyncRelayCommand(async () =>
        {
            await tcs.Task;
        });

        var executeTask = command.ExecuteAsync();
        wasRunning = command.IsRunning;
        tcs.SetResult();
        await executeTask;

        Assert.True(wasRunning);
        Assert.False(command.IsRunning);
    }

    [Fact]
    public async Task Cancellation_StopsExecution()
    {
        var command = new AsyncRelayCommand(async ct =>
        {
            await Task.Delay(TimeSpan.FromSeconds(30), ct);
        });

        var executeTask = command.ExecuteAsync();
        command.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => executeTask);
    }
}
```

---

### 9.3 — Unit Tests for DOM Capture Service

Test snapshot parsing, element extraction, and attribute mapping.

**Implementation (xUnit):**

```csharp
public class DomCaptureServiceTests
{
    private const string SampleDomJson = """
    {
      "tag": "form",
      "attributes": { "id": "loginForm", "class": "auth-form" },
      "children": [
        {
          "tag": "input",
          "attributes": { "id": "username", "type": "text", "name": "user", "placeholder": "Username" },
          "children": []
        },
        {
          "tag": "input",
          "attributes": { "id": "password", "type": "password", "name": "pass" },
          "children": []
        },
        {
          "tag": "button",
          "attributes": { "id": "loginBtn", "type": "submit" },
          "textContent": "Sign In",
          "children": []
        }
      ]
    }
    """;

    [Fact]
    public void ParseSnapshot_ExtractsCorrectElementCount()
    {
        var elements = DomParser.ParseElements(SampleDomJson);

        Assert.Equal(4, elements.Count); // form + 3 children
    }

    [Fact]
    public void ParseSnapshot_ExtractsAttributes()
    {
        var elements = DomParser.ParseElements(SampleDomJson);
        var username = elements.First(e => e.Id == "username");

        Assert.Equal("input", username.Tag);
        Assert.Equal("text", username.Attributes["type"]);
        Assert.Equal("user", username.Attributes["name"]);
        Assert.Equal("Username", username.Attributes["placeholder"]);
    }

    [Fact]
    public void ParseSnapshot_ExtractsNestedElements()
    {
        var elements = DomParser.ParseElements(SampleDomJson);
        var form = elements.First(e => e.Tag == "form");

        Assert.Equal(3, form.Children.Count);
    }

    [Fact]
    public void ParseSnapshot_ExtractsTextContent()
    {
        var elements = DomParser.ParseElements(SampleDomJson);
        var button = elements.First(e => e.Tag == "button");

        Assert.Equal("Sign In", button.TextContent);
    }

    [Fact]
    public void ParseSnapshot_HandlesEmptyDom()
    {
        var elements = DomParser.ParseElements("""{ "tag": "div", "attributes": {}, "children": [] }""");

        Assert.Single(elements);
    }

    [Fact]
    public void ParseSnapshot_HandlesElementsWithoutId()
    {
        var json = """{ "tag": "span", "attributes": { "class": "label" }, "children": [] }""";
        var elements = DomParser.ParseElements(json);

        Assert.Null(elements[0].Id);
        Assert.Equal("label", elements[0].Attributes["class"]);
    }
}

public class CorpusServiceTests : IAsyncLifetime
{
    private CorpusService _sut = null!;
    private string _dbPath = null!;

    public async Task InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"corpus_test_{Guid.NewGuid()}.db");
        _sut = new CorpusService(_dbPath);
        await _sut.InitializeAsync();
    }

    public Task DisposeAsync()
    {
        (_sut as IDisposable)?.Dispose();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
        return Task.CompletedTask;
    }

    [Fact]
    public async Task StoreSnapshotAsync_StoresAndRetrievesCorrectly()
    {
        var snapshot = new DomSnapshot("https://app.example.com/login", "LoginPage", SampleDomJson);

        await _sut.StoreSnapshotAsync(snapshot);
        var retrieved = await _sut.GetSnapshotAsync("LoginPage");

        Assert.NotNull(retrieved);
        Assert.Equal("LoginPage", retrieved.PageName);
        Assert.Equal(snapshot.DomJson, retrieved.DomJson);
    }

    [Fact]
    public async Task StoreSnapshotAsync_ReRecording_MarksOldAsHistorical()
    {
        var original = new DomSnapshot("https://app.example.com/login", "LoginPage", "{}");
        await _sut.StoreSnapshotAsync(original);

        var updated = new DomSnapshot("https://app.example.com/login", "LoginPage", "{\"tag\":\"div\"}");
        await _sut.StoreSnapshotAsync(updated);

        var snapshots = await _sut.ListSnapshotsAsync();
        var loginSnapshots = snapshots.Where(s => s.PageName == "LoginPage").ToList();

        Assert.Equal(2, loginSnapshots.Count);
        Assert.Single(loginSnapshots, s => s.Status == SnapshotStatus.Current);
        Assert.Single(loginSnapshots, s => s.Status == SnapshotStatus.Historical);
    }

    [Fact]
    public async Task SearchElementsAsync_FindsByTag()
    {
        var snapshot = new DomSnapshot("https://app.example.com/login", "LoginPage", SampleDomJson);
        await _sut.StoreSnapshotAsync(snapshot);

        var results = await _sut.SearchElementsAsync(tag: "input");

        Assert.Equal(2, results.Count); // username + password inputs
    }

    [Fact]
    public async Task SearchElementsAsync_FindsById()
    {
        var snapshot = new DomSnapshot("https://app.example.com/login", "LoginPage", SampleDomJson);
        await _sut.StoreSnapshotAsync(snapshot);

        var results = await _sut.SearchElementsAsync(id: "loginBtn");

        Assert.Single(results);
        Assert.Equal("button", results[0].Tag);
    }

    [Fact]
    public async Task SearchElementsAsync_FindsByDataTestId()
    {
        var domWithTestId = """
        {
          "tag": "div",
          "attributes": { "data-testid": "submit-section" },
          "children": [
            { "tag": "button", "attributes": { "data-testid": "submit-btn" }, "children": [] }
          ]
        }
        """;
        var snapshot = new DomSnapshot("https://app.example.com/form", "FormPage", domWithTestId);
        await _sut.StoreSnapshotAsync(snapshot);

        var results = await _sut.SearchElementsAsync(dataTestId: "submit-btn");

        Assert.Single(results);
    }

    [Fact]
    public async Task ListSnapshotsAsync_ReturnsAllPagesWithCorrectStatus()
    {
        await _sut.StoreSnapshotAsync(new DomSnapshot("https://app.example.com/login", "LoginPage", "{}"));
        await _sut.StoreSnapshotAsync(new DomSnapshot("https://app.example.com/home", "HomePage", "{}"));

        var snapshots = await _sut.ListSnapshotsAsync();

        Assert.Equal(2, snapshots.Count);
        Assert.All(snapshots, s => Assert.Equal(SnapshotStatus.Current, s.Status));
    }

    [Fact]
    public async Task SiteAlias_MultipleUrlsMappedToSameCorpus()
    {
        await _sut.AddSiteAliasAsync("https://staging.example.com", "example-app");
        await _sut.AddSiteAliasAsync("https://app.example.com", "example-app");

        var corpus1 = await _sut.ResolveSiteAsync("https://staging.example.com");
        var corpus2 = await _sut.ResolveSiteAsync("https://app.example.com");

        Assert.Equal(corpus1, corpus2);
    }
}
```

---

### 9.4 — Unit Tests for LLM Prompt Builder

Test system prompt assembly and DOM-to-prompt conversion.

**Implementation (xUnit):**

```csharp
public class PromptBuilderTests
{
    [Fact]
    public void BuildSystemPrompt_ContainsConventionsSection()
    {
        var prompt = PromptBuilder.BuildSystemPrompt();

        Assert.Contains("Conventions", prompt);
    }

    [Fact]
    public void BuildSystemPrompt_ContainsExampleSection()
    {
        var prompt = PromptBuilder.BuildSystemPrompt();

        Assert.Contains("Example", prompt);
    }

    [Fact]
    public void BuildUserPrompt_ContainsDomContent()
    {
        var domJson = """{ "tag": "div", "attributes": { "id": "main" }, "children": [] }""";

        var prompt = PromptBuilder.BuildUserPrompt(domJson, "MainPage");

        Assert.Contains("div", prompt);
        Assert.Contains("main", prompt);
    }

    [Fact]
    public void BuildUserPrompt_ContainsPageName()
    {
        var prompt = PromptBuilder.BuildUserPrompt("{}", "LoginPage");

        Assert.Contains("LoginPage", prompt);
    }

    [Fact]
    public void BuildUserPrompt_FormatsNestedDom()
    {
        var domJson = """
        {
          "tag": "form",
          "attributes": { "id": "loginForm" },
          "children": [
            { "tag": "input", "attributes": { "id": "email" }, "children": [] }
          ]
        }
        """;

        var prompt = PromptBuilder.BuildUserPrompt(domJson, "LoginPage");

        Assert.Contains("loginForm", prompt);
        Assert.Contains("email", prompt);
    }

    [Fact]
    public void BuildSystemPrompt_ContainsRequiredUsingStatements()
    {
        var prompt = PromptBuilder.BuildSystemPrompt();

        Assert.Contains("Brinell.Html.Abstractions", prompt);
        Assert.Contains("Brinell.Html.Controls", prompt);
        Assert.Contains("Brinell.Core.Locators", prompt);
    }

    [Fact]
    public void BuildSystemPrompt_IncludesCustomControlsFromRegistry()
    {
        var controls = new[]
        {
            new CustomControlDefinition("DateRangePicker", "IDateRangePicker", "ExactOnline.Controls"),
            new CustomControlDefinition("CurrencyInput", "ICurrencyInput", "ExactOnline.Controls")
        };

        var prompt = PromptBuilder.BuildSystemPrompt(customControls: controls);

        Assert.Contains("DateRangePicker", prompt);
        Assert.Contains("ICurrencyInput", prompt);
        Assert.Contains("ExactOnline.Controls", prompt);
    }

    [Fact]
    public void BuildSystemPrompt_IncludesSiteSpecificPatterns()
    {
        var patterns = new SitePatterns
        {
            CommonLocatorStrategies = ["data-testid", "aria-label"],
            FrequentElements = [("input", 42), ("button", 18)]
        };

        var prompt = PromptBuilder.BuildSystemPrompt(sitePatterns: patterns);

        Assert.Contains("data-testid", prompt);
        Assert.Contains("aria-label", prompt);
    }

    [Fact]
    public void BuildSystemPrompt_LocatorPreferenceOrder()
    {
        var prompt = PromptBuilder.BuildSystemPrompt();

        var textIdx = prompt.IndexOf("text", StringComparison.OrdinalIgnoreCase);
        var testIdIdx = prompt.IndexOf("data-testid", StringComparison.OrdinalIgnoreCase);
        var ariaIdx = prompt.IndexOf("aria-label", StringComparison.OrdinalIgnoreCase);
        var idIdx = prompt.IndexOf("By.Id", StringComparison.OrdinalIgnoreCase);
        var cssIdx = prompt.IndexOf("CSS", StringComparison.OrdinalIgnoreCase);

        // Preferred order: text → data-testid → aria-label → id → CSS
        Assert.True(textIdx < testIdIdx, "text should come before data-testid");
        Assert.True(testIdIdx < ariaIdx, "data-testid should come before aria-label");
        Assert.True(ariaIdx < idIdx, "aria-label should come before id");
        Assert.True(idIdx < cssIdx, "id should come before CSS");
    }

    [Fact]
    public void BuildAnalysisPrompt_DiffersFromGenerationPrompt()
    {
        var analysisPrompt = PromptBuilder.BuildAnalysisPrompt("{}" , "TestSite");
        var generationPrompt = PromptBuilder.BuildUserPrompt("{}", "TestPage");

        Assert.NotEqual(analysisPrompt, generationPrompt);
        Assert.Contains("analy", analysisPrompt, StringComparison.OrdinalIgnoreCase);
    }
}
```

---

### 9.5 — Unit Tests for Code Output Service

Test file naming, namespace detection, and merge logic.

**Implementation (xUnit):**

```csharp
public class CodeOutputServiceTests
{
    [Theory]
    [InlineData("LoginPage", "Pages", "LoginPage.cs")]
    [InlineData("TimeEntryPage", "Pages", "TimeEntryPage.cs")]
    [InlineData("Dashboard", "Pages", "Dashboard.cs")]
    public void GeneratePageFileName_WritesToPagesSubfolder(string pageName, string expectedFolder, string expectedFile)
    {
        var result = CodeOutputService.GenerateFilePath(pageName, OutputKind.Page);

        Assert.Equal(Path.Combine(expectedFolder, expectedFile), result);
    }

    [Theory]
    [InlineData("DateRangePicker", "Controls", "DateRangePicker.cs")]
    [InlineData("CurrencyInput", "Controls", "CurrencyInput.cs")]
    public void GenerateControlFileName_WritesToControlsSubfolder(string controlName, string expectedFolder, string expectedFile)
    {
        var result = CodeOutputService.GenerateFilePath(controlName, OutputKind.Control);

        Assert.Equal(Path.Combine(expectedFolder, expectedFile), result);
    }

    [Fact]
    public void DetectNamespace_ReadsFromCsproj()
    {
        var csprojContent = """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <RootNamespace>ExactOnline.Pages</RootNamespace>
          </PropertyGroup>
        </Project>
        """;

        var ns = CodeOutputService.DetectNamespace(csprojContent);

        Assert.Equal("ExactOnline.Pages", ns);
    }

    [Fact]
    public void DetectNamespace_FallsBackToProjectName_WhenNoRootNamespace()
    {
        var csprojContent = """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>net10.0</TargetFramework>
          </PropertyGroup>
        </Project>
        """;

        var ns = CodeOutputService.DetectNamespace(csprojContent, projectName: "Synergy.Pages");

        Assert.Equal("Synergy.Pages", ns);
    }
}

public class PageObjectMergeServiceTests
{
    [Fact]
    public void Merge_AddsNewProperties_WithoutRemovingExisting()
    {
        var existingCode = """
        using Brinell.Html.Abstractions;

        namespace ExactOnline.Pages;

        public class LoginPage : HtmlPageObject
        {
            public ITextInput UsernameInput => Find<ITextInput>(By.Id("username"));
        }
        """;

        var newProperties = new[]
        {
            new GeneratedProperty("PasswordInput", "ITextInput", "By.Id(\"password\")"),
            new GeneratedProperty("LoginButton", "IButton", "By.Id(\"loginBtn\")")
        };

        var merged = PageObjectMergeService.Merge(existingCode, newProperties);

        Assert.Contains("UsernameInput", merged);   // existing preserved
        Assert.Contains("PasswordInput", merged);    // new added
        Assert.Contains("LoginButton", merged);      // new added
    }

    [Fact]
    public void Merge_SkipsExistingProperties()
    {
        var existingCode = """
        namespace Test;

        public class TestPage : HtmlPageObject
        {
            public ITextInput UsernameInput => Find<ITextInput>(By.Id("username"));
        }
        """;

        var newProperties = new[]
        {
            new GeneratedProperty("UsernameInput", "ITextInput", "By.Id(\"username\")"),
        };

        var merged = PageObjectMergeService.Merge(existingCode, newProperties);

        // Should contain UsernameInput exactly once
        var count = merged.Split("UsernameInput").Length - 1;
        Assert.Equal(1, count);
    }

    [Fact]
    public void GeneratedPage_IncludesCustomControlUsingStatements()
    {
        var customControls = new[]
        {
            new CustomControlDefinition("DateRangePicker", "IDateRangePicker", "ExactOnline.Controls"),
        };

        var code = CodeOutputService.GeneratePageCode(
            "ReportPage",
            new[] { new GeneratedProperty("DateRange", "IDateRangePicker", "By.DataTestId(\"date-range\")") },
            "ExactOnline.Pages",
            customControls);

        Assert.Contains("using ExactOnline.Controls;", code);
        Assert.Contains("IDateRangePicker", code);
    }
}
```

---

### 9.6 — Unit Tests for Roslyn Validation

Test syntax error detection and formatting output.

**Implementation (xUnit):**

```csharp
public class RoslynValidationServiceTests
{
    [Fact]
    public void Validate_ValidCode_ReturnsNoErrors()
    {
        var code = """
        namespace Test;

        public class LoginPage
        {
            public string Name { get; set; } = "";
        }
        """;

        var result = RoslynValidationService.Validate(code);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_InvalidCode_ReportsErrors()
    {
        var code = """
        namespace Test;

        public class LoginPage
        {
            public string Name { get; set } // missing =
        }
        """;

        var result = RoslynValidationService.Validate(code);

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Validate_MissingSemicolon_ReportsError()
    {
        var code = """
        namespace Test;

        public class LoginPage
        {
            public string Name { get; set; } = ""
        }
        """;

        var result = RoslynValidationService.Validate(code);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("CS"));
    }

    [Fact]
    public void Format_NormalizesWhitespace()
    {
        var messyCode = """
        namespace   Test;
        public class   LoginPage{
        public    string    Name{get;set;}="";
        }
        """;

        var formatted = RoslynValidationService.Format(messyCode);

        Assert.Contains("public class LoginPage", formatted);
        Assert.Contains("public string Name { get; set; }", formatted);
    }

    [Fact]
    public void Format_PreservesSemantics()
    {
        var code = """
        namespace Test;

        public class LoginPage
        {
            public string Name { get; set; } = "";
            public int Count { get; set; }
        }
        """;

        var formatted = RoslynValidationService.Format(code);

        Assert.Contains("Name", formatted);
        Assert.Contains("Count", formatted);
    }
}
```

---

### 9.7 — Unit Tests for `DomDiffService`

Test snapshot comparison and change detection.

**Implementation (xUnit):**

```csharp
public class DomDiffServiceTests
{
    private readonly DomDiffService _sut = new();

    private const string BaseSnapshot = """
    {
      "tag": "form",
      "attributes": { "id": "loginForm" },
      "children": [
        { "tag": "input", "attributes": { "id": "username", "type": "text" }, "children": [] },
        { "tag": "input", "attributes": { "id": "password", "type": "password" }, "children": [] },
        { "tag": "button", "attributes": { "id": "loginBtn" }, "textContent": "Sign In", "children": [] }
      ]
    }
    """;

    [Fact]
    public void Diff_DetectsAddedElements()
    {
        var updated = """
        {
          "tag": "form",
          "attributes": { "id": "loginForm" },
          "children": [
            { "tag": "input", "attributes": { "id": "username", "type": "text" }, "children": [] },
            { "tag": "input", "attributes": { "id": "password", "type": "password" }, "children": [] },
            { "tag": "input", "attributes": { "id": "remember", "type": "checkbox" }, "children": [] },
            { "tag": "button", "attributes": { "id": "loginBtn" }, "textContent": "Sign In", "children": [] }
          ]
        }
        """;

        var diff = _sut.Compare(BaseSnapshot, updated);

        Assert.Single(diff.Added);
        Assert.Equal("remember", diff.Added[0].Id);
    }

    [Fact]
    public void Diff_DetectsRemovedElements()
    {
        var updated = """
        {
          "tag": "form",
          "attributes": { "id": "loginForm" },
          "children": [
            { "tag": "input", "attributes": { "id": "username", "type": "text" }, "children": [] },
            { "tag": "button", "attributes": { "id": "loginBtn" }, "textContent": "Sign In", "children": [] }
          ]
        }
        """;

        var diff = _sut.Compare(BaseSnapshot, updated);

        Assert.Single(diff.Removed);
        Assert.Equal("password", diff.Removed[0].Id);
    }

    [Fact]
    public void Diff_DetectsChangedElements()
    {
        var updated = """
        {
          "tag": "form",
          "attributes": { "id": "loginForm" },
          "children": [
            { "tag": "input", "attributes": { "id": "username", "type": "email" }, "children": [] },
            { "tag": "input", "attributes": { "id": "password", "type": "password" }, "children": [] },
            { "tag": "button", "attributes": { "id": "loginBtn" }, "textContent": "Log In", "children": [] }
          ]
        }
        """;

        var diff = _sut.Compare(BaseSnapshot, updated);

        Assert.Equal(2, diff.Changed.Count);
        Assert.Contains(diff.Changed, c => c.Id == "username"); // type: text → email
        Assert.Contains(diff.Changed, c => c.Id == "loginBtn"); // text: Sign In → Log In
    }

    [Fact]
    public void Diff_IdenticalSnapshots_ProducesEmptyDiff()
    {
        var diff = _sut.Compare(BaseSnapshot, BaseSnapshot);

        Assert.Empty(diff.Added);
        Assert.Empty(diff.Removed);
        Assert.Empty(diff.Changed);
    }

    [Fact]
    public void Diff_MatchesById_ThenDataTestId_ThenStructuralPath()
    {
        var before = """
        {
          "tag": "div", "attributes": {}, "children": [
            { "tag": "span", "attributes": { "id": "title" }, "textContent": "Hello", "children": [] },
            { "tag": "span", "attributes": { "data-testid": "subtitle" }, "textContent": "World", "children": [] },
            { "tag": "span", "attributes": { "class": "info" }, "textContent": "Old", "children": [] }
          ]
        }
        """;
        var after = """
        {
          "tag": "div", "attributes": {}, "children": [
            { "tag": "span", "attributes": { "id": "title" }, "textContent": "Hi", "children": [] },
            { "tag": "span", "attributes": { "data-testid": "subtitle" }, "textContent": "Earth", "children": [] },
            { "tag": "span", "attributes": { "class": "info" }, "textContent": "New", "children": [] }
          ]
        }
        """;

        var diff = _sut.Compare(before, after);

        // All three matched (no adds/removes), three text changes detected
        Assert.Empty(diff.Added);
        Assert.Empty(diff.Removed);
        Assert.Equal(3, diff.Changed.Count);
    }
}
```

---

### 9.8 — Unit Tests for `ControlRegistryService`

Test custom control CRUD, approval workflow, and duplicate detection.

**Implementation (xUnit):**

```csharp
public class ControlRegistryServiceTests : IAsyncLifetime
{
    private ControlRegistryService _sut = null!;
    private string _dbPath = null!;

    public async Task InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"registry_test_{Guid.NewGuid()}.db");
        _sut = new ControlRegistryService(_dbPath);
        await _sut.InitializeAsync();
    }

    public Task DisposeAsync()
    {
        (_sut as IDisposable)?.Dispose();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
        return Task.CompletedTask;
    }

    [Fact]
    public async Task StoreAndRetrieve_CustomControl()
    {
        var control = new CustomControlDefinition("DateRangePicker", "IDateRangePicker", "ExactOnline.Controls")
        {
            SiteName = "exact-online",
            MatchPattern = "div.date-range-picker",
            GeneratedCode = "public interface IDateRangePicker { }"
        };

        await _sut.StoreAsync(control);
        var retrieved = await _sut.GetAsync("DateRangePicker");

        Assert.NotNull(retrieved);
        Assert.Equal("IDateRangePicker", retrieved.InterfaceName);
        Assert.Equal("exact-online", retrieved.SiteName);
    }

    [Fact]
    public async Task Approve_SetsApprovedStatus()
    {
        var control = new CustomControlDefinition("Grid", "IGrid", "App.Controls") { SiteName = "myapp" };
        await _sut.StoreAsync(control);

        await _sut.ApproveAsync("Grid");
        var retrieved = await _sut.GetAsync("Grid");

        Assert.Equal(ControlStatus.Approved, retrieved!.Status);
    }

    [Fact]
    public async Task Reject_SetsRejectedStatus()
    {
        var control = new CustomControlDefinition("Wizard", "IWizard", "App.Controls") { SiteName = "myapp" };
        await _sut.StoreAsync(control);

        await _sut.RejectAsync("Wizard");
        var retrieved = await _sut.GetAsync("Wizard");

        Assert.Equal(ControlStatus.Rejected, retrieved!.Status);
    }

    [Fact]
    public async Task GetBySite_ReturnsOnlyMatchingSite()
    {
        await _sut.StoreAsync(new CustomControlDefinition("Picker", "IPicker", "A.Controls") { SiteName = "site-a" });
        await _sut.StoreAsync(new CustomControlDefinition("Grid", "IGrid", "B.Controls") { SiteName = "site-b" });
        await _sut.StoreAsync(new CustomControlDefinition("Tree", "ITree", "A.Controls") { SiteName = "site-a" });

        var results = await _sut.GetBySiteAsync("site-a");

        Assert.Equal(2, results.Count);
        Assert.All(results, c => Assert.Equal("site-a", c.SiteName));
    }

    [Fact]
    public async Task Store_DuplicateControl_ThrowsOrUpdates()
    {
        var control = new CustomControlDefinition("Picker", "IPicker", "App.Controls") { SiteName = "myapp" };
        await _sut.StoreAsync(control);

        var duplicate = new CustomControlDefinition("Picker", "IPicker", "App.Controls") { SiteName = "myapp" };

        // Duplicate storage should either throw or update (upsert) — not create a second entry
        await _sut.StoreAsync(duplicate);
        var bySite = await _sut.GetBySiteAsync("myapp");

        Assert.Single(bySite.Where(c => c.ClassName == "Picker"));
    }
}
```

---

### 9.9 — Unit Tests for `PipelineOrchestrator`

Test pipeline state management, step progression, resume, and incremental generation.

**Implementation (xUnit):**

```csharp
public class PipelineOrchestratorTests
{
    private PipelineOrchestrator CreateOrchestrator(
        ICorpusService? corpus = null,
        IControlRegistryService? registry = null,
        ICodeOutputService? output = null)
    {
        return new PipelineOrchestrator(
            corpus ?? Substitute.For<ICorpusService>(),
            registry ?? Substitute.For<IControlRegistryService>(),
            output ?? Substitute.For<ICodeOutputService>(),
            Substitute.For<IAnalysisService>(),
            Substitute.For<IGenerationService>());
    }

    [Fact]
    public async Task NewPipeline_StartsAtAnalysisStep()
    {
        var sut = CreateOrchestrator();

        var state = await sut.CreatePipelineAsync("test-site");

        Assert.Equal(PipelineStep.Analysis, state.CurrentStep);
    }

    [Theory]
    [InlineData(PipelineStep.Analysis, PipelineStep.ControlApproval)]
    [InlineData(PipelineStep.ControlApproval, PipelineStep.ControlGeneration)]
    [InlineData(PipelineStep.ControlGeneration, PipelineStep.PageGeneration)]
    [InlineData(PipelineStep.PageGeneration, PipelineStep.ProjectOutput)]
    [InlineData(PipelineStep.ProjectOutput, PipelineStep.Complete)]
    public async Task AdvanceStep_TransitionsCorrectly(PipelineStep from, PipelineStep expectedNext)
    {
        var sut = CreateOrchestrator();
        var state = new PipelineState { CurrentStep = from, SiteName = "test" };

        var next = await sut.AdvanceAsync(state);

        Assert.Equal(expectedNext, next.CurrentStep);
    }

    [Fact]
    public async Task Resume_ContinuesFromSavedState()
    {
        var corpus = Substitute.For<ICorpusService>();
        corpus.GetPipelineStateAsync("test-site").Returns(
            new PipelineState { CurrentStep = PipelineStep.PageGeneration, SiteName = "test-site" });

        var sut = CreateOrchestrator(corpus: corpus);

        var state = await sut.ResumeAsync("test-site");

        Assert.Equal(PipelineStep.PageGeneration, state.CurrentStep);
    }

    [Fact]
    public async Task Resume_ReturnsNull_WhenNoPipelineExists()
    {
        var corpus = Substitute.For<ICorpusService>();
        corpus.GetPipelineStateAsync("unknown").Returns((PipelineState?)null);

        var sut = CreateOrchestrator(corpus: corpus);

        var state = await sut.ResumeAsync("unknown");

        Assert.Null(state);
    }

    [Fact]
    public async Task IncrementalGeneration_SkipsUnchangedPages()
    {
        var corpus = Substitute.For<ICorpusService>();
        corpus.GetChangedPagesAsync("test-site").Returns(
            new[] { "LoginPage", "DashboardPage" }); // only 2 of N pages changed

        var output = Substitute.For<ICodeOutputService>();
        var sut = CreateOrchestrator(corpus: corpus, output: output);

        var options = new PipelineOptions { GenerationMode = GenerationMode.GenerateChanged };
        await sut.RunGenerationStepAsync("test-site", options);

        // Verify generation was called only for changed pages
        await output.Received(1).WritePageAsync(Arg.Is<string>(p => p == "LoginPage"), Arg.Any<string>());
        await output.Received(1).WritePageAsync(Arg.Is<string>(p => p == "DashboardPage"), Arg.Any<string>());
        await output.DidNotReceive().WritePageAsync(Arg.Is<string>(p => p != "LoginPage" && p != "DashboardPage"), Arg.Any<string>());
    }
}
```

---

## Acceptance Criteria

- [ ] All `ViewModelBase` tests pass — `SetProperty` raises/doesn't raise correctly, value types work.
- [ ] All `RelayCommand` tests pass — execute, can-execute, event firing.
- [ ] All `AsyncRelayCommand` tests pass — async execution, `IsRunning` flag, cancellation.
- [ ] DOM capture tests verify element count, attribute extraction, nested parsing, text content, edge cases.
- [ ] `CorpusService` tests pass — store/retrieve snapshots, re-recording marks old as historical, search by tag/id/data-testid, list with status, site alias resolution.
- [ ] Prompt builder tests verify system prompt contains conventions/examples/usings, user prompt contains DOM and page name.
- [ ] Prompt builder tests verify custom controls and site patterns are included, locator preference order is correct, analysis prompt differs from generation prompt.
- [ ] Code output tests verify pages written to `Pages/` subfolder, controls written to `Controls/` subfolder.
- [ ] Code output tests verify custom control `using` statements are included in generated page files.
- [ ] Code output tests verify namespace detection from `.csproj`, fallback behavior.
- [ ] Merge tests verify new properties are added, existing properties are preserved, duplicates are skipped.
- [ ] Roslyn validation tests verify valid code passes, invalid code reports CS errors, formatting normalizes whitespace.
- [ ] `DomDiffService` tests pass — added/removed/changed elements detected, identical snapshots produce empty diff, matching by id → data-testid → structural path.
- [ ] `ControlRegistryService` tests pass — store/retrieve, approve/reject, get by site, duplicate detection.
- [ ] `PipelineOrchestrator` tests pass — step progression, resume from saved state, incremental generation skips unchanged pages.
- [ ] All tests use xUnit with `[Fact]` and `[Theory]` attributes.
- [ ] No tests require external dependencies (WebView2, LLM API, file system) — all use in-memory/temp-file data.

## Dependencies

- Phase 1 (MVVM Infrastructure) — `ViewModelBase`, `RelayCommand`, `AsyncRelayCommand` implementations.
- Phase 3 (Corpus Storage) — `CorpusService`, `ControlRegistryService` implementations.
- Phase 4 (DOM Capture) — `DomParser` and `DomCaptureService` implementations.
- Phase 4 (DOM Diff) — `DomDiffService` implementation.
- Phase 5 (LLM Code Generation) — `PromptBuilder` implementation (with corpus context support).
- Phase 7 (Project Output) — `CodeOutputService`, `PageObjectMergeService`, `RoslynValidationService`.
- Phase 8 (Pipeline) — `PipelineOrchestrator` implementation.
- xUnit + NSubstitute + `Microsoft.CodeAnalysis.CSharp` test dependencies.
