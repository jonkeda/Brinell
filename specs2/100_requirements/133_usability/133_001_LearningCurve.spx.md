# 133_001 Learning Curve

## usability LearningCurve

- **title**: Intuitive API and Fast Onboarding
- **requirement**: New users can write tests quickly with minimal documentation
- **priority**: high

---

## Description

This requirement ensures the framework is easy to learn and use, with intuitive APIs that follow common patterns and provide a smooth onboarding experience.

---

## Sub-Requirements

### NFR-USE-001.1: Intuitive API

- API design SHOULD follow common patterns and conventions
- Method names SHOULD be self-explanatory
- The framework SHOULD minimize boilerplate code

### NFR-USE-001.2: Getting Started

- New users SHOULD be able to write first test within 30 minutes
- Framework SHOULD provide working examples
- Framework SHOULD provide project templates

---

## Acceptance Criteria

- User testing shows first test written in < 30 minutes
- Sample projects compile and run out of the box
- API follows .NET naming conventions

---

## API Design Principles

### Self-Explanatory Methods

```csharp
// Good - clear intent
button.Click();
textField.Enter("hello");
label.AssertTextEquals("Expected");

// Bad - unclear
button.Do();
textField.Set("hello");
label.Verify("Expected");
```

### Minimal Boilerplate

```csharp
// Good - fluent, minimal
LoginPage.Username.Enter("user");
LoginPage.Password.Enter("pass");
LoginPage.LoginButton.Click();

// Bad - verbose
var page = GetPage<LoginPage>();
var usernameField = page.GetControl("Username");
usernameField.WaitForVisible();
usernameField.ClearText();
usernameField.EnterText("user");
```

---

## Related

- [G-004 Fast Test Development](../110_goal/110_004_FastTestDevelopment.spx.md)
- [G-005 Easy Onboarding](../110_goal/110_005_EasyOnboarding.spx.md)
- [NFR-MAINT-003 Documentation](../130_quality/130_003_Documentation.spx.md)

---

## Source

REQ-002-non-functional-requirements.md § NFR-USE-001
