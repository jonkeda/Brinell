# functional SampleApplications
- **id**: FR-950
- **title**: Sample Applications per Technology
- **priority**: high
- **status**: draft
- **category**: Testing Infrastructure

Each technology supported by Brinell must have a dedicated sample application for testing purposes.

## capabilities

### TechnologyCoverage
- **id**: FR-950.1
- **title**: Sample app per technology

Each supported technology must have its own sample application:

| Technology | Sample App | Location |
|------------|------------|----------|
| MAUI | Brinell.Samples.Maui.App | samples/Brinell.Samples.Maui.App/ |
| Blazor | Brinell.Samples.Blazor.App | samples/Brinell.Samples.Blazor.App/ |
| WPF | Brinell.Samples.Wpf.App | samples/Brinell.Samples.Wpf.App/ |
| WinForms | Brinell.Samples.WinForms.App | samples/Brinell.Samples.WinForms.App/ |
| Stride | Brinell.Samples.Stride.App | samples/Brinell.Samples.Stride.App/ |

### SampleAppPurpose
- **id**: FR-950.2
- **title**: Sample app purpose

Sample applications serve these purposes:
1. **ControlObject Testing** — Provide real UI controls for UI tests
2. **Development Reference** — Show how controls are used in real apps
3. **Regression Testing** — Stable target for framework regression tests
4. **Documentation** — Living documentation of control implementations

### ControlCoverage
- **id**: FR-950.3
- **title**: All ControlObjects must have sample controls

Every control type that has a ControlObject implementation must:
1. Have at least one corresponding control in the sample app
2. Controls must be accessible via automation ID
3. Controls must be in a testable state (visible, enabled where applicable)
4. Controls must demonstrate typical usage patterns

### ControlPageOrganization
- **id**: FR-950.4
- **title**: Control page organization

Sample apps should organize controls logically:

| Page/View | Control Categories |
|-----------|-------------------|
| Basic Controls | Button, Label, Entry/TextBox, CheckBox |
| Selection Controls | Dropdown, ListBox, RadioGroup, ComboBox |
| Advanced Controls | Slider, DatePicker, Tab, Accordion |
| Data Controls | DataGrid, ListView, TreeView |
| Form Controls | Form layout with validation examples |
| Navigation | Navigation patterns, menu, tabs |

### AutomationIdentifiers
- **id**: FR-950.5
- **title**: Automation identifiers

All testable controls must have:
1. **Unique AutomationId** — Each control has unique identifier
2. **Consistent Naming** — Follow naming convention: `{ControlType}_{Purpose}`
3. **Documented IDs** — All IDs documented in sample app README

Examples:
```
Button_Submit
Entry_Username
CheckBox_RememberMe
Dropdown_Country
```

### SampleAppStability
- **id**: FR-950.6
- **title**: Sample app stability requirements

Sample applications must:
1. **Build reliably** — No warnings, clean build
2. **Run standalone** — Can be launched independently
3. **No external dependencies** — No network/database required for basic controls
4. **Deterministic state** — Controls start in known state
5. **Version controlled** — Changes tracked, no breaking changes without notice

### ControlVariants
- **id**: FR-950.7
- **title**: Control variants and states

For each control type, sample app should demonstrate:

| Aspect | Required Variants |
|--------|-------------------|
| States | Enabled, Disabled |
| Visibility | Visible, Hidden (toggle-able) |
| Content | Empty, With data, With long text |
| Validation | Valid state, Error state |

---

## relationships

- Sample apps are targets for [FR-960 Unit Tests](120_960_UnitTests.spx.md)
- Sample apps are targets for [FR-970 UI Tests](120_970_UITests.spx.md)
- Control types defined in [FR-100 ControlObject](120_100_ControlObject.spx.md)

---

## constraints

- Sample apps must not include production business logic
- Sample apps must be minimal — only what's needed for testing
- Sample apps must follow platform best practices
- Sample apps should start quickly (< 5 seconds to interactive)

