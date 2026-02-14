# 130_014 Internationalization

## quality Internationalization

- **attribute**: Localization
- **requirement**: Framework supports testing applications in multiple languages
- **priority**: high

---

## Description

This requirement ensures the framework can effectively test internationalized applications without depending on display text for element identification.

---

## Sub-Requirements

### NFR-I18N-001.1: Multi-Language UI Testing

- Framework SHOULD support testing applications in multiple languages
- Element finding SHOULD NOT depend on display text
- Framework SHOULD support culture-specific formatting

---

## Element Identification Strategy

### Preferred: Automation IDs

```csharp
// Good - language independent
var submitButton = page.GetControl<IButton>("SubmitButton");

// Bad - depends on display text
var submitButton = page.FindByText("Submit"); // Fails in French: "Soumettre"
```

### When Text is Required

```csharp
// Use localization resources
var localizedText = Resources.GetString("SubmitButton_Text", culture);
var button = page.FindByText(localizedText);
```

---

## Culture-Specific Considerations

### Date/Time Formatting

```csharp
// Verify date display matches expected format
var expectedDate = date.ToString("d", new CultureInfo("de-DE"));
dateField.AssertTextEquals(expectedDate);
```

### Number Formatting

```csharp
// Account for locale-specific decimal separators
var expectedPrice = price.ToString("C", new CultureInfo("fr-FR"));
priceLabel.AssertTextEquals(expectedPrice);
```

### Text Direction

```csharp
// RTL languages may affect layout
if (culture.TextInfo.IsRightToLeft)
{
    // Adjust expectations for RTL layout
}
```

---

## Test Patterns

### Multi-Language Test Suite

```csharp
[Theory]
[InlineData("en-US")]
[InlineData("de-DE")]
[InlineData("ja-JP")]
public void SubmitForm_InDifferentLocales_Succeeds(string cultureName)
{
    var culture = new CultureInfo(cultureName);
    App.SetCulture(culture);
    
    // Test using automation IDs, not display text
    LoginPage.UsernameField.Enter("user");
    LoginPage.PasswordField.Enter("pass");
    LoginPage.SubmitButton.Click();
    
    HomePage.AssertVisible();
}
```

---

## Best Practices

1. **Use Automation IDs** - Primary identification method
2. **Avoid Text Matching** - Display text varies by locale
3. **Parameterize Tests** - Run same tests across locales
4. **Resource Files** - Centralize expected strings
5. **Culture in Setup** - Configure locale before test

---

## Related

- [FR-002 Control Object Pattern](../120_functional/120_002_ControlObjectPattern.spx.md)
- [NFR-COMPAT-001 Platform Support](130_007_PlatformSupport.spx.md)

---

## Source

REQ-002-non-functional-requirements.md § NFR-I18N-001
