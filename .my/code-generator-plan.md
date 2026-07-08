# Code Generator Plan: Wrapper Method Generation

**Date:** 2026-07-08  
**Purpose:** Design a code generator to automatically create public wrapper methods from protected virtual Core methods

---

## 1. Problem Statement

Currently, **ClickableControlBase** and similar control classes have repetitive boilerplate:
- Each protected virtual `*Core` method has a corresponding public wrapper method
- The wrapper follows a consistent pattern:
  - Calls `EnsureClickableCore(element)` 
  - Delegates to the `*Core` method
  - Wraps in `RunDoWithElement` for fluent chaining

Example:
```csharp
// Protected virtual
protected virtual void RightClickCore(IMauiElement element, int? timeoutMs = null) { ... }

// Public wrapper (boilerplate)
public TScope RightClick(int? timeoutMs = null)
{
    return RunDoWithElement(element =>
    {
        EnsureClickableCore(element);
        RightClickCore(element);
    }, timeoutMs);
}
```

This pattern is repeated for `Click`, `DoubleClick`, `RightClick`, `Hover`, `LongPress`, `Press`, etc.

---

## 2. Solution: Roslyn-Based Code Generator

### 2.1 High-Level Design

```
Input File (Button.code.cs)
        ↓
    [Roslyn Parser]
        ↓
[Method Analyzer]
  (find protected virtual *Core methods)
        ↓
[Wrapper Generator]
  (StringBuilder builds public methods)
        ↓
Output File (Button.gen.cs)
```

### 2.2 Components

#### A. **Roslyn Parser**
- **Tool:** `CSharpSyntaxTree.ParseText()` + `CSharpCompilation.Create()`
- **Job:** Read `.code.cs` file and build syntax tree
- **Extract:**
  - Class name
  - Generic type parameters (`<TScope>`)
  - Using statements / namespace
  - Protected virtual `*Core` methods (signatures, parameters, XML docs)
  - Method constraints (e.g., `where TScope : IMauiScope<TScope>`)

#### B. **Method Analyzer**
- **Criteria:** Identify methods to wrap
  - Name ends with `Core`
  - Access modifier: `protected`
  - Modifier: `virtual`
  - Return type: `void` (primary pattern)
- **Extract:**
  - Method name stem (remove "Core" → `RightClick` from `RightClickCore`)
  - Parameter list (preserve all parameters except `element`)
  - XML documentation (if present)
  - Special handling: Check for `timeoutMs` parameter

#### C. **Wrapper Generator (StringBuilder)**
- **Pattern:** Generate public method using template
- **Template Logic:**
  ```csharp
  public TScope {MethodName}({Parameters})
  {
      return RunDoWithElement(element =>
      {
          EnsureClickableCore(element);
          {CoreMethodName}(element{PassedParameters});
      }, timeoutMs);
  }
  ```
- **Output:** Plain `.gen.cs` file with no external dependencies

#### D. **File Writer**
- Write to `ClassName.gen.cs`
- Include header comment: `// This file is auto-generated. Do not edit manually.`
- Preserve namespace and using statements

---

## 3. Implementation Phases

### Phase 1: Spike / Prototype
**Scope:** Proof-of-concept for ClickableControlBase
- **Deliverable:** Console app that reads `ClickableControlBase.cs` and outputs wrapped methods
- **Input:** File path
- **Output:** Console output of generated methods
- **Duration:** 1-2 hours

### Phase 2: Generator Tool
**Scope:** Standalone tool that can be integrated into build
- **Deliverable:** Roslyn-based code generator (executable or library)
- **Features:**
  - Reads `.code.cs` file
  - Generates `.gen.cs` file
  - Configurable Core method naming pattern (default: ends with "Core")
  - XML documentation preservation
  - Parameter mapping intelligence

### Phase 3: Integration
**Scope:** MSBuild / Pre-build integration
- **Options:**
  1. **MSBuild Task:** Invoke generator as pre-build step
  2. **Project File:** Add `<GenerateWrappers>true</GenerateWrappers>` property
  3. **Analyzer:** Optional Roslyn analyzer to flag missing wrappers
- **Validation:** Ensure `.gen.cs` doesn't get checked into git (add to `.gitignore` or use `[GeneratedCode]` attribute)

---

## 4. Technical Details

### 4.1 Roslyn Usage

```csharp
// Load and parse file
var code = File.ReadAllText("ClickableControlBase.cs");
var tree = CSharpSyntaxTree.ParseText(code);
var root = tree.GetCompilationUnitSyntax();

// Extract class
var classDecl = root.Members.OfType<ClassDeclarationSyntax>().First();

// Find protected virtual methods ending with "Core"
var coreMethods = classDecl.Members
    .OfType<MethodDeclarationSyntax>()
    .Where(m => 
        m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.ProtectedKeyword)) &&
        m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.VirtualKeyword)) &&
        m.Identifier.Text.EndsWith("Core"))
    .ToList();
```

### 4.2 Method Name Conversion

```csharp
// Strip "Core" suffix
string CoreMethodName = "RightClickCore";
string PublicMethodName = CoreMethodName.Substring(0, CoreMethodName.Length - 4); // "RightClick"
```

### 4.3 Parameter Handling

```
Input:  protected virtual void RightClickCore(IMauiElement element, int? timeoutMs = null)
Output: public TScope RightClick(int? timeoutMs = null)

Passed: RightClickCore(element, timeoutMs)
```

**Rules:**
- First parameter `IMauiElement element` → remove from public signature, pass to Core call
- Other parameters → preserve in public signature and pass to Core call

### 4.4 Special Cases

| Pattern | Action |
|---------|--------|
| `void` return | Wrap in `RunDoWithElement` + return `TScope` |
| `bool` return | Consider generating `RunFuncWithElement` variant |
| `string` return | Consider generating `RunFuncWithElement` variant |
| Multiple overloads | Generate for each |

---

## 5. File Structure

```
tools/
  Brinell.CodeGenerator/              ← New project
    Brinell.CodeGenerator.csproj
    Program.cs
    CoreMethodAnalyzer.cs            ← Roslyn logic
    MethodInfo.cs                    ← Model
    WrapperGenerator.cs              ← StringBuilder logic
    
srcnew/
  Brinell.Maui/
    Controls/
      ClickableControlBase.code.cs   ← Source (manual edits)
      ClickableControlBase.gen.cs    ← Generated (auto)
      ClickableControlBase.cs        ← Partial (combines both)
```

---

## 6. Benefits

| Benefit | Impact |
|---------|--------|
| **DRY Principle** | Eliminates repetitive wrapper boilerplate |
| **Maintainability** | Change Core method → auto-update public wrapper |
| **Consistency** | All wrappers follow same pattern automatically |
| **Scaling** | Easy to apply to other control types (TextBox, Picker, etc.) |
| **Testing** | Fewer manual edits = fewer bugs |

---

## 7. Alternative Approaches Considered

### A. Source Generators (Modern Roslyn)
- ✅ Integrated into compilation
- ❌ Complex attribute setup, debugging harder
- 📌 **Decision:** Start with standalone tool (easier), migrate later if needed

### B. T4 Templates
- ✅ Built-in to Visual Studio
- ❌ Older, different syntax
- 📌 **Decision:** Roslyn is more modern and flexible

### C. Code Templates (Rider MPS)
- ✅ IDE-aware
- ❌ Not portable, not open-source friendly
- 📌 **Decision:** Roslyn is best fit

---

## 8. Next Steps

1. **Create spike:** New console app in `tools/` that parses `ClickableControlBase.cs`
2. **Extract core method info** using Roslyn
3. **Build wrapper generator** with StringBuilder
4. **Validate output** against current `ClickableControlBase` implementation
5. **Refactor:** Move Core method logic to `.code.cs`, add `.gen.cs` to build
6. **Extend:** Apply pattern to other control types
7. **Integrate:** Add as pre-build MSBuild task

---

## 9. Acceptance Criteria

- [ ] Generator reads `.code.cs` file without errors
- [ ] Generates `.gen.cs` with identical wrapper methods as current manual code
- [ ] Handles method parameter mapping correctly
- [ ] Preserves XML documentation (if present)
- [ ] Output is consistent, formatted, and commented
- [ ] Can be run as command-line tool: `dotnet run --input file.code.cs --output file.gen.cs`
- [ ] Generated file compiles without warnings
- [ ] Zero manual edits needed to generated code
