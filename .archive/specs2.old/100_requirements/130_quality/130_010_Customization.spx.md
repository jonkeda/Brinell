# 130_010 Customization

## quality Customization

- **attribute**: Extensibility
- **requirement**: Users can create custom controls and customize framework behavior
- **priority**: high

---

## Description

This requirement ensures users can extend the framework with custom control types, waiting strategies, and other customizations to meet specific testing needs.

---

## Sub-Requirements

### NFR-EXT-001.1: Custom Controls

- Users MUST be able to create custom control types
- Custom controls SHOULD be able to extend framework base classes
- Framework SHOULD provide extension points for custom behavior

### NFR-EXT-001.2: Custom Waiting Strategies

- Users SHOULD be able to define custom wait conditions
- Users SHOULD be able to override default timeouts
- Users SHOULD be able to customize polling intervals

---

## Custom Control Example

```csharp
// Custom control extending base class
public class CustomDatePicker : ControlBase, ITextControl
{
    public CustomDatePicker(AppiumElement element, IPageObject page)
        : base(element, page)
    {
    }
    
    public void SetDate(DateTime date)
    {
        var formatted = date.ToString("yyyy-MM-dd");
        Enter(formatted);
    }
    
    public DateTime GetDate()
    {
        var text = GetText();
        return DateTime.Parse(text);
    }
}
```

---

## Custom Wait Conditions

```csharp
// Custom wait condition
public class CustomWaitConditions
{
    public static Func<IControlObject, bool> HasMinimumLength(int length)
    {
        return control => control.GetText()?.Length >= length;
    }
}

// Usage
textField.WaitFor(CustomWaitConditions.HasMinimumLength(5));
```

---

## Configuration Overrides

```csharp
// Per-test timeout override
options.DefaultTimeout = TimeSpan.FromSeconds(10);
options.PollingInterval = TimeSpan.FromMilliseconds(100);

// Per-operation override
button.WaitVisible(true, timeoutMs: 5000);
```

---

## Extension Points

| Extension Point | Purpose | Example |
|-----------------|---------|---------|
| Control Base | Custom controls | DatePickerControl |
| Wait Condition | Custom conditions | WaitForAnimation |
| Logger | Custom logging | TestRailLogger |
| Screenshot | Custom capture | AnnotatedScreenshot |

---

## Related

- [FR-008 Extensibility](../120_functional/120_008_Extensibility.spx.md)
- [G-008 Extensible Framework](../110_goal/110_008_ExtensibleFramework.spx.md)

---

## Source

REQ-002-non-functional-requirements.md § NFR-EXT-001
