# Test 5.4 — Control Type Validator Tests

**Covers:** Step 5.10 — `CodeValidator.ValidateWithRegistry()` (control type checking against dynamic registry)

**File:** `Brinell.Scraper.Tests/Services/ControlTypeValidatorTests.cs`

## Test Inventory (4 tests)

| # | Test Name | Assertion |
|---|-----------|-----------|
| 1 | `ValidateWithRegistry_BuiltInTypes_NoWarnings` | Code using only built-in types (`TextInputControl`, `ButtonControl`, `SelectControl`, etc.) produces no warnings |
| 2 | `ValidateWithRegistry_CustomTypeFromRegistry_NoWarning` | Register `DatePickerControl` in a mock `IControlRegistry`; code using `DatePickerControl<LoginPage>` produces no warnings |
| 3 | `ValidateWithRegistry_UnknownType_WarnsUser` | Code using `FancyWidgetControl<LoginPage>` (not in built-in or registry) produces a warning: "Unknown control type: 'FancyWidgetControl'" |
| 4 | `ValidateWithRegistry_MixedKnownAndUnknown_WarnsOnlyUnknown` | Code with `TextInputControl` (known), `DatePickerControl` (registered), and `MysteryControl` (unknown) — only `MysteryControl` produces a warning |

## Notes

- Use a mock or in-memory `IControlRegistry` implementation for tests.
- Built-in types list: `TextInputControl`, `ButtonControl`, `SelectControl`, `LabelControl`, `CheckBoxControl`, `RadioButtonControl`, `LinkControl`, `FileInputControl`, `TextAreaControl`, `ImageControl`, `ElementControl`.
- Framework types (`HtmlPageObjectBase`, `ContainerBase`, `List`, `Task`) should not trigger warnings.
- Warnings are separate from errors — syntax errors are checked by `Validate()` first.
- Test that the registry is queried (verify the mock was called).
- Unknown types are **warnings**, not errors — `IsValid` should still be `true` if syntax is correct.
