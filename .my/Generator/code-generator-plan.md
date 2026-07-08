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

### 2.2 Project Structure

**Brinell.Generator** (core library)
- Roslyn parsing and analysis logic
- Wrapper generation engine
- Reusable across tools and integrations

**Brinell.Generator.Cli** (command-line interface)
- Entry point for standalone tool
- Argument parsing
- File I/O orchestration

### 2.3 Components

#### A. **Roslyn Parser**
- **Tool:** `CSharpSyntaxTree.ParseText()` + `CSharpCompilation.Create()`
- **Job:** Read `.code.cs` file and build syntax tree
- **Extract:**
  - Class name
  - Generic type parameters (`<TScope>`)
  - Using statements / namespace
  - Protected virtual `*Core` methods (signatures, parameters, XML docs)
  - Method constraints (e.g., `where TScope : IMauiScope<TScope>`)

#### B. **Pluggable Method Handler Interface**
- **Interface:** `IMethodHandler` (abstract pattern matcher and transformer)
- **Responsibility:** Define how to identify and extract methods for wrapping
- **Built-in Handlers:**
  - `CoreMethodHandler` - Wraps protected virtual `*Core` methods
  - Extensible for future patterns (e.g., `*Async` → `*` async wrappers)
- **Handler Config:**
  - Method name pattern (suffix, prefix, regex)
  - Access modifiers to match
  - Required modifiers (virtual, async, etc.)
  - Wrapper generation strategy
  - Name transformation logic

#### C. **Method Analyzer** (uses pluggable handler)
- **Input:** Method handler + syntax tree
- **Process:**
  - Iterate class methods
  - Delegate to handler's `Matches()` predicate
  - Extract metadata via handler's `Extract()` method
  - Return list of `MethodInfo` models
- **Output:** Portable metadata (not syntax trees)

#### D. **Wrapper Generator (Roslyn SyntaxFactory)**
- **Input:** `MethodInfo` list + handler strategy
- **Process:**
  - Use `SyntaxFactory` to build method syntax nodes
  - Preserve parameter types, names, default values
  - Generate method bodies as syntax trees
  - Build class syntax tree with new methods
- **Output:** `SyntaxTree` (structured IR)

#### E. **Code Formatter (Roslyn CSharpFormatter)**
- **Input:** `SyntaxTree`
- **Process:**
  - Apply Roslyn formatting rules (indentation, spacing)
  - Use workspace formatting options
  - Generate final C# text
- **Output:** Formatted `.gen.cs` file text

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
**Scope:** Standalone tool with pluggable architecture
- **Deliverable:** 
  - `Brinell.Generator` library with pluggable handlers and Roslyn-based code generation
  - `Brinell.Generator.Cli` wrapper with argument parsing
- **Features:**
  - `IMethodHandler` interface for pattern matching strategies
  - `CoreMethodHandler` for `*Core` protected virtual methods
  - Reads `.code.cs` file
  - Generates `.gen.cs` file using `SyntaxFactory`
  - Formats output using `CSharpFormatter`
  - Extensible for future patterns without core changes

### Phase 3: Integration
**Scope:** MSBuild / Pre-build integration
- **Options:**
  1. **MSBuild Task:** Invoke generator as pre-build step
  2. **Project File:** Add `<GenerateWrappers>true</GenerateWrappers>` property
  3. **Analyzer:** Optional Roslyn analyzer to flag missing wrappers
- **Validation:** Ensure `.gen.cs` doesn't get checked into git (add to `.gitignore` or use `[GeneratedCode]` attribute)

---

## 4. Technical Details

### 4.1 Pluggable Handler Design

```csharp
// Base interface for method extraction strategies
public interface IMethodHandler
{
    /// <summary>
    /// Determines if this handler should process a method.
    /// </summary>
    bool Matches(MethodDeclarationSyntax method);
    
    /// <summary>
    /// Extracts portable metadata from a matched method.
    /// </summary>
    MethodInfo Extract(MethodDeclarationSyntax method);
    
    /// <summary>
    /// Generates wrapper method syntax for this handler's pattern.
    /// </summary>
    MethodDeclarationSyntax GenerateWrapper(MethodInfo coreMethod);
}

// Built-in handler for *Core protected virtual methods
public class CoreMethodHandler : IMethodHandler
{
    // Matches: protected virtual void *Core(IMauiElement element, ...)
    public bool Matches(MethodDeclarationSyntax method) => ...
    
    // Extract: name stem, parameters, doc, etc.
    public MethodInfo Extract(MethodDeclarationSyntax method) => ...
    
    // Generate: public wrapper with RunDoWithElement
    public MethodDeclarationSyntax GenerateWrapper(MethodInfo info) => ...
}
```

**Benefits:**
- Add new patterns without touching existing code
- Test each handler independently
- Support multiple handlers per class (future: async wrappers, proxy patterns, etc.)

### 4.2 Roslyn SyntaxFactory for Code Generation

Instead of StringBuilder, build syntax trees directly:

```csharp
// Generate a method using SyntaxFactory
var methodSyntax = SyntaxFactory.MethodDeclaration(
    SyntaxFactory.ParseTypeName("TScope"),
    SyntaxFactory.Identifier(publicMethodName))
    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
    .AddParameterListParameters(parameters)
    .WithBody(SyntaxFactory.Block(
        SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.IdentifierName("RunDoWithElement"))
                .WithArgumentList(SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(arguments))))));

// Format using CSharpFormatter
var options = new CSharpSyntaxFormattingOptions();
var formattedSyntax = Formatter.Format(methodSyntax, workspace, options);
var code = formattedSyntax.ToFullString();
```

**Benefits:**
- Roslyn handles all indentation, spacing, line breaks
- No manual string formatting
- Consistent with C# conventions automatically
- Easy to validate syntax tree before output

### 4.3 Code Generation Pipeline

```
Input File (.code.cs)
    ↓
[Roslyn Parser]
    ↓
[Find Class, Get Handlers]
    ↓
[For Each Handler]
    ├─ Check Matches() on each method
    ├─ Extract() portable metadata
    ├─ GenerateWrapper() syntax tree
    └─ Collect method syntax nodes
    ↓
[Build Class SyntaxTree]
    (combine original + generated methods)
    ↓
[Roslyn Formatter]
    (CSharpFormatter with workspace options)
    ↓
Output File (.gen.cs)
    (fully formatted, no manual tweaks)
```

### 4.4 Parameter Handling (Roslyn-aware)

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
  Brinell.Generator/                  ← Core library
    Brinell.Generator.csproj
    
    Handlers/
      IMethodHandler.cs               ← Pluggable interface
      CoreMethodHandler.cs            ← Built-in handler for *Core methods
      
    Models/
      MethodInfo.cs                   ← Portable method metadata
      HandlerOptions.cs               ← Configuration for handlers
      GeneratorOptions.cs             ← Overall generation config
      
    Analysis/
      CoreMethodAnalyzer.cs           ← Roslyn syntax tree parsing
      
    Generation/
      WrapperGenerator.cs             ← Roslyn SyntaxFactory builder
      CodeFormatter.cs                ← Roslyn CSharpFormatter
      
  Brinell.Generator.Cli/              ← CLI wrapper
    Brinell.Generator.Cli.csproj
    Program.cs                        ← Argument parsing, file I/O
    
srcnew/
  Brinell.Maui/
    Controls/
      ClickableControlBase.code.cs    ← Source (manual edits)
      ClickableControlBase.gen.cs     ← Generated (auto)
      ClickableControlBase.cs         ← Partial (combines both)
```

---

## 7. Benefits & Reusability

| Benefit | Impact |
|---------|--------|
| **DRY Principle** | Eliminates repetitive wrapper boilerplate |
| **Maintainability** | Change Core method → auto-update public wrapper |
| **Consistency** | All wrappers follow same pattern automatically |
| **Scaling** | Easy to apply to other control types (TextBox, Picker, etc.) |
| **Testing** | Fewer manual edits = fewer bugs |
| **Modularity** | `Brinell.Generator` lib can be reused by build tasks, MSBuild tools, IDEs |
| **CLI Separation** | `Brinell.Generator.Cli` decouples UI tool from core logic |
| **Pluggable Handlers** | Add new method patterns (async, proxy, decorator) without core changes |
| **Roslyn Formatting** | No manual formatting — Roslyn handles all indentation and spacing |

---

## 8. Alternative Approaches Considered

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

## 9. Next Steps

1. **Create Brinell.Generator.Cli spike:** New CLI console app in `tools/` that parses `ClickableControlBase.cs`
2. **Extract core method info** using Roslyn in `Brinell.Generator` library
3. **Build wrapper generator** with StringBuilder in `Brinell.Generator`
4. **Validate output** against current `ClickableControlBase` implementation
5. **Refactor:** Move Core method logic to `.code.cs`, add `.gen.cs` to build
6. **Extend:** Apply pattern to other control types
7. **Integrate:** Add as pre-build MSBuild task

---

## 10. Acceptance Criteria

- [x] `Brinell.Generator` library compiles without errors
- [x] `Brinell.Generator.Cli` wraps generator with argument parsing
- [x] `IMethodHandler` interface allows pluggable pattern matching
- [x] `CoreMethodHandler` correctly identifies and extracts `*Core` protected virtual methods
- [x] Generator reads `.code.cs` file without errors
- [x] Generates `.gen.cs` with identical wrapper methods as current manual code
- [x] Handles method parameter mapping correctly (preserves all except first IMauiElement)
- [x] Preserves XML documentation (if present)
- [x] Uses Roslyn `SyntaxFactory` to build method syntax trees
- [x] Uses Roslyn `CSharpFormatter` for output formatting
- [x] Output is consistent, properly indented, and formatted
- [x] Can be run as command-line tool: `dotnet run --project tools/Brinell.Generator.Cli --input file.code.cs --output file.gen.cs`
- [ ] Generated file compiles without warnings
- [ ] Zero manual edits needed to generated code
- [ ] Handler design allows adding new patterns (async, proxy, etc.) without core changes
- [ ] Refactor `ClickableControlBase` into `.code.cs` + `.gen.cs` structure
- [ ] Validate generated code matches current manual implementation
- [ ] Add MSBuild integration for pre-build generation
- [ ] Extend pattern to other control types (TextBoxControlBase, PickerControlBase, etc.)
