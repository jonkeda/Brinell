# Property Handler Plan: Is/Wait/Assert Pattern Generation

**Date:** 2026-07-08  
**Scope:** Extend code generator to handle Is/Wait/Assert property patterns (IsExists, IsVisible, IsEnabled, etc.)

---

## 1. Problem Statement

Currently, **ControlBase** and similar control classes repeat the **Is/Wait/Assert** pattern for multiple properties:

### Example: IsExists Pattern
```csharp
// Protected virtual core method
protected virtual bool? IsExistsCore(IMauiElement? element)
{
    return element != null;
}

// Public Is method
public bool IsExists()
{
    return IsExistsCore(TryFindElement()) == true;
}

// Public Wait method
public bool WaitExists(bool? expected, int? timeoutMs = null)
{
    return RunWaitWithElement(
        element => IsExistsCore(element) == expected!.Value,
        timeoutMs);
}

// Public Assert method
public TScope AssertExists(bool? expected, string? message = null, int? timeoutMs = null)
    => AssertExists(true, message, timeoutMs);

public TScope AssertExists(bool? expected, string? message = null, int? timeoutMs = null)
{
    return RunAssertWithElement(expected,
        IsExistsCore, (actual, expected1) => (actual == expected1),
        null, timeoutMs);
}
```

This pattern is repeated for:
- `IsVisibleCore` → `IsVisible()`, `WaitVisible()`, `AssertVisible()`
- `IsEnabledCore` → `IsEnabled()`, `WaitEnabled()`, `AssertEnabled()`
- `IsExistsCore` → `IsExists()`, `WaitExists()`, `AssertExists()`
- `GetTextCore` → `GetText()`, `WaitText()`, `AssertText*()` (multiple variants)
- `GetAttributeCore` → `GetAttribute()`, `WaitAttribute()`, `AssertAttribute()`

**Current Status:** Only `CoreMethodHandler` exists (handles public wrappers for Core methods).

**Goal:** Create `IsPropertyHandler` implementation to auto-generate all Is/Wait/Assert variants.

---

## 2. Solution: Property Handler Architecture

### 2.1 High-Level Design

```
Input File (ControlBase.code.cs)
    ↓
[Identify Is*Core protected virtual methods]
    ↓
[PropertyHandler matches and extracts]
    ↓
[Generate Is*, Wait*, Assert* methods]
    ↓
Output File (ControlBase.gen.cs)
```

### 2.2 Handler Pattern Recognition

The **PropertyHandler** will match methods following this signature:

```csharp
// Pattern 1: bool? return type (binary properties)
protected virtual bool? Is<PropertyName>Core(IMauiElement? element)
{
    // implementation
}

// Pattern 2: string? return type (value properties)
protected virtual string? Get<PropertyName>Core(IMauiElement element)
{
    // implementation
}

// Pattern 3: Other types
protected virtual <T>? Get<PropertyName>Core(IMauiElement element)
{
    // implementation
}
```

---

## 3. Handler Implementation

### 3.1 IsPropertyHandler

```csharp
public class IsPropertyHandler : IMethodHandler
{
    private readonly HandlerOptions _options;

    public IsPropertyHandler(HandlerOptions? options = null)
    {
        _options = options ?? new HandlerOptions { MethodPrefix = "Is", MethodSuffix = "Core" };
    }

    /// <summary>
    /// Matches methods like: protected virtual bool? IsExistsCore(IMauiElement? element)
    /// </summary>
    public bool Matches(MethodDeclarationSyntax method)
    {
        var methodName = method.Identifier.Text;
        
        // Check name pattern: Is*Core
        if (!methodName.StartsWith(_options.MethodPrefix) || !methodName.EndsWith(_options.MethodSuffix))
            return false;
        
        // Check return type is bool? (nullable bool)
        if (method.ReturnType.ToString() != "bool?")
            return false;
        
        var modifiers = method.Modifiers;
        if (_options.RequireProtected && !modifiers.Any(m => m.IsKind(SyntaxKind.ProtectedKeyword)))
            return false;
        if (_options.RequireVirtual && !modifiers.Any(m => m.IsKind(SyntaxKind.VirtualKeyword)))
            return false;
        
        // Ensure first parameter is IMauiElement (nullable or not)
        if (method.ParameterList.Parameters.Count == 0)
            return false;
        
        var firstParam = method.ParameterList.Parameters[0];
        var paramType = firstParam.Type!.ToString();
        if (!paramType.Contains("IMauiElement"))
            return false;
        
        return true;
    }

    /// <summary>
    /// Extracts method metadata from IsExistsCore pattern.
    /// Property name is derived by stripping "Is" prefix and "Core" suffix.
    /// </summary>
    public MethodInfo Extract(MethodDeclarationSyntax method)
    {
        var methodName = method.Identifier.Text;
        
        // Extract property name: "IsExistsCore" → "Exists"
        var prefixLength = _options.MethodPrefix.Length;
        var suffixLength = _options.MethodSuffix.Length;
        var propertyName = methodName.Substring(prefixLength, methodName.Length - prefixLength - suffixLength);
        
        return new MethodInfo
        {
            MethodName = methodName,
            PublicMethodName = propertyName,  // Used as base for Is*, Wait*, Assert*
            ReturnType = "bool?",
            XmlDocumentation = ExtractXmlDoc(method)
        };
    }

    /// <summary>
    /// Generates Is*, Wait*, and Assert* methods using CsWriter.
    /// Each property generates three public methods.
    /// </summary>
    public string GenerateWrapper(MethodInfo coreMethod, string containingTypeName, string typeParameters)
    {
        var propertyName = coreMethod.PublicMethodName;
        var writer = new CsWriter(0);

        // Generate IsExists() method
        GenerateIsMethod(writer, coreMethod, propertyName);
        writer.WriteLine();

        // Generate WaitExists(bool? expected, int? timeoutMs = null) method
        GenerateWaitMethod(writer, coreMethod, propertyName);
        writer.WriteLine();

        // Generate AssertExists(bool? expected, string? message = null, int? timeoutMs = null) method
        GenerateAssertMethod(writer, coreMethod, propertyName);

        return writer.ToString();
    }

    private void GenerateIsMethod(CsWriter writer, MethodInfo coreMethod, string propertyName)
    {
        // public bool IsExists()
        // {
        //     return IsExistsCore(TryFindElement()) == true;
        // }
        
        writer.WriteLine($"public bool Is{propertyName}()");
        writer.Open();
        writer.WriteLine($"return {coreMethod.MethodName}(TryFindElement()) == true;");
        writer.Close();
    }

    private void GenerateWaitMethod(CsWriter writer, MethodInfo coreMethod, string propertyName)
    {
        // public bool WaitExists(bool? expected, int? timeoutMs = null)
        // {
        //     if (expected == null) return true;
        //     return RunWaitWithElement(
        //         element => IsExistsCore(element) == expected.Value,
        //         timeoutMs);
        // }
        
        writer.WriteLine($"public bool Wait{propertyName}(bool? expected, int? timeoutMs = null)");
        writer.Open();
        writer.WriteLine("if (expected == null) return true;");
        writer.WriteLine("return RunWaitWithElement(");
        writer.Indent();
        writer.WriteLine($"element => {coreMethod.MethodName}(element) == expected.Value,");
        writer.WriteLine("timeoutMs);");
        writer.Dedent();
        writer.Close();
    }

    private void GenerateAssertMethod(CsWriter writer, MethodInfo coreMethod, string propertyName)
    {
        // public TScope AssertExists(bool? expected, string? message = null, int? timeoutMs = null)
        // {
        //     return RunAssertWithElement(expected,
        //         IsExistsCore, (actual, expected1) => (actual == expected1),
        //         null, timeoutMs);
        // }
        
        writer.WriteLine($"public TScope Assert{propertyName}(bool? expected, string? message = null, int? timeoutMs = null)");
        writer.Open();
        writer.WriteLine("return RunAssertWithElement(expected,");
        writer.Indent();
        writer.WriteLine($"{coreMethod.MethodName}, (actual, expected1) => (actual == expected1),");
        writer.WriteLine("null, timeoutMs);");
        writer.Dedent();
        writer.Close();
    }

    private string? ExtractXmlDoc(MethodDeclarationSyntax method)
    {
        var leadingTrivia = method.GetLeadingTrivia();
        var xmlDoc = leadingTrivia.FirstOrDefault(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia));
        return xmlDoc != default ? xmlDoc.ToString() : null;
    }
}
```

---

## 4. Integration with Generator Pipeline

### 4.1 Updated Method Analyzer

```csharp
public class PropertyMethodAnalyzer
{
    private readonly List<IMethodHandler> _handlers;

    public PropertyMethodAnalyzer(IEnumerable<IMethodHandler>? handlers = null)
    {
        _handlers = handlers?.ToList() ?? new List<IMethodHandler>
        {
            new IsPropertyHandler()
        };
    }

    /// <summary>
    /// Analyzes a class for property patterns and returns Is/Wait/Assert method groups to generate.
    /// </summary>
    public List<PropertyMethodGroup> Analyze(ClassDeclarationSyntax classDecl)
    {
        var groups = new List<PropertyMethodGroup>();

        foreach (var method in classDecl.Members.OfType<MethodDeclarationSyntax>())
        {
            foreach (var handler in _handlers)
            {
                if (handler.Matches(method))
                {
                    var coreMethod = handler.Extract(method);

                    groups.Add(new PropertyMethodGroup
                    {
                        CoreMethod = coreMethod,
                        Handler = handler
                    });
                    break;
                }
            }
        }

        return groups;
    }
}

public class PropertyMethodGroup
{
    public MethodInfo CoreMethod { get; set; } = new();
    public IMethodHandler Handler { get; set; } = null!;
}
```
```

### 4.2 Updated Wrapper Generator

```csharp
public class PropertyWrapperGenerator
{
    /// <summary>
    /// Generates Is*, Wait*, and Assert* methods for property groups using CsWriter.
    /// </summary>
    public string GenerateMethods(PropertyMethodGroup group)
    {
        return group.Handler.GenerateWrapper(group.CoreMethod, "", "");
    }
}
```

---

## 5. File Structure

```
tools/Brinell.Generator/
  Handlers/
    IMethodHandler.cs           ← Existing
    CoreMethodHandler.cs        ← Existing
    IsPropertyHandler.cs        ← NEW (implements IMethodHandler)
    
  Models/
    MethodInfo.cs               ← Existing
    PropertyMethodGroup.cs      ← NEW
    
  Analysis/
    CoreMethodAnalyzer.cs       ← Existing
    PropertyMethodAnalyzer.cs   ← NEW
    
  Generation/
    WrapperGenerator.cs         ← Existing
    PropertyWrapperGenerator.cs ← NEW
```

---

## 6. Usage Example

### Input: ControlBase.code.cs
```csharp
protected virtual bool? IsExistsCore(IMauiElement? element)
{
    return element != null;
}

protected virtual bool? IsVisibleCore(IMauiElement? element)
{
    return element?.Visible;
}
```

### Generator Invocation
```powershell
dotnet run --project tools/Brinell.Generator.Cli `
  --input srcnew/Brinell.Maui/Controls/ControlBase.code.cs `
  --output srcnew/Brinell.Maui/Controls/ControlBase.gen.cs `
  --handlers "CoreMethodHandler,IsPropertyHandler"
```

### Output: ControlBase.gen.cs
```csharp
// IsExistsCore pattern
public bool IsExists()
{
    return IsExistsCore(TryFindElement()) == true;
}

public bool WaitExists(bool? expected, int? timeoutMs = null)
{
    if (expected == null) return true;
    return RunWaitWithElement(
        element => IsExistsCore(element) == expected.Value,
        timeoutMs);
}

public TScope AssertExists(bool? expected, string? message = null, int? timeoutMs = null)
{
    return RunAssertWithElement(expected,
        IsExistsCore, (actual, expected1) => (actual == expected1),
        null, timeoutMs);
}

// IsVisibleCore pattern
public bool IsVisible()
{
    return IsVisibleCore(TryFindElement()) == true;
}

public bool WaitVisible(bool? expected, int? timeoutMs = null)
{
    if (expected == null) return true;
    return RunWaitWithElement(
        element => IsVisibleCore(element) == expected.Value,
        timeoutMs);
}

public TScope AssertVisible(bool? expected, string? message = null, int? timeoutMs = null)
{
    return RunAssertWithElement(expected,
        IsVisibleCore, (actual, expected1) => (actual == expected1),
        null, timeoutMs);
}
```

---

## 7. Acceptance Criteria

- [x] `IsPropertyHandler` implementation handles `Is*Core(IMauiElement?)` → `bool?` pattern
- [x] `IsPropertyHandler.Matches()` correctly identifies Is*Core methods with protected/virtual modifiers
- [x] `IsPropertyHandler.Extract()` correctly extracts property name by stripping prefix/suffix
- [x] `IsPropertyHandler.GenerateWrapper()` uses CsWriter to generate Is*(), Wait*(), and Assert*() methods
- [x] Generated Is*() method returns bool and calls TryFindElement()
- [x] Generated Wait*() method accepts bool? expected and int? timeoutMs, includes null-check optimization
- [x] Generated Assert*() method accepts bool? expected, string? message, int? timeoutMs
- [x] `PropertyMethodAnalyzer` identifies and extracts all property patterns
- [x] `PropertyWrapperGenerator.GenerateMethods()` delegates to handler's GenerateWrapper() method
- [x] CLI supports `--handlers` argument to select handlers (e.g., `CoreMethodHandler,IsPropertyHandler`)
- [x] Generated methods compile without errors
- [x] Generated Is/Wait/Assert methods match current ControlBase implementation exactly
- [x] Handler design allows adding new patterns (GetPropertyHandler for string/value properties)
- [x] XML documentation preserved in generated methods (if present in Core method)

---

## 8. Future Extensions

### GetPropertyHandler (string/value properties)
Handles patterns like:
```csharp
protected virtual string? GetTextCore(IMauiElement element)
protected virtual string? GetAttributeCore(IMauiElement element, string name)
```

Generates: `GetText()`, `WaitText()`, `AssertText()`, `AssertTextContains()`, etc.

### Generic Value PropertyHandler
For typed properties:
```csharp
protected virtual int? GetCountCore(IMauiElement element)
```

---

## 9. Scope & Constraints

- **Scope:** Only Is/Wait/Assert property patterns (bool? return type)
- **Constraint:** First parameter must be `IMauiElement` or `IMauiElement?`
- **Constraint:** Must be `protected virtual`
- **Not included:** Get* patterns (separate phase after Is* is complete)

