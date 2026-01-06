# 130_013 Standards Compliance

## quality StandardsCompliance

- **attribute**: Compliance
- **requirement**: Code follows Microsoft C# coding standards and passes static analysis
- **priority**: high

---

## Description

This requirement ensures framework code adheres to industry standards for C# development, maintaining consistency and quality across the codebase.

---

## Sub-Requirements

### NFR-COMP-002.1: Coding Standards

- Code SHOULD follow Microsoft C# coding standards
- Code SHOULD pass static analysis (StyleCop, FxCop)
- Code SHOULD have consistent formatting

---

## Coding Standards Reference

### Microsoft Guidelines

- [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [Framework Design Guidelines](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/)
- [Naming Guidelines](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/naming-guidelines)

---

## Static Analysis Tools

### EditorConfig

```ini
# .editorconfig
root = true

[*.cs]
indent_style = space
indent_size = 4
charset = utf-8
trim_trailing_whitespace = true
insert_final_newline = true

# Naming conventions
dotnet_naming_rule.interface_should_be_begins_with_i.severity = warning
dotnet_naming_style.begins_with_i.required_prefix = I
```

### Analyzers

```xml
<!-- Directory.Build.props -->
<ItemGroup>
  <PackageReference Include="Microsoft.CodeAnalysis.NetAnalyzers" Version="8.0.0" />
  <PackageReference Include="StyleCop.Analyzers" Version="1.2.0-beta.556" />
</ItemGroup>
```

---

## Enforcement

| Check | Tool | CI Integration |
|-------|------|----------------|
| Formatting | dotnet format | Pre-commit hook |
| Naming | Roslyn analyzers | Build warnings |
| Style | StyleCop | Build warnings |
| Security | SecureCodeAnalysis | Build errors |

---

## CI Validation

```yaml
- name: Check Formatting
  run: dotnet format --verify-no-changes

- name: Build with Warnings as Errors
  run: dotnet build /p:TreatWarningsAsErrors=true
```

---

## Related

- [NFR-MAINT-002 Code Quality](130_005_CodeQuality.spx.md)
- [NFR-MAINT-001 Code Organization](130_004_CodeOrganization.spx.md)

---

## Source

REQ-002-non-functional-requirements.md § NFR-COMP-002
