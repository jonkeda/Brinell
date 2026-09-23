# Phase 6 — Code Preview & Editing

## Goal

Show generated code to the user for review. Use Roslyn for validation and formatting. Editing is deferred to VS Code / Visual Studio.

## Tasks

### 6.1 — C# Code Editor Panel (AvalonEdit)

Display the generated C# code in a syntax-highlighted editor using **AvalonEdit**. The editor is read-only by default — actual editing is left to VS Code or Visual Studio.

**Implementation:**

- Add NuGet: `AvaloniaEdit` (WPF version: `ICSharpCode.AvalonEdit`)
  ```xml
  <PackageReference Include="AvalonEdit" Version="6.*" />
  ```
- Use `TextEditor` control in XAML:
  ```xml
  <avalonEdit:TextEditor
      x:Name="CodeEditor"
      SyntaxHighlighting="C#"
      IsReadOnly="True"
      FontFamily="Cascadia Code, Consolas, Courier New"
      FontSize="13"
      ShowLineNumbers="True"
      WordWrap="False"
      HorizontalScrollBarVisibility="Auto"
      VerticalScrollBarVisibility="Auto" />
  ```
- AvalonEdit provides built-in C# syntax highlighting (`SyntaxHighlighting="C#"`)
- Dark theme: set `Background="#1E1E1E"` and `Foreground="#D4D4D4"`, or load a custom `.xshd` highlighting definition for VS Code-like colors
- Bind document text via code-behind (`CodeEditor.Text = viewModel.GeneratedCode`) since AvalonEdit `Text` is not a dependency property — use an attached behavior or event-based binding
- If multiple classes were generated (PageObject + ContainerBase classes), show them concatenated with a `// ─── {ClassName} ───` separator comment
- Folding support: enable `FoldingManager` for collapsing class bodies and regions

**Header — custom control indicators:**

The code preview header shows which custom controls from the site's control registry are in use by the generated PageObject. For example:

```
Code Preview — Using controls: DatePicker ✅, Autocomplete ✅
```

**Controls Manager reuse:**

The same AvalonEdit panel is reused in the Controls Manager view with `IsReadOnly="False"` so users can directly edit custom control code (ControlObjects). In the PageObject preview the editor remains read-only.

**ViewModel:**

```csharp
public sealed class CodePreviewViewModel : ViewModelBase
{
    private string _generatedCode = "";
    public string GeneratedCode
    {
        get => _generatedCode;
        set => SetProperty(ref _generatedCode, value);
    }

    private bool _hasValidationErrors;
    public bool HasValidationErrors
    {
        get => _hasValidationErrors;
        set => SetProperty(ref _hasValidationErrors, value);
    }

    private string _validationSummary = "";
    public string ValidationSummary
    {
        get => _validationSummary;
        set => SetProperty(ref _validationSummary, value);
    }
}
```

---

### 6.2 — Re-Generate Single Control

Allow the user to select a single control property in the preview and re-map it to a different DOM element. The preview can show either ControlObject code (from Phase 5 custom control analysis) or PageObject code (from Phase 5 generation).

**Workflow:**

1. User selects a property name in the code preview (or picks from a dropdown of generated properties)
2. User enters inspection mode and clicks a different DOM element in the browser
3. App re-runs the LLM for just that single property, providing:
   - The existing class context (other properties)
   - The new DOM element
   - Full corpus context: site-wide patterns, existing controls registry, and the analyzer's findings for the target element
   - Instruction: "Replace the `{PropertyName}` property with a control targeting this element"
4. Parse the LLM response for the single property declaration
5. Splice the new property into the existing generated code, replacing the old one

**Splice logic:**

```csharp
public static string ReplaceProperty(string code, string propertyName, string newPropertyCode)
{
    // Use Roslyn to parse the code
    var tree = CSharpSyntaxTree.ParseText(code);
    var root = tree.GetRoot();

    // Find the property declaration by name
    var property = root.DescendantNodes()
        .OfType<PropertyDeclarationSyntax>()
        .FirstOrDefault(p => p.Identifier.Text == propertyName);

    if (property is null) return code;

    // Parse the new property code
    var newProperty = SyntaxFactory.ParseMemberDeclaration(newPropertyCode);
    if (newProperty is null) return code;

    // Replace in syntax tree
    var newRoot = root.ReplaceNode(property, newProperty);
    return newRoot.ToFullString();
}
```

---

### 6.3 — Roslyn Validation

Parse generated code with Roslyn and report syntax errors inline.

**Implementation:**

```csharp
public sealed class RoslynValidator
{
    public ValidationResult Validate(string code)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var diagnostics = syntaxTree.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        var errors = diagnostics.Select(d =>
        {
            var lineSpan = d.Location.GetLineSpan();
            return new ValidationError
            {
                Line = lineSpan.StartLinePosition.Line + 1,
                Column = lineSpan.StartLinePosition.Character + 1,
                Message = d.GetMessage(),
                Severity = d.Severity.ToString()
            };
        }).ToList();

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }
}
```

**Error display:**

- Show error count badge on the preview panel header: "Code Preview (2 errors)"
- Below the code preview, show a collapsible error list:
  ```
  Line 12, Col 5: ; expected
  Line 18, Col 22: Type or namespace 'UnknownControl' could not be found
  ```
- Clicking an error scrolls the AvalonEdit editor to that line (`CodeEditor.ScrollToLine(lineNumber)`) and highlights it

**Validation triggers:**

- Run automatically after LLM generation completes
- Run again after single-property re-generation (Task 6.2)
- Run after Roslyn formatting (Task 6.4)

---

### 6.4 — Roslyn Formatting

Auto-format generated code using `Microsoft.CodeAnalysis.CSharp.Formatting`.

**Implementation:**

```csharp
public static class CodeFormatter
{
    public static string Format(string code)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var root = syntaxTree.GetRoot();

        using var workspace = new AdhocWorkspace();
        var formattedRoot = Formatter.Format(root, workspace);

        return formattedRoot.ToFullString();
    }
}
```

**Formatting options:**

- Use default workspace options (4-space indentation, standard C# formatting)
- Apply formatting automatically after LLM generation and after property replacement
- Format button in toolbar for manual re-format

**Post-format flow:**

1. LLM generates code → format → validate → display
2. Property re-generation → splice → format → validate → display

---

### 6.5 — Copy to Clipboard / Open in VS Code

Export the generated code to the clipboard or directly to a file opened in VS Code.

**Copy to clipboard:**

```csharp
Clipboard.SetText(generatedCode);
// Show brief "Copied!" tooltip or status bar message
```

**Save to file:**

- `SaveFileDialog` with filter: `C# files (*.cs)|*.cs`
- Default filename derived from class name: `LoginPage.cs`
- Default directory: last used output directory (persisted in user settings)

**Open in VS Code:**

```csharp
public static void OpenInVsCode(string filePath)
{
    Process.Start(new ProcessStartInfo
    {
        FileName = "code",
        Arguments = $"\"{filePath}\"",
        UseShellExecute = true
    });
}
```

- Button: "Open in VS Code" — saves to temp file if not already saved, then opens
- If VS Code is not found (`code` not on PATH), show a message and fall back to default editor via `Process.Start(filePath)` with `UseShellExecute = true`

**Toolbar layout:**

```
[ Copy to Clipboard ] [ Save As... ] [ Open in VS Code ] [ Format ] [ Re-validate ]
```

---

## UI Design — Code Preview Panel (AvalonEdit)

Uses **ICSharpCode.AvalonEdit** for C# syntax highlighting, line numbers, and code folding.

```
┌─ Generated Code ─────────────────┐
│ Class: ExactTimePage       [≡]   │
│ Namespace: ExactOnline.Pages     │
│ Using controls: DatePicker ✅    │
│ ─────────────────────────────── │
│  1 │ using Brinell.Core.Locators;│
│  2 │ using Brinell.Html.Abstract…│
│  3 │ using Brinell.Html.Controls;│
│  4 │ using ExactOnline.Controls; │
│  5 │                              │
│  6 │ namespace ExactOnline.Pages;│
│  7 │                              │
│  8 │▼public sealed class Exact…  │
│  9 │ {                            │
│ 10 │   public ExactTimePage(      │
│ 11 │     IHtmlTestContext context)│
│ 12 │     : base(context) { }     │
│ 13 │                              │
│ 14 │   public TextInputControl    │
│ 15 │     <ExactTimePage> Hours…   │
│ 16 │     => new(this,             │
│ 17 │        Locator.ById("hours")│
│ 18 │                              │
│ 19 │   public DatePickerControl   │
│ 20 │     <ExactTimePage> Date…    │
│ 21 │     => new(this,             │
│ 22 │        Locator.ByCss(".dp…"))│
│ 23 │ }                            │
│                                   │
│ ─────────────────────────────── │
│  ✅ Roslyn: No errors            │
│ ─────────────────────────────── │
│ [📋 Copy] [📂 Open in VS Code]  │
│ [💾 Save to Project]            │
│ [🔄 Regenerate]                  │
└───────────────────────────────────┘
```

- Monospace font, read-only, C# syntax highlighted (AvalonEdit)
- Line numbers in gutter
- Code folding (▼) for class bodies
- Header shows which custom controls are in use
- Roslyn status bar (✅ No errors / ❌ 2 errors)
- Buttons at bottom: Copy, Open in editor, Save, Regenerate

---

## Acceptance Criteria

- [ ] Generated code is displayed in AvalonEdit with C# syntax highlighting, line numbers, and read-only mode
- [ ] Multiple generated classes are shown with clear separator comments
- [ ] Code preview header shows which custom controls from the site registry are in use (e.g. "Using controls: DatePicker ✅, Autocomplete ✅")
- [ ] AvalonEdit panel is reused in Controls Manager view with `IsReadOnly="False"` for editing ControlObjects
- [ ] ViewModel uses custom MVVM (`ViewModelBase` / `SetProperty`) — no CommunityToolkit.Mvvm dependency
- [ ] Single control re-generation replaces the correct property without affecting other code
- [ ] Re-generation sends full corpus context (site patterns, existing controls, analyzer findings) to the LLM
- [ ] Roslyn validation detects and reports syntax errors with line and column numbers
- [ ] Error count badge updates correctly after each validation pass
- [ ] Roslyn formatting produces consistently indented, idiomatic C# code
- [ ] Copy to clipboard works and provides user feedback
- [ ] Save As dialog defaults to the class name and `.cs` extension
- [ ] Open in VS Code launches the file in VS Code (or falls back to default editor)
- [ ] Full flow: generate → format → validate → preview completes without errors for valid pages

## Dependencies

- **Phase 5** — LLM code generation must produce C# code to preview
- **ICSharpCode.AvalonEdit** — NuGet package for WPF C# syntax-highlighted editor
  ```xml
  <PackageReference Include="AvalonEdit" Version="6.*" />
  ```
- **Microsoft.CodeAnalysis.CSharp** — NuGet package for Roslyn parsing, validation, and formatting
  ```xml
  <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.*" />
  <PackageReference Include="Microsoft.CodeAnalysis.CSharp.Workspaces" Version="4.*" />
  ```
- **Phase 1, step 1.2** (MVVM Foundation) — `ViewModelBase`, `RelayCommand`, `AsyncRelayCommand`
- **VS Code** — `code` CLI must be on PATH for "Open in VS Code" functionality

---

## Unit Test Plan

### Testable Components (~22 tests)

| Component | Tests | Strategy |
|-----------|-------|---------|
| `CodePreviewViewModel` | 5 | Property change notifications, validation state, generated code binding |
| `RoslynValidator` | 5 | Valid code passes, syntax errors with line/column, multiple errors, empty input, partial code |
| `RoslynFormatter` | 4 | Indentation normalization, using statement ordering, whitespace cleanup, idempotent formatting |
| Property splice (ReplaceProperty) | 5 | Replace existing property, property not found, multiple properties, preserve formatting, Roslyn round-trip |
| Copy to clipboard command | 3 | Code copied, empty code handled, command disabled when no code |

### Not Unit-Tested

- AvalonEdit rendering and syntax highlighting — WPF control
- "Open in VS Code" — requires `code` CLI on PATH
- Dark theme / `.xshd` highlighting — visual only
